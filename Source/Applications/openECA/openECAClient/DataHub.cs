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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ECACommonUtilities;
using ECACommonUtilities.Model;
using GSF.Configuration;
using GSF.IO;
using GSF.Web.Model.HubOperations;
using GSF.Web.Security;
using Microsoft.AspNet.SignalR;
using openECAClient.Model;

using DataType = ECACommonUtilities.Model.DataType;

namespace openECAClient
{
    public class DataHub : Hub, IDataSubscriptionOperations, IDirectoryBrowserOperations
    {
        #region [ Members ]

        // Fields
        private readonly DataSubscriptionOperations m_dataSubscriptionOperations;

        #endregion

        #region [ Constructors ]

        public DataHub()
        {
            m_dataSubscriptionOperations = new DataSubscriptionOperations(this, (message, updateType) => Program.LogStatus(message), ex => Program.LogException(ex));
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets name from user identity for current context, if available.
        /// </summary>
        public string UserName
        {
            get
            {
                try
                {
                    return Context.User?.Identity?.Name ?? "Undefined User";
                }
                catch (NullReferenceException)
                {
                    return null;
                }
            }
        }

        #endregion

        #region [ Methods ]

        public override Task OnConnected()
        {
            // Store the current connection ID for this thread
            s_connectionID.Value = Context.ConnectionId;

            string userName = UserName;

            if ((object)userName != null)
            {
                s_connectCount++;
                Program.LogStatus($"DataHub connect by {UserName} [{Context.ConnectionId}] - count = {s_connectCount}");
            }

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            if (stopCalled)
            {
                // Dispose any associated hub operations associated with current SignalR client
                m_dataSubscriptionOperations?.EndSession();

                string userName = UserName;

                if ((object)userName != null)
                {
                    s_connectCount--;
                    Program.LogStatus($"DataHub disconnect by {UserName} [{Context.ConnectionId}] - count = {s_connectCount}");
                }
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
        private static readonly ThreadLocal<string> s_connectionID;
        private static volatile int s_connectCount;
        private static readonly string s_udtDirectory;
        private static readonly string s_udimDirectory;
        private static readonly string s_udomDirectory;
        private static readonly object s_udtLock;
        private static readonly object s_udimLock;
        private static readonly object s_udomLock;

        // Static Constructor
        static DataHub()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string ecaClientDataPath = Path.Combine(appData, "Grid Protection Alliance", "openECAClient");

            s_connectionID = new ThreadLocal<string>();
            s_udtDirectory = Path.Combine(ecaClientDataPath, "UserDefinedTypes");
            s_udimDirectory = Path.Combine(ecaClientDataPath, "UserDefinedInputMappings");
            s_udomDirectory = Path.Combine(ecaClientDataPath, "UserDefinedOutputMappings");
            s_udtLock = new object();
            s_udimLock = new object();
            s_udomLock = new object();
        }

        #endregion

        // Client-side script functionality

        #region [ User Defined Types ]

        public IEnumerable<DataType> GetDefinedTypes()
        {
            UDTCompiler udtCompiler = CreateUDTCompiler();
            return udtCompiler.DefinedTypes;
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

            MappingCompiler mappingInputCompiler = new MappingCompiler(udtCompiler);
            mappingInputCompiler.Scan(s_udimDirectory);
            MappingCompiler mappingOutputCompiler = new MappingCompiler(udtCompiler);
            mappingOutputCompiler.Scan(s_udomDirectory);


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

            MappingWriter mappingInputWriter = new MappingWriter();
            mappingInputWriter.Mappings.AddRange(mappingInputCompiler.DefinedMappings);

            lock (s_udimLock)
                mappingInputWriter.WriteFiles(s_udomDirectory);

            MappingWriter mappingOutputWriter = new MappingWriter();
            mappingOutputWriter.Mappings.AddRange(mappingInputCompiler.DefinedMappings);

            lock (s_udimLock)
                mappingOutputWriter.WriteFiles(s_udomDirectory);

        }

        public void RemoveUDT(UserDefinedType udt)
        {

            UDTCompiler udtCompiler = CreateUDTCompiler();
            MappingCompiler mappingInputCompiler = new MappingCompiler(udtCompiler);
            mappingInputCompiler.Scan(s_udimDirectory);
            MappingCompiler mappingOutputCompiler = new MappingCompiler(udtCompiler);
            mappingOutputCompiler.Scan(s_udomDirectory);


            List<DataType> dataTypes = GetEnumeratedReferenceTypes(udt);
            dataTypes.Reverse();

            foreach (DataType type in dataTypes)
            {
                foreach ( TypeMapping mapping in mappingInputCompiler.GetMappings((UserDefinedType)type))
                {
                    RemoveInputMapping( mapping);
                }

                foreach (TypeMapping mapping in mappingOutputCompiler.GetMappings((UserDefinedType)type))
                {
                    RemoveOutputMapping(mapping);
                }

                string categoryPath = Path.Combine(s_udtDirectory, type.Category);
                string typePath = Path.Combine(categoryPath, type.Identifier + ".ecaidl");

                lock (s_udtLock)
                {
                    File.Delete(typePath);

                    if (!Directory.EnumerateFileSystemEntries(categoryPath).Any())
                        Directory.Delete(categoryPath);
                }
            }

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

        public void FixUDT(string filePath, string contents)
        {
            UDTCompiler udtCompiler = CreateUDTCompiler();

            if (udtCompiler.BatchErrors.Any(ex => ex.FilePath == filePath))
                File.WriteAllText(filePath, contents);
        }

        public List<InvalidUDTException> GetUDTCompilerErrors()
        {
            UDTCompiler udtCompiler = CreateUDTCompiler();
            return udtCompiler.BatchErrors;
        }

        public string GetUDTFileDirectory()
        {
            return s_udtDirectory;
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

        public List<DataType> ReadUDTFile(string udtfileContents)
        {
            StringReader udtsr = new StringReader(udtfileContents);


            UDTCompiler compiler = new UDTCompiler();
            compiler.Compile(udtsr);

            return compiler.DefinedTypes.Where(x => x.IsUserDefined).ToList();
        }

        #endregion

        #region [ User Defined Input Mappings ]

        public void AddInputMapping(TypeMapping typeMapping)
        {
            TypeMapping tm = ParseTimeWindow(typeMapping);

            MappingWriter mappingWriter = new MappingWriter();

            mappingWriter.Mappings.Add(tm);

            lock (s_udimLock)
                mappingWriter.WriteFiles(s_udimDirectory);
        }

        public void RemoveInputMapping(TypeMapping typeMapping)
        {
            string mappingPath = Path.Combine(s_udimDirectory, typeMapping.Identifier + ".ecamap");

            lock (s_udimLock)
                File.Delete(mappingPath);
        }

        public void EditInputMapping(TypeMapping typeMapping)
        {
            RemoveInputMapping(typeMapping);
            AddInputMapping(typeMapping);
        }

        private MappingCompiler CreateInputMappingCompiler()
        {
            UDTCompiler udtCompiler = CreateUDTCompiler();
            MappingCompiler mappingCompiler = new MappingCompiler(udtCompiler);

            lock (s_udimLock)
            {
                if (Directory.Exists(s_udimDirectory))
                    mappingCompiler.Scan(s_udimDirectory);
            }

            return mappingCompiler;
        }

        public IEnumerable<TypeMapping> GetDefinedInputMappings()
        {
            MappingCompiler mappingCompiler = CreateInputMappingCompiler();
            return mappingCompiler.DefinedMappings;
        }

        public List<TypeMapping> GetInputMappings(UserDefinedType udt)
        {
            MappingCompiler mappingCompiler = CreateInputMappingCompiler();
            return mappingCompiler.GetMappings(udt);
        }

        public void ExportInputMappings(IEnumerable<TypeMapping> list, string file)
        {
            MappingWriter mappingWriter = new MappingWriter();

            foreach (TypeMapping tm in list)
            {
                mappingWriter.Mappings.Add(tm);
            }

            lock (s_udimLock)
                mappingWriter.Write(file);
        }

        public void FixInputMapping(string filePath, string contents)
        {
            MappingCompiler mappingCompiler = CreateInputMappingCompiler();

            if (mappingCompiler.BatchErrors.Any(ex => ex.FilePath == filePath))
                File.WriteAllText(filePath, contents);
        }

        public List<InvalidMappingException> GetInputMappingCompilerErrors()
        {
            MappingCompiler mappingCompiler = CreateInputMappingCompiler();
            return mappingCompiler.BatchErrors;
        }

        public string GetInputMappingFileDirectory()
        {
            return s_udimDirectory;
        }

        #endregion

        #region [ User Defined Output Mappings ]

        public void AddOutputMapping(TypeMapping typeMapping)
        {
            TypeMapping tm = ParseTimeWindow(typeMapping);

            MappingWriter mappingWriter = new MappingWriter();

            mappingWriter.Mappings.Add(tm);

            lock (s_udomLock)
                mappingWriter.WriteFiles(s_udomDirectory);
        }

        public void RemoveOutputMapping(TypeMapping typeMapping)
        {
            string mappingPath = Path.Combine(s_udomDirectory, typeMapping.Identifier + ".ecamap");

            lock (s_udomLock)
                File.Delete(mappingPath);
        }

        public void EditOutputMapping(TypeMapping typeMapping)
        {
            RemoveOutputMapping(typeMapping);
            AddOutputMapping(typeMapping);
        }

        private MappingCompiler CreateOutputMappingCompiler()
        {
            UDTCompiler udtCompiler = CreateUDTCompiler();
            MappingCompiler mappingCompiler = new MappingCompiler(udtCompiler);

            lock (s_udomLock)
            {
                if (Directory.Exists(s_udomDirectory))
                    mappingCompiler.Scan(s_udomDirectory);
            }

            return mappingCompiler;
        }

        public IEnumerable<TypeMapping> GetDefinedOutputMappings()
        {
            MappingCompiler mappingCompiler = CreateOutputMappingCompiler();
            return mappingCompiler.DefinedMappings;
        }

        public List<TypeMapping> GetOutputMappings(UserDefinedType udt)
        {
            MappingCompiler mappingCompiler = CreateOutputMappingCompiler();
            return mappingCompiler.GetMappings(udt);
        }

        public void ExportOutputMappings(IEnumerable<TypeMapping> list, string file)
        {
            MappingWriter mappingWriter = new MappingWriter();

            foreach (TypeMapping tm in list)
            {
                mappingWriter.Mappings.Add(tm);
            }

            lock (s_udomLock)
                mappingWriter.Write(file);
        }

        public void FixOutputMapping(string filePath, string contents)
        {
            MappingCompiler mappingCompiler = CreateOutputMappingCompiler();

            if (mappingCompiler.BatchErrors.Any(ex => ex.FilePath == filePath))
                File.WriteAllText(filePath, contents);
        }

        public List<InvalidMappingException> GetOutputMappingCompilerErrors()
        {
            MappingCompiler mappingCompiler = CreateOutputMappingCompiler();
            return mappingCompiler.BatchErrors;
        }

        public string GetOutputMappingFileDirectory()
        {
            return s_udomDirectory;
        }

        #endregion


        #region [ Combined Mapping Functions ]

        public void AddMapping(TypeMapping typeMapping, bool isOutput)
        {
            if (isOutput)
                AddOutputMapping(typeMapping);

            else
                AddInputMapping(typeMapping);
        }

        public void RemoveMapping(TypeMapping typeMapping, bool isOutput)
        {
            if (isOutput)
                RemoveOutputMapping(typeMapping);

            else
                RemoveInputMapping(typeMapping);
        }

        public void EditMapping(TypeMapping typeMapping, bool isOutput)
        {
            if (isOutput)
                EditOutputMapping(typeMapping);

            else
                EditInputMapping(typeMapping);
        }

        private MappingCompiler CreateMappingCompiler(bool isOutput)
        {
            if (isOutput)
                return CreateOutputMappingCompiler();

            else
                return CreateInputMappingCompiler();
        }

        public IEnumerable<TypeMapping> GetDefinedMappings(bool isOutput)
        {
            if (isOutput)
                return GetDefinedOutputMappings();

            else
                return GetDefinedInputMappings();
        }

        public List<TypeMapping> GetMappings(UserDefinedType udt, bool isOutput)
        {
            if (isOutput)
                return GetOutputMappings(udt);

            else
                return GetInputMappings(udt);
        }

        public void ExportMappings(IEnumerable<TypeMapping> list, string file, bool isOutput)
        {
            if (isOutput)
                ExportOutputMappings(list, file);

            else
                ExportInputMappings(list, file);
        }

        public void FixMapping(string filePath, string contents, bool isOutput)
        {
            if (isOutput)
                FixOutputMapping(filePath, contents);

            else
                FixInputMapping(filePath, contents);
        }

        public List<InvalidMappingException> GetMappingCompilerErrors(bool isOutput)
        {
            if (isOutput)
                return GetOutputMappingCompilerErrors();

            else
                return GetInputMappingCompilerErrors();
        }

        public string GetMappingFileDirectory(bool isOutput)
        {
            if (isOutput)
                return GetOutputMappingFileDirectory();

            else
                return GetInputMappingFileDirectory();
        }

        #endregion

        #region [ Shared Mapping Functions ]

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

        public void ImportData(IEnumerable<UserDefinedType> userDefinedTypes, IEnumerable<TypeMapping> inputTypeMappings, IEnumerable<TypeMapping> outputTypeMappings)
        {
            if (userDefinedTypes.Any())
            {

                UDTWriter udtWriter = new UDTWriter();

                foreach (UserDefinedType type in userDefinedTypes)
                    udtWriter.Types.Add(type);

                lock (s_udtLock)
                    udtWriter.WriteFiles(s_udtDirectory);
            }

            if (inputTypeMappings.Any())
            {

                MappingWriter mappingWriter = new MappingWriter();

                foreach (TypeMapping mapping in inputTypeMappings)
                    mappingWriter.Mappings.Add(mapping);

                lock (s_udimLock)
                    mappingWriter.WriteFiles(s_udimDirectory);
            }
            if (outputTypeMappings.Any())
            {

                MappingWriter mappingWriter = new MappingWriter();

                foreach (TypeMapping mapping in outputTypeMappings)
                    mappingWriter.Mappings.Add(mapping);

                lock (s_udomLock)
                    mappingWriter.WriteFiles(s_udomDirectory);
            }

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

        public IEnumerable<TypeMapping> GetDefinedMappings()
        {
            MappingCompiler mappingCompiler = CreateMappingCompiler();
            return mappingCompiler.DefinedMappings;
        }

        #endregion

        #region [ Other Shared Functions ]

        private TypeMapping ParseTimeWindow(TypeMapping typeMapping)
        {
            for (int i = 0; i < typeMapping.FieldMappings.Count; ++i)
            {

                if (typeMapping.FieldMappings[i].TimeWindowExpression != "")
                {
                    try
                    {
                        int index = 0;

                        string[] parts = typeMapping.FieldMappings[i].TimeWindowExpression.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                        if (parts[index].Equals("last", StringComparison.OrdinalIgnoreCase))
                        {
                            ArrayMapping am = new ArrayMapping();
                            am.Field = typeMapping.FieldMappings[i].Field;
                            am.Expression = typeMapping.FieldMappings[i].Expression;
                            am.RelativeTime = typeMapping.FieldMappings[i].RelativeTime;
                            am.RelativeUnit = typeMapping.FieldMappings[i].RelativeUnit;
                            am.SampleRate = typeMapping.FieldMappings[i].SampleRate;
                            am.SampleUnit = typeMapping.FieldMappings[i].SampleUnit;
                            am.TimeWindowExpression = "";

                            am.WindowSize = Convert.ToDecimal(parts[++index]);
                            ++index;

                            if (parts[index].Equals("points", StringComparison.OrdinalIgnoreCase))
                            {
                                index += 2;

                                am.WindowUnit = TimeSpan.Zero;
                                if (parts.Length - 1 > index)
                                {
                                    am.SampleRate = Convert.ToDecimal(parts[index]);
                                    index += 2;
                                    am.SampleUnit = GetTimeSpan(parts[index]);
                                }
                            }
                            else
                            {
                                am.WindowUnit = GetTimeSpan(parts[index]);
                                index += 2;
                                if (parts.Length - 1 > index)
                                {
                                    am.SampleRate = Convert.ToDecimal(parts[index]);
                                    index += 2;
                                    am.SampleUnit = GetTimeSpan(parts[index]);
                                }
                            }

                            typeMapping.FieldMappings.RemoveAt(i);
                            am.TimeWindowExpression = "";
                            typeMapping.FieldMappings.Insert(i, am);

                        }
                        else if (parts[index].Equals("from", StringComparison.OrdinalIgnoreCase))
                        {
                            ArrayMapping am = new ArrayMapping();
                            am.Field = typeMapping.FieldMappings[i].Field;
                            am.Expression = typeMapping.FieldMappings[i].Expression;
                            am.RelativeTime = typeMapping.FieldMappings[i].RelativeTime;
                            am.RelativeUnit = typeMapping.FieldMappings[i].RelativeUnit;
                            am.SampleRate = typeMapping.FieldMappings[i].SampleRate;
                            am.SampleUnit = typeMapping.FieldMappings[i].SampleUnit;
                            am.TimeWindowExpression = "";

                            am.RelativeTime = Convert.ToDecimal(parts[++index]);
                            ++index;

                            if (parts[index].Equals("points", StringComparison.OrdinalIgnoreCase))
                                am.RelativeUnit = TimeSpan.Zero;
                            else
                                am.RelativeUnit = GetTimeSpan(parts[index]);

                            index += 3;

                            am.WindowSize = Convert.ToDecimal(parts[index++]);

                            if (parts[index].Equals("points", StringComparison.OrdinalIgnoreCase))
                                am.WindowUnit = TimeSpan.Zero;
                            else
                                am.WindowUnit = GetTimeSpan(parts[index]);

                            if (parts.Length > ++index)
                            {
                                am.SampleRate = Convert.ToDecimal(parts[++index]);
                                index += 2;
                                am.SampleUnit = GetTimeSpan(parts[index]);
                            }

                            typeMapping.FieldMappings.RemoveAt(i);
                            am.TimeWindowExpression = "";
                            typeMapping.FieldMappings.Insert(i, am);


                        }
                        else
                        {
                            typeMapping.FieldMappings[i].RelativeTime = Convert.ToDecimal(parts[index]);
                            ++index;

                            if (parts[index].Equals("points", StringComparison.OrdinalIgnoreCase))
                            {
                                index += 3;
                                typeMapping.FieldMappings[i].RelativeUnit = TimeSpan.Zero;
                                if (parts.Length > index)
                                {
                                    typeMapping.FieldMappings[i].SampleRate = Convert.ToDecimal(parts[index]);
                                    index += 2;
                                    typeMapping.FieldMappings[i].SampleUnit = GetTimeSpan(parts[index]);
                                }
                            }
                            else
                            {
                                typeMapping.FieldMappings[i].RelativeUnit = GetTimeSpan(parts[index]);
                                index += 3;
                                if (parts.Length - 1 > index)
                                {
                                    typeMapping.FieldMappings[i].SampleRate = Convert.ToDecimal(parts[index]);
                                    index += 2;
                                    typeMapping.FieldMappings[i].SampleUnit = GetTimeSpan(parts[index]);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.LogException(new InvalidOperationException($"Failed to parse time window: {ex.Message}", ex));
                    }
                }
            }

            return typeMapping;
        }

        private TimeSpan GetTimeSpan(string unit)
        {
            switch (unit)
            {
                case "microseconds":
                case "microsecond":
                    return TimeSpan.FromTicks(10);
                case "milliseconds":
                case "millisecond":
                    return new TimeSpan(0, 0, 0, 0, 1);
                case "seconds":
                case "second":
                    return new TimeSpan(0, 0, 0, 1, 0);
                case "minutes":
                case "minute":
                    return new TimeSpan(0, 0, 1, 0, 0);
                case "hours":
                case "hour":
                    return new TimeSpan(0, 1, 0, 0, 0);
                case "days":
                case "day":
                    return new TimeSpan(1, 0, 0, 0, 0);
                default:
                    return TimeSpan.FromTicks(10);
            }

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

        #endregion

        #region [ Create Project ]

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

        public bool CheckProjectName(string projectName, string targetDirectory)
        {
            if (Directory.Exists(targetDirectory))
            {
                return true;
            }
            return false;
        }

        private MappingCompiler CreateMappingCompiler()
        {
            UDTCompiler udtCompiler = CreateUDTCompiler();
            MappingCompiler mappingCompiler = new MappingCompiler(udtCompiler);

            lock (s_udimLock)
            {
                if (Directory.Exists(s_udimDirectory))
                    mappingCompiler.Scan(s_udimDirectory);
            }

            lock (s_udomLock)
            {
                if (Directory.Exists(s_udomDirectory))
                    mappingCompiler.Scan(s_udomDirectory);
            }

            return mappingCompiler;
        }

        #endregion

        #region [ Settings ]

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

            MainWindow.Model.Global.ProjectName = systemSettings["ProjectName"].Value;
            // Save the configuration settings to the application configuration file
            configurationFile.Save();
        }

        #endregion

        #region [ Data Subscription Operations ]

        // These functions are dependent on subscriptions to data where each client connection can customize the subscriptions, so an instance
        // of the DataHubSubscriptionClient is created per SignalR DataHub client connection to manage the subscription life-cycles.

        public IEnumerable<MeasurementValue> GetMeasurements()
        {
            return m_dataSubscriptionOperations.GetMeasurements();
        }

        public IEnumerable<DeviceDetail> GetDeviceDetails()
        {
            return m_dataSubscriptionOperations.GetDeviceDetails();
        }

        public IEnumerable<MeasurementDetail> GetMeasurementDetails()
        {
            return m_dataSubscriptionOperations.GetMeasurementDetails();
        }

        public IEnumerable<PhasorDetail> GetPhasorDetails()
        {
            return m_dataSubscriptionOperations.GetPhasorDetails();
        }

        public IEnumerable<SchemaVersion> GetSchemaVersion()
        {
            return m_dataSubscriptionOperations.GetSchemaVersion();
        }

        public IEnumerable<PowerCalculation> GetPowerCalculation()
        {
            return m_dataSubscriptionOperations.GetPowerCalculation();
        }

        public IEnumerable<MeasurementValue> GetStats()
        {
            return m_dataSubscriptionOperations.GetStats();
        }

        public IEnumerable<StatusLight> GetLights()
        {
            return m_dataSubscriptionOperations.GetLights();
        }

        public void InitializeSubscriptions()
        {
            m_dataSubscriptionOperations.InitializeSubscriptions();
        }

        internal void RegisterMetadataReceivedHandler(Action callback)
        {
            m_dataSubscriptionOperations.RegisterMetadataReceivedHandler(callback);
        }

        public void TerminateSubscriptions()
        {
            m_dataSubscriptionOperations.TerminateSubscriptions();
        }

        public void UpdateFilters(string filterExpression)
        {
            m_dataSubscriptionOperations.UpdateFilters(filterExpression);
        }

        public void StatSubscribe(string filterExpression)
        {
            m_dataSubscriptionOperations.StatSubscribe(filterExpression);
        }

        public void MetaSignalCommand(MetaSignal signal)
        {
            m_dataSubscriptionOperations.MetaSignalCommand(signal);
        }

        public void RefreshMetaData()
        {
            m_dataSubscriptionOperations.RefreshMetaData();
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

