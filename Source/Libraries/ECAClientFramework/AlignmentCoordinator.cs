﻿//******************************************************************************************************
//  AlignmentCoordinator.cs - Gbtc
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
//  10/11/2016 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using GSF;
using GSF.TimeSeries;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable RedundantCaseLabel
namespace ECAClientFramework
{
    /// <summary>
    /// Defines strategies which can be used to resample
    /// data when aligning data to a fixed sampling window.
    /// </summary>
    public enum ResamplingStrategy
    {
        /// <summary>
        /// Fills in missing values using the measurement that is
        /// nearest to the timestamp of the missing measurement.
        /// </summary>
        NearestMeasurement,

        /// <summary>
        /// Fills in missing values with null measurements so that
        /// data gaps can be detected and processed by the user.
        /// </summary>
        FillMissingData,

        /// <summary>
        /// No resampling, meaning data will be
        /// returned as-is from the signal buffer.
        /// </summary>
        None
    }

    /// <summary>
    /// Queries data from signal buffers and aligns data
    /// to moving time windows at differing sample rates.
    /// </summary>
    public class AlignmentCoordinator
    {
        #region [ Members ]

        // Nested Types

        /// <summary>
        /// Represents a window of time, relative to a moving
        /// timestamp, over which data should be sampled.
        /// </summary>
        public class SampleWindow
        {
            #region [ Members ]

            // Fields

            // The timeline below illustrates the relationship
            // between the frame time and the sample window as
            // defined by the frame offset and start offset.
            //
            //                  [Timeline]
            // |====|====|====|====|====|====|====|====|====|
            //                                 | (frame time)
            //      |------ frame offset ------|
            //      |--- start offset ---| (sample window)
            private readonly TimeSpan m_frameOffset;
            private readonly TimeSpan m_startOffset;
            private readonly int m_windowSize;

            #endregion

            #region [ Constructors ]

            /// <summary>
            /// Creates a new instance of the <see cref="SampleWindow"/> class.
            /// </summary>
            /// <param name="relativeTime">The amount of time, relative to the frame time, to reach the start of the window.</param>
            /// <param name="relativeUnit">The units of <paramref name="relativeTime"/>.</param>
            /// <param name="sampleRate">The number of samples per <paramref name="sampleUnit"/>.</param>
            /// <param name="sampleUnit">The units of the <paramref name="sampleRate"/>.</param>
            /// <exception cref="ArgumentException">
            /// <para><paramref name="relativeTime"/> is negative</para>
            /// <para>- OR -</para>
            /// <para><paramref name="relativeUnit"/> is negative</para>
            /// <para>- OR -</para>
            /// <para><paramref name="sampleRate"/> is not positive</para>
            /// <para>- OR -</para>
            /// <para><paramref name="sampleUnit"/> is not positive</para>
            /// </exception>
            internal SampleWindow(decimal relativeTime, TimeSpan relativeUnit, decimal sampleRate, TimeSpan sampleUnit)
            {
                if (relativeTime < 0.0M)
                    throw new ArgumentException($"Relative time ({relativeTime}) cannot be negative", nameof(relativeTime));

                if (relativeTime == 0.0M)
                    m_frameOffset = TimeSpan.Zero;
                else if (relativeUnit < TimeSpan.Zero)
                    throw new ArgumentException($"Relative unit ({relativeUnit}) cannot be negative", nameof(relativeUnit));
                else if (relativeUnit > TimeSpan.Zero)
                    m_frameOffset = TimeSpan.FromTicks((long)Math.Round(relativeTime * relativeUnit.Ticks));
                else if (sampleRate <= 0.0M)
                    throw new ArgumentException($"Sample rate ({sampleRate}) must be greater than zero", nameof(sampleRate));
                else if (sampleUnit <= TimeSpan.Zero)
                    throw new ArgumentException($"Sample unit ({sampleUnit}) must be greater than zero", nameof(sampleUnit));
                else
                    m_frameOffset = TimeSpan.FromTicks((long)Math.Round(relativeTime * sampleUnit.Ticks / sampleRate));

                m_startOffset = TimeSpan.Zero;
                m_windowSize = 1;
            }

