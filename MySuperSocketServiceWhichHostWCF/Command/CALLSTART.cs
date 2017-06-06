using System;
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
    class classParameterSessionAndRequestID
    {
        public TCPSocketSession ts { get; set; }
        public string sToMonitor { get; set; }
        public CommandDetail cmdDetail { get; set; }
        public int para1 { get; set; }
    }

    public class CALLSTART : CommandBase<TCPSocketSession, StringRequestInfo>
    {
        public override void ExecuteCommand(TCPSocketSession session, StringRequestInfo requestInfo)
        {
            CommandDetail cmdDetail = new CommandDetail();
            cmdDetail.requestID = Guid.NewGuid().ToString();
            cmdDetail.sessionIP = session.RemoteEndPoint.ToString();
            cmdDetail.sessionID = session.SessionID;
            cmdDetail.commandName = this.Name;
            cmdDetail.cmd_recv_time = DateTime.Now;
            cmdDetail.cmd_content = requestInfo.Key + @":" + requestInfo.Body;
            //now cmdDetail only left reply content and err reason

            if (requestInfo.Parameters.Count() != 3)     //need 3 parameters
            {
                // if need send back  TODO
                //session.Send("The wrong format\r\n");
                session.AppServer.Logger.Error("CustomLog CALLSTART PARAMETER MUST BE 3 , now is :" + requestInfo.Key + @":" + requestInfo.Body);
                return;
            }

            string strCallID = requestInfo.Parameters[0].ToString();


            session.iTotalRecv++;

            string sSendToMonitor = null;
            sSendToMonitor = "From " + requestInfo.Key + @":" + requestInfo.Body;

            CacheItem ci = new CacheItem();
            ci.session = session;
            ci.commandKey = requestInfo.Key;
            ci.commandParameter = requestInfo.Body;
            ci.cDetail = cmdDetail;
            ci.sToMonitor = sSendToMonitor;

            if (!((TCPSocketServer)session.AppServer).cacheList.enQueue(ci))
            {
                Console.WriteLine("cache have full");
                return;
            }


            string sReply = @"<reply>" + @"CallStartOK;" + strCallID + @",CallStartIsOK" + @"</reply>";

            byte[] rv = Encoding.ASCII.GetBytes(sReply);

            try
            {
                Console.WriteLine("quick reply " + sReply + " {0} times", session.iTotalRecv);

                cmdDetail.cmd_reply_time = DateTime.Now;
                session.Send(rv, 0, rv.Length);    // reply OK first
                sSendToMonitor = sSendToMonitor + " To " + "CallStartOK";
            }
            catch (Exception tc)           //TimeoutException
            {
                cmdDetail.cmd_reply_time = DateTime.Now;
                Console.WriteLine("send CALLSTART ok back error");
                cmdDetail.reply_content = sReply;
                cmdDetail.err_reason = "send CALLSTART ok back time out";
                //write log ， send what fail

                ((TCPSocketServer)session.AppServer).CommandDetailList.Enqueue(cmdDetail);

                sSendToMonitor = @"<reply>NORMALLOG@" + sSendToMonitor + @". error:send ok back time out" + @"</reply>";

                CommonTools.SendToEveryMonitor(sSendToMonitor, session);

                return;

            }

            //put all parameter into one class
            //classParameterSessionAndRequestID cp1 = new classParameterSessionAndRequestID();
            //cp1.ts = session;
            //cp1.sToMonitor = sSendToMonitor;
            //cp1.cmdDetail = cmdDetail;
            //cp1.para1 = int.Parse(requestInfo.Parameters[0].ToString());

            //ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadWhichWillCallSQL), cp1);

        }  //end of execute command 



        public void ThreadWhichWillCallSQL(Object o1)  //static
        {

            classParameterSessionAndRequestID typed = (classParameterSessionAndRequestID)o1;
            string sSQLRtnMsg = null;

            #region callsql
            using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString.ToString()))
            {
                try
                {
                    conn.Open();

                    SqlCommand comm = conn.CreateCommand();
                    comm.CommandText = "sp_cmd_CALLSTART";
                    comm.CommandType = System.Data.CommandType.StoredProcedure;
                    comm.CommandTimeout = 300;


                    comm.Parameters.Add(new SqlParameter()
                    {
                        ParameterName = "@I_CallID_A",
                        SqlDbType = SqlDbType.Int,
                        Value = typed.para1,
                        Size = 4
                    });

                    comm.Parameters.Add(new SqlParameter()
                    {
                        ParameterName = "@O_ErrCode",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Output,
                        Size = 4
                    });

                    comm.Parameters.Add(new SqlParameter()
                    {
                        ParameterName = "@O_Msg",
                        SqlDbType = SqlDbType.VarChar,
                        //Value =sSQLRtnMsg,
                        Direction = ParameterDirection.Output,
                        Size = 1000
                    });

                    comm.ExecuteNonQuery();

                    sSQLRtnMsg = comm.Parameters["@O_Msg"].Value.ToString();
                }
                catch (Exception uep)
                {
                    //记录 request and  出错原因
                    Console.WriteLine("CALLSTART call SQL error");
                    typed.cmdDetail.cmd_reply_time = DateTime.Now;
                    typed.cmdDetail.reply_content = uep.Message;
                    ((TCPSocketServer)typed.ts.AppServer).CommandDetailList.Enqueue(typed.cmdDetail);

                    typed.sToMonitor = @"<reply>NORMALLOG@" + typed.sToMonitor + @". error:call sp wrong ,  reason is : " + uep.Message + @".</reply>";

                    CommonTools.SendToEveryMonitor(typed.sToMonitor, typed.ts);

                    return;
                }
            }   //using

            #endregion


            string sReply = @"<reply>" + sSQLRtnMsg + @"</reply>";

            byte[] rv = Encoding.ASCII.GetBytes(sReply);

            try
            {
                if (typed.ts.Connected)
                {
                    typed.ts.Send(rv, 0, rv.Length);   // reply OK first
                    Console.WriteLine(sReply + @"---------" + typed.ts.iTotalFinish.ToString());
                }

                typed.cmdDetail.reply_content = sReply;
                typed.cmdDetail.cmd_reply_time = DateTime.Now;

                ((TCPSocketServer)typed.ts.AppServer).CommandDetailList.Enqueue(typed.cmdDetail);

                string sRequest = @"<reply>NORMALLOG@" + typed.ts.RemoteEndPoint.Address.ToString() + @"----" + typed.sToMonitor + "To " + sSQLRtnMsg + @"</reply>";
                byte[] bRequest = Encoding.ASCII.GetBytes(sRequest);

                var sessions = typed.ts.AppServer.GetSessions(s => s.bIfMonitorClient == true && s.iDebugLevel < 1);
                foreach (var s in sessions)
                {
                    s.Send(bRequest, 0, bRequest.Length);
                }

                Thread.BeginCriticalRegion();
                typed.ts.iTotalFinish++;          // total handle +1
                Thread.EndCriticalRegion();


            }
            catch (Exception tc)   //TimeoutException
            {
                Console.WriteLine("there happen error");
                return;

            }


            return;
        }

    }
}
