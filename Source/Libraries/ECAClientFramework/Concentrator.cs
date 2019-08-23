//******************************************************************************************************
//  Concentrator.cs - Gbtc
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

using GSF.TimeSeries;
using System;

namespace ECAClientFramework
{
    public class Concentrator : ConcentratorBase
    {
        #region [ Constructors ]

        public Concentrator(IMapper mapper)
        {
            if ((object)mapper == null)
                throw new ArgumentNullException (nameof(mapper), "No mapper instance was provided to concentrator. Cannot process meta-data without mapper.");

            Mapper = mapper;
        }

        #endregion

        #region [ Properties ]

        public IMapper Mapper { get; }

        #endregion

        #region [ Methods ]

        protected override void PublishFrame(IFrame frame, int index)
        {
            Mapper.Map(frame.Timestamp, frame.Measurements);
        }

        #endregion
    }
}
