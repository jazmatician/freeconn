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
        public HttpProxyServer(Socket sockClient)
        {
            m_sockClient = sockClient;
        }
        Socket m_sockClient; //, m_sockServer;
        Byte[] readBuf = new Byte[1024];
        Byte[] buffer = null;
        Encoding ASCII = Encoding.ASCII;

        public void Run()
        {
            string strFromClient = "";
            try
            {
                // Read the incoming text on the socket/
                int bytes = ReadMessage(m_sockClient,
                                         readBuf, ref strFromClient);
                // If it's empty, it's an error, so just return.
                // This will termiate the thread.
                if (bytes == 0)
                    return;
                // Get the URL for the connection. The client browser sends a GET command
                // followed by a space, then the URL, then and identifer for the HTTP version.
                // Extract the URL as the string betweeen the spaces.
                int index1 = strFromClient.IndexOf(' ');
                int index2 = strFromClient.IndexOf(' ', index1 + 1);
                string strClientConnection =
                      strFromClient.Substring(index1 + 1, index2 - index1);

                if ((index1 < 0) || (index2 < 0))
                {
                    throw (new IOException());
                }
                // Write a messsage that we are connecting.
                Trace.WriteLine("Connecting to Site " +
                                   strClientConnection);
                Trace.WriteLine("Connection from " +
                                   m_sockClient.RemoteEndPoint);
                // Create a WebRequest object.
                WebRequest req = (WebRequest)WebRequest.Create
                                                     (strClientConnection);
                // Get the response from the Web site.
                WebResponse response = req.GetResponse();
                int BytesRead = 0;
                Byte[] Buffer = new Byte[32];
                int BytesSent = 0;

                // Create a response stream object.
                Stream ResponseStream = response.GetResponseStream();

                // Read the response into a buffer.
                BytesRead = ResponseStream.Read(Buffer, 0, 32);
                StringBuilder strResponse = new StringBuilder("");
                while (BytesRead != 0)
                {
                    // Pass the response back to the client
                    strResponse.Append(Encoding.ASCII.GetString(Buffer,
                                        0, BytesRead));
                    m_sockClient.Send(Buffer, BytesRead, 0);
                    BytesSent += BytesRead;
                    // Read the next part of the response
                    BytesRead = ResponseStream.Read(Buffer, 0, 32);
                }
            }
            catch (FileNotFoundException e)
            {
                SendErrorPage(404, "File Not Found", e.Message);
            }
            catch (IOException e)
            {
                SendErrorPage(503, "Service not available", e.Message);
            }
            catch (Exception e)
            {
                SendErrorPage(404, "File Not Found", e.Message);
                Trace.WriteLine(e.StackTrace);
                Trace.WriteLine(e.Message);
            }
            finally
            {
                // Disconnect and close the socket.
                if (m_sockClient != null)
                {
                    if (m_sockClient.Connected)
                    {
                        m_sockClient.Close();
                    }
                }
            }
            // Returning from this method will terminate the thread.
        }
        // Write an error response to the client.
        void SendErrorPage(int status, string strReason, string strText)
        {
            SendMessage(m_sockClient, "HTTP/1.0" + " " +
                         status + " " + strReason + "\r\n");
            SendMessage(m_sockClient, "Content-Type: text/plain" + "\r\n");
            SendMessage(m_sockClient, "Proxy-Connection: close" + "\r\n");
            SendMessage(m_sockClient, "\r\n");
            SendMessage(m_sockClient, status + " " + strReason);
            SendMessage(m_sockClient, strText);
        }

        // Send a string to a socket.
        void SendMessage(Socket sock, string strMessage)
        {
            buffer = new Byte[strMessage.Length + 1];
            int len = ASCII.GetBytes(strMessage.ToCharArray(),
                                      0, strMessage.Length, buffer, 0);
            sock.Send(buffer, len, 0);
        }

        // Read a string from a socket.
        int ReadMessage(Socket sock, byte[] buf, ref string strMessage)
        {
            int iBytes = sock.Receive(buf, 1024, 0);
            strMessage = Encoding.ASCII.GetString(buf);
            return (iBytes);
        }
    }
}
