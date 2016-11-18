//******************************************************************************************************
//  MetaSignal.cs - Gbtc
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
//  11/18/2016 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.ComponentModel;
using System.Configuration;
using GSF.Configuration;

namespace ECACommonUtilities.Model
{
    public class MetaSignal
    {
        [Setting]
        public string AnalyticProjectName { get; set; }

        [Setting]
        public string AnalyticInstanceName { get; set; }

        public Guid DeviceID { get; set; }

        [Setting]
        public ushort RuntimeID { get; set; }

        public Guid SignalID { get; set; }

        [Setting]
        public string PointTag { get; set; }

        [Setting]
        public string SignalType { get; set; }

        [Setting]
        public string Description { get; set; }

        #region [ Hidden Properties ]

        [Setting]
        [SettingName("DeviceID")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string _DeviceID
        {
            get
            {
                return DeviceID.ToString();
            }
            set
            {
                Guid id;

                DeviceID = !Guid.TryParse(value, out id)
                    ? Guid.Empty
                    : id;
            }
        }

        [Setting]
        [SettingName("SignalID")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string _SignalID
        {
            get
            {
                return SignalID.ToString();
            }
            set
            {
                Guid id;

                SignalID = !Guid.TryParse(value, out id)
                    ? Guid.Empty
                    : id;
            }
        }

        #endregion
    }
}
