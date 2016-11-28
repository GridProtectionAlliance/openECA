//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using VoltController.VcControlDevice;
//using VoltController.VcMessages;
//using VoltController.VcSubRoutines;

//namespace VoltController.VcReadCurrentControl
//{
//    public class ReadCurrentTxValuesAndVoltages
//    {
  
//        #region[ Read All Transformer Values and Voltage ]
//        public void ReadCurrentTxValuesAndVoltage()
//        {
//            VcSubstationAlarmDevice VcSubAlarm = new VcSubstationAlarmDevice();
//            for (int Indextransformer = 0; Indextransformer < VcSubAlarm.ZNTX; Indextransformer++)
//            {
//                CheckTxLocalAndRemoteSwitch(Indextransformer);
//                CheckScaDaControlSwitch(Indextransformer);
//                CheckTransformerHighSideBreaker(Indextransformer);
//                CheckTransformerLowSideBreaker(Indextransformer);
//                CheckTransformerBusVoltages(Indextransformer);
//                CheckTransformerWattsandVars(Indextransformer);
//                CheckTransformerLTCTapPosition(Indextransformer);
//                CheckNumTransformersInService(Indextransformer);

//            }

//        }
//        #endregion

//        #region[ Private Methods ]
//        #region [ Check the ith Transformer and its Remote Switch ]
//        private void CheckTxLocalAndRemoteSwitch(int i)
//        {
//            VoltVarController VC = new VoltVarController();
//            VcSubstationInfomation VcSubInfo = new VcSubstationInfomation();
//            ProgLogMessage PM = new ProgLogMessage();
//            BellyUps BU = new BellyUps();

//            //VC.ControlTransformers[i].LocRemV = &sw_db::read_and_verify_point($TX[$i]{ device_id}, $TX[$i]{ loc_rem_id}, @quit_bits);
//            VC.ControlTransformers[i].LocRemV = 115; // this value is given randamlly;

//            if (VC.ControlTransformers[i].LocRemV != 0)   // it should be null here    //bug
//            {
//                if (Convert.ToString(VC.ControlTransformers[i].LocRemV) != VcSubInfo.Remote) // locRemV is int but remote is string //bug
//                {
//                    VC.ControlTransformers[i].InSvc = 0;
//                    PM.PrintMessage(string.Format("loc_rem not in service {0} {1} {2} = {3}",
//                            VC.ControlTransformers[i].DeviceId,
//                            VC.ControlTransformers[i].LocRemId,
//                            VC.ControlTransformers[i].LocRemV));
//                }
//                else
//                {
//                    VC.ControlTransformers[i].InSvc = 1;
//                }
//            }
//            else
//            {
//                BU.BellyUp(string.Format("Undefed or bits set {0} {1}}", VC.ControlTransformers[i].DeviceId, VC.ControlTransformers[i].LocRemId));
//            }
//        }

//        #endregion

//        #region[ Check Scada Control Switch ]
//        private void CheckScaDaControlSwitch(int i)
//        {

//            VoltVarController VC = new VoltVarController();
//            VcSubstationInfomation VcSubInfo = new VcSubstationInfomation();
//            ProgLogMessage PM = new ProgLogMessage();
//            BellyUps BU = new BellyUps();

//            //VC.ControlTransformers[i].ScadaSwV= &sw_db::read_and_verify_point($TX[$i]{ device_id}, $TX[$i]{ loc_rem_id}, @quit_bits);
//            VC.ControlTransformers[i].ScadaSwV = 115; // this value is given randamlly;
//            if (VC.ControlTransformers[i].ScadaSwV != 0)
//            {
//                if(Convert.ToString(VC.ControlTransformers[i].ScadaSwV) != VcSubInfo.ON) //  ScadaSWV is not string //bug
//                {
//                    VC.ControlTransformers[i].InSvc = 0;
//                    PM.PrintMessage(string.Format("SCADA SW Out of Service = {0}",
//                            VC.ControlTransformers[i].ScadaSwV));
//                }
//            }
//            else
//            {
//                BU.BellyUp(string.Format("Undefed or bits set{0} {1}",
//                            VC.ControlTransformers[i].DeviceId, VC.ControlTransformers[i].ScadaSw));
//            }

