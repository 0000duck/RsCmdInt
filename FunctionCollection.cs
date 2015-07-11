using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using ABB.Robotics.Math;
using ABB.Robotics.RobotStudio;
using ABB.Robotics.RobotStudio.Environment;
using ABB.Robotics.RobotStudio.Stations;
using ABB.Robotics.RobotStudio.Stations.Forms;
using System.Collections.Generic;
using System.Collections;

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

        public static void Load(string stationFilepath)
        {
            string filepath = @"";
            Station station = Station.Load(stationFilepath, false);
            if (station != null)
            {
                Project.ActiveProject = station;
                GraphicControl gc = new GraphicControl();
                gc.RootObject = station;
                DocumentWindow docwindow = new DocumentWindow();
                docwindow.Control = gc;
                docwindow.Caption = System.IO.Path.GetFileName(filepath);
                UIEnvironment.Windows.Add(docwindow);
                string test = ABB.Robotics.RobotStudio.Controllers.ControllerType.StationVC.ToString();

                Logger.AddMessage(new LogMessage(test, "MyKey"));

                
            }
                
            RsTask task = station.ActiveTask;
            RsIrc5Controller rscontroller = (RsIrc5Controller)task.Parent;
            //while (rscontroller.SystemState.ToString() != "Started")
                //DelayTask();                                    
        }

        public static void LoadModuleFromFile(string moduleFilePath)
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
        }

        private static async Task DelayTask()
        {
            await Task.Delay(1000);
        }

        public static void CreateWorkobject()
        {
            try
            {
                //get the active station
                Station station = Project.ActiveProject as Station;                
                string moduleName = "myModule";

                //create Workobject
                RsWorkObject wobj = new RsWorkObject();
                wobj.Name = station.ActiveTask.GetValidRapidName("wobj", "_", 1);
                wobj.ModuleName = moduleName;
                station.ActiveTask.DataDeclarations.Add(wobj);
            }
            catch (Exception exception)
            {
                Logger.AddMessage(new LogMessage(exception.Message.ToString()));
            }
        }

        public static void AddTarget(string targetName, double x, double y, double z)
        {
            //Begin UndoStep
            Project.UndoContext.BeginUndoStep("CreateTarget");

            try
            {                
                // 
                x = x / 1000; 
                y = y / 1000;
                z = z / 1000;

                // Adding target
                ShowTarget(targetName, new Vector3(x, y, z));
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
        }

        private static void DeleteActiveTargets()
        {                        
            Station station = Project.ActiveProject as Station;

            try
            {
                foreach (RsTarget target in station.ActiveTask.Targets)
                {
                    station.ActiveTask.Targets.Remove(target);                    
                }
            }

            catch (Exception exception)
            {
                Logger.AddMessage(new LogMessage(exception.Message.ToString()));
            }
        }

        public static void AddHome()
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
        }

        public static void AddToMain()
        {
            Station station = Project.ActiveProject as Station;
            string ep = station.ActiveTask.EntryPoint;
            RsPathProcedure procMain;
            Logger.AddMessage(new LogMessage(ep, "MyKey"));
            
            
            try
            {
                //station.ActiveTask = "";

                foreach(RsPathProcedure proc in station.ActiveTask.PathProcedures)
                {
                    

                    Logger.AddMessage(new LogMessage(proc.ToString(), "MyKey"));
                    procMain = (RsPathProcedure)proc.Copy();

                }


                
            }

            catch (Exception exception)
            {
                Logger.AddMessage(new LogMessage(exception.Message.ToString()));
            }
        }

        public static void SyncToStation()
        {
            Station station = Project.ActiveProject as Station;            
            RsTask task = station.ActiveTask;

            try
            {
                RsIrc5Controller rsIrc5Controller = (RsIrc5Controller)task.Parent;
                //Get Controller object
                ABB.Robotics.Controllers.Controller controller =
                    new ABB.Robotics.Controllers.Controller(new Guid(rsIrc5Controller.SystemId.ToString()));

                //Request for Mastership from controller  
                //If granted then call SyncPathProcedure instance method of RsTask      

                using (ABB.Robotics.Controllers.Mastership m =
                    ABB.Robotics.Controllers.Mastership.Request(controller.Rapid))
                {
                    try
                    {
                        ArrayList messages = new ArrayList();

                        //Synchronization to Station 
                        task.SyncPathProcedure("module1" + "/" + "main",
                            SyncDirection.ToStation,
                            messages);
                    }
                    catch (Exception)
                    {

                    }
                }
                
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new LogMessage(ex.Message.ToString()));
            }
        }

        public static void RunSimulation()
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
        }

        public static void Logg()
        {
            LogMessage[] log;
            log = Logger.GetMessages("Simulation");
            int i = log.GetLength(0);
            for (int j = 0; j < i; j++ )
            {
                Logger.AddMessage(new LogMessage(log[j].Text.ToString() + " At: " +log[j].TimeStamp.ToString(), "MyKey"));
            }
     
        }

        public static void AutoConfigurePath(string pathName)
        {
            Station station = Project.ActiveProject as Station;
            RsPathProcedure path = new RsPathProcedure(pathName);

            try
            {
                path = station.ActiveTask.FindPathProcedureFromModuleScope(pathName, "module1");

                path.Synchronize = true;

                Logger.AddMessage(new LogMessage(path.Name, "MyKey"));
                
            }

            catch (Exception exception)
            {
                Logger.AddMessage(new LogMessage(exception.Message.ToString()));
            }
        }
        
        public static void SyncToVC()
        {
            Station station = Project.ActiveProject as Station;
            RsTask task = station.ActiveTask;

            try
            {
                foreach (RsPathProcedure path in station.ActiveTask.PathProcedures)
                {

                    path.Synchronize = true;
                    //Get reference to instance of RsIrc5Controller    

                    RsIrc5Controller rsIrc5Controller = (RsIrc5Controller)task.Parent;
                    //Get virtual controller instance from RsIrc5Controller instance
                    ABB.Robotics.Controllers.Controller controller =
                        new ABB.Robotics.Controllers.Controller(new Guid(rsIrc5Controller.SystemId.ToString()));


                    //Request for Mastership from controller  
                    //If granted then call SyncPathProcedure instance method of RsTask    

                    using (ABB.Robotics.Controllers.Mastership m =
                        ABB.Robotics.Controllers.Mastership.Request(controller.Rapid))
                    {
                        try
                        {
                            ArrayList messages = new ArrayList();
                            task.SyncPathProcedure(path.ModuleName + "/" + path.Name,
                                SyncDirection.ToController,
                                messages);

                            

                        }
                        catch (Exception)
                        {

                        }
                    }
                }
                //SyncToStation();
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new LogMessage(ex.Message.ToString()));
            }
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

        public static void CreatePath(string procedureName)
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
        }

        public static void createCollisionSet(string firstobjects, string secondobjects)
        {
            Station station = Project.ActiveProject as Station;

            CollisionSet cs = new CollisionSet();
            cs.Name = "CollisionSet";

            cs.NearMissDistance = 0.01;

            cs.Active = true;

            station.CollisionSets.Add(cs);

            
            GraphicComponent a,b;
            station.GraphicComponents.TryGetGraphicComponent(firstobjects, out a);
            station.GraphicComponents.TryGetGraphicComponent(secondobjects,out b);
            cs.FirstGroup.Add(a);
            cs.SecondGroup.Add(b);

            CollisionDetector.CheckCollisions(station);
            CollisionDetector.CheckCollisions(cs);
        }

    }
}

