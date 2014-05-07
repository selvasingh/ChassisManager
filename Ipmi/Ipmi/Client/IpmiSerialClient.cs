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

namespace Microsoft.GFS.WCS.ChassisManager.Ipmi
{
    using System;
    using System.IO.Ports;
    using System.Reflection;
    using System.Diagnostics;
    using System.Collections.Generic;

    /// <summary>
    /// Ipmi Serial Client [Mode Basic]
    /// </summary>
    internal sealed class IpmiSerialClient : IpmiClientExtended, IDisposable
    {

        # region Private Variables

        // Serial Ipmi Packet Start Byte
        private const byte startByte = 0xA0;
        // Serial Ipmi Packet Stop byte
        private const byte stopByte = 0xA5;
        // Serial Ipmi Packet data escape
        private const byte dataEscape = 0xAA;
        // Serial Ipmi Packet data escape
        private const byte dataHandShake = 0xA6;

        // prevents redundant dispose calls.
        private bool disposed = false;

        // Serial COM Port Name
        private string comPort;
        
        // Serial Baud Rate
        private int baudRate;
        
        // Serial Parity
        private Parity parity;
        
        // Serial Data Bits
        private int dataBits;

        // Serial Stop Bits
        private StopBits stopBits;

        // Serial Port
        private SerialPort serialPort;

        /// <summary>
        /// Represents the current IPMI Session Id.
        /// </summary>
        private uint ipmiSessionId;

        /// <summary>
        /// Overrides command retrys
        /// </summary>
        private bool allowRetryOverride;

        /// <summary>
        /// locker object for accessing global resources.
        /// </summary>
        private object reqSeqLock = new object();

        /// <summary>
        /// Locker object for modifying the client cache
        /// </summary>
        private object cacheLock = new object();

        /// <summary>
        /// Signals serial port reading is ok by
        /// Serial console data scooper method
        /// </summary>
        private volatile bool reader = false;

        /// <summary>
        /// Gets a unique ReqSeq for each Ipmi message
        /// </summary>
        private byte GetReqSeq()
        {
            lock (reqSeqLock)
            {
                return base.IpmiRqSeq++;
            }
        }

        /// <summary>
        /// Resets the ReqSeq to zero and return it.
        /// </summary>
        private void ResetReqSeq()
        {
            lock (reqSeqLock)
            {
                base.IpmiRqSeq = 1;
            }
        }

        /// <summary>
        /// Double byte charactors to replace ipmi escape charactors.
        /// See IPMI 2.0: 14.4.1 - Basic Mode Packet Framing
        /// See IPMI 2.0: 14.4.2 - Data Byte Escaping 
        /// </summary>
        private readonly List<EscapeCharactor> escChars = new List<EscapeCharactor>(5)
        {
            new EscapeCharactor(0xAA, new byte[2]{0xAA, 0xBA}),
            new EscapeCharactor(0xA0, new byte[2]{0xAA, 0xB0}),
            new EscapeCharactor(0xA5, new byte[2]{0xAA, 0xB5}),
            new EscapeCharactor(0xA6, new byte[2]{0xAA, 0xB6}),
            new EscapeCharactor(0x1B, new byte[2]{0xAA, 0x3B})
        };

        #endregion

        # region Internal Variables

        /// <summary>
        /// Gets and sets the current IPMI Session Id.
        /// </summary>
        /// <value>IPMI Session Id.</value>
        internal uint IpmiSessionId
        {
            get { lock (cacheLock) { return this.ipmiSessionId; } }
            set { lock (cacheLock) { this.ipmiSessionId = value; } }
        }

        /// <summary>
        ///  Serial COM Port Name
        /// </summary>
        internal string ClientPort
        {
            get {return this.comPort; }
            set {this.comPort = value;}
        }

        // Serial Baud Rate
        internal int ClientBaudRate
        {
            get {return this.baudRate; }
            set {this.baudRate = value;}
        }

