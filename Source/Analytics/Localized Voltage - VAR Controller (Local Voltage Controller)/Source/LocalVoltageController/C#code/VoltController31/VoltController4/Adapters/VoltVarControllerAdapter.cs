//******************************************************************************************************
//  VoltVarControllerAdapter.cs
//
//  Copyright © 2016, Duotong Yang  All Rights Reserved.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  11/09/2016 - Duotong Yang
//       Generated original version of source code.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using VoltController.VcControlDevice;
using VoltController.Adapters;
using VoltController.VcReadCurrentControl;
using VoltController.VcSubRoutines;
using VoltController.VcReadPreviousControl;
using VoltController.PythonScript;


namespace VoltController.Adapters
{
    [Serializable()]
    public class VoltVarControllerAdapter
    {
        #region [ Private Members ]
        private VoltVarController m_inputFrame;
        private string m_configurationPathName;
        private string m_logMessage;
        #endregion

        #region[ Properties ] 

        [XmlIgnore()]
        public VoltVarController InputFrame
        {
            get
            {
                return m_inputFrame;
            }
            set
            {
                m_inputFrame = value;
            }
        }

        [XmlIgnore()]
        public string ConfigurationPathName
        {
            get
            {
                return m_configurationPathName;
            }
            set
            {
                m_configurationPathName = value;
            }
        }

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

        #region[ Constructor ]
        public VoltVarControllerAdapter()
        {

            InitializeInputFrame();
        }

        #endregion

        #region [ Private Methods  ]

        private void InitializeInputFrame()
        {

            m_inputFrame = new VoltVarController();
            
        }

        #endregion

        #region [ Public Methods ]
        public void Initialize()
        {

            m_inputFrame = VoltVarController.DeserializeFromXml(m_configurationPathName);
            m_logMessage = null;
        }

        public void PublishFrame(VoltVarController Frame, string inputdatafolder, VoltVarController PreviousFrame)
        {
            SubRoutine sub = new SubRoutine();
            ReadCurrentControl ReadCurrentCon = new ReadCurrentControl();

            #region [ Read the Input Value Key Pairs]

            foreach (KeyValuePair<string, object> kvp in Frame.RawkeyValuePairs)
            {
                m_inputFrame.RawkeyValuePairs.Add(kvp.Key, kvp.Value);
            }

            #endregion

            #region [ Measurements Mapping ]

            m_inputFrame.OnNewMeasurements();

            #endregion

            #region [ Read The Previous Run ]

            m_inputFrame.ReadPreviousRun(PreviousFrame);

            #endregion

            #region[ Verify Program Controls ]

            ReadCurrentCon.VerifyProgramControl(m_inputFrame.SubstationAlarmDevice.LtcProgram);

            #endregion

            #region[ Adjust Control Delay Counters ]

            //#-----------------------------------------------------------------------#
            //# adjust the cap bank control delay counter, which is used to ensure:	#
            //# a. we don't do two cap bank control within 30 minutes of each other.	#
            //# b. we don't do a tap control within a minute of a cap bank control.	#
            //#-----------------------------------------------------------------------#

            if (m_inputFrame.SubstationInformation.Ncdel < m_inputFrame.SubstationInformation.Zcdel)
            {
                m_inputFrame.SubstationInformation.Ncdel = m_inputFrame.SubstationInformation.Ncdel + 1;
            }


            //#-----------------------------------------------------------------------#
            //# Adjust the tap control delay counter, which is used to ensure we	#
            //# don't do a cap bank control within a minute of a tap control.		#
            //#-----------------------------------------------------------------------#


            if (m_inputFrame.SubstationInformation.Ntdel < m_inputFrame.SubstationInformation.Zdel)
            {
                m_inputFrame.SubstationInformation.Ntdel = m_inputFrame.SubstationInformation.Ntdel + 1;
            }


            #endregion

            #region [ Read Curren Tx Values and Voltages ]

            m_inputFrame = ReadCurrentCon.ReadCurrentTransformerValuesAndVoltages(m_inputFrame);

            #endregion

            #region [ Check if the Previous Control Reults can Meet Our Expectation ]

            m_inputFrame = ReadCurrentCon.CheckPreviousControlResults(m_inputFrame);

            #endregion

            #region [ Call Sub Taps ]

            m_inputFrame = sub.Taps(m_inputFrame);

            #endregion

            #region [CapBank]

            m_inputFrame = sub.CapBank(m_inputFrame);

            #endregion

            #region [ Save before Exit ]

            m_logMessage = ReadCurrentCon.MessageInput;
            m_logMessage += sub.MessageInput;
            m_inputFrame.RawkeyValuePairs.Clear();
            m_inputFrame.LtcStatus.Avv = 0;
            m_inputFrame.LtcStatus.Nins = 0;
            m_inputFrame.LtcStatus.MinVar = 99999;
            m_inputFrame.LtcStatus.MaxVar = -9999;

            #endregion
        }

