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
            IPAddress address = IPAddress.Parse(GetLocalIPAddress());
            int port = 1337;
            int maxConnections = 5;
            string contentPath = Environment.CurrentDirectory + "\\index.txt";
            
            ///Creates a new WebServer
            WebServer webServer = new WebServer();

            Console.WriteLine("Starting The server...");
            webServer.Start(address, port, maxConnections, contentPath);
            Console.WriteLine($"Server has been started on ip: {address.ToString()}:{port}");

            Console.WriteLine("Press any key to stop the webserver");
            if(Console.ReadLine() == "stop")
            {
                webServer.Stop();
            }
        }

        /// <summary>
        /// Returns the local IPv4 address of the machine
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
