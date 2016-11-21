using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoltController.Measurements
{
    public class Configuration
    {
        private string m_progStat;

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
    }
}
