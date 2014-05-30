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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    static class SharedFunc
    {
        /// <summary>
        /// Global flat to Signals Serial Session is enabled
        /// </summary>
        private static volatile bool enableSerialSession = false;
        internal static bool SerialSerialSession
        {
            get { return enableSerialSession; }
            private set { enableSerialSession = value; }
        }
        internal static void SetSerialSession(bool enabled)
        {
            SerialSerialSession = enabled;
        }
        /// <summary>
        /// Byte to Hex string representation
        /// </summary>  
        internal static string ByteToHexString(byte Bytes)
        {
            StringBuilder Result = new StringBuilder();
            string HexAlphabet = "0123456789ABCDEF";
            Result.Append("0x");
            Result.Append(HexAlphabet[(int)(Bytes >> 4)]);
            Result.Append(HexAlphabet[(int)(Bytes & 0xF)]);
            return Result.ToString();
        }
        /// <summary>
        /// Byte Array to Hex string representation
        /// </summary>  
        internal static string ByteArrayToHexString(byte[] Bytes)
        {
            StringBuilder Result = new StringBuilder();
            string HexAlphabet = "0123456789ABCDEF";
            Result.Append("0x");
            foreach (byte B in Bytes)
            {
                Result.Append(HexAlphabet[(int)(B >> 4)]);
                Result.Append(HexAlphabet[(int)(B & 0xF)]);
            }
            return Result.ToString();
        }
        /// <summary>
        /// Compare two byte arrays. 
        /// </summary>
        internal static bool CompareByteArray(byte[] arrayA, byte[] arrayB)
        {
            bool bEqual = false;
            if (arrayA.Length == arrayB.Length)
            {
                int i = 0;
                while ((i < arrayA.Length) && (arrayA[i] == arrayB[i]))
                {
                    i += 1;
                }
                if (i == arrayA.Length)
                {
                    bEqual = true;
                }
            }
            return bEqual;
        }
    }
}
