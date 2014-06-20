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
    using System.Threading;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Text;
    using Microsoft.GFS.WCS.ChassisManager;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;
    internal static class SimChassisManager
    {
        static private bool haveAllTestsPassed = true;
        const string strFail = "[FAIL] ";
        const string strPass = "[PASS] ";
        const byte success = 0x00;
        const byte failure = 0xff;
        const byte bladeId = 0x01;
        internal static void Initialize()
        {
            bool isSelfTest = true;
            int numRequesters = 1;
            bool bladeInitialized = false;
            CompletionCode completionCode = CommunicationDevice.Init();
            if (CompletionCodeChecker.Failed(completionCode) == true)
            {
                Console.WriteLine(strFail + "Failed to initialize CommunicationDevice");
                return;
            }
            Console.WriteLine(strPass + "CM CommunicationDevice.Init succeedded");
            if (isSelfTest == true)
            {
                numRequesters = 1;
            }
            else
            {
                numRequesters = 2;
            }
            Thread[] requesterThreads = new Thread[numRequesters];
            for (int i = 0; i < numRequesters; i++)
            {
                if (isSelfTest == true)
                {
                    requesterThreads[i] = new Thread(DoSelfTest);
                }
                requesterThreads[i].Start();
                Thread.Sleep(1000);
            }
            for (int i = 0; i < numRequesters; i++)
            {
                requesterThreads[i].Join();
            }
            if (haveAllTestsPassed == true)
            {
                Console.WriteLine(strPass + "All the Chassis Manager internal tests have passed");
                if (InitializeFacade())
                {
                    // blade was initialized
                    bladeInitialized = true;
                    Console.WriteLine(strPass + "All the Chassis Manager internal tests have passed");
                }
                else
                {
                    Console.WriteLine(strFail + "The WCS Blade in Slot 1 failed to properly initialize");
                }
            }
            else
            {
                Console.WriteLine(strFail + "Some of the Chassis Manager internal tests have failed");
            }
            if (bladeInitialized)
            {
                ExecuteAllBladeCommands();
                WcsBladeFacade.Release();
            }
            CommunicationDevice.Release();
        }
        private static void SendReceiveDevices(PriorityLevel priority, byte deviceType, ref byte[] request, int numDevices)
        {
            byte deviceId;
            byte[] response;
            for (byte i = 1; i <= numDevices; i++)
            {
                response = null;
                deviceId = i;
                CommunicationDevice.SendReceive(priority, deviceType, deviceId, request, out response);
                CheckAndPrintResponsePacket(deviceId, ref response);
            }
        }
        private static void CheckAndPrintResponsePacket(byte deviceId, ref byte[] response)
        {
            if (response == null)
            {
                Console.WriteLine(strFail + "response is null");
            }
            else
            {
                byte completionCode = response[0];
                if (CompletionCodeChecker.Succeeded((CompletionCode)completionCode) == true)
                {
                    Console.Write(strPass);
                }
                else
                {
                    haveAllTestsPassed = false;
                    Console.Write(strFail);
                }
                Console.Write("Response from Device {0}: ", deviceId);
                for (int i = 0; i < response.Length; i++)
                {
                    Console.Write("[{0:x}]", response[i]);
                }
                Console.WriteLine("");
            }
        }
        private static void DoSelfTest()
        {
            byte[] request = null;
            PriorityLevel priority = PriorityLevel.System;
            byte deviceType;
            const int numFansToTest = 6;
            const int numPsusToTest = 6;
            // Test fans
            // Set fan speed
            Console.WriteLine("# Testing SetFanSpeed");
            deviceType = (byte)DeviceType.Fan;
            request = new byte[4];
            request[0] = (byte)FunctionCode.SetFanSpeed;
            request[1] = 1;
            request[2] = 0;
            // PWM duty cycle
            request[3] = 100;
            SendReceiveDevices(priority, deviceType, ref request, 1);
            // Get fan speed
            Console.WriteLine("# Testing GetFanSpeed");
            deviceType = (byte)DeviceType.Fan;
            request = null;
            request = new byte[3];
            request[0] = (byte)FunctionCode.GetFanSpeed;
            SendReceiveDevices(priority, deviceType, ref request, numFansToTest);
            // Test PSUs
            //Console.WriteLine("# Testing PsuStatusWord");
            //deviceType = (byte)DeviceType.Psu;
            //request = null;
            //request = new byte[3];
            //request[0] = (byte)FunctionCode.PsuOperations;
            //SendReceiveDevices(priority, deviceType, ref request, numPsusToTest);
            //Console.WriteLine("# Testing PsuReadPout");
            //request = null;
            //request = new byte[3];
            //request[0] = (byte)FunctionCode.PsuReadPout;
            //SendReceiveDevices(priority, deviceType, ref request, numPsusToTest);
            //Console.WriteLine("# Testing PsuReadSerialNumber");
            //request = null;
            //request = new byte[3];
            //request[0] = (byte)FunctionCode.PsuReadSerialNumber;
            //SendReceiveDevices(priority, deviceType, ref request, numPsusToTest);
            // Test blade_enable
            Console.WriteLine("# Testing blade_enable");
            deviceType = (byte)DeviceType.Power;
            request = null;
            request = new byte[3];
            request[0] = (byte)FunctionCode.TurnOnServer;
            SendReceiveDevices(priority, deviceType, ref request, ConfigLoaded.Population);
            Console.WriteLine("# Testing blade_enable status");
            deviceType = (byte)DeviceType.Power;
            request = null;
            request = new byte[3];
            request[0] = (byte)FunctionCode.GetServerPowerStatus;
            SendReceiveDevices(priority, deviceType, ref request, ConfigLoaded.Population);
            //// Test servers
            //Console.WriteLine("# Testing IPMI BladeInitialize");
            //DateTime startTime = DateTime.Now;
            
            //DateTime stopTime = DateTime.Now;
            //TimeSpan executionTime = stopTime - startTime;
            //Console.WriteLine("Elapsed time for IPMI initialization: {0}", executionTime.TotalSeconds);
            // Test watch dog timer
            Console.WriteLine("# Testing WatchDogTimer");
            deviceType = (byte)DeviceType.WatchDogTimer;
            request = null;
            request = new byte[3];
            request[0] = (byte)FunctionCode.EnableWatchDogTimer;
            SendReceiveDevices(priority, deviceType, ref request, 1);
            Console.WriteLine("# Testing ResetWatchDogTimer");
            deviceType = (byte)DeviceType.WatchDogTimer;
            request = null;
            request = new byte[3];
            request[0] = (byte)FunctionCode.ResetWatchDogTimer;
            SendReceiveDevices(priority, deviceType, ref request, 1);
            Console.WriteLine("# Testing StatusLed/TurnOnLed");
            deviceType = (byte)DeviceType.StatusLed;
            request = null;
            request = new byte[3];
            request[0] = (byte)FunctionCode.TurnOnLed;
            SendReceiveDevices(priority, deviceType, ref request, 1);
            Console.WriteLine("# Testing StatusLed/GetLedStatus");
            deviceType = (byte)DeviceType.StatusLed;
            request = null;
            request = new byte[3];
            request[0] = (byte)FunctionCode.GetLedStatus;
            SendReceiveDevices(priority, deviceType, ref request, 1);
            Console.WriteLine("# Testing RearAttentionLed/TurnOnLed");
            deviceType = (byte)DeviceType.RearAttentionLed;
            request = null;
            request = new byte[3];
            request[0] = (byte)FunctionCode.TurnOnLed;
            SendReceiveDevices(priority, deviceType, ref request, 1);
            Console.WriteLine("# Testing StatusLed/GetLedStatus");
            deviceType = (byte)DeviceType.StatusLed;
            request = null;
            request = new byte[3];
            request[0] = (byte)FunctionCode.GetLedStatus;
            SendReceiveDevices(priority, deviceType, ref request, 1);
            Console.WriteLine("# Testing TurnOffPowerSwitch");
            deviceType = (byte)DeviceType.PowerSwitch;
            request = null;
            request = new byte[3];
            request[0] = (byte)FunctionCode.TurnOffPowerSwitch;
            SendReceiveDevices(priority, deviceType, ref request, ConfigLoaded.NumPowerSwitches);
            Console.WriteLine("# Testing GetPowerSwitchStatus");
            request = null;
            request = new byte[3];
            request[0] = (byte)FunctionCode.GetPowerSwitchStatus;
            SendReceiveDevices(priority, deviceType, ref request, ConfigLoaded.NumPowerSwitches);
            Thread.Sleep(3000);
            Console.WriteLine("# Testing TurnOnPowerSwitch");
            deviceType = (byte)DeviceType.PowerSwitch;
            request = null;
            request = new byte[3];
            request[0] = (byte)FunctionCode.TurnOnPowerSwitch;
            SendReceiveDevices(priority, deviceType, ref request, ConfigLoaded.NumPowerSwitches);
            Console.WriteLine("# Testing GetPowerSwitchStatus");
            request = null;
            request = new byte[3];
            request[0] = (byte)FunctionCode.GetPowerSwitchStatus;
            SendReceiveDevices(priority, deviceType, ref request, ConfigLoaded.NumPowerSwitches);
            Random rand = new Random();
            byte baseValue = (byte)(rand.Next() % 10);
            Console.WriteLine("# Testing WriteEeprom (baseValue: {0})", baseValue);
            deviceType = (byte)DeviceType.ChassisFruEeprom;
            const int offsetPlusLengthInBytes = 4;
            const int dataSizeLowByte = 63;
            const int dataSizeHighByte = 0;
            const int offsetLowByte = 3;
            const int offsetHighByte = 2;
            request = null;
            request = new byte[3 + offsetPlusLengthInBytes + dataSizeLowByte];
            request[0] = (byte)FunctionCode.WriteEeprom;
            request[1] = offsetPlusLengthInBytes + dataSizeLowByte;
            request[2] = 0;
            request[3] = offsetLowByte;
            request[4] = offsetHighByte;
            request[5] = dataSizeLowByte;
            request[6] = dataSizeHighByte;
            for (int i = 0; i < dataSizeLowByte; i++)
            {
                request[i + 7] = (byte)(i + baseValue);
            }
            SendReceiveDevices(priority, deviceType, ref request, 1);
            Console.WriteLine("# Testing ReadEeprom");
            request = null;
            request = new byte[3 + offsetPlusLengthInBytes];
            request[0] = (byte)FunctionCode.ReadEeprom;
            request[1] = offsetPlusLengthInBytes;
            request[2] = 0;
            request[3] = offsetLowByte;
            request[4] = offsetHighByte;
            request[5] = dataSizeLowByte;
            request[6] = dataSizeHighByte;
            SendReceiveDevices(priority, deviceType, ref request, 1);
        }
        private static bool InitializeFacade()
        {
            WcsBladeFacade.Initialize();
            return InitializeBlade();
        }
        private static bool InitializeBlade()
        {
            return WcsBladeFacade.InitializeClient(bladeId);
        }
        private static void ExecuteAllBladeCommands()
        {
            ResponseBase response = new CustomResponse(failure);
            // Get the Power Status of a given device
            response = WcsBladeFacade.GetPowerStatus(bladeId);
            ValidateResponse(response, "Get Power Status");
            // Get Sensor Reading
            response = WcsBladeFacade.GetSensorReading(bladeId, 0x01);
            ValidateResponse(response, "Get Sensor Reading");
            // Get Blade Information
            BladeStatusInfo bladeInfo = WcsBladeFacade.GetBladeInfo(bladeId);
            ValidateResponse(response, "Get Blade Info");
            // Get Fru Device info for Given Device Id
            FruDevice fru = WcsBladeFacade.GetFruDeviceInfo(bladeId);
            ValidateResponse(response, "Get Fru Device");
            // Queries BMC for the currently set boot device.
            response = WcsBladeFacade.GetNextBoot(bladeId);
            ValidateResponse(response, "Get Next Boot");
            // Set next boot device
            response = WcsBladeFacade.SetNextBoot(bladeId, BootType.ForceDefaultHdd, true, false);
            ValidateResponse(response, "Set Next Boot");
            // Physically identify the computer by using a light or sound.
            if (WcsBladeFacade.Identify(bladeId, 255))
            {
                ValidateResponse(new CustomResponse(success), "Set Identify: On");
            }
            else
            {
                ValidateResponse(new CustomResponse(failure), "Set Identify: On");
            }
            // Physically identify the computer by using a light or sound.
            if (WcsBladeFacade.Identify(bladeId, 0))
            {
                ValidateResponse(new CustomResponse(success), "Set Identify: Off");
            }
            else
            {
                ValidateResponse(new CustomResponse(failure), "Set Identify: Off");
            }
            // Set the Power Cycle interval.
            if (WcsBladeFacade.SetPowerCycleInterval(bladeId, 0x08))
            {
                ValidateResponse(new CustomResponse(success), "Set Power Cycle Interval: 8");
            }
            else
            {
                ValidateResponse(new CustomResponse(failure), "Set Power Cycle Interval: 8");
            }
            // Set the computer power state Off
            if (WcsBladeFacade.SetPowerState(bladeId, IpmiPowerState.Off) == 0x00)
            {
                ValidateResponse(new CustomResponse(success), "SetPowerState: Off");
            }
            else
            {
                ValidateResponse(new CustomResponse(failure), "SetPowerState: Off");
            }
            // Set the computer power state On
            if (WcsBladeFacade.SetPowerState(bladeId, IpmiPowerState.On) == 0x00)
            {
                ValidateResponse(new CustomResponse(success), "SetPowerState: On");
            }
            else
            {
                ValidateResponse(new CustomResponse(failure), "SetPowerState: On");
            }
            // Gets BMC firmware revision.  Returns HEX string.
            response = WcsBladeFacade.GetFirmware(bladeId);
            ValidateResponse(response, "Get Firmware");
            // Queries BMC for the GUID of the system.
            response = WcsBladeFacade.GetSystemGuid(bladeId);
            ValidateResponse(response, "Get System Guid");
            // Reset SEL Log
            WcsBladeFacade.ClearSel(bladeId);
            ValidateResponse(response, "Clear Sel");
            // Recursively retrieves System Event Log entries.
            response = WcsBladeFacade.GetSel(bladeId);
            ValidateResponse(response, "Get Sel");
            //  Get System Event Log Information. Returns SEL Info.
            response = WcsBladeFacade.GetSelInfo(bladeId);
            ValidateResponse(response, "Get Sel Info");
            // Sensor Data Record
            ConcurrentDictionary<byte, SensorMetadataBase> sdr = WcsBladeFacade.Sdr(bladeId);
            // Get Device Id Command
            response = WcsBladeFacade.GetDeviceId(bladeId);
            ValidateResponse(response, "Get DeviceId");
            // Get/Set Power Policy
            response = WcsBladeFacade.SetPowerRestorePolicy(bladeId, PowerRestoreOption.StayOff);
            ValidateResponse(response, "Set Power Restore Policy: Off");
            response = WcsBladeFacade.SetPowerRestorePolicy(bladeId, PowerRestoreOption.AlwaysPowerUp);
            ValidateResponse(response, "Set Power Restore Policy: Always On");
            // Switches Serial Port Access from BMC to System for Console Redirection
            response = WcsBladeFacade.SetSerialMuxSwitch(bladeId, MuxSwtich.ForceSystem);
            ValidateResponse(response, "Set Serial MuxSwitch");
            // Switches Serial port sharing from System to Bmc
            response = WcsBladeFacade.ResetSerialMux(bladeId);
            ValidateResponse(response, "Reset Serial Mux");
            // Get the current advanced state of the host computer.
            response = WcsBladeFacade.GetChassisState(bladeId);
            ValidateResponse(response, "Get Chassis State");
            // Get Processor Information
            response = WcsBladeFacade.GetProcessorInfo(bladeId, 0x01);
            ValidateResponse(response, "Get Processor Info");
            // Get Memory Information
            response = WcsBladeFacade.GetMemoryInfo(bladeId, 0x01);
            ValidateResponse(response, "Get Memory Info");
            // Get PCIe Info
            response = WcsBladeFacade.GetPCIeInfo(bladeId, 0x01);
            ValidateResponse(response, "Get PCIe Info");
            // Get Nic Info
            response = WcsBladeFacade.GetNicInfo(bladeId, 0x01);
            ValidateResponse(response, "Get Nic Info");
            // Get Hardware Info
            HardwareStatus hwStatus = WcsBladeFacade.GetHardwareInfo(bladeId, true, true,
                        true, true, true, true, true, true, true);
            if (hwStatus.CompletionCode == 0x00)
            { ValidateResponse(new CustomResponse(success), "Hardware Status"); }
            else
            { ValidateResponse(new CustomResponse(failure), "Hardware Status"); }
            // DCMI Get Power Limit Command
            response = WcsBladeFacade.GetPowerLimit(bladeId);
            ValidateResponse(response, "Get Power Limit");
            // DCMI Set Power Limit Command
            response = WcsBladeFacade.SetPowerLimit(bladeId, 220, 6000, 0x00, 0x00);
            ValidateResponse(response, "Set Power Limit");
            // DCMI Get Power Reading Command
            List<PowerReading> pwReadings = WcsBladeFacade.GetPowerReading(bladeId);
            if(pwReadings.Count > 0)
            {
                ValidateResponse(pwReadings[0], "Get Power Reading");
            }
            else
            {
                ValidateResponse(new CustomResponse(failure), "Get Power Reading");
            }
            // Activate/Deactivate DCMI power limit
            if (WcsBladeFacade.ActivatePowerLimit(bladeId, true))
            {
                ValidateResponse(new CustomResponse(success), "Activate Power Limit");
            }
            else
            {
                ValidateResponse(new CustomResponse(failure), "Activate Power Limit");
            }
            if (WcsBladeFacade.ActivatePowerLimit(bladeId, false))
            {
                ValidateResponse(new CustomResponse(success), "Activate Power Limit");
            }
            else
            {
                ValidateResponse(new CustomResponse(failure), "Activate Power Limit");
            }
        }
        private static void ValidateResponse(ResponseBase response, string command)
        {
            if (response.CompletionCode == 0x00)
                Console.WriteLine("{0} Command {1}", strPass, command);
            else
                Console.WriteLine("{0} Command {1}", strFail, command);
        }
        internal class CustomResponse : ResponseBase
        {
            internal CustomResponse(byte completionCode)
            { base.CompletionCode = completionCode; }
            internal override void SetParamaters(byte[] param)
            { }
        }
    }
}
