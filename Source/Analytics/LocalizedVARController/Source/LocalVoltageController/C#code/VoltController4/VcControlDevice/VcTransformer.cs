using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VoltController.VcControlDevice
{
    [Serializable()]
    public class VcTransformer : IControlDevice
    {
        #region [ Private Members ]
        private string m_deviceId;     // device location

        private string m_locRemId;    // Tx4 Local/Remote Switch 
        private string m_locRemV;

        private string m_scadaSw;             // SCADA Control Switch   
        private string m_scadaSwV;

        private string m_highSideId;       // H434H High side Moab
        private int m_highSideV;

        private string m_lowSideId;      // CBL432 Low Side Moab
        private int m_lowSideV;

        private string m_tapId;        // Tx4 tap position analog
        private int m_tapV;
        private int m_stTapV;

        private string m_mwId;           // Tx4 Megawatts analog
        private double m_mwV;

        private string m_mvrId;         // Tx4 Megawatts analog
        private double m_mvrV;
        private double m_stMvrV;

        private string m_voltsId;      // 115 kV Bus Volts
        private double m_voltsV;
        private int m_voltsTime;

        private string m_ltcCtlId;   // Tx 5 Control ID
        private int m_tapTime;

        private int m_ctlDone;
        private int m_inSvc;
        private string m_prevCtl;

        private int m_mvrBal;                 // Megavar balance status
        private string m_iRange;             // is Voltage Out of Range
        #endregion

        #region [ Properties ]
        [XmlAttribute("DeviceId")]
        public string DeviceId
        {
            get
            {
                return m_deviceId;
            }
            set
            {
                m_deviceId = value;
            }
        }

        [XmlAttribute("LocRemId")]
        public string LocRemId    // Tx4 Local/Remote Switch
        {
            get
            {
                return m_locRemId;
            }
            set
            {
                m_locRemId = value;
            }
        }

        [XmlAttribute("LocRemV")]
        public string LocRemV
        {
            get
            {
                return m_locRemV;
            }
            set
            {
                m_locRemV = value;
            }
        }

        [XmlAttribute("ScadaSw")]
        public string ScadaSw  // SCADA Control Switch  
        {
            get
            {
                return m_scadaSw;
            }
            set
            {
                m_scadaSw = value;
            }
        }

        [XmlAttribute("ScadaSwV")]
        public string ScadaSwV
        {
            get
            {
                return m_scadaSwV;
            }
            set
            {
                m_scadaSwV = value;
            }
        }

        [XmlAttribute("HighSideId")]
        public string HighSideId  // H434H High side Moab
        {
            get
            {
                return m_highSideId;
            }
            set
            {
                m_highSideId = value;
            }
        }

        [XmlAttribute("HighSideV")]
        public int HighSideV
        {
            get
            {
                return m_highSideV;
            }
            set
            {
                m_highSideV = value;
            }
        }

        [XmlAttribute("LowSideId")]
        public string LowSideId   // CBL432 Low Side Moab
        {
            get
            {
                return m_lowSideId;
            }
            set
            {
                m_lowSideId = value;
            }
        }

        [XmlAttribute("LowSideV")]
        public int LowSideV
        {
            get
            {
                return m_lowSideV;
            }
            set
            {
                m_lowSideV = value;
            }
        }

        [XmlAttribute("TapId")]
        public string TapId  // Tx4 tap position analog
        {
            get
            {
                return m_tapId;
            }
            set
            {
                m_tapId = value;
            }
        }

        [XmlAttribute("TapV")]
        public int TapV
        {
            get
            {
                return m_tapV;
            }
            set
            {
                m_tapV = value;
            }
        }

        [XmlAttribute("StTapV")]
        public int StTapV
        {
            get
            {
                return m_stTapV;
            }
            set
            {
                m_stTapV = value;
            }
        }
        [XmlAttribute("MwId")]
        public string MwId  // Tx4 Megawatts analog
        {
            get
            {
                return m_mwId;
            }
            set
            {
                m_mwId = value;
            }
        }

        [XmlAttribute("MwV")]
        public double MwV
        {
            get
            {
                return m_mwV;
            }
            set
            {
                m_mwV = value;
            }
        }

        [XmlAttribute("MvrId")]
        public string MvrId         // Tx4 Megawatts analog
        {
            get
            {
                return m_mvrId;
            }
            set
            {
                m_mvrId = value;
            }
        }

        [XmlAttribute("MvrV")]
        public double MvrV
        {
            get
            {
                return m_mvrV;
            }
            set
            {
                m_mvrV = value;
            }
        }

        [XmlAttribute("StMvrV")]
        public double StMvrV
        {
            get
            {
                return m_stMvrV;
            }
            set
            {
                m_stMvrV = value;
            }
        }

        [XmlAttribute("VoltsId")]
        public string VoltsId     // 115 kV Bus Volts
        {
            get
            {
                return m_voltsId;
            }
            set
            {
                m_voltsId = value;
            }
        }

        [XmlAttribute("VoltsV")]
        public double VoltsV
        {
            get
            {
                return m_voltsV;
            }
            set
            {
                m_voltsV = value;
            }
        }

        [XmlAttribute("VoltsTime")]
        public int VoltsTime
        {
            get
            {
                return m_voltsTime;
            }
            set
            {
                m_voltsTime = value;
            }
        }

        [XmlAttribute("LtcCtlId")]
        public string LtcCtlId   // Tx 5 Control ID
        {
            get
            {
                return m_ltcCtlId;
            }
            set
            {
                m_ltcCtlId = value;
            }
        }

        [XmlAttribute("TapTime")]
        public int TapTime
        {
            get
            {
                return m_tapTime;
            }
            set
            {
                m_tapTime = value;
            }
        }

        [XmlAttribute("CtlDone")]
        public int CtlDone
        {
            get
            {
                return m_ctlDone;
            }
            set
            {
                m_ctlDone = value;
            }
        }

        [XmlAttribute("InSvc")]
        public int InSvc
        {
            get
            {
                return m_inSvc;
            }
            set
            {
                m_inSvc = value;
            }
        }

        [XmlAttribute("PrevCtl")]
        public string PrevCtl
        {
            get
            {
                return m_prevCtl;
            }
            set
            {
                m_prevCtl = value;
            }
        }

        [XmlAttribute("MvrBal")]
        public int MvrBal                 // Megavar balance status
        {
            get
            {
                return m_mvrBal;
            }
            set
            {
                m_mvrBal = value;
            }
        }

        [XmlAttribute("IRange")]
        public string IRange             // is Voltage Out of Range
        {
            get
            {
                return m_iRange;
            }
            set
            {
                m_iRange = value;
            }
        }

        #endregion


    }
}
