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

        //public bool IfAlreadyTrying { get; set; }

        public bool IsFirstTry { get; set; }
        //public bool IsNotLastTry { get; set; }

        public bool IfAlreadyTry { get; set; }
        public bool IfAlreadySend { get; set; }
        public bool IfSuccess { get; set; }
        public DateTime SuccessTime { get; set; }

        public string IPOUT { get; set; }
        public DateTime setuptime { get; set; }
        public DateTime connecttime { get; set; }
        public DateTime disconnecttime { get; set; }
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

        //public bool IsFirstTry { get; set; }
        //public bool IsNotLastTry { get; set; }

        public bool IfTryingAny { get; set; }
        public DateTime constructTime { get; set; }
        public DateTime lastOperTime { get; set; }
        public string lastOper { get; set; }


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

                string sql = string.Empty;

                StringBuilder sb = new StringBuilder();
                List<string> Paras = typed.commandParameter.Split(',').ToList<string>();

                #region callsql
                try
                {

                    using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString.ToString()))   // every command open a new connection , is it ok ?
                    {
                        conn.Open();
                        SqlCommand comm = conn.CreateCommand();
                        SqlDataReader reader = null;
                        DataTable dt = new DataTable();
                        

                        //OutgoingCallTringLists myObject = null;
                        //availableRoutes ar = null;

                        switch (typed.commandKey)
                        {
                            case "ROUTEREQUEST":
                                {
                                    comm.CommandText = "sp_api_route_request";
                                    comm.CommandType = System.Data.CommandType.StoredProcedure;
                                    comm.CommandTimeout = 300;

                                    #region temp comment
                                    //comm.Parameters.Add(new SqlParameter()
                                    //{
                                    //    ParameterName = "@I_Callid",
                                    //    SqlDbType = SqlDbType.VarChar,
                                    //    Value = Paras[0],
                                    //    Size = 50
                                    //});

                                    //comm.Parameters.Add(new SqlParameter()
                                    //{
                                    //    ParameterName = "@I_SRC",
                                    //    SqlDbType = SqlDbType.VarChar,
                                    //    Value = Paras[1],
                                    //    Size = 50
                                    //});

                                    //comm.Parameters.Add(new SqlParameter()
                                    //{
                                    //    ParameterName = "@I_DST",
                                    //    SqlDbType = SqlDbType.VarChar,
                                    //    Value = Paras[2],
                                    //    Size = 50
                                    //});

                                    //comm.Parameters.Add(new SqlParameter()
                                    //{
                                    //    ParameterName = "@I_NAP",
                                    //    SqlDbType = SqlDbType.VarChar,
                                    //    Value = Paras[3],
                                    //    Size = 50
                                    //});

                                    //comm.Parameters.Add(new SqlParameter()
                                    //{
                                    //    ParameterName = "@I_IP",
                                    //    SqlDbType = SqlDbType.VarChar,
                                    //    Value = Paras[4],
                                    //    Size = 50
                                    //});


                                    //comm.Parameters.Add(new SqlParameter()
                                    //{
                                    //    ParameterName = "@O_CanAccept",
                                    //    SqlDbType = SqlDbType.Int,
                                    //    Direction = ParameterDirection.Output,
                                    //    Size = 4
                                    //});

                                    //comm.Parameters.Add(new SqlParameter()
                                    //{
                                    //    ParameterName = "@O_Reason",
                                    //    SqlDbType = SqlDbType.VarChar,
                                    //    //Value =sSQLRtnMsg,
                                    //    Direction = ParameterDirection.Output,
                                    //    Size = 50
                                    //});

                                    //comm.Parameters.Add(new SqlParameter()
                                    //{
                                    //    ParameterName = "@O_CustID",
                                    //    SqlDbType = SqlDbType.Int,
                                    //    //Value =sSQLRtnMsg,
                                    //    Direction = ParameterDirection.Output,
                                    //    Size = 4
                                    //});

                                    //comm.Parameters.Add(new SqlParameter()
                                    //{
                                    //    ParameterName = "@O_CustAuthID",
                                    //    SqlDbType = SqlDbType.UniqueIdentifier,
                                    //    //Value =sSQLRtnMsg,
                                    //    Direction = ParameterDirection.Output,
                                    //    Size = 40
                                    //});

                                    //comm.Parameters.Add(new SqlParameter()
                                    //{
                                    //    ParameterName = "@O_RegularSRC",
                                    //    SqlDbType = SqlDbType.VarChar,
                                    //    //Value =sSQLRtnMsg,
                                    //    Direction = ParameterDirection.Output,
                                    //    Size = 30
                                    //});

                                    //comm.Parameters.Add(new SqlParameter()
                                    //{
                                    //    ParameterName = "@O_RegularDST",
                                    //    SqlDbType = SqlDbType.VarChar,
                                    //    //Value =sSQLRtnMsg,
                                    //    Direction = ParameterDirection.Output,
                                    //    Size = 30
                                    //});

                                    #endregion

                                    comm.Parameters.Add("@I_Callid", SqlDbType.NVarChar, 30);
                                    comm.Parameters["@I_Callid"].Direction = ParameterDirection.Input;
                                    comm.Parameters["@I_Callid"].Value = Paras[0];
                                    comm.Parameters.Add("@I_SRC", SqlDbType.NVarChar, 30);
                                    comm.Parameters["@I_SRC"].Direction = ParameterDirection.Input;
                                    comm.Parameters["@I_SRC"].Value = Paras[1];
                                    comm.Parameters.Add("@I_DST", SqlDbType.NVarChar, 30);
                                    comm.Parameters["@I_DST"].Direction = ParameterDirection.Input;
                                    comm.Parameters["@I_DST"].Value = Paras[2];
                                    comm.Parameters.Add("@I_NAP", SqlDbType.NVarChar, 50);
                                    comm.Parameters["@I_NAP"].Direction = ParameterDirection.Input;
                                    comm.Parameters["@I_NAP"].Value = Paras[3];
                                    comm.Parameters.Add("@I_IP", SqlDbType.NVarChar, 50);
                                    comm.Parameters["@I_IP"].Direction = ParameterDirection.Input;
                                    comm.Parameters["@I_IP"].Value = Paras[4];
                                    comm.Parameters.Add("@O_CustID", SqlDbType.Int, 4);
                                    comm.Parameters["@O_CustID"].Direction = ParameterDirection.Output;
                                    comm.Parameters.Add("@O_CustAuthID", SqlDbType.UniqueIdentifier, 16);
                                    comm.Parameters["@O_CustAuthID"].Direction = ParameterDirection.Output;
                                    comm.Parameters.Add("@O_RegularSRC", SqlDbType.NVarChar, 30);
                                    comm.Parameters["@O_RegularSRC"].Direction = ParameterDirection.Output;
                                    comm.Parameters.Add("@O_RegularDST", SqlDbType.NVarChar, 30);
                                    comm.Parameters["@O_RegularDST"].Direction = ParameterDirection.Output;
                                    comm.Parameters.Add("@O_CanAccept", SqlDbType.Int, 4);
                                    comm.Parameters["@O_CanAccept"].Direction = ParameterDirection.Output;
                                    comm.Parameters.Add("@O_Reason", SqlDbType.NVarChar, 50);
                                    comm.Parameters["@O_Reason"].Direction = ParameterDirection.Output;


                                    sql = CommonTools.CommandAsSql(comm);

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

                                    octl.IfTryingAny = false;
                                    octl.constructTime = DateTime.Now;
                                    octl.lastOper = "GETROUTELIST";
                                    octl.lastOperTime = DateTime.Now;

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


                                        AR.IfAlreadyTry = false;
                                        AR.IfAlreadySend = false;
                                        AR.IfSuccess = false;
                                        AR.SuccessTime = DateTime.MinValue;
                                        AR.IsFirstTry = false;
                                        AR.IPOUT = string.Empty;
                                        AR.setuptime = DateTime.MinValue;
                                        AR.connecttime = DateTime.MinValue;
                                        AR.disconnecttime = DateTime.MinValue;


                                        octl.routeList.Add(AR);
                                    }

                                ((TCPSocketServer)typed.session.AppServer).loctl.Add(octl);

                                    sb = sb.Remove(sb.Length - 1, 1);
                                    //Console.WriteLine("send back route request list {0} at {1}",sb,DateTime.Now.ToString());
                                    ((TCPSocketServer)typed.session.AppServer).Logger.Debug("send back route request list " + sb + " at " +  DateTime.Now.ToString());


                                    string sReply = @"<reply>" + sb.ToString() + @"</reply>";

                                    byte[] rv = Encoding.ASCII.GetBytes(sReply);

                                    typed.cDetail.reply_content = sReply;
                                    typed.cDetail.cmd_reply_time = DateTime.Now;


                                    typed.session.Send(rv, 0, rv.Length);

                                }

                                break;
                            case "OUTGOINGTRYFAIL":
                                {
                                    //RouterCommon.CMD_OUTGOINGTRYFAIL cmd = new RouterCommon.CMD_OUTGOINGTRYFAIL(
                                    //    Paras[0]
                                    //    , Paras[0]
                                    //    ...
                                    //    );

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
                                        ar.IfAlreadyTry = true;
                                        ar.IPOUT = Paras[4];           // record IP  , three time 
                                        //ar.setuptime = cmd.setup_time;  TODO  20170630b  shaohua 


                                        ar.setuptime = DateTime.Parse(Paras[5]);
                                        ar.connecttime = DateTime.Parse(Paras[6]);
                                        ar.disconnecttime = DateTime.Parse(Paras[7]);

                                        if (!myObject.IfTryingAny)
                                        {
                                            //Console.WriteLine("no route have trying , this is the first");
                                            myObject.IfTryingAny = true;
                                            ar.IsFirstTry = true;
                                        }


                                        bool AllHaveTry = true;
                                        foreach (availableRoutes arTest in myObject.routeList)
                                        {
                                            if ((arTest.NAPName != Paras[3]) && (arTest.IfAlreadyTry && !arTest.IfSuccess && !arTest.IfAlreadySend))
                                            {
                                                //TODO  there  exist multi thread problem ,  same fail record maybe record multi times 
                                                // send previous ongoing call fail 
                                                arTest.IfAlreadySend = true;
                                                //Console.WriteLine("callid {0} send previous nap {1} tryfail when exist", Paras[0], arTest.NAPName);
                                                ((TCPSocketServer)typed.session.AppServer).Logger.Debug("callid " + Paras[0].ToString() + " send previous nap " + arTest.NAPName + " tryfail when exist");


                                                myObject.lastOper = "TRYINGFAIL";
                                                myObject.lastOperTime = DateTime.Now;

                                                #region sendPreviousFail

                                                comm.CommandText = "sp_api_call_failed";
                                                comm.CommandType = System.Data.CommandType.StoredProcedure;
                                                comm.CommandTimeout = 300;

                                                comm.Parameters.Clear();

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
                                                comm.Parameters["@I_SRCNumOut"].Value = arTest.SRC;
                                                comm.Parameters.Add("@I_DSTNumOut", SqlDbType.NChar, 30);
                                                comm.Parameters["@I_DSTNumOut"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_DSTNumOut"].Value = arTest.DST;
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
                                                comm.Parameters["@I_EntryDetailID"].Value = arTest.EntryDetailID;
                                                comm.Parameters.Add("@I_ByPrefix", SqlDbType.NVarChar, 50);
                                                comm.Parameters["@I_ByPrefix"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_ByPrefix"].Value = arTest.prefix;
                                                comm.Parameters.Add("@I_ByPrefixGroup", SqlDbType.NVarChar, 50);
                                                comm.Parameters["@I_ByPrefixGroup"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_ByPrefixGroup"].Value = arTest.prefixGrp;
                                                comm.Parameters.Add("@I_VendorID", SqlDbType.Int, 4);
                                                comm.Parameters["@I_VendorID"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_VendorID"].Value = arTest.vendorID;
                                                comm.Parameters.Add("@I_TGIDOut", SqlDbType.UniqueIdentifier, 16);
                                                comm.Parameters["@I_TGIDOut"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_TGIDOut"].Value = arTest.TGID;
                                                comm.Parameters.Add("@I_TIDOut", SqlDbType.UniqueIdentifier, 16);
                                                comm.Parameters["@I_TIDOut"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_TIDOut"].Value = arTest.TID;
                                                comm.Parameters.Add("@I_NAPOut", SqlDbType.NVarChar, 50);
                                                comm.Parameters["@I_NAPOut"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_NAPOut"].Value = arTest.NAPName;
                                                comm.Parameters.Add("@I_IPOut", SqlDbType.NVarChar, 50);
                                                comm.Parameters["@I_IPOut"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_IPOut"].Value = arTest.IPOUT;
                                                comm.Parameters.Add("@I_SetupTime", SqlDbType.DateTime, 8);
                                                comm.Parameters["@I_SetupTime"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_SetupTime"].Value = arTest.setuptime;
                                                comm.Parameters.Add("@I_Connecttime", SqlDbType.DateTime, 8);
                                                comm.Parameters["@I_Connecttime"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_Connecttime"].Value = arTest.connecttime;
                                                comm.Parameters.Add("@I_Disconnecttime", SqlDbType.DateTime, 8);
                                                comm.Parameters["@I_Disconnecttime"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_Disconnecttime"].Value = arTest.disconnecttime;


                                                comm.Parameters.Add("@I_FlagIsFirstTry", SqlDbType.Int, 4);
                                                comm.Parameters["@I_FlagIsFirstTry"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_FlagIsFirstTry"].Value = arTest.IsFirstTry ? 1 : 0;

                                                comm.Parameters.Add("@I_FlagNotLastTry", SqlDbType.Int, 4);
                                                comm.Parameters["@I_FlagNotLastTry"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_FlagNotLastTry"].Value = 1;    // ****   is  1



                                                comm.Parameters.Add("@I_FlagFailedButConnect", SqlDbType.Int, 4);
                                                comm.Parameters["@I_FlagFailedButConnect"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_FlagFailedButConnect"].Value = 0;
                                                comm.Parameters.Add("@I_FlagFailedButConnectOut", SqlDbType.Int, 4);
                                                comm.Parameters["@I_FlagFailedButConnectOut"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_FlagFailedButConnectOut"].Value = 0;
                                                comm.Parameters.Add("@I_Duration", SqlDbType.Int, 4);
                                                comm.Parameters["@I_Duration"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_Duration"].Value = 0;
                                                comm.Parameters.Add("@I_Fee", SqlDbType.Decimal, 9);
                                                comm.Parameters["@I_Fee"].Precision = 18;
                                                comm.Parameters["@I_Fee"].Scale = 8;
                                                comm.Parameters["@I_Fee"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_Fee"].Value = arTest.RateFee;
                                                comm.Parameters.Add("@I_Cost", SqlDbType.Decimal, 9);
                                                comm.Parameters["@I_Cost"].Precision = 18;
                                                comm.Parameters["@I_Cost"].Scale = 8;
                                                comm.Parameters["@I_Cost"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_Cost"].Value = arTest.RateCost;
                                                comm.Parameters.Add("@O_ErrCode", SqlDbType.Int, 4);
                                                comm.Parameters["@O_ErrCode"].Direction = ParameterDirection.Output;
                                                comm.Parameters.Add("@O_Msg", SqlDbType.NVarChar, 200);
                                                comm.Parameters["@O_Msg"].Direction = ParameterDirection.Output;
                                                //comm.Parameters.Add("@RETURN_VALUE", SqlDbType.Int, 4);
                                                //comm.Parameters["@RETURN_VALUE"].Direction = ParameterDirection.ReturnValue;


                                                sql = CommonTools.CommandAsSql(comm);
                                                //typed.session.AppServer.Logger.Info(sql1);

                                                comm.ExecuteNonQuery();



                                                //int O_ErrCode = Convert.ToInt32(comm.Parameters["@O_ErrCode"].Value.ToString());
                                                //string O_Msg = comm.Parameters["@O_Msg"].Value.ToString();


                                                #endregion

                                            }
                                            if (arTest.IfAlreadyTry)
                                            { }
                                            else
                                            {
                                                AllHaveTry = false;
                                            }
                                        }

                                        if (AllHaveTry)    // all  have fail 
                                        {
                                            //Console.WriteLine("callid {0} all route have trying  this {1} is the last one", Paras[0], Paras[3]);
                                            ((TCPSocketServer)typed.session.AppServer).Logger.Debug("callid " + Paras[0].ToString() + " all route have trying  this " + Paras[3] + " is the last one");


                                            ar.IfAlreadySend = true;
                                           
                                            myObject.lastOper = "TRYINGFAIL";
                                            myObject.lastOperTime = DateTime.Now;

                                            #region sendLastFail

                                            comm.Parameters.Clear();

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
                                            comm.Parameters["@I_FlagIsFirstTry"].Value = ar.IsFirstTry ? 1 : 0;

                                            comm.Parameters.Add("@I_FlagNotLastTry", SqlDbType.Int, 4);
                                            comm.Parameters["@I_FlagNotLastTry"].Direction = ParameterDirection.Input;
                                            comm.Parameters["@I_FlagNotLastTry"].Value = 0;     //  ****   is 0



                                            comm.Parameters.Add("@I_FlagFailedButConnect", SqlDbType.Int, 4);
                                            comm.Parameters["@I_FlagFailedButConnect"].Direction = ParameterDirection.Input;
                                            comm.Parameters["@I_FlagFailedButConnect"].Value = 0;
                                            comm.Parameters.Add("@I_FlagFailedButConnectOut", SqlDbType.Int, 4);
                                            comm.Parameters["@I_FlagFailedButConnectOut"].Direction = ParameterDirection.Input;
                                            comm.Parameters["@I_FlagFailedButConnectOut"].Value = 0;
                                            comm.Parameters.Add("@I_Duration", SqlDbType.Int, 4);
                                            comm.Parameters["@I_Duration"].Direction = ParameterDirection.Input;
                                            comm.Parameters["@I_Duration"].Value = 0;
                                            comm.Parameters.Add("@I_Fee", SqlDbType.Decimal, 9);
                                            comm.Parameters["@I_Fee"].Precision = 18;
                                            comm.Parameters["@I_Fee"].Scale = 8;
                                            comm.Parameters["@I_Fee"].Direction = ParameterDirection.Input;
                                            comm.Parameters["@I_Fee"].Value = ar.RateFee;
                                            comm.Parameters.Add("@I_Cost", SqlDbType.Decimal, 9);
                                            comm.Parameters["@I_Cost"].Precision = 18;
                                            comm.Parameters["@I_Cost"].Scale = 8;
                                            comm.Parameters["@I_Cost"].Direction = ParameterDirection.Input;
                                            comm.Parameters["@I_Cost"].Value = ar.RateCost;
                                            comm.Parameters.Add("@O_ErrCode", SqlDbType.Int, 4);
                                            comm.Parameters["@O_ErrCode"].Direction = ParameterDirection.Output;
                                            comm.Parameters.Add("@O_Msg", SqlDbType.NVarChar, 200);
                                            comm.Parameters["@O_Msg"].Direction = ParameterDirection.Output;
                                            //comm.Parameters.Add("@RETURN_VALUE", SqlDbType.Int, 4);
                                            //comm.Parameters["@RETURN_VALUE"].Direction = ParameterDirection.ReturnValue;


                                            sql = CommonTools.CommandAsSql(comm);


                                            comm.ExecuteNonQuery();



                                            int O_ErrCode = Convert.ToInt32(comm.Parameters["@O_ErrCode"].Value.ToString());
                                            string O_Msg = comm.Parameters["@O_Msg"].Value.ToString();



                                            #endregion

                                            // remove from  List
                                            ((TCPSocketServer)typed.session.AppServer).loctl.Remove(myObject);


                                        }




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

                                        foreach (availableRoutes arTest in myObject.routeList)
                                        {
                                            if ((arTest.NAPName != Paras[3]) && (arTest.IfAlreadyTry && !arTest.IfSuccess && !arTest.IfAlreadySend))
                                            {
                                                //Console.WriteLine("send previous tryfail when exist");
                                                #region send previous fail 


                                                comm.CommandText = "sp_api_call_failed";
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
                                                comm.Parameters["@I_SRCNumOut"].Value = arTest.SRC;
                                                comm.Parameters.Add("@I_DSTNumOut", SqlDbType.NChar, 30);
                                                comm.Parameters["@I_DSTNumOut"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_DSTNumOut"].Value = arTest.DST;
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
                                                comm.Parameters["@I_EntryDetailID"].Value = arTest.EntryDetailID;
                                                comm.Parameters.Add("@I_ByPrefix", SqlDbType.NVarChar, 50);
                                                comm.Parameters["@I_ByPrefix"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_ByPrefix"].Value = arTest.prefix;
                                                comm.Parameters.Add("@I_ByPrefixGroup", SqlDbType.NVarChar, 50);
                                                comm.Parameters["@I_ByPrefixGroup"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_ByPrefixGroup"].Value = arTest.prefixGrp;
                                                comm.Parameters.Add("@I_VendorID", SqlDbType.Int, 4);
                                                comm.Parameters["@I_VendorID"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_VendorID"].Value = arTest.vendorID;
                                                comm.Parameters.Add("@I_TGIDOut", SqlDbType.UniqueIdentifier, 16);
                                                comm.Parameters["@I_TGIDOut"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_TGIDOut"].Value = arTest.TGID;
                                                comm.Parameters.Add("@I_TIDOut", SqlDbType.UniqueIdentifier, 16);
                                                comm.Parameters["@I_TIDOut"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_TIDOut"].Value = arTest.TID;
                                                comm.Parameters.Add("@I_NAPOut", SqlDbType.NVarChar, 50);
                                                comm.Parameters["@I_NAPOut"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_NAPOut"].Value = arTest.NAPName;
                                                comm.Parameters.Add("@I_IPOut", SqlDbType.NVarChar, 50);
                                                comm.Parameters["@I_IPOut"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_IPOut"].Value = arTest.IPOUT;
                                                comm.Parameters.Add("@I_SetupTime", SqlDbType.DateTime, 8);
                                                comm.Parameters["@I_SetupTime"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_SetupTime"].Value = arTest.setuptime;
                                                comm.Parameters.Add("@I_Connecttime", SqlDbType.DateTime, 8);
                                                comm.Parameters["@I_Connecttime"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_Connecttime"].Value = arTest.connecttime;
                                                comm.Parameters.Add("@I_Disconnecttime", SqlDbType.DateTime, 8);
                                                comm.Parameters["@I_Disconnecttime"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_Disconnecttime"].Value = arTest.disconnecttime;


                                                comm.Parameters.Add("@I_FlagIsFirstTry", SqlDbType.Int, 4);
                                                comm.Parameters["@I_FlagIsFirstTry"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_FlagIsFirstTry"].Value = arTest.IsFirstTry ? 1 : 0;

                                                comm.Parameters.Add("@I_FlagNotLastTry", SqlDbType.Int, 4);
                                                comm.Parameters["@I_FlagNotLastTry"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_FlagNotLastTry"].Value = 1;    // ****   is  1



                                                comm.Parameters.Add("@I_FlagFailedButConnect", SqlDbType.Int, 4);
                                                comm.Parameters["@I_FlagFailedButConnect"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_FlagFailedButConnect"].Value = 0;
                                                comm.Parameters.Add("@I_FlagFailedButConnectOut", SqlDbType.Int, 4);
                                                comm.Parameters["@I_FlagFailedButConnectOut"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_FlagFailedButConnectOut"].Value = 0;
                                                comm.Parameters.Add("@I_Duration", SqlDbType.Int, 4);
                                                comm.Parameters["@I_Duration"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_Duration"].Value = 0;
                                                comm.Parameters.Add("@I_Fee", SqlDbType.Decimal, 9);
                                                comm.Parameters["@I_Fee"].Precision = 18;
                                                comm.Parameters["@I_Fee"].Scale = 8;
                                                comm.Parameters["@I_Fee"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_Fee"].Value = arTest.RateFee;
                                                comm.Parameters.Add("@I_Cost", SqlDbType.Decimal, 9);
                                                comm.Parameters["@I_Cost"].Precision = 18;
                                                comm.Parameters["@I_Cost"].Scale = 8;
                                                comm.Parameters["@I_Cost"].Direction = ParameterDirection.Input;
                                                comm.Parameters["@I_Cost"].Value = arTest.RateCost;
                                                comm.Parameters.Add("@O_ErrCode", SqlDbType.Int, 4);
                                                comm.Parameters["@O_ErrCode"].Direction = ParameterDirection.Output;
                                                comm.Parameters.Add("@O_Msg", SqlDbType.NVarChar, 200);
                                                comm.Parameters["@O_Msg"].Direction = ParameterDirection.Output;
                                                //comm.Parameters.Add("@RETURN_VALUE", SqlDbType.Int, 4);
                                                //comm.Parameters["@RETURN_VALUE"].Direction = ParameterDirection.ReturnValue;


                                                sql = CommonTools.CommandAsSql(comm);
                                                //typed.session.AppServer.Logger.Info(sql1);

                                                comm.ExecuteNonQuery();

                                                #endregion

                                                arTest.IfAlreadySend = true;

                                             

                                                myObject.lastOper = "TRYINGFAIL";
                                                myObject.lastOperTime = DateTime.Now;
                                            }
                                        }

                                        if (!myObject.IfTryingAny)
                                        {
                                            //Console.WriteLine("no route have trying , this is the first try and success");
                                            myObject.IfTryingAny = true;

                                            #region send call start , first try success

                                            comm.Parameters.Clear();
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
                                            comm.Parameters["@I_IsFirst"].Value = 1;
                                            comm.Parameters.Add("@O_ErrCode", SqlDbType.Int, 4);
                                            comm.Parameters["@O_ErrCode"].Direction = ParameterDirection.Output;
                                            comm.Parameters.Add("@O_Msg", SqlDbType.NVarChar, 200);
                                            comm.Parameters["@O_Msg"].Direction = ParameterDirection.Output;

                                            sql = CommonTools.CommandAsSql(comm);
                                            //typed.session.AppServer.Logger.Info(sql);

                                            comm.ExecuteNonQuery();

                                            #endregion


                                            myObject.lastOper = "TRYINGSUCCESS";
                                            myObject.lastOperTime = DateTime.Now;
                                            ar.IfAlreadySend = true;
                                        }
                                        else
                                        {

                                            #region send call start ,not  first try success

                                            comm.Parameters.Clear();
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
                                            comm.Parameters["@I_IsFirst"].Value = 0;
                                            comm.Parameters.Add("@O_ErrCode", SqlDbType.Int, 4);
                                            comm.Parameters["@O_ErrCode"].Direction = ParameterDirection.Output;
                                            comm.Parameters.Add("@O_Msg", SqlDbType.NVarChar, 200);
                                            comm.Parameters["@O_Msg"].Direction = ParameterDirection.Output;

                                            sql = CommonTools.CommandAsSql(comm);
                                            //typed.session.AppServer.Logger.Info(sql);

                                            comm.ExecuteNonQuery();

                                            #endregion

                                            myObject.lastOper = "TRYINGSUCCESS";
                                            myObject.lastOperTime = DateTime.Now;

                                            ar.IfAlreadySend = true;

                                        }

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

                                        //Console.WriteLine("call stop by nap {0}", Paras[3]);
                                        ((TCPSocketServer)typed.session.AppServer).Logger.Debug("call stop by nap " + Paras[3]);

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
                                        //comm.Parameters.Add("@RETURN_VALUE", SqlDbType.Int, 4);
                                        //comm.Parameters["@RETURN_VALUE"].Direction = ParameterDirection.ReturnValue;


                                        sql = CommonTools.CommandAsSql(comm);
                                        //typed.session.AppServer.Logger.Info(sql);

                                        comm.ExecuteNonQuery();

                                        // remove  object 
                                        ((TCPSocketServer)typed.session.AppServer).loctl.Remove(myObject);

                                    }



                                }
                                break;
                            default:
                                break;
                        }   // switch 

                    
            }   //using
        }   // try 
        catch (Exception uep)
        {
            //TODO record request and error reason

            //Console.WriteLine("{0} exception happen ",uep.ToString());
            typed.session.AppServer.Logger.Info("****************************************************************");

            typed.session.AppServer.Logger.Info(uep.ToString());


            typed.session.AppServer.Logger.Info(sql);

            typed.session.AppServer.Logger.Info("Exception CallID is " + Paras[0]);

            typed.session.AppServer.Logger.Info("****************************************************************");

                    //typed.cDetail.cmd_reply_time = DateTime.Now;
                    //typed.cDetail.reply_content = uep.Message;
                    //ts.CommandDetailList.Enqueue(typed.cDetail);

                    //typed.sToMonitor = @"<reply>NORMALLOG@" + typed.sToMonitor + @". error:call sp wrong ,  reason is : " + uep.Message + @".</reply>";

                    //CommonTools.SendToEveryMonitor(typed.sToMonitor, typed.session);

                    return;
        }

                #endregion

                try
                {
                    //if (typed.session.Connected)
                    //{
                    //typed.cDetail.reply_content = sReply;
                    //typed.cDetail.cmd_reply_time = DateTime.Now;


                    //typed.session.Send(rv, 0, rv.Length);
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
                    typed.session.AppServer.Logger.Info("command try into record queue  fail ");
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
            _timer = new System.Threading.Timer(new TimerCallback(checkPendingOngoingCall), null, 0, 5000);

        }

        System.Threading.Thread th = null;


        private void checkPendingOngoingCall(object state)
        {

            for (int i = loctl.Count - 1; i >= 0; i--)
            {
                if (loctl[i].lastOper == "TRYINGFAIL")
                {
                    TimeSpan ts = DateTime.Now - loctl[i].lastOperTime;
                    if (ts.TotalSeconds > 30)
                    {
                        #region send_last_try_as_final_try
                        try
                        {
                            using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString.ToString()))   // every command open a new connection , is it ok ?
                            {
                                conn.Open();
                                SqlCommand comm = conn.CreateCommand();

                                foreach (availableRoutes arTest in loctl[i].routeList)
                                {
                                    if (arTest.IfAlreadyTry && !arTest.IfSuccess && !arTest.IfAlreadySend)
                                    {

                                        comm.CommandText = "sp_api_call_failed";
                                        comm.CommandType = System.Data.CommandType.StoredProcedure;
                                        comm.CommandTimeout = 300;

                                        comm.Parameters.Add("@I_Callid", SqlDbType.NVarChar, 30);
                                        comm.Parameters["@I_Callid"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Callid"].Value = loctl[i].CallID;
                                        comm.Parameters.Add("@I_RegularSRC", SqlDbType.NVarChar, 30);
                                        comm.Parameters["@I_RegularSRC"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_RegularSRC"].Value = loctl[i].regularSRC;
                                        comm.Parameters.Add("@I_RegularDST", SqlDbType.NVarChar, 30);
                                        comm.Parameters["@I_RegularDST"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_RegularDST"].Value = loctl[i].regularDST;
                                        comm.Parameters.Add("@I_SRCNumOut", SqlDbType.NChar, 30);
                                        comm.Parameters["@I_SRCNumOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_SRCNumOut"].Value = arTest.SRC;
                                        comm.Parameters.Add("@I_DSTNumOut", SqlDbType.NChar, 30);
                                        comm.Parameters["@I_DSTNumOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_DSTNumOut"].Value = arTest.DST;
                                        comm.Parameters.Add("@I_CustID", SqlDbType.Int, 4);
                                        comm.Parameters["@I_CustID"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_CustID"].Value = loctl[i].customID;
                                        comm.Parameters.Add("@I_AuthIDIn", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_AuthIDIn"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_AuthIDIn"].Value = loctl[i].custAuthenID;
                                        comm.Parameters.Add("@I_NAPIn", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_NAPIn"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_NAPIn"].Value = loctl[i].NAPIN;
                                        comm.Parameters.Add("@I_IPIn", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_IPIn"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_IPIn"].Value = loctl[i].IPIN;
                                        comm.Parameters.Add("@I_EntryDetailID", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_EntryDetailID"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_EntryDetailID"].Value = arTest.EntryDetailID;
                                        comm.Parameters.Add("@I_ByPrefix", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_ByPrefix"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_ByPrefix"].Value = arTest.prefix;
                                        comm.Parameters.Add("@I_ByPrefixGroup", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_ByPrefixGroup"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_ByPrefixGroup"].Value = arTest.prefixGrp;
                                        comm.Parameters.Add("@I_VendorID", SqlDbType.Int, 4);
                                        comm.Parameters["@I_VendorID"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_VendorID"].Value = arTest.vendorID;
                                        comm.Parameters.Add("@I_TGIDOut", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_TGIDOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_TGIDOut"].Value = arTest.TGID;
                                        comm.Parameters.Add("@I_TIDOut", SqlDbType.UniqueIdentifier, 16);
                                        comm.Parameters["@I_TIDOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_TIDOut"].Value = arTest.TID;
                                        comm.Parameters.Add("@I_NAPOut", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_NAPOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_NAPOut"].Value = arTest.NAPName;
                                        comm.Parameters.Add("@I_IPOut", SqlDbType.NVarChar, 50);
                                        comm.Parameters["@I_IPOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_IPOut"].Value = arTest.IPOUT;
                                        comm.Parameters.Add("@I_SetupTime", SqlDbType.DateTime, 8);
                                        comm.Parameters["@I_SetupTime"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_SetupTime"].Value = arTest.setuptime;
                                        comm.Parameters.Add("@I_Connecttime", SqlDbType.DateTime, 8);
                                        comm.Parameters["@I_Connecttime"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Connecttime"].Value = arTest.connecttime;
                                        comm.Parameters.Add("@I_Disconnecttime", SqlDbType.DateTime, 8);
                                        comm.Parameters["@I_Disconnecttime"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Disconnecttime"].Value = arTest.disconnecttime;


                                        comm.Parameters.Add("@I_FlagIsFirstTry", SqlDbType.Int, 4);
                                        comm.Parameters["@I_FlagIsFirstTry"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_FlagIsFirstTry"].Value = arTest.IsFirstTry ? 1 : 0;

                                        comm.Parameters.Add("@I_FlagNotLastTry", SqlDbType.Int, 4);
                                        comm.Parameters["@I_FlagNotLastTry"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_FlagNotLastTry"].Value = 1;    // ****   is  1



                                        comm.Parameters.Add("@I_FlagFailedButConnect", SqlDbType.Int, 4);
                                        comm.Parameters["@I_FlagFailedButConnect"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_FlagFailedButConnect"].Value = 0;
                                        comm.Parameters.Add("@I_FlagFailedButConnectOut", SqlDbType.Int, 4);
                                        comm.Parameters["@I_FlagFailedButConnectOut"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_FlagFailedButConnectOut"].Value = 0;
                                        comm.Parameters.Add("@I_Duration", SqlDbType.Int, 4);
                                        comm.Parameters["@I_Duration"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Duration"].Value = 0;
                                        comm.Parameters.Add("@I_Fee", SqlDbType.Decimal, 9);
                                        comm.Parameters["@I_Fee"].Precision = 18;
                                        comm.Parameters["@I_Fee"].Scale = 8;
                                        comm.Parameters["@I_Fee"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Fee"].Value = arTest.RateFee;
                                        comm.Parameters.Add("@I_Cost", SqlDbType.Decimal, 9);
                                        comm.Parameters["@I_Cost"].Precision = 18;
                                        comm.Parameters["@I_Cost"].Scale = 8;
                                        comm.Parameters["@I_Cost"].Direction = ParameterDirection.Input;
                                        comm.Parameters["@I_Cost"].Value = arTest.RateCost;
                                        comm.Parameters.Add("@O_ErrCode", SqlDbType.Int, 4);
                                        comm.Parameters["@O_ErrCode"].Direction = ParameterDirection.Output;
                                        comm.Parameters.Add("@O_Msg", SqlDbType.NVarChar, 200);
                                        comm.Parameters["@O_Msg"].Direction = ParameterDirection.Output;
                                        //comm.Parameters.Add("@RETURN_VALUE", SqlDbType.Int, 4);
                                        //comm.Parameters["@RETURN_VALUE"].Direction = ParameterDirection.ReturnValue;



                                        comm.ExecuteNonQuery();

                                        arTest.IfAlreadySend = true;

                                        break;
                                    }

                                }  //  for

                            }  // using 
                        }   //try
                        catch (Exception uep)
                        {


                        }



                        #endregion

                        Logger.Debug("Last opetime is " + loctl[i].lastOperTime.ToString() + " but current time is " + DateTime.Now.ToString() + " So  remove this item ");
                        loctl.RemoveAt(i);
                    }
                }

            }

            return;
        }


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
