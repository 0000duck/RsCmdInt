using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using ABB.Robotics.Math;
using ABB.Robotics.RobotStudio;
using ABB.Robotics.RobotStudio.Environment;
using ABB.Robotics.RobotStudio.Stations;
using ABB.Robotics.RobotStudio.Stations.Forms;
using System.Collections.Generic;
using System.Collections;
using ABB.Robotics.Controllers.RapidDomain;

namespace RST.Framework
{
    public class FunctionCollection
    {
        static List<RsTarget> targets = new List<RsTarget>();

        public static SynchronizationContext MainThread;

        public static void AddinMain()
        {
            MainThread = SynchronizationContext.Current;
            if (MainThread == null) MainThread = new SynchronizationContext();
            CreateButton();
        }

        public static void SocketServer()
        {
            SyncServer.StartListening();
        }

        private static void CreateButton()
        {
            //Begin UndoStep
            Project.UndoContext.BeginUndoStep("Add Buttons");
            try
            {
                // Create a new tab.
                RibbonTab ribbonTab = new RibbonTab("Automation", "Automation");
                UIEnvironment.RibbonTabs.Add(ribbonTab);
                //make tab as active tab
                UIEnvironment.ActiveRibbonTab = ribbonTab;

                // Create a group for buttons
                RibbonGroup ribbonGroup = new RibbonGroup("Automation", "Automation");

                // Create first small button
                CommandBarButton buttonFirst = new CommandBarButton("Start server", "Start server");
                buttonFirst.HelpText = "Click to start socket-server";
                buttonFirst.Image = Image.FromFile(@"H:\Examensarbete\knapp.jpg");
                buttonFirst.DefaultEnabled = true;
                ribbonGroup.Controls.Add(buttonFirst);


                // Set the size of the buttons.
                RibbonControlLayout[] ribbonControlLayout = { RibbonControlLayout.Small, RibbonControlLayout.Large };
                ribbonGroup.SetControlLayout(buttonFirst, ribbonControlLayout[0]);

                //Add ribbon group to ribbon tab
                ribbonTab.Groups.Add(ribbonGroup);

                // Add an event handler.
                buttonFirst.UpdateCommandUI += new UpdateCommandUIEventHandler(button_UpdateCommandUI);
                // Add an event handler for pressing the button.
                buttonFirst.ExecuteCommand += new ExecuteCommandEventHandler(button_ExecuteCommand);

            }
            catch (Exception ex)
            {
                Project.UndoContext.CancelUndoStep(CancelUndoStepType.Rollback);
                Logger.AddMessage(new LogMessage(ex.Message.ToString()));
            }
            finally
            {
                Project.UndoContext.EndUndoStep();
            }
        }

        private static void button_ExecuteCommand(object sender, ExecuteCommandEventArgs e)
        {
            Thread listenThread = new Thread(new ThreadStart(SocketServer));
            listenThread.Start();
        }

        private static void button_UpdateCommandUI(object sender, UpdateCommandUIEventArgs e)
        {
            // This enables the button, instead of "button1.Enabled = true".
            e.Enabled = true;
        }

        public static string LoadStation(string stationFilepath)
        {
            Station station = Station.Load(stationFilepath, false);
            if (station != null)
            {
                Project.ActiveProject = station;
                GraphicControl gc = new GraphicControl();
                gc.RootObject = station;
                DocumentWindow docwindow = new DocumentWindow();
                docwindow.Control = gc;
                docwindow.Caption = System.IO.Path.GetFileName("View");
                UIEnvironment.Windows.Add(docwindow);                
                string test = ABB.Robotics.RobotStudio.Controllers.ControllerType.StationVC.ToString();

                Logger.AddMessage(new LogMessage(test, "MyKey"));
            }

            RsTask task = station.ActiveTask;
            RsIrc5Controller rscontroller = (RsIrc5Controller)task.Parent;

            return "Loading station: " + stationFilepath;
        }

        public static string CloseStation()
        {
            Station station = Project.ActiveProject as Station;
            DocumentWindow.ActiveDocumentWindow.Close();
            Logger.Clear();
            station.Close();
            return "Closing station";
        }

