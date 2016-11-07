//******************************************************************************************************
//  MetaValues.cs - Gbtc
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
//  11/07/2016 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System;

namespace ECAClientFramework
{
    /// <summary>
    /// Meta-values associated with a measurement data value.
    /// </summary>
    /// <remarks>
    /// These values are supplemental to an actual measured data value and will appear in a user-defined that will parallel
    /// the data structure for easy reference.
    /// </remarks>
    public struct MetaValues
    {
        /// <summary>
        /// Gets ID associated with measurement.
        /// </summary>
        /// <remarks>
        /// This will typically be the lookup ID for a matching "SignalID" available in the cached meta-data received from the publisher.
        /// </remarks>
        public Guid ID;

        /// <summary>
        /// Timestamp of received measurement value.
        /// </summary>
        /// <remarks>
        /// For synchrophasor data this will be the GPS accurate timestamp as measured by the input source.
        /// </remarks>
        public DateTime Timestamp;

        /// <summary>
        /// Discrete flags of the received measurement value.
        /// </summary>
        /// <remarks>
        /// Value represents a common flag set derived from a protocol specific input source, e.g., IEEE C37.118.
        /// </remarks>
        public MeasurementFlags Flags;
    }
}