            /// <summary>
            /// Creates a new instance of the <see cref="SampleWindow"/> class.
            /// </summary>
            /// <param name="relativeTime">The amount of time, relative to the frame time, to reach the start of the window.</param>
            /// <param name="relativeUnit">The units of <paramref name="relativeTime"/>.</param>
            /// <param name="sampleRate">The number of samples per <paramref name="sampleUnit"/>.</param>
            /// <param name="sampleUnit">The units of the <paramref name="sampleRate"/>.</param>
            /// <param name="windowSize">The size of the sample window.</param>
            /// <param name="windowUnit">The units of the sample window.</param>
            /// <exception cref="ArgumentException">
            /// <para><paramref name="relativeTime"/> is negative</para>
            /// <para>- OR -</para>
            /// <para><paramref name="relativeUnit"/> is negative</para>
            /// <para>- OR -</para>
            /// <para><paramref name="sampleRate"/> is not positive</para>
            /// <para>- OR -</para>
            /// <para><paramref name="sampleUnit"/> is not positive</para>
            /// <para>- OR -</para>
            /// <para><paramref name="windowSize"/> is not positive</para>
            /// <para>- OR -</para>
            /// <para><paramref name="windowUnit"/> is negative</para>
            /// </exception>
            internal SampleWindow(decimal relativeTime, TimeSpan relativeUnit, decimal sampleRate, TimeSpan sampleUnit, decimal windowSize, TimeSpan windowUnit)
                : this(relativeTime, relativeUnit, sampleRate, sampleUnit)
            {
                if (sampleRate <= 0.0M)
                    throw new ArgumentException($"Sample rate ({sampleRate}) must be greater than zero", nameof(sampleRate));

                if (sampleUnit <= TimeSpan.Zero)
                    throw new ArgumentException($"Sample unit ({sampleUnit}) must be greater than zero", nameof(sampleUnit));

                if (windowSize <= 0.0M)
                    throw new ArgumentException($"Window size ({windowSize}) must be greater than zero", nameof(windowSize));

                if (windowUnit < TimeSpan.Zero)
                    throw new ArgumentException($"Window unit ({windowUnit}) cannot be negative", nameof(windowUnit));

                if (windowUnit > TimeSpan.Zero)
                    m_startOffset = TimeSpan.FromTicks((long)Math.Round(windowSize * windowUnit.Ticks));
                else
                    m_startOffset = TimeSpan.FromTicks((long)Math.Round(windowSize * sampleUnit.Ticks / sampleRate));

                m_windowSize = (int)Math.Round(m_startOffset.Ticks * sampleRate / sampleUnit.Ticks);
            }

            #endregion

            #region [ Methods ]

            /// <summary>
            /// Gets the aligned distribution of timestamps within the sample window.
            /// </summary>
            /// <param name="frameTime">The time of the frame that defines the current sample window.</param>
            /// <returns>The aligned distribution of timestamps within the sample window.</returns>
            public List<Ticks> GetTimestamps(Ticks frameTime)
            {
                List<Ticks> timestamps = new List<Ticks>(m_windowSize);
                Ticks startTime = ((DateTime)frameTime) - m_frameOffset;

                for (int i = 0; i < m_windowSize; i++)
                {
                    TimeSpan startOffset = TimeSpan.FromTicks(m_startOffset.Ticks * i / m_windowSize);
                    timestamps.Add(((DateTime)startTime) + startOffset);
                }

                return timestamps;
            }

            /// <summary>
            /// Creates new measurements with timestamps aligned to the sample window.
            /// </summary>
            /// <param name="key">Measurement key for meta-data values.</param>
            /// <param name="frameTime">The time of the frame that defines the current sample window.</param>
            /// <returns>New measurements with aligned timestamps.</returns>
            public List<MetaValues> CreateMetaValues(MeasurementKey key, Ticks frameTime)
            {
                return GetTimestamps(frameTime)
                    .Select(timestamp => new MetaValues()
                    {
                        ID = key.SignalID,
                        Timestamp = timestamp,
                        Flags = MeasurementFlags.CalculatedValue
                    })
                    .ToList();
            }

            /// <summary>
            /// Aligns data from the given signal buffer to the nearest samples in the sample window.
            /// </summary>
            /// <param name="signalBuffer">The signal buffer that contains the data to be aligned.</param>
            /// <param name="frameTime">The time of the frame that defines the current sample window.</param>
            /// <returns>Data from the signal buffer aligned to the nearest samples in the sample window.</returns>
            public List<IMeasurement> AlignNearest(SignalBuffer signalBuffer, Ticks frameTime)
            {
                return GetTimestamps(frameTime)
                    .Select(Timestamp => new { Timestamp, Measurement = signalBuffer.GetNearestMeasurement(Timestamp) })
                    .Where(obj => (object)obj.Measurement != null)
                    .Select(obj => ChangeTimestamp(obj.Measurement, obj.Timestamp))
                    .ToList();
            }