        // Serial Parity
        internal Parity ClientParity
        {
            get {return this.parity; }
            set {this.parity = value;}
        }

        // Serial Data Bits
        internal int ClientDataBits
        {
            get {return this.dataBits; }
            set {this.dataBits = value;}
        }

        // Serial Stop Bits
        internal StopBits ClientStopBits
        {
            get {return this.stopBits; }
            set {this.stopBits = value;}
        }

        /// <summary>
        /// Override Ipmi Command Retrys.  Default = false
        /// false = Allows retrys if command has retry.
        /// true = Prevents all retrys.
        /// </summary>
        internal bool OverRideRetry
        {
            get { return this.allowRetryOverride; }
            set { this.allowRetryOverride = value; }
        }

        #endregion

        #region Ipmi Escape Framing

        /// <summary>
        /// Replace serial framing charactors on outbound payload with 
        /// substatute byte sequence: 
        ///         IPMI 2.0: 14.4.1 - Basic Mode Packet Framing
        ///         IPMI 2.0: 14.4.2 - Data Byte Escaping 
        /// </summary>
        internal byte[] ReplaceFrameChars(byte[] payload)
        {
            // initialize dictionary for tracking positions of frame charactors
            SortedDictionary<int, EscapeCharactor> instances = new SortedDictionary<int, EscapeCharactor>();

            // generate list for tracking positions
            List<int> positions = new List<int>();

            // array resize increase
            int len = 0;

            // array indexer
            int index = 0;

            // array offset
            int offset = 0;
            
            // array incrementer
            int increase = 0;

            // iterate the frame charactors
            foreach (EscapeCharactor esc in escChars)
            {
                // use IndexOf to detect a single occurance of the frame charactor
                // if a single instance is detected, search for more.
                if (IpmiSharedFunc.GetInstance(payload, esc.Frame) >= 0)
                {
                    // list all positions of the frame char
                    positions = GetFramePositions(payload, esc.Frame);

                    // for each position found, added it to the dictionary
                    // for tracking the bit.
                    foreach (int occurance in positions)
                    {
                        instances.Add(occurance, esc);    
                    }
                }
            }

            // if instances of frame charactors have been found
            // enter into the replacement method.
            if (instances.Count > 0)
            {
                len = (payload.Length + instances.Count);
                byte[] newPayload = new byte[len];
                {
                    // reset indexers
                    index = 0; offset = 0; increase = 0;
                    foreach (KeyValuePair<int, EscapeCharactor> esc in instances)
                    {
                        // copy in the original byte array, up to the first frame char
                        Buffer.BlockCopy(payload, index, newPayload, offset, (esc.Key - index));

                        // set offset + byte offset 
                        // every pass adds 1 byte to increase
                        offset = esc.Key + increase;
                        
                        // copy in the replacement escape charactor array.
                        Buffer.BlockCopy(esc.Value.Replace, 0, newPayload, offset, esc.Value.Replace.Length);

                        // add 1 byte to the offset, as byte 1 
                        // in esc.Value.replace always overwrites,
                        // payload[index]
                        increase++;

                        // offset + 2 byte offset
                        offset = (esc.Key + increase +1);

                        // add 1 to index, to index past itself.
                        index = (esc.Key +1);
                    }
                    // copy remaining bytes into the new array
                    Buffer.BlockCopy(payload, index, newPayload, offset, (payload.Length - index));
                }

                // copy the remaining payload bytes.
                payload = newPayload;
            }

            return payload;
        }

