using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;

namespace MyRouteService.Command
{

    public class CHECK : CommandBase<TCPSocketSession, StringRequestInfo>
    {
        public override void ExecuteCommand(TCPSocketSession session, StringRequestInfo requestInfo)
        {
            if (requestInfo.Parameters.Count() != 1)
            {
                session.Send("The wrong format\r\n");
            }
            else
            {

            }
        }  //end of execute command 
    }
}
