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
using System.Text.RegularExpressions;
using System.Collections;
using System.Reflection;

namespace RST.Framework
{
    class SyncServer
    {
        // Incoming data from the client.
        public static string data = null;

        public static void StartListening()
        {
            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.
            // Dns.GetHostName returns the name of the 
            // host running the application.

            String hostname = Dns.GetHostName();

            IPHostEntry host = Dns.GetHostEntry(hostname);

            String localIP = GetIpAddress(host);

            Logger.AddMessage(new LogMessage(localIP, "MyKey"));

            IPAddress ipAddress = IPAddress.Parse(localIP);

            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and 
            // listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening for connections.
                while (true)
                {
                    Logger.AddMessage(new LogMessage("waiting for conn...!", "MyKey"));
                    // Program is suspended while waiting for an incoming connection.
                    Socket handler = listener.Accept();
                    data = null;

                    // An incoming connection needs to be processed.
                    while (true)
                    {
                        bytes = new byte[1024];
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data.IndexOf("<EOF>") > -1)
                        {
                            ExecuteCommand(data);
                            break;
                        }
                    }

                    // Show the data on the console.
                    Console.WriteLine("Text received : {0}", data);

                    // Echo the data back to the client.
                    byte[] msg = Encoding.ASCII.GetBytes(data);

                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }
        
        private static void ExecuteCommand(string data)
        {
            String[] parameters;            

            Logger.AddMessage(new LogMessage(data, "MyKey"));

            data = data.Replace("<EOF>", "");

            parameters = data.Split(';');

            switch (parameters[0])
            {
                case "addrobothome":

                    FunctionCollection.MainThread.Send((object stat) =>
                    {
                        FunctionCollection.AddHome();
                    }, null);

                    break;

                case "addtarget":

                    double x = double.Parse(parameters[2]);
                    double y = double.Parse(parameters[3]);
                    double z = double.Parse(parameters[4]);

                    FunctionCollection.MainThread.Send((object stat) =>
                    {
                        FunctionCollection.AddTarget(parameters[1], x, y, z);
                    }, null);

                    break;

                case "addtomain":

                    //int startIndex = Int

                    FunctionCollection.MainThread.Send((object stat) =>
                    {
                        FunctionCollection.AddToMain();
                    }, null);

                    break;


                case "autoconfigurepath":

                    FunctionCollection.MainThread.Send((object stat) =>
                    {
                        FunctionCollection.AutoConfigurePath(parameters[1]);
                    }, null);

                    break;

                case "createpath":

                    FunctionCollection.MainThread.Send((object stat) =>
                    {
                        FunctionCollection.CreatePath(parameters[1]);
                    }, null);

                    break;

                case "createworkobj":

                    FunctionCollection.MainThread.Send((object stat) =>
                    {
                        FunctionCollection.CreateWorkobject(); // edit Remove?
                    }, null);

                    break;

                case "load":

                    FunctionCollection.MainThread.Send((object stat) =>
                    {
                        FunctionCollection.Load(parameters[1]); 
                    }, null);

                    break;

                case "loadmodule":

                    FunctionCollection.MainThread.Send((object stat) =>
                    {
                        FunctionCollection.Load(parameters[1]); // edit
                    }, null);

                    break;

                case "loadprogram":

                    FunctionCollection.MainThread.Send((object stat) =>
                    {
                        FunctionCollection.Load(parameters[1]); // edit
                    }, null);

                    break;

                case "loadstation":

                    FunctionCollection.MainThread.Send((object stat) =>
                    {
                        FunctionCollection.Load(parameters[1]); // edit
                    }, null);

                    break;

                case "synctostation":

                    FunctionCollection.MainThread.Send((object stat) =>
                    {
                        FunctionCollection.SyncToStation();
                    }, null);

                    break;

                case "synctovc":

                    FunctionCollection.MainThread.Send((object stat) =>
                    {
                        FunctionCollection.SyncToVC();
                    }, null);

                    break;

                case "runsimulation":

                    FunctionCollection.MainThread.Send((object stat) =>
                    {
                        FunctionCollection.RunSimulation();
                    }, null);

                    break;
            }
        }
        
        public static String GetIpAddress(IPHostEntry host)
        {
            // Get computer host name
            String hostname = Dns.GetHostName();

            // Local ip of the computer
            String localIP;

            foreach (IPAddress ip in host.AddressList)
            {
                // Get local IP from Adresslist
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();

                    return localIP;
                }
            }
            return "127.0.0.1";
        }

    }
}
