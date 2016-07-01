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

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using GSF;
using GSF.TimeSeries;
using GSF.TimeSeries.Transport;

namespace ECAClientFramework
{
    public class Subscriber
    {
        #region [ Members ]

        // Fields
        private DataSubscriber m_dataSubscriber;
        private Concentrator m_concentrator;

        // Events
        public event EventHandler<EventArgs<string>> StatusMessage;
        public event EventHandler<EventArgs<Exception>> ProcessException;

        #endregion

        #region [ Constructors ]

        public Subscriber(Concentrator concentrator)
        {
            m_concentrator = concentrator;

            m_dataSubscriber = new DataSubscriber();
            m_dataSubscriber.ConnectionEstablished += DataSubscriber_ConnectionEstablished;
            m_dataSubscriber.MetaDataReceived += DataSubscriber_MetaDataReceived;
            m_dataSubscriber.NewMeasurements += DataSubscriber_NewMeasurements;
            m_dataSubscriber.StatusMessage += DataSubscriber_StatusMessage;
            m_dataSubscriber.ProcessException += DataSubscriber_ProcessException;
        }

        #endregion

        #region [ Properties ]

        public string Status
        {
            get
            {
                return m_dataSubscriber.Status;
            }
        }

        #endregion

        #region [ Methods ]

        public void Start()
        {
            if (!m_dataSubscriber.Initialized)
            {
                m_dataSubscriber.ConnectionString = SystemSettings.ConnectionString;
                m_dataSubscriber.Initialize();
            }

            if (!m_dataSubscriber.IsConnected)
                m_dataSubscriber.Start();
        }

        public void Stop()
        {
            m_dataSubscriber.Stop();
            m_dataSubscriber.Dispose();
        }

        private void DataSubscriber_ConnectionEstablished(object sender, EventArgs args)
        {
            m_dataSubscriber.RefreshMetadata();
        }

        private void DataSubscriber_MetaDataReceived(object sender, EventArgs<DataSet> args)
        {
            UnsynchronizedSubscriptionInfo subscriptionInfo = new UnsynchronizedSubscriptionInfo(false);
            m_concentrator.Mapper.CrunchMetadata(args.Argument);
            subscriptionInfo.FilterExpression = m_concentrator.Mapper.FilterExpression;
            m_dataSubscriber.Subscribe(subscriptionInfo);
        }

        private void DataSubscriber_NewMeasurements(object sender, EventArgs<ICollection<IMeasurement>> args)
        {
            m_concentrator.SortMeasurements(args.Argument);
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
