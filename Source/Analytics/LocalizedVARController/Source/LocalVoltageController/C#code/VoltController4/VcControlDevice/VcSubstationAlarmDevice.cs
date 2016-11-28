using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace VoltController.VcControlDevice
{
    [Serializable()]
    public class VcSubstationAlarmDevice
    {
        #region [ Private Members ]
        private string m_host;

        private string m_swSchedId;
        private string m_swSchedField;
        private string m_swSchedClear;

        private string m_ltcDevice;   // Alarm Location name
        private string m_contFail;   // Control Failure location
        private string m_ltcLimit;   // LTC Limit Alarm
        private string m_outService;  // LTC out of service
        private string m_voltsOut;   // LTC volts out of range
        private string m_ltcProgram;   // LTC program status

        private int m_zntx;           // number of transformers
        private int m_zncp;           // number of capacitros  
        private double m_zvlo;    // voltage extreme low limit 
        private double m_vllim;   // voltage low limit
        private double m_vhlim;   // voltgae high limit
        private double m_zvhi;    // voltage extreme high limit
        private int m_zhitap;        // Highest tap position
        private int m_zlotap;        // lowest tap position
        private int m_ztcons;         // No. of consecutive triggers

        private double m_zbal;      // if difference between highest
                                    // and lowest Megavars > ZBAL,
                                    // Megavars are unbalanced 

        private double m_zlowv;     // Low Vale for watts
        private double m_zdiftap;   // Maximum LTC Tap Position Diff
        #endregion

        #region [ Properties ]
        [XmlAttribute("Host")]
        public string Host
        {
            get
            {
                return m_host;
            }
            set
            {
                m_host = value;
            }
        }

        [XmlAttribute("SwschedId")]
        public string SwSchedId
        {
            get
            {
                return m_swSchedId;
            }
            set
            {
                m_swSchedId = value;
            }
        }

        [XmlAttribute("SwSchedField")]
        public string SwSchedField
        {
            get
            {
                return m_swSchedField;
            }
            set
            {
                m_swSchedField = value;
            }
        }

        [XmlAttribute("SwSchedClear")]
        public string SwSchedClear
        {
            get
            {
                return m_swSchedClear;
            }
            set
            {
                m_swSchedClear = value;
            }
        }

        [XmlAttribute("LtcDevice")]
        public string LtcDevice   // Alarm Location name
        {
            get
            {
                return m_ltcDevice;
            }
            set
            {
                m_ltcDevice = value;
            }
        }

        [XmlAttribute("ContFail")]
        public string ContFail   // Control Failure location
        {
            get
            {
                return m_contFail;
            }
            set
            {
                m_contFail = value;
            }
        }

        [XmlAttribute("LtcLimit")]
        public string LtcLimit   // LTC Limit Alarm
        {
            get
            {
                return m_ltcLimit;
            }
            set
            {
                m_ltcLimit = value;
            }
        }

        [XmlAttribute("OutService")]
        public string OutService  // LTC out of service
        {
            get
            {
                return m_outService;
            }
            set
            {
                m_outService = value;
            }
        }

        [XmlAttribute("VoltsOut")]
        public string VoltsOut   // LTC volts out of range
        {
            get
            {
                return m_voltsOut;
            }
            set
            {
                m_voltsOut = value;
            }
        }

        [XmlAttribute("LtcProgram")]
        public string LtcProgram   // LTC program status
        {
            get
            {
                return m_ltcProgram;
            }
            set
            {
                m_ltcProgram = value;
            }

        }

        [XmlAttribute("ZNTX")]
        public int ZNTX           // number of transformers
        {
            get
            {
                return m_zntx;
            }
            set
            {
                m_zntx = value;
            }
        }
        [XmlAttribute("ZNCP")]
        public int ZNCP           // number of capacitros
        {
            get
            {
                return m_zncp;
            }
            set
            {
                m_zncp = value;
            }
        }

        [XmlAttribute("ZVLO")]
        public double ZVLO    // voltage extreme low limit
        {
            get
            {
                return m_zvlo;
            }
            set
            {
                m_zvlo = value;
            }
        }

        [XmlAttribute("VLLIM")]
        public double VLLIM   // voltage low limit
        {
            get
            {
                return m_vllim;
            }
            set
            {
                m_vllim = value;
            }
        }

        [XmlAttribute("VHLIM")]
        public double VHLIM   // voltgae high limit
        {
            get
            {
                return m_vhlim;
            }
            set
            {
                m_vhlim = value;
            }
        }

        [XmlAttribute("ZVHI")]
        public double ZVHI    // voltage extreme high limit
        {
            get
            {
                return m_zvhi;
            }
            set
            {
                m_zvhi = value;
            }
        }

        [XmlAttribute("ZHITAP")]
        public int ZHITAP        // Highest tap position
        {
            get
            {
                return m_zhitap;
            }
            set
            {
                m_zhitap = value;
            }
        }

        [XmlAttribute("ZLOTAP")]
        public int ZLOTAP        // lowest tap position
        {
            get
            {
                return m_zlotap;
            }
            set
            {
                m_zlotap = value;
            }
        }

        [XmlAttribute("ZTCONS")]
        public int ZTCONS         // No. of consecutive triggers
        {
            get
            {
                return m_ztcons;
            }
            set
            {
                m_ztcons = value;
            }
        }

        [XmlAttribute("ZBAL")]
        public double ZBAL      // if difference between highest
        {                       // and lowest Megavars > ZBAL,Megavars are unbalanced 
            get
            {
                return m_zbal;
            }
            set
            {
                m_zbal = value;
            }
        }

        [XmlAttribute("ZLOWV")]
        public double ZLOWV
        {
            get
            {
                return m_zlowv;
            }
            set
            {
                m_zlowv = value;
            }
        }

        [XmlAttribute("ZDIFTAP")]
        public double ZDIFTAP
        {
            get
            {
                return m_zdiftap;
            }
            set
            {
                m_zdiftap = value;
            }
        }
        #endregion

        #region [ Xml Serialization/Deserialization methods ]
        public void SerializeToXml(string pathName)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(VcSubstationAlarmDevice));

                TextWriter writer = new StreamWriter(pathName);

                serializer.Serialize(writer, this);

                writer.Close();
            }
            catch (Exception exception)
            {
                throw new Exception("Failed to Serialzie");
            }
        }

        public static VcSubstationAlarmDevice DeserializeFromXml(string pathName)
        {
            try
            {
                VcSubstationAlarmDevice voltVarController = null;

                XmlSerializer deserializer = new XmlSerializer(typeof(VcSubstationAlarmDevice));

                StreamReader reader = new StreamReader(pathName);

                voltVarController = (VcSubstationAlarmDevice)deserializer.Deserialize(reader);

                reader.Close();

                return voltVarController;
            }
            catch (Exception exception)
            {
                throw new Exception("Failed to Deserialzie");
            }
        }

        #endregion
    }
}