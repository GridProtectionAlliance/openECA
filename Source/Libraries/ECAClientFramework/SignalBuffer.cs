//******************************************************************************************************
//  SignalBuffer.cs - Gbtc
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
//  10/04/2016 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using GSF;
using GSF.Collections;
using GSF.TimeSeries;

namespace ECAClientFramework
{
    /// <summary>
    /// Represents a buffer for a real-time stream of time-series
    /// measurements belonging to an individual signal.
    /// </summary>
    public class SignalBuffer
    {
        #region [ Members ]

        // Nested Types

        /// <summary>
        /// Represents a block of measurements which are
        /// allocated together and deallocated together.
        /// </summary>
        private class MeasurementBlock
        {
            #region [ Members ]

            // Fields
            private IMeasurement[] m_measurements;
            private int m_endIndex;

            #endregion

            #region [ Constructors ]

            /// <summary>
            /// Creates a new instance of the <see cref="MeasurementBlock"/> class.
            /// </summary>
            /// <param name="size"></param>
            public MeasurementBlock(int size)
            {
                m_measurements = new IMeasurement[size];

                for (int i = 0; i < size; i++)
                    m_measurements[i] = new Measurement();
            }

            #endregion

            #region [ Properties ]

            /// <summary>
            /// Gets the measurement at the given index.
            /// </summary>
            /// <param name="index">The index of the measurement to be retrieved.</param>
            /// <returns>The measurement at the given index.</returns>
            public IMeasurement this[int index]
            {
                get
                {
                    if (index < 0 || index >= m_endIndex)
                        ThrowIndexOutOfRangeException();

                    return m_measurements[index];
                }
            }

            /// <summary>
            /// Gets the timestamp of the first index in the block.
            /// </summary>
            public Ticks Timestamp
            {
                get
                {
                    if (m_endIndex == 0)
                        ThrowIndexOutOfRangeException();

                    return m_measurements[0].Timestamp;
                }
            }

            /// <summary>
            /// Gets the number of measurements in the block.
            /// </summary>
            public int Count
            {
                get
                {
                    return m_endIndex;
                }
            }

            #endregion

            #region [ Methods ]

            /// <summary>
            /// If there is space available in the block, adds the given measurement to the block.
            /// </summary>
            /// <param name="measurement">The measurement to be added to the block.</param>
            /// <returns>True if the measurement was added to the block; false otherwise.</returns>
            public bool Add(IMeasurement measurement)
            {
                if (m_endIndex >= m_measurements.Length)
                    return false;

                // Rather than retaining the given measurement in the measurement block,
                // we copy the properties of the given measurement to an existing
                // measurement in the block so that block-allocated measurements
                // can be better managed by the garbage collector
                IMeasurement blockMeasurement = m_measurements[m_endIndex++];

                blockMeasurement.Metadata = measurement.Metadata;
                blockMeasurement.Timestamp = measurement.Timestamp;
                blockMeasurement.Value = measurement.Value;
                blockMeasurement.StateFlags = measurement.StateFlags;
                blockMeasurement.PublishedTimestamp = measurement.PublishedTimestamp;
                blockMeasurement.ReceivedTimestamp = measurement.ReceivedTimestamp;

                return true;
            }

            /// <summary>
            /// Gets a list of all the measurements in the block.
            /// </summary>
            /// <returns>A list of all the measurements in the block.</returns>
            public IList<IMeasurement> GetMeasurements()
            {
                return m_measurements.GetRange(0, m_endIndex);
            }

            /// <summary>
            /// Gets the index of the measurement within the block
            /// whose timestamp is closest to the given timestamp.
            /// </summary>
            /// <param name="timestamp">The timestamp to be searched for.</param>
            /// <returns>The index of the measurement that is closest to the given timestamp.</returns>
            public int GetMeasurementIndex(Ticks timestamp)
            {
                int min = 0;
                int max = m_endIndex - 1;
                int mid = (min + max) / 2;

                while (min < max)
                {
                    if (m_measurements[mid].Timestamp == timestamp)
                        break;

                    if (m_measurements[mid].Timestamp < timestamp)
                        min = mid + 1;
                    else
                        max = mid;

                    mid = (min + max) / 2;
                }

                return mid;
            }

            /// <summary>
            /// Resets the measurement block so that
            /// new measurements can be added to it.
            /// </summary>
            public void Reset()
            {
                m_endIndex = 0;
            }

            private void ThrowIndexOutOfRangeException()
            {
                throw new IndexOutOfRangeException("Attempted to access an index outside the bounds of the measurement block.");
            }

