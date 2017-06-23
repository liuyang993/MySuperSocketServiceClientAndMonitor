using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net.Mail;
using Microsoft.Win32;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Net;

namespace MyLib
{
    public class Prompt : Exception
    {
        public Prompt(string message)
            : base(message)
        {

        }
    }

    class MyException : Exception
    {
        public MyException(string message)
            : base(message)
        {

        }
    }

    public static class Utility
    {
        public static string ExceptionInfo(this Exception ex)
        {
            string e1 = String.Format("Exception: {0}\r\nMessage: {1}\r\nStack: {2}", ex.GetType().Name, ex.Message, ex.StackTrace);
            string e2 = "";
            if (ex.InnerException != null)
                e2 = String.Format("\r\nException: {0}\r\nMessage: {1}\r\nStack: {2}", ex.InnerException.GetType().Name, ex.InnerException.Message, ex.InnerException.StackTrace);
            return e1 + e2;
        }

        public static int IsNull(object data, int def)
        {
            try
            {
                if (data == null)
                    return def;
                else
                    return Convert.ToInt32(data);
            }
            catch
            {
                return def;
            }
        }

        public static string IsNull(object data, string def)
        {
            try
            {
                if (data == null)
                    return def;
                else
                    return (string)data;
            }
            catch
            {
                return def;
            }
        }

        public static void Swap(ref int a, ref int b)
        {
            int t = a;
            a = b;
            b = t;
        }

        public static void Swap(ref string a, ref string b)
        {
            string t = a;
            a = b;
            b = t;
        }

        public static void Swap(ref DateTime a, ref DateTime b)
        {
            DateTime t = a;
            a = b;
            b = t;
        }

        public static void Swap(ref TimeSpan a, ref TimeSpan b)
        {
            TimeSpan t = a;
            a = b;
            b = t;
        }

        public static string GetCurrentPath()
        {
            return System.Windows.Forms.Application.StartupPath + "\\";
        }

        public static string LoadTextFile(string filename)
        {
            return LoadTextFile(filename, System.Text.Encoding.UTF8);
        }

        public static string LoadTextFile(string filename, System.Text.Encoding encode)
        {
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(filename, encode);
                return sr.ReadToEnd();
            }
            catch
            {
                throw new Exception("Read file error! " + filename);
            }
            finally
            {
                if (sr != null)
                    sr.Close();
                sr = null;
            }
        }

        public static void SaveTextFile(string filename, string content)
        {
            SaveTextFile(filename, content, System.Text.Encoding.UTF8);
        }

        public static void SaveTextFile(string filename, string content, System.Text.Encoding encode)
        {
            FileStream fs = null;
            StreamWriter sw = null;
            try
            {
                fs = new FileStream(filename, FileMode.Create);
                sw = new StreamWriter(fs, encode);
                sw.Write(content);
                sw.Flush();
            }
            catch(Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (fs != null) fs.Close();
                fs = null;
                //if (sw != null) sw.Close();
                sw = null;
            }
        }