            /// <summary>
            /// Aligns data from the given signal buffer to the appropriate samples
            /// in the sample window and fills in with null where data is missing.
            /// </summary>
            /// <param name="signalBuffer">The signal buffer that contains the data to be aligned.</param>
            /// <param name="frameTime">The time of the frame that defines the current sample window.</param>
            /// <returns>Data from the signal buffer aligned to the appropriate samples in the sample window.</returns>
            public List<IMeasurement> AlignFill(SignalBuffer signalBuffer, Ticks frameTime)
            {
                Ticks maxDiff = (m_startOffset.Ticks / m_windowSize) / 2;

                return GetTimestamps(frameTime)
                    .Select(Timestamp => new { Timestamp, Measurement = signalBuffer.GetNearestMeasurement(Timestamp) })
                    .Where(obj => (object)obj.Measurement != null)
                    .Select(obj =>
                    {
                        Ticks timestamp = obj.Timestamp;
                        IMeasurement nearestMeasurement = obj.Measurement;
                        Ticks diff = Math.Abs(nearestMeasurement.Timestamp - timestamp);

                        return (diff <= maxDiff) ? nearestMeasurement : new Measurement()
                        {
                            Metadata = signalBuffer.Key.Metadata,
                            Timestamp = timestamp,
                            Value = double.NaN,
                            StateFlags = MeasurementStateFlags.UpSampled
                        };
                    })
                    .ToList();
            }

            /// <summary>
            /// Returns data within the time range defined by the sample window without attempting to align.
            /// </summary>
            /// <param name="signalBuffer">The signal buffer that contains the data to be aligned.</param>
            /// <param name="frameTime">The time of the frame that defines the current sample window.</param>
            /// <returns>Data from the signal buffer within the time range defined by the sample window.</returns>
            public List<IMeasurement> AlignNone(SignalBuffer signalBuffer, Ticks frameTime)
            {
                Ticks startTime = ((DateTime)frameTime) - m_frameOffset;
                Ticks endTime = ((DateTime)startTime) + m_startOffset;
                return signalBuffer.GetMeasurements(startTime, endTime);
            }

            private IMeasurement ChangeTimestamp(IMeasurement measurement, Ticks newTimestamp)
            {
                if (measurement.Timestamp == newTimestamp)
                    return measurement;

                return new Measurement()
                {
                    Metadata = measurement.Metadata,
                    Timestamp = newTimestamp,
                    Value = measurement.Value,
                    StateFlags = measurement.StateFlags | MeasurementStateFlags.UpSampled
                };
            }

            #endregion
        }

        // Constants
        public const decimal DefaultSampleRate = 30.0M;
        public static readonly TimeSpan DefaultSampleUnit = TimeSpan.FromSeconds(1.0D);

