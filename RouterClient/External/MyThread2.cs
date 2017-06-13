using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;

namespace CDRCatcher
{
    public abstract class MyThread2
    {
        System.Threading.Thread threadWorker = null;
        bool started = false;
        bool stopped = false;
        object session_lock = new object();

        public void Start()
        {
            if (!started)
            {
                OnStart();

                this.started = true;
                this.stopped = false;

                threadWorker = new Thread(Worker);
                threadWorker.IsBackground = false;
                threadWorker.Priority = ThreadPriority.Normal;
                threadWorker.Start();
            }
        }

        public void Stop()
        {
            if (this.started)
            {
                this.started = false;
                while (!this.stopped)
                {
                    Thread.Sleep(1000);
                    Log.Info("wait for stopped signal...");
                }

                OnStop();
            }
        }

        protected object Locker { get { return this.session_lock; } }

        //int mSleepAfterJob = 0;
        //protected int SleepAfterJob
        //{
        //    get
        //    {
        //        return mSleepAfterJob;
        //    }
        //    set
        //    {
        //        if (value < 0)
        //            mSleepAfterJob = 0;
        //        else
        //            mSleepAfterJob = value;
        //    }
        //}

        #region interface
        protected virtual void OnStart()
        {
        }

        protected virtual void OnStop()
        {
        }

        protected abstract void OnWork();
        #endregion

        #region Worker

        void Worker()
        {
            Log.Info("Checker is working.");
            while (true)
            {
                if (!started)
                    break;

                try
                {
                    OnWork();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }

                //if (mSleepAfterJob > 0)
                //    SleepMS(mSleepAfterJob);
            }
            stopped = true;
            Log.Info("Checker stopped.");
        }

        public void Sleep2(int seconds)
        {
            SleepMS(seconds * 1000);
        }

        public void SleepMS(int timeMS)
        {
            int temp = timeMS;
            while (temp > 0)
            {
                if (!started)
                {
                    Log.Info("Thread is stopping, cancel sleep.");
                    return;
                }

                temp = temp - 100;
                Thread.Sleep(100);
            }
        }

        #endregion

    }

    public class Schedule
    {
        int interval;
        DateTime last_time;

        public Schedule(int seconds)
            : this(seconds, true)
        {
        }

        public Schedule(int seconds, bool right_now)
        {
            this.interval = seconds;
            if (right_now)
                this.last_time = DateTime.MinValue;
            else
                this.last_time = DateTime.Now;
        }

        public bool Expired
        {
            get
            {
                bool expired = last_time.AddSeconds(interval) < DateTime.Now;
                if (expired)
                    last_time = DateTime.Now;
                return expired;
            }
        }
    }

}