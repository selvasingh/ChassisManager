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
    using System.Diagnostics;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;
    class AnsiEscape : Vt100Base
    {
        private IpmiSerialClient ipmiClient;
        /// <summary>
        /// Local cache locker object
        /// </summary>
        private static object _locker = new object();
        /// <summary>
        /// Cursor position tracker
        /// </summary>
        private static int _posLeft = 0;
        /// <summary>
        /// Start position tracker, used in conjunction
        /// with cursor position tracker
        /// </summary>
        private static bool _start = false;
        /// <summary>
        /// Console payload, in string format.
        /// </summary>
        private static string scrData = string.Empty;
        ///// <summary>
        ///// Flag for Console Read while loop
        ///// </summary>
        //internal volatile bool threadRun = false;
        /// <summary>
        /// Initializes class and sets defaults.
        /// </summary>
        internal AnsiEscape(IpmiSerialClient sc)
        {
            // set console and buffer size
            base.SetConsoleSize(80, 25);
            base.SetConsoleBufferSize(80, 25);
            // Ipmi Client.
            this.ipmiClient = sc;
            // set cursor positions to begining
            PositionLeft = 0;
            PositionTop = 0;
            // set cursor position to default
            base.SetCursorPosition(PositionLeft, PositionTop);
            
            // Set the Console Code page to 437
            NativeMethods.SetCodePage();
            // pInvoke Call to Disable Console
            // wordwrap.  By default VT100 does not
            // expect wordwarp
            NativeMethods.DisableWordWrap(); // Disable Word Wrap
            Console.TreatControlCAsInput = true;
        }
        /// <summary>
        /// Designed to run continously reading for user
        /// input.  The method intercepts user input
        /// </summary>
        internal void ReadConsole()
        {
            //ConsoleKeyInfo keyInf;
            while (SharedFunc.SerialSerialSession)
            {
                // read key and intercept
                ConsoleKeyInfo keyInf = Console.ReadKey(true);
                if (!IsFunctionKey(keyInf))
                {
                    if (!_start)
                    { 
                        _posLeft = Console.CursorLeft;
                        _start = true;
                    }
                    scrData = scrData + keyInf.KeyChar;
                    Console.Write(keyInf.KeyChar);
                }
                else
                {
                    SendKeyData(keyInf);
                }
            }
        }
        /// <summary>
        /// Check ConcoleKeyInfo for VT100 Function Key
        /// </summary>
        private bool IsFunctionKey(ConsoleKeyInfo keyInfo)
        {
            if ((keyInfo.Key >= ConsoleKey.F1 // if key between F1 & F12
                && keyInfo.Key <= ConsoleKey.F12) ||
                (keyInfo.Key >= ConsoleKey.LeftArrow // if key is Arrow
                && keyInfo.Key <= ConsoleKey.DownArrow) ||
                (keyInfo.Key == ConsoleKey.Enter) || // if key is Enter
                (keyInfo.Key == ConsoleKey.Escape) || // if key is Escape
                (keyInfo.Key == ConsoleKey.C && 
                 keyInfo.Modifiers == ConsoleModifiers.Control) // Ctrl + C
                )
            {
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// Send encodeded payload
        /// </summary>
        private void SendKeyData(ConsoleKeyInfo keyInfo)
        {
            byte[] payload = Vt100Encode(keyInfo);
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                if (_start)
                {
                    Console.CursorLeft = _posLeft;
                    _start = false;
                }
            }
            ipmiClient.SerialWrite(payload);
        }
        /// <summary>
        /// Encode VT100 escape sequences into function key
        /// </summary>
        private byte[] Vt100Encode(ConsoleKeyInfo keyInfo)
        {
            byte[] enc = new byte[3];
            enc[0] = 0x1B; // Esc
            if (keyInfo.Key >= ConsoleKey.F1 // if key between F1 & F12
                && keyInfo.Key <= ConsoleKey.F12)
            {
                enc[1] = 0x4F; // O
                switch (keyInfo.Key)
                {
                    case ConsoleKey.F1:
                        enc[2] = 0x50; // P
                        break;
                    case ConsoleKey.F2:
                        enc[2] = 0x51; // Q
                        break;
                    case ConsoleKey.F3:
                        enc[2] = 0x52; // R
                        break;
                    case ConsoleKey.F4:
                        enc[2] = 0x53; // S
                        break;
                    case ConsoleKey.F5:
                        enc = new byte[5] { 0x1B, 0x5B, 0x31, 0x37, 0x7E };
                        break;
                    case ConsoleKey.F6:
                        enc = new byte[5] { 0x1B, 0x5B, 0x31, 0x38, 0x7E };
                        break;
                    case ConsoleKey.F7:
                        enc = new byte[5] { 0x1B, 0x5B, 0x31, 0x39, 0x7E };
                        break;
                    case ConsoleKey.F8:
                        enc = new byte[5] { 0x1B, 0x5B, 0x32, 0x30, 0x7E };
                        break;
                    case ConsoleKey.F9:
                        enc = new byte[5] { 0x1B, 0x5B, 0x32, 0x31, 0x7E };
                        break;
                    case ConsoleKey.F10:
                        enc = new byte[5] { 0x1B, 0x5B, 0x32, 0x33, 0x7E };
                        break;
                    case ConsoleKey.F11:
                        enc = new byte[5] { 0x1B, 0x5B, 0x32, 0x34, 0x7E };
                        break;
                    case ConsoleKey.F12:
                        enc = new byte[5] { 0x1B, 0x5B, 0x32, 0x35, 0x7E };
                        break;
                    default:
                        break;
                }
            }
            else if (keyInfo.Key >= ConsoleKey.LeftArrow // if key is Arrow
                    && keyInfo.Key <= ConsoleKey.DownArrow)
            {
                enc[1] = 0x5B; // bracket
                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        enc[2] = 0x41; // P
                        break;
                    case ConsoleKey.DownArrow:
                        enc[2] = 0x42; // P
                        break;
                    case ConsoleKey.RightArrow:
                        enc[2] = 0x43; // P
                        break;
                    case ConsoleKey.LeftArrow:
                        enc[2] = 0x44; // P
                        break;
                    default:
                        break;
                }
            }
            else if (keyInfo.Key == ConsoleKey.Enter) // if key is Enter
            {
                enc = new byte[2] { 0x0D, 0x0A };
                if (scrData != string.Empty && scrData.Length > 0)
                {
                    // get screen data bytes
                    byte[] scrPayload = Encoding.UTF8.GetBytes(string.Format(scrData));
                    // flush screen data
                    scrData = string.Empty;
                    // create new serialized packet with screen bytes and return payload
                    enc = new byte[(scrPayload.Length + 2)];
                    Buffer.BlockCopy(scrPayload, 0, enc, 0, scrPayload.Length);
                    // Add return key
                    enc[scrPayload.Length] = 0x0D;
                    enc[(scrPayload.Length+1)] = 0x0A;
                }
            }
            else if (keyInfo.Key == ConsoleKey.Escape) // Escape
            {
                enc = new byte[1];
                enc[0] = 0x1B;
            }
            else if (keyInfo.Key == ConsoleKey.C &&
                 keyInfo.Modifiers == ConsoleModifiers.Control) // Ctrl + C
            {
                // Issue a graceful terminate.
                SharedFunc.SetSerialSession(false);
                Debug.WriteLine("Control C Pressed");
                // flush console screen
                Console.Clear();
            }
            return enc;
        }
    }
}
