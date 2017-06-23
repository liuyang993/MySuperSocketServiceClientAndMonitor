using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MyLib;
using log4net;

namespace RouterClient
{
    public partial class Form1 : Form
    {
        static Form1 m_form = null;

        public Form1()
        {
            InitializeComponent();

            m_form = this;
            Log.OnLog += new Log.OnLogHandler(OnLog);
        }

        //public void WriteLogImplement(Color color, string s, bool log_to_window, bool log_to_file)
        //{
        //    try
        //    {
        //        if (tbLog.TextLength > 100000)
        //        {
        //            tbLog.SelectionStart = 0;
        //            tbLog.SelectionLength = 50000;
        //            tbLog.SelectedText = "";
        //        }

        //        if (log_to_window)
        //        {
        //            string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss: ");
        //            int start = tbLog.TextLength;
        //            tbLog.AppendText(dt);
        //            tbLog.SelectionStart = start;
        //            tbLog.SelectionLength = dt.Length;
        //            tbLog.SelectionColor = Color.Blue;

        //            start = tbLog.TextLength;
        //            tbLog.AppendText(s + "\r\n");
        //            tbLog.SelectionStart = start;
        //            tbLog.SelectionLength = s.Length;
        //            tbLog.SelectionColor = color;

        //            tbLog.SelectionStart = tbLog.TextLength;
        //            tbLog.SelectionLength = 0;
        //            tbLog.ScrollToCaret();
        //        }

        //        if (log_to_file)
        //        {
        //            string log_filename = Utility.GetCurrentPath() + (MyLib.StringUtility.LeftCompare(s, "Warning:") ? "Warning_" : "Log_") + DateTime.Now.ToString("yyyy_MM_dd") + ".txt";
        //            FileStream fs = new System.IO.FileStream(log_filename, FileMode.Append);
        //            byte[] buffer = System.Text.ASCIIEncoding.ASCII.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss: ") + s + "\r\n");
        //            fs.Write(buffer, 0, buffer.Length);
        //            fs.Flush();
        //            fs.Close();
        //        }
        //        //Application.DoEvents();
        //    }
        //    catch
        //    {
        //    }
        //}

        delegate void delegate_OnLog(Color color, string s);
        static void OnLog(Log.ErrorLevel level, string message)
        {
            try
            {
                if ((m_form != null) && m_form.Created)
                {
                    Color color;
                    switch (level)
                    {
                        case Log.ErrorLevel.Fatal:
                        case Log.ErrorLevel.Error:
                        case Log.ErrorLevel.Warn: color = Color.Red; break;
                        case Log.ErrorLevel.Debug: color = Color.Blue; break;
                        case Log.ErrorLevel.Info: color = Color.Black; break;
                        default: color = Color.Black; break;
                    }
                    m_form.Invoke(new delegate_OnLog(m_form.WriteLogImplement), new object[] { color, message });
                }
            }
            catch //(Exception ex)
            {

            }
        }

        public void WriteLogImplement(Color color, string s)
        {
            try
            {
                if (tbLog.TextLength > 1000000)
                {
                    tbLog.SelectionStart = 0;
                    tbLog.SelectionLength = 500000;
                    tbLog.SelectedText = "";
                }

                string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss: ");
                int start = tbLog.TextLength;
                tbLog.AppendText(dt);
                tbLog.SelectionStart = start;
                tbLog.SelectionLength = dt.Length;
                tbLog.SelectionColor = Color.Blue;

                start = tbLog.TextLength;
                tbLog.AppendText(s);
                tbLog.SelectionStart = start;
                tbLog.SelectionLength = s.Length;
                tbLog.SelectionColor = color;

                tbLog.SelectionStart = tbLog.TextLength;
                tbLog.SelectionLength = 0;
                tbLog.ScrollToCaret();
                //Application.DoEvents();
            }
            catch
            {
            }
        }


        RClient client1 = new RClient(1, 1000000);
        RClient client2 = new RClient(2, 1000000);
        RClient client3 = new RClient(3, 1000000);
        RClient client4 = new RClient(4, 1000000);

        private void btnStart_Click(object sender, EventArgs e)
        {
            client1.Start();
            //client2.Start();
            //client3.Start();
            //client4.Start();

            timer1.Interval = 1000;
            timer1.Enabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            client1.Stop();
            client2.Stop();
            client3.Stop();
            client4.Stop();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //client 1
            lbl_Client1HourSend.Text = client1.HourCounter.ToString();
            lbl_Client1DaySend.Text = client1.DayCounter.ToString();
            lbl_Client1TotalSend.Text = client1.TotalCounter.ToString();


            lbl_Client1HourRecv.Text = client1.HourRecvCounter.ToString();
            lbl_Client1DayRecv.Text = client1.DayRecvCounter.ToString();
            lbl_Client1TotalRecv.Text = client1.TotalRecvCounter.ToString();


            //

            lbl_Client2HourSend.Text = client2.HourCounter.ToString();
            lbl_Client2DaySend.Text = client2.DayCounter.ToString();
            lbl_Client2TotalSend.Text = client2.TotalCounter.ToString();

            lbl_Client3HourSend.Text = client3.HourCounter.ToString();
            lbl_Client3DaySend.Text = client3.DayCounter.ToString();
            lbl_Client3TotalSend.Text = client3.TotalCounter.ToString();

            lbl_Client4HourSend.Text = client4.HourCounter.ToString();
            lbl_Client4DaySend.Text = client4.DayCounter.ToString();
            lbl_Client4TotalSend.Text = client4.TotalCounter.ToString();
        }
    }
}
