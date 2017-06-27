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



    public class availableRoutes
    {
        public int sequence { get; set; }
        public string NAPName { get; set; }
        public int vendorID { get; set; }
        public string SRC { get; set; }
        public string DST { get; set; }
        public float RateFee { get; set; }
        public float RateCost { get; set; }

        public int priority { get; set; }
        public int weight { get; set; }

        public string prefix { get; set; }
        public string prefixGrp { get; set; }

        public Guid TGID { get; set; }
        public Guid TID { get; set; }
        public Guid EntryDetailID {get;set;}

        public bool IfAlreadyTrying { get; set; }
    }

    public class OutgoingCallTringLists
    {
        public string CallID { get; set; }
        public int customID { get; set;}
        public Guid custAuthenID { get; set;}
        public string regularSRC { get; set;}
        public string regularDST { get; set;}

        public string NAPIN { get; set; }
        public string IPIN { get; set; }

        public bool IsFirstTry { get; set; }
        public bool IsNotLastTry { get; set; }

        public List<availableRoutes> routeList {get;set;}

    }


    public class TCPSocketServer : AppServer<TCPSocketSession>
    {
        public ConcurrentQueue<CommandDetail> CommandDetailList = null;    // ConcurrentQueue :  thread safe queue
        public int iTotalFinish = 0;

        List<OutgoingCallTringLists> loctl = new List<OutgoingCallTringLists>();


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

                        //OutgoingCallTringLists myObject = null;
                        //availableRoutes ar = null;

                        switch (typed.commandKey)
                        {
                            case "ROUTEREQUEST":
                                { 
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
                                    ParameterName = "@O_CustID",
                                    SqlDbType = SqlDbType.Int,
                                    //Value =sSQLRtnMsg,
                                    Direction = ParameterDirection.Output,
                                    Size = 4
                                });

                                comm.Parameters.Add(new SqlParameter()
                                {
                                    ParameterName = "@O_CustAuthID",
                                    SqlDbType = SqlDbType.UniqueIdentifier,
                                    //Value =sSQLRtnMsg,
                                    Direction = ParameterDirection.Output,
                                    Size = 40
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


                                string sql = CommonTools.CommandAsSql(comm);
                                typed.session.AppServer.Logger.Info(sql);



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
                                iSQLRtnCustID = int.Parse(comm.Parameters["@O_CustID"].Value.ToString());

                                OutgoingCallTringLists octl = new OutgoingCallTringLists();
                                octl.CallID = Paras[0];
                                octl.NAPIN = Paras[3];
                                octl.IPIN = Paras[4];
                                octl.customID = iSQLRtnCustID;
                                octl.custAuthenID = Guid.Parse(comm.Parameters["@O_CustAuthID"].Value.ToString());
                                octl.regularSRC = sSQLRtnSRC;
                                octl.regularDST = sSQLRtnDST;
                                octl.IsFirstTry = true;
                                octl.IsNotLastTry = true;

                                octl.routeList = new List<availableRoutes>();

                                sb.Append(@"ROUTEREQUEST;" + comm.Parameters["@I_Callid"].Value.ToString() + @"," + iSQLRtnCanAccept.ToString() + @"," + sSQLRtnReason + @",");


                                for (int iDT = 0; iDT < dt.Rows.Count; iDT++)
                                {
                                    sb.Append(dt.Rows[iDT]["NAP"].ToString() + @"," + dt.Rows[iDT]["SRC"].ToString() + @"," + dt.Rows[iDT]["DST"].ToString() + @"," + dt.Rows[iDT]["RateFee"].ToString() + @"," + dt.Rows[iDT]["RateCost"].ToString() + @",");

                                    availableRoutes AR = new availableRoutes();
                                    AR.sequence = int.Parse(dt.Rows[iDT]["Seq"].ToString());
                                    AR.NAPName = dt.Rows[iDT]["NAP"].ToString();
                                    AR.vendorID = int.Parse(dt.Rows[iDT]["VendorID"].ToString());
                                    AR.SRC = dt.Rows[iDT]["SRC"].ToString();
                                    AR.DST = dt.Rows[iDT]["DST"].ToString();
                                    AR.RateFee = float.Parse(dt.Rows[iDT]["RateFee"].ToString());
                                    AR.RateCost = float.Parse(dt.Rows[iDT]["RateCost"].ToString());
                                    AR.priority = int.Parse(dt.Rows[iDT]["Piority"].ToString());
                                    AR.weight = int.Parse(dt.Rows[iDT]["Weight"].ToString());

                                    AR.prefix = dt.Rows[iDT]["Prefix"].ToString();
                                    AR.prefixGrp = dt.Rows[iDT]["PrefixGroup"].ToString();

                                    AR.TGID = Guid.Parse(dt.Rows[iDT]["TGID"].ToString());
                                    AR.TID = Guid.Parse(dt.Rows[iDT]["TID"].ToString());
                                    AR.EntryDetailID = Guid.Parse(dt.Rows[iDT]["EntryDetailID"].ToString());
                                    AR.IfAlreadyTrying = false;

                                    octl.routeList.Add(AR);
                                }

                                ((TCPSocketServer)typed.session.AppServer).loctl.Add(octl);

                                sb = sb.Remove(sb.Length - 1, 1);

                        }

                                break;
                            case "OUTGOINGTRYFAIL":
                                {
                                    comm.CommandText = "sp_api_call_failed";
                                    comm.CommandType = System.Data.CommandType.StoredProcedure;
                                    comm.CommandTimeout = 300;

                                    // search in OutgoingCallTringLists
                                    //OutgoingCallTringLists myObject = ((TCPSocketServer)typed.session.AppServer).loctl.FirstOrDefault(o.CallID.Any(io => io.Id == 2));

                                    OutgoingCallTringLists myObject = null;
                                    availableRoutes ar = null;

                                    myObject = ((TCPSocketServer)typed.session.AppServer).loctl.FirstOrDefault(x => x.CallID == Paras[0]);

                                    ar = myObject.routeList.FirstOrDefault(x => x.NAPName == Paras[3]);


                                    if (ar != null)
                                    {

                                        Console.WriteLine("fail try nap {0}", Paras[3]);

                                        ar.IfAlreadyTrying = true;


                                        myObject.IsNotLastTry = false;
                                        foreach (availableRoutes ar1 in myObject.routeList)
                                        {
                                            if (ar1.IfAlreadyTrying == false)
                                                myObject.IsNotLastTry = true;

                                        }

                                        comm.Parameters.Add("@I_Callid", SqlDbType.NVarChar, 30);
                                        comm.Parameters["@I_Callid"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Callid"].Value = Paras[0];
                                        comm.Parameters.Add("@I_RegularSRC", SqlDbType.NVarChar, 30);
                                        comm.Parameters["@I_RegularSRC"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_RegularSRC"].Value = myObject.regularSRC;
                                        comm.Parameters.Add("@I_RegularDST", SqlDbType.NVarChar, 30);
                                        comm.Parameters["@I_RegularDST"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_RegularDST"].Value = myObject.regularDST;
                                        comm.Parameters.Add("@I_SRCNumOut", SqlDbType.NChar, 30);
                                        comm.Parameters["@I_SRCNumOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_SRCNumOut"].Value = Paras[1];
                                        comm.Parameters.Add("@I_DSTNumOut", SqlDbType.NChar, 30);
                                        comm.Parameters["@I_DSTNumOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_DSTNumOut"].Value = Paras[2];
                                        comm.Parameters.Add("@I_CustID", SqlDbType.Int, 4);
                                        comm.Parameters["@I_CustID"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_CustID"].Value = myObject.customID;
                                        comm.Parameters.Add("@I_AuthIDIn", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_AuthIDIn"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_AuthIDIn"].Value = myObject.custAuthenID;
                                        comm.Parameters.Add("@I_NAPIn", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_NAPIn"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_NAPIn"].Value = myObject.NAPIN;
                                        comm.Parameters.Add("@I_IPIn", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_IPIn"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_IPIn"].Value = myObject.IPIN;
                                        comm.Parameters.Add("@I_EntryDetailID", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_EntryDetailID"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_EntryDetailID"].Value = ar.EntryDetailID;
                                        comm.Parameters.Add("@I_ByPrefix", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_ByPrefix"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_ByPrefix"].Value = ar.prefix;
                                        comm.Parameters.Add("@I_ByPrefixGroup", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_ByPrefixGroup"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_ByPrefixGroup"].Value = ar.prefixGrp;
                                        comm.Parameters.Add("@I_VendorID", SqlDbType.Int, 4);
                                        comm.Parameters["@I_VendorID"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_VendorID"].Value = ar.vendorID;
                                        comm.Parameters.Add("@I_TGIDOut", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_TGIDOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_TGIDOut"].Value = ar.TGID;
                                        comm.Parameters.Add("@I_TIDOut", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_TIDOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_TIDOut"].Value = ar.TID;
                                        comm.Parameters.Add("@I_NAPOut", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_NAPOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_NAPOut"].Value = ar.NAPName;
                                        comm.Parameters.Add("@I_IPOut", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_IPOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_IPOut"].Value = Paras[4];
                                        comm.Parameters.Add("@I_SetupTime", SqlDbType.DateTime, 8);
                                        comm.Parameters["@I_SetupTime"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_SetupTime"].Value = Paras[5];
                                        comm.Parameters.Add("@I_Connecttime", SqlDbType.DateTime, 8);
                                        comm.Parameters["@I_Connecttime"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Connecttime"].Value = Paras[6];
                                        comm.Parameters.Add("@I_Disconnecttime", SqlDbType.DateTime, 8);
                                        comm.Parameters["@I_Disconnecttime"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Disconnecttime"].Value = Paras[7];


                                        comm.Parameters.Add("@I_FlagIsFirstTry", SqlDbType.Int, 4);
                                        comm.Parameters["@I_FlagIsFirstTry"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_FlagIsFirstTry"].Value = myObject.IsFirstTry ? 1 : 0;

                                        comm.Parameters.Add("@I_FlagNotLastTry", SqlDbType.Int, 4);
                                        comm.Parameters["@I_FlagNotLastTry"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_FlagNotLastTry"].Value = myObject.IsNotLastTry ? 1 : 0;



                                        comm.Parameters.Add("@I_FlagFailedButConnect", SqlDbType.Int, 4);
                                        comm.Parameters["@I_FlagFailedButConnect"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_FlagFailedButConnect"].Value = 0;
                                        comm.Parameters.Add("@I_FlagFailedButConnectOut", SqlDbType.Int, 4);
                                        comm.Parameters["@I_FlagFailedButConnectOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_FlagFailedButConnectOut"].Value = 0;
                                        comm.Parameters.Add("@I_Duration", SqlDbType.Int, 4);
                                        comm.Parameters["@I_Duration"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Duration"].Value = 1;
                                        comm.Parameters.Add("@I_Fee", SqlDbType.Decimal, 9);
                                        comm.Parameters["@I_Fee"].Precision = 18;
                                        comm.Parameters["@I_Fee"].Scale = 8;
                                        comm.Parameters["@I_Fee"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Fee"].Value = 0.01;
                                        comm.Parameters.Add("@I_Cost", SqlDbType.Decimal, 9);
                                        comm.Parameters["@I_Cost"].Precision = 18;
                                        comm.Parameters["@I_Cost"].Scale = 8;
                                        comm.Parameters["@I_Cost"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Cost"].Value = 0.009;
                                        comm.Parameters.Add("@O_ErrCode", SqlDbType.Int, 4);
                                        comm.Parameters["@O_ErrCode"].Direction = ParameterDirection.Output;
                                        comm.Parameters.Add("@O_Msg", SqlDbType.NVarChar, 200);
                                        comm.Parameters["@O_Msg"].Direction = ParameterDirection.Output;
                                        comm.Parameters.Add("@RETURN_VALUE", SqlDbType.Int, 4);
                                        comm.Parameters["@RETURN_VALUE"].Direction = ParameterDirection.ReturnValue;


                                        string sql = CommonTools.CommandAsSql(comm);
                                        typed.session.AppServer.Logger.Info(sql);

                                        comm.ExecuteNonQuery();

                                        myObject.IsFirstTry = false;

                                        int O_ErrCode = Convert.ToInt32(comm.Parameters["@O_ErrCode"].Value.ToString());
                                        string O_Msg = comm.Parameters["@O_Msg"].Value.ToString();

                                        //reader = comm.ExecuteReader();

                                    }
                                }

                                break;
                            
                            case "OUTGOINGTRYSUCCESS":
                                {
                                    OutgoingCallTringLists myObject = ((TCPSocketServer)typed.session.AppServer).loctl.FirstOrDefault(x => x.CallID == Paras[0]);

                                    availableRoutes ar = myObject.routeList.FirstOrDefault(x => x.NAPName == Paras[3]);


                                    if (ar != null)
                                    {

                                        Console.WriteLine("success try nap {0}", Paras[3]);

                                        ar.IfAlreadyTrying = true;

                                        comm.CommandText = "sp_api_acc_start";
                                        comm.CommandType = System.Data.CommandType.StoredProcedure;
                                        comm.CommandTimeout = 300;

                                        comm.Parameters.Add("@I_Callid", SqlDbType.NVarChar, 30);
                                        comm.Parameters["@I_Callid"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Callid"].Value = Paras[0];
                                        comm.Parameters.Add("@I_RegularSRC", SqlDbType.NVarChar, 30);
                                        comm.Parameters["@I_RegularSRC"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_RegularSRC"].Value = myObject.regularSRC;
                                        comm.Parameters.Add("@I_RegularDST", SqlDbType.NVarChar, 30);
                                        comm.Parameters["@I_RegularDST"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_RegularDST"].Value = myObject.regularDST;
                                        comm.Parameters.Add("@I_SRCNumOut", SqlDbType.NChar, 30);
                                        comm.Parameters["@I_SRCNumOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_SRCNumOut"].Value = Paras[1];
                                        comm.Parameters.Add("@I_DSTNumOut", SqlDbType.NChar, 30);
                                        comm.Parameters["@I_DSTNumOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_DSTNumOut"].Value = Paras[2];
                                        comm.Parameters.Add("@I_CustID", SqlDbType.Int, 4);
                                        comm.Parameters["@I_CustID"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_CustID"].Value = myObject.customID;
                                        comm.Parameters.Add("@I_AuthIDIn", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_AuthIDIn"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_AuthIDIn"].Value = myObject.custAuthenID;
                                        comm.Parameters.Add("@I_NAPIn", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_NAPIn"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_NAPIn"].Value = myObject.NAPIN;
                                        comm.Parameters.Add("@I_IPIn", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_IPIn"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_IPIn"].Value = myObject.IPIN;
                                        comm.Parameters.Add("@I_EntryDetailID", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_EntryDetailID"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_EntryDetailID"].Value = ar.EntryDetailID;
                                        comm.Parameters.Add("@I_ByPrefix", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_ByPrefix"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_ByPrefix"].Value = ar.prefix;
                                        comm.Parameters.Add("@I_ByPrefixGroup", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_ByPrefixGroup"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_ByPrefixGroup"].Value = ar.prefixGrp;
                                        comm.Parameters.Add("@I_VendorID", SqlDbType.Int, 4);
                                        comm.Parameters["@I_VendorID"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_VendorID"].Value = ar.vendorID;
                                        comm.Parameters.Add("@I_TGIDOut", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_TGIDOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_TGIDOut"].Value = ar.TGID;
                                        comm.Parameters.Add("@I_TIDOut", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_TIDOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_TIDOut"].Value = ar.TID;
                                        comm.Parameters.Add("@I_NAPOut", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_NAPOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_NAPOut"].Value = ar.NAPName;
                                        comm.Parameters.Add("@I_IPOut", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_IPOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_IPOut"].Value = Paras[4];
                                        comm.Parameters.Add("@I_SetupTime", SqlDbType.DateTime, 8);
                                        comm.Parameters["@I_SetupTime"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_SetupTime"].Value = Paras[5];
                                        comm.Parameters.Add("@I_Connecttime", SqlDbType.DateTime, 8);
                                        comm.Parameters["@I_Connecttime"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Connecttime"].Value = Paras[6];
                                        comm.Parameters.Add("@I_IsFirst", SqlDbType.Int, 4);
                                        comm.Parameters["@I_IsFirst"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_IsFirst"].Value = myObject.IsFirstTry ? 1 : 0;
                                        comm.Parameters.Add("@O_ErrCode", SqlDbType.Int, 4);
                                        comm.Parameters["@O_ErrCode"].Direction = ParameterDirection.Output;
                                        comm.Parameters.Add("@O_Msg", SqlDbType.NVarChar, 200);
                                        comm.Parameters["@O_Msg"].Direction = ParameterDirection.Output;

                                        string sql = CommonTools.CommandAsSql(comm);
                                        typed.session.AppServer.Logger.Info(sql);

                                        comm.ExecuteNonQuery();


                                        //2017-6-26   find return null

                                        //int O_ErrCode = Convert.ToInt32(comm.Parameters["@O_ErrCode"].Value.ToString());
                                        //string O_Msg = comm.Parameters["@O_Msg"].Value.ToString();
                                    }

                                }

                                break;
                            case "CALLSTOP":
                                {
                                    OutgoingCallTringLists myObject = ((TCPSocketServer)typed.session.AppServer).loctl.FirstOrDefault(x => x.CallID == Paras[0]);

                                    availableRoutes ar = myObject.routeList.FirstOrDefault(x => x.NAPName == Paras[3]);


                                    if (ar != null)
                                    {

                                        Console.WriteLine("call stop by nap {0}", Paras[3]);

                                        comm.CommandText = "sp_api_acc_stop";
                                        comm.CommandType = System.Data.CommandType.StoredProcedure;
                                        comm.CommandTimeout = 300;


                                        comm.Parameters.Add("@I_Callid", SqlDbType.NVarChar, 30);
                                        comm.Parameters["@I_Callid"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Callid"].Value = Paras[0];
                                        comm.Parameters.Add("@I_RegularSRC", SqlDbType.NVarChar, 30);
                                        comm.Parameters["@I_RegularSRC"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_RegularSRC"].Value = myObject.regularSRC;
                                        comm.Parameters.Add("@I_RegularDST", SqlDbType.NVarChar, 30);
                                        comm.Parameters["@I_RegularDST"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_RegularDST"].Value = myObject.regularDST;
                                        comm.Parameters.Add("@I_SRCNumOut", SqlDbType.NChar, 30);
                                        comm.Parameters["@I_SRCNumOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_SRCNumOut"].Value = Paras[1];
                                        comm.Parameters.Add("@I_DSTNumOut", SqlDbType.NChar, 30);
                                        comm.Parameters["@I_DSTNumOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_DSTNumOut"].Value = Paras[2];
                                        comm.Parameters.Add("@I_CustID", SqlDbType.Int, 4);
                                        comm.Parameters["@I_CustID"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_CustID"].Value = myObject.customID;
                                        comm.Parameters.Add("@I_AuthIDIn", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_AuthIDIn"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_AuthIDIn"].Value = myObject.custAuthenID;
                                        comm.Parameters.Add("@I_NAPIn", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_NAPIn"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_NAPIn"].Value = myObject.NAPIN;
                                        comm.Parameters.Add("@I_IPIn", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_IPIn"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_IPIn"].Value = myObject.IPIN;
                                        comm.Parameters.Add("@I_EntryDetailID", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_EntryDetailID"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_EntryDetailID"].Value = ar.EntryDetailID;
                                        comm.Parameters.Add("@I_ByPrefix", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_ByPrefix"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_ByPrefix"].Value = ar.prefix;
                                        comm.Parameters.Add("@I_ByPrefixGroup", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_ByPrefixGroup"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_ByPrefixGroup"].Value = ar.prefixGrp;
                                        comm.Parameters.Add("@I_VendorID", SqlDbType.Int, 4);
                                        comm.Parameters["@I_VendorID"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_VendorID"].Value = ar.vendorID;
                                        comm.Parameters.Add("@I_TGIDOut", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_TGIDOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_TGIDOut"].Value = ar.TGID;
                                        comm.Parameters.Add("@I_TIDOut", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_TIDOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_TIDOut"].Value = ar.TID;
                                        comm.Parameters.Add("@I_NAPOut", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_NAPOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_NAPOut"].Value = ar.NAPName;
                                        comm.Parameters.Add("@I_IPOut", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_IPOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_IPOut"].Value = Paras[4];
                                        comm.Parameters.Add("@I_SetupTime", SqlDbType.DateTime, 8);
                                        comm.Parameters["@I_SetupTime"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_SetupTime"].Value = Paras[5];
                                        comm.Parameters.Add("@I_Connecttime", SqlDbType.DateTime, 8);
                                        comm.Parameters["@I_Connecttime"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Connecttime"].Value = Paras[6];
                                        comm.Parameters.Add("@I_Disconnecttime", SqlDbType.DateTime, 8);
                                        comm.Parameters["@I_Disconnecttime"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Disconnecttime"].Value = Paras[7];
                                        comm.Parameters.Add("@I_Duration", SqlDbType.Int, 4);
                                        comm.Parameters["@I_Duration"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Duration"].Value = Paras[8];
                                        comm.Parameters.Add("@I_Fee", SqlDbType.Decimal, 9);
                                        comm.Parameters["@I_Fee"].Precision = 18;
                                        comm.Parameters["@I_Fee"].Scale = 8;
                                        comm.Parameters["@I_Fee"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Fee"].Value = 0.001;
                                        comm.Parameters.Add("@I_Cost", SqlDbType.Decimal, 9);
                                        comm.Parameters["@I_Cost"].Precision = 18;
                                        comm.Parameters["@I_Cost"].Scale = 8;
                                        comm.Parameters["@I_Cost"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Cost"].Value = 0.0009;
                                        comm.Parameters.Add("@I_FlagIsFirstTry", SqlDbType.Int, 4);
                                        comm.Parameters["@I_FlagIsFirstTry"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_FlagIsFirstTry"].Value = 0;
                                        comm.Parameters.Add("@O_ErrCode", SqlDbType.Int, 4);
                                        comm.Parameters["@O_ErrCode"].Direction = ParameterDirection.Output;
                                        comm.Parameters.Add("@O_Msg", SqlDbType.NVarChar, 200);
                                        comm.Parameters["@O_Msg"].Direction = ParameterDirection.Output;
                                        comm.Parameters.Add("@RETURN_VALUE", SqlDbType.Int, 4);
                                        comm.Parameters["@RETURN_VALUE"].Direction = ParameterDirection.ReturnValue;
                                        //if (conn.State == ConnectionState.Closed)
                                        //    conn.Open();


                                        string sql = CommonTools.CommandAsSql(comm);
                                        typed.session.AppServer.Logger.Info(sql);

                                        comm.ExecuteNonQuery();



                                        //2017-6-26   find return null

                                        //int O_ErrCode = Convert.ToInt32(comm.Parameters["@O_ErrCode"].Value.ToString());
                                        //string O_Msg = comm.Parameters["@O_Msg"].Value.ToString();
                                    }



                                }
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
                    //if (typed.session.Connected)
                    //{
                    typed.cDetail.reply_content = sReply;
                    typed.cDetail.cmd_reply_time = DateTime.Now;


                    typed.session.Send(rv, 0, rv.Length);
                    //Console.WriteLine(sReply + @"---------" + ts.iTotalFinish.ToString());
                    //ts.iTotalFinish++;

                    //}

                    ts.CommandDetailList.Enqueue(typed.cDetail);

                    //var sessions = ts.GetSessions(s => s.bIfMonitorClient == true && s.iDebugLevel < 1);  // send to monitor who open debug mode
                    //foreach (var s in sessions)
                    //{
                    //    string sRequest = @"<reply>NORMALLOG;" + typed.session.RemoteEndPoint.Address.ToString() + @"----" + typed.sToMonitor + "To " + sSQLRtnReason + @"</reply>";
                    //    byte[] bRequest = Encoding.ASCII.GetBytes(sRequest);

                    //    s.Send(bRequest, 0, bRequest.Length);
                    //}


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
            _timer = new System.Threading.Timer(new TimerCallback(JobCallBack), null, 0, 5000);

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
