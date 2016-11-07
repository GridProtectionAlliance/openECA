//******************************************************************************************************
//  Framework.cs - Gbtc
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
//  11/01/2016 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GSF.TimeSeries;

namespace ECAClientFramework
{
    public class Framework : IDisposable
    {
        #region [ Members ]

        // Fields
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        public Framework(Func<Framework, IMapper> mapperFactory)
        {
            SignalLookup = new SignalLookup();
            SignalBuffers = new ConcurrentDictionary<MeasurementKey, SignalBuffer>();
            AlignmentCoordinator = new AlignmentCoordinator(SignalBuffers);
            Mapper = mapperFactory(this);
            Concentrator = new Concentrator(Mapper);
            Subscriber = new Subscriber(Concentrator);
        }

        #endregion

        #region [ Properties ]

        public SignalLookup SignalLookup { get; }
        public IDictionary<MeasurementKey, SignalBuffer> SignalBuffers { get; }
        public AlignmentCoordinator AlignmentCoordinator { get; }
        public IMapper Mapper { get; }
        public Subscriber Subscriber { get; }
        public Concentrator Concentrator { get; }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases all the resources used by the <see cref="Framework"/> object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="Framework"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                try
                {
                    if (disposing)
                    {
                        Subscriber.Stop();
                        Concentrator.Stop();
                        Concentrator.Dispose();
                    }
                }
                finally
                {
                    m_disposed = true;  // Prevent duplicate dispose.
                }
            }
        }

        #endregion
    }
}
