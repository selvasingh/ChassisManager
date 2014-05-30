// Copyright ? Microsoft Open Technologies, Inc.
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
    using System.Diagnostics;
    using System.Collections.Generic;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;
    class ProcCmd
    {
        IpmiClientExtended ipmi;
        bool showDetail = false;
        public ProcCmd(IpmiClientExtended client, bool detail)
        {
            this.ipmi = client;
            this.showDetail = detail;
        }
        #region Ipmi Commands
        /// <summary>
        /// Queries BMC for the currently set boot device.
        /// </summary>
        /// <returns>Flags indicating the boot device.</returns>
        public void GetNextBoot()
        {
            try
            {
                NextBoot response = ipmi.GetNextBoot();
                if(response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Next Boot");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Next Boot", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// The helper for several boot type setting methods, as they
        /// essentially send the same sequence of messages.
        /// </summary>
        public void SetNextBootBios()
        {
            BootType bootType = BootType.ForceIntoBiosSetup;
            try
            {
                ipmi.SetNextBoot(bootType, true, false, 0, false);
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// The helper for several boot type setting methods, as they
        /// essentially send the same sequence of messages.
        /// </summary>
        public void SetNextBootPxe()
        {
            BootType bootType = BootType.ForcePxe;
            try
            {
                ipmi.SetNextBoot(bootType, true, false, 0, false);
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// The helper for several boot type setting methods, as they
        /// essentially send the same sequence of messages.
        /// </summary>
        public void SetNextBootNormal()
        {
            BootType bootType = BootType.ForceDefaultHdd;
            try
            {
                ipmi.SetNextBoot(bootType, true, false, 0, false);
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        #region Identify
       /// <summary>
        /// Physically identify the computer by using a light or sound.
        /// </summary>
        /// <param name="interval">Identify interval in seconds or 255 for indefinite.</param>
        public void Identify()
        {
            try
            {
                if (ipmi.Identify(10))
                {
                    Console.WriteLine("Command Passed: {0}", "Identify");
                }
                else
                {
                    Console.WriteLine("Command failed: {0}", "Identify");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Queries BMC for the GUID of the system.
        /// </summary>
        /// <returns>GUID reported by Baseboard Management Controller.</returns>
        public void GetSystemGuid()
        {
            try
            {
                DeviceGuid response = ipmi.GetSystemGuid();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Device Guid");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Device Guid", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        #endregion
        #region Sensor Reading
        /// <summary>
        ///  Get Sensor Data Repository. Returns SDR Info.
        /// </summary>
        public void GetSdr()
        {
            try
            {
                SdrCollection response = ipmi.GetSdr();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Sdr");
                    if (showDetail)
                    {
                        Console.WriteLine();
                        Console.WriteLine(" Enumerating Objects for: {0}", "System Event Log");
                        Console.WriteLine(" ===================================");
                        Console.WriteLine();
                        foreach (SensorMetadataBase sdr in response)
                        {
                            Console.WriteLine("Sdr Type            {0}", sdr.GetType().ToString());
                            Console.WriteLine("Sensor Number:      {0}", sdr.SensorNumber);
                            Console.WriteLine("Sensor Type#:       {0} Detail: {1}", SharedFunc.ByteToHexString(sdr.RawSensorType), sdr.SensorType);
                            Console.WriteLine("Sensor Description: {0}", sdr.Description);
                            Console.WriteLine();
                        }
                        Console.WriteLine(" ===================================");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Sdr", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        ///  Get Sensor Data Repository Information Incrementally. Returns SDR Info.
        /// </summary>
        public void GetSdrIncrement()
        {
            try
            {
                SdrCollection response = ipmi.GetSdrIncrement();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Sdr Increment");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Sdr Increment", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        ///  Get Sensor Data Record Information. Returns Sdr Info.
        /// </summary>
        public void GetSdrInfo()
        {
            try
            {
                SdrRepositoryInfo response = ipmi.GetSdrInfo();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Sdr Info");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Sdr Info", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Gets Sensor Reading
        /// </summary>
        public void GetSensorReading()
        {
            byte SensorNumber = ConfigLoad.SensorNo;
            byte SensorType = ConfigLoad.SensorType;
            try
            {
                SensorReading response = ipmi.GetSensorReading(SensorNumber, SensorType);
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Sensor Reading");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Sensor Reading", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        #endregion
        #region Power
        /// <summary>
        /// Set the computer power state.
        /// </summary>
        /// <param name="powerState">Power state to set.</param>
        public void SetPowerOff()
        {
            try
            {
                byte response = ipmi.SetPowerState(IpmiPowerState.Off);
                if (response == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Set Power Off");
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Set Power Off", response);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Set the computer power state.
        /// </summary>
        /// <param name="powerState">Power state to set.</param>
        public void SetPowerOn()
        {
            try
            {
                byte response = ipmi.SetPowerState(IpmiPowerState.On);
                if (response == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Set Power On");
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Set Power On", response);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Set the computer power state.
        /// </summary>
        /// <param name="powerState">Power state to set.</param>
        public void SetPowerReset()
        {
            try
            {
                byte response = ipmi.SetPowerState(IpmiPowerState.Reset);
                if (response == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Set Power Reset");
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Set Power Reset", response);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Sets the Power-On time
        /// </summary>
        /// <param name="interval">00 interval is none, other integers are interpretted as seconds.</param>
        public void SetPowerOnTime()
        {
            try
            {
                bool response = ipmi.SetPowerOnTime(8);
                if (response)
                {
                    Console.WriteLine("Command Passed: {0}", "Set Power On Time");
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Set Power On Time", response);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Get the current power state of the host computer.
        /// </summary>
        /// <returns>ImpiPowerState enumeration.</returns>
        /// <devdoc>
        /// Originally used the 'Get ACPI Power State' message to retrieve the power state but not supported
        /// by the Arima's Scorpio IPMI card with firmware 1.10.00610100.  The 'Get Chassis Status' message
        /// returns the correct information for all IPMI cards tested.
        /// </devdoc>
        public void GetPowerState()
        {
            try
            {
                 SystemStatus response = ipmi.GetChassisState();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Power State");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Power State", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Get the current power state of the host computer.
        /// </summary>
        /// <returns>ImpiPowerState enumeration.</returns>
        /// <devdoc>
        /// Originally used the 'Get ACPI Power State' message to retrieve the power state but not supported
        /// by the Arima's Scorpio IPMI card with firmware 1.10.00610100.  The 'Get Chassis Status' message
        /// returns the correct information for all IPMI cards tested.
        /// </devdoc>
        public void GetChassisState()
        {
            try
            {
                SystemStatus response = ipmi.GetChassisState();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Chassis State");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Chassis State", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Set Chassis Power Restore Policy.
        /// </summary>
        public void SetPowerRestorePolicyOff()
        {
            try
            {
                PowerRestorePolicy response = ipmi.SetPowerRestorePolicy(PowerRestoreOption.StayOff);
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Set Power Restore Policy Off");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Set Power Restore Policy Off", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Set Chassis Power Restore Policy.
        /// </summary>
        public void SetPowerRestorePolicyOn()
        {
            try
            {
                PowerRestorePolicy response = ipmi.SetPowerRestorePolicy(PowerRestoreOption.AlwaysPowerUp);
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Set Power Restore Policy On");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Set Power Restore Policy On", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Get the Power-On-Hours (POH) of the host computer.
        /// </summary>
        /// <returns>System Power On Hours.</returns>
        /// <remarks> Specification Note: Power-on hours shall accumulate whenever the system is in 
        /// the operational (S0) state. An implementation may elect to increment power-on hours in the S1 
        /// and S2 states as well.
        /// </remarks>
        public void PowerOnHours()
        {
            try
            {
                PowerOnHours response = ipmi.PowerOnHours();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Power On Hours");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Power On Hours", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        #endregion
        #region FRU
        /// <summary>
        /// Write Fru Data Command.  Note:
        ///     The command writes the specified byte or word to the FRU Inventory Info area. This is a ‘low level’ direct 
        ///     interface to a non-volatile storage area. The interface does not interpret or check any semantics or 
        ///     formatting for the data being written.  The offset used in this command is a ‘logical’ offset that may or may not 
        ///     correspond to the physical address. For example, FRU information could be kept in FLASH at physical address 1234h, 
        ///     however offset 0000h would still be used with this command to access the start of the FRU information.
        ///     
        ///     IPMI FRU device data (devices that are formatted per [FRU]) as well as processor and DIMM FRU data always starts 
        ///     from offset 0000h unless otherwise noted.
        /// </summary>
        public void WriteFruDevice(int deviceId, ushort offset, byte[] payload)
        {
            try
            {
                WriteFruDevice response = ipmi.WriteFruDevice(0, 2001, new byte[2]{0x01, 0x01});
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Write Fru Device");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Write Fru Device", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Reads raw fru data, and returns a byte array.
        /// </summary>
        public void ReadFruDevice(int deviceId, ushort offset, byte readCount)
        {
            try
            {
                byte[] response = ipmi.ReadFruDevice(0, 2001, 2);
                if (response.Length == 2)
                {
                    Console.WriteLine("Command Passed: {0}", "Read Fru Device");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} ", "Read Fru Device");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Get Fru Inventory Area
        /// </summary>
        public void GetFruInventoryArea()
        {
            try
            {
                FruInventoryArea response = ipmi.GetFruInventoryArea(0);
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Fru Inventory Area");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Fru Inventory Area", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Get Fru Device
        /// </summary>
        public void GetFruDeviceInfo()
        {
            try
            {
                FruDevice response = ipmi.GetFruDeviceInfo(0, true);
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Fru DeviceInfo");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Fru DeviceInfo", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        #endregion
        #region Firmware
        /// <summary>
        /// Gets BMC firmware revision.  Returns HEX string.
        /// </summary>
        /// <returns>firmware revision</returns>
        public void GetFirmware()
        {
            try
            {
                BmcFirmware response = ipmi.GetFirmware();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Firmware");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Firmware", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Gets Device Id.  Returns HEX string.
        /// </summary>
        /// <returns>firmware revision</returns>
        public void GetDeviceId()
        {
            try
            {
                BmcDeviceId response = ipmi.GetDeviceId();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get DeviceId");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get DeviceId", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        #endregion
        #region User
        /// <summary>
        /// Get Users. Returns dictionary of User Ids and corresponding User names
        /// </summary>
        public void GetUsers()
        {
            try
            {
               Dictionary<int, string> response = ipmi.GetUsers();
                if (response.Count > 0)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Users");
                }
                else
                {
                    Console.WriteLine("Command failed: {0}", "Get Users");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Set Password
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="operation">operation. setPassword, testPassword, disable\enable User</param>
        /// <param name="password">password to be set, 16 byte max for IPMI V1.5 and 20 byte max for V2.0</param>
        public void SetUserPassword()
        {
            try
            {
                // TODO Add Paramaters
                int userId = 1;
                string password = ConfigLoad.Password;
                IpmiAccountManagment operation = IpmiAccountManagment.SetPassword;
                if (ipmi.SetUserPassword(userId, operation, password))
                {
                    Console.WriteLine("Command Passed: {0}", "Set User Password");
                }
                else
                {
                    Console.WriteLine("Command failed: {0}", "Set User Password");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Set the User Name for a given User Id
        /// </summary>       
        public void SetUserName()
        {
            try
            {
                byte userId = (byte)ConfigLoad.UserId;
                string userName = ConfigLoad.SecondUser;
                if (ipmi.SetUserName(userId, userName))
                {
                    Console.WriteLine("Command Passed: {0}", "Set User Name");
                }
                else
                {
                    Console.WriteLine("Command failed: {0}", "Set User Name");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Get User Name
        /// </summary>
        /// <param name="userId">User Id.</param>
        public void GetUserName()
        {
            try
            {
                byte userId = (byte)ConfigLoad.UserId;
                UserName response = ipmi.GetUserName(userId);
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get User Name");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0}", "Get User Name");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Set User Access
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="userLmit">User Privilege Level.</param>
        /// <param name="allowBitMod">True|False, allow modification of bits in request byte</param>
        /// <param name="callBack">True|False, allow callbacks, usually set to False</param>
        /// <param name="linkAuth">True|False, allow link authoriation, usually set to True</param>
        /// <param name="ipmiMessage">allow Impi messaging, usually set to True</param>
        /// <param name="channel">channel used to communicate with BMC, 1-7</param>
        public void SetUserAccess()
        {
            try
            {
                int userId = ConfigLoad.UserId;
                PrivilegeLevel priv = PrivilegeLevel.Administrator;
                bool allowBitMod = true; 
                bool callback = false;
                bool linkAuth = true;
                bool ipmiMessage = false;
                int channel = 2;
                if (ipmi.SetUserAccess(userId, priv, allowBitMod, callback, linkAuth, ipmiMessage, channel))
                {
                    Console.WriteLine("Command Passed: {0}", "Set User Acces");
                }
                else
                {
                    Console.WriteLine("Command failed: {0}", "Set User Acces");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Get user privilege level
        /// </summary>
        public void GetUserPrivlige()
        {
            try
            {
                byte userId = (byte)ConfigLoad.UserId;
                byte channel = 2;
                UserPrivilege response = ipmi.GetUserPrivlige(userId, channel);
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get User Privlige");
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get User Privlige", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        #endregion
        #region System Event Log
        /// <summary>
        /// Reset SEL Log
        /// </summary>
        public void ClearSel()
        {
            try
            {
                if (ipmi.ClearSel())
                {
                    Console.WriteLine("Command Passed: {0}", "Clear Sel");
                }
                else
                {
                    Console.WriteLine("Command failed: {0}", "Clear Sel");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        ///  Get System Event Log Information. Returns SEL Info.
        /// </summary>
        public void GetSelInfo()
        {
            try
            {
                SystemEventLogInfo response = ipmi.GetSelInfo();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get System Event Log Info");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get System Event Log Info", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Recursively retrieves System Event Log entries.
        /// </summary>
        public void GetSel()
        {
            try
            {
                SystemEventLog response = ipmi.GetSel();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get System Event Log");
                    if (showDetail)
                    {
                        Console.WriteLine();
                        Console.WriteLine(" Enumerating Objects for: {0}", "System Event Log");
                        Console.WriteLine(" ===================================");
                        Console.WriteLine();
                        if (response.EventLog != null)
                        {
                            foreach (SystemEventLogMessage msg in response.EventLog)
                            {
                                Console.WriteLine("Message Type {0}", msg.GetType().ToString());
                                Console.WriteLine("Message Payload Data: {0}", SharedFunc.ByteArrayToHexString(msg.RawPayload));
                            }
                        }
                        else
                        {
                            Console.WriteLine(" Error: Getting Event Log Records ");
                            Console.WriteLine();
                        }
                        Console.WriteLine(" ===================================");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get System Event Log", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Gets the System Event Log Time
        /// </summary>
        public void GetSelTime()
        {
            try
            {
                GetEventLogTime response = ipmi.GetSelTime();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get System Event Log Time");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get System Event Log Time", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Set System Event Log Time
        /// </summary>
        public void SetSelTime()
        {
            try
            {
                if (ipmi.SetSelTime(DateTime.Now))
                {
                    Console.WriteLine("Command Passed: {0}", "Set Sel Time");
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Set Sel Time", "Unknown");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        #endregion
        #region Serial Modem
        /// <summary>
        /// Set Serial Mux Switch to System for Console Redirection.
        /// </summary>
        public void SetSerialMuxSwitch()
        {
            try
            {
                SerialMuxSwitch response = ipmi.SetSerialMuxSwitch();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Set Serial/Modem Mux Switch");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Set Serial/Modem Mux Switch", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Switches Serial control from System serial port to Bmc to close console redirection
        /// </summary>
        public void ResetSerialMux()
        {
            try
            {
                SerialMuxSwitch response = ipmi.ResetSerialMux();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Reset Serial/Modem Mux Switch");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Reset Serial/Modem Mux Switch", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Ipmi Set Serial/Modem Configuration
        /// </summary>
        public void GetSerialTimeout()
        {
            try
            {
                SerialConfig.SessionTimeout test = ipmi.GetSerialConfig<SerialConfig.SessionTimeout>(new SerialConfig.SessionTimeout());
                if (test.TimeOut == 0)
                {
                    Console.WriteLine("Command Passed: {0}", "GetSerialTimeout does not timeout");
                }
                else if(test.TimeOut > 0 )
                {
                    Console.WriteLine("Command Passed: {0} Timeout {1}", "GetSerialTimeout", test.TimeOut);
                }
                else if (test.TimeOut > 0)
                {
                    Console.WriteLine("Command Passed: {0}", "GetSerialTimeout does not timeout");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Ipmi Set Serial/Modem Configuration
        /// </summary>
        public void SetSerialTimeout()
        {
            try
            {
                if (ipmi.SetSerialConfig<SerialConfig.SessionTimeout>(new SerialConfig.SessionTimeout(0x06)))
                {
                    Console.WriteLine("Command Passed: {0}", "Set Serial Timeout");
                }
                else
                {
                    Console.WriteLine("Command failed: {0}", "Set Serial Timeout");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Ipmi Set Serial/Modem Configuration
        /// </summary>
        public void SetSerialTermination()
        {
            try
            {
                if (ipmi.SetSerialConfig<SerialConfig.SessionTermination>(new SerialConfig.SessionTermination(false, true)))
                {
                    Console.WriteLine("Command Passed: {0}", "Set Serial Termination");
                }
                else
                {
                    Console.WriteLine("Command failed: {0}", "Set Serial Termination");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Ipmi Set Serial/Modem Configuration
        /// </summary>
        public void GetSerialTermination()
        {
            try
            {
                SerialConfig.SessionTermination test = ipmi.GetSerialConfig<SerialConfig.SessionTermination>(new SerialConfig.SessionTermination());
                if (test.SessionTimeout)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Serial Termination does timeout");
                }
                else
                {
                    Console.WriteLine("Command Failed: {0}", "Get Serial Termination does not timeout");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Ipmi Get Channel Info command
        /// </summary>
        public void GetChannelInfo()
        {
            try
            {
                ChannelInfo response = ipmi.GetChannelInfo(0x0E);
                if (response.CompletionCode == 0x00)
                { 
                    Console.WriteLine("Command Passed: {0}", "Get Channel Info");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Channel Info", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        #endregion
        #region Session
        /// <summary>
        /// Ipmi Get Session Info Command.
        /// </summary>
        public void GetSessionInfo()
        {
            try
            {
                GetSessionInfoResponse response = ipmi.GetSessionInfo();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Session Info Response");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Session Info Response", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Negotiates the ipmi version and sets client accordingly. Also sets the authentication type for V1.5
        /// </summary>
        public void GetAuthenticationCapabilities()
        {
            try
            {
                ChannelAuthenticationCapabilities response = ipmi.GetAuthenticationCapabilities(PrivilegeLevel.Administrator);
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Channel Authentication Capabilities");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Channel Authentication Capabilities", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Send an IPMI Set Session Privilege Level request message and return the response.
        /// </summary>
        /// <param name="privilegeLevel">Privilege level for this session.</param>
        /// <returns>GetSessionChallengeResponse instance.</returns>
        public void SetSessionPrivilegeLevel()
        {
            try
            {
                ipmi.SetSessionPrivilegeLevel(PrivilegeLevel.Administrator);
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        #endregion
        #region JBOD
        /// <summary>
        /// Gets the Disk Status of JBODs
        /// </summary>
        public void GetDiskStatus()
        {
            try
            {
                DiskStatusInfo response = ipmi.GetDiskStatus();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "JBOD Get Disk Status");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "JBOD Get Disk Status", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Gets the Disk Status of JBODs
        /// </summary>
        public void GetDiskInfo()
        {
            try
            {
                DiskInformation response = ipmi.GetDiskInfo();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "JBOD Get Disk Info");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "JBOD Get Disk Info", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        #endregion
        #region OEM
        /// <summary>
        /// Gets Processor Information
        /// </summary>
        public void GetProcessorInfo()
        {
            try
            {
                ProcessorInfo response = ipmi.GetProcessorInfo((byte)ConfigLoad.ProcessorNo);
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Processor Info");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Processor Info", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Gets Memory Information
        /// </summary>
        public void GetMemoryInfo()
        {
            try
            {
                MemoryInfo response = ipmi.GetMemoryInfo((byte)ConfigLoad.MemoryNo);
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Memory Info");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Memory Info", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Gets PCIe Information
        /// </summary>
        public void GetPCIeInfo()
        {
            try
            {
                PCIeInfo response = ipmi.GetPCIeInfo((byte)ConfigLoad.PCIeNo);
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get PCIe Info");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get PCIe Info", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Gets Nic Information
        /// </summary>
        public void GetNicInfo()
        {
            try
            {
                NicInfo response = ipmi.GetNicInfo((byte)ConfigLoad.NicNo);
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get NicNo Info");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get NicNo Info", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        #endregion
        #endregion
        #region DCMI Commands
        /// <summary>
        /// DCMI Get Power Limit Command
        /// </summary>
        public void GetPowerLimit()
        {
            try
            {
                PowerLimit response = ipmi.GetPowerLimit();
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Get Power Limit");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Get Power Limit", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// DCMI Set Power Limit Command
        /// </summary>
        public void SetPowerLimit()
        {
            short watts = ConfigLoad.LimitWatts;
            int correctionTime = ConfigLoad.CorrectionTime; 
            byte action = ConfigLoad.Action;
            short samplingPeriod = ConfigLoad.SamplingPeriod;
            try
            {
                ActivePowerLimit response = ipmi.SetPowerLimit(watts, correctionTime, action, samplingPeriod);
                if (response.CompletionCode == 0x00)
                {
                    Console.WriteLine("Command Passed: {0}", "Set Power Limit");
                    if (showDetail)
                        EnumProp.EnumerableObject(response);
                }
                else
                {
                    Console.WriteLine("Command failed: {0} Completion Code: {1}", "Set Power Limit", response.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// DCMI Get Power Reading Command
        /// </summary>
        public void GetAdvancedPowerReading()
        {
            try
            {
                //TODO.  Add paramaters //short watts, int correctionTime, byte action, short samplingPeriod
                List<PowerReading> response = ipmi.GetAdvancedPowerReading();
                if (response.Count > 0)
                {
                    if (response[0].CompletionCode == 0x00)
                    {
                        Console.WriteLine("Command Passed: {0}", "Get Advanced Power Reading");
                        if (showDetail)
                            EnumProp.EnumerableObject(response[0]);
                    }
                    else
                    {
                        Console.WriteLine("Command failed: {0} ", "Get Advanced Power Reading");
                    }
                }
                else
                {
                    Console.WriteLine("Command failed: {0} ", "Get Advanced Power Reading");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        public void GetPowerReading()
        {
            try
            {
                
                List<PowerReading> response = ipmi.GetPowerReading();
                if (response.Count > 0)
                {
                    if (response[0].CompletionCode == 0x00)
                    {
                        Console.WriteLine("Command Passed: {0}", "Get Power Reading");
                        if (showDetail)
                            EnumProp.EnumerableObject(response[0]);
                    }
                    else
                    {
                        Console.WriteLine("Command failed: {0} ", "Get Power Reading");
                    }
                }
                else
                {
                    Console.WriteLine("Command failed: {0} ", "Get Power Reading");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        /// <summary>
        /// Activate/Deactivate DCMI power limit
        /// </summary>
        /// <param name="enable">Activate/Deactivate</param>
        public void ActivatePowerLimit()
        {
            try
            {
                if (ipmi.ActivatePowerLimit(true))
                {
                    Console.WriteLine("Command Passed: {0}", "Activate Power Limit");
                }
                else
                {
                    Console.WriteLine("Command failed: {0}", "Activate Power Limit");
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }
        private void WriteException(Exception ex)
        {
            Debug.WriteLine(string.Format("Command failed: {0}", ex.TargetSite.Name.ToString()));
            Tracer.WriteError(string.Format("Command failed: {0}", ex.TargetSite.Name.ToString()));
            Tracer.WriteError(ex.ToString());
        }
        #endregion
    }
}
