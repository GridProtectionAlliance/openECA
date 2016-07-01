//******************************************************************************************************
//  IMapper.cs - Gbtc
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
//  07/01/2016 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System.Collections.Generic;
using System.Data;
using GSF.TimeSeries;

namespace ECAClientFramework
{
    /// <summary>
    /// Interface for defining an object to map measurements to user-defined types.
    /// </summary>
    public interface IMapper
    {
        /// <summary>
        /// Gets the filter expression containing the list of input signals.
        /// </summary>
        string FilterExpression { get; }

        /// <summary>
        /// Crunches through metadata received by the server
        /// so it can be used for filter expression lookups.
        /// </summary>
        /// <param name="metadata">The set of data tables received from the server.</param>
        void CrunchMetadata(DataSet metadata);

        /// <summary>
        /// Maps the given collection of measurements to the algorithm's
        /// input type and calls the user-defined algorithm.
        /// </summary>
        /// <param name="measurements">The collection of measurement received from the server.</param>
        void Map(IDictionary<MeasurementKey, IMeasurement> measurements);
    }
}
