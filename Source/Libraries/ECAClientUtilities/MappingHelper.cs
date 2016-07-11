//******************************************************************************************************
//  MappingHelper.cs - Gbtc
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
//  07/08/2016 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using ECAClientFramework;
using GSF.TimeSeries;

namespace ECAClientUtilities
{
    /// <summary>
    /// Defines a delegate based implementation of <see cref="MapperBase"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For .NET implementations that cannot sub-class the mapper base class, a mapper helper can be created
    /// that takes a delegate for custom mapper implementations.
    /// </para>
    /// <para>
    /// Implementors are expected to manually assign <see cref="MapperBase.InputMapping"/> that is otherwise
    /// normally accepted via base class constructor.
    /// </para>
    /// </remarks>
    public class MappingHelper : MapperBase
    {
        #region [ Members ]

        // Fields
        private Action<IDictionary<MeasurementKey, IMeasurement>> m_mapFunction;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="MappingHelper"/>.
        /// </summary>
        /// <param name="signalLookup">Signal lookup instance.</param>
        public MappingHelper(SignalLookup signalLookup) : base(signalLookup, null)
        {
        }

        /// <summary>
        /// Gets or sets mapping function delegate for helper class.
        /// </summary>
        public Action<IDictionary<MeasurementKey, IMeasurement>> MapFunction
        {
            get
            {
                return m_mapFunction;
            }
            set
            {
                m_mapFunction = value;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Maps the given collection of measurements to the algorithm's input type and calls the user-defined algorithm.
        /// </summary>
        /// <param name="measurements">The collection of measurement received from the server.</param>
        public override void Map(IDictionary<MeasurementKey, IMeasurement> measurements)
        {
            m_mapFunction(measurements);
        }

        #endregion
    }
}
