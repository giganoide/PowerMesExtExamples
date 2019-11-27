using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;

namespace TeamSystem.Customizations
{
    public class TcpServer
    {
        private const string LOGRSOURCE = @"TcpServer";
        TcpListener tcpServer = null;
        private const int PORT = 59567;

        private static IMesAppLogger _MesLogger;
        private Thread tcpThread;

        public TcpServer(IMesAppLogger mesLogger)
        {
            _MesLogger = mesLogger;
        }

        public void StartListening()
        {
            Log($"StartListening at port {PORT}");
            try
            {
                tcpServer = new TcpListener(IPAddress.Any, PORT);

                tcpThread = new Thread(TCPServerProc)
                {
                    IsBackground = true,
                    Name = "TCP server thread"
                };
                tcpThread.Start(tcpServer);
            }
            catch (Exception ex)
            {
                Log(MessageLevel.Error, $"StartListening exception: {ex}");
            }
            finally
            {
                tcpServer?.Stop();
            }
        }

        public void StopListening()
        {
            Log("StopListening: stoping");
            tcpThread.Abort();
            tcpServer.Stop();
            Log("StopListening: stoped");
        }

        private static void TCPServerProc(object arg)
        {
            Log("Thread started");

            try
            {
                var server = (TcpListener) arg;
                var buffer = new byte[2048];

                server.Start();

                for (;;)
                {
                    var client = server.AcceptTcpClient();
                    Log("Client connected");

                    using (var stream = client.GetStream())
                    {
                        int count;
                        while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            /* *** Creazione comandi MES *** */
                            Log(Encoding.ASCII.GetString(buffer, 0, count));
                        }
                    }

                    client.Close();
                    Log("Client closed");
                }
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode != 10004) // unexpected
                    Log(MessageLevel.Error, "TCPServerProc exception: " + ex);
            }
            catch (Exception ex)
            {
                Log(MessageLevel.Error, "TCPServerProc exception: " + ex);
            }

            Log("TCP server thread finished");
        }

        private static void Log(string message)
        {
            _MesLogger.WriteMessage(MessageLevel.Diagnostics, true, LOGRSOURCE, message);
        }

        private static void Log(MessageLevel level, string message)
        {
            _MesLogger.WriteMessage(level, true, LOGRSOURCE, message);
        }
    }
}