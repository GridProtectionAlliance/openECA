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
            HashSet<UserDefinedType> userDefinedTypes = new HashSet<UserDefinedType>();
            HashSet<TypeMapping> userDefinedMappings = new HashSet<TypeMapping>();

            GetReferencedTypesAndMappings(inputMapping, userDefinedTypes, userDefinedMappings);
            GetReferencedTypesAndMappings(outputMapping, userDefinedTypes, userDefinedMappings);

            CopyTemplateTo(projectPath);
            CopyDependenciesTo(Path.Combine(projectPath, "Dependencies", "GSF"));
            WriteSettingsTo(Path.Combine(projectPath, m_projectName), inputMapping);
            WriteModelsTo(Path.Combine(projectPath, m_projectName, "Model"), inputMapping.Type, outputMapping.Type);
            WriteMapperTo(Path.Combine(projectPath, m_projectName, "Model"), inputMapping, outputMapping);
            WriteMappingsTo(Path.Combine(projectPath, m_projectName, "Model"), userDefinedTypes, userDefinedMappings);
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

        private void GetReferencedTypesAndMappings(TypeMapping sourceMapping, HashSet<UserDefinedType> userDefinedTypes, HashSet<TypeMapping> userDefinedMappings)
        {
            userDefinedTypes.Add(sourceMapping.Type);

            if (!userDefinedMappings.Add(sourceMapping))
                return;

            IEnumerable<FieldMapping> udtFields = sourceMapping.FieldMappings
                .Where(fieldMapping =>
                {
                    DataType fieldType = fieldMapping.Field.Type;
                    DataType underlyingType = (fieldType as ArrayType)?.UnderlyingType ?? fieldType;
                    return underlyingType.IsUserDefined;
                });

            foreach (FieldMapping fieldMapping in udtFields)
            {
                foreach (TypeMapping typeMapping in m_compiler.EnumerateTypeMappings(fieldMapping.Expression))
                    GetReferencedTypesAndMappings(typeMapping, userDefinedTypes, userDefinedMappings);
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
                File.WriteAllText(destination, File.ReadAllText(destination)
                    .Replace("AlgorithmTemplate", m_projectName)
                    .Replace(@"..\..\..\Dependencies\GSF\", @"..\Dependencies\GSF\"));
            }
        }

        // Copies the necessary GSF dependencies to the given path.
        private void CopyDependenciesTo(string path)
        {
            string[] dependencies =
            {
                "GSF.Communication.dll",
                "GSF.Core.dll",
                "GSF.TimeSeries.dll"
            };

            // Create the directory at the destination path
            Directory.CreateDirectory(path);

            // Copy each of the necessary GSF
            // assemblies to the destination directory
            foreach (string dependency in dependencies)
                File.Copy(FilePath.GetAbsolutePath(dependency), Path.Combine(path, dependency), true);
        }

        // Writes system settings to the given path.
        private void WriteSettingsTo(string path, TypeMapping inputMapping)
        {
            // Determine the path to the system settings file
            string systemSettingsPath = Path.Combine(path, "SystemSettings.cs");

            HashSet<FieldMapping> allFieldMappings = new HashSet<FieldMapping>();
            Action<TypeMapping> fillAllFieldMappings = null;

            fillAllFieldMappings = typeMapping =>
            {
                foreach (FieldMapping fieldMapping in typeMapping.FieldMappings)
                {
                    DataType fieldType = fieldMapping.Field.Type;
                    DataType underlyingType = (fieldType as ArrayType)?.UnderlyingType ?? fieldType;

                    // Add the field mapping to the set of all field mappings
                    if (!allFieldMappings.Add(fieldMapping))
                        return;

                    // Base case: mappings for primitive types do not
                    //            reference other type mappings
                    if (!underlyingType.IsUserDefined)
                        continue;

                    // Get the list of type mappings referenced by this field mapping
                    TypeMapping[] nestedMappings = m_compiler.EnumerateTypeMappings(fieldMapping.Expression).ToArray();

                    if (!fieldType.IsArray)
                    {
                        // If the field type is not an array type,
                        // make sure it is mapped to exactly one type mapping
                        if (nestedMappings.Length > 1)
                            throw new InvalidOperationException($"Too many type mappings returned by filter expression {{ {fieldMapping.Expression} }} for field mapping {fieldMapping.Field.Identifier} in type mapping {typeMapping.Identifier}.");

                        if (nestedMappings.Length == 0)
                            throw new InvalidOperationException($"No mappings returned by filter expression {{ {fieldMapping.Expression} }} for field mapping {fieldMapping.Field.Identifier} in type mapping {typeMapping.Identifier}.");
                    }
                    else
                    {
                        if (((ArrayMapping)fieldMapping).WindowSize > 0.0M)
                            throw new NotSupportedException("Sliding windows are not yet supported!");

                        // Get the collection of types that do not match the field's underlying type
                        UserDefinedType[] nestedTypes = nestedMappings
                            .Select(mapping => mapping.Type)
                            .Where(type => type != underlyingType)
                            .Distinct()
                            .ToArray();

                        // If there are any non-matching types, throw an error
                        if (nestedTypes.Length > 0)
                        {
                            IEnumerable<string> typeNames = nestedTypes.Select(type => type.Category + " " + type.Identifier);

                            string message = $"Mappings returned by filter expression {{ {fieldMapping.Expression} }} " +
                                             $"for field mapping {fieldMapping.Field.Identifier} in type mapping {typeMapping.Identifier} " +
                                             $"returned mappings that do not match the field type: {string.Join(", ", typeNames)}";

                            throw new InvalidOperationException(message);
                        }
                    }

                    // Recursively add the field mappings of the referenced
                    // type mappings to the set of all field mappings
                    foreach (TypeMapping nestedMapping in nestedMappings)
                        fillAllFieldMappings(nestedMapping);
                }
            };

            // Populate the set of all field mappings
            fillAllFieldMappings(inputMapping);

            // Build the list of filter expressions from the set of all field mappings
            Func<FieldMapping, bool> primitiveTypeFilter = fieldMapping =>
            {
                DataType fieldType = fieldMapping.Field.Type;
                DataType underlyingType = (fieldType as ArrayType)?.UnderlyingType ?? fieldType;
                return !underlyingType.IsUserDefined;
            };

            string filterExpressions = string.Join("," + Environment.NewLine, allFieldMappings
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
        private void WriteModelsTo(string path, UserDefinedType inputType, UserDefinedType outputType)
        {
            // Clear out all existing models
            // so they can be regenerated
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            // Recursively traverse the type references to add all
            // the necessary classes for creating inputs and outputs
            WriteModelTo(path, inputType);
            WriteModelTo(path, outputType);
        }

        // Generates the class for the given type and writes it to the given path,
        // then recursively traverses the types referenced by the fields of the given type.
        private void WriteModelTo(string path, UserDefinedType type)
        {
            // Determine the path to the directory and class file to be generated
            string categoryDirectory = Path.Combine(path, type.Category);
            string filePath = Path.Combine(categoryDirectory, type.Identifier + ".cs");
            string content;

            // If the file already exists,
            // then it's already been generated
            if (File.Exists(filePath))
                return;

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

            // Recursively generate classes for each of the
            // UDTs referenced by the one we just generated
            foreach (UDTField field in type.Fields)
            {
                DataType fieldType = field.Type;
                DataType underlyingType = (fieldType as ArrayType)?.UnderlyingType ?? fieldType;

                if (underlyingType.IsUserDefined)
                    WriteModelTo(path, (UserDefinedType)underlyingType);
            }
        }

        // Generates the class that maps measurements to objects of the input and output types.
        private void WriteMapperTo(string path, TypeMapping inputMapping, TypeMapping outputMapping)
        {
            // Determine the path to the mapper class file
            string mapperPath = Path.Combine(path, "Mapper.cs");
            StringBuilder keyBuilder = new StringBuilder();
            StringBuilder lookupBuilder = new StringBuilder();
            StringBuilder mapBuilder = new StringBuilder();

            // Write the line that creates the input object into the string builder
            mapBuilder.AppendLine($"            {m_projectName}.Model.{inputMapping.Type.Category}.{inputMapping.Type.Identifier} input = new {m_projectName}.Model.{inputMapping.Type.Category}.{inputMapping.Type.Identifier}();");

            // Call the method that recursively adds the lines of
            // code to map measurements to the fields of the input type
            PopulateInputFields(keyBuilder, lookupBuilder, mapBuilder, inputMapping, "input");
            mapBuilder.AppendLine();

            // Write the line of code that calls the user's algorithm and receives the user's output
            mapBuilder.AppendLine($"            {GetTypeName(outputMapping.Type)} output = {m_projectName}.Algorithm.Execute(input);");

            // Write the content of the mapper class file to the target location
            File.WriteAllText(mapperPath, GetTextFromResource("ECAClientUtilities.Template.CSharp.MapperTemplate.txt")
                .Replace("{ProjectName}", m_projectName)
                .Replace("{MeasurementKeys}", keyBuilder.ToString().Trim())
                .Replace("{LookupCode}", lookupBuilder.ToString().Trim())
                .Replace("{MappingCode}", mapBuilder.ToString().Trim()));
        }

        // Writes lines of code to the given string builder for populating the input type.
        private void PopulateInputFields(StringBuilder keyBuilder, StringBuilder lookupBuilder, StringBuilder mapBuilder, TypeMapping typeMapping, string objectPath)
        {
            foreach (FieldMapping fieldMapping in typeMapping.FieldMappings)
            {
                if (fieldMapping.Field.Type.IsUserDefined)
                {
                    // Fields for user defined types are mapped to other type mappings
                    // so we recursively add code to populate those types as well
                    TypeMapping[] nestedMappings = m_compiler.EnumerateTypeMappings(fieldMapping.Expression).ToArray();

                    if (nestedMappings.Length > 1)
                        throw new InvalidOperationException($"Too many type mappings returned by filter expression {{ {fieldMapping.Expression} }} for field mapping {fieldMapping.Field.Identifier} in type mapping {typeMapping.Identifier}.");

                    if (nestedMappings.Length == 0)
                        throw new InvalidOperationException($"No mappings returned by filter expression {{ {fieldMapping.Expression} }} for field mapping {fieldMapping.Field.Identifier} in type mapping {typeMapping.Identifier}.");

                    mapBuilder.AppendLine($"            {objectPath}.{fieldMapping.Field.Identifier} = new {GetTypeName(fieldMapping.Field.Type)}();");
                    PopulateInputFields(keyBuilder, lookupBuilder, mapBuilder, nestedMappings[0], $"{objectPath}.{fieldMapping.Field.Identifier}");
                }
                else if (fieldMapping.Field.Type.IsArray)
                {
                    if (((ArrayMapping)fieldMapping).WindowSize > 0.0M)
                        throw new NotSupportedException("Sliding windows are not yet supported!");

                    DataType underlyingType = ((ArrayType)fieldMapping.Field.Type).UnderlyingType;

                    if (!underlyingType.IsUserDefined)
                    {
                        // Create a unique identifier for the measurement key variable
                        string keyIdentifier = $"m_key{keyBuilder.Length}";

                        // Add the line of code to define the measurement keys used to look up the measurements
                        keyBuilder.AppendLine($"        private MeasurementKey[] {keyIdentifier};");

                        // Add the line of code to look up the measurement keys from the filter expression
                        lookupBuilder.AppendLine($"            {keyIdentifier} = m_lookup.GetMeasurementKeys(@\"{fieldMapping.Expression.Replace("\"", "\"\"")}\");");

                        // Add the line of code to look up the collection of measurements and store their values in the appropriate input field
                        mapBuilder.AppendLine($"            {objectPath}.{fieldMapping.Field.Identifier} = m_lookup.GetMeasurements({keyIdentifier}).Select(measurement => ({GetTypeName(underlyingType)})measurement.Value).ToArray();");
                    }
                    else
                    {
                        // Get the collection type mappings referenced by this field mapping
                        TypeMapping[] nestedMappings = m_compiler.EnumerateTypeMappings(fieldMapping.Expression).ToArray();

                        // Get the collection of types that do not match the field's underlying type
                        UserDefinedType[] nestedTypes = nestedMappings
                            .Select(mapping => mapping.Type)
                            .Where(type => type != underlyingType)
                            .Distinct()
                            .ToArray();

                        // If there are any non-matching types, throw an error
                        if (nestedTypes.Length > 0)
                        {
                            IEnumerable<string> typeNames = nestedTypes.Select(type => type.Category + " " + type.Identifier);

                            string message = $"Mappings returned by filter expression {{ {fieldMapping.Expression} }} " +
                                             $"for field mapping {fieldMapping.Field.Identifier} in type mapping {typeMapping.Identifier} " +
                                             $"returned mappings that do not match the field type: {string.Join(", ", typeNames)}";

                            throw new InvalidOperationException(message);
                        }

                        // Add the line of code to initialize the array
                        mapBuilder.AppendLine($"            {objectPath}.{fieldMapping.Field.Identifier} = new {GetTypeName(underlyingType)}[{nestedMappings.Length}];");

                        // Populate each of the elements of the array
                        for (int i = 0; i < nestedMappings.Length; i++)
                            PopulateInputFields(keyBuilder, lookupBuilder, mapBuilder, nestedMappings[i], $"{objectPath}.{fieldMapping.Field.Identifier}[{i}]");
                    }
                }
                else
                {
                    // Create a unique identifier for the measurement key variable
                    string keyIdentifier = $"m_key{keyBuilder.Length}";

                    // Add the line of code to define the measurement keys used to look up the measurements
                    keyBuilder.AppendLine($"        private MeasurementKey {keyIdentifier};");

                    // Add the line of code to look up the measurement keys from the filter expression
                    lookupBuilder.AppendLine($"            {keyIdentifier} = m_lookup.GetMeasurementKey(@\"{fieldMapping.Expression.Replace("\"", "\"\"")}\");");

                    // Add the line of code to look up the measurement value and store it in the appropriate input field
                    mapBuilder.AppendLine($"            {objectPath}.{fieldMapping.Field.Identifier} = ({GetTypeName(fieldMapping.Field.Type)})m_lookup.GetMeasurement({keyIdentifier}).Value;");
                }
            }
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
