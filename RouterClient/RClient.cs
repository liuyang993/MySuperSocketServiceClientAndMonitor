
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
using RouterCommon;

namespace RouterClient
{
    enum CallState : int
    {
        FullFail = 1,
        Success = 2,
        FailCozInterrupt = 3
    }

    public class availableRoutes
    {
        public string NAPName { get; set; }

        public string IP { get; set; }
        public string SRC { get; set; }
        public string DST { get; set; }
        public bool IfAlreadyTry { get; set; }

        public bool IfSuccess { get; set; }
        public DateTime SuccessTime { get; set; }

    }
    //public class OutgoingCallTringLists
    //{
    //    public string CallID { get; set; }

    //}

    public class RClient : MyThread2
    {
        private int mID;
        private int mMaxCalls;
        public SortedList<string, RRequest> mPendingRequests = new SortedList<string, RRequest>();

        //private List<OutgoingCallTringLists> loctl = new List<OutgoingCallTringLists>();

        //private ConcurrentBag<OutgoingCallTringLists> loctl = new ConcurrentBag<OutgoingCallTringLists>();

        public EasyClient client;

        public RClient(int id, int calls)
        {
            this.mID = id;
            this.mMaxCalls = calls;

            var result = InitEasyClient();
        }


        private int InitEasyClient()
        {
            client = new EasyClient();

            client.Initialize(new MyReceiveFilter(), (request) =>
            {
                // handle the received request
                OnRecvData(request.Key, request.Body);
            });

            var connected = client.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2080));

