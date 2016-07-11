//******************************************************************************************************
//  MapperBase.cs - Gbtc
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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using ECAClientFramework;
using ECAClientUtilities.Model;
using GSF.TimeSeries;

namespace ECAClientUtilities
{
    /// <summary>
    /// Defines a base implementation of <see cref="IMapper"/>.
    /// </summary>
    public abstract class MapperBase : IMapper
    {
        #region [ Members ]

        // Fields
        private readonly SignalLookup m_signalLookup;
        private readonly MappingCompiler m_mappingCompiler;
        private readonly List<MeasurementKey[]> m_keys;
        private readonly ReadOnlyCollection<MeasurementKey[]> m_readonlyKeys;
        private string m_inputMapping;
        private string m_filterExpression;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="MapperBase"/>.
        /// </summary>
        /// <param name="signalLookup">Signal lookup instance.</param>
        /// <param name="inputMapping">Input mapping name.</param>
        protected MapperBase(SignalLookup signalLookup, string inputMapping)
        {
            m_signalLookup = signalLookup;
            UDTCompiler udtCompiler = new UDTCompiler();
            m_mappingCompiler = new MappingCompiler(udtCompiler);
            m_keys = new List<MeasurementKey[]>();
            m_readonlyKeys = m_keys.AsReadOnly();
            m_inputMapping = inputMapping;

            udtCompiler.Compile(Path.Combine("Model", "UserDefinedTypes.ecaidl"));
            m_mappingCompiler.Compile(Path.Combine("Model", "UserDefinedMappings.ecamap"));
        }

        #endregion

        /// <summary>
        /// Gets the filter expression containing the list of input signals.
        /// </summary>
        public string FilterExpression => m_filterExpression;

        /// <summary>
        /// Gets or sets input mapping.
        /// </summary>
        public string InputMapping
        {
            get
            {
                return m_inputMapping;
            }
            set
            {
                m_inputMapping = value;
            }
        }

        /// <summary>
        /// Gets signal lookup instance.
        /// </summary>
        public SignalLookup SignalLookup => m_signalLookup;

        /// <summary>
        /// Gets mapping compiler.
        /// </summary>
        public MappingCompiler MappingCompiler => m_mappingCompiler;

        /// <summary>
        /// Gets generated key list.
        /// </summary>
        public ReadOnlyCollection<MeasurementKey[]> Keys => m_readonlyKeys;

        void IMapper.CrunchMetadata(DataSet metadata)
        {
            m_signalLookup.CrunchMetadata(metadata);
            BuildMeasurementKeys(m_mappingCompiler.GetTypeMapping(m_inputMapping));
            m_filterExpression = string.Join(";", m_keys.SelectMany(keys => keys).Select(key => key.SignalID).Distinct());
        }

        /// <summary>
        /// Maps the given collection of measurements to the algorithm's
        /// input type and calls the user-defined algorithm.
        /// </summary>
        /// <param name="measurements">The collection of measurement received from the server.</param>
        public abstract void Map(IDictionary<MeasurementKey, IMeasurement> measurements);

        private void BuildMeasurementKeys(TypeMapping inputMapping)
        {
            foreach (FieldMapping fieldMapping in inputMapping.FieldMappings)
            {
                DataType fieldType = fieldMapping.Field.Type;
                DataType underlyingType = (fieldType as ArrayType)?.UnderlyingType;

                // ReSharper disable once PossibleNullReferenceException
                if (fieldType.IsArray && underlyingType.IsUserDefined)
                    m_mappingCompiler.EnumerateTypeMappings(fieldMapping.Expression).ToList().ForEach(BuildMeasurementKeys);
                else if (fieldType.IsUserDefined)
                    BuildMeasurementKeys(m_mappingCompiler.GetTypeMapping(fieldMapping.Expression));
                else if (fieldType.IsArray)
                    m_keys.Add(m_signalLookup.GetMeasurementKeys(fieldMapping.Expression));
                else
                    m_keys.Add(new[] { m_signalLookup.GetMeasurementKey(fieldMapping.Expression) });
            }
        }
    }
}
