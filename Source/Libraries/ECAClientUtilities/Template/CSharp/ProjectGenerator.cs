//******************************************************************************************************
//  CSharp.cs - Gbtc
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
//  05/31/2016 - Stephen Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using GSF.IO;
using ECAClientUtilities.Model;

namespace ECAClientUtilities.Template.CSharp
{
    public class ProjectGenerator
    {
        #region [ Members ]

        // Fields
        private string m_projectName;
        private MappingCompiler m_compiler;
        private ProjectSettings m_settings;

        #endregion

        #region [ Constructors ]

        public ProjectGenerator(string projectName, MappingCompiler compiler)
        {
            m_projectName = projectName;
            m_compiler = compiler;
            m_settings = new ProjectSettings();
        }

        #endregion

        #region [ Properties ]

        public ProjectSettings Settings
        {
            get
            {
                return m_settings;
            }
        }

        #endregion

        #region [ Methods ]

        public void Generate(string projectPath, TypeMapping inputMapping, TypeMapping outputMapping)
        {
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
            WriteSettingsTo(Path.Combine(projectPath, m_projectName), inputMappingReferences);
            WriteModelsTo(Path.Combine(projectPath, m_projectName, "Model"), allTypeReferences);
            WriteMapperTo(Path.Combine(projectPath, m_projectName, "Model"), inputMapping, outputMapping, inputTypeReferences);
            WriteMappingsTo(Path.Combine(projectPath, m_projectName, "Model"), allTypeReferences, allMappingReferences);
            WriteAlgorithmTo(Path.Combine(projectPath, m_projectName), inputMapping.Type, outputMapping.Type);
            UpdateProjectFile(projectPath);
        }

        public void RefreshMappings(string projectPath, TypeMapping inputMapping, TypeMapping outputMapping)
        {
            HashSet<UserDefinedType> userDefinedTypes = new HashSet<UserDefinedType>();
            HashSet<TypeMapping> userDefinedMappings = new HashSet<TypeMapping>();
            GetReferencedTypesAndMappings(inputMapping, userDefinedTypes, userDefinedMappings);
            GetReferencedTypesAndMappings(outputMapping, userDefinedTypes, userDefinedMappings);
            WriteMappingsTo(Path.Combine(projectPath, m_projectName, "Model"), userDefinedTypes, userDefinedMappings);
        }

        // Recursively traverses all referenced types and mappings from the source mapping and stores them in the given hash sets.
        private void GetReferencedTypesAndMappings(TypeMapping sourceMapping, HashSet<UserDefinedType> referencedTypes, HashSet<TypeMapping> referencedMappings)
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
        private void CopyTemplateTo(string path)
        {
            string templateDirectory = FilePath.GetAbsolutePath(@"Templates\CSharp");

            // Establish the directory structure of the
            // template project at the destination path
            Directory.CreateDirectory(path);

            foreach (string directory in Directory.EnumerateDirectories(templateDirectory, "*", SearchOption.AllDirectories))
            {
                // Determine the full path to the destination directory
                string destination = directory
                    .Replace(templateDirectory, path)
                    .Replace("AlgorithmTemplate", m_projectName);

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
                    .Replace("AlgorithmTemplate", m_projectName);

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

                string replacement = text
                    .Replace("AlgorithmTemplate", m_projectName)
                    .Replace(@"..\..\..\Dependencies\GSF\", @"..\Dependencies\GSF\");

                if (text != replacement)
                    File.WriteAllText(destination, replacement);
            }
        }

        // Copies the necessary GSF dependencies to the given path.
        private void CopyDependenciesTo(string path)
        {
            string[] gsfDependencies =
            {
                "GSF.Communication.dll",
                "GSF.Core.dll",
                "GSF.TimeSeries.dll"
            };

            // Create the directory at the destination path
            Directory.CreateDirectory(Path.Combine(path, "GSF"));
            Directory.CreateDirectory(Path.Combine(path, "openECA"));

            // Copy each of the necessary assemblies to the destination directory
            foreach (string dependency in gsfDependencies)
                File.Copy(FilePath.GetAbsolutePath(dependency), Path.Combine(path, "GSF", dependency), true);

            File.Copy(FilePath.GetAbsolutePath("ECAClientUtilities.dll"), Path.Combine(path, "openECA", "ECAClientUtilities.dll"), true);
        }

        // Writes system settings to the given path.
        private void WriteSettingsTo(string path, IEnumerable<TypeMapping> inputMappingReferences)
        {
            // Determine the path to the system settings file
            string systemSettingsPath = Path.Combine(path, "SystemSettings.cs");

            // Build the list of filter expressions from the set of all field mappings
            Func<FieldMapping, bool> primitiveTypeFilter = fieldMapping =>
            {
                DataType fieldType = fieldMapping.Field.Type;
                DataType underlyingType = (fieldType as ArrayType)?.UnderlyingType ?? fieldType;
                return !underlyingType.IsUserDefined;
            };

            string filterExpressions = string.Join("," + Environment.NewLine, inputMappingReferences
                .SelectMany(inputMapping => inputMapping.FieldMappings)
                .Where(primitiveTypeFilter)
                .Select(fieldMapping => $"            @\"{fieldMapping.Expression.Replace("\"", "\"\"")}\"")
                .Distinct());

