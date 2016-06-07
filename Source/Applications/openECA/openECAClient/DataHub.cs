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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GSF;
using GSF.Data;
using GSF.Collections;
using GSF.IO;
using GSF.TimeSeries;
using GSF.TimeSeries.Transport;
using GSF.Web.Security;
using Microsoft.AspNet.SignalR;
using openECAClient.Model;
using openECAClient.Template.CSharp;

using DataType = openECAClient.Model.DataType;
using Measurement = openECAClient.Model.Measurement;

namespace openECAClient
{
    public class DataHub : Hub
    {
        #region [ Members ]

        // Fields
        private readonly object m_udtLock = new object();
        private readonly object m_mapLock = new object();

        #endregion

        #region [ Methods ]

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
        private static readonly string s_udtFile;
        private static readonly string s_udmFile;
        private static readonly ThreadLocal<string> s_connectionID;
        private static readonly DataSubscriber s_dataSubscription;
        private static readonly DataSubscriber s_statisticSubscription;
        private static readonly DataSubscriber s_statusLightsSubscription;
        private static readonly UnsynchronizedSubscriptionInfo s_dataSubscriptionInfo;
        private static readonly UnsynchronizedSubscriptionInfo s_statisticSubscriptionInfo;
        private static readonly UnsynchronizedSubscriptionInfo s_statusLightsSubscriptionInfo;
        private static List<Measurement> s_measurements;
        private static List<DeviceDetail> s_deviceDetails;
        private static List<MeasurementDetail> s_measurementDetails;
        private static List<PhasorDetail> s_phasorDetails;
        private static List<SchemaVersion> s_schemaVersion;
        private static List<StatusLight> s_statusLights;
        private static readonly List<Measurement> s_statistics;
        private static readonly object s_displayLock;
        private static readonly object s_measurementLock;
        private static readonly object s_statsLock;
        private static readonly object s_lightsLock;

