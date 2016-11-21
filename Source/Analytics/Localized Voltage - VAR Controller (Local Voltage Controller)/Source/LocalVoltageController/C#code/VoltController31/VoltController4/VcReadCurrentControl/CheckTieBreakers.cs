using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoltController.VcControlDevice;
using VoltController.VcMessages;

namespace VoltController.VcReadCurrentControl
{
    public class CheckTieBreakers
    {
        public void CheckTieBreaker()
        {
            VcSubstationInfomation VcSubInfo = new VcSubstationInfomation();
            VcSubstationAlarmDevice VcSubAlarm = new VcSubstationAlarmDevice();
            BellyUps BU = new BellyUps();

            // tie_v = sw_db::read_and_verify_point(farm_dev_id, tie_230_id, @Zquit_bits);
            VcSubInfo.TieV = 1;
            if (VcSubInfo.TieV != 0)    // "Value Type" in .NET (int) cannot, by definition, be null.
            {
                //if (tie_v != CLOSE)
                //{
                //    ibal = 0;                                       // can not balance mvrs tie open
                //}
            }
            else
            {
                BU.BellyUp(string.Format("undefed bits set or {0} = prog_stat", VcSubAlarm.LtcProgram));
            }
        }
    }
}