        public static void CopyFile(string src, string dest)
        {
            if (File.Exists(src))
            {
                if (File.Exists(dest))
                    (new FileInfo(dest)).Attributes = FileAttributes.Normal;
                else
                {
                    string dir = System.IO.Path.GetDirectoryName(dest);
                    if (!System.IO.Directory.Exists(dir))
                        System.IO.Directory.CreateDirectory(dir);
                }
                File.Copy(src, dest, true);
            }
            else if (Directory.Exists(src))
            {
                DirectoryInfo source = new DirectoryInfo(src);
                DirectoryInfo target = new DirectoryInfo(dest);

                if (target.FullName.StartsWith(source.FullName, StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new Exception("父目录不能拷贝到子目录！");
                }

                if (!source.Exists)
                {
                    return;
                }

                if (!target.Exists)
                {
                    target.Create();
                }

                FileInfo[] files = source.GetFiles();

                for (int i = 0; i < files.Length; i++)
                {
                    CopyFile(files[i].FullName, target.FullName + @"\" + files[i].Name);
                }

                DirectoryInfo[] dirs = source.GetDirectories();

                for (int j = 0; j < dirs.Length; j++)
                {
                    CopyFile(dirs[j].FullName, target.FullName + @"\" + dirs[j].Name);
                } 

            }
        }

        public static string ReadAttribute(XmlNode node, string attr)
        {
            if (node.Attributes[attr] != null)
                return node.Attributes[attr].Value;
            else
            {
                foreach (XmlAttribute a in node.Attributes)
                    if (a.Name.IsSameText(attr))
                        return a.Value;
                return "";
            }
        }
 
        public static void AddParam(SqlCommand command, string paramName, SqlDbType type, int size, byte prec, byte scale, ParameterDirection direction, object value)
        {
            SqlParameter param = new SqlParameter();
            param.ParameterName = paramName;
            param.SqlDbType = type;
            param.Size = size;
            if (type == SqlDbType.Decimal)
            {
                param.Precision = prec;
                param.Scale = scale;
            }
            param.Direction = direction;
            command.Parameters.Add(param);
            if ((direction == ParameterDirection.Output) && (value == DBNull.Value))
                return;
            SetParam(command, paramName, value);
        }

        public static void SetParam(SqlCommand sqlCommand, string paramName, object value)
        {
            sqlCommand.Parameters[paramName].Value = value;
        }

        public static object GetParam(SqlCommand sqlCommand, string paramName)
        {
            return sqlCommand.Parameters[paramName].Value;
        }

        public static void NOP()
        {
        }

        public static string MakeValidNameForID(string id)
        {
            return id.Replace(" ", "_").Replace("-", "_").Replace(".", "_");
        }

        public static string MakeValidNameForSqlID(string id)
        {
            if (id.Contains("["))
                return id;
            string[] ss = id.Split('.');
            string result = "";
            for (int i = 0; i < ss.Length; i++)
            {
                if (id.Contains(" ") || id.Contains(" ") || id.Contains(" "))
                    result += (i > 0 ? "." : "") + "[" + id + "]";
                else
                    result += (i > 0 ? "." : "") + id;
            }
            return result;
        }

        public static string MakeValidNameForFile(string id)
        {
            return id.Replace(" ", "_"); //.Replace("-", "_");
        }


        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void KeepColumns(DataTable dt, string c1)
        {
            int i = 0;
            while (i < dt.Columns.Count)
            {
                if (dt.Columns[i].ColumnName.ToUpper() != c1.ToUpper())
                    dt.Columns.RemoveAt(i);
                else
                    i++;
            }
        }

        public static void KeepColumns(DataTable dt, string c1, string c2)
        {
            int i = 0;
            while (i < dt.Columns.Count)
            {
                if ((dt.Columns[i].ColumnName.ToUpper() != c1.ToUpper()) && (dt.Columns[i].ColumnName.ToUpper() != c2.ToUpper()))
                    dt.Columns.RemoveAt(i);
                else
                    i++;
            }
        }

        public static string AppendFileName(string fullname, string append)
        {
            return ExtractFilePath(fullname) + ExtractFileName(fullname) + append + ExtractFileExt(fullname);
        }

        public static string ExtractFilePath(string fullname)
        {
            return fullname.LastIndexOf("\\") >= 0 ? fullname.Substring(0, fullname.LastIndexOf("\\") + 1) : fullname;
        }

        public static string ExtractFileNameExt(string fullname)
        {
            return fullname.LastIndexOf("\\") >= 0 ? fullname.Substring(fullname.LastIndexOf("\\") + 1) : fullname;
        }

        public static string ExtractFileName(string fullname)
        {
            string filenam_ext = ExtractFileNameExt(fullname);
            return filenam_ext.LastIndexOf(".") >= 0 ? filenam_ext.Substring(0, filenam_ext.LastIndexOf(".")) : filenam_ext;
        }

        public static string ExtractFileExt(string fullname)
        {
            string filenam_ext = ExtractFileNameExt(fullname);
            return filenam_ext.LastIndexOf(".") >= 0 ? filenam_ext.Substring(filenam_ext.LastIndexOf(".")) : "";
        }


    }
}
