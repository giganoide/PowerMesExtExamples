using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;

namespace SocketClient
{
    public class SynchronousSocketClient
    {
        private readonly IMesAppLogger _MesLogger;
        private Socket sender;
        private const string LOGRSOURCE = @"SocketClient";

        public SynchronousSocketClient() : this(new Logger()) { }

        public SynchronousSocketClient(IMesAppLogger mesLogger)
        {
            _MesLogger = mesLogger;
        }

        public void Connect()
        {
            try
            {
                // Establish the remote endpoint for the socket.  
                // This example uses port 11000 on the local computer.  
                var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                var ipAddress = ipHostInfo.AddressList[0];
                var remoteEP = new IPEndPoint(ipAddress, 11000);

                // Create a TCP/IP  socket.  
                sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                sender.Connect(remoteEP);
                Log(MessageLevel.Diagnostics, $"Connect to {sender.RemoteEndPoint}");
            }
            catch (Exception e)
            {
                Log(MessageLevel.Error, $"Connect() exception: {e}");
            }
        }

        public void Send(string message)
        {
            // Encode the data string into a byte array.  
            var msg = Encoding.ASCII.GetBytes($"{message}<EOF>");
            // Send the data through the socket.  
            var bytesSent = sender.Send(msg);
            Log(MessageLevel.Diagnostics, $"Send: {message}");

            //var bytes = new byte[1024];
            //var bytesRec = sender.Receive(bytes);
            //Log(MessageLevel.Diagnostics, $"Echoed test: {Encoding.ASCII.GetString(bytes, 0, bytesRec)}");
        }

        public void Shutdown()
        {
            Log(MessageLevel.Diagnostics, "Shutdown(): Disables sends and receives");
            sender.Shutdown(SocketShutdown.Both);
        }

        public void Close()
        {
            Log(MessageLevel.Diagnostics, "Close(): Closes the connection and releases all associated resources.");
            sender.Close();
        }

        public void StartClient()
        {
            // Data buffer for incoming data.  
            var bytes = new byte[1024];

            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                // This example uses port 11000 on the local computer.  
                var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                var ipAddress = ipHostInfo.AddressList[0];
                var remoteEP = new IPEndPoint(ipAddress, 11000);

                // Create a TCP/IP  socket.  
                var sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.  
                try
                {
                    sender.Connect(remoteEP);

                    Log(MessageLevel.Diagnostics, $"Socket connected to {sender.RemoteEndPoint}");

                    // Encode the data string into a byte array.  
                    var msg = Encoding.ASCII.GetBytes("This is a test<EOF>");

                    // Send the data through the socket.  
                    var bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.  
                    var bytesRec = sender.Receive(bytes);
                    Log(MessageLevel.Diagnostics, $"Echoed test = {Encoding.ASCII.GetString(bytes, 0, bytesRec)}");

                    // Release the socket.  
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }
                catch (ArgumentNullException ane)
                {
                    _MesLogger.WriteMessage(MessageLevel.Error, false, LOGRSOURCE, $"ArgumentNullException: {ane}");
                }
                catch (SocketException se)
                {
                    _MesLogger.WriteMessage(MessageLevel.Error, false, LOGRSOURCE, $"SocketException: {se}");
                }
                catch (Exception e)
                {
                    _MesLogger.WriteMessage(MessageLevel.Error, false, LOGRSOURCE, $"Unexpected exception: {e}");
                }
            }
            catch (Exception e)
            {
                _MesLogger.WriteMessage(MessageLevel.Error, false, LOGRSOURCE, $"{e}");
            }
        }

        private void Log(MessageLevel level, string message)
        {
            _MesLogger.WriteMessage(level, true, LOGRSOURCE, message);
        }
    }
}