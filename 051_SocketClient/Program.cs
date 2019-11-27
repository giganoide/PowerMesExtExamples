using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketClient
{
    class Program
    {
        static void Main(string[] args)
        {

            AlwaysNewClientAndNotCloseConnection();
            //var message = Console.ReadLine();
            //client.Send(message);

            Console.WriteLine("\nDone!\nPress any key to exit the process...");
            Console.ReadKey();
        }

        private static void AlwaysNewClientAndNotCloseConnection()
        {
            var client = new MyTcpClient();
            client.Connect();
            Thread.Sleep(1000);
            client.Send("I+ABC111+10+20150316085013");
            Thread.Sleep(500);
            client.Send("F+ABC111+10+1+20150316085510");
            
            //client.Close();

            Thread.Sleep(3000);
            client = new MyTcpClient();
            client.Connect();
            Thread.Sleep(1000);
            client.Send("I+ABC222+10+20150316085013");
            Thread.Sleep(1000);
            client.Send("F+ABC222+10+1+20150316085510");
            Thread.Sleep(1000);

            Thread.Sleep(1000);
            client = new MyTcpClient();
            client.Connect();
            Thread.Sleep(1000);
            client.Send("I+ABC333+10+20150316085013");
            Thread.Sleep(1000);
            client.Send("F+ABC333+10+1+20150316085510");
            Thread.Sleep(1000);

            client.Close();
        }

        private static void OneClient()
        {
            var client = new MyTcpClient();
            client.Connect();
            Thread.Sleep(1000);
            client.Send("I+ABCDE+10+20150316085013");
            Thread.Sleep(500);
            client.Send("F+ABCDE+10+1+20150316085510");
            Thread.Sleep(1000);
            client.Close();

            client.Connect();
            Thread.Sleep(3000);
            client.Send("I+ABCDE+10+20150316085013");
            Thread.Sleep(5000);
            client.Send("F+ABCDE+10+1+20150316085510");
            Thread.Sleep(1000);
            client.Close();
        }
    }
}
