using System;
using Topshelf.Logging;

namespace TcpListenerServer
{
    public class TcpServerService
    {
        private TcpServer server;
        private readonly LogWriter _log = HostLogger.Get<TcpServerService>();

        public void Start()
        {
            _log.Info("Starting TcpServerService ...");
            server = new TcpServer(_log);
            server.StartListening();
        }

        public void Stop()
        {
            _log.Info("Stopping TcpServerService ...");
            server.StopListening();
            server = null;
        }
    }
}