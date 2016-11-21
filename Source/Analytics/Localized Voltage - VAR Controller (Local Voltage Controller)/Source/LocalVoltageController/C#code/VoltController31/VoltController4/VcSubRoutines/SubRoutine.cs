using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoltController.VcControlDevice;

namespace VoltController.VcSubRoutines
{
    public class SubRoutine
    {

        #region [ Priovate Members ]

        private string m_messageInput;

        #endregion

        #region [ Properties ]

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

        #region [ Private Methids ]
        #region [ ConTap ]
        private VoltVarController ConTap(int i, string Control, string Description, VoltVarController vc)
        {
            vc.LtcStatus.Cfail = 0;
            //set_clear_alarm;
            if (i == -1)
            {
                m_messageInput += string.Format("CON TAP Control Deselected No control |");
            }
            else
            {
                m_messageInput += string.Format("Control Decision {0} {1} {2} |", vc.ControlTransformers[i].DeviceId,
                    vc.ControlTransformers[i].LtcCtlId, Control);
                
                vc.ControlTransformers[i].PrevCtl = Control;
                vc.ControlTransformers[i].CtlDone = 1;

                vc.SubstationInformation.Ntdel = 0;  // Once the tap is operated, the delay counting is reset.
                vc.SubstationInformation.ConsecTap = 0;
            }
            // select undef, undef, undef, 1.00 delay fraction of second
            return vc;
        }
        #endregion

        #region [ CapStat ]
        private VoltVarController CapStat(int i, VoltVarController vc)
        {
            if (vc.ControlCapacitorBanks[i].ScadaSwV == vc.SubstationInformation.OFF)
            {
                vc.ControlCapacitorBanks[i].InSvc = 0;
            }

            if (vc.ControlCapacitorBanks[i].MiscV == vc.SubstationInformation.Alarm || vc.ControlCapacitorBanks[i].MiscV == vc.SubstationInformation.OFF)
            {
                vc.ControlCapacitorBanks[i].InSvc = 0;
            }

            if (vc.ControlCapacitorBanks[i].BusBkrV == null)
            {
                m_messageInput += string.Format("CAP I = {0} Undefed bits set {1} {2} |", i, vc.SubstationInformation.SubDevId, vc.ControlCapacitorBanks[i].BusBkrId);
                vc.ControlCapacitorBanks[i].InSvc = 0;    
            }

            if (vc.ControlCapacitorBanks[i].BusBkrV != vc.SubstationInformation.Close)
            {
                vc.ControlCapacitorBanks[i].InSvc = 0;
            }

            if (vc.ControlCapacitorBanks[i].CapBkrV == null)
            {
                m_messageInput += string.Format("CAP I = {0} Undefed bits set {1} {2} |", i, vc.SubstationInformation.SubDevId, vc.ControlCapacitorBanks[i].CapBkrV);
                vc.ControlCapacitorBanks[i].InSvc = 0;
            }

            if (vc.ControlCapacitorBanks[i].LockvV < vc.SubstationAlarmDevice.ZVLO || vc.ControlCapacitorBanks[i].LockvV > vc.SubstationAlarmDevice.ZVHI)
            {
                vc.ControlCapacitorBanks[i].InSvc = 0;
            }

            return vc;
        }
        #endregion

        #region [ ConCap ]
        private VoltVarController ConCap(int i, string CNAME, VoltVarController vc)
        {
        
            if(CNAME == vc.SubstationInformation.Close)
            {
                if (vc.ControlCapacitorBanks[i].CloseEx == 0)
                {
                    m_messageInput += string.Format("Exceeded MAX Control Count {0} {1} {2} {3}|", CNAME, i, vc.ControlCapacitorBanks[i].CapCtlDev, vc.ControlCapacitorBanks[i].CapCtlId);
                    vc.ControlCapacitorBanks[i].CloseEx = 1;
                }
                else
                {
                    vc.ControlCapacitorBanks[i].NcClose = vc.ControlCapacitorBanks[i].NcClose + 1;
                   // ret = &sw_db::write_point($CP[$i]{ cap_ctl_dev}, $CP[$i]{ cap_ctl_id} , $CNAME);
                    vc.SubstationInformation.Ncdel = 0;
                    m_messageInput += string.Format("CapControl {0} {1} {2} {3}|", vc.ControlCapacitorBanks[i].CapCtlDev, vc.ControlCapacitorBanks[i].CapCtlId, CNAME, vc.ControlCapacitorBanks[i].LockvV);
                    
                }
                
            }
            else if (CNAME == vc.SubstationInformation.Trip)
            {
                if (vc.ControlCapacitorBanks[i].NcTrip >= vc.SubstationInformation.Zmaxtrip)
                {
                    if (vc.ControlCapacitorBanks[i].TripEx == 0)
                    {
                        m_messageInput += string.Format("Exceeded MAX Control Count {0} {1} {2}|", i, vc.ControlCapacitorBanks[i].CapCtlDev, vc.ControlCapacitorBanks[i].CapCtlId);
                        vc.ControlCapacitorBanks[i].TripEx = 1;
                    }
                }
                else
                {
                    vc.ControlCapacitorBanks[i].NcTrip = vc.ControlCapacitorBanks[i].NcTrip + 1;
                    vc.SubstationInformation.Ncdel = 0;
                    m_messageInput += string.Format("CapControl {0} {1} {2} {3}|", vc.ControlCapacitorBanks[i].CapCtlDev, vc.ControlCapacitorBanks[i].CapCtlId, CNAME, vc.ControlCapacitorBanks[i].LockvV);
       
                }
            }
            return vc;
        }