        /// <summary>
        /// Replace serial escape charactors on received payload with 
        /// substatute byte sequence: 
        ///         IPMI 2.0: 14.4.1 - Basic Mode Packet Framing
        ///         IPMI 2.0: 14.4.2 - Data Byte Escaping 
        /// </summary>
        internal byte[] ReplaceEscapeChars(byte[] payload)
        {
            // initialize dictionary for tracking positions of escape charactors
            SortedDictionary<int, EscapeCharactor> instances = new SortedDictionary<int, EscapeCharactor>();

            // generate list for tracking positions
            List<int> positions = new List<int>();

            // array resize increase
            int len = 0;

            // array indexer
            int index = 0;

            // array offset
            int offset = 0;

            // iterate the escape charactors
            foreach (EscapeCharactor esc in escChars)
            {
                // use IndexOf to detect a single occurance of the escape charactor
                // if a single instance is detected, search for more.
                if (IpmiSharedFunc.GetInstance(payload, esc.Replace) >= 0)
                {
                    // list all positions of the escape char
                    positions = GetEscapePositions(payload, esc.Replace);

                    // for each position found, added it to the dictionary
                    // for tracking the bit.
                    foreach (int occurance in positions)
                    {
                        instances.Add(occurance, esc);
                    }
                }
            }

            // if instances of escape charactors have been found
            // enter into the replacement method.
            if (instances.Count > 0)
            {
                // lenght is payload minus the count of two byte escape sequences.
                len = (payload.Length - instances.Count);
                byte[] newPayload = new byte[len];
                {
                    // reset indexers
                    index = 0; offset = 0;
                    foreach (KeyValuePair<int, EscapeCharactor> esc in instances)
                    {
                        // copy in the original byte array, up to the first escape char
                        Buffer.BlockCopy(payload, index, newPayload, offset, (esc.Key - index));

                        // increment offset the size of bytes copied
                        offset += (esc.Key - index);

                        // increase the index based the 2 byte escape sequence
                        index = (esc.Key + 2);
                        
                        // replace escape charactors with frame charactor
                        newPayload[offset] = esc.Value.Frame;

                        // increase the offset for this new byte
                        offset++;
                    }

                    // copy remaining bytes into the new array
                    Buffer.BlockCopy(payload, index, newPayload, offset, (payload.Length - index));
                }

                // copy the remaining payload bytes.
                payload = newPayload;
            }

            return payload;
        }

        /// <summary>
        /// Detect escape charactors in payload
        /// </summary>
        /// <param name="payload">ipmi unframed payload</param>
        /// <param name="pattern">escape pattern</param>
        /// <returns>List of position integers</returns>
        private static List<int> GetEscapePositions(byte[] payload, byte[] pattern)
        {
            List<int> indexes = new List<int>();

            for (int i = 0; i < (payload.Length -1); i++)
            {
                if (pattern[0] == payload[i] && pattern[1] == payload[i+1])
                {
                    indexes.Add(i);
                }
            }
            return indexes;
        }

        /// <summary>
        /// Detect escape charactors in payload
        /// </summary>
        /// <param name="payload">ipmi unframed payload</param>
        /// <param name="pattern">escape pattern</param>
        /// <returns>List of position integers</returns>
        private static List<int> GetFramePositions(byte[] payload, byte pattern)
        {
            List<int> indexes = new List<int>();

            for (int i = 0; i < payload.Length; i++)
            {
                if (payload[i] == pattern)
                {
                   indexes.Add(i);
                }
            }

            return indexes;
        }

        internal void SerialWrite(byte[] payload)
        {
            if (this.serialPort != null)
            {
                try
                {
                    if (this.serialPort.IsOpen)
                    {
                        serialPort.Write(payload, 0, payload.Length);
                    }
                    else
                    {
                        Debug.WriteLine("Error: Data Write Serial Port Closed");
                    }
                }
                catch (Exception ex)
                {

                    Debug.WriteLine("Data Write exception occured when reading serial console data: " + ex.Message.ToString());
                }
                
            }
            else
            {
                Debug.WriteLine("Error: Data Write Serial Port Null");
            }
        }

        internal bool Reader
        { 
            get { return this.reader; }
            set { this.reader = value; }
        }

