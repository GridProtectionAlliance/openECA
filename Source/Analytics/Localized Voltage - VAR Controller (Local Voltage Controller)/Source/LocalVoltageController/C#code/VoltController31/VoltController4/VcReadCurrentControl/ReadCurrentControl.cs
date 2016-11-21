using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoltController.VcMessages;
using VoltController.VcControlDevice;



namespace VoltController.VcReadCurrentControl
{
    public class ReadCurrentControl
    {
        #region [ Private Members ]
        private string m_progStat;
        private string m_messageInput;

        #endregion

        #region [ Properties ]
        public string ProgStat
        {
            get
            {
                return m_progStat;
            }
            set
            {
                m_progStat = value;
            }
        }

        public string MessageInput
        {
            get
            {
                return m_messageInput;
            }
            set
            {
                m_messageInput = value;
            }
        }
        #endregion

        #region [ Public Methods ]

        public void VerifyProgramControl(string ProgStat)
        {
            ProgLogMessage ProgLogMsg = new ProgLogMessage();
            VcSubstationAlarmDevice VcSubAlarm = new VcSubstationAlarmDevice();
            BellyUps BU = new BellyUps();

            if (ProgStat != null)
            {
                //// Good Verify no bits set
                //string messageInput = string.Format("Program {0} {1} LTC state: {2}", VcSubAlarm.LtcDevice, VcSubAlarm.LtcProgram, ProgStat);
                //ProgLogMsg.PrintProgLogMessage(messageInput);
                //ProgLogMsg.LogMessage = messageInput;
                //ProgLogMsg.SerializeToXml(logFolderName);

                if (ProgStat == "ON")
                {
                    m_messageInput += string.Format("{0} {1} Program control is {2} |", VcSubAlarm.LtcDevice, VcSubAlarm.LtcProgram, ProgStat);
           
                }
            }
            else
            {
               m_messageInput += string.Format("Undefed bits set or {0} = {1} | ", VcSubAlarm.LtcProgram, ProgStat);
               
            }
            
        }

        public VoltVarController ReadCurrentTransformerValuesAndVoltages(VoltVarController vc)
        {
            for (int i = 0; i< vc.SubstationAlarmDevice.ZNTX; i++)
            {

                #region [ check Transformer Local/Remote Switch ]
                if (vc.ControlTransformers[i].LocRemV != vc.SubstationInformation.Remote)
                {
                    vc.ControlTransformers[i].InSvc = 0;
                    m_messageInput += string.Format("loc_Rem not in service {0} {1} {2} = {3} |", i, vc.ControlTransformers[i].DeviceId, vc.ControlTransformers[i].LocRemId, vc.ControlTransformers[i].LocRemV);

                }
                else
                {
                    vc.ControlTransformers[i].InSvc = 1;
                }

                #endregion

                #region [ Check Scada Control Switch ]

                if (vc.ControlTransformers[i].ScadaSwV != vc.SubstationInformation.ON)
                {
                    vc.ControlTransformers[i].InSvc = 0;
                    m_messageInput += string.Format("ScadaSw {0} SCADA SW OUT of Service =  {1} |", i, vc.ControlTransformers[i].ScadaSwV);

                }

                #endregion

                #region [ Check Transformer High Side Breaker ]
                if (Convert.ToString(vc.ControlTransformers[i].HighSideV) == vc.SubstationInformation.Trip)
                {
                    vc.ControlTransformers[i].InSvc = 0;
                    m_messageInput += string.Format("high side breaker open = {0} {1} {2} |", vc.ControlTransformers[i].DeviceId, vc.ControlTransformers[i].HighSideId, vc.ControlTransformers[i].HighSideV);
                }

                #endregion

                #region [ Check Transformer Low Side Breaker ]
                if (Convert.ToString(vc.ControlTransformers[i].LowSideV) == vc.SubstationInformation.Trip)
                {
                    vc.ControlTransformers[i].InSvc = 0;
                    m_messageInput = string.Format("Low side breaker open = {0} {1} {2} |", vc.ControlTransformers[i].DeviceId, vc.ControlTransformers[i].LowSideId, vc.ControlTransformers[i].LowSideV);

                }

                #endregion

                #region [ Check Transfromer Bus Voltage ]
                if (vc.ControlTransformers[i].VoltsV < vc.SubstationAlarmDevice.ZVLO || vc.ControlTransformers[i].VoltsV > vc.SubstationAlarmDevice.ZVHI)
                {
                    vc.ControlTransformers[i].InSvc = 0;
                    m_messageInput = string.Format("Volts out of range {0},{1} = {2}  set alarm |", vc.ControlTransformers[i].DeviceId, vc.ControlTransformers[i].VoltsId, vc.ControlTransformers[i].VoltsV);
                }

                #endregion

                #region [ Check Transformer Watts and Vars ]

                vc.ControlTransformers[i].StMvrV = vc.ControlTransformers[i].MvrV;
                if (Math.Abs(vc.ControlTransformers[i].MwV) < vc.SubstationAlarmDevice.ZLOWV && Math.Abs(vc.ControlTransformers[i].MvrV) < vc.SubstationAlarmDevice.ZLOWV)
                {
                    vc.ControlTransformers[i].InSvc = 0;
                    m_messageInput = string.Format("TX watts-vars {0} {1} {2} |", vc.ControlTransformers[i].DeviceId, vc.ControlTransformers[i].MwId, vc.ControlTransformers[i].MvrId);
                }

                #endregion

                #region [ Check Transformer LTC Tap Position ]
                if (vc.ControlTransformers[i].TapV < vc.SubstationAlarmDevice.ZLOTAP || vc.ControlTransformers[i].TapV > vc.SubstationAlarmDevice.ZHITAP)
                {
                    vc.ControlTransformers[i].InSvc = 0;
                    m_messageInput += string.Format("LTC Tap Position Unreasonable {0} {1} {2} ", vc.ControlTransformers[i].DeviceId, vc.ControlTransformers[i].TapId, vc.ControlTransformers[i].TapV);
                }

                #endregion

                #region [ For all Tx's that are in Service ]

                if (vc.ControlTransformers[i].InSvc != 0)
                {
                    vc.LtcStatus.Nins++;
                    vc.LtcStatus.Avv = vc.LtcStatus.Avv + vc.ControlTransformers[i].VoltsV;

                    if (vc.ControlTransformers[i].MvrV < vc.LtcStatus.MinVar)
                    {
                        vc.LtcStatus.RTX = i;
                        vc.LtcStatus.MinVar = vc.ControlTransformers[i].MvrV;
                    }

                    if (vc.ControlTransformers[i].MvrV > vc.LtcStatus.MaxVar)
                    {
                        vc.LtcStatus.LTX = i;
                        vc.LtcStatus.MaxVar = vc.ControlTransformers[i].MvrV;
                    }

                    if (vc.ControlTransformers[i].TapV < vc.LtcStatus.MinTap)
                    {
                        vc.LtcStatus.MinTap = vc.ControlTransformers[i].TapV;
                    }

                    if (vc.ControlTransformers[i].TapV > vc.LtcStatus.MaxTap)
                    {
                        vc.LtcStatus.MaxTap = vc.ControlTransformers[i].TapV;
                    }
                }
                else
                {
                    m_messageInput += string.Format("{0} {1} Transformer{2} is NOT service = {3}", vc.ControlTransformers[i].DeviceId, vc.ControlTransformers[i].LtcCtlId, i, vc.ControlTransformers[i].InSvc);
                }

                #endregion
            }

            return vc;
        }

