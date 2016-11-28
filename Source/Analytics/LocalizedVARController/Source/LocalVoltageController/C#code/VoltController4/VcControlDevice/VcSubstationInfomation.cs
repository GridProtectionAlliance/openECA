using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace VoltController
{
    public class VcSubstationInfomation
    {
        #region [ Private Members ]

        // Substation Information

        private string m_subDevId;
        private string m_tieId;
        private int m_tieV;

        private string m_cloverDev;
        private string m_g1MwId;
        private string m_g1MvrId;
        private string m_g2MwId;
        private string m_g2MvrId;

        private double m_g1Mw;
        private double m_g1Mvr;
        private double m_g2Mw;
        private double m_g2Mvr;

        private int m_consecTap;
        private int m_consecCap;
        private int m_ncdel;
        private int m_ntdel;
        private int m_oldDay;

        // Cap Information
        private int m_zcdel;         // jjj FOR TST // minimum number of run
        private int m_zdel;           // minimum No of run after a cap control before a tap control, or vice versa
        private int m_zmaxtrip;       // max cap trips per day
        private int m_zmaxclose;      // max cap clsoes per day
        private int m_zccons;         // No. of consecutive triggers
        private double m_zclmvr;  // MVRS to close cap bank
        private double m_ztrmvr;  // MVRS to trip cap bank

        private string m_alarm;
        private string m_normal;
        private string m_raise;
        private string m_lower;
        private string m_on;

     //   private string m_controlOn;
        private string m_off;
        private string m_close;
        private string m_trip;
        private string m_remote;
        private string m_local;
        private string m_auto;
        private string m_manual;
        private string m_dashes;

       




        #endregion

        #region[ Properties ]

        [XmlAttribute("SubDevId")]
        public string SubDevId
        {
            get
            {
                return m_subDevId;
            }
            set
            {
                m_subDevId = value;
            }
        }
        [XmlAttribute("TieId")]
        public string TieId
        {
            get
            {
                return m_tieId;
            }
            set
            {
                m_tieId = value;
            }
        }

        [XmlAttribute("TieV")]
        public int TieV
        {
            get
            {
                return m_tieV;
            }
            set
            {
                m_tieV = value;
            }
        }

        [XmlAttribute("CloverDev")]
        public string CloverDev
        {
            get
            {
                return m_cloverDev;
            }
            set
            {
                m_cloverDev = value;
            }
        }
        [XmlAttribute("G1MwId")]
        public string G1MwId
        {
            get
            {
                return m_g1MwId;
            } 
            set
            {
                m_g1MwId = value;
            }
        }
        [XmlAttribute("G1MvrId")]
        public string G1MvrId
        {
            get
            {
                return m_g1MvrId; 
            }
            set
            {
                m_g1MvrId = value;
            }
        }

        [XmlAttribute("G2MwId")]
        public string G2MwId
        {
            get
            {
                return m_g2MwId;
            }
            set
            {
                m_g2MwId = value;
            }
        }

        [XmlAttribute("G2Mvrd")]
        public string G2MvrId
        {
            get
            {
                return m_g2MvrId;
            }
            set
            {
                m_g2MvrId = value;
            }
        }

        [XmlAttribute("ConsecTap")]
        public int ConsecTap
        {
            get
            {
                return m_consecTap;
            }
            set
            {
                m_consecTap = value;
            }
        }

        [XmlAttribute("G1Mw")]
        public double G1Mw
        {
            get
            {
                return m_g1Mw;
            }
            set
            {
                m_g1Mw = value;
            }
        }

        [XmlAttribute("G1Mvr")]
        public double G1Mvr
        {
            get
            {
                return m_g1Mvr;
            }
            set
            {
                m_g1Mvr = value;
            }
        }

        [XmlAttribute("G2Mw")]
        public double G2Mw
        {
            get
            {
                return m_g2Mw;
            }
            set
            {
                m_g2Mw = value;
            }
        }

        [XmlAttribute("G2Mvr")]
        public double G2Mvr
        {
            get
            {
                return m_g2Mvr;
            }
            set
            {
                m_g2Mvr = value;
            }
        }


        [XmlAttribute("ConsecCap")]
        public int ConsecCap
        {
            get
            {
                return m_consecCap;
            }
            set
            {
                m_consecCap = value;
            }
        }

        [XmlAttribute("Ncdel")]
        public int Ncdel
        {
            get
            {
                return m_ncdel;
            }
            set
            {
                m_ncdel = value;
            }
        }

        [XmlAttribute("Ntdel")]
        public int Ntdel
        {
            get
            {
                return m_ntdel;
            }
            set
            {
                m_ntdel = value;
            }
        }

        [XmlAttribute("OldDay")]
        public int OldDay
        {
            get
            {
                return m_oldDay;
            }
            set
            {
                m_oldDay = value;
            }
        }

        [XmlAttribute("Zcdel")]
        public int Zcdel         // jjj FOR TST // minimum number of run
        {
            get
            {
                return m_zcdel;
            }
            set
            {
                m_zcdel = value;
            }
        }

        [XmlAttribute("Zdel")]
        public int Zdel           // minimum No of run after a cap control before a tap control, or vice versa
        {
            get
            {
                return m_zdel;
            }
            set
            {
                m_zdel = value;
            }
        }

        [XmlAttribute("Zmaxtrip")]
        public int Zmaxtrip       // max cap trips per day
        {
            get
            {
                return m_zmaxtrip;
            }
            set
            {
                m_zmaxtrip = value;
            }
        }

        [XmlAttribute("Zmaxclose")]
        public int Zmaxclose      // max cap clsoes per day
        {
            get
            {
                return m_zmaxclose; 
            }
            set
            {
                m_zmaxclose = value;
            }
        }

        [XmlAttribute("Zccons")]
        public int Zccons         // No. of consecutive triggers
        {
            get
            {
                return m_zccons;
            }
            set
            {
                m_zccons = value;
            }
        }

        [XmlAttribute("Zclmvr")]
        public double Zclmvr  // MVRS to close cap bank
        {
            get
            {
                return m_zclmvr;
            }
            set
            {
                m_zclmvr = value;
            }
        }

        [XmlAttribute("Ztrmvr")]
        public double Ztrmvr  // MVRS to trip cap bank
        {
            get
            {
                return m_ztrmvr;
            }
            set
            {
                m_ztrmvr = value;
            }
        }

        [XmlAttribute("Alarm")]
        public string Alarm
        {
            get
            {
                return m_alarm;
            }
            set
            {
                m_alarm = value;
            }
        }

        [XmlAttribute("Normal")]
        public string Normal
        {
            get
            {
                return m_normal;
            }
            set
            {
                m_normal = value;
            }
        }

        [XmlAttribute("Raise")]
        public string Raise
        {
            get
            {
                return m_raise;
            }
            set
            {
                m_raise = value;
            }
        }

        [XmlAttribute("Lower")]
        public string Lower
        {
            get
            {
                return m_lower;
            }
            set
            {
                m_lower = value;
            }
        }

        [XmlAttribute("ON")]
        public string ON
        {
            get
            {
                return m_on;
            }
            set
            {
                m_on = value;
            }
        }

        //[XmlAttribute("ControlOn")]
        //public string ControlOn
        //{
        //    get
        //    {
        //        return m_controlOn;
        //    }
        //    set
        //    {
        //        m_controlOn = value;
        //    }
        //}

        [XmlAttribute("OFF")]
        public string OFF
        {
            get
            {
                return m_off;
            }
            set
            {
                m_off = value;
            }
        }

        [XmlAttribute("Close")]
        public string Close
        {
            get
            {
                return m_close;
            }
            set
            {
                m_close = value;
            }
        }

        [XmlAttribute("Trip")]
        public string Trip
        {
            get
            {
                return m_trip;
            }
            set
            {
                m_trip = value;
            }
        }

        [XmlAttribute("Remote")]
        public string Remote
        {
            get
            {
                return m_remote;
            }
            set
            {
                m_remote = value;
            }
        }

        [XmlAttribute("Local")]
        public string Local
        {
            get
            {
                return m_local;
            }
            set
            {
                m_local = value;
            }
        }

        [XmlAttribute("Auto")]
        public string Auto
        {
            get
            {
                return m_auto;
            }
            set
            {
                m_auto = value;
            }
        }

        [XmlAttribute("Manual")]
        public string Manual
        {
            get
            {
                return m_manual;
            }
            set
            {
                m_manual = value;
            }
        }

        [XmlAttribute("Dashes")]
        public string Dashes
        {
            get
            {
                return m_dashes;
            }
            set
            {
                m_dashes = value;
            }
        }

        #endregion
    }
}
