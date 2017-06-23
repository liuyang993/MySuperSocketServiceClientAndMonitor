using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using MyLib;
using SuperSocket.ClientEngine;
using SuperSocket.ProtoBase;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RouterClient
{
    public class RClient : MyThread2
    {
        private int mID;
        private int mMaxCalls;
        private  SortedList<string, RRequest> mPendingRequests = new SortedList<string, RRequest>();

        private EasyClient client;

        public RClient(int id, int calls)
        {
            this.mID = id;
            this.mMaxCalls = calls;

            var result = AccessTheWebAsync();
        }


        private int  AccessTheWebAsync()
        {
            client = new EasyClient();

            client.Initialize(new MyReceiveFilter(), (request) =>
            {
                // handle the received request
                ShowWindowsMessage(request.Key, request.Body);
            });

            var connected =  client.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2080));

            return 0;

        }


        public void ShowWindowsMessage(string sHead, string sReply)
        {
            DateTime now = DateTime.Now;
            if (now.Hour == lastReportAt.Hour)
            {
                this.HourRecvCounter++;
                this.DayRecvCounter++;
                this.TotalRecvCounter++;
            }
            else
            {
                if (now.Day == lastReportAt.Day)
                {
                    this.HourRecvCounter = 0;
                    this.DayRecvCounter++;
                    this.TotalRecvCounter++;
                }
                else
                {
                    this.HourRecvCounter = 0;
                    this.DayRecvCounter = 0;
                    this.TotalRecvCounter++;
                }
            }

            return;

            //if (sHead == "ROUTEREQUEST")
            //{
                //if (DateTime.Now.Hour == iCurrentHour)
                //{
                //    iHourlyRecv += 1;
                //}
                //else
                //{
                //    iHourlyRecv = 0;
                //}

                //if (DateTime.Now.Day == iCurrentDay)
                //{
                //    iDaillyRecv += 1;
                //}
                //else
                //{
                //    iDaillyRecv = 0;
                //}

                //iTotalRecv += 1;

                //textBox3.Invoke((Action)(() => textBox3.Text = iHourlyRecv.ToString()));
                //textBox7.Invoke((Action)(() => textBox7.Text = iDaillyRecv.ToString()));
                //textBox11.Invoke((Action)(() => textBox11.Text = iTotalRecv.ToString()));


                //// pending
                //textBox5.Invoke((Action)(() => textBox5.Text = (iTotalSend - iTotalRecv / 2).ToString()));

                //richTextBox2.Invoke((Action)(() => richTextBox2.AppendText(sReply + "\r\n")));

            //}

            return;

        }


        private DateTime lastReportAt;

        public int HourCounter
        {
            get; set;
        }

        public int DayCounter
        {
            get; set;
        }

        public int TotalCounter
        {
            get; set;
        }


        public int HourRecvCounter
        {
            get; set;
        }

        public int DayRecvCounter
        {
            get; set;
        }

        public int TotalRecvCounter
        {
            get; set;
        }


        public int PendingInQueue
        {
            get
            {
                return this.mPendingRequests.Count;
            }
        }


        protected override void OnStart()
        {
            //Log.Write("Router client[" + this.mID + "] started.");
            lastReportAt = DateTime.MinValue;
        }

        protected override void OnStop()
        {
            Log.Write("Router client[" + this.mID + "] stopped.");
        }

        int requestID = 0;

        protected override void OnWork()
        {
            lock (Locker)
            {
            }

            try
            {
                if (requestID >= this.mMaxCalls)
                {
                    SleepMS(1000);
                    return;
                }

                DateTime now = DateTime.Now;
                if (now.Hour == lastReportAt.Hour)
                {
                    this.HourCounter++;
                    this.DayCounter++;
                    this.TotalCounter++;
                }
                else
                {
                    if (now.Day == lastReportAt.Day)
                    {
                        this.HourCounter = 0;
                        this.DayCounter++;
                        this.TotalCounter++;
                    }
                    else
                    {
                        this.HourCounter = 0;
                        this.DayCounter = 0;
                        this.TotalCounter++;
                    }
                }

                if (this.lastReportAt == DateTime.MinValue
                     || this.lastReportAt.Hour != now.Hour)
                {
                    //Log.Info("Hourly report: [{0}]: hour:{1} day:{2} total:{3}", this.mID, this.HourCounter, this.DayCounter, this.TotalCounter);
                    this.lastReportAt = now;
                }

                requestID++;
                string call_id =
                    now.ToString("yyyyMMdd")
                    + ("00" + mID.ToString()).RightStr(2)
                    + ("00000000" + requestID.ToString()).RightStr(8);
                RRequest request = new RRequest(call_id);                
                mPendingRequests.Add(request.CallID, request);
                sendRequest(request);
                int sleep = MyLib.Rand.RandomInt(1, 1000);
                SleepMS(sleep);               
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }


        void OnReceived(string data)
        {
            //decode message

            //mPendingRequests.IndexOfKey(callid)
                            
            //update mPendingRequests 

        }

        void sendRequest(RRequest request)
        {
            if (client.IsConnected)
            {
                string sTrySend = @"<cmd>ROUTEREQUEST;" + request.CallID + @",60133878332,2228613803088967,ACECALL,202.96.74.13</cmd>";
                byte[] bData = Encoding.ASCII.GetBytes(sTrySend);
                client.Send(bData);
            }



            //Log.Info("Send request[{0}]", request.CallID);
        }
    }


    public class RRequest
    {
        public string CallID;
        public string _Type;


        public DateTime SendAt;
        public bool Reply1;
        public DateTime Reply1At;
        public bool Reply2;
        public DateTime Reply2At;

        public RRequest(string call_id)
        {
            this.CallID = call_id;
        }
    }

}
