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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using ECAClientFramework;
using ECACommonUtilities;
using ECACommonUtilities.Model;
using GSF;
using GSF.Collections;
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
        private readonly AlignmentCoordinator m_alignmentCoordinator;
        private readonly MappingCompiler m_mappingCompiler;
        private readonly List<MeasurementKey[]> m_keys;
        private readonly ReadOnlyCollection<MeasurementKey[]> m_readonlyKeys;
        private readonly IDictionary<MeasurementKey, SignalBuffer> m_signalBuffers;
        private IDictionary<MeasurementKey, TimeSpan> m_retentionTimes;
        private Ticks m_currentFrameTime;
        private IDictionary<MeasurementKey, IMeasurement> m_currentFrame;
        private DataSet m_metadataCache;
        private string m_inputMapping;
        private string m_filterExpression;
        private int m_keyIndex;
        private int m_lastKeyIndex;
        private IDictionary<MeasurementKey, IMeasurement> m_cachedFrame;
        private List<IDictionary<MeasurementKey, IMeasurement>> m_cachedFrames;
        private TypeMapping m_cachedMapping;
        private TypeMapping[] m_cachedMappings;
        private IMeasurement[] m_cachedMeasurements;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="MapperBase"/>.
        /// </summary>
        /// <param name="framework">Container object for framework elements.</param>
        /// <param name="inputMapping">Input mapping name.</param>
        protected MapperBase(Framework framework, string inputMapping)
        {
            m_signalLookup = framework.SignalLookup;
            m_alignmentCoordinator = framework.AlignmentCoordinator;
            m_signalBuffers = framework.SignalBuffers;

            UDTCompiler udtCompiler = new UDTCompiler();
            m_mappingCompiler = new MappingCompiler(udtCompiler);
            udtCompiler.Compile(Path.Combine("Model", "UserDefinedTypes.ecaidl"));
            m_mappingCompiler.Compile(Path.Combine("Model", "UserDefinedMappings.ecamap"));

            m_keys = new List<MeasurementKey[]>();
            m_readonlyKeys = m_keys.AsReadOnly();
            m_inputMapping = inputMapping;
        }

        #endregion

        #region [ Properties ]

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
        /// Gets alignment coordinator instance.
        /// </summary>
        public AlignmentCoordinator AlignmentCoordinator => m_alignmentCoordinator;

        /// <summary>
        /// Gets mapping compiler.
        /// </summary>
        public MappingCompiler MappingCompiler => m_mappingCompiler;

        /// <summary>
        /// Gets generated key list.
        /// </summary>
        public ReadOnlyCollection<MeasurementKey[]> Keys => m_readonlyKeys;

        /// <summary>
        /// Gets a lookup table to find buffers for measurements based on measurement key.
        /// </summary>
        public IDictionary<MeasurementKey, SignalBuffer> SignalBuffers => m_signalBuffers;

        /// <summary>
        /// Gets access to cached metadata received from the publisher.
        /// </summary>
        public DataSet MetdataCache => m_metadataCache;

        /// <summary>
        /// Gets or sets the current frame time.
        /// </summary>
        protected Ticks CurrentFrameTime
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

        /// <summary>
        /// Gets or sets the current frame.
        /// </summary>
        protected IDictionary<MeasurementKey, IMeasurement> CurrentFrame
        {
            get
            {
                return m_currentFrame;
            }
            set
            {
                m_currentFrame = value;
            }
        }

        /// <summary>
        /// Gets or sets current measurement key index.
        /// </summary>
        protected int KeyIndex
        {
            get
            {
                return m_keyIndex;
            }
            set
            {
                m_keyIndex = value;
            }
        }

        #endregion

        #region [ Methods ]

        void IMapper.CrunchMetadata(DataSet metadata)
        {
            m_metadataCache = metadata;
            TypeMapping inputMapping = m_mappingCompiler.GetTypeMapping(m_inputMapping);
            m_signalLookup.CrunchMetadata(metadata);
            BuildMeasurementKeys(inputMapping);
            m_retentionTimes = BuildRetentionTimes(inputMapping);
            FixSignalBuffers();
            m_filterExpression = string.Join(";", m_keys.SelectMany(keys => keys).Select(key => key.SignalID).Distinct());
        }

        /// <summary>
        /// Maps the given collection of measurements to the algorithm's
        /// input type and calls the user-defined algorithm.
        /// </summary>
        /// <param name="timestamp">The timestamp of the frame of measurements being processed.</param>
        /// <param name="measurements">The collection of measurement received from the server.</param>
        void IMapper.Map(Ticks timestamp, IDictionary<MeasurementKey, IMeasurement> measurements)
        {
            SignalBuffer signalBuffer;

            m_keyIndex = 0;
            m_currentFrameTime = timestamp;
            m_currentFrame = measurements;

            Map(measurements);

            foreach (KeyValuePair<MeasurementKey, TimeSpan> kvp in m_retentionTimes)
            {
                Ticks retentionTime = ((DateTime)timestamp) - kvp.Value;

                if (m_signalBuffers.TryGetValue(kvp.Key, out signalBuffer))
                    signalBuffer.RetentionTime = retentionTime;
            }
        }

        /// <summary>
        /// Maps the given collection of measurements to the algorithm's
        /// input type and calls the user-defined algorithm.
        /// </summary>
        /// <param name="measurements">The collection of measurement received from the server.</param>
        public abstract void Map(IDictionary<MeasurementKey, IMeasurement> measurements);

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
            return MappingCompiler.GetTypeMapping(fieldMapping.Expression);
        }

        protected IDictionary<MeasurementKey, IMeasurement> GetRelativeFrame(FieldMapping fieldMapping)
        {
            IEnumerable<FieldMapping> signalMappings = MappingCompiler.TraverseSignalMappings(fieldMapping);
            MeasurementKey[] keys = signalMappings.SelectMany(mapping => SignalLookup.GetMeasurementKeys(mapping.Expression)).ToArray();
            AlignmentCoordinator.SampleWindow sampleWindow = CreateSampleWindow(fieldMapping);
            return AlignmentCoordinator.GetFrame(keys, CurrentFrameTime, sampleWindow);
        }

        protected void PushCurrentFrame()
        {
            m_cachedFrame = CurrentFrame;
        }

        protected void PopCurrentFrame()
        {
            CurrentFrame = m_cachedFrame;
        }

        protected void PushRelativeFrame(FieldMapping fieldMapping)
        {
            if (fieldMapping.RelativeTime != 0.0M)
            {
                PushCurrentFrame();
                CurrentFrame = GetRelativeFrame(fieldMapping);
            }
        }

        protected void PopRelativeFrame(FieldMapping fieldMapping)
        {
            if (fieldMapping.RelativeTime != 0.0M)
                PopCurrentFrame();
        }

        protected AlignmentCoordinator.SampleWindow GetSampleWindow(ArrayMapping arrayMapping, out MeasurementKey[] keys)
        {
            IEnumerable<FieldMapping> signalMappings = MappingCompiler.TraverseSignalMappings(arrayMapping);
            keys = signalMappings.SelectMany(mapping => SignalLookup.GetMeasurementKeys(mapping.Expression)).ToArray();
            return CreateSampleWindow(arrayMapping);
        }

        protected int GetUDTArrayTypeMappingCount(ArrayMapping arrayMapping)
        {
            // UDT[] where each array element is the same mapping, but represent different times
            if (arrayMapping.WindowSize != 0.0M)
            {
                MeasurementKey[] keys;
                AlignmentCoordinator.SampleWindow sampleWindow = GetSampleWindow(arrayMapping, out keys);

                m_lastKeyIndex = m_keyIndex;
                m_cachedMapping = GetTypeMapping(arrayMapping);
                m_cachedFrames = AlignmentCoordinator.GetFrames(keys, CurrentFrameTime, sampleWindow);

                return m_cachedFrames.Count;
            }

            // UDT[] where each array element is the same time (relative to now), but represent different mappings
            if (arrayMapping.RelativeTime != 0.0M)
            {
                MeasurementKey[] keys;
                AlignmentCoordinator.SampleWindow sampleWindow = GetSampleWindow(arrayMapping, out keys);

                CurrentFrame = AlignmentCoordinator.GetFrame(keys, CurrentFrameTime, sampleWindow);
                m_cachedMappings = MappingCompiler.EnumerateTypeMappings(arrayMapping.Expression).ToArray();

                return m_cachedMappings.Length;
            }

            // UDT[] where each array element is the same time, but represent different mappings
            m_cachedMappings = MappingCompiler.EnumerateTypeMappings(arrayMapping.Expression).ToArray();

            return m_cachedMappings.Length;
        }

        protected TypeMapping GetUDTArrayTypeMapping(ArrayMapping arrayMapping, int index)
        {
            if (arrayMapping.WindowSize != 0.0M)
            {
                m_keyIndex = m_lastKeyIndex;
                CurrentFrame = m_cachedFrames[index];
                return m_cachedMapping;
            }

            return m_cachedMappings[index];
        }

        protected int GetArrayMeasurementCount(ArrayMapping arrayMapping)
        {
            if (arrayMapping.WindowSize != 0.0M)
            {
                // native[] where each array element is the same mapping, but represent different times
                AlignmentCoordinator.SampleWindow sampleWindow = CreateSampleWindow(arrayMapping);

                m_cachedMeasurements = AlignmentCoordinator.GetMeasurements(Keys[m_keyIndex++].Single(), CurrentFrameTime, sampleWindow).ToArray();
            }
            else if (arrayMapping.RelativeTime != 0.0M)
            {
                // native[] where each array element is the same time (relative to now), but represent different mappings
                AlignmentCoordinator.SampleWindow sampleWindow = CreateSampleWindow(arrayMapping);

                m_cachedMeasurements = Keys[m_keyIndex++].Select(key => AlignmentCoordinator.GetMeasurement(key, CurrentFrameTime, sampleWindow)).ToArray();
            }
            else
            {
                // native[] where each array element is the same time, but represent different mappings
                m_cachedMeasurements = SignalLookup.GetMeasurements(Keys[m_keyIndex++]);
            }

            return m_cachedMeasurements.Length;
        }

        protected IMeasurement GetArrayMeasurement(int index)
        {
            return m_cachedMeasurements[index];
        }

        protected IMeasurement GetMeasurement(FieldMapping fieldMapping)
        {
            if (fieldMapping.RelativeTime != 0.0M)
            {
                AlignmentCoordinator.SampleWindow sampleWindow = CreateSampleWindow(fieldMapping);
                MeasurementKey key = Keys[m_keyIndex++].Single();

                return AlignmentCoordinator.GetMeasurement(key, CurrentFrameTime, sampleWindow);
            }

            return SignalLookup.GetMeasurement(Keys[m_keyIndex++].Single());
        }

        protected MetaValues GetMetaValues(IMeasurement measurement)
        {
            return new MetaValues
            {
                ID = measurement.ID,
                Timestamp = measurement.Timestamp,
                Flags = GetMeasurementFlags(measurement)
            };
        }

        protected MeasurementFlags GetMeasurementFlags(IMeasurement measurement)
        {
            MeasurementStateFlags tslFlags = measurement.StateFlags;    // Time-series Library Measurement State Flags
            MeasurementFlags ecaflags = MeasurementFlags.Normal;        // openECA Measurement Flags

            if (tslFlags.HasFlag(MeasurementStateFlags.BadData) ||
                tslFlags.HasFlag(MeasurementStateFlags.SuspectData) ||
                tslFlags.HasFlag(MeasurementStateFlags.ReceivedAsBad) ||
                tslFlags.HasFlag(MeasurementStateFlags.DiscardedValue) ||
                tslFlags.HasFlag(MeasurementStateFlags.MeasurementError))
                    ecaflags |= MeasurementFlags.BadValue;

            if (tslFlags.HasFlag(MeasurementStateFlags.BadTime) ||
                tslFlags.HasFlag(MeasurementStateFlags.SuspectTime) ||
                tslFlags.HasFlag(MeasurementStateFlags.LateTimeAlarm) ||
                tslFlags.HasFlag(MeasurementStateFlags.FutureTimeAlarm))
                    ecaflags |= MeasurementFlags.BadTime;

            if (tslFlags.HasFlag(MeasurementStateFlags.CalcuatedValue) ||
                tslFlags.HasFlag(MeasurementStateFlags.UpSampled) ||
                tslFlags.HasFlag(MeasurementStateFlags.DownSampled))
                    ecaflags |= MeasurementFlags.CalculatedValue;

            if (tslFlags.HasFlag(MeasurementStateFlags.OverRangeError) ||
                tslFlags.HasFlag(MeasurementStateFlags.UnderRangeError) ||
                tslFlags.HasFlag(MeasurementStateFlags.AlarmHigh) ||
                tslFlags.HasFlag(MeasurementStateFlags.AlarmLow) ||
                tslFlags.HasFlag(MeasurementStateFlags.WarningHigh) ||
                tslFlags.HasFlag(MeasurementStateFlags.WarningLow) ||
                tslFlags.HasFlag(MeasurementStateFlags.FlatlineAlarm) ||
                tslFlags.HasFlag(MeasurementStateFlags.ComparisonAlarm) ||
                tslFlags.HasFlag(MeasurementStateFlags.ROCAlarm) ||
                tslFlags.HasFlag(MeasurementStateFlags.CalculationError) ||
                tslFlags.HasFlag(MeasurementStateFlags.CalculationWarning) ||
                tslFlags.HasFlag(MeasurementStateFlags.SystemError) ||
                tslFlags.HasFlag(MeasurementStateFlags.SystemWarning))
                    ecaflags |= MeasurementFlags.UnreasonableValue;

            return ecaflags;
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
                    m_keys.Add(m_signalLookup.GetMeasurementKeys(fieldMapping.Expression));
                else
                    m_keys.Add(new[] {m_signalLookup.GetMeasurementKey(fieldMapping.Expression)});
            }
        }

        private IDictionary<MeasurementKey, TimeSpan> BuildRetentionTimes(TypeMapping inputMapping)
        {
            IDictionary<MeasurementKey, TimeSpan> retentionTimes = new Dictionary<MeasurementKey, TimeSpan>();

            BuildRetentionTimes(retentionTimes, inputMapping, TimeSpan.Zero);

            return retentionTimes;
        }

        private void BuildRetentionTimes(IDictionary<MeasurementKey, TimeSpan> retentionTimes, TypeMapping typeMapping, TimeSpan parentRetention)
        {
            foreach (FieldMapping fieldMapping in typeMapping.FieldMappings)
            {
                TimeSpan retentionTime = TimeSpan.Zero;

                if (fieldMapping.RelativeTime != 0)
                    retentionTime = GetRetentionTime(fieldMapping);
                else if (fieldMapping.Field.Type.IsArray && ((ArrayMapping)fieldMapping).WindowSize != 0)
                    retentionTime = GetRetentionTime((ArrayMapping)fieldMapping);

                if (retentionTime != TimeSpan.Zero && parentRetention != TimeSpan.Zero)
                    throw new NotSupportedException($"Detected nested buffering while processing mapping with identifier '{typeMapping.Identifier}'.");
                else if (retentionTime == TimeSpan.Zero)
                    retentionTime = parentRetention;

                DataType fieldType = fieldMapping.Field.Type;
                DataType underlyingType = (fieldType as ArrayType)?.UnderlyingType;

                if ((underlyingType ?? fieldType).IsUserDefined)
                {
                    foreach (TypeMapping nestedMapping in m_mappingCompiler.EnumerateTypeMappings(fieldMapping.Expression))
                        BuildRetentionTimes(retentionTimes, nestedMapping, retentionTime);
                }
                else if (retentionTime != TimeSpan.Zero)
                {
                    foreach (MeasurementKey key in m_signalLookup.GetMeasurementKeys(fieldMapping.Expression))
                        retentionTimes.AddOrUpdate(key, k => retentionTime, (k, time) => (time > retentionTime) ? time : retentionTime);
                }
            }
        }

        private void FixSignalBuffers()
        {
            foreach (MeasurementKey key in m_signalBuffers.Keys)
            {
                if (!m_retentionTimes.ContainsKey(key))
                    m_signalBuffers.Remove(key);
            }

            foreach (MeasurementKey key in m_retentionTimes.Keys)
                m_signalBuffers.GetOrAdd(key, k => new SignalBuffer());
        }

        #endregion

        #region [ Static ]

        // Static Methods
        private static TimeSpan GetRetentionTime(FieldMapping fieldMapping)
        {
            decimal amount = fieldMapping.RelativeTime;
            TimeSpan unit = fieldMapping.RelativeUnit;
            decimal sampleAmount = fieldMapping.SampleRate;
            TimeSpan sampleUnit = fieldMapping.SampleUnit;
            return GetRelativeTime(amount, unit, sampleAmount, sampleUnit);
        }

        private static TimeSpan GetRetentionTime(ArrayMapping arrayMapping)
        {
            decimal amount = arrayMapping.WindowSize;
            TimeSpan unit = arrayMapping.WindowUnit;
            decimal sampleAmount = arrayMapping.SampleRate;
            TimeSpan sampleUnit = arrayMapping.SampleUnit;
            return GetRelativeTime(amount, unit, sampleAmount, sampleUnit);
        }

        private static TimeSpan GetRelativeTime(decimal amount, TimeSpan unit, decimal sampleAmount, TimeSpan sampleUnit)
        {
            if (amount == 0.0M)
                return TimeSpan.Zero;

            if (unit != TimeSpan.Zero)
                return TimeSpan.FromTicks((long)(amount * unit.Ticks));
            else if (sampleAmount != 0 && sampleUnit != TimeSpan.Zero)
                return TimeSpan.FromTicks((long)(amount / sampleAmount * sampleUnit.Ticks));
            else
                return TimeSpan.FromTicks((long)(amount / SystemSettings.FramesPerSecond * TimeSpan.TicksPerSecond));
        }

        #endregion
    }
}
