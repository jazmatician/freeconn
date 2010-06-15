using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System.Net.Sockets;
using System.IO;

namespace FreeConnHttpProxyServer
{
    public class WorkerRole : RoleEntryPoint
    {
        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.WriteLine("FreeConnHttpProxyServer entry point called", "Information");

            RoleInstanceEndpoint endPoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["ProxyPort"];
            try
            {
                // Create a listener for the proxy port
                TcpListener sockServer = new TcpListener(endPoint.IPEndpoint);
                sockServer.Start();

                Trace.WriteLine("Read !!!");
                while (true)
                {
                    // Accept connections on the proxy port.
                    Socket socket = sockServer.AcceptSocket();

                    // When AcceptSocket returns, it means there is a connection. Create
                    // an instance of the proxy server class and start a thread running.
                    HttpProxyServer proxy = new HttpProxyServer(socket);
                    Thread thrd = new Thread(new ThreadStart(proxy.Run));
                    thrd.Start();
                    // While the thread is running, the main program thread will loop around
                    // and listen for the next connection request.
                }
            }
            catch (IOException e)
            {
                Trace.WriteLine(e.Message);
            }

        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            DiagnosticMonitor.Start("DiagnosticsConnectionString");

            //DiagnosticMonitorConfiguration config = DiagnosticMonitor.GetDefaultInitialConfiguration();
            //config.Logs.ScheduledTransferPeriod = new System.TimeSpan(0, 0, 30);
            //DiagnosticMonitor.Start("AzureDiagnosticsConnectionString");

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            RoleEnvironment.Changing += RoleEnvironmentChanging;

            return base.OnStart();
        }

        private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            // If a configuration setting is changing
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
            {
                // Set e.Cancel to true to restart this role instance
                e.Cancel = true;
            }
        }
    }
}
