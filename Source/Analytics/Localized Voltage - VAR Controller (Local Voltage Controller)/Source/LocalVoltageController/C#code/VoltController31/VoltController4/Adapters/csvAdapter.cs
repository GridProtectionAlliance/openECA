using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GSF;
using ReadWriteCSV;
using GSF.TimeSeries.Adapters;
using System.ComponentModel;
using GSF.TimeSeries;
using VoltController.Testing;


namespace VoltController.Adapters
{
    public class CsvAdapter

    {
        #region [ Members ]

        private string[,] m_frame;
        int numberofColumns = 0;
        int numberofRows = 0;
        int numberofFrames = 0;

        #endregion

        #region [ Properties ]
        public string[,] Frame
        {
            get
            {
                return m_frame;
            }
            set
            {
                m_frame = value;
            }
        }


        #endregion

        #region [ Public Methods ]

        public void ReadCSV(string PathName)
        {

            Frame = new string[100, 100];
            using (CsvFileReader reader = new CsvFileReader(PathName))
            {
                CsvRow row = new CsvRow();
                while (reader.ReadRow(row))
                {

                    foreach (string s in row)
                    {
                        Frame[numberofRows, numberofColumns] = s;
                        numberofColumns++;
                    }
                    numberofRows++;
                    numberofColumns = 0;
                }
            }
        }

        #endregion

    }

    //    protected override void PublishFrame(IFrame frame, int index)
    //    {
    //        / Increment number of published frames
    //        numberofFrames++;

    //        / Reset frame counter
    //        if (numberofFrames > 99) /// Change this
    //        {
    //            numberofFrames = 0;
    //        }

    //        / Prepare to clone the output measurements.
    //        IMeasurement[] outputMeasurements = OutputMeasurements;

    //        / Create a list of IMeasurement objects
    //        List<IMeasurement> output = new List<IMeasurement>();

    //        for (int i = 0; i < 6; i++)
    //        {
    //            output.Add(Measurement.Clone(outputMeasurements[i],
    //                                Convert.ToDouble(VoltagePhasorData[numberofFrames, i]),
    //                                                           frame.Timestamp));
    //        }

    //        / Output the next measurement frame
    //        OnNewMeasurements(output);
    //    }


 
    //    public override bool SupportsTemporalProcessing
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //}
}