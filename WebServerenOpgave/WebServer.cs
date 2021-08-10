using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WebServerenOpgave
{
    public class WebServer
    {
        public bool running = false;

        private int timeout = 8;
        private Encoding charEncoder = Encoding.UTF8;
        private Socket serverSocket;
        private string contentPath;

        //Dictionary to hold the filetype extensions
        private Dictionary<string, string> extensions = new Dictionary<string, string>()
        {
            { "html", "text/html" },
            { "xml", "text/xml" },
            { "txt", "text/plain" },
            { "css", "text/css" },
            { "png", "image/png" },
            { "gif", "image/gif" },
            { "jpg", "image/jpg" },
            { "jpeg", "image/jpeg" },
            { "zip", "application/zip"}
        };

        /// <summary>
        /// Starts the Webserver
        /// </summary>
        /// <param name="iPAddress"></param>
        /// <param name="port"></param>
        /// <param name="maxConnections"></param>
        /// <param name="contentPath"></param>
        /// <returns></returns>
        public bool Start(IPAddress iPAddress, int port, int maxConnections, string contentPath)
        {
            //Will not start if already running
            if (running) return false;

            try
            {
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(iPAddress, port));
                serverSocket.Listen(maxConnections);
                serverSocket.ReceiveTimeout = timeout;
                serverSocket.SendTimeout = timeout;
                running = true;
                this.contentPath = contentPath;
            }
            catch (Exception ex)
            { 
                Console.WriteLine(ex.Message);
                return false;
            }

            //Creates a thread to listen to client requests
            Thread requestListenerT = new Thread(() =>
            {
                while (running)
                {
                    Socket clientSocket;
                    try
                    {
                        clientSocket = serverSocket.Accept();

                        //creates a thread to handle the request
                        Thread requestHandler = new Thread(() =>
                        {
                            clientSocket.ReceiveTimeout = timeout;
                            clientSocket.SendTimeout = timeout;
                            try 
                            {
                                HandleTheRequest(clientSocket);
                            }
                            catch( Exception e)
                            {
                                Console.WriteLine(e.Message);

                                try 
                                { 
                                    clientSocket.Close(); 
                                }
                                catch (Exception ex)
                                { 
                                    Console.WriteLine(ex.Message); 
                                }
                            }
                        });

                        requestHandler.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message); 
                    }
                }
            });

            requestListenerT.Start();

            return true;
        }

        /// <summary>
        /// Stops the webserver
        /// </summary>
        public void Stop()
        {
            if (running)
            {
                running = false;
                try
                {
                    serverSocket.Close();
                }
                catch (Exception ex)
                { 
                    Console.WriteLine(ex.Message); 
                }
                serverSocket = null;
            }
        }

        /// <summary>
        /// Method to handle requests from the client
        /// </summary>
        /// <param name="clientSocket"></param>
        private void HandleTheRequest(Socket clientSocket)
        {
            Console.WriteLine("Handling request from Client");

            byte[] buffer = new byte[10240];
            int recievedBCount = clientSocket.Receive(buffer);
            string stringReceived = charEncoder.GetString(buffer, 0, recievedBCount);

            //Takes index 0 and reads it as an httpMethod
            string httpMethod = stringReceived.Substring(0, stringReceived.IndexOf(" "));

            int start = stringReceived.IndexOf(httpMethod) + httpMethod.Length + 1;
            int length = stringReceived.LastIndexOf("HTTP") - start - 1;
            string requestedUrl = stringReceived.Substring(start, length);

            string requestedFile;

            //Checks if the request includes a GET or POST method call
            if (httpMethod.Equals("GET") || httpMethod.Equals("POST"))
                requestedFile = requestedUrl.Split('?')[0];
            else
            {
                NotImplemented(clientSocket);
                return;
            }

            //Replacing symbols to not break strings
            requestedFile = requestedFile.Replace("/", @"\").Replace("\\..", "");
            start = requestedFile.LastIndexOf('.') + 1;

            if (start > 0)
            {
                length = requestedFile.Length - start;
                string extension = requestedFile.Substring(start, length);
                if (extensions.ContainsKey(extension))
                {
                    //Checks if there is any content to send back in the body of the response
                    if (File.Exists(contentPath + requestedFile))
                    {
                        SendOkResponse(clientSocket,
                            File.ReadAllBytes(contentPath + requestedFile), extensions[extension]);
                    }
                    else
                    {
                        NotFound(clientSocket);
                    }
                }
            }
            else
            {
                if (requestedFile.Substring(length - 1, 1) != @"\")
                {
                    requestedFile += @"\";
                }

                if (File.Exists(contentPath + requestedFile + "index.html"))
                {
                    SendOkResponse(clientSocket,
                      File.ReadAllBytes(contentPath + requestedFile + "\\index.html"), "text/html");
                }
                else if (File.Exists(contentPath + requestedFile + "index.html"))
                {
                    SendOkResponse(clientSocket,
                        File.ReadAllBytes(contentPath + requestedFile + "\\index.html"), "text/html");
                }
                else
                {
                    NotFound(clientSocket);
                }
            }

        }

        /// <summary>
        /// Method for sending the status code 200
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="bContent"></param>
        /// <param name="contentType"></param>
        private void SendOkResponse(Socket clientSocket, byte[] bContent, string contentType)
        {
            SendResponse(clientSocket, bContent, "200 OK", contentType);
        }

        /// <summary>
        /// Method for sending a 404 error
        /// </summary>
        /// <param name="clientSocket"></param>
        private void NotFound(Socket clientSocket)
        {
            string html = new string(@"<html><head><meta http-equiv=\Content - Type\content=\text/html; 
            charset = utf - 8\></head><body><h2>Jespers Test WebServer </h2><div>404 - Not Found</div></body></html> ");

            Console.WriteLine("Issuing a 404 Error");
            SendResponse(clientSocket, html, "404 Not Found", "text/html");
        }

        /// <summary>
        /// Method for sending a 501 error
        /// </summary>
        /// <param name="clientSocket"></param>
        private void NotImplemented(Socket clientSocket)
        {
            string html = new string(@"<html><head><meta http-equiv=\Content-Type\content=\text/html; 
             charset = utf - 8\></head><body><h2>Jesper's Test WebServer</h2><div>501 - Method Not Implemented</div></body></html>");

            Console.WriteLine("Issuing a 501 Error");
            SendResponse(clientSocket, html, "501 Not Implemented", "text/html");
        }

        /// <summary>
        /// Takes a content string and sends a response to the client
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="stringContent"></param>
        /// <param name="responseCode"></param>
        /// <param name="contentType"></param>
        private void SendResponse(Socket clientSocket, string stringContent, string responseCode, string contentType)
        {
            byte[] bContent = charEncoder.GetBytes(stringContent);
            SendResponse(clientSocket, bContent, responseCode, contentType);
        }

        /// <summary>
        /// Takes a byte array and sends a response to the client
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="bContent"></param>
        /// <param name="responseCode"></param>
        /// <param name="contentType"></param>
        private void SendResponse(Socket clientSocket, byte[] bContent, string responseCode, string contentType)
        {
            try
            {
                //Fills out the header of the response
                byte[] bHeader = charEncoder.GetBytes(
                                    "HTTP/1.1 " + responseCode + "\r\n"
                                  + "Server: Jesper's Test WebServer\r\n"
                                  + "Content-Length: " + bContent.Length.ToString() + "\r\n"
                                  + "Connection: close\r\n"
                                  + "Content-Type: " + contentType + "\r\n\r\n");

                Console.WriteLine("Sending response");
                clientSocket.Send(bHeader);
                clientSocket.Send(bContent);
                clientSocket.Close();
            }
            catch (Exception ex)
            { Console.WriteLine(ex.Message); }
        }

    }
}
