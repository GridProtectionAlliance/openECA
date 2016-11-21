using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using VoltController.VcControlDevice;
using VoltController.VcMessages;
using System.Collections;

namespace VoltController.VcReadPreviousControl
{

    public class ReadPreviousRun
    {
        #region [ Private Members ]
        private VoltVarController m_previousFrame;

        #endregion

        #region [ Properties ]

        public VoltVarController PreviousFrame
        {
            get
            {
                return m_previousFrame;
            }
            set
            {
                m_previousFrame = value;
            }
        }

        #endregion

        #region [ Constructor ]
        public ReadPreviousRun()
        {
            Initialize();
        }

        #endregion

        #region [ Private Methods ]
        private void Initialize()
        {
            m_previousFrame = new VoltVarController();
        }

        #endregion

        #region [ Public Methods ]
        
        public VoltVarController ReadPreviousFrame(VoltVarController CurrentFrame, VoltVarController PreviousFrame)
        {
            PreviousFrame = CurrentFrame;
            //VcSubstationInfomation VcSubInfo = new VcSubstationInfomation();
            //VcTransformer VcTx = new VcTransformer();
            //VcCapacitorBank VcCap = new VcCapacitorBank();
            //VcSubstationAlarmDevice VcSubAlarm = new VcSubstationAlarmDevice();
            //ProgLogMessage ProgLogMsg = new ProgLogMessage();

            //if (infoline != null)
            //{
            //    // ltc_log_message(string.Format("Get Read info = {0}", infoline));
            //    char[] delimiterchars = { '|' };
            //    string[] arr = infoline.Split(delimiterchars);

            //    Hashtable Hashtable_temp = new Hashtable();

            //    char[] delimiterchars1 = { '~' };
            //    for (int i = 0; i < arr.Length; i++)
            //    {
            //        string[] splitedarr;
            //        splitedarr = arr[i].Split(delimiterchars1);
            //        string name = splitedarr[0];
            //        string val = splitedarr[1];
            //        Hashtable_temp[name] = val;
            //    }


            //    VcSubInfo.ConsecTap = (int)Hashtable_temp["ConsecTap"];
            //    VcSubInfo.ConsecCap = (int)Hashtable_temp["ConsecCap"];
            //    VcSubInfo.Ntdel = (int)Hashtable_temp["Ntdel"];
            //    VcSubInfo.Ncdel = (int)Hashtable_temp["Ncdel"];
            //    VcSubInfo.OldDay = (int)Hashtable_temp["OldDay"];

            //    VcSubAlarm.ZNTX = 2;
            //    VcSubAlarm.ZNCP = 2;

            //    for (int i = 0; i < VcSubAlarm.ZNTX; i++)
            //    {
            //        string prefix = "tx" + Convert.ToString(i);
            //        VcTx.CtlDone = Convert.ToInt32(Hashtable_temp[prefix + "CltDone"]);
            //        VcTx.TapV = (int)Hashtable_temp[prefix + "TapV"];
            //        VcTx.MvrV = (int)Hashtable_temp[prefix + "MvrV"];
            //        VcTx.PrevCtl = (string)Hashtable_temp[prefix + "PrevCtl"];

            //    }

            //    // two hashtables are indexed by a list
            //    for (int i = 0; i < VcSubAlarm.ZNCP; i++)
            //    {
            //        string prefix1 = "CP" + Convert.ToString(i);
            //        VcCap.CtlDone = Convert.ToInt32(Hashtable_temp[prefix1 + "CtlDone"]);
            //        VcCap.PrevCtl = (string)Hashtable_temp[prefix1 + "PrevCtl"];
            //        VcCap.NcTrip = Convert.ToInt32(Hashtable_temp[prefix1 + "NcTrip"]);
            //        VcCap.NcClose = Convert.ToInt32(Hashtable_temp[prefix1 + "NcClose"]);
            //        VcCap.TripEx = Convert.ToInt32(Hashtable_temp[prefix1 + "TripEx"]);
            //        VcCap.CloseEx = Convert.ToInt32(Hashtable_temp[prefix1 + "CloseEx"]);
            //    }
            //}
            //else
            //{
            //    ProgLogMsg.PrintProgLogMessage(string.Format("Nothing saved on previous run"));
            //    VcSubInfo.ConsecTap = 0;
            //}

            return CurrentFrame;

        }

        internal VoltVarController ReadPreviousFrame(VoltVarController frame, ReadPreviousRun previousFrame)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region [ Xml Serialization/Deserialization methods ]
        //public void SerializeToXml(string pathName)
        //{
        //    try
        //    {
        //        XmlSerializer serializer = new XmlSerializer(typeof(ReadPreviousRun));

        //        TextWriter writer = new StreamWriter(pathName);

        //        serializer.Serialize(writer, this);

        //        writer.Close();
        //    }
        //    catch (Exception exception)
        //    {
        //        throw new Exception("Failed to Serialzie");
        //    }
        //}

        //public static ReadPreviousRun DeserializeFromXml(string pathName)
        //{
        //    try
        //    {
        //        ReadPreviousRun readPreviousRun = null;

        //        XmlSerializer deserializer = new XmlSerializer(typeof(ReadPreviousRun));

        //        StreamReader reader = new StreamReader(pathName);

        //        readPreviousRun = (ReadPreviousRun)deserializer.Deserialize(reader);

        //        reader.Close();

        //        return readPreviousRun;
        //    }
        //    catch (Exception exception)
        //    {
        //        throw new Exception("Failed to Deserialzie");
        //    }
        //}

        #endregion

    }
}
