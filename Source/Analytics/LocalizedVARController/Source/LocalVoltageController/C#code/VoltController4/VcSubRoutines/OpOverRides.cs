using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoltController.VcControlDevice;

namespace VoltController.VcSubRoutines
{
    public class OpOverRides
    {
        #region [ Private Members ]

        private string m_statV;
        private string m_analV;


        #endregion

        #region [ Properties ]
        public string StatV
        {
            get
            {
                return m_statV; 
            }
            set
            {
                m_statV = value;
            }
        }

        public string AnalV
        {
            get
            {
                return m_analV;
            }
            set
            {
                m_statV = value;
            }
        }


        #endregion
 
        #region [ Public Methods ]
        //public void OpOverRide(VoltVarController Frame)
        //{
        //    string statId = "VOLTOVER";
        //    string analId = "VOLTRANGE";
        //    double offset = 2.0;
        //    m_lowLim = Frame.SubstationAlarmDevice.VLLIM - offset;
        //    m_hiLim = Frame.SubstationAlarmDevice.VHLIM - offset;

        //    int temp = 0;
        //    int ret = 0;
        //    string statV = 
        //    string analV = 
       // }

        #endregion

    }
}
