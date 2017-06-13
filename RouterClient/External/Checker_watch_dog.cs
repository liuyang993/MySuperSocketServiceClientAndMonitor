using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Data;
using MyLib;

namespace CDRCatcher
{
    public partial class Checker : MyThread2
    {
        Schedule sc_watch_dog = new Schedule(3, true);
        void watch_dog()
        {
            try
            {
                if (!sc_watch_dog.Expired)
                    return;

                /* int rtn = */ FeedDog("CDRCatcher");
                //if (rtn != 0)
                //    Log.Write("Feed dog failed.");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

    }
}
