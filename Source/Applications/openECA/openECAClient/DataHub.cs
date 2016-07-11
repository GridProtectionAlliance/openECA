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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ECAClientUtilities;
using ECAClientUtilities.Model;
using GSF.Configuration;
using GSF.IO;
using GSF.Web.Security;
using Microsoft.AspNet.SignalR;
using openECAClient.Model;

using DataType = ECAClientUtilities.Model.DataType;
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
        private static readonly string s_udtDirectory;
        private static readonly string s_udmDirectory;
        private static readonly object s_udtLock;
        private static readonly object s_udmLock;

        // Static Constructor
        static DataHub()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string ecaClientDataPath = Path.Combine(appData, "Grid Protection Alliance", "openECAClient");

            s_dataHubClients = new ConcurrentDictionary<string, DataHubClient>(StringComparer.OrdinalIgnoreCase);
            s_connectionID = new ThreadLocal<string>();
            s_udtDirectory = Path.Combine(ecaClientDataPath, "UserDefinedTypes");
            s_udmDirectory = Path.Combine(ecaClientDataPath, "UserDefinedMappings");
            s_udtLock = new object();
            s_udmLock = new object();
        }

        #endregion

        // Client-side script functionality

        #region [ DataHub Operations ]

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
            UDTWriter udtWriter = new UDTWriter();

            udtWriter.Types.Add(udt);

            lock (s_udtLock)
                udtWriter.WriteFiles(s_udtDirectory);
        }

        public void UpdateUDT(UserDefinedType udt, string oldCat, string oldIdent)
        {
            UDTCompiler udtCompiler = CreateUDTCompiler();

            MappingCompiler mappingCompiler = new MappingCompiler(udtCompiler);
            mappingCompiler.Scan(s_udmDirectory);

            foreach (UserDefinedType dt in udtCompiler.DefinedTypes.OfType<UserDefinedType>())
            {
                if (dt.Category == oldCat && dt.Identifier == oldIdent)
                {
                    dt.Fields.Clear();
                    foreach (UDTField dataType in udt.Fields)
                    {
                        dt.Fields.Add(dataType);
                    }

                    dt.Category = udt.Category;
                    dt.Identifier = udt.Identifier;
                }
            }

            string categoryPath = Path.Combine(s_udtDirectory, oldCat);
            string typePath = Path.Combine(categoryPath, oldIdent + ".ecaidl");

            lock (s_udtLock)
            {
                File.Delete(typePath);

                if (!Directory.EnumerateFileSystemEntries(categoryPath).Any())
                    Directory.Delete(categoryPath);
            }


            UDTWriter udtWriter = new UDTWriter();
            udtWriter.Types.AddRange(udtCompiler.DefinedTypes.OfType<UserDefinedType>());

            lock (s_udtLock)
                udtWriter.WriteFiles(s_udtDirectory);

            MappingWriter mappingWriter = new MappingWriter();
            mappingWriter.Mappings.AddRange(mappingCompiler.DefinedMappings);

            lock (s_udmLock)
                mappingWriter.WriteFiles(s_udmDirectory);
        }

        public void AddMapping(TypeMapping typeMapping)
        {
            MappingWriter mappingWriter = new MappingWriter();

            mappingWriter.Mappings.Add(typeMapping);

            lock (s_udmLock)
                mappingWriter.WriteFiles(s_udmDirectory);
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
            string categoryPath = Path.Combine(s_udtDirectory, udt.Category);
            string typePath = Path.Combine(categoryPath, udt.Identifier + ".ecaidl");

            lock (s_udtLock)
            {
                File.Delete(typePath);

                if (!Directory.EnumerateFileSystemEntries(categoryPath).Any())
                    Directory.Delete(categoryPath);
            }
        }

        public void RemoveMapping(TypeMapping typeMapping)
        {
            string mappingPath = Path.Combine(s_udmDirectory, typeMapping.Identifier + ".ecamap");

            lock (s_udmLock)
                File.Delete(mappingPath);
        }

        public void ExportUDTs(IEnumerable<UserDefinedType> list, string file)
        {
            UDTWriter udtWriter = new UDTWriter();

            foreach (UserDefinedType udt in list)
            {
                udtWriter.Types.Add(udt);
            }

            lock (s_udtLock)
                udtWriter.Write(file);

        }

        public void ExportMappings(IEnumerable<TypeMapping> list, string file)
        {
            MappingWriter mappingWriter = new MappingWriter();

            foreach (TypeMapping tm in list)
            {
                mappingWriter.Mappings.Add(tm);
            }

            lock (s_udmLock)
                mappingWriter.Write(file);
        }

        public List<DataType> ReadUDTFile(string udtfileContents)
        {
            //Debug.WriteLine(blob);
            StringReader udtsr = new StringReader(udtfileContents);


            UDTCompiler compiler = new UDTCompiler();
            compiler.Compile(udtsr);

            return compiler.DefinedTypes.Where(x => x.IsUserDefined).ToList();
        }

        public List<TypeMapping> ReadMappingFile(string udtfileContents, string mappingFileContents)
        {
            StringReader udtsr = new StringReader(udtfileContents);
            StringReader mappingsr = new StringReader(mappingFileContents);

            UDTCompiler comp = new UDTCompiler();
            comp.Compile(udtsr);
            MappingCompiler compiler = new MappingCompiler(comp);
            compiler.Compile(mappingsr);

            return compiler.DefinedMappings;
        }

        public void ImportData(IEnumerable<UserDefinedType> userDefinedTypes, IEnumerable<TypeMapping> typeMappings)
        {
            if (!userDefinedTypes.Any())
                return;

            UDTWriter udtWriter = new UDTWriter();

            foreach (UserDefinedType type in userDefinedTypes)
                udtWriter.Types.Add(type);

            lock (s_udtLock)
                udtWriter.WriteFiles(s_udtDirectory);

            if (!typeMappings.Any())
                return;

            MappingWriter mappingWriter = new MappingWriter();

            foreach (TypeMapping mapping in typeMappings)
                mappingWriter.Mappings.Add(mapping);

            lock (s_udmLock)
                mappingWriter.WriteFiles(s_udmDirectory);
        }

        public string UpdateUDT(string udtFileContents, string category, string identifier, string newcat, string newident)
        {
            StringReader udtsr = new StringReader(udtFileContents);

            UDTCompiler udtCompiler = new UDTCompiler();
            udtCompiler.Compile(udtsr);

            foreach (DataType dt in udtCompiler.DefinedTypes)
            {
                if (dt.Category == category && dt.Identifier == identifier)
                {
                    if (newcat != null)
                        dt.Category = newcat;
                    if (newident != null)
                        dt.Identifier = newident;
                }
            }

            UDTWriter udtWriter = new UDTWriter();

            udtWriter.Types.AddRange(udtCompiler.DefinedTypes.OfType<UserDefinedType>());

            StringBuilder sb = new StringBuilder();
            udtWriter.Write(new StringWriter(sb));

            return sb.ToString();
        }

        public string UpdateMappingForUDT(string udtFileContents, string mappingFileContents, string category, string identifier, string newcat, string newident)
        {
            StringReader udtsr = new StringReader(udtFileContents);
            StringReader mappingsr = new StringReader(mappingFileContents);

            UDTCompiler udtCompiler = new UDTCompiler();
            udtCompiler.Compile(udtsr);
            MappingCompiler mappingCompiler = new MappingCompiler(udtCompiler);
            mappingCompiler.Compile(mappingsr);

            foreach (DataType dt in udtCompiler.DefinedTypes)
            {
                if (dt.Category == category && dt.Identifier == identifier)
                {
                    if (newcat != null)
                        dt.Category = newcat;
                    if (newident != null)
                        dt.Identifier = newident;
                }
            }

            MappingWriter mappingWriter = new MappingWriter();
            mappingWriter.Mappings.AddRange(mappingCompiler.DefinedMappings);

            StringBuilder sb = new StringBuilder();
            mappingWriter.Write(new StringWriter(sb));

            return sb.ToString();
        }

        public string UpdateMapping(string udtFileContents, string mappingFileContents, string identifier, string newident)
        {
            StringReader udtsr = new StringReader(udtFileContents);
            StringReader mappingsr = new StringReader(mappingFileContents);

            UDTCompiler udtCompiler = new UDTCompiler();
            udtCompiler.Compile(udtsr);

            MappingCompiler mappingCompiler = new MappingCompiler(udtCompiler);
            mappingCompiler.Compile(mappingsr);

            foreach (TypeMapping tm in mappingCompiler.DefinedMappings)
            {
                if (tm.Identifier == identifier)
                    tm.Identifier = newident;
            }

            MappingWriter mappingWriter = new MappingWriter();
            mappingWriter.Mappings.AddRange(mappingCompiler.DefinedMappings);

            StringBuilder sb = new StringBuilder();
            mappingWriter.Write(new StringWriter(sb));

            return sb.ToString();
        }

        public void FixUDT(string filePath, string contents)
        {
            UDTCompiler udtCompiler = CreateUDTCompiler();

            if (udtCompiler.BatchErrors.Any(ex => ex.FilePath == filePath))
                File.WriteAllText(filePath, contents);
        }

        public void FixMapping(string filePath, string contents)
        {
            MappingCompiler mappingCompiler = CreateMappingCompiler();

            if (mappingCompiler.BatchErrors.Any(ex => ex.FilePath == filePath))
                File.WriteAllText(filePath, contents);
        }

        public List<InvalidUDTException> GetUDTCompilerErrors()
        {
            UDTCompiler udtCompiler = CreateUDTCompiler();
            return udtCompiler.BatchErrors;
        }

        public List<InvalidMappingException> GetMappingCompilerErrors()
        {
            MappingCompiler mappingCompiler = CreateMappingCompiler();
            return mappingCompiler.BatchErrors;
        }

        public void CreateProject(string projectName, string targetDirectory, TypeMapping inputMapping, TypeMapping outputMapping, string targetLanguage)
        {
            MappingCompiler mappingCompiler = CreateMappingCompiler();
            TypeMapping compiledInput = mappingCompiler.GetTypeMapping(inputMapping.Identifier);
            TypeMapping compiledOutput = mappingCompiler.GetTypeMapping(outputMapping.Identifier);

            if (targetLanguage == "C#")
            {
                ECAClientUtilities.Template.CSharp.ProjectGenerator projectGenerator = new ECAClientUtilities.Template.CSharp.ProjectGenerator(projectName, mappingCompiler);
                projectGenerator.Settings.SubscriberConnectionString = MainWindow.Model.Global.SubscriptionConnectionString;
                projectGenerator.Generate(targetDirectory, compiledInput, compiledOutput);
            }
            else if (targetLanguage == "F#")
            {
                ECAClientUtilities.Template.FSharp.ProjectGenerator projectGenerator = new ECAClientUtilities.Template.FSharp.ProjectGenerator(projectName, mappingCompiler);
                projectGenerator.Settings.SubscriberConnectionString = MainWindow.Model.Global.SubscriptionConnectionString;
                projectGenerator.Generate(targetDirectory, compiledInput, compiledOutput);
            }
            else if (targetLanguage == "VB")
            {
                ECAClientUtilities.Template.VisualBasic.ProjectGenerator projectGenerator = new ECAClientUtilities.Template.VisualBasic.ProjectGenerator(projectName, mappingCompiler);
                projectGenerator.Settings.SubscriberConnectionString = MainWindow.Model.Global.SubscriptionConnectionString;
                projectGenerator.Generate(targetDirectory, compiledInput, compiledOutput);
            }
            else if (targetLanguage == "IronPython")
            {
                ECAClientUtilities.Template.IronPython.ProjectGenerator projectGenerator = new ECAClientUtilities.Template.IronPython.ProjectGenerator(projectName, mappingCompiler);
                projectGenerator.Settings.SubscriberConnectionString = MainWindow.Model.Global.SubscriptionConnectionString;
                projectGenerator.Generate(targetDirectory, compiledInput, compiledOutput);
            }
            else if (targetLanguage == "MATLAB")
            {
                ECAClientUtilities.Template.Matlab.ProjectGenerator projectGenerator = new ECAClientUtilities.Template.Matlab.ProjectGenerator(projectName, mappingCompiler);
                projectGenerator.Settings.SubscriberConnectionString = MainWindow.Model.Global.SubscriptionConnectionString;
                projectGenerator.Generate(targetDirectory, compiledInput, compiledOutput);
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
        }

        public string GetUDTFileDirectory()
        {
            return s_udtDirectory;
        }

        public string GetMappingFileDirectory()
        {
            return s_udmDirectory;
        }

        public Dictionary<string, string> GetApplicationSettings()
        {
            CategorizedSettingsElementCollection systemSettings = ConfigurationFile.Current.Settings["systemSettings"];

            return systemSettings
                .Cast<CategorizedSettingsElement>()
                .Where(setting => setting.Scope == SettingScope.User)
                .ToDictionary(setting => setting.Name, setting => setting.Value);
        }

        public void UpdateApplicationSettings(Dictionary<string, string> settings)
        {
            ConfigurationFile configurationFile = ConfigurationFile.Current;
            CategorizedSettingsElementCollection systemSettings = configurationFile.Settings["systemSettings"];

            // Update the configuration file
            foreach (KeyValuePair<string, string> setting in settings)
            {
                if (systemSettings[setting.Key]?.Scope == SettingScope.User)
                    systemSettings[setting.Key].Update(setting.Value);
            }

            // Update the global settings in AppModel
            MainWindow.Model.Global.SubscriptionConnectionString = systemSettings["SubscriptionConnectionString"].Value;

            string currentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            MainWindow.Model.Global.DefaultProjectPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(systemSettings["DefaultProjectPath"].Value));
            Directory.SetCurrentDirectory(currentDirectory);

            // Save the configuration settings to the application configuration file
            configurationFile.Save();
        }

        private UDTCompiler CreateUDTCompiler()
        {
            UDTCompiler udtCompiler = new UDTCompiler();

            lock (s_udtLock)
            {
                if (Directory.Exists(s_udtDirectory))
                    udtCompiler.Scan(s_udtDirectory);
            }

            return udtCompiler;
        }

        private MappingCompiler CreateMappingCompiler()
        {
            UDTCompiler udtCompiler = CreateUDTCompiler();
            MappingCompiler mappingCompiler = new MappingCompiler(udtCompiler);

            lock (s_udmLock)
            {
                if (Directory.Exists(s_udmDirectory))
                    mappingCompiler.Scan(s_udmDirectory);
            }

            return mappingCompiler;
        }

        #endregion

        #region [ DataHub Client Connection Operations ]

        // These functions are dependent on subscriptions to data where each client connection can customize the subscriptions, so
        // an instance of the DataHubClient is created per SignalR DataHub client connection to manage the subscription life-cycles.

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

        public void TerminateSubscriptions()
        {
            Client.TerminateSubscriptions();
        }

        public void UpdateFilters(string filterExpression)
        {
            Client.UpdatePrimaryDataSubscription(filterExpression);
        }

        public void StatSubscribe(string filterExpression)
        {
            Client.UpdateStatisticsDataSubscription(filterExpression);
        }

        #endregion

        #region [ DirectoryBrowser Hub Operations ]

        public IEnumerable<string> LoadDirectories(string rootFolder, bool showHidden)
        {
            if (string.IsNullOrWhiteSpace(rootFolder))
                return Directory.GetLogicalDrives();

            IEnumerable<string> directories = Directory.GetDirectories(rootFolder);

            if (!showHidden)
                directories = directories.Where(path => !new DirectoryInfo(path).Attributes.HasFlag(FileAttributes.Hidden));

            return new[] { "..\\" }.Concat(directories.Select(path => FilePath.AddPathSuffix(FilePath.GetLastDirectoryName(path))));
        }

        public bool IsLogicalDrive(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            DirectoryInfo info = new DirectoryInfo(path);
            return info.FullName == info.Root.FullName;
        }

        public string ResolvePath(string path)
        {
            if (IsLogicalDrive(path) && Path.GetFullPath(path) == path)
                return path;

            return Path.GetFullPath(FilePath.GetAbsolutePath(Environment.ExpandEnvironmentVariables(path)));
        }

        public string CombinePath(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        public void CreatePath(string path)
        {
            Directory.CreateDirectory(path);
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

        /// <summary>
        /// Gets the current server time.
        /// </summary>
        /// <returns>Current server time.</returns>
        public DateTime GetServerTime() => DateTime.UtcNow;

        /// <summary>
        /// Gets current performance statistics for service.
        /// </summary>
        /// <returns>Current performance statistics for service.</returns>
        public string GetPerformanceStatistics() => Program.PerformanceMonitor.Status;

        #endregion
    }
}
