using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;
using System.Threading;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;


namespace MyRouteService.Command
{

    public class ROUTEREQUEST : CommandBase<TCPSocketSession, StringRequestInfo>
    {

        public override void ExecuteCommand(TCPSocketSession session, StringRequestInfo requestInfo)
        {
            
            if (requestInfo.Parameters.Count() != CommonTools.ROUTEREQUEST_PARACOUNT)
            {
                // if need send back  TODO       session.Send("The wrong format\r\n");
                session.AppServer.Logger.Error("CustomLog ROUTEREQUEST PARAMETER MUST BE 5 , now is :" + requestInfo.Key + @":" + requestInfo.Body);
                return;
            }

            CommandDetail cmdDetail = new CommandDetail();
            cmdDetail.requestID = Guid.NewGuid().ToString();
            cmdDetail.sessionIP = session.RemoteEndPoint.ToString();
            cmdDetail.sessionID = session.SessionID;
            cmdDetail.commandName = this.Name;
            cmdDetail.cmd_recv_time = DateTime.Now;
            cmdDetail.cmd_content = requestInfo.Key + @":" + requestInfo.Body;
            //now cmdDetail only left reply content and err reason is empty

            string strCallID = requestInfo.Parameters[0].ToString();

            //Console.WriteLine("callid {0} route request have recv at {1}", strCallID,DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"));

            ((TCPSocketServer)session.AppServer).Logger.Debug("callid " + strCallID + " route request have recv at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"));

            session.iTotalRecv++;

            #region SaveRequestIntoSQL

            // using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString.ToString()))
            //{
            //    SqlCommand sqlCmd = null;
            //    SqlCommand sqlCmdUpdate = null;
            //    DateTime dtNow = DateTime.Now;

            //    try
            //    {
            //        conn.Open();

            //        sqlCmdUpdate = new SqlCommand("insert into  Table_RequestReply ( Session_SN,Request_ID,Request_Content) values ( @SessionSN,@RequestID,@RequestBody) ", conn);

            //        sqlCmdUpdate.Parameters.AddWithValue("@SessionSN", session.SN);
            //        sqlCmdUpdate.Parameters.AddWithValue("@RequestID", requestID);
            //        sqlCmdUpdate.Parameters.AddWithValue("@RequestBody", requestInfo.Body);




            //        sqlCmdUpdate.CommandTimeout = 200;
            //        if (conn.State == ConnectionState.Closed)
            //            conn.Open();
            //        try
            //        {
            //            sqlCmdUpdate.ExecuteNonQuery();
            //        }
            //        catch (Exception aep)
            //        {
            //            // File.AppendAllText(strCurrentPath + @"\test.txt", "insert error " + aep.Message + "\r\n");
            //            return;
            //        }
            //    }
            //    catch (Exception cep)
            //    {
            //    }
            //}





            //using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString.ToString()))
            //{
            //    SqlCommand sqlCmd = null;
            //    SqlCommand sqlCmdUpdate = null;
            //    DateTime dtNow = DateTime.Now;

            //    try
            //    {
            //        conn.Open();

            //        sqlCmdUpdate = new SqlCommand("update  SessionTable set  Session_Total_Recv_Request = @SessionRecv, Session_Total_Handle_Request= @SessionHandled where Session_SN = @SessionID", conn);

            //        sqlCmdUpdate.Parameters.AddWithValue("@SessionID", session.SN);
            //        sqlCmdUpdate.Parameters.AddWithValue("@SessionRecv", session.iTotalRecv);
            //        sqlCmdUpdate.Parameters.AddWithValue("@SessionHandled", session.iTotalFinish);




            //        sqlCmdUpdate.CommandTimeout = 200;
            //        if (conn.State == ConnectionState.Closed)
            //            conn.Open();
            //        try
            //        {
            //            sqlCmdUpdate.ExecuteNonQuery();
            //        }
            //        catch (Exception aep)
            //        {
            //            // File.AppendAllText(strCurrentPath + @"\test.txt", "insert error " + aep.Message + "\r\n");
            //            return;
            //        }
            //    }
            //    catch (Exception cep)
            //    {
            //    }
            //}

            #endregion

            string sSendToMonitor  = "From " + requestInfo.Key + @":" + requestInfo.Body;

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

            string sReply = @"<reply>" + @"ROUTEREQUEST;" + strCallID + @",GetNapRouteIsOK" + @"</reply>";
            byte[] rv = Encoding.ASCII.GetBytes(sReply);

            try
            {
                //Console.WriteLine("quick reply " + sReply + " {0} times", session.iTotalRecv);
                cmdDetail.cmd_reply_time = DateTime.Now;
                session.Send(rv, 0, rv.Length);    // reply OK first
                sSendToMonitor = sSendToMonitor + "To " + "GetNapRouteIsOK";

                //throw  TODO  simulate send error  ; 
            }
            catch (Exception tc)           //for exampleTimeoutException
            {
                cmdDetail.cmd_reply_time = DateTime.Now;
                session.AppServer.Logger.Error("send ROUTEREQUEST ok back error");
                //Console.WriteLine("send ROUTEREQUEST ok back error");    //when become service , will print this line in output window
                cmdDetail.reply_content = sReply;

                cmdDetail.err_reason = "send ROUTEREQUEST ok back time out";
                //write log ， send what fail

                ((TCPSocketServer)session.AppServer).CommandDetailList.Enqueue(cmdDetail);

                sSendToMonitor = @"<reply>NORMALLOG@" + sSendToMonitor + @". error:send ok back time out" + @"</reply>";

                CommonTools.SendToEveryMonitor(sSendToMonitor, session);

                return;
            }




