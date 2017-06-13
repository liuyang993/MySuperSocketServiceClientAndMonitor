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
        Schedule sc_get_cmd = new Schedule(10, true);
        void get_cmd()
        {
            try
            {
                if (!sc_get_cmd.Expired)
                    return;

                string I_CatcherID = "";
                string O_Result = "";
                int O_cmd_id = 0;
                string O_SwitchID = "";
                string O_cmd = "";
                string O_param1 = "";
                string O_param2 = "";
                string O_param3 = "";
                if (!DBCommands.sp_Catcher_get_cmd(I_CatcherID
                  , ref O_Result
                  , ref O_cmd_id
                  , ref O_SwitchID
                  , ref O_cmd
                  , ref O_param1
                  , ref O_param2
                  , ref O_param3))
                {
                    Log.Error(DBCommands.LastError);
                    return;
                }
                if (O_Result == "N")
                    return;

                Log.Write("Find new command: Result={0}, cmd_id={1} SwitchID={2}, cmd={3}, param1={4}, param2={5}, param3={6}", O_Result, O_cmd_id, O_SwitchID, O_cmd, O_param1, O_param2, O_param3);
                Program.frm.RemoteCmd(O_cmd_id, I_CatcherID, O_SwitchID, O_cmd.ToUpper(), O_param1, O_param2, O_param3);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

    }
}
