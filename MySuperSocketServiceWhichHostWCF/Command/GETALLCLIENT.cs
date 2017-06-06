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
    class clientState
    {
        public DateTime clientConnectTime { get; set; }
        public int clientRecv { get; set; }
        public int clienthandle { get; set; }
        public string clientIP { get; set; } 
    }

    public class GETALLCLIENT : CommandBase<TCPSocketSession, StringRequestInfo>
    {
        public override void ExecuteCommand(TCPSocketSession session, StringRequestInfo requestInfo)
        {
            List<clientState> clientList = new List<clientState>();
            foreach (var client in session.AppServer.GetAllSessions())
            {
                clientState cst = new clientState { clientConnectTime = client.ClientConnectTime, clientRecv = client.iTotalRecv,clienthandle=client.iTotalFinish,clientIP=client.ClientIP};
                clientList.Add(cst);
                
            }

           
            string sReply = JsonConvert.SerializeObject(clientList);
            byte[] bHead = Encoding.ASCII.GetBytes(@"<reply>");
            byte[] bTail = Encoding.ASCII.GetBytes(@"</reply>");
            byte[] bData = Encoding.ASCII.GetBytes(sReply);


            byte[] rv = new byte[bHead.Length + bTail.Length + bData.Length];
            System.Buffer.BlockCopy(bHead, 0, rv, 0, bHead.Length);
            System.Buffer.BlockCopy(bData, 0, rv, bHead.Length, bData.Length);
            System.Buffer.BlockCopy(bTail, 0, rv, bHead.Length + bData.Length, bTail.Length);





            var str = System.Text.Encoding.Default.GetString(rv);
            

            session.Send(rv, 0, rv.Length);    // reply OK first
            Console.WriteLine(str);

            //session.Send("success\r\n");

            
        }  //end of execute command 
    }
}
