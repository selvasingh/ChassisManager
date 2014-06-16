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
    using Config = System.Configuration.ConfigurationManager;
    static class ConfigLoad
    {
        /// <summary>
        /// BMC User Name
        /// </summary>
        internal static readonly string UserName;
        /// <summary>
        /// BMC Password
        /// </summary>
        internal static readonly string Password;
        /// <summary>
        /// Sensor Number
        /// </summary>
        internal static readonly byte SensorNo;
        /// <summary>
        /// Sensor Type
        /// </summary>
        internal static readonly byte SensorType;
        /// <summary>
        /// User Id
        /// </summary>
        internal static readonly int UserId;
        /// <summary>
        /// Test User for User Command tests
        /// </summary>
        internal static readonly string SecondUser;
        /// <summary>
        /// Processor Number
        /// </summary>
        internal static readonly int ProcessorNo;
        /// <summary>
        /// Memory Number
        /// </summary>
        internal static readonly int MemoryNo;
        /// <summary>
        /// PCIe Number
        /// </summary>
        internal static readonly int PCIeNo;
        /// <summary>
        /// Nic Number
        /// </summary>
        internal static readonly int NicNo;
        /// <summary>
        /// Power Limit Watts
        /// </summary>
        internal static readonly short LimitWatts;
        /// <summary>
        /// Power Limit Correction Time
        /// </summary>
        internal static readonly int CorrectionTime;
        /// <summary>
        /// Power Limit Action
        /// </summary>
        internal static readonly byte Action;
        /// <summary>
        /// Power Limit SamplingPeriod
        /// </summary>
        internal static readonly short SamplingPeriod;
        /// <summary>
        /// Power Limit SamplingPeriod
        /// </summary>
        internal static readonly string ReportLogFilePath = "C:\\WcsTestUtil.txt";
        internal static readonly int BaudRate = 115200;
        internal static readonly uint SerialTimeout = 100;
        /// <summary>
        /// Class Constructor.
        /// </summary>
        static ConfigLoad()
        {
            UserName = Config.AppSettings["BmcUserName"].ToString();
            UserName = UserName == string.Empty ? "admin" : UserName;
            Password = Config.AppSettings["BmcUserKey"].ToString();
            Password = Password == string.Empty ? "admin" : Password;
            SecondUser = Config.AppSettings["SecondUser"].ToString();
            SecondUser = SecondUser == string.Empty ? "TestUser" : SecondUser;
            if (SecondUser == UserName)
                SecondUser = (SecondUser + "1");
            int.TryParse(Config.AppSettings["SerialSpeed"], out BaudRate);
            SerialRate baud;
            if(Enum.TryParse<SerialRate>(BaudRate.ToString(), out baud))
            {
                switch (baud)
                {
                    case SerialRate.B9600:
                        BaudRate = 9600;
                        break;
                    case SerialRate.B19200:
                        BaudRate = 19200;
                        break;
                    case SerialRate.B38400:
                        BaudRate = 38400;
                        break;
                    case SerialRate.B57600:
                        BaudRate = 57600;
                        break;
                    case SerialRate.B115200:
                        BaudRate = 115200;
                        break;
                    default:
                        BaudRate = 115200;
                        break;
                }
            }
            BaudRate = BaudRate < 9600 ? 115200 : BaudRate;
            int.TryParse(Config.AppSettings["ProcessorNo"], out ProcessorNo);
            ProcessorNo = ProcessorNo <= 0 ? 1 : ProcessorNo;
            int.TryParse(Config.AppSettings["MemoryNo"], out MemoryNo);
            MemoryNo = MemoryNo <= 0 ? 1 : MemoryNo;
            int.TryParse(Config.AppSettings["PCIeNo"], out PCIeNo);
            PCIeNo = PCIeNo <= 0 ? 1 : PCIeNo;
            int.TryParse(Config.AppSettings["NicNo"], out NicNo);
            NicNo = NicNo < 0 ? 0 : NicNo;
            uint.TryParse(Config.AppSettings["SerialTimeout"], out SerialTimeout);
            SerialTimeout = SerialTimeout < 50 ? 100 : SerialTimeout;
            byte.TryParse(Config.AppSettings["SensorNo"], out SensorNo);
            SensorNo = SensorNo == (byte)0 ? (byte)1 : SensorNo;
            byte.TryParse(Config.AppSettings["SensorType"], out SensorType);
            SensorType = SensorType == (byte)0 ? (byte)1 : SensorType;
            int.TryParse(Config.AppSettings["UserId"], out UserId);
            UserId = UserId <= 0 ? 2 : UserId;
            if (UserId > 15)
                UserId = 15;
            short.TryParse(Config.AppSettings["LimitWatts"], out LimitWatts);
            LimitWatts = LimitWatts <= (short)0 ? (short)0 : LimitWatts;
            int.TryParse(Config.AppSettings["LimitCorrection"], out CorrectionTime);
            CorrectionTime = CorrectionTime <= 0 ? 0 : CorrectionTime;
            Action = 0x00;
            byte.TryParse(Config.AppSettings["LimitAction"], out Action);
            short.TryParse(Config.AppSettings["LimitPeriod"], out SamplingPeriod);
            SamplingPeriod = SamplingPeriod <= (short)0 ? (short)0 : SamplingPeriod;
            ReportLogFilePath = Config.AppSettings["ReportLogFilePath"].ToString();
            ReportLogFilePath = ReportLogFilePath == string.Empty ? "C:\\WcsTestUtil.txt" : ReportLogFilePath;
        }
        private enum SerialRate
        { 
             B9600 = 0,
             B19200 = 1,
             B38400 = 2,
             B57600 = 3,
             B115200 = 4,
        }
    }
}
