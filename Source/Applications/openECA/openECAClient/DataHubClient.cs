﻿//******************************************************************************************************
//  DataHubClient.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  06/07/2016 - Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using GSF;
using GSF.Collections;
using GSF.Data;
using GSF.IO;
using GSF.TimeSeries;
using GSF.TimeSeries.Transport;
using openECAClient.Model;

using Measurement = openECAClient.Model.Measurement;

namespace openECAClient
{
    /// <summary>
    /// Represents a client instance of a <see cref="DataHub"/>.
    /// </summary>
    public class DataHubClient : IDisposable
    {
        #region [ Members ]

        // Fields
        private readonly dynamic m_hubClient;
        private DataSubscriber m_dataSubscription;
        private DataSubscriber m_statisticSubscription;
        private readonly UnsynchronizedSubscriptionInfo m_dataSubscriptionInfo;
        private readonly UnsynchronizedSubscriptionInfo m_statisticSubscriptionInfo;
        private readonly List<Measurement> m_measurements;
        private readonly List<Measurement> m_statistics;
        private readonly List<StatusLight> m_statusLights;
        private readonly List<DeviceDetail> m_deviceDetails;
        private readonly List<MeasurementDetail> m_measurementDetails;
        private readonly List<PhasorDetail> m_phasorDetails;
        private readonly List<SchemaVersion> m_schemaVersion;
        private readonly object m_measurementLock;
        private bool m_disposed;
        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="DataHubClient"/> instance.
        /// </summary>
        /// <param name="hubClient">Hub client connection.</param>
        public DataHubClient(dynamic hubClient)
        {
            m_hubClient = hubClient;
            m_statisticSubscriptionInfo = new UnsynchronizedSubscriptionInfo(false);
            m_dataSubscriptionInfo = new UnsynchronizedSubscriptionInfo(false);
            m_measurements = new List<Measurement>();
            m_statistics = new List<Measurement>();
            m_statusLights = new List<StatusLight>();
            m_deviceDetails = new List<DeviceDetail>();
            m_measurementDetails = new List<MeasurementDetail>();
            m_phasorDetails = new List<PhasorDetail>();
            m_schemaVersion = new List<SchemaVersion>();
            m_measurementLock = new object();
        }

        #endregion

        #region [ Properties ]

        public DataSubscriber DataSubscription
        {
            get
            {
                if (m_dataSubscription == null)
                {
                    try
                    {
                        Program.LogStatus("Initializing data subscriptions...", true);

                        m_dataSubscription = new DataSubscriber();

                        m_dataSubscription.StatusMessage += DataSubscriptionStatusMessage;
                        m_dataSubscription.ProcessException += DataSubscriptionProcessException;
                        m_dataSubscription.ConnectionTerminated += DataSubscriptionConnectionTerminated;
                        m_dataSubscription.NewMeasurements += DataSubscriptionNewMeasurements;

                        m_dataSubscription.ConnectionString = Program.Global.SubscriptionConnectionString;
                        m_dataSubscription.AutoSynchronizeMetadata = false;
                        m_dataSubscription.OperationalModes |= OperationalModes.UseCommonSerializationFormat | OperationalModes.CompressMetadata | OperationalModes.CompressSignalIndexCache;
                        m_dataSubscription.CompressionModes = CompressionModes.GZip;

                        m_dataSubscription.Initialize();
                        m_dataSubscription.Start();
                    }
                    catch (Exception ex)
                    {
                        Program.LogException(new InvalidOperationException($"Failed to initialize and start primary data subscription: {ex.Message}", ex), true);
                    }
                }

                return m_dataSubscription;
            }
        }

