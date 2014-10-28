// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at 
// http://www.apache.org/licenses/LICENSE-2.0 

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR
// CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
// See the Apache 2 License for the specific language governing permissions and limitations under the License. 

namespace WcsTestUtil
{
    using System;
    using System.Collections.Generic;
    using System.IO.Ports;
    using System.Linq;
    using System.Management;
    using System.Text.RegularExpressions;
    using System.Reflection;
    using System.Threading;
    using Microsoft.GFS.WCS.ChassisManager;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;
    class IpmiUtil
    {
        #region Private Variables
        /// <summary>
        /// Supported Command List
        /// </summary>
        static SortedDictionary<int, string> commands = new SortedDictionary<int, string>() 
        {
            {1, "GetNextBoot"},
            {2, "SetNextBootBios"},
            {3, "SetNextBootPxe"},
            {4, "SetNextBootNormal"},
            //{5, "GetChassisCapabilities"},  Currently not documented or required for WCS.
            {6, "Identify"},
            {7, "GetSystemGuid"},
            {8, "GetSdr"},
            {9, "GetSdrInfo"},
            {10, "GetSensorReading"},
            {11, "SetPowerOff"},
            {12, "SetPowerOn"},
            {13, "SetPowerReset"},
            {14, "SetPowerOnTime"},
            {15, "GetPowerState"},
            {16, "GetChassisState"},
            {17, "SetPowerRestorePolicyOn"},
            {18, "SetPowerRestorePolicyOff"},
            //{1, "WriteFruDevice"},
            {19, "GetFruInventoryArea"},
            {20, "GetFruDeviceInfo"},
            {21, "GetFirmware"},          
            {22, "GetDeviceId"},
            {23, "ClearSel"},
            {24, "GetSelInfo"},
            {25, "GetSel"},
            //{26, "SetSerialMuxSwitch"},
            //{27, "ResetSerialMux"},
            {28, "GetSerialTimeout"},
            {29, "SetSerialTimeout"},
            {30, "SetSerialTermination"},
            {31, "GetSerialTermination"},
            {32, "GetChannelInfo"},
            //{33, "GetSessionInfo"},
            {34, "GetAuthenticationCapabilities"},
         //   {35, "SetSessionPrivilegeLevel"},
            {36, "GetProcessorInfo"},
            {37, "GetMemoryInfo"},
            {38, "GetPCIeInfo"},
            {39, "GetNicInfo"},
            //{40, "GetSelTime"},
            //{41, "SetSelTime"},
            {42, "GetPowerLimit"},    
            {43, "SetPowerLimit"}, 
            {44, "ActivatePowerLimit"},
            {45, "GetPowerReading"}
        };
        /// <summary>
        /// Supported Command List for JBOD
        /// </summary>
        static SortedDictionary<int, string> JbodCommands = new SortedDictionary<int, string>() 
        {
            {1, "GetDiskStatus"},
            {2, "GetDiskInfo"},
            {3, "GetAuthenticationCapabilities"},
            {4, "GetSystemGuid"}
        };
        /// <summary>
        /// Commands not allowed for Inband vesion.
        /// </summary>
        static Dictionary<int, string> InbandForbidden = new Dictionary<int, string>() 
        { 
            {11, "SetPowerOff"},
            {12, "SetPowerOn"},
            {13, "SetPowerReset"}
        };
        /// <summary>
        /// Blade Type
        /// </summary>
        enum BladeType
        { 
            Blade = 0,
            JBOD = 1
        }
        /// <summary>
        /// Test Passes
        /// </summary>
        static int _passes;
        /// <summary>
        /// COM Port Number (default = 1) 
        /// </summary>
        static int port = 1;
        /// <summary>
        /// BMC Connection Type
        /// </summary>
        static int connectionType = 0;
        /// <summary>
        /// locker object
        /// </summary>
        static readonly object locker = new object();
        /// <summary>
        /// Test Passes
        /// </summary>
        static int passes
        {
            get
            {
                lock (locker)
                {
                    return _passes;
                }
            }
            set
            {
                lock (locker)
                {
                    _passes = (value > 100 ? 0 : value);
                }
            }
        }
        /// <summary>
        /// Commands
        /// </summary>
        static string cmd = string.Empty;
        /// <summary>
        /// Flag to signal outputting response
        /// message details.
        /// </summary>
        static bool showDetail = false;
        /// <summary>
        /// Flag to signal switch to serial console
        /// </summary>
        static bool serialConsole = false;
        /// <summary>
        /// Time (delay) between commands
        /// </summary>
        static int throttle = 0;
        /// <summary>
        /// Device Type
        /// </summary>
        static BladeType bladeType = BladeType.Blade;
        #endregion
        /// <summary>
        /// Applicaiton Entry Point
        /// </summary>
        static void Main(string[] args)
        {
            if (CheckSyntax(args))
            {
                if (connectionType == 1) // Wmi Inband
                {
                    Proceed();
                    WmiClient();
                }
                else if (connectionType == 2) // Serial Client
                {
                    Proceed();
                    SerialClient();
                }
                else if (connectionType == 3) // Chassis Manager
                {
                    Proceed();
                    ChassisMgrSim();
                }
                else
                {
                    Console.WriteLine("Unknown BMC connection type specified.");
                }
            }
            else
            {
                ShowSyntax();
            }
        }
        /// <summary>
        /// Wmi Client
        /// </summary>
        private static void WmiClient()
        {
            // indicates proceed to processing commands
            bool connected = false;
            // wmi scope for target server
            ManagementScope _scope = new ManagementScope("\\\\" + Environment.MachineName + "\\root\\wmi");
            try
            {
                IpmiWmiClient wmi = new IpmiWmiClient(_scope);
                connected = true;
                // WMI is throttled by default
                throttle = 200;
                if (connected)
                    ProcessCmd<IpmiWmiClient>(wmi);
                else
                {
                    Console.WriteLine("Error connecting to WMI Ipmi provider");
                    Tracer.WriteError("Error connecting to WMI Ipmi provider");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error connecting to WMI Ipmi provider: {0}", ex.ToString());
                Tracer.WriteError("Error connecting to WMI Ipmi provider: {0}", ex.ToString());
            }
        }
        /// <summary>
        /// Serial Client
        /// </summary>
        private static void SerialClient()
        {
            // indicates proceed to processing commands
            bool connected = false;
            IpmiSerialClient sc = new IpmiSerialClient();
            sc.ClientPort = string.Format("COM{0}", port.ToString());
            sc.ClientBaudRate = ConfigLoad.BaudRate;
            sc.ClientDataBits = 8;
            sc.ClientParity = Parity.None;
            sc.ClientStopBits = StopBits.One;
            // disable logon retry.
            sc.OverRideRetry = true;
            sc.Timeout = ConfigLoad.SerialTimeout;
            Console.WriteLine("Serial Connection: PORT {0} TIMEOUT {1}", sc.ClientPort, sc.Timeout);
            Tracer.WriteInfo("Serial Connection: PORT {0} TIMEOUT {1}", sc.ClientPort, sc.Timeout);
            try
            {
                sc.Connect();
                // if the above processed proceed.
                connected = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Connecting to Serial Port {0}", ex.ToString());
                Tracer.WriteError("Error Connecting to Serial Port {0}", ex.ToString());
            }
            if (connected)
            {
                // logon to the session
                sc.LogOn(ConfigLoad.UserName, ConfigLoad.Password);
                if (throttle > 0)
                    System.Threading.Thread.Sleep(throttle);
                if (serialConsole)
                {
                    GetSerialConsole(sc);
                }
                else
                {
                    ProcessCmd<IpmiSerialClient>(sc);
                }
                // attempt to close the session
                sc.Close();
            }
            else
            {
                Console.WriteLine("Error connecting to Serial Port Ipmi instance");
            }
        }
        /// <summary>
        /// Chassis Manager Client Test.
        /// </summary>
        private static void ChassisMgrSim()
        {
            SimChassisManager.Initialize();        
        }
        /// <summary>
        /// Uses reflection to construct ProcessCmd and execute methods in the
        /// commands list.
        /// </summary>
        private static void ProcessCmd<T>(T ipmiClient) where T : IpmiClientExtended
        {
            string bmcInterface = string.Empty;
            if (connectionType == 1)
            { bmcInterface = "Inband WMI"; }
            else
            { bmcInterface = "Out of Band Serial"; }
            // Swap Jbod for blade commands
            // if target type is Jbod.  Also remove reboot commands
            // if inteface is inband.
            SwapCmd();
            // Display list of commands to be processed.
            Console.WriteLine("Processing commands over {0} interface", bmcInterface);
            Console.WriteLine();
            Console.WriteLine("Commands:");
            foreach(KeyValuePair<int, string> rec in commands)
            {
                Console.WriteLine("           " + rec.Value);
            }
            Console.WriteLine();
            string commandName = string.Empty;
            string methodName = string.Empty;
            
            try
            {
                Type classType = typeof(ProcCmd);
                Type[] paramType = new Type[2];
                paramType[0] = typeof(IpmiClientExtended);
                paramType[1] = typeof(bool);
                // Get the public instance constructor that takes an IpmiClientBase parameter.
                ConstructorInfo classConstruct = classType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public, null,
                    CallingConventions.HasThis, paramType, null);
                object classInstance = classConstruct.Invoke(new object[2] { ipmiClient, showDetail });
                // Get the target method and invoke
                foreach(KeyValuePair<int, string> cmd in commands)
                {
                    Console.WriteLine(cmd.Value);
                    commandName = cmd.Value;
                    MethodInfo targetMethod = classType.GetMethod(cmd.Value);
                    methodName = targetMethod.Name.ToString();
                    object obj = targetMethod.Invoke(classInstance, null);
                    if (throttle > 0)
                        System.Threading.Thread.Sleep(throttle);
                    commandName = string.Empty;
                    methodName = string.Empty;
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error when processing command: {0} Method: {1} Error: {2}",
                                    commandName,
                                    methodName,
                                    ex.ToString());
                Tracer.WriteError(string.Format("Error when processing command: {0} Method: {1} Error: {2}",
                    commandName,
                    methodName,
                    ex.ToString()));
            }
        }
        /// <summary>
        /// Serial Console
        /// </summary>
        private static void GetSerialConsole(IpmiSerialClient sc)
        {
            SerialMuxSwitch mux = sc.SetSerialMuxSwitch();
            if (mux.CompletionCode == 0x00)
            {
                Console.Clear();
                Console.WriteLine();
                Console.WriteLine("Switching to Serial Console Mode");
                Thread.Sleep(2000);
                Console.Clear();
                AnsiEscape ansi = new AnsiEscape(sc);
                SharedFunc.SetSerialSession(true);
                Thread td = new Thread(ansi.ReadConsole);
                td.Start();
                while (SharedFunc.SerialSerialSession)
                {
                    ansi.SplitAnsiEscape(sc.SerialRead());
                    Thread.Sleep(150);
                }
            }
            else
            {
                Console.WriteLine("Ipmi error with Serial Mux Switch command. Completion Code: {0} ", SharedFunc.ByteToHexString(mux.CompletionCode));
                Tracer.WriteError("Ipmi error with Serial Mux Switch command. Completion Code: {0} ", SharedFunc.ByteToHexString(mux.CompletionCode));
            }
        }
        /// <summary>
        /// Serial IPMI Power Off
        /// </summary>
        private static bool PowerOff(IpmiSerialClient sc)
        {
            if (sc.SetPowerState(IpmiPowerState.Off) == 0x00)
            {
                Thread.Sleep(3000);
                SystemStatus status = sc.GetChassisState();
                if (status.CompletionCode == 0x00)
                {
                    Console.WriteLine("Power State: {0}", status.PowerState.ToString());
                    return true;
                }
                else
                {
                    Console.WriteLine("Power State: {0}", "Unknown");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Power Off: {0}", "Failed");
                return false;
            }
        }
        /// <summary>
        /// Serial IPMI Power ON
        /// </summary>
        private static bool PowerOn(IpmiSerialClient sc)
        {
            if (sc.SetPowerState(IpmiPowerState.On) == 0x00)
            {
                Thread.Sleep(3000);
                SystemStatus status = sc.GetChassisState();
                if (status.CompletionCode == 0x00)
                {
                    Console.WriteLine("Power State: {0}", status.PowerState.ToString());
                    return true;
                }
                else
                {
                    Console.WriteLine("Power State: {0}", "Unknown");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Power On: {0}", "Failed");
                return false;
            }
        }
        /// <summary>
        /// Swap Blade for JBOD commands, if the connection is Serial
        /// and the target device type is JBOD.
        /// </summary>
        private static void SwapCmd()
        {
            if (connectionType == 2 && bladeType == BladeType.JBOD)
            {
                // clear the existing blade command list
                commands.Clear();
                // add each Jbod command to the cleared command list
                foreach(KeyValuePair<int, string> cmd in JbodCommands)
                {
                    commands.Add(cmd.Key, cmd.Value);
                }
            }
            // remove reboot commands from inband connection.
            else if (connectionType == 1)
            {
                foreach (KeyValuePair<int, string> cmd in InbandForbidden)
                {
                    commands.Remove(cmd.Key);
                }           
            }
        }
        #region Syntax
        /// <summary>
        /// Checks the console arguments
        /// </summary>
        private static bool CheckSyntax(string[] args)
        {
            //  return false if no
            // arguments are supplied.
            if (args.Length == 0)
            {
                return false;
            }
            // return false if ? is contained
            // in the first argument.
            else if (args[0].Contains("?"))
            {
                return false;
            }
            // argument paramater string
            string param;
            // argument value string
            string value;
            // connection type
            string conn = string.Empty;
            // device type.  Blade / JBOD
            string deviceType = string.Empty;
            // input regex
            string regex = @"(?<=-|/)(?<arg>\w+):(?<value>[a-zA-Z0-9_-]+)";
            // number of passes
            int pass = 0;
            foreach (string arg in args)
            {
                // match regex pattern
                Match match = Regex.Match(arg, regex);
                // capture match success
                if (match.Success)
                {
                    // check the argument value is not nothing.
                    if (match.Groups["value"] != null)
                    {
                        // set the paramater
                        param = match.Groups["arg"].Value;
                        // set the argument value
                        value = match.Groups["value"].Value;
                        // switch upper case paramater
                        // and set variables
                        switch (param.ToUpper())
                        {
                            case "CONN":
                                conn = value.ToString().Replace(" ", "");
                                break;
                            case "CMD":
                                cmd = value.ToString().Replace(" ", "");
                                break;
                            case "COM":
                                if (Int32.TryParse(value, out port))
                                {
                                    if (port <= 0)
                                        port = 1;
                                };
                                break;
                            case "PASS":
                                if (Int32.TryParse(value, out pass))
                                {
                                    if (pass < 0)
                                        pass = 0;
                                    passes = pass;
                                };
                                break;
                            case "TRTL":
                                if (Int32.TryParse(value, out throttle))
                                {
                                    if (throttle < 0)
                                        throttle = 0;
                                };
                                break;
                            case "DTL":
                                bool.TryParse(value, out showDetail);
                                break;
                            case "TYPE":
                                deviceType = value.ToString().Replace(" ", "");
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            List<string> commandList = commands.Values.ToList<string>();
            if ((conn.ToUpper() == "IB" || conn.ToUpper() == "OOB") &&
                (cmd.ToUpper() == "A" || commandList.Contains(cmd, StringComparer.OrdinalIgnoreCase)))
            {
                if (conn.ToUpper() == "IB")
                    connectionType = 1;
                else
                    connectionType = 2;
                if (connectionType == 2 && cmd.ToUpper() == "C")
                {
                    commands.Clear();
                    serialConsole = true;
                }
                if (cmd.ToUpper() != "A" && cmd.ToUpper() != "C" && commandList.Contains(cmd, StringComparer.OrdinalIgnoreCase))
                {
                    int index = commandList.FindIndex(a => a == cmd);
                    string command = commandList[index];
                    commands.Clear();
                    commands.Add(1, command);
                }
                if (deviceType.ToUpper() == "JBOD")
                    bladeType = BladeType.JBOD;
                   
                if (port > 9)
                    return false;
                else
                    return true;
            }
            else if (conn.ToUpper() == "WCS")
            {
                connectionType = 3;
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Show Syntax
        /// </summary>
        private static void ShowSyntax()
        {
            Console.WriteLine();
            Console.WriteLine("There are 3 types of verification test available through this conformance utility:");
            Console.WriteLine(" 1.	Windows Server native IPMI driver (WMI).  This test must be performed");
            Console.WriteLine("     from within blade Windows Operating System environment.");
            Console.WriteLine(" 2.	Serial Port (OOB).  The test must be performed over a serial port connected");
            Console.WriteLine("     to the blade WCS compliant BMC.");
            Console.WriteLine(" 3.	WCS Chassis Manager (WCS).  The blade must be inserted into slot 1 of a WCS Chassis.");
            Console.WriteLine();
            Console.WriteLine("Syntax:");
            Console.WriteLine("=======");
            Console.WriteLine();
            Console.WriteLine("/Conn:   Connection Types:   IB = In Band");
            Console.WriteLine("                             OOB = Out of Band (Serial)");
            Console.WriteLine("                             WCS = Chassis Manager (WCS - PDB)");
            Console.WriteLine();
            Console.WriteLine("/Com:    Serial Port Number:   1-9. (Default = 1)");
            Console.WriteLine();
            Console.WriteLine("/Pass:   Passes: 0-100");
            Console.WriteLine();
            Console.WriteLine("/Trtl:   Trottle in millseconds.  This increases the interval");
            Console.WriteLine("         between commands being sent to the BMC.  The deault is");
            Console.WriteLine("         zero.  Any value other than zerio will not simulate WCS");
            Console.WriteLine("         Chassis Manager functionality");
            Console.WriteLine();
            Console.WriteLine("/Dtl:    True/False. Optional paramater to output command response values");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("/Type:   Identifies the target system as Blade or JBOD.  This command");
            Console.WriteLine("         is used in conjunction with /Conn:OOB. Acceptable values are:");
            Console.WriteLine("         JBOD / Blade.  Default if unspecified is blade.");
            Console.WriteLine();
            Console.WriteLine("/Cmd:    Blade commands:   A = All WCS Commands, C = Serial Console");
            foreach (KeyValuePair<int, string> command in commands)
            {
                Console.WriteLine("                             {0}", command.Value);
            }
            Console.WriteLine();
            Console.WriteLine("/Cmd:    JBOD commands:   A = All WCS Commands (used with /Type:JBOD");
            foreach (KeyValuePair<int, string> command in commands)
            {
                Console.WriteLine("                             {0}", command.Value);
            }
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("========");
            Console.WriteLine("WcsTestUtil.exe /Conn:IB /Cmd:A /Pass:1");
            Console.WriteLine();
            Console.WriteLine("WcsTestUtil.exe /Conn:OOB /Com:1 /Cmd:A /Pass:1");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("WcsTestUtil.exe /Conn:OOB /Com:1 /Type:JBOD /Cmd:A /Pass:1");
            Console.WriteLine();
        }
        private static void Proceed()
        {
            Console.WriteLine();
            Console.WriteLine("Initializing application");
            Console.WriteLine();
            try
            {
                if (Tracer.TraceEnabled.Enabled)
                {
                    Console.WriteLine("Log file path: {0}", Tracer.tracefileName.ToString());
                }
                else
                {
                    Console.WriteLine("Loging has been disabled in the config settings");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating log file: " + ex.ToString());
            }
            Console.WriteLine();
        }
        #endregion
    }
}
