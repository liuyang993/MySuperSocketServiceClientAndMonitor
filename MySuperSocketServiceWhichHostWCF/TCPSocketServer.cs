using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using System.Threading;

using System.Data;
using System.Data.SqlClient;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketEngine;    // for  BootstrapFactory 
using System.Net.Sockets;

using System.Collections.Concurrent;

namespace MyRouteService
{
    public class CommandDetail
    {
        public string requestID { get; set; }
        public string sessionIP { get; set; }
        public string sessionID { get; set; }
        public string commandName { get; set; }
        public DateTime cmd_recv_time { get; set; }
        public DateTime cmd_reply_time { get; set; }
        public string cmd_content { get; set; }
        public string reply_content { get; set; }
        public string err_reason { get; set; }
    }

    //class of cache list item 
    public class CacheItem
    {
        public TCPSocketSession session { get; set; }
        public string commandKey { get; set; }
        public string commandParameter { get; set; }
        public CommandDetail cDetail { get; set; }
        public string sToMonitor { get; set; }
    }


    public class TCPSocketServer : AppServer<TCPSocketSession>
    {
        public ConcurrentQueue<CommandDetail> CommandDetailList = null;    // ConcurrentQueue :  thread safe queue
        public int iTotalFinish = 0;

        public class CacheQueueList                        // why exist this class ?  because alway stick to this word "Cache" ,  original deal with Command in TCPSession
        {
            public ConcurrentQueue<CacheItem> CachelList { get; set; }
            public TCPSocketServer ts;