            return 0;

        }


        public void OnRecvData(string sHead, string sContent)
        {
            //decode message
            RRequest.OnReply(this, sHead, sContent);

            //mPendingRequests.IndexOfKey(callid)

            return;
        }


        public DateTime lastReportAt;

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
                if (requestID <= 100)
                {


                    string call_id =
                        now.ToString("yyyyMMdd")
                        + ("00" + mID.ToString()).RightStr(2)
                        + ("00000000" + requestID.ToString()).RightStr(8);

                    //Console.WriteLine("callid is {0}", call_id);
                    Log.Write("callid is {0}", call_id);


                    RRequest request = new RRequest(this, call_id);
                    mPendingRequests.Add(request.CallID, request);
                    sendRequest(request);


                    int sleep = MyLib.Rand.RandomInt(1, 100);
                    SleepMS(sleep);
                    //

                }





                SleepMS(100);

                foreach (KeyValuePair<string, RRequest> kvp in mPendingRequests)
                {
                    //Console.WriteLine(kvp.Value);
                    //Console.WriteLine(kvp.Key);

                    foreach (availableRoutes araaa in kvp.Value.routeList)
                    {
                        TimeSpan ts = DateTime.Now - araaa.SuccessTime;
                        if ((araaa.IfSuccess == true) && (ts.TotalSeconds > 60))
                        {

                            string sTrySend = @"<cmd>CALLSTOP;" + kvp.Key + @"," + araaa.SRC + @"," + araaa.DST + @"," + araaa.NAPName + @",12.34.56.789" + @"," + araaa.SuccessTime.ToString("yyyy/MM/dd HH:mm") + @"," + araaa.SuccessTime.ToString("yyyy/MM/dd HH:mm") + @"," + DateTime.Now.ToString("yyyy/MM/dd HH:mm") + @"," + (int)ts.TotalSeconds + @"</cmd>";

                            byte[] bData = Encoding.ASCII.GetBytes(sTrySend);
                            client.Send(bData);
                            araaa.SuccessTime = DateTime.MaxValue;

                            //Console.WriteLine("{0} have success trying", octlaaa.CallID);
                            Log.Write("{0} have success trying", kvp.Key);
                            break;
                        }
                    }

                }   // end foreach 




            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
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
        public RClient parent;
        public string CallID;
        public string _Type;

        public List<availableRoutes> routeList { get; set; }

        public DateTime SendAt;
        public bool Reply1;
        public DateTime Reply1At;
        public bool Reply2;
        public DateTime Reply2At;

        public RRequest(RClient _parent, string call_id)
        {
            this.parent = _parent;
            this.CallID = call_id;
            this.routeList = new List<availableRoutes>();
        }

        public static void OnReply(RClient host, string sHeader, string sContent)
        {
            //mPendingRequests.IndexOfKey(callid)

            //update mPendingRequests 

            DateTime now = DateTime.Now;
            if (now.Hour == host.lastReportAt.Hour)
            {
                host.HourRecvCounter++;
                host.DayRecvCounter++;
                host.TotalRecvCounter++;
            }
            else
            {
                if (now.Day == host.lastReportAt.Day)
                {
                    host.HourRecvCounter = 0;
                    host.DayRecvCounter++;
                    host.TotalRecvCounter++;
                }
                else
                {
                    host.HourRecvCounter = 0;
                    host.DayRecvCounter = 0;
                    host.TotalRecvCounter++;
                }
            }

            if ((sHeader == "ROUTEREQUEST") && (sContent.Length > 100))
            {
                List<string> Paras = sContent.Split(',').ToList<string>();
                RRequest request = host.mPendingRequests[Paras[0]];
                request.routeList.Clear();
                int iLoop = (Paras.Count - 3) / 5;
                for (int i = 0; i < iLoop; i++)
                {
                    availableRoutes ar = new availableRoutes();
                    ar.NAPName = Paras[i * 5 + 3];
                    ar.SRC = Paras[i * 5 + 4];
                    ar.DST = Paras[i * 5 + 5];
                    ar.IfAlreadyTry = false;
                    ar.IfSuccess = false;
                    ar.SuccessTime = DateTime.MinValue;
                    ar.IP = "123.456.78.9";
                    request.routeList.Add(ar);
                }
                request.Process_ROUTEREQUEST();
            }
            else //if ((sHeader == "ROUTEREQUEST") && (sContent.Length > 100))
            {
                //Log.Error("Unexpected reply");
            }

        }

        void Process_ROUTEREQUEST()
        {
           // this.parent.SleepMS(200);      //  before send next command , wait 200 ms 

            CallState randomBar = (CallState)MyLib.Rand.RandomInt(1, 3);

            //Console.WriteLine("callID is {0} and random is {1}", Paras[0], randomBar.ToString());
            Log.Write("callID is {0} and random is {1}", this.CallID, randomBar.ToString());


            switch (randomBar)
            {
                case (CallState.FullFail):
                    {
                        for (int j = 0; j < this.routeList.Count; j++)
                        {
                            string sTrySend = (new CMD_OUTGOINGTRYFAIL(
                                this.CallID
                                , this.routeList[j].SRC
                                , this.routeList[j].DST
                                , this.routeList[j].NAPName
                                , this.routeList[j].IP
                                , DateTime.Now
                                , DateTime.Now
                                , DateTime.Now
                                )).Encode();

                            //Console.WriteLine(sTrySend);
                            Log.Write(sTrySend);

                            byte[] bData = Encoding.ASCII.GetBytes(sTrySend);
                            this.parent.client.Send(bData);
                            //this.parent.SleepMS(200);
                        }

                        //loctl.Remove(octl);
                    }
                    break;
                case (CallState.Success):
                    {
                        // make a randome number , decide which time trying will success
                        Random r = new Random();
                        int rInt = r.Next(0, this.routeList.Count - 1);

                        if (rInt == 0)   // mean success from the first trying 
                        {
                            string sTrySend = (new CMD_OUTGOINGTRYSUCCESS(
                                                this.CallID
                                                , this.routeList[0].SRC
                                                , this.routeList[0].DST
                                                , this.routeList[0].NAPName
                                                , this.routeList[0].IP
                                                , DateTime.Now
                                                , DateTime.Now
                                                )).Encode();

                            //Console.WriteLine(sTrySend);
                            Log.Write(sTrySend);
                            byte[] bData = Encoding.ASCII.GetBytes(sTrySend);
                            this.routeList[0].IfSuccess = true;
                            this.routeList[0].SuccessTime = DateTime.Now;

                            this.parent.client.Send(bData);
                        }
                        else
                        {
                            for (int j = 0; j < rInt; j++)
                            {
                                string sTrySend = (new CMD_OUTGOINGTRYFAIL(
                                    this.CallID
                                    , this.routeList[j].SRC
                                    , this.routeList[j].DST
                                    , this.routeList[j].NAPName
                                    , this.routeList[j].IP
                                    , DateTime.Now
                                    , DateTime.Now
                                    , DateTime.Now
                                    )).Encode();
                                //Console.WriteLine(sTrySend);
                                Log.Write(sTrySend);
                                byte[] bData = Encoding.ASCII.GetBytes(sTrySend);
                                this.parent.client.Send(bData);
                                this.parent.SleepMS(200);
                            }

                            string sTrySendFinal = (new CMD_OUTGOINGTRYSUCCESS(
                                    this.CallID
                                    , this.routeList[rInt].SRC
                                    , this.routeList[rInt].DST
                                    , this.routeList[rInt].NAPName
                                    , this.routeList[rInt].IP
                                    , DateTime.Now
                                    , DateTime.Now
                                    )).Encode();

                            //Console.WriteLine(sTrySendFinal);
                            Log.Write(sTrySendFinal);

                            byte[] bDataFinal = Encoding.ASCII.GetBytes(sTrySendFinal);

                            this.routeList[rInt].IfSuccess = true;
                            this.routeList[rInt].SuccessTime = DateTime.Now;

                            this.parent.client.Send(bDataFinal);

                        }
                    }
                    break;
                case (CallState.FailCozInterrupt):
                    {
                        Random r = new Random();
                        int rInt = r.Next(0, this.routeList.Count - 1);

                        for (int j = 0; j < rInt; j++)
                        {
                            string sTrySend = (new CMD_OUTGOINGTRYFAIL(
                                this.CallID
                                , this.routeList[j].SRC
                                , this.routeList[j].DST
                                , this.routeList[j].NAPName
                                , this.routeList[j].IP
                                , DateTime.Now
                                , DateTime.Now
                                , DateTime.Now
                                )).Encode();

                            //Console.WriteLine(sTrySend);
                            Log.Write(sTrySend);
                            byte[] bData = Encoding.ASCII.GetBytes(sTrySend);
                            this.parent.client.Send(bData);
                            //this.parent.SleepMS(200);
                        }

                    }
                    break;
            }    //  end switch 



        }
    }

}