            // Generate the content for the system settings file
            File.WriteAllText(systemSettingsPath, GetTextFromResource("ECAClientUtilities.Template.CSharp.SettingsTemplate.txt")
                .Replace("{ProjectName}", m_projectName)
                .Replace("{ConnectionString}", $"@\"{m_settings.SubscriberConnectionString.Replace("\"", "\"\"")}\"")
                .Replace("{FilterExpressions}", filterExpressions.Trim()));
        }

        // Generates classes for the all the models used by the input and output types.
        private void WriteModelsTo(string path, IEnumerable<UserDefinedType> allTypeReferences)
        {
            // Clear out all existing models
            // so they can be regenerated
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            foreach (UserDefinedType type in allTypeReferences)
            {
                // Determine the path to the directory and class file to be generated
                string categoryDirectory = Path.Combine(path, type.Category);
                string filePath = Path.Combine(categoryDirectory, type.Identifier + ".cs");
                string content;

                // Create the directory if it doesn't already exist
                Directory.CreateDirectory(categoryDirectory);

                // Create the file for the class being generated
                using (TextWriter writer = File.CreateText(filePath))
                {
                    // Build the list of fields as properties of the generated class
                    string fieldList = string.Join(Environment.NewLine, type.Fields
                        .Select(field => $"        public {GetTypeName(field.Type)} {field.Identifier} {{ get; set; }}"));

                    // Generate the contents of the class file
                    content = GetTextFromResource("ECAClientUtilities.Template.CSharp.UDTTemplate.txt")
                        .Replace("{ProjectName}", m_projectName)
                        .Replace("{Category}", type.Category)
                        .Replace("{Identifier}", type.Identifier)
                        .Replace("{Fields}", fieldList.Trim());

                    // Write the contents to the class file
                    writer.Write(content);
                }
            }
        }

        // Generates the class that maps measurements to objects of the input and output types.
        private void WriteMapperTo(string path, TypeMapping inputMapping, TypeMapping outputMapping, IEnumerable<UserDefinedType> inputTypeReferences)
        {
            // Determine the path to the mapper class file
            string mapperPath = Path.Combine(path, "Mapper.cs");

            // Grab strings used for replacement in the mapper class template
            string inputTypeName = GetTypeName(inputMapping.Type);
            string inputCategoryIdentifier = inputMapping.Type.Category;
            string inputTypeIdentifier = inputMapping.Type.Identifier;
            string outputTypeName = GetTypeName(outputMapping.Type);

            // Create string builders for code generation
            StringBuilder lookupCode = new StringBuilder();
            StringBuilder mappingFunctions = new StringBuilder();

            // Define a recursive method to generate code for measurement key lookups
            Action<TypeMapping> generateLookupCode = null;

            generateLookupCode = typeMapping =>
            {
                foreach (FieldMapping fieldMapping in typeMapping.FieldMappings)
                {
                    // Get the type of the field and its
                    // underlying type if it is an array
                    DataType fieldType = fieldMapping.Field.Type;
                    DataType underlyingType = (fieldType as ArrayType)?.UnderlyingType;

                    // For user-defined types, recursively traverse their fields as well
                    // For primitive types, generate the code to look up the measurement keys
                    if (fieldType.IsArray && underlyingType.IsUserDefined)
                        m_compiler.EnumerateTypeMappings(fieldMapping.Expression).ToList().ForEach(generateLookupCode);
                    else if (fieldType.IsUserDefined)
                        generateLookupCode(m_compiler.GetTypeMapping(fieldMapping.Expression));
                    else if (fieldType.IsArray)
                        lookupCode.AppendLine($"            m_keys.Add(m_lookup.GetMeasurementKeys(@\"{fieldMapping.Expression.Replace("\"", "\"\"")}\"));");
                    else
                        lookupCode.AppendLine($"            m_keys.Add(new MeasurementKey[] {{ m_lookup.GetMeasurementKey(@\"{fieldMapping.Expression.Replace("\"", "\"\"")}\") }});");
                }
            };

            // Call the recursive method to generate code for measurement key lookups
            generateLookupCode(inputMapping);

            // Generate a method for each data type of the input mappings in
            // order to map measurement values to the fields of the data types
            foreach (UserDefinedType type in inputTypeReferences)
            {
                // Grab strings used for replacement
                // in the mapping function template
                string typeName = GetTypeName(type);
                string categoryIdentifier = type.Category;
                string typeIdentifier = type.Identifier;

                // Create the string builder for code generation
                StringBuilder mappingCode = new StringBuilder();

                foreach (UDTField field in type.Fields)
                {
                    // Get the field type and its
                    // underlying type if it is an array
                    DataType fieldType = field.Type;
                    DataType underlyingType = (field.Type as ArrayType)?.UnderlyingType;

                    // For user-defined types, call the method to generate an object of their corresponding data type
                    // For primitive types, call the method to get the values of the mapped measurements
                    if (fieldType.IsArray && underlyingType.IsUserDefined)
                        mappingCode.AppendLine($"			obj.{field.Identifier} = m_mappingCompiler.EnumerateTypeMappings(fieldLookup[\"{field.Identifier}\"].Expression).Select(Create{underlyingType.Category}{underlyingType.Identifier}).ToArray();");
                    else if (fieldType.IsUserDefined)
                        mappingCode.AppendLine($"			obj.{field.Identifier} = Create{field.Type.Category}{field.Type.Identifier}(m_mappingCompiler.GetTypeMapping(fieldLookup[\"{field.Identifier}\"].Expression));");
                    else if (fieldType.IsArray)
                        mappingCode.AppendLine($"			obj.{field.Identifier} = m_lookup.GetMeasurements(m_keys[m_index++]).Select(measurement => ({GetTypeName(underlyingType)})measurement.Value).ToArray();");
                    else
                        mappingCode.AppendLine($"			obj.{field.Identifier} = ({GetTypeName(field.Type)})m_lookup.GetMeasurement(m_keys[m_index++][0]).Value;");
                }

                // Write the content of the mapping function to the string builder containing mapping function code
                mappingFunctions.AppendLine(GetTextFromResource("ECAClientUtilities.Template.CSharp.MappingFunctionTemplate.txt")
                    .Replace("{TypeName}", typeName)
                    .Replace("{CategoryIdentifier}", categoryIdentifier)
                    .Replace("{TypeIdentifier}", typeIdentifier)
                    .Replace("{MappingCode}", mappingCode.ToString().Trim()));

                mappingFunctions.AppendLine();
            }

            // Write the content of the mapper class file to the target location
            File.WriteAllText(mapperPath, GetTextFromResource("ECAClientUtilities.Template.CSharp.MapperTemplate.txt")
                .Replace("{ProjectName}", m_projectName)
                .Replace("{LookupCode}", lookupCode.ToString().Trim())
                .Replace("{InputMapping}", inputMapping.Identifier)
                .Replace("{InputTypeName}", inputTypeName)
                .Replace("{InputCategoryIdentifier}", inputCategoryIdentifier)
                .Replace("{InputTypeIdentifier}", inputTypeIdentifier)
                .Replace("{OutputTypeName}", outputTypeName)
                .Replace("{MappingFunctions}", mappingFunctions.ToString().Trim()));
        }

        // Writes the UDT and mapping files to the specified path, containing the specified types and mappings.
        private void WriteMappingsTo(string path, IEnumerable<UserDefinedType> userDefinedTypes, IEnumerable<TypeMapping> userDefinedMappings)
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
        private void WriteAlgorithmTo(string path, UserDefinedType inputType, UserDefinedType outputType)
        {
            // Determine the path to the file containing the user's algorithm
            string algorithmPath = Path.Combine(path, "Algorithm.cs");

            // Do not overwrite the user's algorithm
            if (File.Exists(algorithmPath))
                return;

            // Generate usings for the namespaces of the classes the user needs for their inputs and outputs
            string usings = string.Join(Environment.NewLine, new UserDefinedType[] { inputType, outputType }
                .Select(type => $"using {m_projectName}.Model.{type.Category};")
                .Distinct()
                .OrderBy(str => str));

            // Write the contents of the user's algorithm class to the class file
            File.WriteAllText(algorithmPath, GetTextFromResource("ECAClientUtilities.Template.CSharp.AlgorithmTemplate.txt")
                .Replace("{Usings}", usings)
                .Replace("{ProjectName}", m_projectName)
                .Replace("{InputType}", inputType.Identifier)
                .Replace("{OutputType}", outputType.Identifier));
        }

        // Updates the .csproj file to include the newly generated classes.
        private void UpdateProjectFile(string projectPath)
        {
            // Determine the path to the project file and the generated models
            string projectFilePath = Path.Combine(projectPath, m_projectName, m_projectName + ".csproj");
            string modelPath = Path.Combine(projectPath, m_projectName, "Model");

            // Load the project file as an XML file
            XDocument document = XDocument.Load(projectFilePath);
            XNamespace xmlNamespace = document.Root?.GetDefaultNamespace() ?? XNamespace.None;

            // Remove elements referencing files in the model folder
            document
                .Descendants()
                .Where(element => element.Attribute("Include")?.Value.StartsWith(@"Model\") ?? false)
                .ToList()
                .ForEach(element => element.Remove());

            // Remove elements referencing Algorithm.cs
            document
                .Descendants()
                .Where(element => (string)element.Attribute("Include") == "Algorithm.cs")
                .ToList()
                .ForEach(element => element.Remove());

            // Remove elements reference ECAClientUtilities.dll
            document
                .Descendants()
                .Where(element => (string)element.Attribute("Include") == "ECAClientUtilities, Version=0.1.12.0, Culture=neutral, PublicKeyToken=null")
                .ToList()
                .ForEach(element => element.Remove());

            // Locate the item group that contains <Compile> child elements
            XElement itemGroup = document
                .Descendants(xmlNamespace + "ItemGroup")
                .Where(element => !element.Elements().Any())
                .FirstOrDefault() ?? new XElement(xmlNamespace + "ItemGroup");

            // If the ItemGroup element was just created,
            // add it to the root of the document
            if ((object)itemGroup.Parent == null)
                document.Root?.Add(itemGroup);

            // Add references to every item in the Model directory
            foreach (string model in Directory.EnumerateFiles(modelPath, "*", SearchOption.AllDirectories))
            {
                XAttribute includeAttribute = new XAttribute("Include", model.Replace(modelPath, "Model"));

                if (model.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    itemGroup.Add(new XElement(xmlNamespace + "Compile", includeAttribute));
                else
                    itemGroup.Add(new XElement(xmlNamespace + "Content", includeAttribute, new XElement(xmlNamespace + "CopyToOutputDirectory", "PreserveNewest")));
            }

            // Add a reference to the user algorithm
            itemGroup.Add(new XElement(xmlNamespace + "Compile", new XAttribute("Include", "Algorithm.cs")));

            // Add a reference to ECAClientUtilities.dll
            itemGroup.Add(
                new XElement(xmlNamespace + "Reference", new XAttribute("Include", "ECAClientUtilities, Version=0.1.12.0, Culture=neutral, PublicKeyToken=null"),
                    new XElement(xmlNamespace + "SpecificVersion", "False"),
                    new XElement(xmlNamespace + "HintPath", @"..\Dependencies\openECA\ECAClientUtilities.dll")));

            // Save changes to the project file
            document.Save(projectFilePath);
        }

        // Converts an embedded resource to a string.
        private string GetTextFromResource(string resourceName)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

            if ((object)stream == null)
                return string.Empty;

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
            {
                return reader.ReadToEnd();
            }
        }

        // Converts the given data type to string representing the corresponding C# data type.
        private string GetTypeName(DataType type)
        {
            Dictionary<string, string> primitiveTypes = new Dictionary<string, string>()
            {
                { "Integer.Byte", "byte" },
                { "Integer.Int16", "short" },
                { "Integer.Int32", "int" },
                { "Integer.Int64", "long" },
                { "Integer.UInt16", "ushort" },
                { "Integer.UInt32", "uint" },
                { "Integer.UInt64", "ulong" },
                { "FloatingPoint.Decimal", "decimal" },
                { "FloatingPoint.Double", "double" },
                { "FloatingPoint.Single", "float" },
                { "DateTime.Date", "System.DateTime" },
                { "DateTime.DateTime", "System.DateTime" },
                { "DateTime.Time", "System.TimeSpan" },
                { "DateTime.TimeSpan", "System.TimeSpan" },
                { "Text.Char", "char" },
                { "Text.String", "string" },
                { "Other.Boolean", "bool" },
                { "Other.Guid", "System.Guid" }
            };

            DataType underlyingType = (type as ArrayType)?.UnderlyingType ?? type;
            string typeName;

            // Namespace: ProjectName.Model.Category
            // TypeName: Identifier
            if (!primitiveTypes.TryGetValue($"{underlyingType.Category}.{underlyingType.Identifier}", out typeName))
                typeName = $"{m_projectName}.Model.{underlyingType.Category}.{underlyingType.Identifier}";

            if (type.IsArray)
                typeName += "[]";

            return typeName;
        }

        #endregion
    }
}
