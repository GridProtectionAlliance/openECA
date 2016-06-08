// COMPILER GENERATED CODE
// THIS WILL BE OVERWRITTEN AT EACH GENERATION
// EDIT AT YOUR OWN RISK

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using GSF.TimeSeries;
using PowerCalculator.Framework;

namespace PowerCalculator.Model
{
    [CompilerGenerated]
    public class Mapper
    {
        #region [ Members ]

        // Fields
        private SignalLookup m_lookup;

        private MeasurementKey m_key0;
        private MeasurementKey m_key40;
        private MeasurementKey m_key81;
        private MeasurementKey m_key122;

        #endregion

        #region [ Constructors ]

        public Mapper(SignalLookup lookup)
        {
            m_lookup = lookup;
        }

        #endregion

        #region [ Properties ]

        public SignalLookup Lookup
        {
            get
            {
                return m_lookup;
            }
        }

        #endregion

        #region [ Methods ]

        public void CrunchMetadata(DataSet metadata)
        {
            m_lookup.CrunchMetadata(metadata);

            m_key0 = m_lookup.GetMeasurementKey(@"FILTER ActiveMeasurements WHERE SignalReference = 'TESTDEVICE-PM1'");
            m_key40 = m_lookup.GetMeasurementKey(@"FILTER ActiveMeasurements WHERE SignalReference = 'TESTDEVICE-PA1'");
            m_key81 = m_lookup.GetMeasurementKey(@"FILTER ActiveMeasurements WHERE SignalReference = 'TESTDEVICE-PM3'");
            m_key122 = m_lookup.GetMeasurementKey(@"FILTER ActiveMeasurements WHERE SignalReference = 'TESTDEVICE-PA3'");
        }

        public void Map(IDictionary<MeasurementKey, IMeasurement> measurements)
        {
            m_lookup.UpdateMeasurementLookup(measurements);

            PowerCalculator.Model.Test.VIPair input = new PowerCalculator.Model.Test.VIPair();
            input.Voltage = new PowerCalculator.Model.Test.Phasor();
            input.Voltage.Magnitude = (double)m_lookup.GetMeasurement(m_key0).Value;
            input.Voltage.Angle = (double)m_lookup.GetMeasurement(m_key40).Value;
            input.Current = new PowerCalculator.Model.Test.Phasor();
            input.Current.Magnitude = (double)m_lookup.GetMeasurement(m_key81).Value;
            input.Current.Angle = (double)m_lookup.GetMeasurement(m_key122).Value;

            PowerCalculator.Model.Test.Power output = PowerCalculator.Algorithm.Execute(input);

            // TODO: Later versions will publish this to the openECA server
        }

        #endregion
    }
}
