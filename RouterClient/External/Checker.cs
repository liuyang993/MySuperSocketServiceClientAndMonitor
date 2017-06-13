using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Data;
using MyLib;
using System.Runtime.InteropServices;

namespace CDRCatcher
{
    public partial class Checker : MyThread2
    {     

        protected override void OnStart()
        {
            Log.Write("Remote control started.");         
        }

        protected override void OnStop()
        {
            Log.Write("Remote control stopped.");
        }

        protected override void OnWork()
        {
            lock (Locker)
            {
            }

            try
            {
                //do something


                Sleep2(10);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }      

          
        } 

    }
}