        // Static Constructor
        static DataHub()
        {
            s_udtFile = FilePath.GetAbsolutePath("wwwroot\\Data\\UserDefinedTypes.txt");
            s_udmFile = FilePath.GetAbsolutePath("wwwroot\\Data\\UserDefinedMappings.txt");
            s_lightsLock = new object();
            s_statsLock = new object();
            s_measurementLock = new object();
            s_displayLock = new object();
            s_statistics = new List<Measurement>();
            s_statusLights = new List<StatusLight>();
            s_schemaVersion = new List<SchemaVersion>();
            s_phasorDetails = new List<PhasorDetail>();
            s_measurementDetails = new List<MeasurementDetail>();
            s_deviceDetails = new List<DeviceDetail>();
            s_measurements = new List<Measurement>();
            s_statusLightsSubscriptionInfo = new UnsynchronizedSubscriptionInfo(false);
            s_statisticSubscriptionInfo = new UnsynchronizedSubscriptionInfo(false);
            s_dataSubscriptionInfo = new UnsynchronizedSubscriptionInfo(false);
            s_statusLightsSubscription = new DataSubscriber();
            s_statisticSubscription = new DataSubscriber();
            s_dataSubscription = new DataSubscriber();
            s_connectionID = new ThreadLocal<string>();

            s_dataSubscription.StatusMessage += DataSubscriptionStatusMessage;
            s_dataSubscription.ProcessException += DataSubscriptionProcessException;
            s_dataSubscription.ConnectionTerminated += DataSubscriptionConnectionTerminated;
            s_dataSubscription.NewMeasurements += DataSubscriptionNewMeasurements;

            s_statisticSubscription.StatusMessage += StatisticSubscriptionStatusMessage;
            s_statisticSubscription.ProcessException += StatisticSubscriptionProcessException;
            s_statisticSubscription.ConnectionEstablished += StatisticSubscriptionConnectionEstablished;
            s_statisticSubscription.ConnectionTerminated += StatisticSubscriptionConnectionTerminated;
            s_statisticSubscription.NewMeasurements += StatisticSubscriptionNewMeasurements;
            s_statisticSubscription.MetaDataReceived += StatisticSubscriptionMetaDataReceived;

            s_statusLightsSubscription.StatusMessage += StatusLightsSubscriptionStatusMessage;
            s_statusLightsSubscription.ProcessException += StatusLightsSubscriptionProcessException;
            s_statusLightsSubscription.ConnectionTerminated += StatusLightsSubscriptionConnectionTerminated;
            s_statusLightsSubscription.NewMeasurements += StatusLightsSubscriptionNewMeasurements;

            s_dataSubscriptionInfo.FilterExpression = "";
            s_statisticSubscriptionInfo.FilterExpression = "";
            s_statusLightsSubscriptionInfo.FilterExpression = "";

            // TODO: Define the connection strings in GlobalSettings and read from config file

            // Initialize subscribers
            s_dataSubscription.ConnectionString = "server=localhost:6190; interface=0.0.0.0";
            s_dataSubscription.AutoSynchronizeMetadata = false;
            s_dataSubscription.OperationalModes |= OperationalModes.UseCommonSerializationFormat | OperationalModes.CompressMetadata | OperationalModes.CompressSignalIndexCache | OperationalModes.CompressPayloadData;
            s_dataSubscription.CompressionModes = CompressionModes.TSSC | CompressionModes.GZip;

            s_statisticSubscription.ConnectionString = "server=localhost:6190; interface=0.0.0.0";
            s_statisticSubscription.AutoSynchronizeMetadata = false;
            s_statisticSubscription.OperationalModes |= OperationalModes.UseCommonSerializationFormat | OperationalModes.CompressMetadata | OperationalModes.CompressSignalIndexCache | OperationalModes.CompressPayloadData;
            s_statisticSubscription.CompressionModes = CompressionModes.TSSC | CompressionModes.GZip;

            s_statusLightsSubscription.ConnectionString = "server=localhost:6190; interface=0.0.0.0";
            s_statusLightsSubscription.AutoSynchronizeMetadata = false;
            s_statusLightsSubscription.OperationalModes |= OperationalModes.UseCommonSerializationFormat | OperationalModes.CompressMetadata | OperationalModes.CompressSignalIndexCache | OperationalModes.CompressPayloadData;
            s_statusLightsSubscription.CompressionModes = CompressionModes.TSSC | CompressionModes.GZip;

            try
            {
                s_dataSubscription.Initialize();
                s_dataSubscription.Start();
            }
            catch (Exception ex)
            {

            }

            s_statisticSubscription.Initialize();
            s_statisticSubscription.Start();

            s_statusLightsSubscription.Initialize();
            s_statusLightsSubscription.Start();
        }

        // Static Methods
        static void DataSubscriptionStatusMessage(object sender, EventArgs<string> e)
        {
            lock (s_displayLock)
            {
                Console.WriteLine(e.Argument);
            }

        }

