using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketEngine;    // for  BootstrapFactory 
using System.IO;

namespace MyRouteService
{
    public partial class RouteService : ServiceBase
    {
        private IBootstrap bootstrap;

        public RouteService()
        {
            InitializeComponent();
            bootstrap = BootstrapFactory.CreateBootstrap();
        }

        protected override void OnStart(string[] args)
        {
            string path = @"C:\liuyang\logs\log.txt";


           // IBootstrap bootstrap = BootstrapFactory.CreateBootstrap();
            if (!bootstrap.Initialize())
            {
                using (StreamWriter writer = File.AppendText(path))
                {
                    writer.WriteLine("init fail");
                    writer.Close();
                }
                return;
            }

            using (StreamWriter writer = File.AppendText(path))
            {
                writer.WriteLine("starting...");
                writer.Close();
            }

            var result = bootstrap.Start();
            foreach (var server in bootstrap.AppServers)
            {
                if (server.State == ServerState.Running)
                {
                    using (StreamWriter writer = File.AppendText(path))
                    {
                        writer.WriteLine("running...");
                        writer.Close();
                    }

                }
                else
                {

                    using (StreamWriter writer = File.AppendText(path))
                    {
                        writer.WriteLine("run fail");
                        writer.Close();
                    }
                }
            }

            switch (result)
            {
                case StartResult.Failed:
                    using (StreamWriter writer = File.AppendText(path))
                    {
                        writer.WriteLine("can not start service , pls check log");
                        writer.Close();
                    }
                    return;
                case StartResult.None:

                    using (StreamWriter writer = File.AppendText(path))
                    {
                        writer.WriteLine("no service setting");
                        writer.Close();
                    }
                    return;
                case StartResult.PartialSuccess:

                    using (StreamWriter writer = File.AppendText(path))
                    {
                        writer.WriteLine("part success");
                        writer.Close();
                    }
                    break;
                case StartResult.Success:
                    using (StreamWriter writer = File.AppendText(path))
                    {
                        writer.WriteLine("service already start");
                        writer.Close();
                    }
                    break;
            }

        }

        protected override void OnStop()
        {
            bootstrap.Stop();
            base.OnStop();
        }

        public void Start(string[] args)
        {
            OnStart(args);
        }
    }
}
