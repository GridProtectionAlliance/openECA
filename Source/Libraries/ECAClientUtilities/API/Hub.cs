//******************************************************************************************************
//  Hub.cs - Gbtc
//
//  Copyright © 2017, Grid Protection Alliance.  All Rights Reserved.
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
//  04/05/2017 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using ECAClientFramework;
using GSF;
using GSF.Data;
using GSF.TimeSeries;

namespace ECAClientUtilities.API
{
    /// <summary>
    /// Represents the hub that exposes API calls to client algorithms.
    /// </summary>
    public class Hub
    {
        #region [ Members ]

        // Fields
        private object m_lookupLock;
        private Dictionary<Guid, DataRow> m_metadataLookup;
        private Dictionary<Guid, ActiveMeasurement> m_metadataModelLookup;
        private Action m_metadataUpdatedCallback;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="Hub"/> class.
        /// </summary>
        /// <param name="framework">The framework that the user interacts with through this hub.</param>
        public Hub(Framework framework)
        {
            Framework = framework;
            m_lookupLock = new object();
            m_metadataLookup = new Dictionary<Guid, DataRow>();
            m_metadataModelLookup = new Dictionary<Guid, ActiveMeasurement>();

            Framework.SignalLookup.MetadataUpdated += (sender, args) =>
            {
                lock (m_lookupLock)
                {
                    Func<DataRow, Guid> getSignalID = row => row.ConvertField<Guid>("SignalID");
                    m_metadataLookup = args.Argument.Tables["ActiveMeasurements"].Select().ToDictionary(getSignalID);
                    m_metadataModelLookup.Clear();
                }

                m_metadataUpdatedCallback?.Invoke();
            };
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// The underlying framework object that the user interacts with through the hub.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public Framework Framework { get; }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Creates an output signal on the server.
        /// </summary>
        public Guid CreateOutputMetadata()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a data row that contains information about the signal identified by the given signal ID.
        /// </summary>
        /// <param name="signalID">The ID of the signal to query for information.</param>
        /// <returns>Information about the signal identified by the given signal ID.</returns>
        public DataRow GetRawMetadata(Guid signalID)
        {
            lock (m_lookupLock)
            {
                DataRow metadata;

                if ((object)m_metadataLookup == null)
                    return null;

                if (!m_metadataLookup.TryGetValue(signalID, out metadata))
                    return null;

                return metadata;
            }
        }

        /// <summary>
        /// Gets a structured metadata object that contains information about the signal identified by the given signal ID.
        /// </summary>
        /// <param name="signalID">The ID of the signal to query for information.</param>
        /// <returns>Information about the signal identified by the given signal ID.</returns>
        public ActiveMeasurement GetMetadata(Guid signalID)
        {
            lock (m_lookupLock)
            {
                ActiveMeasurement metadata;

                if (m_metadataModelLookup.TryGetValue(signalID, out metadata))
                    return metadata;

                DataRow rawMetadata = GetRawMetadata(signalID);

                if ((object)rawMetadata == null)
                    return null;

                metadata = new ActiveMeasurement(rawMetadata);
                m_metadataModelLookup[signalID] = metadata;
                return metadata;
            }
        }

        /// <summary>
        /// Gets the minimum amount of time that data should be retained in the
        /// signal buffer for the signal identified by the given signal ID.
        /// </summary>
        /// <param name="signalID">The ID of the signal.</param>
        /// <returns>The retention time of the signal.</returns>
        public TimeSpan GetMinimumRetentionTime(Guid signalID)
        {
            return GetMinimumRetentionTime(MeasurementKey.LookUpBySignalID(signalID));
        }

        /// <summary>
        /// Gets the minimum amount of time that data should be retained in the
        /// signal buffer for the signal identified by the given measurement key.
        /// </summary>
        /// <param name="key">The measurement key that identifies the signal.</param>
        /// <returns>The retention time of the signal.</returns>
        public TimeSpan GetMinimumRetentionTime(MeasurementKey key)
        {
            return Framework.Mapper.GetMinimumRetentionTime(key);
        }

        /// <summary>
        /// Gets a lookup table containing all minimum retention times for all signals with signal buffers.
        /// </summary>
        /// <returns>The collection of minimum retention times by signal.</returns>
        public IDictionary<MeasurementKey, TimeSpan> GetAllMinimumRetentionTimes()
        {
            return Framework.Mapper.GetAllMinimumRetentionTimes();
        }

        /// <summary>
        /// Sets the minimum retention time of the signal identified by the given signal ID.
        /// </summary>
        /// <param name="signalID">The ID of the signal.</param>
        /// <param name="retentionTime">The minimum retention time of the signal.</param>
        public void SetMinimumRetentionTime(Guid signalID, TimeSpan retentionTime)
        {
            SetMinimumRetentionTime(MeasurementKey.LookUpBySignalID(signalID), retentionTime);
        }

        /// <summary>
        /// Sets the minimum retention time of the signal identified by the given measurement key.
        /// </summary>
        /// <param name="key">The measurement key that identifies the signal.</param>
        /// <param name="retentionTime">The minimum retention time of the signal.</param>
        public void SetMinimumRetentionTime(MeasurementKey key, TimeSpan retentionTime)
        {
            Framework.Mapper.SetMinimumRetentionTime(key, retentionTime);
        }

        /// <summary>
        /// Queries the signal buffer for the measurement whose timestamp is nearest the given timestamp.
        /// </summary>
        /// <param name="signalID">The ID for the signal to be queried.</param>
        /// <param name="timestamp">The timestamp of the measurement to be queried.</param>
        /// <returns>The measurement whose timestamp is nearest the given timestamp.</returns>
        public IMeasurement QuerySignalBuffer(Guid signalID, DateTime timestamp)
        {
            MeasurementKey key = MeasurementKey.LookUpBySignalID(signalID);

            if (key == MeasurementKey.Undefined)
                return null;

            return QuerySignalBuffer(key, timestamp);
        }

        /// <summary>
        /// Queries the signal buffer for the measurement whose timestamp is nearest the given timestamp.
        /// </summary>
        /// <param name="key">The key which identifies the signal to be queried.</param>
        /// <param name="timestamp">The timestamp of the measurement to be queried.</param>
        /// <returns>The measurement whose timestamp is nearest the given timestamp.</returns>
        public IMeasurement QuerySignalBuffer(MeasurementKey key, DateTime timestamp)
        {
            SignalBuffer signalBuffer;

            if (!Framework.SignalBuffers.TryGetValue(key, out signalBuffer))
                return null;

            return signalBuffer.GetNearestMeasurement(timestamp);
        }

        /// <summary>
        /// Queries the signal buffer for the measurement whose timestamp is nearest the given timestamp,
        /// but only returns the measurement if its timestamp falls within a specified tolerance around the given timestamp.
        /// </summary>
        /// <param name="signalID">The ID of the signal to be queried.</param>
        /// <param name="timestamp">The timestamp of the measurement to be queried.</param>
        /// <param name="tolerance">The tolerance that determines whether the nearest measurement is valid.</param>
        /// <returns>The measurement whose timestamp is nearest the given timestamp or null if the measurement's timestamp falls outside the given tolerance.</returns>
        public IMeasurement QuerySignalBuffer(Guid signalID, DateTime timestamp, TimeSpan tolerance)
        {
            MeasurementKey key = MeasurementKey.LookUpBySignalID(signalID);

            if (key == MeasurementKey.Undefined)
                return null;

            return QuerySignalBuffer(key, timestamp, tolerance);
        }

        /// <summary>
        /// Queries the signal buffer for the measurement whose timestamp is nearest the given timestamp,
        /// but only returns the measurement if its timestamp falls within a specified tolerance around the given timestamp.
        /// </summary>
        /// <param name="key">The key which identifies the signal to be queried.</param>
        /// <param name="timestamp">The timestamp of the measurement to be queried.</param>
        /// <param name="tolerance">The tolerance that determines whether the nearest measurement is valid.</param>
        /// <returns>The measurement whose timestamp is nearest the given timestamp or null if the measurement's timestamp falls outside the given tolerance.</returns>
        public IMeasurement QuerySignalBuffer(MeasurementKey key, DateTime timestamp, TimeSpan tolerance)
        {
            IMeasurement nearestMeasurement = QuerySignalBuffer(key, timestamp);

            if ((object)nearestMeasurement == null)
                return null;

            if (nearestMeasurement.Timestamp < (timestamp - tolerance).Ticks || nearestMeasurement.Timestamp > (timestamp + tolerance).Ticks)
                return null;

            return nearestMeasurement;
        }

        /// <summary>
        /// Queries the signal buffer for the collection of measurements
        /// between the dates specified by the given time range.
        /// </summary>
        /// <param name="signalID">The ID of the signal to be queried.</param>
        /// <param name="timeRange">The time range to be queried.</param>
        /// <returns>The collection of measurements for the signal that fall between the dates in the given time range.</returns>
        public IEnumerable<IMeasurement> QuerySignalBuffer(Guid signalID, Range<DateTime> timeRange)
        {
            return QuerySignalBuffer(signalID, timeRange.Start, timeRange.End);
        }

        /// <summary>
        /// Queries the signal buffer for the collection of measurements
        /// between the dates specified by the given time range.
        /// </summary>
        /// <param name="signalID">The ID of the signal to be queried.</param>
        /// <param name="startTime">The beginning of the time range to be queried.</param>
        /// <param name="endTime">The end of the time range to be queried.</param>
        /// <returns>The collection of measurements for the signal that fall between the dates in the given time range.</returns>
        public IEnumerable<IMeasurement> QuerySignalBuffer(Guid signalID, DateTime startTime, DateTime endTime)
        {
            MeasurementKey key = MeasurementKey.LookUpBySignalID(signalID);

            if (key == MeasurementKey.Undefined)
                return Enumerable.Empty<IMeasurement>();

            return QuerySignalBuffer(key, startTime, endTime);
        }

        /// <summary>
        /// Queries the signal buffer for the collection of measurements
        /// between the dates specified by the given time range.
        /// </summary>
        /// <param name="key">The key which identifies the signal to be queried.</param>
        /// <param name="timeRange">The time range to be queried.</param>
        /// <returns>The collection of measurements for the signal that fall between the dates in the given time range.</returns>
        public IEnumerable<IMeasurement> QuerySignalBuffer(MeasurementKey key, Range<DateTime> timeRange)
        {
            return QuerySignalBuffer(key, timeRange.Start, timeRange.End);
        }

        /// <summary>
        /// Queries the signal buffer for the collection of measurements
        /// between the dates specified by the given time range.
        /// </summary>
        /// <param name="key">The key which identifies the signal to be queried.</param>
        /// <param name="startTime">The beginning of the time range to be queried.</param>
        /// <param name="endTime">The end of the time range to be queried.</param>
        /// <returns>The collection of measurements for the signal that fall between the dates in the given time range.</returns>
        public IEnumerable<IMeasurement> QuerySignalBuffer(MeasurementKey key, DateTime startTime, DateTime endTime)
        {
            SignalBuffer signalBuffer;

            if (!Framework.SignalBuffers.TryGetValue(key, out signalBuffer))
                return Enumerable.Empty<IMeasurement>();

            return signalBuffer.GetMeasurements(startTime, endTime);
        }

        /// <summary>
        /// Registers a callback to be called when an updated
        /// set of metadata is received from the server.
        /// </summary>
        /// <param name="callback">The callback to be called when metadata is updated.</param>
        public void RegisterMetadataUpdatedCallback(Action callback)
        {
            m_metadataUpdatedCallback = callback;
        }

        /// <summary>
        /// Gets the underlying framework object that the user interacts with through the hub.
        /// </summary>
        /// <returns>The underlying framework.</returns>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public Framework GetFramework()
        {
            return Framework;
        }

        #endregion
    }
}