        public static string LoadModuleFromFile(string moduleFilePath)
        {
            //Get Station object           
            Station station = Project.ActiveProject as Station;

            //Check for existance of Module 
            if (System.IO.File.Exists(moduleFilePath))
            {
                try
                {
                    RsTask task = station.ActiveTask;
                    if (task != null)
                    {
                        RsIrc5Controller rsIrc5Controller = (RsIrc5Controller)task.Parent;
                        ABB.Robotics.Controllers.Controller controller =
                            new ABB.Robotics.Controllers.Controller(new Guid(rsIrc5Controller.SystemId.ToString()));

                        if (controller != null)
                        {
                            //Request Mastership           
                            using (ABB.Robotics.Controllers.Mastership m =
                                    ABB.Robotics.Controllers.Mastership.Request(controller.Rapid))
                            {
                                if (controller.Rapid.ExecutionStatus ==
                                           ABB.Robotics.Controllers.RapidDomain.ExecutionStatus.Stopped)
                                {
                                    //Load Module if Rapid Execution State is stopped
                                    ABB.Robotics.Controllers.RapidDomain.Task vTask = controller.Rapid.GetTask(task.Name);
                                    bool loadResult = vTask.LoadModuleFromFile(moduleFilePath,
                                        ABB.Robotics.Controllers.RapidDomain.RapidLoadMode.Replace);
                                    Thread.Sleep(1000);
                                }
                            }
                        }
                    }
                }
                catch (ABB.Robotics.GeneralException gex)
                {
                    Logger.AddMessage(new LogMessage(gex.Message.ToString()));
                }

                catch (Exception ex)
                {
                    Logger.AddMessage(new LogMessage(ex.Message.ToString()));
                }

            }
            return "Loading module: " + moduleFilePath;
        }

        public static string AddTarget(string targetName, double x, double y, double z)
        {
            //Begin UndoStep
            Project.UndoContext.BeginUndoStep("CreateTarget");

            try
            {
                // Adding target
                ShowTarget(targetName, new Vector3((x/1000), (y/1000), (z/1000)));
            }

            catch (Exception exception)
            {
                Project.UndoContext.CancelUndoStep(CancelUndoStepType.Rollback);
                Logger.AddMessage(new LogMessage(exception.Message.ToString()));
            }
            finally
            {
                //End UndoStep
                Project.UndoContext.EndUndoStep();
            }
            return "Added target: " + x + "; " + y + "; " + z ;
        }

        public static string AddHome()
        {
            Station station = Project.ActiveProject as Station;

            try
            {
                foreach (RsTarget target in station.ActiveTask.Targets)
                {
                    if (target.Name == "robot_home")
                    {
                        Logger.AddMessage(new LogMessage(target.Name, "MyKey"));
                        targets.Add(target);
                    }
                }
            }

            catch (Exception exception)
            {
                Logger.AddMessage(new LogMessage(exception.Message.ToString()));
            }
            return "Added robot home";
        }

        public static string RunSimulation()
        {
            Station station = Project.ActiveProject as Station;

            try
            {
                Simulator.Start();
            }

            catch (Exception exception)
            {
                Logger.AddMessage(new LogMessage(exception.Message.ToString()));
            }
            return "Simulator: Started";
        }

        public static string GetLog()
        {
            LogMessage[] log;
            string retString = "";
            log = Logger.GetMessages();
            int i = log.GetLength(0);
            for (int j = 0; j < i; j++)
            {
                if (log[j].Severity == LogMessageSeverity.Warning || log[j].Severity == LogMessageSeverity.Error)
                {
                    Logger.AddMessage(new LogMessage(log[j].Text.ToString(), "MyKey"));
                    retString = retString + "\n" + log[j].Text.ToString();
                }
            
            }
            Logger.AddMessage(new LogMessage(retString, "MyKey"));
            return retString;
        }

        private static void ShowTarget(string targetName, Vector3 position)
        {
            try
            {
                //get the active station
                Station station = Project.ActiveProject as Station;

                //create robtarget
                RsRobTarget robTarget = new RsRobTarget();
                robTarget.Name = station.ActiveTask.GetValidRapidName(targetName, "_", 10);

                //translation
                robTarget.Frame.Translation = position;

                //add robtargets to datadeclaration
                station.ActiveTask.DataDeclarations.Add(robTarget);
                //create target
                RsTarget target = new RsTarget(station.ActiveTask.ActiveWorkObject, robTarget);
                target.Name = robTarget.Name;
                target.Attributes.Add(target.Name, true);

                //add targets to active task
                station.ActiveTask.Targets.Add(target);

                // add target to list
                targets.Add(target);

            }
            catch (Exception exception)
            {
                Logger.AddMessage(new LogMessage(exception.Message.ToString()));
            }
        }

        public static string CreatePath(string procedureName)
        {
            Project.UndoContext.BeginUndoStep("RsPathProcedure Create");

            try
            {
                //Get the active Station
                Station station = Project.ActiveProject as Station;
                // Create a PathProcedure.
                RsPathProcedure myPath = new RsPathProcedure(procedureName);

                // Add the path to the ActiveTask.
                station.ActiveTask.PathProcedures.Add(myPath);
                myPath.ModuleName = "module1";

                myPath.ShowName = true;
                myPath.Synchronize = true;
                myPath.Visible = true;

                //Make the path procedure as active path procedure
                station.ActiveTask.ActivePathProcedure = myPath;

                //Create Path 
                foreach (RsTarget target in targets)
                {
                    RsMoveInstruction moveInstruction =
                        new RsMoveInstruction(station.ActiveTask, "Move", "Default",
                        MotionType.Linear, target.WorkObject.Name,
                        target.Name, station.ActiveTask.ActiveTool.Name);

                    myPath.Instructions.Add(moveInstruction);

                }
                ArrayList messages = new ArrayList();

                station.ActiveTask.SyncPathProcedure(myPath.ModuleName + "/" + myPath.Name,
                SyncDirection.ToController,
                messages);

                targets.Clear();
            }
            catch (Exception ex)
            {
                Project.UndoContext.CancelUndoStep(CancelUndoStepType.Rollback);
                Logger.AddMessage(new LogMessage(ex.Message.ToString()));
            }
            finally
            {
                Project.UndoContext.EndUndoStep();
            }
            return "Path " + procedureName + " created";
        }