//        }
//        #endregion

//        #region[ Check Transoformer High Side Breaker ]
//        private void CheckTransformerHighSideBreaker(int i)
//        {
//            VoltVarController VC = new VoltVarController();
//            VcSubstationInfomation VcSubInfo = new VcSubstationInfomation();
//            ProgLogMessage PM = new ProgLogMessage();
//            BellyUps BU = new BellyUps();

//            //VC.ControlTransformers[i].HighSideV= &sw_db::read_and_verify_point($TX[$i]{ device_id}, $TX[$i]{ loc_rem_id}, @quit_bits);
//            VC.ControlTransformers[i].HighSideV = 0; // this value is given randomlly;
//            if (VC.ControlTransformers[i].HighSideV != 0)
//            {
//                if (Convert.ToString(VC.ControlTransformers[i].HighSideV) == VcSubInfo.Trip)
//                {
//                    VC.ControlTransformers[i].InSvc = 0;
//                    PM.PrintMessage(string.Format("High side breaker open = {0}, {1}, {2}", 
//                        VC.ControlTransformers[i].DeviceId, 
//                        VC.ControlTransformers[i].HighSideId, 
//                        VC.ControlTransformers[i].HighSideV));         
//                }
              
//            }
//            else
//            {
//                VC.ControlTransformers[i].InSvc = 0;
//                BU.BellyUp(string.Format("Undefed or bits set{0} {1}",
//                            VC.ControlTransformers[i].DeviceId, VC.ControlTransformers[i].HighSideId, VC.ControlTransformers[i].HighSideV));
//            }

//        }
//        #endregion

//        #region[ Check Transformer Low Side Breaker ]
//        private void CheckTransformerLowSideBreaker(int i)
//        {
//            VoltVarController VC = new VoltVarController();
//            VcSubstationInfomation VcSubInfo = new VcSubstationInfomation();
//            ProgLogMessage PM = new ProgLogMessage();
//            LtcLogMessages LM = new LtcLogMessages();
//            BellyUps BU = new BellyUps();

//            //VC.ControlTransformers[i].LowSideV= &sw_db::read_and_verify_point($TX[$i]{ device_id}, $TX[$i]{ loc_rem_id}, @quit_bits);
//            VC.ControlTransformers[i].LowSideV = 0; // this value is given randomlly;
//            if(VC.ControlTransformers[i].LowSideV != 0)
//            {
//                if(Convert.ToString(VC.ControlTransformers[i].LowSideV) == VcSubInfo.Trip)
//                {
//                    VC.ControlTransformers[i].InSvc = 0;
//                    PM.PrintMessage(string.Format("lsb {0} TX out of service = {1}", VC.ControlTransformers[i].LowSideId,VC.ControlTransformers[i].LowSideV));
//                }
//            }
//            else
//            {
//                VC.ControlTransformers[i].InSvc = 0;
//                LM.LtcLogMessage(string.Format("Undefed or bits set{0} {1}",
//                            VC.ControlTransformers[i].DeviceId, VC.ControlTransformers[i].LowSideId));
//            }

//        }

//        #endregion

//        #region[ Check Transformer Bus Voltages ]
//        private void CheckTransformerBusVoltages(int i)
//        {
//            VoltVarController VC = new VoltVarController();
//            VcSubstationInfomation VcSubInfo = new VcSubstationInfomation();
//            ProgLogMessage PM = new ProgLogMessage();
//            LtcLogMessages LM = new LtcLogMessages();
//            BellyUps BU = new BellyUps();
//            TimeChecks TC = new TimeChecks();
//            VcSubstationAlarmDevice VcSubAlarm = new VcSubstationAlarmDevice();

