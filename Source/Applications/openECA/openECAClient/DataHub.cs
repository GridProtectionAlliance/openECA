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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GSF.IO;
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
        private DataHubClient m_client;

        #endregion

        #region [ Properties ]

        private DataHubClient Client
        {
            get
            {
                return m_client ?? (m_client = s_dataHubClients.GetOrAdd(Context.ConnectionId, id => new DataHubClient(Clients.Client(Context.ConnectionId))));
            }
        }

        #endregion

        #region [ Methods ]

        public override Task OnConnected()
        {
            // Store the current connection ID for this thread
            s_connectionID.Value = Context.ConnectionId;
            s_connectCount++;

            Program.LogStatus($"DataHub connect by {Context.User?.Identity?.Name ?? "Undefined User"} [{Context.ConnectionId}] - count = {s_connectCount}");

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            if (stopCalled)
            {
                DataHubClient client;

                // Dispose of data hub client when client connection is disconnected
                if (s_dataHubClients.TryRemove(Context.ConnectionId, out client))
                    client.Dispose();

                m_client = null;
                s_connectCount--;

                Program.LogStatus($"DataHub disconnect by {Context.User?.Identity?.Name ?? "Undefined User"} [{Context.ConnectionId}] - count = {s_connectCount}");
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
        private static readonly ConcurrentDictionary<string, DataHubClient> s_dataHubClients;
        private static readonly ThreadLocal<string> s_connectionID;
        private static volatile int s_connectCount;
        private static readonly string s_udtFile;
        private static readonly string s_udmFile;
        private static readonly object s_udtLock;
        private static readonly object s_udmLock;

        // Static Constructor
        static DataHub()
        {
            s_dataHubClients = new ConcurrentDictionary<string, DataHubClient>(StringComparer.OrdinalIgnoreCase);
            s_connectionID = new ThreadLocal<string>();
            s_udtFile = Path.Combine(FilePath.GetAbsolutePath("wwwroot"), "Data", "UserDefinedTypes.txt");
            s_udmFile = Path.Combine(FilePath.GetAbsolutePath("wwwroot"), "Data", "UserDefinedMappings.txt");
            s_udtLock = new object();
            s_udmLock = new object();
        }

        #endregion

        // Client-side script functionality

        #region [ DataHub Operations ]

        public IEnumerable<Measurement> GetMeasurements()
        {
            return Client.Measurements;
        }

        public IEnumerable<DeviceDetail> GetDeviceDetails()
        {
            return Client.DeviceDetails;
        }

        public IEnumerable<MeasurementDetail> GetMeasurementDetails()
        {
            return Client.MeasurementDetails;
        }

        public IEnumerable<PhasorDetail> GetPhasorDetails()
        {
            return Client.PhasorDetails;
        }

        public IEnumerable<SchemaVersion> GetSchemaVersion()
        {
            return Client.SchemaVersion;
        }

        public IEnumerable<Measurement> GetStats()
        {
            return Client.Statistics;
        }

        public IEnumerable<StatusLight> GetLights()
        {
            return Client.StatusLights;
        }

        public void InitializeSubscriptions()
        {
            Client.InitializeSubscriptions();
        }

        public void UpdateFilters(string filterExpression)
        {
            Client.UpdatePrimaryDataSubscription(filterExpression);
        }

        public void StatSubscribe(string filterExpression)
        {
            Client.UpdateStatisticsDataSubscription(filterExpression);
        }

        public void LightSubscribe(string filterExpression)
        {
            Client.UpdateStatusLightsSubscription(filterExpression);
        }

        private UDTCompiler CreateUDTCompiler()
        {
            UDTCompiler udtCompiler = new UDTCompiler();

            lock (s_udtLock)
            {
                if (File.Exists(s_udtFile))
                    udtCompiler.Compile(s_udtFile);
            }

            return udtCompiler;
        }

        private UDTWriter CreateUDTWriter()
        {
            UDTCompiler udtCompiler = CreateUDTCompiler();
            UDTWriter udtWriter = new UDTWriter();
            udtWriter.Types.AddRange(udtCompiler.DefinedTypes.OfType<UserDefinedType>());
            return udtWriter;
        }

        private MappingCompiler CreateMappingCompiler()
        {
            UDTCompiler udtCompiler = CreateUDTCompiler();
            MappingCompiler mappingCompiler = new MappingCompiler(udtCompiler);

            lock (s_udmLock)
            {
                if (File.Exists(s_udmFile))
                    mappingCompiler.Compile(s_udmFile);
            }

            return mappingCompiler;
        }

        private MappingWriter CreateMappingWriter()
        {
            MappingCompiler mappingCompiler = CreateMappingCompiler();
            MappingWriter mappingWriter = new MappingWriter();
            mappingWriter.Mappings.AddRange(mappingCompiler.DefinedMappings);
            return mappingWriter;
        }

        public IEnumerable<DataType> GetDefinedTypes()
        {
            UDTCompiler udtCompiler = CreateUDTCompiler();
            return udtCompiler.DefinedTypes;
        }

        public IEnumerable<TypeMapping> GetDefinedMappings()
        {
            MappingCompiler mappingCompiler = CreateMappingCompiler();
            return mappingCompiler.DefinedMappings;
        }

        public void AddUDT(UserDefinedType udt)
        {
            UDTWriter udtWriter = CreateUDTWriter();

            udtWriter.Types.Add(udt);

            lock (s_udtLock)
                udtWriter.Write(s_udtFile);
        }

        public void AddMapping(TypeMapping typeMapping)
        {
            MappingWriter mappingWriter = CreateMappingWriter();

            mappingWriter.Mappings.Add(typeMapping);

            lock (s_udmLock)
                mappingWriter.Write(s_udmFile);
        }

        public List<DataType> GetEnumeratedReferenceTypes(DataType dataType)
        {
            List<DataType> referenceTypes = new List<DataType>();
            UDTCompiler udtCompiler = CreateUDTCompiler();

            referenceTypes.Add(dataType);
            GetEnumeratedReferenceTypes(dataType, referenceTypes, udtCompiler);

            return referenceTypes;
        }

        public void GetEnumeratedReferenceTypes(DataType dataType, List<DataType> dataTypes, UDTCompiler compiler)
        {
            IEnumerable<DataType> referencingTypes = compiler.EnumerateReferencingTypes(compiler.GetType(dataType.Category, dataType.Identifier));

            foreach (DataType referencingType in referencingTypes)
            {
                dataTypes.Add(referencingType);
                GetEnumeratedReferenceTypes(referencingType, dataTypes, compiler);
            }
        }

        public List<TypeMapping> GetMappings(UserDefinedType udt)
        {
            MappingCompiler mappingCompiler = CreateMappingCompiler();
            return mappingCompiler.GetMappings(udt);
        }

        public void RemoveUDT(UserDefinedType udt)
        {
            UDTWriter udtWriter = CreateUDTWriter();

            int index = udtWriter.Types.FindIndex(type => type.Category.Equals(udt.Category) && type.Identifier.Equals(udt.Identifier));

            if (index > -1)
            {
                udtWriter.Types.RemoveAt(index);

                lock (s_udtLock)
                    udtWriter.Write(s_udtFile);
            }
        }

        public void RemoveMapping(TypeMapping typeMapping)
        {
            MappingWriter mappingWriter = CreateMappingWriter();

            int index = mappingWriter.Mappings.FindIndex(mapping => mapping.Identifier.Equals(typeMapping.Identifier) && mapping.Type.Identifier.Equals(typeMapping.Type.Identifier));

            if (index > -1)
            {
                mappingWriter.Mappings.RemoveAt(index);

                lock (s_udmLock)
                    mappingWriter.Write(s_udmFile);
            }
        }

        public void CreateProject(string projectName, string targetDirectory, TypeMapping inputMapping, TypeMapping outputMapping, string targetLanguage)
        {
            MappingCompiler mappingCompiler = CreateMappingCompiler();
            TypeMapping compiledInput = mappingCompiler.GetTypeMapping(inputMapping.Identifier);
            TypeMapping compiledOutput = mappingCompiler.GetTypeMapping(outputMapping.Identifier);

            if (targetLanguage == "C#")
            {
                openECAClient.Template.CSharp.ProjectGenerator projectGenerator = new openECAClient.Template.CSharp.ProjectGenerator(projectName, mappingCompiler);
                projectGenerator.Generate(targetDirectory, compiledInput, compiledOutput);
            }
            else if (targetLanguage == "F#")
            {
            }
            else if (targetLanguage == "VB")
            {
            }
            else if (targetLanguage == "F#")
            {
            }
            else if (targetLanguage == "Java")
            {
            }
            else if (targetLanguage == "C++")
            {
            }
            else if (targetLanguage == "Python")
            {
            }
            else if (targetLanguage == "MATLAB")
            {
            }

        }

        #endregion

        #region [ Miscellaneous Hub Operations ]

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
