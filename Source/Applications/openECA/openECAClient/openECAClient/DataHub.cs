//******************************************************************************************************
//  DataHub.cs - Gbtc
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
//  01/14/2016 - Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using GSF;
using GSF.Collections;
using GSF.Data.Model;
using GSF.Identity;
using GSF.IO;
using GSF.Reflection;
using GSF.Security;
using GSF.TimeSeries;
using GSF.TimeSeries.Transport;
using GSF.Web.Model;
using GSF.Web.Security;
using Microsoft.AspNet.SignalR;


namespace openECAClient
{
 
    public class DataHub : Hub
    {
        #region [ Members ]

        // Fields
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        public DataHub()
        {

        }

        #endregion

        #region [ Properties ]

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="DataHub"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                try
                {

                }
                finally
                {
                    m_disposed = true;          // Prevent duplicate dispose.
                    base.Dispose(disposing);    // Call base class Dispose().
                }
            }
        }

        public override Task OnConnected()
        {
            // Store the current connection ID for this thread
            s_connectionID.Value = Context.ConnectionId;
            s_connectCount++;

            //MvcApplication.LogStatusMessage($"DataHub connect by {Context.User?.Identity?.Name ?? "Undefined User"} [{Context.ConnectionId}] - count = {s_connectCount}");
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            if (stopCalled)
            {
                s_connectCount--;
                //MvcApplication.LogStatusMessage($"DataHub disconnect by {Context.User?.Identity?.Name ?? "Undefined User"} [{Context.ConnectionId}] - count = {s_connectCount}");
            }

            return base.OnDisconnected(stopCalled);
        }

        #endregion

        #region [ Static ]

        // Static Properties

        /// <summary>
        /// Gets the hub connection ID for the current thread.
        /// </summary>
        public static string CurrentConnectionID => s_connectionID.Value;

        // Static Fields
        private static volatile int s_connectCount;
        private static readonly ThreadLocal<string> s_connectionID = new ThreadLocal<string>();


        public static readonly DataSubscriber subscriber = new DataSubscriber();
        static readonly object displayLock = new object();
        public static readonly UnsynchronizedSubscriptionInfo unsynchronizedInfo = new UnsynchronizedSubscriptionInfo(false);
        //public static List<string> measurements = new List<string>();
        public static List<Model.Measurement> measurements =  new List<Model.Measurement>();
        static int count = 0;

        /// <summary>
        /// Gets statically cached instance of <see cref="RecordOperationsCache"/> for <see cref="DataHub"/> instances.
        /// </summary>
        /// <returns>Statically cached instance of <see cref="RecordOperationsCache"/> for <see cref="DataHub"/> instances.</returns>
      
        //Setup GEP callbacks
        static void subscriber_StatusMessage(object sender, EventArgs<string> e)
        {
            lock (displayLock)
            {
                Console.WriteLine(e.Argument);
            }

        }

        static void subscriber_MetaDataReceived(object sender, EventArgs<System.Data.DataSet> e)
        {
            DataSet dataSet = e.Argument;
            dataSet.WriteXml(FilePath.GetAbsolutePath("Metadata.xml"), XmlWriteMode.WriteSchema);
            Console.WriteLine("Data set serialized with {0} tables...", dataSet.Tables.Count);
        }

        static void subscriber_NewMeasurements(object sender, EventArgs<ICollection<IMeasurement>> e)
        {
            if(measurements.Count >= 300)
            {
                measurements.RemoveRange(0,30);
            }
            foreach (var measurement in e.Argument)
            {
                Model.Measurement meas = new Model.Measurement();
                meas.Timestamp = measurement.Timestamp;
                meas.Value = measurement.Value;

                measurements.Add(meas);
            }

            // Check to see if total number of added points will exceed process interval used to show periodic
            // messages of how many points have been archived so far...
            //const int interval = 5 * 60;

            //bool showMessage = dataCount + e.Argument.Count >= (dataCount / interval + 1) * interval;

            //dataCount += e.Argument.Count;

            //if (showMessage)
            //{
            //    lock (displayLock)
            //    {
            //        Console.WriteLine(string.Format("{0:N0} measurements have been processed so far...", dataCount));
            //    }

            //// Occasionally request another cipher key rotation
            //if (GSF.Security.Cryptography.Random.Boolean)
            //    subscriber.SendServerCommand(ServerCommand.RotateCipherKeys);
            //}
        }

        static void subscriber_ConnectionEstablished(object sender, EventArgs e)
        {

            subscriber.SendServerCommand(ServerCommand.MetaDataRefresh);

            //// Request cipher key rotation
            //subscriber.SendServerCommand(ServerCommand.RotateCipherKeys);

            //subscriber.SynchronizedSubscribe(true, 30, 0.5D, 1.0D, "DEVARCHIVE:1;DEVARCHIVE:2;DEVARCHIVE:3;DEVARCHIVE:4;DEVARCHIVE:5");
            //subscriber.SynchronizedSubscribe(true, 30, 0.5D, 1.0D, "DEVARCHIVE:1");
            subscriber.UnsynchronizedSubscribe(unsynchronizedInfo);
        }

        static void subscriber_ConnectionTerminated(object sender, EventArgs e)
        {
            subscriber.Start();

            lock (displayLock)
            {
                Console.WriteLine("Connection to publisher was terminated, restarting connection cycle...");
            }
        }

        static void subscriber_ProcessException(object sender, EventArgs<Exception> e)
        {
            lock (displayLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("EXCEPTION: " + e.Argument.Message);
                Console.ResetColor();
            }
        }

     


        // Static Constructor
        static DataHub()
        {
            // Analyze and cache record operations of security hub

            subscriber.StatusMessage += subscriber_StatusMessage;
            subscriber.ProcessException += subscriber_ProcessException;
            subscriber.ConnectionEstablished += subscriber_ConnectionEstablished;
            subscriber.ConnectionTerminated += subscriber_ConnectionTerminated;
            subscriber.NewMeasurements += subscriber_NewMeasurements;
            subscriber.MetaDataReceived += subscriber_MetaDataReceived;



            // Initialize subscriber
            //subscriber.ConnectionString = "server=tcp://127.0.0.1:9898; useZeroMQChannel=true";
            subscriber.ConnectionString = "server=localhost:6190; interface=0.0.0.0";
            subscriber.OperationalModes |= OperationalModes.UseCommonSerializationFormat | OperationalModes.CompressMetadata | OperationalModes.CompressSignalIndexCache | OperationalModes.CompressPayloadData;
            //subscriber.CompressionModes = CompressionModes.TSSC | CompressionModes.GZip;
        }

        #endregion

        // Client-side script functionality

        #region [ Datahub Operations ]

        public IEnumerable<Model.Measurement> GetMeasurements()
        {
            return measurements;
        }
        #endregion


        #region [ Miscellaneous Hub Operations ]

        public string TestDataHub()
        {
            return "testing";
        }

        /// <summary>
        /// Gets UserAccount table ID for current user.
        /// </summary>
        /// <returns>UserAccount.ID for current user.</returns>
        public Guid GetCurrentUserID()
        {
            Guid userID;
            AuthorizationCache.UserIDs.TryGetValue(Thread.CurrentPrincipal.Identity.Name, out userID);
            return userID;
        }

        #endregion
    }
}
