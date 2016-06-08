// COMPILER GENERATED CODE
// THIS WILL BE OVERWRITTEN AT EACH GENERATION
// EDIT AT YOUR OWN RISK

using System.Collections.Generic;
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

        public void Map(IDictionary<MeasurementKey, IMeasurement> measurements)
        {
            m_lookup.UpdateMeasurementLookup(measurements);
            
            PowerCalculator.Model.Test.VIPair input = new PowerCalculator.Model.Test.VIPair();
            input.Voltage = new PowerCalculator.Model.Test.Phasor();
            input.Voltage.Magnitude = (double)m_lookup.GetMeasurement(@"FILTER ActiveMeasurements WHERE SignalReference = 'TESTDEVICE-PM1'").Value;
            input.Voltage.Angle = (double)m_lookup.GetMeasurement(@"FILTER ActiveMeasurements WHERE SignalReference = 'TESTDEVICE-PA1'").Value;
            input.Current = new PowerCalculator.Model.Test.Phasor();
            input.Current.Magnitude = (double)m_lookup.GetMeasurement(@"FILTER ActiveMeasurements WHERE SignalReference = 'TESTDEVICE-PM3'").Value;
            input.Current.Angle = (double)m_lookup.GetMeasurement(@"FILTER ActiveMeasurements WHERE SignalReference = 'TESTDEVICE-PA3'").Value;

            PowerCalculator.Model.Test.Power output = PowerCalculator.Algorithm.Execute(input);

            // TODO: Later versions will publish this to the openECA server
        }

        #endregion
    }
}