        public static string CreateCollisionSet(string firstobjects, string secondobjects, double nmDistance)
        {
            Station station = Project.ActiveProject as Station;
            nmDistance = nmDistance / 1000;
            CollisionSet cs = new CollisionSet();
            CollisionDetector.Collision += new CollisionEventHandler(MyCollisionEventHandler);
            cs.Name = "CSet";

            cs.NearMissDistance = nmDistance;

            cs.Active = true;

            station.CollisionSets.Add(cs);


            GraphicComponent a, b;
            station.GraphicComponents.TryGetGraphicComponent(firstobjects, out a);
            station.GraphicComponents.TryGetGraphicComponent(secondobjects, out b);
            cs.FirstGroup.Add(a);
            cs.SecondGroup.Add(b);

            CollisionDetector.CheckCollisions(station);
            CollisionDetector.CheckCollisions(cs);

            return "Checking collisions";
        }

        private static void MyCollisionEventHandler(object sender, CollisionEventArgs e)
        {
            switch (e.CollisionEvent)
            {
                case CollisionEvent.CollisionStarted:
                    Logger.AddMessage
                    (new LogMessage("Collision started by collision set: '" + e.CollisionSet.Name
                    + "' First part : '" + e.FirstPart.Name
                    + "' Second part: '" + e.SecondPart.Name + "'"));
                    break;
                case CollisionEvent.CollisionEnded:
                    Logger.AddMessage(new LogMessage("Collision ended by collision set: '" + e.CollisionSet.Name
                    + "' First part : '" + e.FirstPart.Name
                    + "' Second part: '" + e.SecondPart.Name + "'"));
                    break;
                case CollisionEvent.NearMissStarted:
                    Logger.AddMessage(new LogMessage("Near Miss started by collision set: '" + e.CollisionSet.Name
                    + "' First part : '" + e.FirstPart.Name
                    + "' Second part: '" + e.SecondPart.Name + "'"));
                    break;
                case CollisionEvent.NearMissEnded:
                    Logger.AddMessage(new LogMessage("Near Miss ended by collision set: '" + e.CollisionSet.Name
                    + "' First part : '"
                    + e.FirstPart.Name + "' Second part: '" + e.SecondPart.Name + "'"));
                    break;
                default:
                    break;
            }
        }

        public static string CheckControllerStatus()
        {
            Station station = Station.ActiveStation as Station;
            RsTask task = station.ActiveTask;
            RsIrc5Controller rsIrc5Controller = (RsIrc5Controller)task.Parent;

            return "Controller: " + rsIrc5Controller.SystemState.ToString();
        }

        public static string CheckSimulationStatus()
        {
            return "Simulator: " + Simulator.State.ToString();
        }

        public static string SaveRapid(string filePath)
        {
            string result = "false";
            try
            {
                Station station = Project.ActiveProject as Station;
                RsTask rsTask = station.ActiveTask;
                RsIrc5Controller rsIrc5Controller = (RsIrc5Controller)rsTask.Parent;

                ABB.Robotics.Controllers.Controller controller = new ABB.Robotics.Controllers.Controller(new Guid(rsIrc5Controller.SystemId.ToString()));

                Task controllerTask = controller.Rapid.GetTask(rsTask.Name);
                string name = controllerTask.Name;                

                if(!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);
                
                filePath = filePath + @"\";
                int i = 1;
                
                while(Directory.Exists(filePath + "simulation-" + i)){ i++;}
                Directory.CreateDirectory(filePath + "simulation-" + i);                
                
                controllerTask.SaveProgramToFile(filePath + "simulation-" + i);                
                result = "true";
            }
            catch (ABB.Robotics.GeneralException)
            {
                result = "false";
            }
            catch (Exception)
            {
                result = "false";
            }
            return result;

        }

        public static string ResetStation(string filePath)
        {
            string result = "false";
            try
            {
                SaveRapid(filePath);
                result = "true";
            }

            catch (ABB.Robotics.GeneralException)
            {
                result = "false";
            }
            catch (Exception)
            {
                result = "false";
            }
            return result;
        }
    }
}

