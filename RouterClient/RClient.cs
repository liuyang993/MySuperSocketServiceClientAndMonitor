using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RouterClient
{
    public class RClient : MyThread2
    {
        private int mID;
        private  SortedList<string, RRequest> mPendingRequests;
        
        public RClient(int id)
        {
            this.mID = id;
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

        public int PendingInQueue
        {
            get
            {
                return this.mPendingRequests.Count;
            }
        }


        protected override void OnStart()
        {
            Log.Write("Router client[" + this.mID + "] started.");
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
                DateTime now = DateTime.Now;
                if (this.lastReportAt == DateTime.MinValue
                     || this.lastReportAt.Hour != now.Hour)
                {
                    Log.Info("Hourly report: [id]: hour/day/total");

                    this.lastReportAt = now;
                }
                     
                requestID++;
                string call_id = ("00" + mID.ToString()).RightString(2)
                    + ("00000000" + requestID.ToString()).RightString(8);
                RRequest request = new RRequest(call_id);
                request._Type = Rand(INIT-- - START-- - TERMINATE)
                mPendingRequests.Add(request.CallID, request);

                sendRequest(request);

                int sleep = rand(1-- - 1000);
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

            mPendingRequests.IndexOfKey(callid)
                            
            //update mPendingRequests 

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
