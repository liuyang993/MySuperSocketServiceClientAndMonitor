//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Drawing;
//using System.Threading;
//using MyLib;

//namespace RouterClient
//{
//    public class Log
//    {
//        #region Send Log Info to form

//        private static Form1 m_form = null;
//        public static Form1 SetForm(Form1 form)
//        {
//            m_form = form;
//            return m_form;
//        }

//        private delegate void delegate_WriteLog(Color color, string s, bool log_to_window, bool log_to_file);
//        public static void Write(Color color, string s, bool log_to_window, bool log_to_file)
//        {
//            try
//            {
//                if ((m_form != null) && m_form.Created)
//                    m_form.Invoke(new delegate_WriteLog(m_form.WriteLogImplement), new object[] { color, s, log_to_window, log_to_file });
//            }
//            catch
//            {
//            }
//        }

//        public static void Write(Color color, string s)
//        {
//            Write(color, s, true, true);
//        }

//        public static void Write(Color color, bool log_to_window, string format, params object[] objects)
//        {
//            Write(color, String.Format(format, objects), log_to_window, true);
//        }

//        public static void Write(Color color, bool log_to_window, bool log_to_file, string format, params object[] objects)
//        {
//            Write(color, String.Format(format, objects), log_to_window, log_to_file);
//        }

//        public static void Write(Color color, string format, params object[] objects)
//        {
//            Write(color, String.Format(format, objects), true, true);
//        }

//        public static void Write(string s)
//        {
//            Write(Color.Black, s, true, true);
//        }

//        public static void Write(string format, params object[] objects)
//        {
//            Write(Color.Black, String.Format(format, objects), true, true);
//        }

//        public static void WriteToFile(string s)
//        {
//            Write(Color.Black, s, false, true);
//        }

//        public static void WriteToFile(string format, params object[] objects)
//        {
//            Write(Color.Black, String.Format(format, objects), false, true);
//        }



//        public static void Info(string s)
//        {
//            Write(Color.Black, s, true, true);
//        }

//        public static void Info(string format, params object[] objects)
//        {
//            if (objects.Length == 0)
//                Write(Color.Black, format, true, true);
//            else
//                Write(Color.Black, String.Format(format, objects), true, true);
//        }

//        public static void Info(Exception ex)
//        {
//            Write(Color.Black, ex.ExceptionInfo(), true, true);
//        }


//        public static void Error(string s)
//        {
//            Write(Color.Red, s, true, true);
//        }

//        public static void Error(string format, params object[] objects)
//        {
//            if (objects.Length == 0)
//                Write(Color.Red, format, true, true);
//            else
//                Write(Color.Red, String.Format(format, objects), true, true);
//        }

//        public static void Error(Exception ex)
//        {
//            Write(Color.Red, ex.ExceptionInfo(), true, true);
//        }
//        #endregion
//    }
//}