            public bool enQueue(CacheItem ci)     // ci combine by TCPSession , and use it as parameter call TCPServer.enQueue
            {
                if (CachelList.Count > CommonTools.MAXCACHENUMBER)   // 10000
                    return false;     // no longer enqueue
                CachelList.Enqueue(ci);
                ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadWhichWillCallSQL), ci);
                return true;
            }
            public void ThreadWhichWillCallSQL(Object o1)
            {
                CacheItem typed = (CacheItem)o1;

                //Interlocked.Decrement(ref ((TCPSocketServer)typed.ts.AppServer).iPendingThread);
                //Console.WriteLine("still have {0} threads pending.", ((TCPSocketServer)typed.ts.AppServer).iPendingThread);

                string sSQLRtnReason = null;
                int iSQLRtnCanAccept;
                string sSQLRtnSRC = null;
                string sSQLRtnDST = null;
                int iSQLRtnCustID;

                StringBuilder sb = new StringBuilder();

                #region callsql
                using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString.ToString()))   // every command open a new connection , is it ok ?
                {
                    try
                    {
                        conn.Open();
                        SqlCommand comm = conn.CreateCommand();
                        SqlDataReader reader = null;
                        DataTable dt = new DataTable();
                        List<string> Paras = typed.commandParameter.Split(',').ToList<string>();

                        switch (typed.commandKey)
                        {
                            case "ROUTEREQUEST":
                                comm.CommandText = "sp_api_route_request";
                                comm.CommandType = System.Data.CommandType.StoredProcedure;
                                comm.CommandTimeout = 300;

                                comm.Parameters.Add(new SqlParameter()
                                {
                                    ParameterName = "@I_Callid",
                                    SqlDbType = SqlDbType.VarChar,
                                    Value = Paras[0],
                                    Size = 50
                                });

                                comm.Parameters.Add(new SqlParameter()
                                {
                                    ParameterName = "@I_SRC",
                                    SqlDbType = SqlDbType.VarChar,
                                    Value = Paras[1],
                                    Size = 50
                                });

                                comm.Parameters.Add(new SqlParameter()
                                {
                                    ParameterName = "@I_DST",
                                    SqlDbType = SqlDbType.VarChar,
                                    Value = Paras[2],
                                    Size = 50
                                });

                                comm.Parameters.Add(new SqlParameter()
                                {
                                    ParameterName = "@I_NAP",
                                    SqlDbType = SqlDbType.VarChar,
                                    Value = Paras[3],
                                    Size = 50
                                });

                                comm.Parameters.Add(new SqlParameter()
                                {
                                    ParameterName = "@I_IP",
                                    SqlDbType = SqlDbType.VarChar,
                                    Value = Paras[4],
                                    Size = 50
                                });


                                comm.Parameters.Add(new SqlParameter()
                                {
                                    ParameterName = "@O_CanAccept",
                                    SqlDbType = SqlDbType.Int,
                                    Direction = ParameterDirection.Output,
                                    Size = 4
                                });

                                comm.Parameters.Add(new SqlParameter()
                                {
                                    ParameterName = "@O_Reason",
                                    SqlDbType = SqlDbType.VarChar,
                                    //Value =sSQLRtnMsg,
                                    Direction = ParameterDirection.Output,
                                    Size = 50
                                });

                                comm.Parameters.Add(new SqlParameter()
                                {
                                    ParameterName = "@I_CustID",
                                    SqlDbType = SqlDbType.Int,
                                    //Value =sSQLRtnMsg,
                                    Direction = ParameterDirection.Output,
                                    Size = 4
                                });

                                comm.Parameters.Add(new SqlParameter()
                                {
                                    ParameterName = "@O_RegularSRC",
                                    SqlDbType = SqlDbType.VarChar,
                                    //Value =sSQLRtnMsg,
                                    Direction = ParameterDirection.Output,
                                    Size = 30
                                });

                                comm.Parameters.Add(new SqlParameter()
                                {
                                    ParameterName = "@O_RegularDST",
                                    SqlDbType = SqlDbType.VarChar,
                                    //Value =sSQLRtnMsg,
                                    Direction = ParameterDirection.Output,
                                    Size = 30
                                });

                                reader = comm.ExecuteReader();

                                if (reader != null)
                                {
                                    dt.Load(reader);
                                }

                                comm.Cancel();
                                reader.Close();

                                iSQLRtnCanAccept = int.Parse(comm.Parameters["@O_CanAccept"].Value.ToString());
                                sSQLRtnReason = comm.Parameters["@O_Reason"].Value.ToString();
                                sSQLRtnSRC = comm.Parameters["@O_RegularSRC"].Value.ToString();
                                sSQLRtnDST = comm.Parameters["@O_RegularDST"].Value.ToString();
                                iSQLRtnCustID = int.Parse(comm.Parameters["@I_CustID"].Value.ToString());


                                sb.Append(@"ROUTEREQUEST;" + comm.Parameters["@I_Callid"].Value.ToString() + @"," + iSQLRtnCanAccept.ToString() + @"," + sSQLRtnReason + @",");

                                for (int iDT = 0; iDT < dt.Rows.Count; iDT++)
                                {
                                    sb.Append(dt.Rows[iDT]["NAP"].ToString() + @"," + dt.Rows[iDT]["SRC"].ToString() + @"," + dt.Rows[iDT]["DST"].ToString() + @"," + dt.Rows[iDT]["RateFee"].ToString() + @"," + dt.Rows[iDT]["RateCost"].ToString()  + @",");
                                }

                                sb = sb.Remove(sb.Length - 1, 1);

                                break;
                            case "CALLSTART":
                                comm.CommandText = "sp_cmd_CALLSTART";
                                comm.CommandType = System.Data.CommandType.StoredProcedure;
                                comm.CommandTimeout = 300;

                                comm.Parameters.Add(new SqlParameter()
                                {
                                    ParameterName = "@I_CallID_A",
                                    SqlDbType = SqlDbType.Int,
                                    Value = 1,
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
                                break;
                            default:
                                break;
                        }
                     
                    }
                    catch (Exception uep)
                    {
                        //TODO record request and error reason
                        Console.WriteLine("call SQL error");
                        typed.cDetail.cmd_reply_time = DateTime.Now;
                        typed.cDetail.reply_content = uep.Message;
                        ts.CommandDetailList.Enqueue(typed.cDetail);

                        typed.sToMonitor = @"<reply>NORMALLOG@" + typed.sToMonitor + @". error:call sp wrong ,  reason is : " + uep.Message + @".</reply>";

                        CommonTools.SendToEveryMonitor(typed.sToMonitor, typed.session);

                        return;
                    }
                }   //using

                #endregion


                string sReply = @"<reply>" + sb.ToString() + @"</reply>";

                byte[] rv = Encoding.ASCII.GetBytes(sReply);

                try
                {
                    if (typed.session.Connected)
                    {
                        typed.session.Send(rv, 0, rv.Length);
                        Console.WriteLine(sReply + @"---------" + ts.iTotalFinish.ToString());
                        ts.iTotalFinish++;

                    }
                    typed.cDetail.reply_content = sReply;
                    typed.cDetail.cmd_reply_time = DateTime.Now;

                    ts.CommandDetailList.Enqueue(typed.cDetail);

                    var sessions = ts.GetSessions(s => s.bIfMonitorClient == true && s.iDebugLevel < 1);  // send to monitor who open debug mode
                    foreach (var s in sessions)
                    {
                        string sRequest = @"<reply>NORMALLOG;" + typed.session.RemoteEndPoint.Address.ToString() + @"----" + typed.sToMonitor + "To " + sSQLRtnReason + @"</reply>";
                        byte[] bRequest = Encoding.ASCII.GetBytes(sRequest);

                        s.Send(bRequest, 0, bRequest.Length);
                    }

                    //Thread.BeginCriticalRegion();
                    //typed.ts.iTotalFinish++;          // total handle +1
                    //Console.WriteLine("total reply {0} " + typed.ts.iTotalFinish);
                    //Thread.EndCriticalRegion();


                }
                catch (Exception tc)   //TimeoutException
                {
                    Console.WriteLine("there happen error " + tc.Message);
                    return;
                }

                CacheItem ciOut;
                CachelList.TryDequeue(out ciOut);            // CacheList must be enlarge , hole additional out parameters , and dequeue when the call is terminate 

                return;

            }
        }    //  end of nested class 


        public CacheQueueList cacheList = null;



        public int iPendingThread = 0;

        private System.Threading.Timer _timer;

        public TCPSocketServer()
            : base(new DefaultReceiveFilterFactory<FixedBeginEndFilter, StringRequestInfo>())
        //: base(new DefaultReceiveFilterFactory<FixedBeginEndFilter, StringRequestInfo>())
        //: base(new CommandLineReceiveFilterFactory(Encoding.Default, new BasicRequestInfoParser(":", ",")))
        {
            CommandDetailList = new ConcurrentQueue<CommandDetail>();
            cacheList = new CacheQueueList();
            cacheList.CachelList = new ConcurrentQueue<CacheItem>();
            cacheList.ts = this;


            // timer to record income and outgoing command into DB
            //_timer = new System.Threading.Timer(new TimerCallback(JobCallBack), null, 0, 5000);

        }

        System.Threading.Thread th = null;

        private void JobCallBack(object state)
        {
            // stop timer 
            //if (_timer != null)
            //{
            //    _timer.Change(Timeout.Infinite, Timeout.Infinite);
            //}
            int iEffect = 0;

            if (CommandDetailList.Count > 100)
            {
                //semaphore.WaitOne();

                // CommandDetailList.CollectionChanged -= cmd_CollectionChanged;
                using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString.ToString()))
                {
                    try
                    {
                        conn.Open();

                        SqlCommand comm = conn.CreateCommand();
                        //comm.CommandText = "sp_app_RatePlanDetail_Add_1114";
                        comm.CommandText = "sp_InsertRequestReply";
                        comm.CommandType = System.Data.CommandType.StoredProcedure;
                        comm.CommandTimeout = 300;


                        comm.Parameters.Add(new SqlParameter()
                        {
                            ParameterName = "@TVP",
                            SqlDbType = SqlDbType.Structured,
                            Value = GetDataTableParam(CommandDetailList)
                        });

                        comm.Parameters.Add(new SqlParameter()
                        {
                            ParameterName = "@result",
                            SqlDbType = SqlDbType.Int,
                            Direction = ParameterDirection.Output,
                            Size = 4
                        });

                        comm.Parameters.Add(new SqlParameter()
                        {
                            ParameterName = "@errormsg",
                            SqlDbType = SqlDbType.VarChar,
                            Direction = ParameterDirection.Output,
                            Size = 1000
                        });

                        iEffect = comm.ExecuteNonQuery();
                        Console.WriteLine("this time total insert {0} rows", iEffect);
                    }
                    catch (Exception uep)
                    {
                        //Logger.Error(uep.Message);
                        //return;
                    }
                }   //using

                Remove(CommandDetailList, iEffect);

                //清空
                // CommandDetailList.Clear();


            }   // if > 100
            else
            {
                //Console.WriteLine("remain {0} rows", CommandDetailList.Count);

            }
            //if (_timer != null)
            //{
            //    _timer.Change(0, 10000);
            //}
        }


        private void Remove(ConcurrentQueue<CommandDetail> q, int count)
        {
            CommandDetail commandDetail;
            Enumerable.Range(1, count).ToList().ForEach(n => q.TryDequeue(out commandDetail));
        }

        public void cmd_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // //Debug.WriteLine("Change type: " + e.Action);
            // //if (e.NewItems != null)
            // //{
            // //    Debug.WriteLine("Items added: ");
            // //    foreach (var item in e.NewItems)
            // //    {
            // //        Debug.WriteLine(item);
            // //    }
            // //}

            // //if (e.OldItems != null)
            // //{
            // //    Debug.WriteLine("Items removed: ");
            // //    foreach (var item in e.OldItems)
            // //    {
            // //        Debug.WriteLine(item);
            // //    }
            // //}


            //ObservableCollection<CommandDetail> obsSender = sender as ObservableCollection<CommandDetail>;

            //if (obsSender.Count > 100)
            //{
            //    //semaphore.WaitOne();

            //   // CommandDetailList.CollectionChanged -= cmd_CollectionChanged;
            //    using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString.ToString()))
            //    {
            //        try
            //        {
            //            conn.Open();

            //            SqlCommand comm = conn.CreateCommand();
            //            //comm.CommandText = "sp_app_RatePlanDetail_Add_1114";
            //            comm.CommandText = "sp_InsertRequestReply";
            //            comm.CommandType = System.Data.CommandType.StoredProcedure;
            //            comm.CommandTimeout = 300;


            //            comm.Parameters.Add(new SqlParameter()
            //            {
            //                ParameterName = "@TVP",
            //                SqlDbType = SqlDbType.Structured,
            //                //Value = GetDataTableParam(obsSender)
            //            });

            //            comm.Parameters.Add(new SqlParameter()
            //            {
            //                ParameterName = "@result",
            //                SqlDbType = SqlDbType.Int,
            //                Direction = ParameterDirection.Output,
            //                Size = 4
            //            });

            //            comm.Parameters.Add(new SqlParameter()
            //            {
            //                ParameterName = "@errormsg",
            //                SqlDbType = SqlDbType.VarChar,
            //                Direction = ParameterDirection.Output,
            //                Size = 1000
            //            });

            //            comm.ExecuteNonQuery();
            //        }
            //        catch (Exception uep)
            //        {
            //           // semaphore.Release(1);
            //            return;
            //        }
            //    }   //using

            //    //清空
            //    obsSender.Clear();
            //    //CommandDetailList.CollectionChanged += cmd_CollectionChanged;
            //    //semaphore.Release(1);
            //}


        }

        public DataTable GetDataTableParam(IEnumerable<CommandDetail> People)
        {
            //define the table and rows (the rows match those in the TVP)
            DataTable peopleTable = new DataTable();
            peopleTable.Columns.Add("Request_ID", typeof(string));

            peopleTable.Columns.Add("Session_IP", typeof(string));
            peopleTable.Columns.Add("Session_ID", typeof(string));
            peopleTable.Columns.Add("CommandName", typeof(string));

            peopleTable.Columns.Add("Request_Recv_Time", typeof(DateTime));
            peopleTable.Columns.Add("Request_reply_Time", typeof(DateTime));

            peopleTable.Columns.Add("Request_Content", typeof(string));
            peopleTable.Columns.Add("Reply_Content", typeof(string));
            peopleTable.Columns.Add("Err_Reason", typeof(string));


            foreach (CommandDetail p in People)
            {
                // add a row for each person
                DataRow row = peopleTable.NewRow();
                //row["clsCarrierID"] = p.clsCarrierID;
                //row["clsis_supplier"] = p.clsis_supplier;
                row["Request_ID"] = p.requestID;

                row["Session_IP"] = p.sessionIP;
                row["Session_ID"] = p.sessionID;
                row["CommandName"] = p.commandName;

                row["Request_Recv_Time"] = p.cmd_recv_time;
                row["Request_reply_Time"] = p.cmd_reply_time;
                row["Request_Content"] = p.cmd_content;
                row["Reply_Content"] = p.reply_content;
                row["Err_Reason"] = p.err_reason;

                peopleTable.Rows.Add(row);
            }
            return peopleTable;
        }

        protected void appServer_NewRequestReceived(TCPSocketSession session, StringRequestInfo requestInfo)
        {
            switch (requestInfo.Key.ToUpper())
            {
                case ("ECHO"):
                    break;

                case ("ADD"):
                    session.Send(requestInfo.Parameters.Select(p => Convert.ToInt32(p)).Sum().ToString());
                    break;

                case ("MULT"):

                    var result = 1;

                    foreach (var factor in requestInfo.Parameters.Select(p => Convert.ToInt32(p)))
                    {
                        result *= factor;
                    }

                    session.Send(result.ToString());
                    break;
            }
        }

        protected override bool Setup(IRootConfig rootConfig, IServerConfig config)
        {
            return base.Setup(rootConfig, config);

        }

        protected override void OnStarted()
        {
            //LogHelper.WriteLog("WeChat服务启动");
            //th = new System.Threading.Thread(new ThreadSendMsg().DoWork);
            //th.IsBackground = true;
            //th.Start();

            //加这句就不再经过 command ,不加由command 接管
            //this.NewRequestReceived += new RequestHandler<TCPSocketSession, StringRequestInfo>(appServer_NewRequestReceived);
            //ThreadPool.SetMinThreads(10, 8);
            //ThreadPool.SetMaxThreads(30, 8);


            //Logger.Error("Unknow request");
            //throw new Exception(string.Format("Unknow request has overheated!"));

            base.OnStarted();

            //Thread tParm = new Thread(threadFunc);
            //tParm.Start(CommandDetailList);


        }

        public void threadFunc(object arg)
        {
        }
        protected override void OnStopped()
        {
            //LogHelper.WriteLog("WeChat服务停止");
            //关闭线程
            if (th != null)
            {
                if (th.ThreadState != System.Threading.ThreadState.Stopped)
                    th.Abort();
            }
            base.OnStopped();
        }

        /// <summary>
        /// 新的连接
        /// </summary>
        /// <param name="session"></param>
        protected override void OnNewSessionConnected(TCPSocketSession session)
        {
            int i = this.SessionCount;



            //LogHelper.WriteLog("WeChat服务新加入的连接:" + session.LocalEndPoint.Address.ToString());
            base.OnNewSessionConnected(session);
        }



    } //  end of class
}
