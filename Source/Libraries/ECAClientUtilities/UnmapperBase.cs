//******************************************************************************************************
//  UnmapperBase.cs - Gbtc
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
//  01/13/2017 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ECAClientFramework;
using ECACommonUtilities;
using ECACommonUtilities.Model;
using GSF;
using GSF.TimeSeries;

namespace ECAClientUtilities
{
    public abstract class UnmapperBase
    {
        #region [ Members ]

        // Fields
        private Framework m_framework;

        private MappingCompiler m_mappingCompiler;
        private readonly List<MeasurementKey[]> m_keys;
        private int m_keyIndex;
        private int m_lastKeyIndex;

        private string m_outputMapping;

        private Ticks m_currentFrameTime;
        private Ticks m_cachedFrameTime;
        private Ticks[] m_cachedFrameTimes;
        private TypeMapping m_cachedMapping;
        private TypeMapping[] m_cachedMappings;

        #endregion

        #region [ Constructors ]

        public UnmapperBase(Framework framework, MappingCompiler mappingCompiler, string outputMapping)
        {
            m_framework = framework;
            m_mappingCompiler = mappingCompiler;
            m_outputMapping = outputMapping;
            m_keys = new List<MeasurementKey[]>();
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets signal lookup instance.
        /// </summary>
        public SignalLookup SignalLookup => m_framework.SignalLookup;

        /// <summary>
        /// Gets alignment coordinator instance.
        /// </summary>
        public AlignmentCoordinator AlignmentCoordinator => m_framework.AlignmentCoordinator;

        /// <summary>
        /// Gets mapping compiler instance.
        /// </summary>
        public MappingCompiler MappingCompiler => m_mappingCompiler;

        /// <summary>
        /// Gets or sets output mapping.
        /// </summary>
        public string OutputMapping
        {
            get
            {
                return m_outputMapping;
            }
        }

        /// <summary>
        /// Gets or sets the current frame time.
        /// </summary>
        public Ticks CurrentFrameTime
        {
            get
            {
                return m_currentFrameTime;
            }
            set
            {
                m_currentFrameTime = value;
            }
        }

        #endregion

        #region [ Methods ]

        public void CrunchMetadata(DataSet metadata)
        {
            TypeMapping outputMapping = m_mappingCompiler.GetTypeMapping(m_outputMapping);
            BuildMeasurementKeys(outputMapping);
        }

        protected void BurnKeyIndex()
        {
            m_keyIndex++;
        }

        protected int GetArrayMeasurementCount(ArrayMapping arrayMapping)
        {
            MeasurementKey[] keys = m_keys[m_keyIndex++];

            if (arrayMapping.WindowSize != 0.0M)
            {
                AlignmentCoordinator.SampleWindow sampleWindow = CreateSampleWindow(arrayMapping);
                return sampleWindow.GetTimestamps(m_currentFrameTime).Count;
            }

            return keys.Length;
        }

        protected MetaValues CreateMetaValues(FieldMapping fieldMapping)
        {
            if (fieldMapping.RelativeTime != 0.0M)
            {
                AlignmentCoordinator.SampleWindow sampleWindow = CreateSampleWindow(fieldMapping);
                MeasurementKey key = m_keys[m_keyIndex++].Single();
                return AlignmentCoordinator.CreateMetaValue(key, CurrentFrameTime, sampleWindow);
            }

            return new MetaValues()
            {
                ID = m_keys[m_keyIndex++].Single().SignalID,
                Timestamp = m_currentFrameTime,
                Flags = MeasurementFlags.CalculatedValue
            };
        }

        protected List<MetaValues> CreateMetaValues(ArrayMapping arrayMapping)
        {
            MeasurementKey[] keys = m_keys[m_keyIndex++];

            if (arrayMapping.WindowSize != 0.0M)
            {
                AlignmentCoordinator.SampleWindow sampleWindow = CreateSampleWindow(arrayMapping);
                MeasurementKey key = keys.Single();
                return AlignmentCoordinator.CreateMetaValues(key, m_currentFrameTime, sampleWindow);
            }

            if (arrayMapping.RelativeTime != 0.0M)
            {
                AlignmentCoordinator.SampleWindow sampleWindow = CreateSampleWindow(arrayMapping);

                return keys
                    .Select(key => AlignmentCoordinator.CreateMetaValue(key, m_currentFrameTime, sampleWindow))
                    .ToList();
            }

            return keys
                .Select(key => new MetaValues()
                {
                    ID = key.SignalID,
                    Timestamp = m_currentFrameTime,
                    Flags = MeasurementFlags.CalculatedValue
                })
                .ToList();
        }

        protected Ticks GetRelativeFrameTime(FieldMapping fieldMapping)
        {
            IEnumerable<FieldMapping> signalMappings = m_mappingCompiler.TraverseSignalMappings(fieldMapping);
            MeasurementKey[] keys = signalMappings.SelectMany(mapping => SignalLookup.GetMeasurementKeys(mapping.Expression)).ToArray();
            AlignmentCoordinator.SampleWindow sampleWindow = CreateSampleWindow(fieldMapping);
            return sampleWindow.GetTimestamps(CurrentFrameTime).FirstOrDefault();
        }

        protected void PushWindowFrameTime(ArrayMapping arrayMapping)
        {
            if (arrayMapping.WindowSize != 0.0M)
            {
                AlignmentCoordinator.SampleWindow sampleWindow = CreateSampleWindow(arrayMapping);
                m_lastKeyIndex = m_keyIndex;
                m_cachedFrameTime = CurrentFrameTime;
                m_cachedFrameTimes = sampleWindow.GetTimestamps(m_currentFrameTime).ToArray();
                m_cachedMapping = GetTypeMapping(arrayMapping);
            }
            else if (arrayMapping.RelativeTime != 0.0M)
            {
                m_cachedFrameTime = CurrentFrameTime;
                CurrentFrameTime = GetRelativeFrameTime(arrayMapping);
                m_cachedMappings = m_mappingCompiler.EnumerateTypeMappings(arrayMapping.Expression).ToArray();
            }
            else
            {
                m_cachedMappings = m_mappingCompiler.EnumerateTypeMappings(arrayMapping.Expression).ToArray();
            }
        }

        protected void PopWindowFrameTime(ArrayMapping arrayMapping)
        {
            if (arrayMapping.RelativeTime != 0.0M)
                CurrentFrameTime = m_cachedFrameTime;
        }

        protected void PushRelativeFrameTime(FieldMapping fieldMapping)
        {
            if (fieldMapping.RelativeTime != 0.0M)
            {
                m_cachedFrameTime = CurrentFrameTime;
                CurrentFrameTime = GetRelativeFrameTime(fieldMapping);
            }
        }

        protected void PopRelativeFrameTime(FieldMapping fieldMapping)
        {
            if (fieldMapping.RelativeTime != 0.0M)
                CurrentFrameTime = m_cachedFrameTime;
        }

        protected int GetUDTArrayTypeMappingCount(ArrayMapping arrayMapping)
        {
            if (arrayMapping.WindowSize != 0.0M)
                return m_cachedFrameTimes.Length;

            return m_cachedMappings.Length;
        }

        protected TypeMapping GetUDTArrayTypeMapping(ArrayMapping arrayMapping, int index)
        {
            if (arrayMapping.WindowSize != 0.0M)
            {
                m_keyIndex = m_lastKeyIndex;
                CurrentFrameTime = m_cachedFrameTimes[index];
                return m_cachedMapping;
            }

            return m_cachedMappings[index];
        }

        /// <summary>
        /// Creates a sample window defined by the given field mapping.
        /// </summary>
        /// <param name="fieldMapping">The mapping that defines the parameters for the sample window.</param>
        /// <returns>The sample window defined by the given field mapping.</returns>
        protected AlignmentCoordinator.SampleWindow CreateSampleWindow(FieldMapping fieldMapping)
        {
            decimal relativeTime = fieldMapping.RelativeTime;
            TimeSpan relativeUnit = fieldMapping.RelativeUnit;
            decimal sampleRate = fieldMapping.SampleRate;
            TimeSpan sampleUnit = fieldMapping.SampleUnit;
            return AlignmentCoordinator.CreateSampleWindow(relativeTime, relativeUnit, sampleRate, sampleUnit);
        }

        protected AlignmentCoordinator.SampleWindow CreateSampleWindow(ArrayMapping arrayMapping)
        {
            decimal relativeTime = arrayMapping.RelativeTime;
            TimeSpan relativeUnit = arrayMapping.RelativeUnit;
            decimal sampleRate = arrayMapping.SampleRate;
            TimeSpan sampleUnit = arrayMapping.SampleUnit;
            decimal windowSize = arrayMapping.WindowSize;
            TimeSpan windowUnit = arrayMapping.WindowUnit;

            // Collection with no time window
            if (windowSize == 0.0M)
                return CreateSampleWindow((FieldMapping)arrayMapping);

            // No relative time indicates that the relative
            // time is actually equal to the window size
            //  Ex: last 5 seconds
            if (relativeTime == 0.0M)
            {
                relativeTime = windowSize;
                relativeUnit = windowUnit;
            }

            return AlignmentCoordinator.CreateSampleWindow(relativeTime, relativeUnit, sampleRate, sampleUnit, windowSize, windowUnit);
        }

        protected TypeMapping GetTypeMapping(FieldMapping fieldMapping)
        {
            return m_mappingCompiler.GetTypeMapping(fieldMapping.Expression);
        }

        protected IMeasurement MakeMeasurement(MetaValues meta, double value)
        {
            return new Measurement()
            {
                Metadata = MeasurementKey.LookUpBySignalID(meta.ID).Metadata,
                Timestamp = meta.Timestamp,
                Value = value,
                StateFlags = GetMeasurementStateFlags(meta.Flags)
            };
        }

        protected MeasurementStateFlags GetMeasurementStateFlags(MeasurementFlags ecaFlags)
        {
            MeasurementStateFlags tslFlags = MeasurementStateFlags.Normal;

            if (ecaFlags.HasFlag(MeasurementFlags.BadValue))
                tslFlags |= MeasurementStateFlags.BadData;

            if (ecaFlags.HasFlag(MeasurementFlags.BadTime))
                tslFlags |= MeasurementStateFlags.BadTime;

            if (ecaFlags.HasFlag(MeasurementFlags.CalculatedValue))
                tslFlags |= MeasurementStateFlags.CalculatedValue;

            if (ecaFlags.HasFlag(MeasurementFlags.UnreasonableValue))
                tslFlags |= MeasurementStateFlags.OverRangeError | MeasurementStateFlags.UnderRangeError;

            if (ecaFlags.HasFlag(MeasurementFlags.UserDefinedFlag1))
                tslFlags |= MeasurementStateFlags.UserDefinedFlag1;

            if (ecaFlags.HasFlag(MeasurementFlags.UserDefinedFlag2))
                tslFlags |= MeasurementStateFlags.UserDefinedFlag2;

            return tslFlags;
        }

        protected void Reset()
        {
            m_keyIndex = 0;
        }

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
                    m_keys.Add(SignalLookup.GetMeasurementKeys(fieldMapping.Expression));
                else
                    m_keys.Add(new[] { SignalLookup.GetMeasurementKey(fieldMapping.Expression) });
            }
        }

        #endregion
    }
}
