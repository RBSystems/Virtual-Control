using System;
using System.IO;
using Crestron.SimplSharp;                              // For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                           // For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;            // For Threading
using Crestron.SimplSharpPro.Diagnostics;               // For System Monitor Access
using Crestron.SimplSharp.WebScripting;                 // For Programmatic REST API
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.DM.Cards;
using Crestron.SimplSharpPro.UI;


namespace TSW750_with_DMMD8x8
{
    public class Server_Handler : IHttpCwsHandler
    {

        private TSW750_w_DMMD8x8 _cs;
        public Server_Handler(TSW750_w_DMMD8x8 cs)
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

    public class TSW750_w_DMMD8x8 : CrestronControlSystem
    {

        private string myFolder { get { return Crestron.SimplSharp.CrestronIO.Directory.GetApplicationRootDirectory() + "/User/"; } }
        private HttpCwsServer myServer;

        private Tsw750 myPanel;
        private DmMd8x8 myFrame;

        DMFrameInit DMFrameMaker = new DMFrameInit();
        
        /* This has been moved to a separate class
        //card list
        private DmcC input1;
        private DmcDvi input2;
        private DmcHd input3;
        private DmcS input4;
        private DmcC input5;
        private Dmc4kHd input6;
        private DmcVga input7;
        private DmcStr input8;

        private Dmc4kCoHdSingle output1_2;
        private Dmc4kHdoSingle output3_4;
        private DmcStroSingle output5_6;
        private DmcCoHdSingle output7_8;
        */

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
        public TSW750_w_DMMD8x8()
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

                //TSW Panel
                myPanel = new Tsw750(0x20, this);

                myPanel.Description = "Touchpanel";
                myPanel.OnlineStatusChange += new OnlineStatusChangeEventHandler(Device_OnlineStatusChange);
                if (myPanel.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    ErrorLog.Error("TDS: Error in Registering myPanel: {0}", myPanel.RegistrationFailureReason);
                else
                    myPanel.SigChange += MyPanel_SigChange;

                //DM Frame

                myFrame = DMFrameMaker.DMFrame(0x19, this);
                               
                /*
                myFrame = new DmMd8x8(0x19, this);
                input1 = new DmcC(1, myFrame);
                input2 = new DmcDvi(2, myFrame);
                input3 = new DmcHd(3, myFrame);
                input4 = new DmcS(4, myFrame);
                input5 = new DmcC(5, myFrame);
                input6 = new Dmc4kHd(6, myFrame);
                input7 = new DmcVga(7, myFrame);
                input8 = new DmcStr(8, myFrame);
                output1_2 = new Dmc4kCoHdSingle(1, myFrame);
                output3_4 = new Dmc4kHdoSingle(2, myFrame);
                output5_6 = new DmcStroSingle(3, myFrame);
                output7_8 = new DmcCoHdSingle(4, myFrame);

                myFrame.VideoEnter.BoolValue = true;
                */

                myFrame.Description = "DigitalMedia Switcher";
                myFrame.OnlineStatusChange += new OnlineStatusChangeEventHandler(Device_OnlineStatusChange);
                if (myFrame.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    ErrorLog.Error("TDS: Error in Registering myFrame: {0}", myFrame.RegistrationFailureReason);
                else
                    myFrame.DMOutputChange += MyFrame_DMOutputChange;
            }
            catch (Exception e)
            {
                ErrorLog.Error("TDS: Error in the constructor: {0}", e.Message);
            }
        }

        private void MyFrame_DMOutputChange(Switch currentDevice, DMOutputEventArgs args)
        {
            if (currentDevice == myFrame)
            {
                string fb = myFrame.Outputs[args.Number].VideoOutFeedback.ToString();
                myPanel.StringInput[1].StringValue = "TDS: Input " + fb.Substring(fb.IndexOf('#'), fb.Length - fb.IndexOf('#')) + " has been routed to Output " + args.Number;
                /*

                switch (args.Number)
                {
                    case 1:
                        {
                            string fb = myFrame.Outputs[1].VideoOutFeedback.ToString();
                            myPanel.StringInput[1].StringValue = "TDS: Input " + fb.Substring(fb.IndexOf('#'), fb.Length - fb.IndexOf('#')) + " has been routed!";
                            break;
                        }
                    case 2:
                        {
                            string fb = myFrame.Outputs[1].VideoOutFeedback.ToString();
                            myPanel.StringInput[1].StringValue = "TDS: Input " + fb.Substring(fb.IndexOf('#'), fb.Length - fb.IndexOf('#')) + " has been routed!";
                            break;
                        }
                    case 3:
                        {
                            string fb = myFrame.Outputs[1].VideoOutFeedback.ToString();
                            myPanel.StringInput[1].StringValue = "TDS: Input " + fb.Substring(fb.IndexOf('#'), fb.Length - fb.IndexOf('#')) + " has been routed!";
                            break;
                        }
                    case 4:
                        {
                            string fb = myFrame.Outputs[1].VideoOutFeedback.ToString();
                            myPanel.StringInput[1].StringValue = "TDS: Input " + fb.Substring(fb.IndexOf('#'), fb.Length - fb.IndexOf('#')) + " has been routed!";
                            break;
                        }
                    case 5:
                        {
                            break;
                        }
                    case 6:
                        {
                            break;
                        }
                    case 7:
                        {
                            break;
                        }
                    case 8:
                        {
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }*/
            }
        }

        void Device_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            if (args.DeviceOnLine)
                CrestronConsole.PrintLine("TDS: {0} is online", currentDevice.Description);
            else
                CrestronConsole.PrintLine("TDS: {0} is offline", currentDevice.Description);
        }

        private void MyPanel_SigChange(Crestron.SimplSharpPro.DeviceSupport.BasicTriList currentDevice, SigEventArgs args)
        {
            if (currentDevice == myPanel)
            {
                switch (args.Sig.Type)
                {
                    case eSigType.Bool:
                        {
                            if (args.Sig.BoolValue)
                            {
                                switch (args.Sig.Number)
                                {
                                    case 10:
                                        {
                                            ErrorLog.Notice("TDS: input 1 pressed");
                                            myFrame.Outputs[1].VideoOut = myFrame.Inputs[1];
                                            break;
                                        }
                                    case 11:
                                        {
                                            ErrorLog.Notice("TDS: input 2 pressed");
                                            myFrame.Outputs[1].VideoOut = myFrame.Inputs[2];
                                            break;
                                        }
                                    case 12:
                                        {
                                            ErrorLog.Notice("TDS: input 3 pressed");
                                            myFrame.Outputs[3].VideoOut = myFrame.Inputs[1];
                                            break;
                                        }
                                    case 13:
                                        {
                                            ErrorLog.Notice("TDS: input 4 pressed");
                                            myFrame.Outputs[3].VideoOut = myFrame.Inputs[2];
                                            break;
                                        }
                                    default:
                                        break;

                                }
                            }
                            else { }

                        }
                        break;
                    case eSigType.UShort:
                        break;
                    default:
                        break;
                }
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
                using (FileStream myStream = File.Create(myFolder + "test.txt"))
                {
                    byte[] whatToWrite = System.Text.Encoding.ASCII.GetBytes(String.Format("TDS: Program Running Name: {0}", InitialParametersClass.RoomName));
                    myStream.Write(whatToWrite,0,whatToWrite.Length);
                    myStream.Flush();
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("TDS: Error in InitializeSystem: {0}", e.Message);
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
