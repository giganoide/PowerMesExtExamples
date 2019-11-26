using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;

namespace SocketClient
{
    public class MyTcpClient
    {
        private readonly IMesAppLogger _MesLogger;
        private const string LOGRSOURCE = @"TcpClient";

        private TcpClient tcpClient = null;
        private NetworkStream tcpStream = null;
        private const int port = 59567;
        
        public MyTcpClient() : this(new Logger()) { }

        public MyTcpClient(IMesAppLogger mesLogger)
        {
            _MesLogger = mesLogger;
        }

        public void Connect()
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(IPAddress.Loopback, port);
                Log($"Connect: Starting TCP clients on port {port}...");
            }
            catch (Exception e)
            {
                Log(MessageLevel.Error, $"Connect exception: {e}");
            }
        }

        public void Send(string message)
        {
            try
            {
                var buffer = Encoding.ASCII.GetBytes(message);

                if (tcpStream == null)
                    tcpStream = tcpClient.GetStream();

                tcpStream.Write(buffer, 0, buffer.Length);
                Log(MessageLevel.Diagnostics, $"Send: {message}");
            }
            catch (Exception e)
            {
                Log(MessageLevel.Error, $"Send exception: {e}");
            }
        }

        public void Close()
        {
            try
            {
                Log(MessageLevel.Diagnostics, "Close: Closes the connection and releases all associated resources.");
                tcpClient.Close();
                tcpStream = null;
                tcpClient = null;
            }
            catch (Exception e)
            {
                Log(MessageLevel.Error, $"Close exception: {e}");
            }
        }

        private void Log(string message)
        {
            _MesLogger.WriteMessage(MessageLevel.Diagnostics, true, LOGRSOURCE, message);
        }

        private void Log(MessageLevel level, string message)
        {
            _MesLogger.WriteMessage(level, true, LOGRSOURCE, message);
        }
    }
}