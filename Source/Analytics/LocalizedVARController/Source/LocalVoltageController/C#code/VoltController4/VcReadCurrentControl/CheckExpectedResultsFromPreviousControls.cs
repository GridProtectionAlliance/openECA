using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoltController.VcControlDevice;
using VoltController.VcMessages;

namespace VoltController.VcReadCurrentControl
{
    public class CheckExpectedResultsFromPreviousControls
    {
        

        #region[ Check any controls and see if they match the expectation]
        public void CheckExpectedResultsFromPreviousControl()
        {
            VcSubstationAlarmDevice VcSubAlarm = new VcSubstationAlarmDevice();
            for (int Indextransformer = 0; Indextransformer < VcSubAlarm.ZNTX; Indextransformer++)
            {
                CheckIfControlDoneLastCycle(Indextransformer);
            }
        }
        #endregion

        #region[ Private Methods ]
        private void CheckIfControlDoneLastCycle(int i)
        {
            VoltVarController VC = new VoltVarController();
            VcSubstationInfomation VcSubInfo = new VcSubstationInfomation();
            ProgLogMessage PM = new ProgLogMessage();
            BellyUps BU = new BellyUps();
            LtcLogMessages LM = new LtcLogMessages();
            VcLtcStatus LTC = new VcLtcStatus();

            if (VC.ControlTransformers[i].CtlDone != 0)
            {
                double TapMove = Math.Abs(VC.ControlTransformers[i].StTapV - VC.ControlTransformers[i].TapV);
                double MvrMove = Math.Abs(VC.ControlTransformers[i].StMvrV - VC.ControlTransformers[i].MvrV);
                if (TapMove < 0.2 && MvrMove < 0.2)
                {
                    LM.LtcLogMessage(string.Format("Control Failed {0} {1} {2} {3}",
                            VC.ControlTransformers[i].DeviceId, VC.ControlTransformers[i].LtcCtlId, VC.ControlTransformers[i].PrevCtl, VC.ControlTransformers[i].TapV));

                    LTC.Cfail++;
                }
                VC.ControlTransformers[i].CtlDone = 0;
            }

        }

        #endregion
    }
}