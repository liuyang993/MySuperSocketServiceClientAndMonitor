using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace MyRouteService
{
    public static class CommonTools
    {

        public  const int MAXCACHENUMBER = 10000;

        public const int ROUTEREQUEST_PARACOUNT = 5;
        public const int OUTGOINGTRYFAIL_PARACOUNT = 8;
        public const int OUTGOINGTRYSUCCESS_PARACOUNT = 7;
        public const int CALLSTOP_PARACOUNT = 9;

        public static byte[] CombineReply(string strContent)
        {
            byte[] bHead = Encoding.ASCII.GetBytes(@"<reply>");
            byte[] bTail = Encoding.ASCII.GetBytes(@"</reply>");
            byte[] bData = Encoding.ASCII.GetBytes(strContent);


            byte[] rv = new byte[bHead.Length + bTail.Length + bData.Length];
            System.Buffer.BlockCopy(bHead, 0, rv, 0, bHead.Length);
            System.Buffer.BlockCopy(bData, 0, rv, bHead.Length, bData.Length);
            System.Buffer.BlockCopy(bTail, 0, rv, bHead.Length + bData.Length, bTail.Length);


            return rv;
        }

        public static void SendToEveryMonitor(string sContent, TCPSocketSession session)
        {
            byte[] bRequest = Encoding.ASCII.GetBytes(sContent);
            var sessions = session.AppServer.GetSessions(s => s.bIfMonitorClient == true);
            foreach (var s in sessions)
            {
                try
                {
                    s.Send(bRequest, 0, bRequest.Length);
                }
                catch (Exception tc)
                {
                    Console.WriteLine("send back to monitor time out");
                }
            }

        }

        public static String ParameterValueForSQL(this SqlParameter sp)
        {
            String retval = "";

            switch (sp.SqlDbType)
            {
                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Text:
                case SqlDbType.Time:
                case SqlDbType.VarChar:
                case SqlDbType.Xml:
                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.DateTimeOffset:
                    retval = "'" + sp.Value.ToString().Replace("'", "''") + "'";
                    break;

                //case SqlDbType.Bit:
                //    retval = (sp.Value.ToBooleanOrDefault(false)) ? "1" : "0";
                //    break;

                default:
                    retval = sp.Value.ToString().Replace("'", "''");
                    break;
            }

            return retval;
        }

        public static String CommandAsSql(this SqlCommand sc)
        {
            StringBuilder sql = new StringBuilder();
            Boolean FirstParam = true;

            sql.AppendLine("use " + sc.Connection.Database + ";");
            switch (sc.CommandType)
            {
                case CommandType.StoredProcedure:
                    sql.AppendLine("declare @return_value int;");

                    foreach (SqlParameter sp in sc.Parameters)
                    {
                        if ((sp.Direction == ParameterDirection.InputOutput) || (sp.Direction == ParameterDirection.Output))
                        {
                            sql.Append("declare " + sp.ParameterName + "\t" + sp.SqlDbType.ToString() + "\t= ");

                            sql.AppendLine(((sp.Direction == ParameterDirection.Output) ? "null" : sp.ParameterValueForSQL()) + ";");

                        }
                    }

                    sql.AppendLine("exec [" + sc.CommandText + "]");

                    foreach (SqlParameter sp in sc.Parameters)
                    {
                        if (sp.Direction != ParameterDirection.ReturnValue)
                        {
                            sql.Append((FirstParam) ? "\t" : "\t, ");

                            if (FirstParam) FirstParam = false;

                            if (sp.Direction == ParameterDirection.Input)
                                sql.AppendLine(sp.ParameterName + " = " + sp.ParameterValueForSQL());
                            else

                                sql.AppendLine(sp.ParameterName + " = " + sp.ParameterName + " output");
                        }
                    }
                    sql.AppendLine(";");

                    sql.AppendLine("select 'Return Value' = @return_value");

                    foreach (SqlParameter sp in sc.Parameters)
                    {
                        if ((sp.Direction == ParameterDirection.InputOutput) || (sp.Direction == ParameterDirection.Output))
                        {
                            sql.AppendLine("select '" + sp.ParameterName + "' =  " + sp.ParameterName );
                        }
                    }
                    break;
                case CommandType.Text:
                    sql.AppendLine(sc.CommandText);
                    break;
            }

            return sql.ToString();
        }

        //public static void checkListCount(List<CommandDetail> lc)
        //{
        //    if (lc.Count > 100)
        //    {
        //        using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString.ToString()))
        //        {
        //            try
        //            {
        //                conn.Open();

        //                SqlCommand comm = conn.CreateCommand();
        //                //comm.CommandText = "sp_app_RatePlanDetail_Add_1114";
        //                comm.CommandText = "sp_InsertRequestReply";
        //                comm.CommandType = System.Data.CommandType.StoredProcedure;
        //                comm.CommandTimeout = 300;


        //                comm.Parameters.Add(new SqlParameter()
        //                {
        //                    ParameterName = "@TVP",
        //                    SqlDbType = SqlDbType.Structured,
        //                    Value = GetDataTableParam(lc)
        //                });

        //                comm.Parameters.Add(new SqlParameter()
        //                {
        //                    ParameterName = "@result",
        //                    SqlDbType = SqlDbType.Int,
        //                    Direction = ParameterDirection.Output,
        //                    Size = 4
        //                });

        //                comm.Parameters.Add(new SqlParameter()
        //                {
        //                    ParameterName = "@errormsg",
        //                    SqlDbType = SqlDbType.VarChar,
        //                    Direction = ParameterDirection.Output,
        //                    Size = 1000
        //                });

        //                comm.ExecuteNonQuery();
        //            }
        //            catch (Exception uep)
        //            {
        //                return;
        //            }
        //        }   //using

        //        //清空
        //        lc.Clear();



        //    }
        //}

        //public static DataTable GetDataTableParam(List<CommandDetail> People)
        //{
        //    //define the table and rows (the rows match those in the TVP)
        //    DataTable peopleTable = new DataTable();
        //    peopleTable.Columns.Add("Session_IP", typeof(string));
        //    peopleTable.Columns.Add("Session_ID", typeof(string));
        //    peopleTable.Columns.Add("CommandName", typeof(string));

        //    peopleTable.Columns.Add("Request_Recv_Time", typeof(DateTime));
        //    peopleTable.Columns.Add("Request_reply_Time", typeof(DateTime));

        //    peopleTable.Columns.Add("Request_Content", typeof(string));
        //    peopleTable.Columns.Add("Reply_Content", typeof(string));
        //    peopleTable.Columns.Add("Err_Reason", typeof(string));


        //    foreach (CommandDetail p in People)
        //    {
        //        // add a row for each person
        //        DataRow row = peopleTable.NewRow();
        //        //row["clsCarrierID"] = p.clsCarrierID;
        //        //row["clsis_supplier"] = p.clsis_supplier;
        //        row["Session_IP"] = p.sessionIP;
        //        row["Session_ID"] = p.sessionID;
        //        row["CommandName"] = p.commandName;

        //        row["Request_Recv_Time"] = p.cmd_recv_time;
        //        row["Request_reply_Time"] = p.cmd_reply_time;
        //        row["Request_Content"] = p.cmd_content;
        //        row["Reply_Content"] = p.reply_content;
        //        row["Err_Reason"] = p.err_reason;

        //        peopleTable.Rows.Add(row);
        //    }
        //    return peopleTable;
        //}

    }
}