        public VoltVarController CheckPreviousControlResults(VoltVarController vc)
        {
            // If we did any controls last time, check they had the expected results
            //  we did the controls, CLEAR CONTROL-DONE FLAG	
            for (int i = 0; i < vc.SubstationAlarmDevice.ZNTX; i++)
            {
                if (vc.ControlTransformers[i].CtlDone != 0) // if Control Was Done
                {
                    double tapmove = Math.Abs(vc.ControlTransformers[i].StTapV - vc.ControlTransformers[i].TapV);
                    double mvrmove = Math.Abs(vc.ControlTransformers[i].StMvrV - vc.ControlTransformers[i].MvrV);
                    if (tapmove < 0.2 && mvrmove < 0.2)
                    {
                        m_messageInput += string.Format("Control Failed {0} {1} {2} {3} |", vc.ControlTransformers[i].DeviceId, vc.ControlTransformers[i].LtcCtlId, vc.ControlTransformers[i].PrevCtl, vc.ControlTransformers[i].TapV);
                        vc.LtcStatus.Cfail++;
                    }

                    vc.ControlTransformers[i].CtlDone = 0;
                }
            }
          
            if (vc.LtcStatus.Cfail > 0)
            {
                //clear control alarm
            }
            else
            {
                //clear control alarm
            }

            if (vc.LtcStatus.Nins == 1)
            {
                vc.LtcStatus.BalMvr = 0;
                vc.LtcStatus.DifTap = 0;
            }
            else if (vc.LtcStatus.Nins > 1)
            {
                vc.LtcStatus.Avv = vc.LtcStatus.Avv / vc.LtcStatus.Nins;

                // set clear alarm //

                vc.LtcStatus.BalMvr = vc.LtcStatus.MaxVar - vc.LtcStatus.MinVar;
                vc.LtcStatus.DifTap = vc.LtcStatus.MaxTap - vc.LtcStatus.MinTap;
            }
            else
            {
                // if more than one transoformer is out of service, quit and set out of service alarm
                vc.SubstationInformation.ConsecTap = 0;
                //save before exit.
                
            }
            
            if (vc.LtcStatus.Avv < vc.SubstationAlarmDevice.ZVLO || vc.LtcStatus.Avv > vc.SubstationAlarmDevice.ZVHI)
            {
                m_messageInput += string.Format("KVs are out of range = {0} must exit |", vc.LtcStatus.Avv);
                //save before exist
            }

            return vc;

        }
        

        #endregion


    }
}
