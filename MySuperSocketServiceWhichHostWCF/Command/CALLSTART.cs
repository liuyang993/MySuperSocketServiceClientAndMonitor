﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;
using Newtonsoft.Json;
using System.Threading;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace MyRouteService.Command
{

    public class OUTGOINGTRYSUCCESS : CommandBase<TCPSocketSession, StringRequestInfo>
    {
        public override void ExecuteCommand(TCPSocketSession session, StringRequestInfo requestInfo)
        {

            if (requestInfo.Parameters.Count() != CommonTools.OUTGOINGTRYSUCCESS_PARACOUNT)     //need 3 parameters
            {
                session.AppServer.Logger.Error("CustomLog CALLSTART PARAMETER MUST BE 7 , now is :" + requestInfo.Key + @":" + requestInfo.Body);
                return;
            }

            CommandDetail cmdDetail = new CommandDetail();
            cmdDetail.requestID = Guid.NewGuid().ToString();
            cmdDetail.sessionIP = session.RemoteEndPoint.ToString();
            cmdDetail.sessionID = session.SessionID;
            cmdDetail.commandName = this.Name;
            cmdDetail.cmd_recv_time = DateTime.Now;
            cmdDetail.cmd_content = requestInfo.Key + @":" + requestInfo.Body;

            string strCallID = requestInfo.Parameters[0].ToString();
            string strNAPout = requestInfo.Parameters[3].ToString();

            string sSendToMonitor = "From " + requestInfo.Key + @":" + requestInfo.Body;

            //Console.WriteLine("callid {0} callstart have recv at {1}", strCallID, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"));

            ((TCPSocketServer)session.AppServer).Logger.Debug("callid " + strCallID + " callstart have recv at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"));

            #region new methoc use tcpserver cache 

            CacheItem ci = new CacheItem();
            ci.session = session;
            ci.commandKey = requestInfo.Key;
            ci.commandParameter = requestInfo.Body;
            ci.cDetail = cmdDetail;
            ci.sToMonitor = sSendToMonitor;

            if (!((TCPSocketServer)session.AppServer).cacheList.enQueue(ci))
            {
                Console.WriteLine("cache have full");
                //TODO  :  if queue is full , what about this request , return will lose it 
                return;
            }

            #endregion


            string sReply = @"<reply>" + @"OUTGOINGTRYSUCCESS;" + strCallID + @"," + strNAPout + @",OK" + @"</reply>";

            byte[] rv = Encoding.ASCII.GetBytes(sReply);

            try
            {
                //Console.WriteLine("quick reply " + sReply + " {0} times", session.iTotalRecv);
                cmdDetail.cmd_reply_time = DateTime.Now;
                session.Send(rv, 0, rv.Length);    // reply OK first
                sSendToMonitor = sSendToMonitor + "To " + "OUTGOINGTRYSUCCESSIsOK";

                //throw  TODO  simulate send error  ; 
            }
            catch (Exception tc)           //for exampleTimeoutException
            {
                cmdDetail.cmd_reply_time = DateTime.Now;
                session.AppServer.Logger.Error("send OUTGOINGTRYSUCCESS ok back error");
                //Console.WriteLine("send ROUTEREQUEST ok back error");    //when become service , will print this line in output window
                cmdDetail.reply_content = sReply;

                cmdDetail.err_reason = "send OUTGOINGTRYSUCCESS ok back time out";
                //write log ， send what fail

                ((TCPSocketServer)session.AppServer).CommandDetailList.Enqueue(cmdDetail);

                sSendToMonitor = @"<reply>NORMALLOG@" + sSendToMonitor + @". error:send ok back time out" + @"</reply>";

                CommonTools.SendToEveryMonitor(sSendToMonitor, session);

                return;
            }


            return;

        }  //end of execute command 



    }
}