        internal byte[] SerialRead()
        {
            byte[] nothing = new byte[0];
            if (this.serialPort != null)
            {
                try
                {
                    byte[] buffer = new byte[512];
                    int readBytes;

                    while (Reader)
                    {
                        if (serialPort.BytesToRead > 0)
                        {

                            readBytes = serialPort.Read(buffer, 0, buffer.Length);

                            byte[] payload = new byte[readBytes];
                            Buffer.BlockCopy(buffer, 0, payload, 0, readBytes);

                            //   _escape.SplitAnsiEscape(payload);
                            return payload;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Data Receive exception occured when reading serial console data: " + ex.Message.ToString());
                }

            }
            else
            {
                Debug.WriteLine("Serial Console is not opened");
            }

            return nothing;
        }

        /// <summary>
        /// Add Start & Stop Serial Framing Charactors.
        /// </summary>
        internal static void AddStartStopFrame(ref byte[] payload)
        {
            payload[0] = startByte;
            payload[(payload.Length -1)] = stopByte;
        }

        #endregion

        #region Close, LogOff & Dispose

        /// <summary>
        /// Closes the connection to the BMC device. This is the preferred method of closing any open 
        /// connection.
        /// </summary>
        internal void Close()
        {
            this.Close(false);
        }

        /// <summary>
        /// Closes the connection to the BMC device. This is the preferred method of closing any open 
        /// connection.
        /// </summary>
        /// <param name="hardClose">
        /// true to close the socket without closing the IPMI session; otherwise false.
        /// </param>
        internal void Close(bool hardClose)
        {
            if (hardClose == false)
            {
                this.LogOff();
            }

            this.SetClientState(IpmiClientState.Disconnected);
        }

        /// <summary>
        /// Releases all resources held by this IpmiClient instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~IpmiSerialClient()
        {
            this.Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Close(false);
                }

                if (this.serialPort != null)
                {
                    this.serialPort.Close();
                    this.serialPort = null;
                }

                // new shared cleanup logic
                disposed = true;
            }
        }


        /// <summary>
        /// End an authenticated session with the BMC.
        /// </summary>
        internal void LogOff()
        {
            if (this.IpmiSessionId != 0)
            {
                this.IpmiSendReceive(
                    new CloseSessionRequest(this.IpmiSessionId),
                    typeof(CloseSessionResponse));
                this.IpmiSessionId = 0;
            }
        }

        #endregion

        #region Connect & Logon

        /// <summary>
        /// Connect to bmc serial port using default connection
        /// paramaters:
        ///     BaudRate = 115200
        ///     Parity = None
        ///     Data Bits = 8
        ///     Stop Bits = 1
        /// </summary>
        internal void Connect(string comPort)
        {
            this.ClientPort = comPort;
            this.ClientBaudRate = 115200;
            this.ClientDataBits = 8;
            this.ClientParity = Parity.None;
            this.ClientStopBits = StopBits.One;
            this.Connect();
        }

        /// <summary>
        /// Connect to bmc serial port using specifying connection
        /// paramaters.
        /// </summary>
        internal void Connect(string comPort, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            this.ClientPort = comPort;
            this.ClientBaudRate = baudRate;
            this.ClientDataBits = dataBits;
            this.ClientParity = parity;
            this.ClientStopBits = stopBits;
            this.Connect();
        }

