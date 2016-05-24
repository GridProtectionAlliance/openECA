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
using GSF.Data;
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
using openECAClient.Model;
using Measurement = openECAClient.Model.Measurement;


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
        public static readonly DataSubscriber statSubscriber = new DataSubscriber();
        static readonly object displayLock = new object();
        public static readonly UnsynchronizedSubscriptionInfo unsynchronizedInfo = new UnsynchronizedSubscriptionInfo(false);
        public static readonly UnsynchronizedSubscriptionInfo statSubscriptionInfo = new UnsynchronizedSubscriptionInfo(false);
        //public static List<string> measurements = new List<string>();
        public static List<Model.Measurement> measurements =  new List<Model.Measurement>();
        public static List<DeviceDetail> deviceDetails = new List<DeviceDetail>();
        public static List<MeasurementDetail> measurementDetails = new List<MeasurementDetail>();
        public static List<PhasorDetail> phasorDetails = new List<PhasorDetail>();
        public static List<SchemaVersion> schemaVersion = new List<SchemaVersion>();
        public static List<Model.Measurement> stats = new List<Model.Measurement>();
        

        public static DataSet MetaDataSet = new DataSet();
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
            deviceDetails = new List<DeviceDetail>();
            measurementDetails = new List<MeasurementDetail>();
            phasorDetails = new List<PhasorDetail>();
            schemaVersion = new List<SchemaVersion>();


            foreach (DataTable table in dataSet.Tables)
            {
                if(table.TableName == "DeviceDetail")
                {
                    foreach (DataRow row in table.Rows)
                    {
                        DeviceDetail dd = new DeviceDetail();
                        dd.NodeID = row.ConvertField<Guid>("NodeID");
                        dd.UniqueID = row.ConvertField<Guid>("UniqueID");
                        dd.OriginalSource = row.ConvertField<string>("OriginalSource");
                        dd.IsConcentrator = row.ConvertField<bool>("IsConcentrator");
                        dd.Acronym = row.ConvertField<string>("Acronym");
                        dd.Name = row.ConvertField<string>("Name");
                        dd.AccessID = row.ConvertField<int>("AccessID");
                        dd.ParentAcronym = row.ConvertField<string>("ParentAcronym");
                        dd.ProtocolName = row.ConvertField<string>("ProtocolName");
                        dd.FramesPerSecond = row.ConvertField<int>("FramesPerSecond");
                        dd.CompanyAcronym = row.ConvertField<string>("CompanyAcronym");
                        dd.VendorAcronym = row.ConvertField<string>("VendorAcronym");
                        dd.VendorDeviceName = row.ConvertField<string>("VendorDeviceName");
                        dd.Longitude = row.ConvertField<decimal>("Longitude");
                        dd.Latitude = row.ConvertField<decimal>("Latitude");
                        dd.InterconnectionName = row.ConvertField<string>("InterconnectionName");
                        dd.ContactList = row.ConvertField<string>("ContactList");
                        dd.Enabled = row.ConvertField<bool>("Enabled");
                        dd.UpdatedOn = row.ConvertField<DateTime>("UpdatedOn");

                        deviceDetails.Add(dd);
                     }
                }
                else if(table.TableName == "MeasurementDetail")
                {
                    foreach (DataRow row in table.Rows)
                    {
                        MeasurementDetail md = new MeasurementDetail();
                        md.DeviceAcronym = row.ConvertField<string>("DeviceAcronym");
                        md.ID = row.ConvertField<string>("ID");
                        md.SignalID = row.ConvertField<Guid>("SignalID");
                        md.PointTag = row.ConvertField<string>("PointTag");
                        md.SignalReference = row.ConvertField<string>("SignalReference");
                        md.SignalAcronym = row.ConvertField<string>("SignalAcronym");
                        md.PhasorSourceIndex = row.ConvertField<int>("PhasorSourceIndex");
                        md.Description = row.ConvertField<string>("Description");
                        md.Internal = row.ConvertField<bool>("Internal");
                        md.Enabled = row.ConvertField<bool>("Enabled");
                        md.UpdatedOn = row.ConvertField<DateTime>("UpdatedOn");

                        measurementDetails.Add(md);

                    }
                }
                else if(table.TableName == "PhasorDetail")
                {
                    foreach (DataRow row in table.Rows)
                    {
                        PhasorDetail pd = new PhasorDetail();
                        pd.DeviceAcronym = row.ConvertField<string>("DeviceAcronym");
                        pd.Label = row.ConvertField<string>("Label");
                        pd.Type = row.ConvertField<string>("Type");
                        pd.Phase = row.ConvertField<string>("Phase");
                        pd.SourceIndex = row.ConvertField<int>("SourceIndex");
                        pd.UpdatedOn = row.ConvertField<DateTime>("UpdatedOn");

                        phasorDetails.Add(pd);
                    }
                }
                else if(table.TableName == "SchemaVersion")
                {
                    foreach (DataRow row in table.Rows)
                    {
                        SchemaVersion sv = new SchemaVersion();
                        sv.VersionNumber = row.ConvertField<int>("VersionNumber");

                        schemaVersion.Add(sv);
                    }
                }
            }
            MetaDataSet = dataSet;
            dataSet.WriteXml(FilePath.GetAbsolutePath("Metadata.xml"), XmlWriteMode.WriteSchema);
            Console.WriteLine("Data set serialized with {0} tables...", dataSet.Tables.Count);
        }

        static void subscriber_NewMeasurements(object sender, EventArgs<ICollection<IMeasurement>> e)
        {
            foreach (var measurement in e.Argument)
            {

                Model.Measurement meas = new Model.Measurement();
                DateTime date = new DateTime(measurement.Timestamp.Value);
                meas.Timestamp = (date.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                meas.Value = measurement.Value;
                meas.ID = measurement.ID;
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


        static void statSubscriber_StatusMessage(object sender, EventArgs<string> e)
        {
            lock (displayLock)
            {
                Console.WriteLine(e.Argument);
            }

        }

        static void statSubscriber_MetaDataReceived(object sender, EventArgs<System.Data.DataSet> e)
        {
            
        }

        static void statSubscriber_NewMeasurements(object sender, EventArgs<ICollection<IMeasurement>> e)
        {
            foreach (var measurement in e.Argument)
            {

                Model.Measurement meas = new Model.Measurement();
                DateTime date = new DateTime(measurement.Timestamp.Value);
                meas.Timestamp = (date.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                meas.Value = measurement.Value;
                meas.ID = measurement.ID;
                stats.Add(meas);

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

        static void statSubscriber_ConnectionEstablished(object sender, EventArgs e)
        {

            statSubscriber.SendServerCommand(ServerCommand.MetaDataRefresh);
        }

        static void statSubscriber_ConnectionTerminated(object sender, EventArgs e)
        {
            subscriber.Start();

            lock (displayLock)
            {
                Console.WriteLine("Connection to publisher was terminated, restarting connection cycle...");
            }
        }

        static void statSubscriber_ProcessException(object sender, EventArgs<Exception> e)
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

            statSubscriber.StatusMessage += statSubscriber_StatusMessage;
            statSubscriber.ProcessException += statSubscriber_ProcessException;
            statSubscriber.ConnectionEstablished += statSubscriber_ConnectionEstablished;
            statSubscriber.ConnectionTerminated += statSubscriber_ConnectionTerminated;
            statSubscriber.NewMeasurements += statSubscriber_NewMeasurements;
            statSubscriber.MetaDataReceived += statSubscriber_MetaDataReceived;

            unsynchronizedInfo.FilterExpression = "";
            statSubscriptionInfo.FilterExpression = "VMDEV!TVA!SULLIVAN:925;VMDEV!TVA!HIS-WBN_500_LINE:1338;";


            // Initialize subscriber
            //subscriber.ConnectionString = "server=tcp://127.0.0.1:9898; useZeroMQChannel=true";
            subscriber.ConnectionString = "server=localhost:6190; interface=0.0.0.0";
            subscriber.AutoSynchronizeMetadata = false;
            subscriber.OperationalModes |= OperationalModes.UseCommonSerializationFormat | OperationalModes.CompressMetadata | OperationalModes.CompressSignalIndexCache | OperationalModes.CompressPayloadData;
            //subscriber.CompressionModes = CompressionModes.TSSC | CompressionModes.GZip;

            statSubscriber.ConnectionString = "server=localhost:6190; interface=0.0.0.0";
            statSubscriber.AutoSynchronizeMetadata = false;
            statSubscriber.OperationalModes |= OperationalModes.UseCommonSerializationFormat | OperationalModes.CompressMetadata | OperationalModes.CompressSignalIndexCache | OperationalModes.CompressPayloadData;

            subscriber.Initialize();
            subscriber.Start();

            statSubscriber.Initialize();
            statSubscriber.Start();
            statSubscriber.UnsynchronizedSubscribe(statSubscriptionInfo);

        }

        #endregion

        // Client-side script functionality

        #region [ Datahub Operations ]

        public IEnumerable<Model.Measurement> GetMeasurements()
        {
            List<Model.Measurement> returnData = new List<Model.Measurement>(measurements);
            measurements = new List<Model.Measurement>();
            return returnData;
        }

        public IEnumerable<DeviceDetail> GetDeviceDetails()
        {
            return deviceDetails;
        }

        public IEnumerable<MeasurementDetail> GetMeasurementDetails()
        {
            return measurementDetails;
        }

        public IEnumerable<PhasorDetail> GetPhasorDetails()
        {
            return phasorDetails;
        }

        public IEnumerable<SchemaVersion> GetSchemaVersion()
        {
            return schemaVersion;
        }

        public IEnumerable<Model.Measurement> GetStats()
        {
            List<Model.Measurement> returnData = new List<Model.Measurement>(stats);
            stats = new List<Model.Measurement>();

            return returnData;
        } 

        public void updateFilters(string filterString)
        {
            measurements = new List<Measurement>();
            unsynchronizedInfo.FilterExpression = filterString;
            subscriber.UnsynchronizedSubscribe(unsynchronizedInfo);

        }

        public void statSubscribe(string filterString)
        {
            statSubscriptionInfo.FilterExpression = filterString;
            statSubscriber.UnsynchronizedSubscribe(statSubscriptionInfo);

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
