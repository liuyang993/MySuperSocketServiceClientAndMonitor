using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.SocketEngine;    // for  BootstrapFactory 

namespace MyRouteService.Command
{
    public class START : CommandBase<TCPSocketSession, StringRequestInfo>
    {
        public override void ExecuteCommand(TCPSocketSession session, StringRequestInfo requestInfo)
        {

            IBootstrap bootstrap = BootstrapFactory.CreateBootstrap();
            if (!bootstrap.Initialize())
            {
                //SetConsoleColor(ConsoleColor.Red);
                Console.WriteLine("初始化失败");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("启动中...");

            var result = bootstrap.Start();
            Console.WriteLine("-------------------------------------------------------------------");
            foreach (var server in bootstrap.AppServers)
            {
                server.Stop();
            }

        }   //  end of start
    }
}