        /// <summary>
        /// Connects the client to the serial ipmi bmc on specified computer.
        /// </summary>
        /// <param name="hostName">Host computer to access via ipmi over serial.</param>
        internal void Connect()
        {
            if (this.ClientState != IpmiClientState.Disconnected)
            {
                throw new InvalidOperationException();
            }

            base.SetClientState(IpmiClientState.Connecting);

            // Set serial port configuration paramaters.
            this.serialPort = new SerialPort(this.comPort, this.baudRate, this.parity, this.dataBits, this.stopBits);
            // Rts required
            this.serialPort.RtsEnable = true;
            // Set the read/write timeouts
            this.serialPort.ReadTimeout = (int)base.Timeout;
            // Write timeout
            this.serialPort.WriteTimeout = 100;
            // Write timeout
            this.serialPort.ReadBufferSize = 1024;

            // set no handshake.
            this.serialPort.Handshake = Handshake.None;

            try
            {
                // attempt to open the serial port
                this.serialPort.Open();
            }
            catch (Exception ex)
            {                
                Debug.WriteLine("Unable to open port: {0}", ex.ToString());
                base.SetClientState(IpmiClientState.Disconnected);
                return;
            }

            base.SetClientState(IpmiClientState.Connected);

        }

        private void LogOn()
        {
            this.LogOn(base.IpmiUserId, base.IpmiPassword);
        }

        /// <summary>
        /// Start an authenticated session with the BMC.
        /// </summary>
        /// <param name="userid">Account userid to authenticate with.</param>
        /// <param name="password">Account password to authenticate with.</param>
        /// <remarks>Only supports administrator sessions.</remarks>
        internal void LogOn(string userId, string password)
        {
            // temp will remove with debugging
            byte[] IpmiChallengeStringData = {};

            // set the user id & password
            base.IpmiUserId = userId;
            base.IpmiPassword = password;

            // set the client maximum previlege level
            base.IpmiPrivilegeLevel = PrivilegeLevel.Administrator;

            // set client state to session challenge
            base.SetClientState(IpmiClientState.SessionChallenge);

            // set the proposed v1.5 authentication type to MD5 
            // MD5 = the highest mandatory level in v1.5 and v2.0
            base.IpmiProposedAuthenticationType = AuthenticationType.Straight;

            // initialize the ipmi 1.5 authentication type to zero (none)
            this.IpmiAuthenticationType = AuthenticationType.None;

            // session challenge
            GetSessionChallengeResponse response =
                (GetSessionChallengeResponse)this.IpmiSendReceive(
                    new GetSessionChallengeRequest(base.IpmiProposedAuthenticationType, base.IpmiUserId),
                    typeof(GetSessionChallengeResponse), false);

            // set challenge string
            IpmiChallengeStringData = response.ChallengeStringData;

            // set temporary session id
            this.IpmiSessionId = response.TemporarySessionId;

            // set client state to activate session
            base.SetClientState(IpmiClientState.ActivateSession);

            // switch the v1.5 authentication type to the negotiated authentication type.
            base.IpmiAuthenticationType = base.IpmiProposedAuthenticationType;

            // ipmi authentication code / user password logon.
            byte[] authCode = IpmiSharedFunc.AuthCodeSingleSession(this.IpmiSessionId, 
                                                                        IpmiChallengeStringData, 
                                                                        base.IpmiAuthenticationType, 
                                                                        base.IpmiPassword);

            // Session Activation.See: IPMI Table   22-21, Activate Session Command
            ActivateSessionResponse activateResponse =
                (ActivateSessionResponse)this.IpmiSendReceive(
                    new ActivateSessionRequest(this.IpmiAuthenticationType, base.IpmiPrivilegeLevel, authCode, 1),
                    typeof(ActivateSessionResponse), false);

            // set the session id for the remainder of the session
            this.IpmiSessionId = activateResponse.SessionId;

            // initialize the ipmi message sequence number to zero
            ResetReqSeq();

            // set client state to authenticated. client state
            // is used for socket and RMCP payload type control
            base.SetClientState(IpmiClientState.Authenticated);

            // set session privilege level
            base.SetSessionPrivilegeLevel(IpmiPrivilegeLevel);
 
        }

        #endregion

        #region Send/Receive

        /// <summary>
        /// Generics method IpmiSendReceive for easier use
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ipmiRequest"></param>
        /// <returns></returns>
        internal override T IpmiSendReceive<T>(IpmiRequest ipmiRequest)
        {
            return (T)this.IpmiSendReceive(ipmiRequest, typeof(T), false);
        }