        public DataSubscriber StatisticSubscription
        {
            get
            {
                if (m_statisticSubscription == null)
                {
                    try
                    {
                        m_statisticSubscription = new DataSubscriber();

                        m_statisticSubscription.StatusMessage += StatisticSubscriptionStatusMessage;
                        m_statisticSubscription.ProcessException += StatisticSubscriptionProcessException;
                        m_statisticSubscription.ConnectionEstablished += StatisticSubscriptionConnectionEstablished;
                        m_statisticSubscription.ConnectionTerminated += StatisticSubscriptionConnectionTerminated;
                        m_statisticSubscription.NewMeasurements += StatisticSubscriptionNewMeasurements;
                        m_statisticSubscription.MetaDataReceived += StatisticSubscriptionMetaDataReceived;

                        m_statisticSubscription.ConnectionString = Program.Global.SubscriptionConnectionString;
                        m_statisticSubscription.AutoSynchronizeMetadata = false;
                        m_statisticSubscription.OperationalModes |= OperationalModes.UseCommonSerializationFormat | OperationalModes.CompressMetadata | OperationalModes.CompressSignalIndexCache;
                        m_statisticSubscription.CompressionModes = CompressionModes.GZip;

                        m_statisticSubscription.Initialize();
                        m_statisticSubscription.Start();
                    }
                    catch (Exception ex)
                    {
                        Program.LogException(new InvalidOperationException($"Failed to initialize and start statistic data subscription: {ex.Message}", ex));
                    }
                }

                return m_statisticSubscription;
            }
        }

        public List<Measurement> Measurements
        {
            get
            {
                List<Measurement> currentMeasurements;

                lock (m_measurementLock)
                {
                    currentMeasurements = new List<Measurement>(m_measurements);
                    m_measurements.Clear();
                }

                return currentMeasurements;
            }
        }

        public List<Measurement> Statistics => m_statistics;

        public List<StatusLight> StatusLights
        {
            get
            {
                foreach (StatusLight statusLight in m_statusLights)
                {
                    DateTime now = DateTime.UtcNow;

                    if ((now - statusLight.Timestamp).TotalSeconds > 30)
                        statusLight.GoodData = false;
                }

                return m_statusLights;
            }
        }

        public List<DeviceDetail> DeviceDetails => m_deviceDetails;

        public List<MeasurementDetail> MeasurementDetails => m_measurementDetails;

        public List<PhasorDetail> PhasorDetails => m_phasorDetails;

        public List<SchemaVersion> SchemaVersion => m_schemaVersion;

