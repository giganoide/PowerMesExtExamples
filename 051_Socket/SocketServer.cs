using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;

namespace TeamSystem.Customizations
{
    public class SocketServer
    {
        private const string LOGRSOURCE = @"SocketServer";

        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        private static IMesAppLogger _MesLogger;

        public SocketServer(IMesAppLogger mesLogger)
        {
            _MesLogger = mesLogger;
        }

        public void StartListening()
        {
            // Establish the local endpoint for the socket.  
            var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = ipHostInfo.AddressList[0];
            var localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                //while (true)
                //{
                    // Set the event to nonsignaled state.  
                    //allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Log(MessageLevel.Diagnostics, "Waiting for a connection...");

                    listener.BeginAccept(AcceptCallback, listener);

                    // Wait until a connection is made before continuing.  
                    //allDone.WaitOne(1000);
                //
            }
            catch (Exception e)
            {
                Log(MessageLevel.Error, e.ToString());
            }
        }

        private static void AcceptCallback(IAsyncResult asyncResult)
        {
            Log(MessageLevel.Diagnostics, $"Connection accepted");
            // Signal the main thread to continue.  
            //allDone.Set();

            // Get the socket that handles the client request.  
            var listener = (Socket) asyncResult.AsyncState;
            var handler = listener.EndAccept(asyncResult);

            // Create the state object.  
            var state = new StateObject {workSocket = handler};
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);
        }

        private static void ReadCallback(IAsyncResult asyncResult)
        {
            Log(MessageLevel.Diagnostics, "Data received");

            var content = string.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            var state = (StateObject) asyncResult.AsyncState;
            var handler = state.workSocket;

            // Read data from the client socket.   
            var bytesRead = handler.EndReceive(asyncResult);

            if (bytesRead <= 0)
                return;

            // There  might be more data, so store the data received so far.  
            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

            // Check for end-of-file tag. If it is not there, read   
            // more data.  
            content = state.sb.ToString();
            if (content.IndexOf("<EOF>") > -1)
            {
                // All the data has been read from the client.
                Log(MessageLevel.Diagnostics, $"Read {content.Length} bytes from socket. Data : {content}");
                // Echo the data back to the client.  
                //Send(handler, content);
            }
            else
            {
                // Not all data received. Get more.  
                Log(MessageLevel.Diagnostics, "Not all data received. Get more.");
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);
            }
        }

        private static void Send(Socket handler, string data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            var byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                var handler = (Socket) ar.AsyncState;

                // Complete sending the data to the remote device.  
                var bytesSent = handler.EndSend(ar);
                Log(MessageLevel.Diagnostics, $"Sent {bytesSent} bytes to client.");

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Log(MessageLevel.Error, e.ToString());
            }
        }

        private static void Log(MessageLevel level, string message)
        {
            _MesLogger.WriteMessage(level, true, LOGRSOURCE, message);
        }
    }
}