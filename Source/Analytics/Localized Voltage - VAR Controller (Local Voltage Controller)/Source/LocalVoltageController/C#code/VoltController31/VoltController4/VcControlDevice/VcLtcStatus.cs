using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VoltController.VcControlDevice
{
    [Serializable()]
    public class VcLtcStatus
    {
        #region [ Private Members ]
        private double m_minVar;
        private double m_maxVar;
        private int m_minTap;
        private int m_maxTap;

        private int m_nins;
        private double m_avv;
        private int m_difTap;
        private double m_balMvr;
        private int m_cfail;

        private int m_rtx;
        private int m_ltx;

        #endregion

        #region [ Properties ]
        [XmlAttribute("MinVar")]
        public double MinVar
        {
            get
            {
                return m_minVar;
            }
            set
            {
                m_minVar = value;
            }
        }

        [XmlAttribute("MaxVar")]
        public double MaxVar
        {
            get
            {
                return m_maxVar;
            }
            set
            {
                m_maxVar = value;
            }
        }

        [XmlAttribute("MinTap")]
        public int MinTap
        {
            get
            {
                return m_minTap;
            }
            set
            {
                m_minTap = value;
            }
        }

        [XmlAttribute("MaxTap")]
        public int MaxTap
        {
            get
            {
                return m_maxTap;
            }
            set
            {
                m_maxTap = value;
            }
        }

        [XmlAttribute("Nins")]
        public int Nins
        {
            get
            {
                return m_nins; 
            }
            set
            {
                m_nins = value;
            }
        }

        [XmlAttribute("Avv")]
        public double Avv
        {
            get
            {
                return m_avv;
            }
            set
            {
                m_avv = value;
            }
        }

        [XmlAttribute("DifTap")]
        public int DifTap
        {
            get
            {
                return m_difTap;
            }
            set
            {
                m_difTap = value;
            }
        }

        [XmlAttribute("BalMvr")]
        public double BalMvr
        {
            get
            {
                return m_balMvr;
            }
            set
            {
                m_balMvr = value;
            }
        }

        [XmlAttribute("Cfail")]
        public int Cfail
        {
            get
            {
                return m_cfail;
            }
            set
            {
                m_cfail = value;
            }
        }

        [XmlAttribute("RTX")]
        public int RTX
        {
            get
            {
                return m_rtx;
            }
            set
            {
                m_rtx = value;
            }
        }

        [XmlAttribute("LTC")]
        public int LTX
        {
            get
            {
                return m_ltx;
            }
            set
            {
                m_ltx = value;
            }

        }

        #endregion
    }
}
