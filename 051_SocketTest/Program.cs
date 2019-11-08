using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;
using TeamSystem.Customizations;

namespace SocketTest
{
    class Program
    {
        private class Logger : IMesAppLogger
        {
            private readonly ConsoleColor _Color;

            public Logger() : this(ConsoleColor.White) { }

            public Logger(ConsoleColor color) { _Color = color; }

            public void WriteMessage(MessageLevel level, bool sendToUi, string source, string message)
            {
                Console.ForegroundColor = _Color;
                Console.WriteLine($"{source}-{message}");
            }

            public void WriteMessage(MessageLevel level, string source, string messageOrFormatString, params object[] args)
            {
                WriteMessage(level,false, source, messageOrFormatString, args);
            }

            public void WriteMessage(MessageLevel level, bool sendToUi, string source, string messageOrFormatString, params object[] args)
            {
                Console.ForegroundColor = _Color;
                var message = $"{source}-{messageOrFormatString}";
                Console.WriteLine(message, args);
            }

            public void WriteException(Exception ex, string source, string message)
            {
                WriteMessage(MessageLevel.Error, false, source, message);
            }

            public void WriteException(Exception ex, string source, string messageOrFormatString, params object[] args)
            {
                WriteMessage(MessageLevel.Error, false, source, messageOrFormatString, args);
            }

            public bool IsUiConnected => false;
        }

        static void Main(string[] args)
        {
            var server = new SocketServer(new Logger(ConsoleColor.Cyan));
            server.StartListening();
            
            Console.WriteLine("\nPress any key to exit the process...");
            Console.ReadKey();
        }
    }
}