//            //VC.ControlTransformers[i].VoltsV = &sw_db::read_and_verify_point($TX[$i]{ device_id}, $TX[$i]{ volts_id}, @quit_bits);
//            //VC.ControlTransformers[i].VoltsTime = &time_check($TX[$i]{ device_id}, $TX[$i]{ volts_id});
//            VC.ControlTransformers[i].VoltsV = 116;
//            VC.ControlTransformers[i].VoltsTime = TC.Timecheck();
//            if (VC.ControlTransformers[i].VoltsV != 0)   // should be VC.ControlTransformers[i].VoltsV != null // bug
//            {
//                if(VC.ControlTransformers[i].VoltsV < VcSubAlarm.ZVLO || VC.ControlTransformers[i].VoltsV > VcSubAlarm.ZVHI)
//                {
//                    VC.ControlTransformers[i].InSvc = 0;
//                    PM.PrintMessage(string.Format("Volts out of range {0} {1} = {2} set alarm",
//                            VC.ControlTransformers[i].DeviceId, VC.ControlTransformers[i].VoltsId,VC.ControlTransformers[i].VoltsV));
//                }
//                else if(VC.ControlTransformers[i].VoltsV < VcSubAlarm.VLLIM)
//                {
//                    VC.ControlTransformers[i].IRange = "LOW";
//                }
//                else if (VC.ControlTransformers[i].VoltsV > VcSubAlarm.VHLIM)
//                {
//                    VC.ControlTransformers[i].IRange = "HI";
//                }
//                else
//                {
//                    VC.ControlTransformers[i].IRange = VcSubInfo.Normal;
//                }
//            }
//        }
//        #endregion

//        #region [ Check Transformer Watts and Vars ]
//        private void CheckTransformerWattsandVars(int i)
//        {
//            VoltVarController VC = new VoltVarController();
//            ProgLogMessage PM = new ProgLogMessage();
//            BellyUps BU = new BellyUps();
//            VcSubstationAlarmDevice VcSubAlarm = new VcSubstationAlarmDevice();

//            // VC.ControlTransformers[i].MwV = sw_db::read_and_verify_point($TX[$i]{ device_id}, $TX[$i]{ mw_id}, @quit_bits);
//            VC.ControlTransformers[i].MwV = 1; // bug
//            VC.ControlTransformers[i].StMvrV = VC.ControlTransformers[i].MvrV; // save previous Tx MVR value b4 read
//            // VC.ControlTransformers[i].MwV = sw_db::read_and_verify_point($TX[$i]{ device_id}, $TX[$i]{ mw_id}, @quit_bits);
//            VC.ControlTransformers[i].MvrV = 1; // bug

//            if (VC.ControlTransformers[i].MwV != 0 && VC.ControlTransformers[i].MvrV != 0) // bug // should be != null
//            {
//                if (Math.Abs(VC.ControlTransformers[i].MwV) < VcSubAlarm.ZLOWV && Math.Abs(VC.ControlTransformers[i].MvrV) < VcSubAlarm.ZLOWV)
//                {
//                    VC.ControlTransformers[i].InSvc = 0;
//                    PM.PrintMessage(string.Format("Tx Watts - Vars {0} {1} {2}",
//                            VC.ControlTransformers[i].DeviceId, VC.ControlTransformers[i].MwId, VC.ControlTransformers[i].MvrId));
//                }
//            }
//            else
//            {
//                BU.BellyUp(string.Format("Undefed or bits set{0} {1}",
//                            VC.ControlTransformers[i].DeviceId, VC.ControlTransformers[i].MwId, VC.ControlTransformers[i].MvrId));
//            }
//        }

//            #endregion

