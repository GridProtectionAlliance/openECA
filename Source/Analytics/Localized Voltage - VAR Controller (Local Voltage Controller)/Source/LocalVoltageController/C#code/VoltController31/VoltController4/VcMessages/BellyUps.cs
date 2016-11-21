using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace VoltController.VcMessages
{
    public class BellyUps
    {
        #region [ Private Members ]
        private string m_bellyUpMessage;
        #endregion

        #region [ Properties ] 
        public string BellyUpMessage
        {
            get
            {
                return m_bellyUpMessage;
            }
            set
            {
                m_bellyUpMessage = value;
            }
        }
        #endregion

        #region [ Public Method ]
        public void BellyUp(string messageInput)
        {
            string error_msg = messageInput;
            DateTime now = DateTime.Now;
            //string croak = swsched_api::swsched_set(HOST, SW_SCHED_ID, SW_SCHED_FIELD, SW_SCHED_CLEAR);
            Console.WriteLine("Belly up {0} {1}",now, messageInput);
            //die("{0} - ERROR PROGRAM TERMINATING!\n\tReason: {1}\n",) // die is an exception
        }

        #endregion

        #region [  Xml Serialization/Deserialization methods ]
        public void SerializeToXml(string logsFileName)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(BellyUps));

                TextWriter writer = new StreamWriter(logsFileName + "BellyUps.txt");

                serializer.Serialize(writer, this);

                writer.Close();
            }
            catch (Exception exception)
            {
                throw new Exception("Failed to Serialzie");
            }
        }



        #endregion
    }
}