        /// <summary>
        /// Send Receive Ipmi messages
        /// </summary>
        internal override IpmiResponse IpmiSendReceive(IpmiRequest ipmiRequest, Type responseType, bool allowRetry = true)
        {
            // Get the request sequence.  This should be incremented
            // for every request/response pair.
            byte reqSeq = GetReqSeq();

            // Serialize the IPMI request into bytes.
            byte[] ipmiRequestMessage = this.ReplaceFrameChars(ipmiRequest.GetBytes(IpmiTransport.Serial, reqSeq));

            // inject start/stop frame bytes.
            AddStartStopFrame(ref ipmiRequestMessage);

            Debug.WriteLine("Command: " + ipmiRequest.GetType().ToString() + " Seq: " + reqSeq);
            Debug.WriteLine("Sending: " + IpmiSharedFunc.ByteArrayToHexString(ipmiRequestMessage)  + 
                    " Time: " + DateTime.Now.ToString("HH:mm:ss.fff", 
                    System.Globalization.DateTimeFormatInfo.InvariantInfo));
            
            byte[] messageResponse = { };
            byte[] ipmiResponseMessage = { };
            byte completionCode;

            // Send the ipmi mssage over serial.
            this.SendReceive(ipmiRequestMessage, out messageResponse);
           
            // format the received message
            ProcessReceivedMessage(messageResponse, out ipmiResponseMessage, out completionCode);

            // messageResponse no longer needed.  data copied to ipmiResponseMessage by ProcessReceivedMessage().
            messageResponse = null;


            // Create the response based on the provided type.
            ConstructorInfo constructorInfo = responseType.GetConstructor(Type.EmptyTypes);
            IpmiResponse ipmiResponse = (IpmiResponse)constructorInfo.Invoke(new Object[0]);

            // check serial protocol completion code
            if (completionCode == 0x00)
            {
                // if serial protocol completion code is successful (0x00).
                // set the packet response completion code to be the ipmi
                // completion code.
                ipmiResponse.CompletionCode = ipmiResponseMessage[7];
            }
            else
            {
                // if the ipmi request reported a time-out response, it is
                // possible the session was terminated unexpectedly.  try to
                // re-establish the session.
                if (completionCode == 0xBE && allowRetry && !OverRideRetry)
                {
                    // Issue a Retry
                    ipmiResponse = LoginRetry(ipmiRequest, responseType, completionCode);
                }
                else
                {
                    // if the Chassis Manager completion code is
                    // unsuccessful, set the ipmi completion code
                    // to the Chassis Manager completion code.
                    ipmiResponse.CompletionCode = completionCode;
                }
            }

            if (ipmiResponse.CompletionCode == 0x00)
            {
                try
                {
                    ipmiResponse.Initialize(IpmiTransport.Serial, ipmiResponseMessage, ipmiResponseMessage.Length, reqSeq);
                    ipmiResponseMessage = null; // response message nolonger needed
                    // reset the communication error counter.
                }
                catch (Exception ex)
                {
                    // set an exception code for invalid data in ipmi data field, as the packet could
                    // not be converted by the InitializeSerial method.
                    ipmiResponse.CompletionCode = 0xCC;

                     Debug.WriteLine(string.Format("Method: {0} Response Packet Completion Code: {1} Exception {2}",
                                            ipmiRequest.GetType().ToString(),
                                            IpmiSharedFunc.ByteArrayToHexString(ipmiResponseMessage),
                                            ex.ToString()));
                }
            }
            else if (ipmiResponse.CompletionCode == 0xD4 && allowRetry && !OverRideRetry) // Catch Ipmi prevelege loss and perform login retry.
            {
                // Issue a re-logon and command retry as Ipmi completion code 
                // D4h indicates session prevelege level issue.
                ipmiResponse = LoginRetry(ipmiRequest, responseType, ipmiResponse.CompletionCode);
            }
            else
            {
                // throw ipmi/dcmi response exception with a custom string message and the ipmi completion code
                Debug.WriteLine(string.Format("Request Type: {0} Response Packet: {1} Completion Code {2}", ipmiRequest.GetType().ToString(),
                    IpmiSharedFunc.ByteArrayToHexString(ipmiResponseMessage), IpmiSharedFunc.ByteToHexString(ipmiResponse.CompletionCode)));
            }

            // Response to the IPMI request message.
            return ipmiResponse;
        }

