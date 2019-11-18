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
            var configPath = ConfigurationManager.GetConfigPath();
            _log.Info($"Load configuration from: {configPath}");
            var config = ConfigurationManager.GetConfiguration(configPath);
            _log.Info("Configuration loaded");
            server = new TcpServer(_log, config.Port, config.FilePath);
            server.StartListening();
            _log.Info("TcpServerService started");
        }

        public void Stop()
        {
            _log.Info("Stopping TcpServerService ...");
            server.StopListening();
            server = null;
            _log.Info("TcpServerService stopped");
        }
    }
}