//        #region [ Check Transformer LTC Tap Position ]
//        private void CheckTransformerLTCTapPosition(int i)
//        {
//            VoltVarController VC = new VoltVarController();
//            VcSubstationInfomation VcSubInfo = new VcSubstationInfomation();
//            ProgLogMessage PM = new ProgLogMessage();
//            LtcLogMessages LM = new LtcLogMessages();
//            BellyUps BU = new BellyUps();
//            TimeChecks TC = new TimeChecks();
//            VcSubstationAlarmDevice VcSubAlarm = new VcSubstationAlarmDevice();

//            VC.ControlTransformers[i].StTapV = VC.ControlTransformers[i].TapV;
//            //VC.ControlTransformers[i].TapV = &sw_db::read_and_verify_point($TX[$i]{ device_id}, $TX[$i]{ tap_id}, @quit_bits);
//            VC.ControlTransformers[i].TapV = 1; // bug
//            //VC.ControlTransformers[i].TapTime = &time_check($TX[$i]{ device_id}, $TX[$i]{ ltc_ctl_id});
//            VC.ControlTransformers[i].TapTime = TC.Timecheck(); // bug

//            if (VC.ControlTransformers[i].TapV != 0) // bug // should be != null
//            {
//                if(VC.ControlTransformers[i].TapV < VcSubAlarm.ZLOTAP || VC.ControlTransformers[i].TapV > VcSubAlarm.ZHITAP)
//                {
//                    VC.ControlTransformers[i].InSvc = 0;
//                    PM.PrintMessage(string.Format("LTC Tap Position Unreasonable{0} {1} = {2}",
//                            VC.ControlTransformers[i].DeviceId, VC.ControlTransformers[i].TapId, VC.ControlTransformers[i].TapV));
//                }
//            }
//            else
//            {
//                BU.BellyUp(string.Format("Undefed or bits set {0} {1} {2}",
//                            VC.ControlTransformers[i].DeviceId, VC.ControlTransformers[i].TapId, VC.ControlTransformers[i].TapV));
//            }
//        }
//        #endregion

//        #region [ Check Number of Transformers are in Service ]
//        private void CheckNumTransformersInService(int i)
//        {
//            VoltVarController VC = new VoltVarController();
//            VcSubstationInfomation VcSubInfo = new VcSubstationInfomation();
//            ProgLogMessage PM = new ProgLogMessage();
//            LtcLogMessages LM = new LtcLogMessages();
//            BellyUps BU = new BellyUps();
//            TimeChecks TC = new TimeChecks();
//            VcSubstationAlarmDevice VcSubAlarm = new VcSubstationAlarmDevice();
//            VcLtcStatus LTC = new VcLtcStatus();

//            if (VC.ControlTransformers[i].InSvc != 0)
//            {
//                LTC.Nins++;
//                LTC.Avv = LTC.Avv + VC.ControlTransformers[i].VoltsV;

//                if (VC.ControlTransformers[i].MvrV < LTC.MinVar)
//                {
//                    LTC.RTX = i;
//                    LTC.MinVar = VC.ControlTransformers[i].MvrV;
//                }
//                if (VC.ControlTransformers[i].MvrV > LTC.MaxVar)
//                {
//                    LTC.LTX = i;
//                    LTC.MinVar = VC.ControlTransformers[i].MvrV;
//                }

//                if (VC.ControlTransformers[i].TapV < LTC.MinTap)
//                {
//                    LTC.MinTap = VC.ControlTransformers[i].TapV;
//                }

//                if (VC.ControlTransformers[i].TapV > LTC.MaxTap)
//                {
//                    LTC.MaxTap = VC.ControlTransformers[i].TapV;
//                }
//            }
//            else
//            {
//                PM.PrintMessage(string.Format("{0} {1} is not in service {2}",
//                            VC.ControlTransformers[i].DeviceId, VC.ControlTransformers[i].LtcCtlId, VC.ControlTransformers[i].InSvc));
//            }
//        }

//        #endregion
//        #endregion
//    }

//}
