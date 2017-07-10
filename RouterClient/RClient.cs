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
using System.Collections.Concurrent;

namespace RouterClient
{
    enum CallState
    {
        FullFail, Success, FailCozInterrupt
    }

    public class availableRoutes
    {
        public string NAPName { get; set; }
        public string SRC { get; set; }
        public string DST { get; set; }
        public bool IfAlreadyTry { get; set; }

        public bool IfSuccess { get; set; }
        public DateTime SuccessTime { get; set; }

    }
    public class OutgoingCallTringLists
    {
        public string CallID { get; set; }

        public List<availableRoutes> routeList { get; set; }

    }

    public class RClient : MyThread2
    {
        private int mID;
        private int mMaxCalls;
        private SortedList<string, RRequest> mPendingRequests = new SortedList<string, RRequest>();

        private ConcurrentBag<OutgoingCallTringLists> loctl = new ConcurrentBag<OutgoingCallTringLists>();

        private EasyClient client;

        public RClient(int id, int calls)
        {
            this.mID = id;
            this.mMaxCalls = calls;

            var result = AccessTheWebAsync();
        }


        private int AccessTheWebAsync()
        {
            client = new EasyClient();

            client.Initialize(new MyReceiveFilter(), (request) =>
            {
                // handle the received request
                ShowWindowsMessage(request.Key, request.Body);
            });

            var connected = client.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2080));

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

