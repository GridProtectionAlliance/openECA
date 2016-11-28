using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace VoltController.VcControlDevice
{
    public class VcCapacitorBank
    {
        #region[ Private Members ]
        private string m_opCapDev;
        private string m_opCapId;
        private string m_opCapV;

        private string m_scadaSwId;
        private string m_scadaSwV;

        private string m_locRemId;
        private string m_locRemV;

        private string m_autoManId;
        private string m_autoManV;

        private string m_miscId;
        private string m_miscV;

        private string m_busBkrId;
        private string m_busBkrV;

        private string m_capBkrId;
        private string m_capBkrV;

        private double m_clov;
        private double m_chiv;

        private double m_alovc;
        private double m_ahivt;

        private string m_locKvDev;
        private string m_locKvId;
        private double m_locKvV;

        private string m_capCtlDev;
        private string m_capCtlId;

        private int m_rtu;
        private int m_ctlDone;
        private int m_inSvc;
        private string m_prevCtl;
        private int m_ncTrip;
        private int m_ncClose;
        private int m_tripEx;
        private int m_closeEx;

        #endregion

        #region [ Properties  ]
        [XmlAttribute("OpCapDev")]
        public string OpCapDev
        {
            get
            {
                return m_opCapDev;
            }
            set
            {
                m_opCapDev = value;
            }
        }

        [XmlAttribute("OpCapId")]
        public string OpCapId
        {
            get
            {
                return m_opCapId;
            }
            set
            {
                m_opCapId = value;
            }
        }

        [XmlAttribute("OpCapV")]
        public string OpCapV
        {
            get
            {
                return m_opCapV;
            }
            set
            {
                m_opCapV = value;
            }
        }

        [XmlAttribute("ScadaSwId")]
        public string ScadaSwId
        {
            get
            {
                return m_scadaSwId;
            }
            set
            {
                m_scadaSwId = value;
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

        [XmlAttribute("LocRemId")]
        public string LocRemId
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

        [XmlAttribute("AutoManId")]
        public string AutoManId
        {
            get
            {
                return m_autoManId;
            }
            set
            {
                m_autoManId = value;
            }
        }


        [XmlAttribute("AutoManV")]
        public string AutoManV
        {
            get
            {
                return m_autoManV;
            }
            set
            {
                m_autoManV = value;
            }
        }

        [XmlAttribute("MiscId")]
        public string MiscId
        {
            get
            {
                return m_miscId;
            }
            set
            {
                m_miscId = value;
            }
        }

        [XmlAttribute("MiscV")]
        public string MiscV
        {
            get
            {
                return m_miscV;
            }
            set
            {
                m_miscV = value;
            }
        }

        [XmlAttribute("BusBkrId")]
        public string BusBkrId
        {
            get
            {
                return m_busBkrId;
            }
            set
            {
                m_busBkrId = value;
            }
        }

        [XmlAttribute("BusBkrV")]
        public string BusBkrV
        {
            get
            {
                return m_busBkrV;
            }
            set
            {
                m_busBkrV = value;
            }
        }

        [XmlAttribute("CapBkrId")]
        public string CapBkrId
        {
            get
            {
                return m_capBkrId;
            }
            set
            {
                m_capBkrId = value;
            }
        }

        [XmlAttribute("CapBkrV")]
        public string CapBkrV
        {
            get
            {
                return m_capBkrV;
            }
            set
            {
                m_capBkrV = value;
            }
        }

        [XmlAttribute("Clov")]
        public double Clov
        {
            get
            {
                return m_clov;
            }
            set
            {
                m_clov = value;
            }
        }

        [XmlAttribute("Chiv")]
        public double Chiv
        {
            get
            {
                return m_chiv;
            }
            set
            {
                m_chiv = value;
            }
        }

        [XmlAttribute("Alovc")]
        public double Alovc
        {
            get
            {
                return m_alovc;
            }
            set
            {
                m_alovc = value;
            }
        }

        [XmlAttribute("Ahivt")]
        public double Ahivt
        {
            get
            {
                return m_ahivt;
            }
            set
            {
                m_ahivt = value;
            }
        }

        [XmlAttribute("LocKvDev")]
        public string LockvDev
        {
            get
            {
                return m_locKvDev;
            }
            set
            {
                m_locKvDev = value;
            }
        }

        [XmlAttribute("LocKvV")]
        public double LockvV
        {
            get
            {
                return m_locKvV;
            }
            set
            {
                m_locKvV = value;
            }
        }

        [XmlAttribute("LocKvId")]
        public string LockvId
        {
            get
            {
                return m_locKvId;
            }
            set
            {
                m_locKvId = value;
            }
        }

        [XmlAttribute("CapCtlDev")]
        public string CapCtlDev
        {
            get
            {
                return m_capCtlDev;
            }
            set
            {
                m_capCtlDev = value;
            }
        }

        [XmlAttribute("CapCtlId")]
        public string CapCtlId
        {
            get
            {
                return m_capCtlId;
            }
            set
            {
                m_capCtlId = value;
            }
        }

        [XmlAttribute("Rtu")]
        public int Rtu
        {
            get
            {
                return m_rtu;
            }
            set
            {
                m_rtu = value;
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

        [XmlAttribute("NcTrip")]
        public int NcTrip
        {
            get
            {
                return m_ncTrip;
            }
            set
            {
                m_ncTrip = value;
            }
        }


        [XmlAttribute("NcClose")]
        public int NcClose
        {
            get
            {
                return m_ncClose;
            }
            set
            {
                m_ncClose = value;
            }
        }

        [XmlAttribute("TripEx")]
        public int TripEx
        {
            get
            {
                return m_tripEx;
            }
            set
            {
                m_tripEx = value;
            }
        }

        [XmlAttribute("CloseEx")]
        public int CloseEx
        {
            get
            {
                return m_closeEx;
            }
            set
            {
                m_closeEx = value;
            }
        }

        #endregion
    }
}
