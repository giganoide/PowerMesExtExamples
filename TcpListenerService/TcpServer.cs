using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Topshelf.Logging;

namespace TcpListenerServer
{
    public class TcpServer
    {
        private TcpListener tcpServer = null;

        private static LogWriter _logger;
        private readonly int _port;
        private readonly string _filePath;
        private readonly Queue<string> messagesToWrite = new Queue<string>();
        private readonly object writeFileLock = new object();

        public TcpServer(LogWriter logger, int port = 59567, string filePath = "./fileToWatch.txt")
        {
            _logger = logger;
            _port = port;
            _filePath = filePath;
        }

        public void StartListening()
        {
            Log($"StartListening at port {_port}");
            Log($"File will be written in {_filePath}");
            try
            {
                tcpServer = new TcpListener(IPAddress.Any, _port);

                var tcpThread = new Thread(AcceptTcpClientProcess)
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

        private void AcceptTcpClientProcess(object arg)
        {
            Thread clientThreadEven = null;
            Thread clientThreadOdd = null;
            ClientThreadArgs argsThreadEven = null;
            ClientThreadArgs argsThreadOdd = null;

            Log("TCP server thread started");

            try
            {
                var server = (TcpListener)arg;
                server.Start();

                for (; ; )
                {
                    var client = server.AcceptTcpClient();
                    Log("Client connected");

                    if (clientThreadEven == null || !clientThreadEven.IsAlive)
                    {
                        clientThreadEven = new Thread(SingleClientListeningProcess)
                        {
                            IsBackground = true,
                            Name = "TCP client even thread"
                        };
                        argsThreadEven = new ClientThreadArgs(client, 1);
                        argsThreadOdd?.TcpClient.Close();
                        clientThreadEven.Start(argsThreadEven);
                    }
                    else
                    {
                        clientThreadOdd = new Thread(SingleClientListeningProcess)
                        {
                            IsBackground = true,
                            Name = "TCP client odd thread"
                        };
                        argsThreadOdd = new ClientThreadArgs(client, 2);
                        argsThreadEven.TcpClient.Close();
                        clientThreadOdd.Start(argsThreadOdd);
                    }
                    
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

        private void SingleClientListeningProcess(object arg)
        {
            var threadArgs = (ClientThreadArgs)arg;
            var client = threadArgs.TcpClient;
            var instanceId = threadArgs.InstanceId;

            Log($"Client Thread [{instanceId}] started");

            try
            {
                
                var buffer = new byte[2048];

                Log($"Client {instanceId} connected");

                using (var stream = client.GetStream())
                {
                    int count;
                    while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        var message = Encoding.ASCII.GetString(buffer, 0, count);
                        Log(message);
                        WriteFile(message);
                    }
                }

                //client.Close();
                Log($"Client {instanceId} closed");
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

            Log($"Client Thread [{instanceId}] finished");
        }

        /*
        private void WriteFile(Stream reader)
        {
            //using (var fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write))
            using (var fileStream = new FileStream("./FileStream.txt", FileMode.Append, FileAccess.Write))
            {
                reader.CopyTo(fileStream);
            }
        }
        */

        private void WriteFile(string message)
        {
            lock (writeFileLock)
            {
                try
                {
                    using (var streamWriter = File.AppendText(_filePath))
                    {
                        if (messagesToWrite.Count > 0)
                        {
                            Log($"Write {messagesToWrite.Count} messages stored");
                            foreach (var messageStored in messagesToWrite)
                                streamWriter.WriteLine(messageStored);

                            messagesToWrite.Clear();
                        }

                        streamWriter.WriteLine(message);
                        streamWriter.Flush();
                    }

                }
                catch (Exception exception)
                {
                    Log(exception);
                    messagesToWrite.Enqueue(message);
                }
            }
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

    public class ClientThreadArgs
    {
        public TcpClient TcpClient { get; }
        public int InstanceId { get; }

        public ClientThreadArgs(TcpClient tcpClient, int instanceId)
        {
            TcpClient = tcpClient;
            InstanceId = instanceId;
        }
    }
}