            //
            if ((sHead == "ROUTEREQUEST") && (sReply.Length > 100))
            {
                List<string> Paras = sReply.Split(',').ToList<string>();
                int iLoop = (Paras.Count - 3) / 5;

                OutgoingCallTringLists octl = new OutgoingCallTringLists();
                octl.routeList = new List<availableRoutes>();

                octl.CallID = Paras[0];

                for (int i = 0; i < iLoop; i++)
                {
                    availableRoutes ar = new availableRoutes();
                    ar.NAPName = Paras[i * 5 + 3];
                    ar.SRC = Paras[i * 5 + 4];
                    ar.DST = Paras[i * 5 + 5];
                    ar.IfAlreadyTry = false;
                    ar.IfSuccess = false;
                    ar.SuccessTime = DateTime.MinValue;
                    octl.routeList.Add(ar);
                }

                loctl.Add(octl);

                Array values = Enum.GetValues(typeof(CallState));
                Random random = new Random();
                CallState randomBar = (CallState)values.GetValue(random.Next(values.Length));

                Log.Write("callID is {0} and random is {1}", Paras[0], randomBar.ToString());



                switch (randomBar)
                {
                    case (CallState.FullFail):
                        {
                            for (int j = 0; j < octl.routeList.Count; j++)
                            {
                                string sTrySend = @"<cmd>OUTGOINGTRYFAIL;" + octl.CallID + @"," + octl.routeList[j].SRC + @"," + octl.routeList[j].DST + @"," + octl.routeList[j].NAPName + @",12.34.56.789" + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"</cmd>";

                                Log.Write(sTrySend);
                                byte[] bData = Encoding.ASCII.GetBytes(sTrySend);
                                client.Send(bData);
                            }

                            //loctl.Remove(octl);

                        }
                        break;
                    case (CallState.Success):
                        {
                            // make a randome number , decide which time trying will success
                            Random r = new Random();
                            int rInt = r.Next(0, octl.routeList.Count - 1);

                            if (rInt == 0)   // mean success from the first trying 
                            {
                                string sTrySend = @"<cmd>OUTGOINGTRYSUCCESS;" + octl.CallID + @"," + octl.routeList[0].SRC + @"," + octl.routeList[0].DST + @"," + octl.routeList[0].NAPName + @",12.34.56.789" + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"</cmd>";
                                Log.Write(sTrySend);
                                byte[] bData = Encoding.ASCII.GetBytes(sTrySend);
                                octl.routeList[0].IfSuccess = true;
                                octl.routeList[0].SuccessTime = DateTime.Now;

                                client.Send(bData);
                            }
                            else
                            {
                                for (int j = 0; j < rInt; j++)
                                {
                                    string sTrySend = @"<cmd>OUTGOINGTRYFAIL;" + octl.CallID + @"," + octl.routeList[j].SRC + @"," + octl.routeList[j].DST + @"," + octl.routeList[j].NAPName + @",12.34.56.789" + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"</cmd>";
                                    Log.Write(sTrySend);
                                    byte[] bData = Encoding.ASCII.GetBytes(sTrySend);
                                    client.Send(bData);
                                }

                                string sTrySendFinal = @"<cmd>OUTGOINGTRYSUCCESS;" + octl.CallID + @"," + octl.routeList[rInt].SRC + @"," + octl.routeList[rInt].DST + @"," + octl.routeList[rInt].NAPName + @",12.34.56.789" + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"</cmd>";


                                Log.Write(sTrySendFinal);
                                byte[] bDataFinal = Encoding.ASCII.GetBytes(sTrySendFinal);

                                octl.routeList[rInt].IfSuccess = true;
                                octl.routeList[rInt].SuccessTime = DateTime.Now;

                                client.Send(bDataFinal);

                            }
                        }
                        break;
                    case (CallState.FailCozInterrupt):
                        {
                            Random r = new Random();
                            int rInt = r.Next(0, octl.routeList.Count - 1);


                            for (int j = 0; j < rInt; j++)
                            {
                                string sTrySend = @"<cmd>OUTGOINGTRYFAIL;" + octl.CallID + @"," + octl.routeList[j].SRC + @"," + octl.routeList[j].DST + @"," + octl.routeList[j].NAPName + @",12.34.56.789" + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"</cmd>";
                                Log.Write(sTrySend);
                                byte[] bData = Encoding.ASCII.GetBytes(sTrySend);
                                client.Send(bData);
                            }

                        }
                        break;
                }





            }  //  end  of  if reply  is route request 
            else if (sHead == "OUTGOINGTRYSUCCESS")
            {

                Log.Write(sHead + @";" + sReply);

            }
            else if (sHead == "CALLSTOPSUCCESS")
            {
                Log.Write(sHead + @";" + sReply);
            }
            else if (sHead == "OUTGOINGTRYFAIL")
            {
                Log.Write(sHead + @";" + sReply);
            }

                



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
                if (requestID <= 1000)
                {
                    string call_id =
                    now.ToString("yyyyMMdd")
                    + ("00" + mID.ToString()).RightStr(2)
                    + ("00000000" + requestID.ToString()).RightStr(8);

                    Log.Write("callid is {0}", call_id);

                    RRequest request = new RRequest(call_id);
                    mPendingRequests.Add(request.CallID, request);
                    sendRequest(request);


                    int sleep = MyLib.Rand.RandomInt(1, 100);
                    SleepMS(sleep);
                    return;
                }
                //

                foreach (OutgoingCallTringLists octlaaa in loctl)
                {
                    foreach (availableRoutes araaa in octlaaa.routeList)
                    {
                        TimeSpan ts = DateTime.Now - araaa.SuccessTime;
                        if ((araaa.IfSuccess == true) && (ts.TotalSeconds > 60))
                        {

                            string sTrySend = @"<cmd>CALLSTOP;" + octlaaa.CallID + @"," + araaa.SRC + @"," + araaa.DST + @"," + araaa.NAPName + @",12.34.56.789" + @"," + araaa.SuccessTime.ToString("yyyy/MM/dd HH:mm") + @"," + araaa.SuccessTime.ToString("yyyy/MM/dd HH:mm") + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"," + (int)ts.TotalSeconds + @"</cmd>";

                            byte[] bData = Encoding.ASCII.GetBytes(sTrySend);
                            client.Send(bData);
                            araaa.SuccessTime = DateTime.MaxValue;

                            //Console.WriteLine("{0} have success trying", octlaaa.CallID);
                            Log.Write(octlaaa.CallID + "have success trying");

                            break;
                        }
                    }

                }

                SleepMS(5000);





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