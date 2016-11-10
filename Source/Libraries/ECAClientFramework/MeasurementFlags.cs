//******************************************************************************************************
//  MeasurementFlags.cs - Gbtc
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
//  11/03/2016 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using GSF;

namespace ECAClientFramework
{
    /// <summary>
    /// Represents a simple set of non-ambiguous flags for measured value states. 
    /// </summary>
    /// <remarks>
    /// These flags are intended to be simple flags that represent a discrete state
    /// for measured values. More complete states, such as those that may be available
    /// based on a source input protocol, can be subscribed to in whole.
    /// </remarks>
    [Flags]
    public enum MeasurementFlags : byte
    {
        /// <summary>
        /// Defines normal state.
        /// </summary>
        Normal = (byte)Bits.Nil,
        /// <summary>
        /// Defines bad time state.
        /// </summary>
        /// <remarks>
        /// Set when a time is considered bad, for any reason.
        /// </remarks>
        BadTime = (byte)Bits.Bit00,
        /// <summary>
        /// Defines bad value state.
        /// </summary>
        /// <remarks>
        /// Set when a value is considered bad, for any reason.
        /// </remarks>
        BadValue = (byte)Bits.Bit01,
        /// <summary>
        /// Defines unreasonable value state.
        /// </summary>
        /// <remarks>
        /// Set when a value is considered unreasonable, e.g., outside engineering limits or latched.
        /// </remarks>
        UnreasonableValue = (byte)Bits.Bit02,
        /// <summary>
        /// Defines calculated value state.
        /// </summary>
        /// <remarks>
        /// Set when value is post-processed, i.e., not measured, rather newly created or modified.
        /// <para>
        /// Note that even though a device could be reporting derived values based on internally sampled
        /// measurements, these reported values would still not be considered calculated. This flag is
        /// intended to indicate that a value was not produced according to a prescriptive methodology
        /// with a GPS accurate timestamp, rather that value was modified or newly calculated.
        /// </para>
        /// </remarks>
        CalculatedValue = (byte)Bits.Bit03,
        /// <summary>
        /// Defines reserved flag 1.
        /// </summary>
        ReservedFlag1 = (byte)Bits.Bit04,
        /// <summary>
        /// Defines reserved flag 2.
        /// </summary>
        ReservedFlag2 = (byte)Bits.Bit05,
        /// <summary>
        /// Defines user defined flag 1.
        /// </summary>
        UserDefinedFlag1 = (byte)Bits.Bit06,
        /// <summary>
        /// Defines user defined flag 2.
        /// </summary>
        UserDefinedFlag2 = (byte)Bits.Bit07
    }
}