        #endregion

        #endregion

        #region [ Public Methods ]

        #region [ Taps ]
        public VoltVarController Taps(VoltVarController vc)
        {
            #region [ check if LTC Taps are too far apart to continue]

            if (vc.LtcStatus.DifTap > vc.SubstationAlarmDevice.ZDIFTAP)
            {
                //set clear alarm
                m_messageInput += string.Format("LTCs are too far apart must EXIT {0} {1} |", vc.LtcStatus.DifTap, vc.SubstationAlarmDevice.ZDIFTAP);
            }
            else
            {
                //set clear alarm
            }

            #endregion

            #region [ Check TimeStamps. Make Sure the Last Voltage TimeStamp is more recent than the last TimeStamp ]

            //for (int i = 0; i < vc.SubstationAlarmDevice.ZNTX; i++)
            //{
            //    if (vc.ControlTransformers[i].VoltsTime > vc.ControlTransformers[i].TapTime)
            //    {
            //        m_messageInput += string.Format("TS last volts time = {0} > TS last control = {1} no Contrl ", vc.ControlTransformers[i].VoltsTime, vc.ControlTransformers[i].TapTime);
            //        //save before exit
            //    }

            //    if (vc.LtcStatus.Avv >= vc.SubstationAlarmDevice.VLLIM && vc.LtcStatus.Avv < vc.SubstationAlarmDevice.VHLIM)
            //    {
            //        if (vc.LtcStatus.BalMvr <= vc.SubstationAlarmDevice.ZBAL)
            //        {
            //            m_messageInput += string.Format("Normal Exit KV= {0} OK & MVRs= {1} balanced ", vc.LtcStatus.Avv, vc.LtcStatus.BalMvr);

            //        }
            //    }

            //}

            #endregion

            #region [ Check for ZCONS consecutive trigers, or the use by the operators of the command which forces us to take action immediately ]

            if (vc.SubstationInformation.ConsecTap < vc.SubstationAlarmDevice.ZTCONS)
            {
                vc.SubstationInformation.ConsecTap = vc.SubstationInformation.ConsecTap + 1;
                
                m_messageInput += string.Format("Not enough Counts yet = {0} < {1} |", vc.SubstationInformation.ConsecTap, vc.SubstationAlarmDevice.ZTCONS);

                return vc;
            }
            #endregion

            else
            {

 

                #region [ Decide which Transformer we would prefer to use ]

                //-----------------------------------------------------------------------#
                // If the megavars are unbalanced, we are only going to tap-change on    #
                // one transformer. Decide which transformer we would prefer to use.	#
                //                                   #
                // If we are here because the voltage is out of range:	         	#
                //                                   #
                // For a raise, we consider only transformers which are in		#
                // service and not already on the highest tap. We select the one	#
                // with the lowest megavars.					#
                //                                   #
                // For a lower, we consider only transformers which are in		#
                // service and not already on the lowest tap. We select the one	#
                // with the highest megavars.					#
                //                                   #
                // If the voltage is in range, and we are here only because the	#
                // megavars are unbalanced:					#
                //                                   #
                // For a raise, we consider only transformers which are in		#
                // service. We select the one with the lowest megavars. If it is	#
                // already on the highest tap, we give up.				#
                //                                   #
                // For a lower, we consider only transformers which are in		#
                // service. We select the one with the highest megavars. If it is	#
                // already on the lowest tap, we give up.				#
                //---------------------------------------------------------------------- #

                //------------------------------------------------------------#
                // Set up megavar unbalance , initial low and high MVRs,      #
                // Set up MIN and MAX MVRs and save , Check to make sure      #
                // not on lowest or highest tap, if they are deselect tap     #
                //------------------------------------------------------------#

                if (vc.LtcStatus.BalMvr > vc.SubstationAlarmDevice.ZBAL)
                {
                    for (int i = 0; i < vc.SubstationAlarmDevice.ZNTX; i++)
                    {
                        if (vc.ControlTransformers[i].InSvc != 0)
                        {
                            if (vc.ControlTransformers[i].TapV >= vc.SubstationAlarmDevice.ZHITAP && vc.ControlTransformers[i].MvrV == vc.LtcStatus.MinVar)
                            {
                                vc.LtcStatus.RTX = -1; // RTX on Highest Tap, we give up
                            }
                            else if (vc.ControlTransformers[i].TapV <= vc.SubstationAlarmDevice.ZLOTAP && vc.ControlTransformers[i].MvrV == vc.LtcStatus.MaxVar)
                            {
                                vc.LtcStatus.LTX = -1; // RTX on Lowest Tap, we give up
                            }
                        }
                    }
                }

                // Start the Control!
                if (vc.LtcStatus.Avv < vc.SubstationAlarmDevice.VLLIM)
                {
                    if (vc.LtcStatus.BalMvr > vc.SubstationAlarmDevice.ZBAL) // if the balance is out of range, we control the Tx with lowest megavars
                    {
                        vc = ConTap(vc.LtcStatus.RTX, vc.SubstationInformation.Raise, "KV =", vc); 
                    }
                    else
                    {
                        for (int i = 0; i < vc.SubstationAlarmDevice.ZNTX; i++) // if the balance is within the range, we control both Tx.
                        {
                            if (vc.ControlTransformers[i].InSvc != 0)
                            {
                                if (vc.ControlTransformers[i].TapV < vc.SubstationAlarmDevice.ZHITAP)
                                {
                                    vc = ConTap(i, vc.SubstationInformation.Raise, "KV=", vc);
                                }
                                else
                                {
                                    vc = ConTap(-1, vc.SubstationInformation.Raise, "KV=", vc); // if it reaches the highest tap, we give up.
                                }
                            }
                        }
                    }
                }
                else if (vc.LtcStatus.Avv >= vc.SubstationAlarmDevice.VHLIM)
                {
                    if (vc.LtcStatus.BalMvr > vc.SubstationAlarmDevice.ZBAL)
                    {
                        ConTap(vc.LtcStatus.LTX, vc.SubstationInformation.Lower, "KV = ", vc);
                    }
                    else
                    {
                        for (int i = 0; i < vc.SubstationAlarmDevice.ZNTX; i++)
                        {
                            if (vc.ControlTransformers[i].InSvc != 0)
                            {
                                if (vc.ControlTransformers[i].TapV > vc.SubstationAlarmDevice.ZLOTAP)
                                {
                                    vc = ConTap(i, vc.SubstationInformation.Lower, "KV = ", vc);
                                }
                                else
                                {
                                    vc = ConTap(-1, vc.SubstationInformation.Lower, "KV = ", vc);
                                }
                            }
                        }
                    }
                }
                else if (vc.LtcStatus.BalMvr > vc.SubstationAlarmDevice.ZBAL)
                {
                    if (vc.LtcStatus.Avv > (vc.SubstationAlarmDevice.VLLIM + vc.SubstationAlarmDevice.VHLIM) / 2.0)
                    {
                        vc = ConTap(vc.LtcStatus.LTX, vc.SubstationInformation.Lower, "MVR SPREAD = ", vc);
                        
                    }
                    else
                    {
                        vc = ConTap(vc.LtcStatus.RTX, vc.SubstationInformation.Raise, "MVR SPREAD = ", vc);
                        
                    }
                }
                else
                {
                    m_messageInput += string.Format("Sub Taps No Conrtols Needed this Cycle |");
                }



                #endregion
                return vc;
            }

        }
        #endregion