        static void DataSubscriptionNewMeasurements(object sender, EventArgs<ICollection<IMeasurement>> e)
        {
            lock (s_measurementLock)
            {
                foreach (IMeasurement measurement in e.Argument)
                {
                    Measurement value = new Measurement();
                    DateTime date = new DateTime(measurement.Timestamp.Value);
                    value.Timestamp = (date.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                    value.Value = measurement.Value;
                    value.ID = measurement.ID;
                    s_measurements.Add(value);
                }
            }
        }

        static void DataSubscriptionConnectionTerminated(object sender, EventArgs e)
        {
            s_dataSubscription.Start();

            lock (s_displayLock)
            {
                Console.WriteLine("Connection to publisher was terminated, restarting connection cycle...");
            }
        }

        static void DataSubscriptionProcessException(object sender, EventArgs<Exception> e)
        {
            lock (s_displayLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("EXCEPTION: " + e.Argument.Message);
                Console.ResetColor();
            }
        }


        static void StatisticSubscriptionStatusMessage(object sender, EventArgs<string> e)
        {
            lock (s_displayLock)
            {
                Console.WriteLine(e.Argument);
            }

        }

        static void StatisticSubscriptionMetaDataReceived(object sender, EventArgs<System.Data.DataSet> e)
        {
            DataSet dataSet = e.Argument;

            s_deviceDetails = new List<DeviceDetail>();
            s_measurementDetails = new List<MeasurementDetail>();
            s_phasorDetails = new List<PhasorDetail>();
            s_schemaVersion = new List<SchemaVersion>();
            s_statusLights = new List<StatusLight>();

            foreach (DataTable table in dataSet.Tables)
            {
                if (table.TableName == "DeviceDetail")
                {
                    foreach (DataRow row in table.Rows)
                    {
                        DeviceDetail dd = new DeviceDetail();
                        StatusLight sl = new StatusLight();
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

                        if (row.ConvertField<bool>("Enabled"))
                        {
                            sl.DeviceAcronym = row.ConvertField<string>("Acronym");
                            sl.Timestamp = new DateTime(1 / 1 / 1);
                            sl.GoodData = false;
                            s_statusLights.Add(sl);
                        }

                        s_deviceDetails.Add(dd);
                    }
                }
                else if (table.TableName == "MeasurementDetail")
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

                        s_measurementDetails.Add(md);

                    }
                }
                else if (table.TableName == "PhasorDetail")
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

                        s_phasorDetails.Add(pd);
                    }
                }
                else if (table.TableName == "SchemaVersion")
                {
                    foreach (DataRow row in table.Rows)
                    {
                        SchemaVersion sv = new SchemaVersion();
                        sv.VersionNumber = row.ConvertField<int>("VersionNumber");

                        s_schemaVersion.Add(sv);
                    }
                }
            }

            //MetaDataSet = dataSet;

            dataSet.WriteXml(FilePath.GetAbsolutePath("Metadata.xml"), XmlWriteMode.WriteSchema);
            Console.WriteLine("Data set serialized with {0} tables...", dataSet.Tables.Count);
        }

        static void StatisticSubscriptionNewMeasurements(object sender, EventArgs<ICollection<IMeasurement>> e)
        {
            lock (s_statsLock)
            {
                foreach (IMeasurement measurement in e.Argument)
                {
                    int index = s_statistics.IndexOf(m => m.ID == measurement.ID);

                    if (index < 0)
                    {
                        Measurement statistic = new Measurement();
                        statistic.ID = measurement.ID;
                        statistic.Value = measurement.Value;
                        DateTime date = new DateTime(measurement.Timestamp.Value);
                        statistic.Timestamp = (date.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                        s_statistics.Add(statistic);
                    }
                    else
                    {
                        s_statistics[index].Value = measurement.Value;
                        DateTime date = new DateTime(measurement.Timestamp.Value);
                        s_statistics[index].Timestamp = (date.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                    }

                }

            }
        }

        static void StatisticSubscriptionConnectionEstablished(object sender, EventArgs e)
        {
            s_statisticSubscription.SendServerCommand(ServerCommand.MetaDataRefresh);
        }

        static void StatisticSubscriptionConnectionTerminated(object sender, EventArgs e)
        {
            s_dataSubscription.Start();

            lock (s_displayLock)
            {
                Console.WriteLine("Connection to publisher was terminated, restarting connection cycle...");
            }
        }

        static void StatisticSubscriptionProcessException(object sender, EventArgs<Exception> e)
        {
            lock (s_displayLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("EXCEPTION: " + e.Argument.Message);
                Console.ResetColor();
            }
        }

        static void StatusLightsSubscriptionStatusMessage(object sender, EventArgs<string> e)
        {
            lock (s_displayLock)
            {
                Console.WriteLine(e.Argument);
            }
        }

        static void StatusLightsSubscriptionNewMeasurements(object sender, EventArgs<ICollection<IMeasurement>> e)
        {
            lock (s_lightsLock)
            {
                foreach (IMeasurement measurement in e.Argument)
                {
                    int index = s_measurementDetails.IndexOf(x => x.SignalID == measurement.ID);

                    if (index >= 0)
                    {
                        int slindex = s_statusLights.IndexOf(x => x.DeviceAcronym == s_measurementDetails[index].DeviceAcronym);
                        if (slindex >= 0)
                        {
                            s_statusLights[slindex].Timestamp = DateTime.UtcNow;
                            s_statusLights[slindex].GoodData = true;
                        }
                    }
                }
            }
        }

        static void StatusLightsSubscriptionConnectionTerminated(object sender, EventArgs e)
        {
            s_dataSubscription.Start();

            lock (s_displayLock)
            {
                Console.WriteLine("Connection to publisher was terminated, restarting connection cycle...");
            }
        }

        static void StatusLightsSubscriptionProcessException(object sender, EventArgs<Exception> e)
        {
            lock (s_displayLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("EXCEPTION: " + e.Argument.Message);
                Console.ResetColor();
            }
        }

        #endregion

        // Client-side script functionality

        #region [ DataHub Operations ]

        public IEnumerable<Measurement> GetMeasurements()
        {
            List<Measurement> returnData;
            lock (s_measurementLock)
            {
                returnData = new List<Measurement>(s_measurements);

            }
            s_measurements = new List<Measurement>();
            return returnData;
        }

        public IEnumerable<DeviceDetail> GetDeviceDetails()
        {
            return s_deviceDetails;
        }

        public IEnumerable<MeasurementDetail> GetMeasurementDetails()
        {
            return s_measurementDetails;
        }

        public IEnumerable<PhasorDetail> GetPhasorDetails()
        {
            return s_phasorDetails;
        }

        public IEnumerable<SchemaVersion> GetSchemaVersion()
        {
            return s_schemaVersion;
        }

        public IEnumerable<Measurement> GetStats()
        {
            return s_statistics;
        }

        public IEnumerable<StatusLight> GetLights()
        {
            foreach (StatusLight sl in s_statusLights)
            {
                DateTime now = DateTime.UtcNow;

                if ((now - sl.Timestamp).TotalSeconds > 30)
                    sl.GoodData = false;
            }

            return s_statusLights;
        }

        public void UpdateFilters(string filterString)
        {
            s_measurements = new List<Measurement>();

            s_dataSubscriptionInfo.FilterExpression = filterString;
            s_dataSubscription.UnsynchronizedSubscribe(s_dataSubscriptionInfo);
        }

        public void StatSubscribe(string filterString)
        {
            s_statisticSubscriptionInfo.FilterExpression = filterString;
            s_statisticSubscription.UnsynchronizedSubscribe(s_statisticSubscriptionInfo);
        }

        public void LightSubscribe(string filterString)
        {
            s_statusLightsSubscriptionInfo.FilterExpression = filterString;
            s_statusLightsSubscription.UnsynchronizedSubscribe(s_statusLightsSubscriptionInfo);
        }

        public UDTWriter CreateUDTWriter()
        {
            UDTCompiler udtc = new UDTCompiler();

            lock (m_udtLock)
            {
                udtc.Compile(s_udtFile);
            }

            UDTWriter udtw = new UDTWriter();
            udtw.Types.AddRange(udtc.DefinedTypes.OfType<UserDefinedType>());

            return udtw;
        }

        public MappingWriter CreateMappingWriter()
        {
            UDTCompiler udtc = new UDTCompiler();

            lock (m_udtLock)
            {
                udtc.Compile(s_udtFile);
            }

            MappingCompiler mc = new MappingCompiler(udtc);

            lock (m_mapLock)
            {
                mc.Compile(s_udmFile);
            }

            MappingWriter mw = new MappingWriter();

            mw.Mappings.AddRange(mc.DefinedMappings);

            return mw;
        }

        public IEnumerable<openECAClient.Model.DataType> GetDefinedTypes()
        {
            UDTCompiler compiler = new UDTCompiler();

            lock (m_udtLock)
            {
                compiler.Compile(s_udtFile);
            }

            return compiler.DefinedTypes;
        }

        public IEnumerable<TypeMapping> GetDefinedMappings()
        {
            UDTCompiler compiler = new UDTCompiler();

            lock (m_udtLock)
            {
                compiler.Compile(s_udtFile);
            }

            MappingCompiler mappingCompiler = new MappingCompiler(compiler);

            lock (m_mapLock)
            {
                mappingCompiler.Compile(s_udmFile);
            }

            return mappingCompiler.DefinedMappings;
        }

        public void AddUDT(UserDefinedType udt)
        {
            UDTWriter write = CreateUDTWriter();

            write.Types.Add(udt);

            lock (m_udtLock)
            {
                write.Write(s_udtFile);
            }
        }

        public void AddMapping(TypeMapping mt)
        {
            MappingWriter write = CreateMappingWriter();

            write.Mappings.Add(mt);

            lock (m_mapLock)
            {
                write.Write(s_udmFile);
            }
        }

        public List<DataType> GetEnumeratedReferenceTypes(DataType type)
        {
            List<DataType> referenceTypes = new List<DataType>();

            referenceTypes.Add(type);

            UDTCompiler compiler = new UDTCompiler();

            lock (m_udtLock)
            {
                compiler.Compile(s_udtFile);
            }

            GetEnumeratedReferenceTypes(type, referenceTypes, compiler);

            return referenceTypes;
        }

        public void GetEnumeratedReferenceTypes(DataType type, List<DataType> list, UDTCompiler compiler)
        {
            IEnumerable<DataType> item = compiler.EnumerateReferencingTypes(compiler.GetType(type.Category, type.Identifier));

            foreach (DataType dt in item)
            {
                list.Add(dt);
                GetEnumeratedReferenceTypes(dt, list, compiler);
            }
        }

        public List<TypeMapping> GetMappings(UserDefinedType udt)
        {
            UDTCompiler compiler = new UDTCompiler();

            lock (m_udtLock)
            {
                compiler.Compile(s_udtFile);
            }

            MappingCompiler mappingCompiler = new MappingCompiler(compiler);

            lock (m_mapLock)
            {
                mappingCompiler.Compile(s_udmFile);
            }

            return mappingCompiler.GetMappings(udt);
        }

        public void RemoveUDT(UserDefinedType udt)
        {
            UDTWriter write = CreateUDTWriter();

            int index = write.Types.FindIndex(x => x.Category.Equals(udt.Category) && x.Identifier.Equals(udt.Identifier));

            if (index > -1)
            {
                write.Types.RemoveAt(index);

                lock (m_udtLock)
                {
                    write.Write(s_udtFile);
                }
            }
        }

        public void RemoveMapping(TypeMapping mt)
        {
            MappingWriter write = CreateMappingWriter();

            int index = write.Mappings.FindIndex(x => x.Identifier.Equals(mt.Identifier) && x.Type.Identifier.Equals(mt.Type.Identifier));

            if (index > -1)
            {
                write.Mappings.RemoveAt(index);

                lock (m_mapLock)
                {
                    write.Write(s_udmFile);
                }
            }

            Debug.WriteLine(write.Mappings);
        }

        public void CreateProject(string projectName, string directory, TypeMapping input, TypeMapping output)
        {
            UDTCompiler compiler = new UDTCompiler();

            lock (m_udtLock)
            {
                compiler.Compile(s_udtFile);
            }

            MappingCompiler mappingCompiler = new MappingCompiler(compiler);

            lock (m_mapLock)
            {
                mappingCompiler.Compile(s_udmFile);
            }

            ProjectGenerator project = new ProjectGenerator(projectName, mappingCompiler);

            project.Generate(directory, input, output);
        }

        #endregion

        #region [ Miscellaneous Hub Operations ]

        // TODO: Is this referenced?
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
