using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Net;

namespace FreeConnHttpProxyServer
{
    class HttpProxyServer
    {
        Socket socketClient;
        Byte[] readBuff = new Byte[1024];
        Byte[] tmpBuffer = null;
        Encoding myEncoding = Encoding.UTF8;

        public HttpProxyServer(Socket socket)
        {
            this.socketClient = socket;
        }

        public void Run()
        {
            string strFromRequest = string.Empty;

            try
            {
                int lengthOfIncomingMessage = ReadMessage(this.socketClient, readBuff, ref strFromRequest);
                Trace.WriteLine("In coming request string is : \n" + strFromRequest);

                if (lengthOfIncomingMessage == 0)
                {
                    return;
                }

                // Get the URL for the connection. The client browser sends a GET command
                // followed by a space, then the URL, then and identifer for the HTTP version.
                // Extract the URL as the string betweeen the spaces.
                int index1 = strFromRequest.IndexOf(' ');
                int index2 = strFromRequest.IndexOf(' ', index1 + 1);

                string strRequestConnection = strFromRequest.Substring(index1 + 1, index2 - 1);
                Trace.WriteLine("Request destination : \n" + strRequestConnection);
                Trace.WriteLine("Request from        : \n" + this.socketClient.RemoteEndPoint);

                if (index1 < 0 || index2 < 0)
                {
                    throw (new IOException());
                }

                // Create web request object
                WebRequest req = WebRequest.Create(strRequestConnection);
                WebResponse response = req.GetResponse();

                int bytesRead = 0;
                Byte[] buffer = new Byte[32];
                int bytesSend = 0;

                Stream responseStream = response.GetResponseStream();
                bytesRead = responseStream.Read(buffer, 0, buffer.Length);
                StringBuilder strResponse = new StringBuilder("");
                while (bytesRead != 0)
                {
                    strResponse.Append(myEncoding.GetString(buffer, 0, buffer.Length));
                    this.socketClient.Send(buffer, bytesRead, SocketFlags.None);
                    bytesSend += bytesRead;
                    bytesRead = responseStream.Read(buffer, 0, buffer.Length);
                }
            }
            catch (FileNotFoundException e)
            {
                SendErrorPage (404, "File Not Found", e.Message);
            }
            catch (IOException e)
            {
                SendErrorPage (503, "Service not available", e.Message);
            }
            catch (Exception e)
            {
                  SendErrorPage (404, "File Not Found", e.Message);
                  Trace.WriteLine (e.StackTrace);
                  Trace.WriteLine (e.Message);
            }
            finally
            {
                // Disconnect and close the socket.
                if (socketClient != null)
                {
                    if (socketClient.Connected)
                    {
                        socketClient.Close ();
                    }
                }
            }
        }

        // Write an error response to the client.
        void SendErrorPage(int status, string strReason, string strText)
        {
            SendMessage(socketClient, "HTTP/1.0" + " " +
                         status + " " + strReason + "\n");
            SendMessage(socketClient, "Content-Type: text/plain" + "\n");
            SendMessage(socketClient, "Proxy-Connection: close" + "\n");
            SendMessage(socketClient, "\n");
            SendMessage(socketClient, status + " " + strReason);
            SendMessage(socketClient, strText);
        }

        // Send a string to a socket.
        void SendMessage(Socket sock, string strMessage)
        {
            tmpBuffer = new Byte[strMessage.Length + 1];
            int len = myEncoding.GetBytes(strMessage.ToCharArray(),
                                          0,
                                          strMessage.Length, tmpBuffer, 0);
            sock.Send(tmpBuffer, len, 0);
        }

        // Read a string from a socket.
        int ReadMessage(Socket socket, byte[] buf, ref string strMessage)
        {
            int iBytes = socket.Receive(buf, 1024, 0);
            strMessage = myEncoding.GetString(buf);
            return (iBytes);
        }
    }
}
