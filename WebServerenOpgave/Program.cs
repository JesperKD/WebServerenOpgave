using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WebServerenOpgave
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress address = IPAddress.Parse("192.168.80.1");
            int port = 1337;
            int maxConnections = 5;
            string contentPath = Environment.CurrentDirectory + "\\index.txt";
            
            ///Creates a new WebServer
            WebServer webServer = new WebServer();

            Console.WriteLine("Starting The server...");
            webServer.Start(address, port, maxConnections, contentPath);
            Console.WriteLine("Server has been started.");

            Console.WriteLine("Press any key to stop the webserver");
            if(Console.ReadLine() == "stop")
            {
                webServer.Stop();
            }
        }
    }
}
