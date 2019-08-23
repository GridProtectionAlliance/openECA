//******************************************************************************************************
//  ActiveMeasurement.cs - Gbtc
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

using GSF.Data;
using GSF.TimeSeries;
using System;
using System.Data;

namespace ECAClientUtilities.API
{
    public class ActiveMeasurement
    {
        #region [ Members ]

        // Fields
        private MeasurementKey m_key;

        #endregion

        #region [ Constructors ]

        public ActiveMeasurement()
        {
            m_key = MeasurementKey.Undefined;
            ID = m_key.ToString();
            SignalID = m_key.SignalID;
        }

        public ActiveMeasurement(DataRow row)
        {
            ID = Get<string>(row, nameof(ID));
            SignalID = Get<Guid?>(row, nameof(SignalID));
            PointTag = Get<string>(row, nameof(PointTag));
            AlternateTag = Get<string>(row, nameof(AlternateTag));
            SignalReference = Get<string>(row, nameof(SignalReference));
            Internal = Get<bool>(row, nameof(Internal));
            Subscribed = Get<bool>(row, nameof(Subscribed));
            Device = Get<string>(row, nameof(Device));
            DeviceID = Get<int?>(row, nameof(DeviceID));
            FramesPerSecond = Get<int?>(row, nameof(FramesPerSecond));
            Protocol = Get<string>(row, nameof(Protocol));
            ProtocolType = Get<string>(row, nameof(ProtocolType));
            SignalType = Get<string>(row, nameof(SignalType));
            EngineeringUnits = Get<string>(row, nameof(EngineeringUnits));
            PhasorID = Get<int?>(row, nameof(PhasorID));
            PhasorType = Get<char?>(row, nameof(PhasorType));
            Phase = Get<char?>(row, nameof(Phase));
            Adder = Get<double>(row, nameof(Adder));
            Multiplier = Get<double>(row, nameof(Multiplier));
            Company = Get<string>(row, nameof(Company));
            Longitude = Get<decimal>(row, nameof(Longitude));
            Latitude = Get<decimal>(row, nameof(Latitude));
            Description = Get<string>(row, nameof(Description));
            UpdatedOn = Get<DateTime>(row, nameof(UpdatedOn));
        }

        #endregion

        #region [ Properties ]

        public string ID
        {
            get;
            set;
        }

        public string Source => Key.Source;

        public ulong PointID => Key.ID;

        public Guid? SignalID
        {
            get;
            set;
        }

        public string PointTag
        {
            get;
            set;
        }

        public string AlternateTag
        {
            get;
            set;
        }

        public string SignalReference
        {
            get;
            set;
        }

        public bool Internal
        {
            get;
            set;
        }

        public bool Subscribed
        {
            get;
            set;
        }

        public string Device
        {
            get;
            set;
        }

        public int? DeviceID
        {
            get;
            set;
        }

        public int? FramesPerSecond
        {
            get;
            set;
        }

        public string Protocol
        {
            get;
            set;
        }

        public string ProtocolType
        {
            get;
            set;
        }

        public string SignalType
        {
            get;
            set;
        }

        public string EngineeringUnits
        {
            get;
            set;
        }

        public int? PhasorID
        {
            get;
            set;
        }

        public char? PhasorType
        {
            get;
            set;
        }

        public char? Phase
        {
            get;
            set;
        }

        public double Adder
        {
            get;
            set;
        }

        public double Multiplier
        {
            get;
            set;
        }

        public string Company
        {
            get;
            set;
        }


        public decimal Longitude
        {
            get;
            set;
        }

        public decimal Latitude
        {
            get;
            set;
        }


        public string Description
        {
            get;
            set;
        }

        public DateTime UpdatedOn
        {
            get;
            set;
        }

        private MeasurementKey Key
        {
            get
            {
                if (SignalID == m_key.SignalID && ID == m_key.ToString())
                    return m_key;

                // ReSharper disable once RedundantCast.0
                if ((object)SignalID == null || SignalID == Guid.Empty)
                    m_key = MeasurementKey.Undefined;
                else
                    m_key = MeasurementKey.CreateOrUpdate(SignalID.GetValueOrDefault(), ID);

                return m_key;
            }
        }

        #endregion

        #region [ Methods ]

        private T Get<T>(DataRow row, string columnName)
        {
            if (row.Table.Columns.Contains(columnName))
                return row.ConvertField<T>(columnName);

            return default(T);
        }

        #endregion
    }
}