        #region [ CapBank ]
        public VoltVarController CapBank(VoltVarController vc)
        {

            //--------------------------------------------------------- #
            //---- Order of Cap bank operation -----
            //1. First  Close 1   SC242
            //2. Second Close 2   SC242
            //3. First  Trip  2   SC242
            //4. Second Trip  1   SC242
            //--------------------------------------------------------- #

            int co = 3;  // 3 means no capbanks require control
            string cName = null;
            double GtMvr = vc.SubstationInformation.G1Mvr + vc.SubstationInformation.G2Mvr;

            for (int i =0; i < vc.SubstationAlarmDevice.ZNCP; i ++ )
            {
                if (vc.ControlCapacitorBanks[i].OpCapV != null)
                {
                    if (vc.ControlCapacitorBanks[i].OpCapV != "ON")
                    {
                        vc.ControlCapacitorBanks[i].InSvc = 0;
                        m_messageInput += string.Format("op Cap Bank Control {0} {1} {2} |", i, vc.ControlCapacitorBanks[i].OpCapDev, vc.ControlCapacitorBanks[i].OpCapId);
                    }
                    else
                    {
                        vc.ControlCapacitorBanks[i].InSvc = 1;

                        // --------------------------------------------------------------------- #
                        // Check status availability. If any statuses are unavailable (i.e.,	#
                        // telemetry is failed on the digital word), just set the field status	#
                        // for this capacitor to bad. We perform this two-step process so that	#
                        // we will be able to perform our voltage out-of-range checks even if	#
                        // one capacitor has telemetry failure.					#
                        // --------------------------------------------------------------------- #
                        // ---  Get the statuses of the cap banks for later checking --- #

                        CapStat(i, vc);
                    }
                }
                else
                {
                    m_messageInput += string.Format("Cap Bank Control {0} {1} {2} Undefed bits set |", i, vc.ControlCapacitorBanks[i].CapCtlDev, vc.ControlCapacitorBanks[i].OpCapId);
                    vc.ControlCapacitorBanks[i].InSvc = 0;
                }
            }

            for (int i =0; i < vc.SubstationAlarmDevice.ZNCP; i ++ )
            {
                if (vc.ControlCapacitorBanks[i].InSvc != 0)
                {
                    m_messageInput += string.Format("{0} {1} {2} {3} {4} < {5} or gtvr = {6} |", i, vc.ControlCapacitorBanks[i].CapCtlDev, vc.ControlCapacitorBanks[i].CapCtlId, vc.ControlCapacitorBanks[i].CapBkrV, vc.ControlCapacitorBanks[i].LockvV, vc.ControlCapacitorBanks[i].Clov, GtMvr);
                    if (vc.ControlCapacitorBanks[i].LockvV < vc.ControlCapacitorBanks[i].Clov || ((GtMvr > vc.SubstationInformation.Zclmvr) && (vc.ControlCapacitorBanks[i].LockvV < vc.ControlCapacitorBanks[i].Alovc)))
                    {
                        if (vc.ControlCapacitorBanks[i].CapBkrV != vc.SubstationInformation.Close)
                        {
                            co = i;
                            cName = vc.SubstationInformation.Close;
                           
                        }
                    }
                    else if (vc.ControlCapacitorBanks[i].LockvV > vc.ControlCapacitorBanks[i].Chiv || ((GtMvr < vc.SubstationInformation.Ztrmvr) && (vc.ControlCapacitorBanks[i].LockvV > vc.ControlCapacitorBanks[i].Ahivt)))
                    {
                        if (vc.ControlCapacitorBanks[i].CapBkrV != vc.SubstationInformation.Trip)
                        {
                            co = i;
                            cName = vc.SubstationInformation.Trip;
                        }
                    }
                }

            }
        
        
        if (vc.SubstationInformation.ConsecCap < vc.SubstationInformation.Zccons || vc.SubstationInformation.Ncdel < vc.SubstationInformation.Zcdel || vc.SubstationInformation.Ntdel < vc.SubstationInformation.Zdel)
        {
            vc.SubstationInformation.ConsecCap = vc.SubstationInformation.ConsecCap + 1;
            m_messageInput += string.Format("Not enough CAPBANK Counts {0} < {1} {2} < {3}  {4} < {5}|", vc.SubstationInformation.ConsecCap, vc.SubstationInformation.Zccons, vc.SubstationInformation.Ncdel, vc.SubstationInformation.Zcdel, vc.SubstationInformation.Ntdel, vc.SubstationInformation.Zdel);
            co = 3;
        }

        if (co != 3)
        {
            vc = ConCap(co, cName, vc);
            vc.SubstationInformation.ConsecCap = 0;
        }
            
        return vc;

        }
       
        #endregion


        #endregion


    }
}