        public void SerializeToXml(string pathName)
        {
            try
            {
                // Create an XmlSerializer with the type of NetworkModel
                XmlSerializer serializer = new XmlSerializer(typeof(VoltVarControllerAdapter));

                // Open a connection to the file and path.
                TextWriter writer = new StreamWriter(pathName);

                // Serialize this instance of NetworkModel
                serializer.Serialize(writer, this);

                // Close the connection
                writer.Close();
            }
            catch (Exception exception)
            {
                throw new Exception("Failed to Serialize the NetworkModel to the Configuration File: " + exception.ToString());
            }
        }

        #endregion
    }

    class Program
    {
        public static void Main()
        {
            // set the  directory      
            string testCase = (@"C:\Users\Duotong\Desktop\2019SUM\LocalVoltageControl\Test1\\");
            string inputdatafolder = (@"C:\Users\Duotong\Desktop\2019SUM\LocalVoltageControl\Test1\Data\\");
            string logsfolder = (@"C:\Users\Duotong\Desktop\2019SUM\LocalVoltageControl\Test1\Logs\\");
            string pythonCmdPath = (@"C:\Users\Duotong\Desktop\2019SUM\LocalVoltageControl\Test1\pythonCode\\");

            // Set the Path for BenchMark Model and Configuration
            string caseName = testCase + "2019SUM_2013Series_Updated_forLocalVoltageControl_BenchMark.sav";
            string testCaseName = testCase + "2019SUM_2013Series_Updated_forLocalVoltageControl_BenchMark_test.sav";
            string configurationPathName = inputdatafolder + "Configurations.xml";

            VoltVarControllerAdapter VCAdapter = new VoltVarControllerAdapter();
            ReadInputAdapter ReadIn = new ReadInputAdapter();
            VoltVarController Frame = new VoltVarController();
            PythonScripts Python = new PythonScripts();
            VoltVarController PreviousFrame = new VoltVarController();

            #region [ Initialization ]

            Python.CleanCmd(pythonCmdPath + "CleanData.py", inputdatafolder, logsfolder, pythonCmdPath, caseName, testCaseName); // clean the inputd data, delete the logs, and copy the benchmark model

            VCAdapter.ConfigurationPathName = configurationPathName;
            VCAdapter.Initialize();
            PreviousFrame = VoltVarController.DeserializeFromXml(configurationPathName);

            #endregion

            #region [ Execute Program for Each Frame ] 
            for (int i = 0; i < 30; i++)
            {
                int rowNumber = i + 1;

                string inputFileName = inputdatafolder + String.Format("{0:yyyy-MM-dd  hh-mm-ss}_{1}", DateTime.UtcNow, rowNumber) + ".xml";
                string logsFileName = logsfolder + String.Format("{0:yyyy-MM-dd  hh-mm-ss}_{1}", DateTime.UtcNow, rowNumber) + " Logs.xml";

                Frame = ReadIn.ReadFrame(inputdatafolder, inputFileName, rowNumber);

                VCAdapter.PublishFrame(Frame, inputdatafolder, PreviousFrame);

                PreviousFrame = VCAdapter.InputFrame;

                VCAdapter.InputFrame.SerializeToXml(inputFileName);

                VCAdapter.SerializeToXml(logsFileName);



                #region [ Control PSSE ]

                try
                {

                    Python.RunCmd(pythonCmdPath + "DVPScaleLoad.py", VCAdapter.LogMessage,inputdatafolder, VCAdapter.InputFrame.SubstationInformation, testCaseName, VCAdapter.InputFrame.ControlCapacitorBanks[0], VCAdapter.InputFrame.ControlCapacitorBanks[1]);
                    //Console.ReadLine();
                    //if (i == 0)
                    //{
                    //    Console.ReadLine();
                    //}


                }
                catch 
                {
                    Console.ReadLine();
                }

                #endregion

            }

            #endregion
        }
    }
}
