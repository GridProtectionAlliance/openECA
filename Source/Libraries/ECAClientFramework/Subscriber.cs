//******************************************************************************************************
//  Subscriber.cs - Gbtc
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

using ECACommonUtilities;
using ECACommonUtilities.Model;
using GSF;
using GSF.Configuration;
using GSF.TimeSeries;
using GSF.TimeSeries.Transport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;

// ReSharper disable PossibleMultipleEnumeration
namespace ECAClientFramework
{
    public class Subscriber
    {
        #region [ Members ]

        private readonly Concentrator m_concentrator;

        // Events
        public event EventHandler<EventArgs<string>> StatusMessage;
        public event EventHandler<EventArgs<Exception>> ProcessException;

        #endregion

        #region [ Constructors ]

        public Subscriber(Concentrator concentrator)
        {
            if ((object)concentrator == null)
                throw new ArgumentNullException(nameof(concentrator), "No concentrator instance was provider to subscriber.");

            m_concentrator = concentrator;

            DataSubscriber = new DataSubscriber();
            DataSubscriber.ConnectionEstablished += DataSubscriber_ConnectionEstablished;
            DataSubscriber.MetaDataReceived += DataSubscriber_MetaDataReceived;
            DataSubscriber.NewMeasurements += DataSubscriber_NewMeasurements;
            DataSubscriber.StatusMessage += DataSubscriber_StatusMessage;
            DataSubscriber.ProcessException += DataSubscriber_ProcessException;
        }

        #endregion

        #region [ Properties ]

        public bool UseConcentration { get; set; }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public DataSubscriber DataSubscriber { get; }

        public string Status => DataSubscriber.Status;

        #endregion

        #region [ Methods ]

        public void Start()
        {
            if (!DataSubscriber.Initialized)
            {
                DataSubscriber.ConnectionString = SystemSettings.ConnectionString;
                DataSubscriber.Initialize();
            }

            if (!DataSubscriber.IsConnected)
                DataSubscriber.Start();
        }

        public void Stop()
        {
            DataSubscriber.Stop();
            DataSubscriber.Dispose();
        }

        public void SendMetadata(MetaSignal metaSignal)
        {
            string message = new ConnectionStringParser<SettingAttribute>().ComposeConnectionString(metaSignal);
            DataSubscriber.SendServerCommand((ServerCommand)ECAServerCommand.MetaSignal, message);
        }

        public void SendMeasurements(IEnumerable<IMeasurement> measurements)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(BigEndian.GetBytes(measurements.Count()));

                foreach (IMeasurement measurement in measurements)
                {
                    ECAMeasurement ecaMeasurement = new ECAMeasurement()
                    {
                        SignalID = measurement.ID,
                        Timestamp = measurement.Timestamp,
                        Value = measurement.Value,
                        StateFlags = measurement.StateFlags
                    };

                    writer.Write(ecaMeasurement.SignalID.ToRfcBytes());
                    writer.Write(BigEndian.GetBytes(ecaMeasurement.Timestamp.Ticks));
                    writer.Write(BigEndian.GetBytes(ecaMeasurement.Value));
                    writer.Write(BigEndian.GetBytes((uint)ecaMeasurement.StateFlags));
                }

                DataSubscriber.SendServerCommand((ServerCommand)ECAServerCommand.SendMeasurements, stream.ToArray());
            }
        }

        public void SendStatusMessage(UpdateType updateType, string message)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                byte[] messageBytes = DataSubscriber.Encoding.GetBytes(message);

                writer.Write((byte)updateType);
                writer.Write(BigEndian.GetBytes(message.Length));
                writer.Write(messageBytes);
                writer.Flush();

                DataSubscriber.SendServerCommand((ServerCommand)ECAServerCommand.StatusMessage, stream.ToArray());
            }
        }

        private void DataSubscriber_ConnectionEstablished(object sender, EventArgs args)
        {
            DataSubscriber.RefreshMetadata();
        }

        private void DataSubscriber_MetaDataReceived(object sender, EventArgs<DataSet> args)
        {
            UnsynchronizedSubscriptionInfo subscriptionInfo = new UnsynchronizedSubscriptionInfo(false);

            if (args?.Argument != null)
                m_concentrator.Mapper.CrunchMetadata(args.Argument);
            else
                throw new ArgumentNullException(nameof(args), "No meta-data dataset received from data subscriber");

            subscriptionInfo.FilterExpression = m_concentrator.Mapper.FilterExpression;
            DataSubscriber.Subscribe(subscriptionInfo);
        }

        private void DataSubscriber_NewMeasurements(object sender, EventArgs<ICollection<IMeasurement>> args)
        {
            if (UseConcentration)
            {
                m_concentrator.SortMeasurements(args.Argument);
            }
            else
            {
                foreach (IGrouping<Ticks, IMeasurement> grouping in args.Argument.GroupBy(measurement => measurement.Timestamp))
                    m_concentrator.Mapper.Map(grouping.Key, grouping.ToDictionary(measurement => measurement.Key));
            }

            foreach (IMeasurement measurement in args.Argument)
            {
                if (m_concentrator.Mapper.SignalBuffers.TryGetValue(measurement.Key, out SignalBuffer signalBuffer))
                    signalBuffer.Queue(measurement);
            }
        }

        private void DataSubscriber_StatusMessage(object sender, EventArgs<string> args)
        {
            if ((object)StatusMessage != null)
                StatusMessage(sender, args);
        }

        private void DataSubscriber_ProcessException(object sender, EventArgs<Exception> args)
        {
            if ((object)ProcessException != null)
                ProcessException(sender, args);
        }

        #endregion
    }
}
