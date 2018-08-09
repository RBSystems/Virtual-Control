using System;
using System.IO;
using Crestron.SimplSharp;                              // For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                           // For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;            // For Threading
using Crestron.SimplSharp.WebScripting;                 // For Programmatic REST API
using Crestron.SimplSharpPro.UI;


namespace TSR310_test_with_SmartObject
{
    public class Server_Handler : IHttpCwsHandler
    {
        private TSR310_w_SmartObject _cs;
        public Server_Handler(TSR310_w_SmartObject cs)
        {
            _cs = cs;
        }

        void IHttpCwsHandler.ProcessRequest(HttpCwsContext context)
        {
            try
            {
                // handle requests
                if (context.Request.RouteData != null)
                {
                    switch (context.Request.RouteData.Route.Name.ToUpper())
                    {
                        case "ROOMNAME":
                            context.Response.StatusCode = 200;
                            context.Response.Write(InitialParametersClass.RoomName, true);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("CWS Handler: {0}", e.Message);
            }

        }
    }

    public class TSR310_w_SmartObject : CrestronControlSystem
    {

        private string MyFolder { get { return Crestron.SimplSharp.CrestronIO.Directory.GetApplicationRootDirectory() + "/User/"; } }
        private HttpCwsServer myServer;

        private Tsr310 MyRemote;
        private SmartObject _MyList;

        public enum ESmartObjectIds
        {
            MyList = 1,
        }

        /// <summary>
        /// ControlSystem Constructor. Starting point for the SIMPL#Pro program.
        /// Use the constructor to:
        /// * Initialize the maximum number of threads (max = 400)
        /// * Register devices
        /// * Register event handlers
        /// * Add Console Commands
        ///
        /// Please be aware that the constructor needs to exit quickly; if it doesn't
        /// exit in time, the SIMPL#Pro program will exit.
        ///
        /// You cannot send / receive data in the constructor
        /// </summary>
        public TSR310_w_SmartObject()
            : base()
        {


            try
            {
                Thread.MaxNumberOfUserThreads = 20;



                //Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(ControlSystem_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(ControlSystem_ControllerProgramEventHandler);
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(ControlSystem_ControllerEthernetEventHandler);

                ErrorLog.Notice("*** : Constructor");
                ErrorLog.Notice("*** : Current Time is {0}", DateTime.Now);
                myServer = new HttpCwsServer("api");
                myServer.Routes.Add(new HttpCwsRoute("roomname") { Name = "ROOMNAME" });
                myServer.HttpRequestHandler = new Server_Handler(this);
                myServer.Register();
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }


        /// <summary>
        /// InitializeSystem - this method gets called after the constructor
        /// has finished.
        ///
        /// Use InitializeSystem to:
        /// * Start threads
        /// * Configure ports, such as serial and verisports
        /// * Start and initialize socket connections
        /// Send initial device configurations
        ///
        /// Please be aware that InitializeSystem needs to exit quickly also;
        /// if it doesn't exit in time, the SIMPL#Pro program will exit.
        /// </summary>
        public override void InitializeSystem()
        {
            try
            {
                ErrorLog.Notice("*** : InitializeSystem");
                using (FileStream myStream = File.Create(MyFolder + "test.txt"))
                {
                    byte[] whatToWrite = System.Text.Encoding.ASCII.GetBytes(String.Format("Program Running Name: {0}", InitialParametersClass.RoomName));
                    myStream.Write(whatToWrite, 0, whatToWrite.Length);
                    myStream.Flush();
                }

                MyRemote = new Tsr310(0x09, this);

                string sgdpath = string.Format("{0}tsr310.sgd", Crestron.SimplSharp.CrestronIO.Directory.GetApplicationRootDirectory() + "/App/");
                MyRemote.LoadSmartObjects(sgdpath);
                _MyList = MyRemote.SmartObjects[(uint)ESmartObjectIds.MyList];
                _MyList.SigChange += new SmartObjectSigChangeEventHandler(MyList_EventChange);

                MyRemote.Description = "TSR310 for testing";
                MyRemote.OnlineStatusChange += new OnlineStatusChangeEventHandler(MyRemote_OnlineStatusChange);
                if (MyRemote.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    ErrorLog.Error("Error in Registering MyRemote: {0}", MyRemote.RegistrationFailureReason);
                else
                    MyRemote.SigChange += new Crestron.SimplSharpPro.DeviceSupport.SigEventHandler(MyRemote_SigChangeHandler); //Will Run MyRemote_SigChangeHandler when anything happens at the panel 
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        void MyRemote_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            ErrorLog.Notice("TDS: Frame Online should be happening");
            if (args.DeviceOnLine)
                ErrorLog.Notice("TDS: {0} is online", currentDevice.Description);
            else
                ErrorLog.Notice("TDS: {0} is offline", currentDevice.Description);
        }

        void MyList_EventChange(GenericBase currentDevice, SmartObjectEventArgs args)
        {
            switch (args.Sig.Number)
            {
                case 1:
                    {
                        _MyList.BooleanInput[2].BoolValue = true;
                        _MyList.BooleanInput[4].BoolValue = false;
                        _MyList.BooleanInput[6].BoolValue = false;
                        break;
                    }
                case 3:
                    {
                        _MyList.BooleanInput[2].BoolValue = false;
                        _MyList.BooleanInput[4].BoolValue = true;
                        _MyList.BooleanInput[6].BoolValue = false;
                        break;
                    }
                case 5:
                    {
                        _MyList.BooleanInput[2].BoolValue = false;
                        _MyList.BooleanInput[4].BoolValue = false;
                        _MyList.BooleanInput[6].BoolValue = true;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }


        void MyRemote_SigChangeHandler(Crestron.SimplSharpPro.DeviceSupport.BasicTriList currentDevice, SigEventArgs args)
        {
            if (currentDevice == MyRemote)
            {
                switch (args.Sig.Type)
                {
                    case eSigType.Bool:
                        {
                            if (args.Sig.BoolValue)
                            {
                                switch (args.Sig.Number)
                                {
                                    case 32:
                                        {
                                            ErrorLog.Notice("TDS: button press 1 registered");
                                            MyRemote.BooleanInput[33].BoolValue = false;
                                            MyRemote.BooleanInput[32].BoolValue = true;
                                            break;
                                        }
                                    case 33:
                                        {
                                            ErrorLog.Notice("TDS: button press 2 registered");
                                            MyRemote.BooleanInput[32].BoolValue = false;
                                            MyRemote.BooleanInput[33].BoolValue = true;
                                            break;
                                        }
                                    default:
                                        break;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Event Handler for Ethernet events: Link Up and Link Down.
        /// Use these events to close / re-open sockets, etc.
        /// </summary>
        /// <param name="ethernetEventArgs">This parameter holds the values
        /// such as whether it's a Link Up or Link Down event. It will also indicate
        /// wich Ethernet adapter this event belongs to.
        /// </param>
        void ControlSystem_ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {//Determine the event type Link Up or Link Down
                case (eEthernetEventType.LinkDown):
                    //Next need to determine which adapter the event is for.
                    //LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                        //
                    }
                    break;
                case (eEthernetEventType.LinkUp):
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {

                    }
                    break;
            }
        }

        /// <summary>
        /// Event Handler for Programmatic events: Stop, Pause, Resume.
        /// Use this event to clean up when a program is stopping, pausing, and resuming.
        /// This event only applies to this SIMPL#Pro program, it doesn't receive events
        /// for other programs stopping
        /// </summary>
        /// <param name="programStatusEventType"></param>
        void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Paused):
                    //The program has been paused.  Pause all user threads/timers as needed.
                    ErrorLog.Notice("*** : Pause");
                    break;
                case (eProgramStatusEventType.Resumed):
                    //The program has been resumed. Resume all the user threads/timers as needed.
                    ErrorLog.Notice("*** : Resumed");
                    break;
                case (eProgramStatusEventType.Stopping):
                    //The program has been stopped.
                    //Close all threads.
                    //Shutdown all Client/Servers in the system.
                    //General cleanup.
                    //Unsubscribe to all System Monitor events
                    ErrorLog.Notice("*** : Stopping");
                    break;
            }

        }

        /// <summary>
        /// Event Handler for system events, Disk Inserted/Ejected, and Reboot
        /// Use this event to clean up when someone types in reboot, or when your SD /USB
        /// removable media is ejected / re-inserted.
        /// </summary>
        /// <param name="systemEventType"></param>
        void ControlSystem_ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case (eSystemEventType.DiskInserted):
                    //Removable media was detected on the system
                    ErrorLog.Notice("*** : Disk Inserted");
                    break;
                case (eSystemEventType.DiskRemoved):
                    //Removable media was detached from the system
                    ErrorLog.Notice("*** : Disk Removed");
                    break;
                case (eSystemEventType.Rebooting):
                    //The system is rebooting.
                    //Very limited time to preform clean up and save any settings to disk.
                    ErrorLog.Notice("*** : Rebooting");
                    break;
            }

        }
    }
}
