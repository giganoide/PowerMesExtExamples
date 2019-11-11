using System;

namespace TcpListenerServer
{
    [Serializable]
    public class ServiceConfiguration
    {
        public int Port { get; set; }
        public string FilePath { get; set; }

        public ServiceConfiguration()
        {
            Port = 59567;
            FilePath = "./fileToWatch.txt";
        }
    }
}