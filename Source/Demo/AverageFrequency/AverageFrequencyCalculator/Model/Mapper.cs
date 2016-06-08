// COMPILER GENERATED CODE
// THIS WILL BE OVERWRITTEN AT EACH GENERATION
// EDIT AT YOUR OWN RISK

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GSF.TimeSeries;
using AverageFrequencyCalculator.Framework;

namespace AverageFrequencyCalculator.Model
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
            
            AverageFrequencyCalculator.Model.Test.FrequencyCollection input = new AverageFrequencyCalculator.Model.Test.FrequencyCollection();
            input.Frequencies = m_lookup.GetMeasurements(@"FILTER ActiveMeasurements WHERE SignalType = 'FREQ'").Select(measurement => (double)measurement.Value).ToArray();

            AverageFrequencyCalculator.Model.Test.AverageFrequency output = AverageFrequencyCalculator.Algorithm.Execute(input);

            // TODO: Later versions will publish this to the openECA server
        }

        #endregion
    }
}