            #endregion
        }

        // Constants
        private const int BlockSize = 128;
        private const int StatWindow = 15;

        // Fields
        private List<MeasurementBlock> m_blocks;
        private object m_blockLock;
        private int m_endBlock;

        private RollingWindow<int> m_removedBlockCounts;
        private Ticks m_retentionTime;
        private Ticks m_lastRecycle;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="SignalBuffer"/> class.
        /// </summary>
        public SignalBuffer()
        {
            const int Sentinel = -1;

            m_blocks = new List<MeasurementBlock>();
            m_blocks.Add(new MeasurementBlock(BlockSize));
            m_removedBlockCounts = new RollingWindow<int>(StatWindow);
            m_blockLock = new object();

            for (int i = 0; i < StatWindow; i++)
                m_removedBlockCounts.Add(Sentinel);
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the retention time which defines the timestamp of
        /// the oldest measurement that needs to be retained by the buffer.
        /// </summary>
        public Ticks RetentionTime
        {
            get
            {
                return m_retentionTime;
            }
            set
            {
                m_retentionTime = value;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Queues the given measurement into the buffer.
        /// </summary>
        /// <param name="measurement">The measurement to be queued.</param>
        public void Queue(IMeasurement measurement)
        {
            // We need to lock the block list here since we
            // don't know when the buffer might recycle blocks
            lock (m_blockLock)
            {
                while (!m_blocks[m_endBlock].Add(measurement))
                    AdvanceBlock();
            }
        }

        /// <summary>
        /// Returns the measurement at the given timestamp or the measurement
        /// with the closest timestamp if no such measurement exists in the buffer.
        /// </summary>
        /// <param name="timestamp">The timestamp of the measurement to be retrieved.</param>
        /// <returns>The measurement with the timestamp closest to the given timestamp.</returns>
        public IMeasurement GetNearestMeasurement(Ticks timestamp)
        {
            Range<IMeasurement> nearestMeasurements = GetNearestMeasurements(timestamp);

            return new IMeasurement[] { nearestMeasurements.Start, nearestMeasurements.End }
                .Where(measurement => (object)measurement != null)
                .MinBy(measurement => Math.Abs(measurement.Timestamp - timestamp));
        }

        /// <summary>
        /// Returns a range encapsulating the nearest measurements around a given timestamp.
        /// If no measurement exists in a given direction on the timeline, a null measurment
        /// is returned instead.
        /// </summary>
        /// <param name="timestamp">The timestamp of the measurements to be retrieved.</param>
        /// <returns>A range enapsulating the nearest measurements around the given timestamp.</returns>
        public Range<IMeasurement> GetNearestMeasurements(Ticks timestamp)
        {
            Recycle();

            int blockIndex = GetBlockIndex(timestamp);
            int measurementIndex = m_blocks[blockIndex].GetMeasurementIndex(timestamp);
            IMeasurement leftMeasurement = m_blocks[blockIndex][measurementIndex];
            IMeasurement rightMeasurement = leftMeasurement;

            if (timestamp < leftMeasurement.Timestamp)
            {
                int absoluteIndex = ToAbsoluteIndex(blockIndex, measurementIndex);
                blockIndex = ToBlockIndex(absoluteIndex - 1);
                measurementIndex = ToMeasurementIndex(absoluteIndex - 1);

                if (blockIndex >= 0 && measurementIndex >= 0)
                    leftMeasurement = m_blocks[blockIndex][measurementIndex];
                else
                    leftMeasurement = null;
            }
            else if (rightMeasurement.Timestamp < timestamp)
            {
                int absoluteIndex = ToAbsoluteIndex(blockIndex, measurementIndex);
                blockIndex = ToBlockIndex(absoluteIndex + 1);
                measurementIndex = ToMeasurementIndex(absoluteIndex + 1);

                if (blockIndex <= m_endBlock && measurementIndex < m_blocks[blockIndex].Count)
                    rightMeasurement = m_blocks[blockIndex][measurementIndex];
                else
                    rightMeasurement = null;
            }

            return new Range<IMeasurement>(leftMeasurement, rightMeasurement);
        }

        /// <summary>
        /// Returns a list of the buffered measurements between the given time range.
        /// </summary>
        /// <param name="startTime">The start of the time range.</param>
        /// <param name="endTime">The end of the time range.</param>
        /// <returns>A list of the buffered measurements between the given time range.</returns>
        public List<IMeasurement> GetMeasurements(Ticks startTime, Ticks endTime)
        {
            Recycle();

            List<IMeasurement> measurements = new List<IMeasurement>();

            int startBlockIndex = GetBlockIndex(startTime);
            int startMeasurementIndex = m_blocks[startBlockIndex].GetMeasurementIndex(startTime);
            int startIndex = ToAbsoluteIndex(startBlockIndex, startMeasurementIndex);

            int endBlockIndex = GetBlockIndex(endTime);
            int endMeasurementIndex = m_blocks[endBlockIndex].GetMeasurementIndex(endTime);
            int endIndex = ToAbsoluteIndex(endBlockIndex, endMeasurementIndex);

            // The measurement returned by the binary search may not have the exact same timestamp as the
            // one used to search because there may not be a measurement with the exact same timestamp
            if (m_blocks[startBlockIndex][startMeasurementIndex].Timestamp < startTime)
                startIndex++;

            if (m_blocks[endBlockIndex][endMeasurementIndex].Timestamp < endTime)
                endIndex--;

            for (int i = startIndex; i <= endIndex; i++)
            {
                int blockIndex = ToBlockIndex(i);
                int measurementIndex = ToMeasurementIndex(i);
                measurements.Add(m_blocks[blockIndex][measurementIndex]);
            }

            return measurements;
        }

        // Converts the given indexes into an absolute
        // index for the measurement across all blocks.
        private int ToAbsoluteIndex(int blockIndex, int measurementIndex)
        {
            return blockIndex * BlockSize + measurementIndex;
        }

        // Converts the given absolute index of the referenced measurement
        // to the index of the block that contains that measurement.
        private int ToBlockIndex(int absoluteIndex)
        {
            return absoluteIndex / BlockSize;
        }

        // Converts the given absolute index of the referenced measurement to
        // the index of that measurement within the block that contains it.
        private int ToMeasurementIndex(int absoluteIndex)
        {
            return absoluteIndex % BlockSize;
        }

        // Gets the index of the block that contains
        // measurements closest to the given timestamp.
        private int GetBlockIndex(Ticks timestamp)
        {
            int endBlock = m_endBlock;

            int min = 0;
            int max = m_blocks[endBlock].Count > 0 ? endBlock : endBlock - 1;
            int mid = (min + max + 1) / 2;

            while (min < max)
            {
                if (m_blocks[mid].Timestamp == timestamp)
                    break;

                if (m_blocks[mid].Timestamp < timestamp)
                    min = mid;
                else
                    max = mid - 1;

                mid = (min + max + 1) / 2;
            }

            return mid;
        }

        // Advances the end index of the block list to the next
        // available block, expanding the list if necessary.
        private void AdvanceBlock()
        {
            if (m_endBlock + 1 >= m_blocks.Count)
                m_blocks.Add(new MeasurementBlock(BlockSize));

            m_endBlock++;
        }

        // Recycles unused blocks by removing them from the beginning
        // of the block list and adding them back to the end of it.
        private void Recycle()
        {
            const int Sentinel = -1;

            // Don't recycle blocks too often so we don't
            // spend too much time spinning our wheels
            Ticks recycleTime = DateTime.UtcNow;

            if ((recycleTime - m_lastRecycle).ToSeconds() < 1.0D)
                return;

            // The retention time tells whether a block is old
            // enough that the cosumer doesn't need it anymore
            Ticks retentionTime = m_retentionTime;
            int unusedBlockCount = -1;

            // Make sure to access m_blocks by index to avoid exceptions during
            // iteration if another thread adds a block to the end of the list
            for (int i = 0; i < m_endBlock && m_blocks[i].Timestamp < retentionTime; i++)
                unusedBlockCount++;

            if (unusedBlockCount <= 0)
                return;

            // The number of blocks retained is computed statistically
            // to minimize both unused space and block allocations
            int retainedBlockCount = unusedBlockCount;

            m_removedBlockCounts.Add(unusedBlockCount);

            if (m_removedBlockCounts[StatWindow - 1] != Sentinel)
                retainedBlockCount = Math.Min((int)Math.Ceiling(m_removedBlockCounts.Average()) + 1, unusedBlockCount);

            List<MeasurementBlock> retainedBlocks = m_blocks.GetRange(0, retainedBlockCount);

            // Now lock the block list since changes to the
            // list may contend with the queuing thread
            lock (m_blockLock)
            {
                m_blocks.RemoveRange(0, unusedBlockCount);
                retainedBlocks.ForEach(block => block.Reset());
                m_blocks.AddRange(retainedBlocks);
                m_endBlock -= unusedBlockCount;
            }

            m_lastRecycle = recycleTime;
        }

        #endregion
    }
}