        /// <summary>
        /// Attempts to re-authenticate with the BMC if the session is dropped.
        /// </summary>
        private IpmiResponse LoginRetry(IpmiRequest ipmiRequest, Type responseType, byte completionCode)
        {

            Debug.WriteLine(string.Format("Ipmi Logon retry for command {0}.",
                                ipmiRequest.GetType().ToString()));

            this.SetClientState(IpmiClientState.Connecting);

            // return resposne
            IpmiResponse response;

            // Attempt to Identify the blade.  (Note: This command does not allow re-try)
            ChannelAuthenticationCapabilities auth = GetAuthenticationCapabilities(PrivilegeLevel.Administrator, false);

            // if get channel authentication succeeds, check if the blade is a compute blade.  If so, re-establish
            // the session and re-execute the command
            if (auth.CompletionCode == 0)
            {
                if (auth.AuxiliaryData == 0x04)
                {
                    this.LogOn();

                    // re-issue original command.                   
                    return response = IpmiSendReceive(ipmiRequest, responseType, false);
                }
            }

            // re-create the original response and return it.
            ConstructorInfo constructorInfo = responseType.GetConstructor(Type.EmptyTypes);
            response = (IpmiResponse)constructorInfo.Invoke(new Object[0]);
            // set the original response code.
            response.CompletionCode = completionCode;

            return response;
        }

        /// <summary>
        /// Read until receiving the stop byte (or timeout)
        /// </summary>
        private void SendReceive(byte[] ipmiRequestMessage, out byte[] messageResponse)
        {
            this.SendData(ipmiRequestMessage);

            // byte 5 is always the sequence byte.
            ReceiveData(ipmiRequestMessage[5], out messageResponse);
            
        }

        private void SendData(byte[] ipmiRequestMessage)
        {
            this.serialPort.Write(ipmiRequestMessage, 0, ipmiRequestMessage.Length);
        }

        private void ReceiveData(byte sequence, out byte[] messageResponse)
        {
             List<byte> receivedBytes = new List<byte>();
            const int maxsize = 128;

            // flags start byte was detected.
            bool startReceived = false;

            // default timeout message
            messageResponse = new byte[1] { 0xBE };

            while (true)
            {
                try
                {
                    int receivedData = serialPort.ReadByte();

                    if (receivedData == startByte)
                    {
                        // flush list encase response was partially received
                        // before another Start Byte sequence was detected
                        receivedBytes.Clear();

                        // set flag to start adding bytes to list
                        startReceived = true;
                    }

                    if (startReceived)
                    {
                        // add the byte to the list
                        byte receivedDataInByte = (byte)receivedData;
                        receivedBytes.Add(receivedDataInByte);

                        // if the 128 byte buffer is exceeded
                        // abort.
                        if (receivedBytes.Count > maxsize)
                        {
                            Debug.WriteLine("Received data packet size is too big");
                            break;
                        }
                    }

                    if (receivedData == stopByte)
                    {
                        // if the stop byte is received, process the message.
                        messageResponse = receivedBytes.ToArray();

                        if (messageResponse[5] == sequence)
                        {
                            break;
                        }
                        else
                        {
                            Debug.WriteLine("Invalid Response Sequence: " + IpmiSharedFunc.ByteArrayToHexString(messageResponse));
                            
                            // continue reading for valid sequence number
                            startReceived = false;

                            // discard received bytes.
                            receivedBytes.Clear();
                        }
                    }

                    // end of buffer
                    if (receivedData == -1)
                    {
                        break;
                    }
                }
                catch (TimeoutException ex)
                {
                    Debug.WriteLine("Serial Port Timeout " + ex.Message.ToString());
                    break;
                }
            }
        }