            //put all parameter into one class     2017-6-1  temp comment   old method

            //classParameterSessionAndRequestID cp1 = new classParameterSessionAndRequestID();
            //cp1.ts = session;
            //cp1.sToMonitor = sSendToMonitor;
            //cp1.cmdDetail = cmdDetail;

            //ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadWhichWillCallSQL), cp1);

            //put all parameter into one class     2017-6-1  temp comment


            return;

        }


        public void ThreadWhichWillCallSQL(Object o1)
        {
            ////Interlocked.Increment(ref CmdCount);

            //classParameterSessionAndRequestID typed = (classParameterSessionAndRequestID)o1;

            ////Interlocked.Decrement(ref ((TCPSocketServer)typed.ts.AppServer).iPendingThread);
            ////Console.WriteLine("still have {0} threads pending.", ((TCPSocketServer)typed.ts.AppServer).iPendingThread);

            //string sSQLRtnMsg = null;

            //#region callsql
            //using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString.ToString()))
            //{
            //    try
            //    {
            //        conn.Open();

            //        SqlCommand comm = conn.CreateCommand();
            //        comm.CommandText = "sp_Test_alwayRetTrue";
            //        comm.CommandType = System.Data.CommandType.StoredProcedure;
            //        comm.CommandTimeout = 300;


            //        comm.Parameters.Add(new SqlParameter()
            //        {
            //            ParameterName = "@I_CallID_A",
            //            SqlDbType = SqlDbType.Int,
            //            Value = typed.para1,
            //            Size = 4
            //        });

            //        comm.Parameters.Add(new SqlParameter()
            //        {
            //            ParameterName = "@O_ErrCode",
            //            SqlDbType = SqlDbType.Int,
            //            Direction = ParameterDirection.Output,
            //            Size = 4
            //        });

            //        comm.Parameters.Add(new SqlParameter()
            //        {
            //            ParameterName = "@O_Msg",
            //            SqlDbType = SqlDbType.VarChar,
            //            //Value =sSQLRtnMsg,
            //            Direction = ParameterDirection.Output,
            //            Size = 1000
            //        });

            //        comm.ExecuteNonQuery();

            //        sSQLRtnMsg = comm.Parameters["@O_Msg"].Value.ToString();
            //    }
            //    catch (Exception uep)
            //    {
            //        //区别 数据库下线还是存储过程出错  TODO 
            //        //记录 request and  出错原因
            //        Console.WriteLine("get NAP call SQL error");
            //        typed.cmdDetail.cmd_reply_time = DateTime.Now;
            //        typed.cmdDetail.reply_content = uep.Message;
            //        ((TCPSocketServer)typed.ts.AppServer).CommandDetailList.Enqueue(typed.cmdDetail);

            //        typed.sToMonitor = @"<reply>NORMALLOG@" + typed.sToMonitor + @". error:call sp wrong ,  reason is : " + uep.Message + @".</reply>";

            //        CommonTools.SendToEveryMonitor(typed.sToMonitor, typed.ts);

            //        return;
            //    }
            //}   //using

            //#endregion

            //string sReply = @"<reply>" + sSQLRtnMsg + @"</reply>";

            //byte[] rv = Encoding.ASCII.GetBytes(sReply);

            //try
            //{
            //    //if (typed.ts.Connected)
            //    //{
            //        typed.ts.Send(rv, 0, rv.Length);
            //        //Console.WriteLine(sReply + @"---------" + typed.ts.iTotalFinish.ToString());

            //        //typed.ts.SocketSession.Client.Send(rv);


            //    //}
            //    //typed.cmdDetail.reply_content = sReply;
            //    //typed.cmdDetail.cmd_reply_time = DateTime.Now;

            //    //((TCPSocketServer)typed.ts.AppServer).CommandDetailList.Enqueue(typed.cmdDetail);

            //    //var sessions = typed.ts.AppServer.GetSessions(s => s.bIfMonitorClient == true && s.iDebugLevel < 1);  // send to monitor who open debug mode
            //    //foreach (var s in sessions)
            //    //{
            //    //    string sRequest = @"<reply>NORMALLOG@" + typed.ts.RemoteEndPoint.Address.ToString() + @"----" + typed.sToMonitor + "To " + sSQLRtnMsg + @"</reply>";
            //    //    byte[] bRequest = Encoding.ASCII.GetBytes(sRequest);

            //    //    s.Send(bRequest, 0, bRequest.Length);
            //    //}

            //    //Thread.BeginCriticalRegion();
            //    //typed.ts.iTotalFinish++;          // total handle +1
            //    ////Console.WriteLine("total reply {0} " + typed.ts.iTotalFinish);
            //    //Thread.EndCriticalRegion();


            //}
            //catch (Exception tc)   //TimeoutException
            //{
            //    Console.WriteLine("there happen error " + tc.Message);
            //    return;
            //}

            //return;
        }


    }
}