        public Action MetadataReceived { get; set; }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases all the resources used by the <see cref="DataHubClient"/> object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="DataHubClient"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                try
                {
                    if (disposing)
                    {
                        m_dataSubscription?.Dispose();
                        m_statisticSubscription?.Dispose();
                    }
                }
                finally
                {
                    m_disposed = true;  // Prevent duplicate dispose.
                }
            }
        }

        public void InitializeSubscriptions()
        {
            DataSubscription.Enabled = true;
            StatisticSubscription.Enabled = true;
        }

        public void TerminateSubscriptions()
        {
            DataSubscription.Unsubscribe();
            StatisticSubscription.Unsubscribe();
        }

        public void ClearMeasurements()
        {
            lock (m_measurementLock)
                m_measurements.Clear();
        }

        public void UpdatePrimaryDataSubscription(string filterExpression)
        {
            ClearMeasurements();

            m_dataSubscriptionInfo.FilterExpression = filterExpression;
            DataSubscription.UnsynchronizedSubscribe(m_dataSubscriptionInfo);
        }

        public void UpdateStatisticsDataSubscription(string filterExpression)
        {
            m_statisticSubscriptionInfo.FilterExpression = filterExpression;
            StatisticSubscription.UnsynchronizedSubscribe(m_statisticSubscriptionInfo);
        }

        private void DataSubscriptionStatusMessage(object sender, EventArgs<string> e)
        {
            Program.LogStatus(e.Argument);
        }

        private void DataSubscriptionNewMeasurements(object sender, EventArgs<ICollection<IMeasurement>> e)
        {
            lock (m_measurementLock)
            {
                foreach (IMeasurement measurement in e.Argument)
                {
                    Measurement value = new Measurement();
                    value.Timestamp = GetUnixMilliseconds(measurement.Timestamp);
                    value.Value = measurement.Value;
                    value.ID = measurement.ID;
                    m_measurements.Add(value);
                }
            }
        }

        private void DataSubscriptionConnectionTerminated(object sender, EventArgs e)
        {
            DataSubscription.Start();
            Program.LogStatus("Connection to publisher was terminated for primary data subscription, restarting connection cycle...", true);
        }

        private void DataSubscriptionProcessException(object sender, EventArgs<Exception> e)
        {
            Exception ex = e.Argument;
            Program.LogException(new InvalidOperationException($"Processing exception encountered by primary data subscription: {ex.Message}", ex), true);
        }


        private void StatisticSubscriptionStatusMessage(object sender, EventArgs<string> e)
        {
            Program.LogStatus(e.Argument);
        }

        private void StatisticSubscriptionMetaDataReceived(object sender, EventArgs<DataSet> e)
        {
            Program.LogStatus("Loading received meta-data...", true);

            DataSet dataSet = e.Argument;

            m_deviceDetails.Clear();
            m_measurementDetails.Clear();
            m_phasorDetails.Clear();
            m_schemaVersion.Clear();
            m_statusLights.Clear();

            foreach (DataTable table in dataSet.Tables)
            {
                if (table.TableName == "DeviceDetail")
                {
                    foreach (DataRow row in table.Rows)
                    {
                        DeviceDetail deviceDetail = new DeviceDetail
                        {
                            NodeID = row.ConvertField<Guid>("NodeID"),
                            UniqueID = row.ConvertField<Guid>("UniqueID"),
                            OriginalSource = row.ConvertField<string>("OriginalSource"),
                            IsConcentrator = row.ConvertField<bool>("IsConcentrator"),
                            Acronym = row.ConvertField<string>("Acronym"),
                            Name = row.ConvertField<string>("Name"),
                            AccessID = row.ConvertField<int>("AccessID"),
                            ParentAcronym = row.ConvertField<string>("ParentAcronym"),
                            ProtocolName = row.ConvertField<string>("ProtocolName"),
                            FramesPerSecond = row.ConvertField<int>("FramesPerSecond"),
                            CompanyAcronym = row.ConvertField<string>("CompanyAcronym"),
                            VendorAcronym = row.ConvertField<string>("VendorAcronym"),
                            VendorDeviceName = row.ConvertField<string>("VendorDeviceName"),
                            Longitude = row.ConvertField<decimal>("Longitude"),
                            Latitude = row.ConvertField<decimal>("Latitude"),
                            InterconnectionName = row.ConvertField<string>("InterconnectionName"),
                            ContactList = row.ConvertField<string>("ContactList"),
                            Enabled = row.ConvertField<bool>("Enabled"),
                            UpdatedOn = row.ConvertField<DateTime>("UpdatedOn")
                        };

                        if (row.ConvertField<bool>("Enabled"))
                        {
                            StatusLight statusLight = new StatusLight
                            {
                                DeviceAcronym = row.ConvertField<string>("Acronym"),
                                Timestamp = DateTime.MinValue,
                                GoodData = false
                            };

                            m_statusLights.Add(statusLight);
                        }

                        m_deviceDetails.Add(deviceDetail);
                    }
                }
                else if (table.TableName == "MeasurementDetail")
                {
                    foreach (DataRow row in table.Rows)
                    {
                        MeasurementDetail measurementDetail = new MeasurementDetail
                        {
                            DeviceAcronym = row.ConvertField<string>("DeviceAcronym"),
                            ID = row.ConvertField<string>("ID"),
                            SignalID = row.ConvertField<Guid>("SignalID"),
                            PointTag = row.ConvertField<string>("PointTag"),
                            SignalReference = row.ConvertField<string>("SignalReference"),
                            SignalAcronym = row.ConvertField<string>("SignalAcronym"),
                            PhasorSourceIndex = row.ConvertField<int>("PhasorSourceIndex"),
                            Description = row.ConvertField<string>("Description"),
                            Internal = row.ConvertField<bool>("Internal"),
                            Enabled = row.ConvertField<bool>("Enabled"),
                            UpdatedOn = row.ConvertField<DateTime>("UpdatedOn")
                        };

                        m_measurementDetails.Add(measurementDetail);
                    }
                }
                else if (table.TableName == "PhasorDetail")
                {
                    foreach (DataRow row in table.Rows)
                    {
                        PhasorDetail phasorDetail = new PhasorDetail
                        {
                            DeviceAcronym = row.ConvertField<string>("DeviceAcronym"),
                            Label = row.ConvertField<string>("Label"),
                            Type = row.ConvertField<string>("Type"),
                            Phase = row.ConvertField<string>("Phase"),
                            SourceIndex = row.ConvertField<int>("SourceIndex"),
                            UpdatedOn = row.ConvertField<DateTime>("UpdatedOn")
                        };

                        m_phasorDetails.Add(phasorDetail);
                    }
                }
                else if (table.TableName == "SchemaVersion")
                {
                    foreach (DataRow row in table.Rows)
                    {
                        SchemaVersion schemaVersion = new SchemaVersion
                        {
                            VersionNumber = row.ConvertField<int>("VersionNumber")
                        };

                        m_schemaVersion.Add(schemaVersion);
                    }
                }
            }

            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string ecaClientDataPath = Path.Combine(appData, "Grid Protection Alliance", "openECAClient");
                string metadataCache = Path.Combine(ecaClientDataPath, "Metadata.xml");

                Directory.CreateDirectory(ecaClientDataPath);

                if (FilePath.TryGetWriteLock(metadataCache))
                {
                    dataSet.WriteXml(metadataCache, XmlWriteMode.WriteSchema);
                    Program.LogStatus($"Data set serialized with {dataSet.Tables.Count} tables...");
                }
            }
            catch (Exception ex)
            {
                Program.LogException(new InvalidOperationException($"Failed to serialize dataset: {ex.Message}", ex));
            }

            try
            {
                m_hubClient.metaDataReceived();
            }
            catch (NullReferenceException)
            {
            }

            MetadataReceived?.Invoke();
        }

        private void StatisticSubscriptionNewMeasurements(object sender, EventArgs<ICollection<IMeasurement>> e)
        {
            foreach (IMeasurement measurement in e.Argument)
            {
                int index = m_statistics.IndexOf(m => m.ID == measurement.ID);

                if (index < 0)
                {
                    Measurement statistic = new Measurement
                    {
                        ID = measurement.ID,
                        Value = measurement.Value,
                        Timestamp = GetUnixMilliseconds(measurement.Timestamp)
                    };

                    m_statistics.Add(statistic);
                }
                else
                {
                    m_statistics[index].Value = measurement.Value;
                    m_statistics[index].Timestamp = GetUnixMilliseconds(measurement.Timestamp);
                }
            }
        }

        private void StatisticSubscriptionConnectionEstablished(object sender, EventArgs e)
        {
            StatisticSubscription.SendServerCommand(ServerCommand.MetaDataRefresh);
        }

        private void StatisticSubscriptionConnectionTerminated(object sender, EventArgs e)
        {
            StatisticSubscription.Start();
            Program.LogStatus("Connection to publisher was terminated for statistic data subscription, restarting connection cycle...");
        }

        private void StatisticSubscriptionProcessException(object sender, EventArgs<Exception> e)
        {
            Exception ex = e.Argument;
            Program.LogException(new InvalidOperationException($"Processing exception encountered by statistic data subscription: {ex.Message}", ex));
        }

        #endregion

        #region [ Static ]

        // Static Methods

        private static double GetUnixMilliseconds(long ticks)
        {
            return new DateTime(ticks).Subtract(new DateTime(UnixTimeTag.BaseTicks)).TotalMilliseconds;
        }

        #endregion
    }
}
