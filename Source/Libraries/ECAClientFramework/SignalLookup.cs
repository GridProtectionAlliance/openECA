//******************************************************************************************************
//  SignalLookup.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  06/01/2016 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using GSF.Data;
using GSF.TimeSeries;
using GSF.TimeSeries.Adapters;

namespace ECAClientFramework
{
    public class SignalLookup
    {
        #region [ Members ]

        // Fields
        private DataSet m_dataSource;
        private IDictionary<MeasurementKey, IMeasurement> m_measurementLookup;

        #endregion

        #region [ Methods ]

        public void CrunchMetadata(DataSet metadata)
        {
            DataSet dataSource = new DataSet();
            DataTable activeMeasurements = new DataTable("ActiveMeasurements");

            activeMeasurements.Columns.Add("SourceNodeID", typeof(Guid));
            activeMeasurements.Columns.Add("ID", typeof(string));
            activeMeasurements.Columns.Add("SignalID", typeof(Guid));
            activeMeasurements.Columns.Add("PointTag", typeof(string));
            activeMeasurements.Columns.Add("AlternateTag", typeof(string));
            activeMeasurements.Columns.Add("SignalReference", typeof(string));
            activeMeasurements.Columns.Add("Internal", typeof(bool));
            activeMeasurements.Columns.Add("Subscribed", typeof(bool));
            activeMeasurements.Columns.Add("Device", typeof(string));
            activeMeasurements.Columns.Add("DeviceID", typeof(int));
            activeMeasurements.Columns.Add("FramesPerSecond", typeof(int));
            activeMeasurements.Columns.Add("Protocol", typeof(string));
            activeMeasurements.Columns.Add("ProtocolType", typeof(string));
            activeMeasurements.Columns.Add("SignalType", typeof(string));
            activeMeasurements.Columns.Add("EngineeringUnits", typeof(string));
            activeMeasurements.Columns.Add("PhasorID", typeof(int));
            activeMeasurements.Columns.Add("PhasorType", typeof(string));
            activeMeasurements.Columns.Add("Phase", typeof(string));
            activeMeasurements.Columns.Add("Adder", typeof(double));
            activeMeasurements.Columns.Add("Multiplier", typeof(double));
            activeMeasurements.Columns.Add("Company", typeof(string));
            activeMeasurements.Columns.Add("Longitude", typeof(decimal));
            activeMeasurements.Columns.Add("Latitude", typeof(decimal));
            activeMeasurements.Columns.Add("Description", typeof(string));
            activeMeasurements.Columns.Add("UpdatedOn", typeof(DateTime));

            IEnumerable<DataRow> deviceDetail = metadata.Tables["DeviceDetail"].Rows.Cast<DataRow>();
            IEnumerable<DataRow> measurementDetail = metadata.Tables["MeasurementDetail"].Rows.Cast<DataRow>();
            IEnumerable<DataRow> phasorDetail = metadata.Tables["PhasorDetail"].Rows.Cast<DataRow>();
            MeasurementKey key;

            measurementDetail
                .Where(measurement => MeasurementKey.TryCreateOrUpdate(measurement.ConvertField<Guid>("SignalID"), measurement.ConvertField<string>("ID"), out key))
                .GroupJoin(deviceDetail,
                    measurement => measurement.ConvertField<string>("DeviceAcronym"),
                    device => device.ConvertField<string>("Acronym"),
                    (measurement, devices) => new
                    {
                        Measurement = measurement,
                        Device = devices.FirstOrDefault()
                    }
                )
                .GroupJoin(phasorDetail,
                    obj => new
                    {
                        DeviceAcronym = obj.Device?.ConvertField<string>("Acronym"),
                        PhasorSourceIndex = obj.Measurement.ConvertField<int>("PhasorSourceIndex")
                    },
                    phasor => new
                    {
                        DeviceAcronym = phasor.ConvertField<string>("DeviceAcronym"),
                        PhasorSourceIndex = phasor.ConvertField<int>("SourceIndex")
                    },
                    (obj, phasors) => new
                    {
                        Device = obj.Device,
                        Measurement = obj.Measurement,
                        Phasor = phasors.FirstOrDefault()
                    }
                )
                .ToList()
                .ForEach(obj =>
                {
                    activeMeasurements.Rows.Add(
                        obj.Device?.ConvertField<Guid>("NodeID") ?? (object)DBNull.Value,
                        obj.Measurement.ConvertField<string>("ID"),
                        obj.Measurement.ConvertField<Guid>("SignalID"),
                        obj.Measurement.ConvertField<string>("PointTag"),
                        DBNull.Value,
                        obj.Measurement.ConvertField<string>("SignalReference"),
                        obj.Measurement.ConvertField<bool>("Internal"),
                        true,
                        obj.Device?.ConvertField<string>("Acronym") ?? (object)DBNull.Value,
                        DBNull.Value,
                        obj.Device?.ConvertField<int?>("FramesPerSecond") ?? (object)DBNull.Value,
                        DBNull.Value,
                        DBNull.Value,
                        obj.Measurement.ConvertField<string>("SignalAcronym"),
                        DBNull.Value,
                        DBNull.Value,
                        obj.Phasor?.ConvertField<string>("Type") ?? (object)DBNull.Value,
                        obj.Phasor?.ConvertField<string>("Phase") ?? (object)DBNull.Value,
                        0,
                        1,
                        obj.Device?.ConvertField<string>("CompanyAcronym") ?? (object)DBNull.Value,
                        obj.Device?.ConvertField<decimal?>("Longitude") ?? (object)DBNull.Value,
                        obj.Device?.ConvertField<decimal?>("Latitude") ?? (object)DBNull.Value,
                        obj.Measurement.ConvertField<string>("Description"),
                        obj.Measurement.ConvertField<DateTime>("UpdatedOn")
                    );
                });

            dataSource.Tables.Add(activeMeasurements);
            m_dataSource = dataSource;
        }