        /// <summary>
        /// Flushes serial buffers (SerialPort.BaseStream.Flush())
        /// </summary>
        private void FlushBuffers()
        {
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();
        }

        /// <summary>
        /// Writes an escape byte to the serial port to cause the BMC
        /// to switch to Serial Console.
        /// </summary>
        /// <param name="escape"></param>
        internal void SendSerialEscapeCharactor(byte escape = 0xAA)
        {
            lock (this)
            {
                serialPort.BaseStream.WriteByte(escape);
            }
        }

        /// <summary>
        /// Process packed received from serial transport class.
        /// </summary>
        /// <param name="message">Message bytes.</param>
        private void ProcessReceivedMessage(byte[] message, out byte[] ipmiResponseMessage, out byte completionCode)
        {
            if (message.Length > 7)
            {
                // replace escape charactors
                message = this.ReplaceEscapeChars(message);

                completionCode = message[7];

                // Detect and ignore Serial/Modem Active Messages.
                // the responder’s address byte should be set 
                // to 81h, which is the software ID (SWID) for 
                // remote console software Or 0x8F for Serial Console.
                Debug.WriteLine("Received: " + IpmiSharedFunc.ByteArrayToHexString(message) + 
                    " Time: " + DateTime.Now.ToString("HH:mm:ss.fff", 
                    System.Globalization.DateTimeFormatInfo.InvariantInfo));
            }
            else
            {
                completionCode = 0xC7;

                Debug.WriteLine("Received: No response.");
            }

            ipmiResponseMessage = message;

            // check completion code
            if (message.Length > 1)
            {
                // strip the 3 byte validation message received from the 
                // transport class.
                ipmiResponseMessage = new byte[message.Length];

                // copy response packet into respones array
                Buffer.BlockCopy(message, 0, ipmiResponseMessage, 0, (message.Length));
                message = null;

                // Ipmi message heard is 7 bytes.
                if (ipmiResponseMessage.Length >= 7)
                {
                    // check resAddr
                    if (ipmiResponseMessage[1] == 0x8F || ipmiResponseMessage[1] == 0x81)
                    {
                        // Validate checsume before passing packet as valid.
                        if (!ValidateCRC(ipmiResponseMessage))
                        {
                            completionCode = 0xD6;
                        }
                    }
                    else
                    {
                        completionCode = 0xAA;
                        Debug.WriteLine("Response did contain ipmi packet {0}", IpmiSharedFunc.ByteArrayToHexString(ipmiResponseMessage));
                    }

                }
                else
                {
                    completionCode = 0xC7;
                    Debug.WriteLine("Response did contain ipmi packet {0}", IpmiSharedFunc.ByteArrayToHexString(ipmiResponseMessage));
                }
            }
            else
            {
                if (completionCode != 0)
                    Debug.WriteLine("Non-zero completion code, no Ipmi payload: {0}", IpmiSharedFunc.ByteArrayToHexString(message));
            }
        }

        /// <summary>
        /// Validate the payload checksum.  The function code checksum
        /// and rqAdd is not important to the serial client.
        /// </summary>
        private static bool ValidateCRC(byte[] message)
        {
            byte checksum = IpmiSharedFunc.TwoComplementChecksum(4, (message.Length - 2), message);
            // Compare checksum
            if (message[(message.Length - 2)] == checksum)
            {
                return true;
            }
            else
            {
                Debug.Write("CheckSum Mismatch: " + message[(message.Length - 2)] + " " + checksum);
                return false;
            }
        }

        #endregion

    }
}
