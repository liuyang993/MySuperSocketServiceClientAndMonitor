using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace MyRouteService
{
    public class TCPSocketSession : AppSession<TCPSocketSession>
    {
        /// <summary>
        /// 是否登录
        /// </summary>
        public bool isLogin { get; set; }

        /// <summary>
        /// 机器编码
        /// </summary>
        public string SN { get; set; }

        /// <summary>
        /// 收到的request
        /// </summary>
        public int iTotalRecv { get; set; }

        /// <summary>
        /// 完成的request
        /// </summary>
        public int iTotalFinish { get; set; }

        /// <summary>
        /// 建立连接的时间
        /// </summary>
        public DateTime ClientConnectTime { get; set; }


        //public DateTime ClientDisConnectTime { get; set; }


        /// <summary>
        /// 客户端IP
        /// </summary>        
        public string ClientIP { get; set; }

        /// <summary>
        /// 是不是管理工具
        /// </summary> 
        public bool bIfMonitorClient { get; set; }

        /// <summary>
        /// 输出log的级别
        /// </summary> 
        public int iDebugLevel { get; set; }

        //protected override void OnReceiveEnded()
        //{
        //    int i = 1;
        //   // RemoveStateFlag(SocketState.InReceiving);
        //}

        protected override void OnSessionStarted()
        {
            iTotalRecv = 0;
            iTotalFinish = 0;

            SN = Guid.NewGuid().ToString();
            ClientConnectTime = DateTime.Now;
            ClientIP = RemoteEndPoint.Address.ToString();
            bIfMonitorClient = false;
            iDebugLevel = 3;

            //this.Send("Welcome to SuperSocket WeChat Server\r\n");

            #region Increase Client Number Using SQL ,  now do not use this way 
            //using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString.ToString()))
            //{
            //    SqlCommand sqlCmd = null;
            //    SqlCommand sqlCmdUpdate = null;
            //    DateTime dtNow = DateTime.Now;
            //    string sClientIP=RemoteEndPoint.Address.ToString();

            //    try
            //    {
            //        conn.Open();

            //        sqlCmdUpdate = new SqlCommand("insert into  SessionTable (Session_SN,Session_Begin_Time,Session_Src_IP) values(@SessionID, @SessionStartTime,@SessionSrcIP)", conn);

            //        sqlCmdUpdate.Parameters.AddWithValue("@SessionID", SN);
            //        sqlCmdUpdate.Parameters.AddWithValue("@SessionStartTime", dtNow);
            //        sqlCmdUpdate.Parameters.AddWithValue("@SessionSrcIP", sClientIP);


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
        }

        protected override void OnInit()
        {
            //this.Charset = Encoding.GetEncoding("gb2312");
            base.OnInit();
        }

        protected override void HandleUnknownRequest(StringRequestInfo requestInfo)
        {
            //LogHelper.WriteLog("收到命令:" + requestInfo.Key.ToString());
            this.Send("不知道如何处理 " + requestInfo.Key.ToString() + " 命令\r\n");
        }


        /// <summary>
        /// 异常捕捉
        /// </summary>
        /// <param name="e"></param>
        protected override void HandleException(Exception e)
        {
            this.Send("\n\r异常信息：{0}", e.Message);
            //base.HandleException(e);
        }

        /// <summary>
        /// 连接关闭
        /// </summary>
        /// <param name="reason"></param>
        protected override void OnSessionClosed(CloseReason reason)
        {
            using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString.ToString()))
            {
                SqlCommand sqlCmd = null;
                SqlCommand sqlCmdUpdate = null;
                DateTime dtNow = DateTime.Now;

                try
                {
                    conn.Open();

                    sqlCmdUpdate = new SqlCommand("update  SessionTable set Session_End_Time =  @SessionEndTime,Session_Total_Recv_Request = @SessionRecv, Session_Total_Handle_Request= @SessionHandled where Session_SN = @SessionID", conn);

                    sqlCmdUpdate.Parameters.AddWithValue("@SessionEndTime", dtNow);
                    sqlCmdUpdate.Parameters.AddWithValue("@SessionID", SN);
                    sqlCmdUpdate.Parameters.AddWithValue("@SessionRecv", iTotalRecv);
                    sqlCmdUpdate.Parameters.AddWithValue("@SessionHandled", iTotalFinish);


                    sqlCmdUpdate.CommandTimeout = 200;
                    if (conn.State == ConnectionState.Closed)
                        conn.Open();
                    try
                    {
                        sqlCmdUpdate.ExecuteNonQuery();
                    }
                    catch (Exception aep)
                    {
                        // File.AppendAllText(strCurrentPath + @"\test.txt", "insert error " + aep.Message + "\r\n");
                        return;
                    }
                }
                catch (Exception cep)
                {
                }
            }




            base.OnSessionClosed(reason);

        }
    }   //  end of class


}
