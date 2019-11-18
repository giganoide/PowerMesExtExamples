using System;

namespace TcpListenerServer
{
    [Serializable]
    public class TcpServerConfiguration
    {
        public int Port { get; set; }
        public string FilePath { get; set; }

        public TcpServerConfiguration()
        {
            Port = 59567;
            FilePath = "./fileToWatch.txt";
        }
    }
}