        // Fields
        private readonly IDictionary<MeasurementKey, SignalBuffer> m_signalBuffers;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="AlignmentCoordinator"/> class.
        /// </summary>
        /// <param name="signalBuffers">The a lookup table for buffers by signal.</param>
        public AlignmentCoordinator(IDictionary<MeasurementKey, SignalBuffer> signalBuffers)
        {
            m_signalBuffers = signalBuffers;
            SampleRate = DefaultSampleRate;
            SampleUnit = DefaultSampleUnit;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the resampling strategy used to handle cases when the
        /// sampling rate of the data does not match the sampling rate of the window.
        /// </summary>
        public ResamplingStrategy ResamplingStrategy { get; set; }

        /// <summary>
        /// Gets or sets the global sample rate used when the sample
        /// rate is not explicitly defined for a sample window.
        /// </summary>
        public decimal SampleRate { get; set; }

        /// <summary>
        /// Gets or sets the units of the global sample rate.
        /// </summary>
        public TimeSpan SampleUnit { get; set; }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Creates new meta values aligned to the given sample window and returns first meta value structure.
        /// This method should typically be used in conjunction with sample windows without a window size,
        /// created using the <see cref="CreateSampleWindow(decimal, TimeSpan, decimal, TimeSpan)"/> method.
        /// </summary>
        /// <param name="key">The key that identifies the signal to generate meta values for.</param>
        /// <param name="frameTime">The time of the frame relative to which the sample window is defined.</param>
        /// <param name="window">The sample window that defines the range of time for which to generate meta values.</param>
        /// <returns>The first meta value structure for the given signal in the given sample window relative to the given frame time.</returns>
        public MetaValues CreateMetaValue(MeasurementKey key, Ticks frameTime, SampleWindow window)
        {
            return CreateMetaValues(key, frameTime, window).FirstOrDefault();
        }

        /// <summary>
        /// Creates new meta values aligned to the given sample window.
        /// </summary>
        /// <param name="key">The key that identifies the signal to generate meta values for.</param>
        /// <param name="frameTime">The time of the frame relative to which the sample window is defined.</param>
        /// <param name="window">The sample window that defines the range of time for which to generate meta values.</param>
        /// <returns>The full collection of meta values for the given signal in the given sample window relative to the given frame time.</returns>
        public List<MetaValues> CreateMetaValues(MeasurementKey key, Ticks frameTime, SampleWindow window)
        {
            return window.CreateMetaValues(key, frameTime);
        }

        /// <summary>
        /// Queries a signal buffer for data in the given sample window and returns first measurement.
        /// This method should typically be used in conjunction with sample windows without a window size,
        /// created using the <see cref="CreateSampleWindow(decimal, TimeSpan, decimal, TimeSpan)"/> method.
        /// </summary>
        /// <param name="key">The key that identifies the signal to be queried.</param>
        /// <param name="frameTime">The time of the frame relative to which the sample window is defined.</param>
        /// <param name="window">The sample window that defines the range of time to be queried from the buffer.</param>
        /// <returns>The first measurement for the given signal in the given sample window relative to the given frame time.</returns>
        public IMeasurement GetMeasurement(MeasurementKey key, Ticks frameTime, SampleWindow window)
        {
            return GetMeasurements(key, frameTime, window).FirstOrDefault();
        }

        /// <summary>
        /// Queries a signal buffer for the full collection of measurements over the given sample window.
        /// </summary>
        /// <param name="key">The key that identifies the signal to be queried.</param>
        /// <param name="frameTime">The time of the frame relative to which the sample window is defined.</param>
        /// <param name="window">The sample window that defines the range of time to be queried from the buffer.</param>
        /// <returns>The full collection of measurements for the given signal in the given sample window relative to the given frame time.</returns>
        public List<IMeasurement> GetMeasurements(MeasurementKey key, Ticks frameTime, SampleWindow window)
        {
            return GetMeasurements(key, frameTime, window, ResamplingStrategy);
        }

        /// <summary>
        /// Queries a signal buffer for the full collection of measurements over the given sample window.
        /// </summary>
        /// <param name="key">The key that identifies the signal to be queried.</param>
        /// <param name="frameTime">The time of the frame relative to which the sample window is defined.</param>
        /// <param name="window">The sample window that defines the range of time to be queried from the buffer.</param>
        /// <param name="resamplingStrategy">The strategy to use for alignment of data to the sample rate of the window.</param>
        /// <returns>The full collection of measurements for the given signal in the given sample window relative to the given frame time.</returns>
        public List<IMeasurement> GetMeasurements(MeasurementKey key, Ticks frameTime, SampleWindow window, ResamplingStrategy resamplingStrategy)
        {
            if (!m_signalBuffers.TryGetValue(key, out SignalBuffer signalBuffer))
                return null;

            switch (resamplingStrategy)
            {
                default:
                case ResamplingStrategy.NearestMeasurement:
                    return window.AlignNearest(signalBuffer, frameTime);

                case ResamplingStrategy.FillMissingData:
                    return window.AlignFill(signalBuffer, frameTime);

                case ResamplingStrategy.None:
                    return window.AlignNone(signalBuffer, frameTime);
            }
        }

        /// <summary>
        /// Queries multiple signal buffers for data in the given sample window and returns first frame.
        /// This method should typically be used in conjunction with sample windows without a window size,
        /// created using the <see cref="CreateSampleWindow(decimal, TimeSpan, decimal, TimeSpan)"/> method.
        /// </summary>
        /// <param name="keys">The keys that identify the signals to be queried.</param>
        /// <param name="frameTime">The time of the frame relative to which the sample window is defined.</param>
        /// <param name="window">The sample window that defines the range of time to be queried from the buffer.</param>
        /// <returns>The first frame for the given signal in the given sample window relative to the given frame time.</returns>
        public IDictionary<MeasurementKey, IMeasurement> GetFrame(MeasurementKey[] keys, Ticks frameTime, SampleWindow window)
        {
            return GetFrames(keys, frameTime, window).FirstOrDefault();
        }

        /// <summary>
        /// Queries multiple signal buffers to build a list of frames of data over the given sample window.
        /// </summary>
        /// <param name="keys">The keys that identify the signals to be queried.</param>
        /// <param name="frameTime">The time of the frame relative to which the sample window is defined.</param>
        /// <param name="window">The sample window that defines the range of time to be queried from the buffer.</param>
        /// <returns>A collection of frames over the given sample window.</returns>
        public List<IDictionary<MeasurementKey, IMeasurement>> GetFrames(MeasurementKey[] keys, Ticks frameTime, SampleWindow window)
        {
            return GetFrames(keys, frameTime, window, ResamplingStrategy);
        }

        /// <summary>
        /// Queries multiple signal buffers to build a list of frames of data over the given sample window.
        /// </summary>
        /// <param name="keys">The keys that identify the signals to be queried.</param>
        /// <param name="frameTime">The time of the frame relative to which the sample window is defined.</param>
        /// <param name="window">The sample window that defines the range of time to be queried from the buffer.</param>
        /// <param name="resamplingStrategy">The strategy to use for alignment of data to the sample rate of the window.</param>
        /// <returns>A collection of frames over the given sample window.</returns>
        public List<IDictionary<MeasurementKey, IMeasurement>> GetFrames(MeasurementKey[] keys, Ticks frameTime, SampleWindow window, ResamplingStrategy resamplingStrategy)
        {
            List<IDictionary<MeasurementKey, IMeasurement>> frames = new List<IDictionary<MeasurementKey, IMeasurement>>();

            foreach (MeasurementKey key in keys)
            {
                if (!m_signalBuffers.TryGetValue(key, out SignalBuffer _))
                    continue;

                List<IMeasurement> measurements = GetMeasurements(key, frameTime, window, resamplingStrategy);

                while (frames.Count < measurements.Count)
                    frames.Add(keys.ToDictionary(k => k, k => (IMeasurement)null));

                for (int i = 0; i < measurements.Count; i++)
                    frames[i][key] = measurements[i];
            }

            return frames;
        }

        /// <summary>
        /// Creates a sample window that defines a single point in time, relative to a moving time slice.
        /// </summary>
        /// <param name="relativeTime">The amount of time between the moving time slice and the sample window.</param>
        /// <param name="relativeUnit">The units of measure used to define the relative time.</param>
        /// <param name="sampleRate">The numerator for the sample rate.</param>
        /// <param name="sampleUnit">The denominator for the sample rate.</param>
        /// <returns>A sample window that defines a single point in time, relative to a moving time slice.</returns>
        public SampleWindow CreateSampleWindow(decimal relativeTime, TimeSpan relativeUnit, decimal sampleRate, TimeSpan sampleUnit)
        {
            if (sampleRate == 0.0M && sampleUnit == TimeSpan.Zero)
            {
                sampleRate = SampleRate;
                sampleUnit = SampleUnit;
            }

            return new SampleWindow(relativeTime, relativeUnit, sampleRate, sampleUnit);
        }

        /// <summary>
        /// Creates a sample window that defines a range of time, relative to a moving time slice.
        /// </summary>
        /// <param name="relativeTime">The amount of time between the moving time slice and the beginning of the sample window.</param>
        /// <param name="relativeUnit">The units of measure used to define the relative time.</param>
        /// <param name="sampleRate">The numerator for the sample rate.</param>
        /// <param name="sampleUnit">The denominator for the sample rate.</param>
        /// <param name="windowSize">The amount of time to define the size of the sample window.</param>
        /// <param name="windowUnit">The units of measure used to define the size of the sample window.</param>
        /// <returns>A sample window that defines a range of time, relative to a moving time slice.</returns>
        public SampleWindow CreateSampleWindow(decimal relativeTime, TimeSpan relativeUnit, decimal sampleRate, TimeSpan sampleUnit, decimal windowSize, TimeSpan windowUnit)
        {
            if (sampleRate == 0.0M && sampleUnit == TimeSpan.Zero)
            {
                sampleRate = SampleRate;
                sampleUnit = SampleUnit;
            }

            return new SampleWindow(relativeTime, relativeUnit, sampleRate, sampleUnit, windowSize, windowUnit);
        }

        #endregion
    }
}