        public void UpdateMeasurementLookup(IDictionary<MeasurementKey, IMeasurement> measurementLookup)
        {
            m_measurementLookup = measurementLookup;
        }

        public MeasurementKey GetMeasurementKey(string filterExpression)
        {
            MeasurementKey[] keys = AdapterBase.ParseInputMeasurementKeys(m_dataSource, false, filterExpression);

            if (keys.Length > 1)
                throw new InvalidOperationException($"Ambiguous filter returned {keys.Length} measurement keys: {filterExpression}.");

            if (keys.Length == 0)
                return MeasurementKey.Undefined;

            return keys[0];
        }

        public MeasurementKey[] GetMeasurementKeys(string filterExpression)
        {
            return AdapterBase.ParseInputMeasurementKeys(m_dataSource, false, filterExpression);
        }

        public IMeasurement GetMeasurement(MeasurementKey key)
        {
            IMeasurement measurement;

            return m_measurementLookup.TryGetValue(key, out measurement)
                ? measurement
                : Measurement.Undefined;
        }

        public IMeasurement[] GetMeasurements(MeasurementKey[] keys)
        {
            return keys.Select(GetMeasurement).ToArray();
        }

        public IMeasurement GetMeasurement(string filterExpression)
        {
            MeasurementKey[] keys = AdapterBase.ParseInputMeasurementKeys(m_dataSource, false, filterExpression);
            IMeasurement measurement;

            if (keys.Length > 1)
                throw new InvalidOperationException($"Ambiguous filter returned {keys.Length} measurements: {filterExpression}.");

            if (keys.Length == 0 || !m_measurementLookup.TryGetValue(keys[0], out measurement))
                return Measurement.Undefined;

            return measurement;
        }

        public IMeasurement[] GetMeasurements(string filterExpression)
        {
            MeasurementKey[] keys = AdapterBase.ParseInputMeasurementKeys(m_dataSource, false, filterExpression);
            IMeasurement measurement = null;

            return keys
                .Where(key => m_measurementLookup.TryGetValue(key, out measurement))
                .Select(key => measurement)
                .ToArray();
        }

        #endregion
    }
}
