using Serilog;
using Topshelf;

namespace TcpListenerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(configure =>
            {
                configure.Service<TcpServerService>(service =>
                {
                    // here you can pass dependencies and configuration to the service
                    service.ConstructUsing(s => new TcpServerService());
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });

                configure.StartAutomatically();
                configure.EnableServiceRecovery(r => r.RestartService(0));
                configure.RunAsLocalSystem();

                configure.SetServiceName("PowerTcpListener");
                configure.SetDisplayName("Power TCP listener");
                configure.SetDescription("Tcp listener service of PowerSuite");
                
                configure.UseSerilog(CreateLogger());
            });
        }

        private static ILogger CreateLogger()
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("log.txt", rollingInterval:RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 102400, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error)
                .CreateLogger();
            return logger;
        }

        
    }
}
