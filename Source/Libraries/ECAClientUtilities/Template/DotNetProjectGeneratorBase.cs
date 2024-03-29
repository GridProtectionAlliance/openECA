﻿//******************************************************************************************************
//  DotNetProjectGeneratorBase.cs - Gbtc
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
//  07/04/2016 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using ECACommonUtilities;
using ECACommonUtilities.Model;
using GSF.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace ECAClientUtilities.Template
{
    public abstract class DotNetProjectGeneratorBase
    {
        #region [ Members ]

        private readonly MappingCompiler m_compiler;
        private readonly string m_arrayMarker;
        private readonly Dictionary<string, string> m_primitiveTypes;

        #endregion

        #region [ Constructors ]

        protected DotNetProjectGeneratorBase(string projectName, MappingCompiler compiler, string fileSuffix, string subFolder, string arrayMarker = "[]")
        {
            ProjectName = projectName;
            m_compiler = compiler;
            Settings = new ProjectSettings();
            FileSuffix = fileSuffix;
            SubFolder = subFolder;
            m_arrayMarker = arrayMarker;

            // ReSharper disable once VirtualMemberCallInConstructor
            m_primitiveTypes = GetPrimitiveTypeMap();
        }

        #endregion

        #region [ Properties ]

        public ProjectSettings Settings { get; }

        public string ProjectName { get; }

        protected string FileSuffix { get; }

        protected string SubFolder { get; }

        #endregion

        #region [ Methods ]

        public void Generate(string projectPath, TypeMapping inputMapping, TypeMapping outputMapping)
        {
            string libraryName = $"{ProjectName}Library";
            string serviceName = $"{ProjectName}Service";
            string testHarnessName = $"{ProjectName}TestHarness";

            string libraryPath = Path.Combine(projectPath, libraryName);
            string servicePath = Path.Combine(projectPath, serviceName);
            string testHarnessPath = Path.Combine(projectPath, testHarnessName);

            HashSet<UserDefinedType> inputTypeReferences = new HashSet<UserDefinedType>();
            HashSet<TypeMapping> inputMappingReferences = new HashSet<TypeMapping>();

            HashSet<UserDefinedType> outputTypeReferences = new HashSet<UserDefinedType>();
            HashSet<TypeMapping> outputMappingReferences = new HashSet<TypeMapping>();

            HashSet<UserDefinedType> allTypeReferences = new HashSet<UserDefinedType>();
            HashSet<TypeMapping> allMappingReferences = new HashSet<TypeMapping>();

            GetReferencedTypesAndMappings(inputMapping, inputTypeReferences, inputMappingReferences);
            GetReferencedTypesAndMappings(outputMapping, outputTypeReferences, outputMappingReferences);

            allTypeReferences.UnionWith(inputTypeReferences);
            allTypeReferences.UnionWith(outputTypeReferences);
            allMappingReferences.UnionWith(inputMappingReferences);
            allMappingReferences.UnionWith(outputMappingReferences);

            CopyTemplateTo(projectPath);
            CopyDependenciesTo(Path.Combine(projectPath, "Dependencies"));

            if (!Directory.Exists(libraryPath))
                libraryPath = Path.Combine(projectPath, ProjectName);

            if (!Directory.Exists(testHarnessPath))
                testHarnessPath = Path.Combine(projectPath, ProjectName);

            WriteModelsTo(Path.Combine(libraryPath, "Model"), allTypeReferences);
            WriteMapperTo(Path.Combine(libraryPath, "Model"), inputMapping.Type, outputMapping.Type, inputTypeReferences);
            WriteUnmapperTo(Path.Combine(libraryPath, "Model"), outputMapping.Type, outputTypeReferences);
            WriteMappingsTo(Path.Combine(libraryPath, "Model"), allTypeReferences, allMappingReferences);
            WriteAlgorithmTo(libraryPath, inputMapping, outputMapping);
            WriteFrameworkFactoryTo(libraryPath);
            WriteProgramTo(testHarnessPath, projectPath, inputTypeReferences, outputMapping.Type);
            WriteAlgorithmHostingEnvironmentTo(servicePath);
            UpdateProjectFiles(projectPath, GetReferencedTypes(inputMapping.Type, outputMapping.Type));
            UpdateSetupScriptFile(projectPath);
        }

        public void RefreshMappings(string projectPath, TypeMapping inputMapping, TypeMapping outputMapping)
        {
            HashSet<UserDefinedType> userDefinedTypes = new HashSet<UserDefinedType>();
            HashSet<TypeMapping> userDefinedMappings = new HashSet<TypeMapping>();
            GetReferencedTypesAndMappings(inputMapping, userDefinedTypes, userDefinedMappings);
            GetReferencedTypesAndMappings(outputMapping, userDefinedTypes, userDefinedMappings);
            WriteMappingsTo(Path.Combine(projectPath, ProjectName, "Model"), userDefinedTypes, userDefinedMappings);
        }

        protected List<UserDefinedType> GetReferencedTypes(params UserDefinedType[] sourceTypes)
        {
            List<UserDefinedType> orderedTypes = new List<UserDefinedType>();
            HashSet<UserDefinedType> typeSet = new HashSet<UserDefinedType>();
            Action<UserDefinedType> buildTypes = null;

            buildTypes = type =>
            {
                // If the type has already been enumerated,
                // do not add it to the list
                if (!typeSet.Add(type))
                    return;

                // Get a collection of all fields of the source type where the field's
                // type is either a user-defined type or an array of user-defined types
                IEnumerable<UserDefinedType> referencedTypes = type.Fields
                    .Select(field => (field.Type as ArrayType)?.UnderlyingType ?? field.Type)
                    .OfType<UserDefinedType>();

                // Recursively search all fields that reference user-defined types
                foreach (UserDefinedType referencedType in referencedTypes)
                    buildTypes(referencedType);

                // Add the type of the source mapping to the collection of referenced types
                orderedTypes.Add(type);
            };

            foreach (UserDefinedType type in sourceTypes)
                buildTypes(type);

            return orderedTypes;
        }

        // Recursively traverses all referenced types and mappings from the source mapping and stores them in the given hash sets.
        protected void GetReferencedTypesAndMappings(TypeMapping sourceMapping, HashSet<UserDefinedType> referencedTypes, HashSet<TypeMapping> referencedMappings)
        {
            // Add the type of the source mapping to the collection of referenced types
            referencedTypes.Add(sourceMapping.Type);

            // Add the source mapping to the collection of referenced
            // mappings and quit if we've already done so before
            if (!referencedMappings.Add(sourceMapping))
                return;

            // Get a collection of all field mappings of the source mapping where the
            // field's type is either a user-defined type or an array of user-defined types
            IEnumerable<FieldMapping> udtFields = sourceMapping.FieldMappings
                .Where(fieldMapping =>
                {
                    DataType fieldType = fieldMapping.Field.Type;
                    DataType underlyingType = (fieldType as ArrayType)?.UnderlyingType ?? fieldType;
                    return underlyingType.IsUserDefined;
                });

            // Recursively search all fields that reference user-defined types
            foreach (FieldMapping fieldMapping in udtFields)
            {
                foreach (TypeMapping typeMapping in m_compiler.EnumerateTypeMappings(fieldMapping.Expression))
                    GetReferencedTypesAndMappings(typeMapping, referencedTypes, referencedMappings);
            }
        }

        // Copies the template project to the given path.
        protected void CopyTemplateTo(string path)
        {
            string templateDirectory = FilePath.GetAbsolutePath($@"Templates\{SubFolder}");

            // Establish the directory structure of the
            // template project at the destination path
            Directory.CreateDirectory(path);

            foreach (string directory in Directory.EnumerateDirectories(templateDirectory, "*", SearchOption.AllDirectories))
            {
                // Determine the full path to the destination directory
                string destination = directory
                    .Replace(templateDirectory, path)
                    .Replace("AlgorithmTemplate", ProjectName);

                // Create the destination directory
                Directory.CreateDirectory(destination);
            }

            // Recursively copy all files from the template project
            // to the destination path, but only if they do not
            // already exist in the destination path
            foreach (string file in Directory.EnumerateFiles(templateDirectory, "*", SearchOption.AllDirectories))
            {
                // Determine the full path to the destination file
                string destination = file
                    .Replace(templateDirectory, path)
                    .Replace("AlgorithmTemplate", ProjectName);

                // If the file already exists
                // at the destination, skip it
                if (File.Exists(destination))
                    continue;

                // Copy the file to its destination
                File.Copy(file, destination);

                // After copying the file, fix the contents first to rename
                // AlgorithmTemplate to the name of the project we are generating,
                // then to fix the GSF dependency paths
                string text = File.ReadAllText(destination);
                string replacement = text.Replace("AlgorithmTemplate", ProjectName);

                if (text != replacement)
                    File.WriteAllText(destination, replacement);
            }
        }

        // Copies the necessary GSF dependencies to the given path.
        protected void CopyDependenciesTo(string path)
        {
            string[] gsfDependencies =
            {
                "GSF.Core.dll",
                "GSF.Communication.dll",
                "GSF.Security.dll",
                "GSF.ServiceProcess.dll",
                "GSF.TimeSeries.dll",
                "Antlr3.Runtime.dll",
                "ExpressionEvaluator.dll"
            };

            string[] ecaDependencies =
            {
                "ECAClientFramework.dll",
                "ECAClientUtilities.dll",
                "ECACommonUtilities.dll"
            };

            string[] microsoftDependencies =
            {
                "FSharp.Core.dll"
            };

            // Create the directory at the destination path
            Directory.CreateDirectory(Path.Combine(path, "GSF"));
            Directory.CreateDirectory(Path.Combine(path, "openECA"));
            Directory.CreateDirectory(Path.Combine(path, "Microsoft"));

            // Copy each of the necessary assemblies to the destination directory
            foreach (string dependency in gsfDependencies)
                File.Copy(FilePath.GetAbsolutePath(dependency), Path.Combine(path, "GSF", dependency), true);

            foreach (string dependency in ecaDependencies)
                File.Copy(FilePath.GetAbsolutePath(dependency), Path.Combine(path, "openECA", dependency), true);

            foreach (string dependency in microsoftDependencies)
            {
                string filePath = FilePath.GetAbsolutePath(dependency);

                if (File.Exists(filePath))
                    File.Copy(filePath, Path.Combine(path, "Microsoft", dependency), true);
            }
        }

        // Generates classes for the all the models used by the input and output types.
        protected void WriteModelsTo(string path, IEnumerable<UserDefinedType> allTypeReferences)
        {
            // Clear out all existing models
            // so they can be regenerated
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            foreach (UserDefinedType type in allTypeReferences)
            {
                // Determine the path to the directory and class file to be generated
                string categoryDirectory = Path.Combine(path, type.Category);
                string filePath = Path.Combine(categoryDirectory, $"{type.Identifier}.{FileSuffix}");

                // Create the directory if it doesn't already exist
                Directory.CreateDirectory(categoryDirectory);

                // Create the file for the data class being generated
                using (TextWriter writer = File.CreateText(filePath))
                {
                    // Write the constructed model contents to the class file
                    writer.Write(ConstructDataModel(type));
                }

                filePath = Path.Combine(categoryDirectory, $"{GetMetaIdentifier(type.Identifier)}.{FileSuffix}");

                // Create the file for the meta class being generated
                using (TextWriter writer = File.CreateText(filePath))
                {
                    // Write the constructed model contents to the class file
                    writer.Write(ConstructMetaModel(type));
                }
            }
        }

        protected abstract string ConstructDataModel(UserDefinedType type);

        protected abstract string ConstructMetaModel(UserDefinedType type);

        // Generates the class that maps measurements to objects of the input and output types.
        protected void WriteMapperTo(string path, UserDefinedType inputType, UserDefinedType outputType, IEnumerable<UserDefinedType> inputTypeReferences)
        {
            // Determine the path to the mapper class file
            string mapperPath = Path.Combine(path, $"Mapper.{FileSuffix}");

            // Grab strings used for replacement in the mapper class template
            string inputCategoryIdentifier = inputType.Category;
            string inputDataTypeName = GetDataTypeName(inputType);
            string inputDataTypeIdentifier = inputType.Identifier;
            string inputMetaTypeName = GetMetaTypeName(inputType);
            string inputMetaTypeIdentifier = GetMetaIdentifier(inputType.Identifier);
            string outputDataTypeName = GetDataTypeName(outputType);
            string outputMetaTypeName = GetMetaTypeName(outputType);

            // Create string builders for code generation
            StringBuilder mappingFunctions = new StringBuilder();

            string mappingFunctionTemplate = GetTextFromResource($"ECAClientUtilities.Template.{SubFolder}.MappingFunctionTemplate.txt");

            // Generate a method for each data type of the input mappings in
            // order to map measurement values to the fields of the data types
            foreach (UserDefinedType type in inputTypeReferences)
            {
                // Grab strings used for replacement
                // in the mapping function template
                string categoryIdentifier = type.Category;
                string typeIdentifier = type.Identifier;

                // Write the content of the mapping function to the string builder containing mapping function code
                mappingFunctions.AppendLine(mappingFunctionTemplate
                    .Replace("{TypeName}", GetDataTypeName(type))
                    .Replace("{CategoryIdentifier}", categoryIdentifier)
                    .Replace("{TypeIdentifier}", typeIdentifier)
                    .Replace("{TypeUsing}", ConstructUsing(type))
                    .Replace("{MappingCode}", ConstructMapping(type, false).Trim()));

                mappingFunctions.AppendLine(mappingFunctionTemplate
                    .Replace("{TypeName}", GetMetaTypeName(type))
                    .Replace("{CategoryIdentifier}", categoryIdentifier)
                    .Replace("{TypeIdentifier}", GetMetaIdentifier(typeIdentifier))
                    .Replace("{TypeUsing}", ConstructUsing(type))
                    .Replace("{MappingCode}", ConstructMapping(type, true).Trim()));
            }

            // Write the content of the mapper class file to the target location
            File.WriteAllText(mapperPath, GetTextFromResource($"ECAClientUtilities.Template.{SubFolder}.MapperTemplate.txt")
                .Replace("{ProjectName}", ProjectName)
                .Replace("{InputCategoryIdentifier}", inputCategoryIdentifier)
                .Replace("{InputDataTypeName}", inputDataTypeName)
                .Replace("{InputDataTypeIdentifier}", inputDataTypeIdentifier)
                .Replace("{InputMetaTypeName}", inputMetaTypeName)
                .Replace("{InputMetaTypeIdentifier}", inputMetaTypeIdentifier)
                .Replace("{OutputDataTypeName}", outputDataTypeName)
                .Replace("{OutputMetaTypeName}", outputMetaTypeName)
                .Replace("{MappingFunctions}", mappingFunctions.ToString().Trim()));
        }

        protected abstract string ConstructMapping(UserDefinedType type, bool isMetaType);

        // Generates the class that maps measurements to objects of the input and output types.
        protected void WriteUnmapperTo(string path, UserDefinedType outputType, IEnumerable<UserDefinedType> outputTypeReferences)
        {
            // Determine the path to the unmapper class file
            string mapperPath = Path.Combine(path, $"Unmapper.{FileSuffix}");

            // Grab strings used for replacement in the mapper class template
            string outputCategoryIdentifier = outputType.Category;
            string outputDataTypeIdentifier = outputType.Identifier;
            string outputMetaTypeIdentifier = GetMetaIdentifier(outputDataTypeIdentifier);
            string outputDataTypeName = GetDataTypeName(outputType);
            string outputMetaTypeName = GetMetaTypeName(outputType);

            // Create string builders for code generation
            StringBuilder fillFunctions = new StringBuilder();
            StringBuilder unmappingFunctions = new StringBuilder();

            string fillFunctionTemplate = GetTextFromResource($"ECAClientUtilities.Template.{SubFolder}.FillFunctionTemplate.txt");
            string unmappingFunctionTemplate = GetTextFromResource($"ECAClientUtilities.Template.{SubFolder}.UnmappingFunctionTemplate.txt");

            // Generate three methods for each data type of the output mappings in
            // order to initialize the output data and meta objects and to map
            // fields of the data types to measurement values
            foreach (UserDefinedType type in outputTypeReferences)
            {
                // Grab strings used for replacement
                // in the function templates
                string categoryIdentifier = type.Category;
                string dataTypeIdentifier = type.Identifier;
                string dataTypeName = GetDataTypeName(type);
                string metaTypeIdentifier = GetMetaIdentifier(dataTypeIdentifier);
                string metaTypeName = GetMetaTypeName(type);

                // Write the content of the fill functions to the string builder containing fill function code
                fillFunctions.AppendLine(fillFunctionTemplate
                    .Replace("{TypeName}", dataTypeName)
                    .Replace("{CategoryIdentifier}", categoryIdentifier)
                    .Replace("{TypeIdentifier}", dataTypeIdentifier)
                    .Replace("{TypeUsing}", ConstructUsing(type))
                    .Replace("{FillCode}", ConstructFillFunction(type, false).Trim()));

                fillFunctions.AppendLine(fillFunctionTemplate
                    .Replace("{TypeName}", metaTypeName)
                    .Replace("{CategoryIdentifier}", categoryIdentifier)
                    .Replace("{TypeIdentifier}", metaTypeIdentifier)
                    .Replace("{TypeUsing}", ConstructUsing(type))
                    .Replace("{FillCode}", ConstructFillFunction(type, true).Trim()));

                // Write the content of the unmapping function to the string builder containing unmapping function code
                unmappingFunctions.AppendLine(unmappingFunctionTemplate
                    .Replace("{DataTypeName}", dataTypeName)
                    .Replace("{MetaTypeName}", metaTypeName)
                    .Replace("{CategoryIdentifier}", categoryIdentifier)
                    .Replace("{TypeIdentifier}", dataTypeIdentifier)
                    .Replace("{UnmappingCode}", ConstructUnmapping(type).Trim()));
            }

            // Write the content of the mapper class file to the target location
            File.WriteAllText(mapperPath, GetTextFromResource($"ECAClientUtilities.Template.{SubFolder}.UnmapperTemplate.txt")
                .Replace("{ProjectName}", ProjectName)
                .Replace("{OutputCategoryIdentifier}", outputCategoryIdentifier)
                .Replace("{OutputDataTypeIdentifier}", outputDataTypeIdentifier)
                .Replace("{OutputMetaTypeIdentifier}", outputMetaTypeIdentifier)
                .Replace("{OutputDataTypeName}", outputDataTypeName)
                .Replace("{OutputMetaTypeName}", outputMetaTypeName)
                .Replace("{FillFunctions}", fillFunctions.ToString().Trim())
                .Replace("{UnmappingFunctions}", unmappingFunctions.ToString().Trim()));
        }

        protected abstract string ConstructFillFunction(UserDefinedType type, bool isMetaType);

        protected abstract string ConstructUnmapping(UserDefinedType type);

        // Writes the UDT and mapping files to the specified path, containing the specified types and mappings.
        protected void WriteMappingsTo(string path, IEnumerable<UserDefinedType> userDefinedTypes, IEnumerable<TypeMapping> userDefinedMappings)
        {
            // Determine the paths to the UDT and mapping files
            string udtFilePath = Path.Combine(path, "UserDefinedTypes.ecaidl");
            string mappingFilePath = Path.Combine(path, "UserDefinedMappings.ecamap");

            // Create the writers to generate the files
            UDTWriter udtWriter = new UDTWriter();
            MappingWriter mappingWriter = new MappingWriter();

            // Add the UDTs and mappings to the writers
            udtWriter.Types.AddRange(userDefinedTypes);
            mappingWriter.Mappings.AddRange(userDefinedMappings);

            // Generate the files
            udtWriter.Write(udtFilePath);
            mappingWriter.Write(mappingFilePath);
        }

        // Writes the file that contains the user's algorithm to the given path.
        protected void WriteAlgorithmTo(string path, TypeMapping inputMapping, TypeMapping outputMapping)
        {
            UserDefinedType inputType = inputMapping.Type;
            UserDefinedType outputType = outputMapping.Type;

            // Determine the path to the file containing the user's algorithm
            string algorithmPath = Path.Combine(path, $"Algorithm.{FileSuffix}");

            // Do not overwrite the user's algorithm
            if (File.Exists(algorithmPath))
                return;

            // Generate usings for the namespaces of the classes the user needs for their inputs and outputs
            string usings = string.Join(Environment.NewLine, new[] { inputType, outputType }
                .Select(ConstructUsing)
                .Distinct()
                .OrderBy(str => str));

            // Write the contents of the user's algorithm class to the class file
            File.WriteAllText(algorithmPath, GetTextFromResource($"ECAClientUtilities.Template.{SubFolder}.AlgorithmTemplate.txt")
                .Replace("{Usings}", usings)
                .Replace("{OutputUsing}", ConstructUsing(outputType))
                .Replace("{ProjectName}", ProjectName)
                .Replace("{ConnectionString}", $"\"{Settings.SubscriberConnectionString.Replace("\"", "\"\"")}\"")
                .Replace("{ConnectionStringSingleQuote}", $"\'{Settings.SubscriberConnectionString.Replace("'", "''''")}\'")
                .Replace("{InputMapping}", inputMapping.Identifier)
                .Replace("{InputDataType}", inputType.Identifier)
                .Replace("{InputMetaType}", GetMetaIdentifier(inputType.Identifier))
                .Replace("{OutputMapping}", outputMapping.Identifier)
                .Replace("{OutputDataType}", outputType.Identifier)
                .Replace("{OutputMetaType}", GetMetaIdentifier(outputType.Identifier)));
        }

        protected abstract string ConstructUsing(UserDefinedType type);

        // Writes the file that contains the user's algorithm to the given path.
        protected void WriteFrameworkFactoryTo(string path)
        {
            // Determine the path to the file containing the user's algorithm
            string frameworkFactoryPath = Path.Combine(path, $"FrameworkFactory.{FileSuffix}");

            // Do not overwrite the user's algorithm
            if (File.Exists(frameworkFactoryPath))
                return;

            // Write the contents of the user's algorithm class to the class file
            File.WriteAllText(frameworkFactoryPath, GetTextFromResource($"ECAClientUtilities.Template.{SubFolder}.FrameworkFactoryTemplate.txt")
                .Replace("{ProjectName}", ProjectName));
        }

        // Writes the file that contains the program startup code to the given path.
        protected void WriteProgramTo(string path, string projectpath, IEnumerable<UserDefinedType> inputTypeReferences, UserDefinedType outputMappingType)
        {
            // Determine the path to the file containing the program startup code
            string programPath = Path.Combine(path, "Program.cs");

            if (!File.Exists(programPath))
                programPath = Path.Combine(path, $"Program.{FileSuffix}");

            // Generate usings for the namespaces of the classes the user needs for their inputs and outputs
            string usings = string.Join(Environment.NewLine, inputTypeReferences.Concat(new[] { outputMappingType })
                .Select(ConstructUsing)
                .Distinct()
                .OrderBy(str => str));

            // Write the contents of the program startup template to the class file
            File.WriteAllText(programPath, GetTextFromResource($"ECAClientUtilities.Template.{SubFolder}.ProgramTemplate.txt")
                .Replace("{Usings}", usings)
                .Replace("{ProjectPath}", FilePath.AddPathSuffix(projectpath))
                .Replace("{ProjectName}", ProjectName));
        }

        protected void WriteAlgorithmHostingEnvironmentTo(string path)
        {
            // Write the contents of the algorithm hosting environment template to the class file
            File.WriteAllText(Path.Combine(path, "AlgorithmHostingEnvironment.cs"), GetTextFromResource($"ECAClientUtilities.Template.{SubFolder}.AlgorithmHostingEnvironment.txt")
                .Replace("{ProjectName}", ProjectName));
        }

        protected virtual string[] ExtraModelCategoryFiles(string modelPath, string categoryName)
        {
            return new string[0];
        }

        // Updates the project files to reference proper dependencies and include new generated classes.
        protected virtual void UpdateProjectFiles(string projectPath, List<UserDefinedType> orderedInputTypes)
        {
            UpdateLibraryProjectFile(projectPath, orderedInputTypes);
            UpdateServiceProjectFile(projectPath);
            UpdateServiceConsoleProjectFile(projectPath);
            UpdateTestHarnessProjectFile(projectPath);
        }

        protected virtual void UpdateLibraryProjectFile(string projectPath, List<UserDefinedType> orderedInputTypes)
        {
            // Determine the path to the project file and the generated models
            string libraryName = $"{ProjectName}Library";
            string libraryPath = Path.Combine(projectPath, libraryName);
            string libraryProjectPath = Path.Combine(libraryPath, $"{libraryName}.{FileSuffix}proj");

            if (!File.Exists(libraryProjectPath))
            {
                libraryPath = Path.Combine(projectPath, ProjectName);
                libraryProjectPath = Path.Combine(libraryPath, ProjectName + $".{FileSuffix}proj");
            }

            // Load the library project file as an XML file
            XDocument document = XDocument.Load(libraryProjectPath);
            XNamespace xmlNamespace = document.Root?.GetDefaultNamespace() ?? XNamespace.None;

            Func<XElement, bool> isRefreshedReference = element =>
                (element.Attribute("Include")?.Value.StartsWith(@"Model\") ?? false) ||
                (string)element.Attribute("Include") == $"Algorithm.{FileSuffix}" ||
                (string)element.Attribute("Include") == $"FrameworkFactory.{FileSuffix}" ||
                (string)element.Attribute("Include") == "GSF.Communication, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" ||
                (string)element.Attribute("Include") == "GSF.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" ||
                (string)element.Attribute("Include") == "GSF.TimeSeries, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" ||
                (string)element.Attribute("Include") == "ExpressionEvaluator, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" ||
                (string)element.Attribute("Include") == "ECAClientFramework, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" ||
                (string)element.Attribute("Include") == "ECAClientUtilities, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" ||
                (string)element.Attribute("Include") == "ECACommonUtilities, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

            // Remove elements referencing files that need to be refreshed
            document
                .Descendants()
                .Where(isRefreshedReference)
                .ToList()
                .ForEach(element => element.Remove());

            // Locate the item group that contains <Compile> child elements
            XElement itemGroup = document.Descendants(xmlNamespace + "ItemGroup")
                                     .FirstOrDefault(element => !element.Elements().Any()) ?? new XElement(xmlNamespace + "ItemGroup");

            // If the ItemGroup element was just created,
            // add it to the root of the document
            if ((object)itemGroup.Parent == null)
                document.Root?.Add(itemGroup);

            // Add references to every item in the Model directory
            string path;
            string modelPath = Path.Combine(libraryPath, "Model");
            XAttribute includeAttribute;
            XElement copyToOutputDirectoryElement;
            string lastCategory = "";

            // Add data and meta models
            foreach (UserDefinedType type in orderedInputTypes)
            {
                if (!lastCategory.Equals(type.Category))
                {
                    lastCategory = type.Category;

                    foreach (string extraProjectLevelFile in ExtraModelCategoryFiles(modelPath, type.Category))
                    {
                        path = $@"Model\{extraProjectLevelFile}";
                        includeAttribute = new XAttribute("Include", path);
                        itemGroup.Add(new XElement(xmlNamespace + "Compile", includeAttribute));
                    }
                }

                path = $@"Model\{type.Category}\{type.Identifier}.{FileSuffix}";
                includeAttribute = new XAttribute("Include", path);
                itemGroup.Add(new XElement(xmlNamespace + "Compile", includeAttribute));

                path = $@"Model\{type.Category}\{GetMetaIdentifier(type.Identifier)}.{FileSuffix}";
                includeAttribute = new XAttribute("Include", path);
                itemGroup.Add(new XElement(xmlNamespace + "Compile", includeAttribute));
            }

            foreach (string extraProjectLevelFile in ExtraModelCategoryFiles(modelPath, null))
            {
                path = $@"Model\{extraProjectLevelFile}";
                includeAttribute = new XAttribute("Include", path);
                itemGroup.Add(new XElement(xmlNamespace + "Compile", includeAttribute));
            }

            path = $@"Model\Unmapper.{FileSuffix}";
            includeAttribute = new XAttribute("Include", path);
            itemGroup.Add(new XElement(xmlNamespace + "Compile", includeAttribute));

            path = $@"Model\Mapper.{FileSuffix}";
            includeAttribute = new XAttribute("Include", path);
            itemGroup.Add(new XElement(xmlNamespace + "Compile", includeAttribute));

            path = @"Model\UserDefinedTypes.ecaidl";
            includeAttribute = new XAttribute("Include", path);
            copyToOutputDirectoryElement = new XElement(xmlNamespace + "CopyToOutputDirectory", "PreserveNewest");
            itemGroup.Add(new XElement(xmlNamespace + "Content", includeAttribute, copyToOutputDirectoryElement));

            path = @"Model\UserDefinedMappings.ecamap";
            includeAttribute = new XAttribute("Include", path);
            copyToOutputDirectoryElement = new XElement(xmlNamespace + "CopyToOutputDirectory", "PreserveNewest");
            itemGroup.Add(new XElement(xmlNamespace + "Content", includeAttribute, copyToOutputDirectoryElement));

            // Add a reference to the user algorithm
            itemGroup.Add(new XElement(xmlNamespace + "Compile", new XAttribute("Include", $"Algorithm.{FileSuffix}")));

            // Add a reference to the framework factory
            itemGroup.Add(new XElement(xmlNamespace + "Compile", new XAttribute("Include", $"FrameworkFactory.{FileSuffix}")));

            // Add a reference to GSF.Communication.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "GSF.Communication, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\GSF\GSF.Communication.dll")));

            // Add a reference to GSF.Core.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "GSF.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\GSF\GSF.Core.dll")));

            // Add a reference to GSF.TimeSeries.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "GSF.TimeSeries, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\GSF\GSF.TimeSeries.dll")));

            // Add a reference to ExpressionEvaluator.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "ExpressionEvaluator, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\GSF\ExpressionEvaluator.dll")));

            // Add a reference to ECAClientFramework.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "ECAClientFramework, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\openECA\ECAClientFramework.dll")));

            // Add a reference to ECAClientUtilities.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "ECAClientUtilities, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\openECA\ECAClientUtilities.dll")));

            // Add a reference to ECACommonUtilities.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "ECACommonUtilities, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\openECA\ECACommonUtilities.dll")));

            // Save changes to the project file
            document.Save(libraryProjectPath);
        }

        protected virtual void UpdateServiceProjectFile(string projectPath)
        {
            // Determine the path to the project file
            string serviceName = $"{ProjectName}Service";
            string servicePath = Path.Combine(projectPath, serviceName);
            string serviceProjectPath = Path.Combine(servicePath, $"{serviceName}.csproj");

            // Load the service project file as an XML file
            XDocument document = XDocument.Load(serviceProjectPath);
            XNamespace xmlNamespace = document.Root?.GetDefaultNamespace() ?? XNamespace.None;

            Func<XElement, bool> isRefreshedReference = element =>
                (string)element.Attribute("Include") == "AlgorithmHostingEnvironment.cs" ||
                (((string)element.Attribute("Include"))?.StartsWith("GSF.Core") ?? false) ||
                (((string)element.Attribute("Include"))?.StartsWith("GSF.Communication") ?? false) ||
                (((string)element.Attribute("Include"))?.StartsWith("GSF.Security") ?? false) ||
                (((string)element.Attribute("Include"))?.StartsWith("GSF.ServiceProcess") ?? false) ||
                (((string)element.Attribute("Include"))?.StartsWith("GSF.TimeSeries") ?? false) ||
                (((string)element.Attribute("Include"))?.StartsWith("ECAClientFramework") ?? false) ||
                (((string)element.Attribute("Include"))?.StartsWith("ECAClientUtilities") ?? false);

            // Remove elements referencing files that need to be refreshed
            document
                .Descendants()
                .Where(isRefreshedReference)
                .ToList()
                .ForEach(element => element.Remove());

            // Locate the item group that contains <Compile> child elements
            XElement itemGroup = document.Descendants(xmlNamespace + "ItemGroup")
                                     .FirstOrDefault(element => !element.Elements().Any()) ?? new XElement(xmlNamespace + "ItemGroup");

            // If the ItemGroup element was just created,
            // add it to the root of the document
            if ((object)itemGroup.Parent == null)
                document.Root?.Add(itemGroup);

            // Add a reference to the algorithm hosting environment code
            itemGroup.Add(new XElement(xmlNamespace + "Compile", new XAttribute("Include", "AlgorithmHostingEnvironment.cs")));

            // Add a reference to GSF.Core.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "GSF.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\GSF\GSF.Core.dll")));

            // Add a reference to GSF.Communication.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "GSF.Communication, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\GSF\GSF.Communication.dll")));

            // Add a reference to GSF.Security.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "GSF.Security, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\GSF\GSF.Security.dll")));

            // Add a reference to GSF.ServiceProcess.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "GSF.ServiceProcess, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\GSF\GSF.ServiceProcess.dll")));

            // Add a reference to GSF.TimeSeries.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "GSF.TimeSeries, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\GSF\GSF.TimeSeries.dll")));

            // Add a reference to ECAClientFramework.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "ECAClientFramework, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\openECA\ECAClientFramework.dll")));

            // Add a reference to ECAClientUtilities.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "ECAClientUtilities, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\openECA\ECAClientUtilities.dll")));

            // Save changes to the project file
            document.Save(serviceProjectPath);

            // Attempt to give service host a unique console port
            int remoteConsolePort = DeriveRemoteConsolePort();

            // Update designer code for service host which contains replaceable token
            string serviceHostDesignerPath = Path.Combine(servicePath, "ServiceHost.Designer.cs");
            string serviceHostDesignerCode = File.ReadAllText(serviceHostDesignerPath);

            File.WriteAllText(serviceHostDesignerPath, serviceHostDesignerCode
                .Replace("[REMOTE_CONSOLE_PORT]", $"{remoteConsolePort}"));
        }

        protected virtual void UpdateServiceConsoleProjectFile(string projectPath)
        {
            // Determine the path to the project file
            string serviceConsoleName = $"{ProjectName}ServiceConsole";
            string serviceConsolePath = Path.Combine(projectPath, serviceConsoleName);
            string serviceConsoleProjectPath = Path.Combine(serviceConsolePath, $"{serviceConsoleName}.csproj");

            // Load the service project file as an XML file
            XDocument document = XDocument.Load(serviceConsoleProjectPath);
            XNamespace xmlNamespace = document.Root?.GetDefaultNamespace() ?? XNamespace.None;

            Func<XElement, bool> isRefreshedReference = element =>
                (((string)element.Attribute("Include"))?.StartsWith("GSF.Core") ?? false) ||
                (((string)element.Attribute("Include"))?.StartsWith("GSF.Communication") ?? false) ||
                (((string)element.Attribute("Include"))?.StartsWith("GSF.Security") ?? false) ||
                (((string)element.Attribute("Include"))?.StartsWith("GSF.ServiceProcess") ?? false);

            // Remove elements referencing files that need to be refreshed
            document
                .Descendants()
                .Where(isRefreshedReference)
                .ToList()
                .ForEach(element => element.Remove());

            // Locate the item group that contains <Compile> child elements
            XElement itemGroup = document.Descendants(xmlNamespace + "ItemGroup")
                                     .FirstOrDefault(element => !element.Elements().Any()) ?? new XElement(xmlNamespace + "ItemGroup");

            // If the ItemGroup element was just created,
            // add it to the root of the document
            if ((object)itemGroup.Parent == null)
                document.Root?.Add(itemGroup);

            // Add a reference to GSF.Core.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "GSF.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\GSF\GSF.Core.dll")));

            // Add a reference to GSF.Communication.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "GSF.Communication, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\GSF\GSF.Communication.dll")));

            // Add a reference to GSF.Security.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "GSF.Security, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\GSF\GSF.Security.dll")));

            // Add a reference to GSF.ServiceProcess.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "GSF.ServiceProcess, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\GSF\GSF.ServiceProcess.dll")));

            // Save changes to the project file
            document.Save(serviceConsoleProjectPath);

            // Update service client files with derived service host port
            int remoteConsolePort = DeriveRemoteConsolePort();

            // Update designer code for service console which contains replaceable token
            string serviceClientDesignerPath = Path.Combine(serviceConsolePath, "ServiceClient.Designer.cs");
            string serviceClientDesignerCode = File.ReadAllText(serviceClientDesignerPath);

            File.WriteAllText(serviceClientDesignerPath, serviceClientDesignerCode
                .Replace("[REMOTE_CONSOLE_PORT]", $"{remoteConsolePort}"));

            // Update service console config which contains replaceable token
            string serviceConsoleConfigPath = Path.Combine(serviceConsolePath, "App.config");
            string serviceConsoleConfigXML = File.ReadAllText(serviceConsoleConfigPath);

            File.WriteAllText(serviceConsoleConfigPath, serviceConsoleConfigXML
                .Replace("[REMOTE_CONSOLE_PORT]", $"{remoteConsolePort}"));
        }

        protected virtual void UpdateTestHarnessProjectFile(string projectPath)
        {
            // Determine the path to the project file
            string testHarnessName = $"{ProjectName}TestHarness";
            string testHarnessPath = Path.Combine(projectPath, testHarnessName);
            string testHarnessProjectPath = Path.Combine(testHarnessPath, $"{testHarnessName}.csproj");

            if (!File.Exists(testHarnessProjectPath))
            {
                testHarnessPath = Path.Combine(projectPath, ProjectName);
                testHarnessProjectPath = Path.Combine(testHarnessPath, ProjectName + $".{FileSuffix}proj");
            }

            // Load the test harness project file as an XML file
            XDocument document = XDocument.Load(testHarnessProjectPath);
            XNamespace xmlNamespace = document.Root?.GetDefaultNamespace() ?? XNamespace.None;

            Func<XElement, bool> isRefreshedReference = element =>
                (string)element.Attribute("Include") == "Program.cs" ||
                (string)element.Attribute("Include") == $"Program.{FileSuffix}" ||
                (string)element.Attribute("Include") == "ECAClientFramework, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" ||
                (string)element.Attribute("Include") == "ECAClientUtilities, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

            // Remove elements referencing files that need to be refreshed
            document
                .Descendants()
                .Where(isRefreshedReference)
                .ToList()
                .ForEach(element => element.Remove());

            // Locate the item group that contains <Compile> child elements
            XElement itemGroup = document.Descendants(xmlNamespace + "ItemGroup")
                                     .FirstOrDefault(element => !element.Elements().Any()) ?? new XElement(xmlNamespace + "ItemGroup");

            // If the ItemGroup element was just created,
            // add it to the root of the document
            if ((object)itemGroup.Parent == null)
                document.Root?.Add(itemGroup);

            // Add a reference to the program startup code
            if (File.Exists(Path.Combine(testHarnessPath, "Program.cs")))
                itemGroup.Add(new XElement(xmlNamespace + "Compile", new XAttribute("Include", "Program.cs")));
            else
                itemGroup.Add(new XElement(xmlNamespace + "Compile", new XAttribute("Include", $"Program.{FileSuffix}")));

            // Add a reference to ECAClientFramework.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "ECAClientFramework, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\openECA\ECAClientFramework.dll")));

            // Add a reference to ECAClientUtilities.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "ECAClientUtilities, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\openECA\ECAClientUtilities.dll")));

            // Save changes to the project file
            document.Save(testHarnessProjectPath);
        }

        protected virtual void UpdateSetupScriptFile(string projectPath)
        {
            // Determine the path to the project file
            string setupName = $"{ProjectName}Setup";
            string setupPath = Path.Combine(projectPath, setupName);
            string setupScriptPath = Path.Combine(setupPath, $"{setupName}.wxs");
            string setupScript = File.ReadAllText(setupScriptPath);

            // Make sure setup script contains unique product ID and upgrade code
            File.WriteAllText(setupScriptPath, setupScript
                .Replace("[PRODUCT_UPGRADE_CODE]", $"{Guid.NewGuid()}"));
        }

        protected virtual int DeriveRemoteConsolePort()
        {
            return 10000 + Math.Abs(ProjectName.GetHashCode()) % short.MaxValue;
        }

        // Converts an embedded resource to a string.
        protected string GetTextFromResource(string resourceName)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

            if ((object)stream == null)
                return string.Empty;

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
            {
                return reader.ReadToEnd();
            }
        }

        // Converts the given data type to string representing the corresponding data class type.
        protected string GetDataTypeName(DataType type)
        {
            DataType underlyingType = (type as ArrayType)?.UnderlyingType ?? type;

            // Namespace: ProjectName.Model.Category
            // TypeName: Identifier
            if (!m_primitiveTypes.TryGetValue($"{underlyingType.Category}.{underlyingType.Identifier}", out string typeName))
                typeName = $"{ProjectName}.Model.{underlyingType.Category}.{underlyingType.Identifier}";

            if (type.IsArray)
                typeName += m_arrayMarker;

            return typeName;
        }

        // Converts the given data type to string representing the corresponding meta class type.
        protected string GetMetaTypeName(DataType type)
        {
            DataType underlyingType = (type as ArrayType)?.UnderlyingType ?? type;

            // Namespace: ProjectName.Model.Category
            // TypeName: Identifier
            if (!m_primitiveTypes.TryGetValue($"{underlyingType.Category}.{underlyingType.Identifier}", out string _))
            {
                if (type.IsArray)
                    return $"{ProjectName}.Model.{underlyingType.Category}.{GetMetaIdentifier(underlyingType.Identifier)}{m_arrayMarker}";

                return $"{ProjectName}.Model.{underlyingType.Category}.{GetMetaIdentifier(underlyingType.Identifier)}";
            }

            if (type.IsArray)
                return $"MetaValues{m_arrayMarker}";

            return "MetaValues";
        }

        protected string GetTypeName(DataType type, bool isMetaType) => isMetaType ? GetMetaTypeName(type) : GetDataTypeName(type);

        protected string GetIdentifier(DataType type, bool isMetaType) => isMetaType ? GetMetaIdentifier(type.Identifier) : type.Identifier;

        protected virtual string GetMetaIdentifier(string identifier) => $"_{identifier}Meta";

        protected abstract Dictionary<string, string> GetPrimitiveTypeMap();

        #endregion
    }
}
