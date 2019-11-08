using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Serilog;
using Serilog.Events;
using Topshelf.Logging;

namespace TcpListenerServer
{
    public class TcpServer
    {
        private TcpListener tcpServer = null;
        private const int PORT = 59567;

        private static LogWriter _logger;

        public TcpServer(LogWriter logger)
        {
            _logger = logger;
        }

        public void StartListening()
        {
            Log($"StartListening at port {PORT}");
            try
            {
                tcpServer = new TcpListener(IPAddress.Any, PORT);

                var tcpThread = new Thread(TCPServerProc)
                {
                    IsBackground = true,
                    Name = "TCP server thread"
                };
                tcpThread.Start(tcpServer);
            }
            catch (Exception ex)
            {
                Log(ex);
            }
            finally
            {
                tcpServer?.Stop();
            }
        }

        public void StopListening()
        {
            Log("StopListening: stop");
        }

        private static void TCPServerProc(object arg)
        {
            Log("Thread started");

            try
            {
                var server = (TcpListener)arg;
                var buffer = new byte[2048];

                server.Start();

                for (; ; )
                {
                    var client = server.AcceptTcpClient();
                    Log("Client connected");

                    using (var stream = client.GetStream())
                    {
                        int count;
                        while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                            Log(Encoding.ASCII.GetString(buffer, 0, count));
                    }

                    client.Close();
                    Log("Client closed");
                }
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode != 10004) // unexpected
                    Log(ex);
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            Log("TCP server thread finished");
        }

        private static void Log(string message)
        {
            _logger.Info(message);
        }

        private static void Log(Exception exception)
        {
            _logger.Error(exception, exception);
        }

        private static void Log(LoggingLevel level, string message)
        {
            _logger.Log(level, message);
        }
    }
}