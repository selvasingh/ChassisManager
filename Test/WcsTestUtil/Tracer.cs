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
    using System.Linq;
    using System.Text;
    using System.Diagnostics;
    using System.IO;
    internal static class Tracer
    {
        static uint infoTracingVerbosityLevel = 1; // It can be 1 (low), 2 (medium) or 3 (high)
        /// <summary>
        /// Changed in the App config.  Signal for checking if tracing is enabled.  Application usage:
        ///     Tracer.Trace.WriteLineIf(traceEnabled.Enabled, "Content to trace");
        /// </summary>
        public static BooleanSwitch TraceEnabled = new BooleanSwitch("TraceEnabled", "On/Off signal for trace checking");
        /// <summary>
        /// Changed in the App config.  Signal level/depth of tracing.  Application usage:
        ///    Trace.WriteLineIf(TraceLevel.TraceError, "App Config: TraceLevel.TraceError");
        ///    Trace.WriteLineIf(TraceLevel.TraceWarning, "App Config: TraceLevel.TraceWarning");
        ///    Trace.WriteLineIf(TraceLevel.TraceInfo, "App Config: TraceLevel.TraceInfo");
        ///    Trace.WriteLineIf(TraceLevel.TraceVerbose, "App Config: TraceLevel.TraceVerbose");
        /// </summary>
        public static TraceSwitch LogLevel = new TraceSwitch("TraceLevel", "Trace severity level switch");
        //private static string _tracefileName = @"C:\WcsTestUtil.txt";
        internal static string tracefileName = ConfigLoad.ReportLogFilePath;
        private static FileStream _traceFile;
        // Constant strings 
        internal const string targetSiteEmpty = "Exception targetSite Is Empty ";
        internal const string stackTraceEmpty = "Exception Stack Trace Is Empty ";
        internal const string exceptionMessageEmpty = "Exception Message Is Empty ";
        /// <summary>
        /// Constructor for initialization
        /// </summary>
        static Tracer()
        {
            try
            {
                _traceFile = new System.IO.FileStream(tracefileName, System.IO.FileMode.Append);
                Debug.Listeners.Add(new TextWriterTraceListener(_traceFile));
                Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
                Trace.AutoFlush = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Trace Logging cannot be done. Exception: " + ex.ToString());
            }
        }
        /// <summary>
        /// System Trace Write Output if log level is enabled in the app config.
        /// </summary>
        public static void WriteError(string message, Object obj1 = null, Object obj2 = null, Object obj3 = null)
        {
            try
            {
                if (LogLevel.TraceError)
                    WriteTrace(String.Format(message, obj1, obj2, obj3), "Error");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Trace Logging cannot be done. Exception: " + ex.ToString());
            }
        }
        /// <summary>
        /// System Trace Write Output if log level is enabled in the app config.
        /// </summary>
        public static void WriteError(Exception exec)
        {
            try
            {
                if (LogLevel.TraceError)
                {
                    WriteTrace(exec.TargetSite != null ? exec.TargetSite.ToString() : targetSiteEmpty, "Error");
                    WriteTrace(exec.StackTrace != null ? exec.StackTrace : stackTraceEmpty, "Error");
                    WriteTrace(exec.Message != null ? exec.Message.ToString() : exceptionMessageEmpty, "Error");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Trace Logging cannot be done. Exception: " + ex.ToString());
            }
        }
        /// <summary>
        /// System Trace Write Output if log level is enabled in the app config.
        /// </summary>
        public static void WriteWarning(string message, Object obj1 = null, Object obj2 = null, Object obj3 = null)
        {
            try
            {
                if (LogLevel.TraceWarning)
                    WriteTrace(String.Format(message, obj1, obj2, obj3), "Warning");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Trace Logging cannot be done. Exception: " + ex.ToString());
            }
        }
        /// <summary>
        /// System Trace Write Output if log level is enabled in the app config.
        /// verbosityLevel can be low (1), medium (2) or high (3)
        /// TODO - create an nicli command to take infoTracingVerbosityLevel
        /// </summary>
        public static void WriteInfo(uint verbosityLevel, string message, Object obj1 = null, Object obj2 = null, Object obj3 = null)
        {
            try
            {
                if (verbosityLevel <= infoTracingVerbosityLevel)
                {
                    if (LogLevel.TraceInfo)
                        WriteTrace(String.Format(message, obj1, obj2, obj3), "Info");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Trace Logging cannot be done. Exception: " + ex.ToString());
            }
        }
        // TODO: Remove this.. make sure code do not break
        // TODO: Check if it works for writeinfo(string+string+obj..)
        public static void WriteInfo(string message, Object obj1 = null, Object obj2 = null, Object obj3 = null)
        {
            try
            {
                // Assume highest verbosity level since not specified.. and always log this content
                if (LogLevel.TraceInfo)
                    WriteTrace(String.Format(message, obj1, obj2, obj3), "Info");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Trace Logging cannot be done. Exception: " + ex.ToString());
            }
        }
        /// <summary>
        /// System Trace Write Output if log level is enabled in the app config.
        /// </summary>
        private static void WriteTrace(string message, string type)
        {
            try
            {
                Trace.WriteLine(string.Format("{0},{1},{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), type, message));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Trace Logging cannot be done. Exception: " + ex.ToString());
            }
        }
    }
}
