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
                case SqlDbType.UniqueIdentifier:
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

    }
}
