using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;
using Newtonsoft.Json;

namespace MyRouteService.Command
{
    public class SETDEBUGLEVEL : CommandBase<TCPSocketSession, StringRequestInfo>
    {
        public override void ExecuteCommand(TCPSocketSession session, StringRequestInfo requestInfo)
        {
   
            if (requestInfo.Parameters.Count() != 2)
            {
                session.Send("The wrong format\r\n");
                return;
            }

            session.iDebugLevel = int.Parse(requestInfo.Parameters[0]);

            session.iTotalFinish++;
            //session.Send("success\r\n");
        }  //end of execute command 
    }
}
