using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace FreeConnHttpProxyServer
{
    class RequestFactory
    {
        private static Byte[] tempBuffer = new Byte[1024];
        public static WebRequest CreateWebRequestFromSocket(Socket socket, Encoding myEncoding)
        {
            HttpWebRequest webReq = null;

            string strClientRequest = ReadMessage(socket, tempBuffer, myEncoding);

            if (strClientRequest != null && strClientRequest.Length > 0)
            {
                strClientRequest = strClientRequest.Replace("\r", "");
                string[] requestValuePairs = strClientRequest.Split(new string[] { "\n" }, StringSplitOptions.None);

                if (requestValuePairs.Length > 1)
                {
                    string[] valuesOfFirstLine = requestValuePairs[0].Split(' ');
                    webReq = (HttpWebRequest)WebRequest.Create(valuesOfFirstLine[1]);

                    Trace.WriteLine("Connecting to Site " + valuesOfFirstLine[1]);
                    Trace.WriteLine("Connection from " + socket.RemoteEndPoint);

                    for (int i = 1; i < requestValuePairs.Length; i++)
                    {
                        string pair = requestValuePairs[i];
                        if (pair != null && pair.Contains(':'))
                        {
                            string[] keyAndValue = pair.Split(':');
                            if (string.Compare(keyAndValue[0], "User-Agent", true) == 0)
                            {
                                webReq.UserAgent = keyAndValue[1];
                            }
                            else if (string.Compare(keyAndValue[0], "Accept", true) == 0)
                            {
                                webReq.Accept = keyAndValue[1];
                            }
                            else if (string.Compare(keyAndValue[0], "Accept-Language", true) == 0)
                            {
                                webReq.Accept = keyAndValue[1];
                            }
                            else if (string.Compare(keyAndValue[0], "Accept-Encoding", true) == 0)
                            {
                                webReq.Accept = keyAndValue[1];
                            }
                            else if (string.Compare(keyAndValue[0], "Accept-Charset", true) == 0)
                            {
                                webReq.Accept = keyAndValue[1];
                            }
                        }
                    }
                }
            }

            return webReq;
        }

        private static string ReadMessage(Socket socket, byte[] buf, Encoding myEncoding)
        {
            int iBytes = socket.Receive(buf, buf.Length, 0);
            return myEncoding.GetString(buf);
        }

    }
}
