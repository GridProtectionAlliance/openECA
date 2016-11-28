using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace VoltController.VcMessages
{
    [Serializable()]
    public class ProgLogMessage
    {

        #region [ Private Members ]
        private string m_logMessage;
        #endregion

        #region [ Properties]


        [XmlAttribute("LogMessage")]
        public string LogMessage
        {
            get
            {
                return m_logMessage;
            }
            set
            {
                m_logMessage = value;
            }
        }
        #endregion


        #region [ Public Methods ]
        public void PrintProgLogMessage(string messageInput)
        {
            
            DateTime now = DateTime.Now;
            //sw:sw_log_set_categories("PROGRAM");
            //sw:sw_log (MessageInput);
            Console.WriteLine("{0} - {1}\n",now, messageInput);
            
        }

        #endregion

        #region [  Xml Serialization/Deserialization methods ]
        public void SerializeToXml(string folderName)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ProgLogMessage));

                TextWriter writer = new StreamWriter(folderName+ "ProgLogMessage_Program.txt");

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
