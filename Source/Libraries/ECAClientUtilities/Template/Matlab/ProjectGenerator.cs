﻿//******************************************************************************************************
//  ProjectGenerator.cs - Gbtc
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
//  07/08/2016 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using ECACommonUtilities;
using ECACommonUtilities.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// ReSharper disable RedundantStringInterpolation
namespace ECAClientUtilities.Template.Matlab
{
    public class ProjectGenerator : DotNetProjectGeneratorBase
    {
        #region [ Members ]

        private readonly Dictionary<string, string> m_primitiveDefaultValues;
        private readonly Dictionary<string, Tuple<string, bool>> m_primitiveConversionFunctions;

        #endregion

        #region [ Constructors ]

        public ProjectGenerator(string projectName, MappingCompiler compiler) : base(projectName, compiler, "m", "Matlab")
        {
            m_primitiveDefaultValues = new Dictionary<string, string>()
            {
                { "Integer.Byte", "0" },
                { "Integer.Int16", "0" },
                { "Integer.Int32", "0" },
                { "Integer.Int64", "0" },
                { "Integer.UInt16", "0" },
                { "Integer.UInt32", "0" },
                { "Integer.UInt64", "0" },
                { "FloatingPoint.Decimal", "0.0" },
                { "FloatingPoint.Double", "0.0" },
                { "FloatingPoint.Single", "0.0" },
                { "DateTime.Date", "System.DateTime.MinValue" },
                { "DateTime.DateTime", "System.DateTime.MinValue" },
                { "DateTime.Time", "System.DateTime.MinValue" },
                { "DateTime.TimeSpan", "System.TimeSpan.MinValue" },
                { "Text.Char", "''" },
                { "Text.String", "''" },
                { "Other.Boolean", "0" },
                { "Other.Guid", "System.Guid.Empty" }
            };

            m_primitiveConversionFunctions = new Dictionary<string, Tuple<string, bool>>
            {
                { "Integer.Byte", new Tuple<string, bool>("uint8", false) },
                { "Integer.Int16", new Tuple<string, bool>("int16", false) },
                { "Integer.Int32", new Tuple<string, bool>("int32", false) },
                { "Integer.Int64", new Tuple<string, bool>("int64", false) },
                { "Integer.UInt16", new Tuple<string, bool>("uint16", false) },
                { "Integer.UInt32", new Tuple<string, bool>("uint32", false) },
                { "Integer.UInt64", new Tuple<string, bool>("uint64", false) },
                { "FloatingPoint.Decimal", new Tuple<string, bool>("double", false) },
                { "FloatingPoint.Double", new Tuple<string, bool>("double", false) },
                { "FloatingPoint.Single", new Tuple<string, bool>("single", false) },
                { "DateTime.Date", new Tuple<string, bool>("DateTime.Parse", true) },
                { "DateTime.DateTime", new Tuple<string, bool>("DateTime.Parse", true) },
                { "DateTime.Time", new Tuple<string, bool>("DateTime.Parse", true) },
                { "DateTime.TimeSpan", new Tuple<string, bool>("TimeSpan.Parse", true) },
                { "Text.Char", new Tuple<string, bool>("char", false) },
                { "Text.String", new Tuple<string, bool>("char", false) },
                { "Other.Boolean", new Tuple<string, bool>("logical", false) },
                { "Other.Guid", new Tuple<string, bool>("Guid.Parse", true) }
            };
        }

        #endregion

        #region [ Methods ]

        // Needs overwrite because Matlab has a different Folder Structure
        // which can not easily be fit into the base class function
        public new void Generate(string projectPath, TypeMapping inputMapping, TypeMapping outputMapping)
        {
            string libraryName = $"{ProjectName}Library";
            string serviceName = $"{ProjectName}Service";

            string libraryPath = Path.Combine(projectPath, libraryName);
            string servicePath = Path.Combine(projectPath, serviceName);

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

            WriteModelsTo(Path.Combine(libraryPath, "Model"), allTypeReferences);
            WriteMapperTo(Path.Combine(libraryPath, "Model"), inputMapping.Type, outputMapping.Type, inputTypeReferences);
            WriteUnmapperTo(Path.Combine(libraryPath, "Model"), outputMapping.Type, outputTypeReferences);
            WriteMappingsTo(Path.Combine(libraryPath, "Model"), allTypeReferences, allMappingReferences);
            WriteAlgorithmTo(libraryPath, inputMapping, outputMapping);
            WriteFrameworkFactoryTo(libraryPath);
            WriteProgramTo(libraryPath, projectPath, inputTypeReferences, outputMapping.Type);
            WriteAlgorithmHostingEnvironmentTo(servicePath);
            UpdateProjectFiles(projectPath, GetReferencedTypes(inputMapping.Type, outputMapping.Type));
            UpdateSetupScriptFile(projectPath);
        }

        protected override string ConstructDataModel(UserDefinedType type)
        {
            string fieldList = string.Join(", ", type.Fields.Select(field => $"'{field.Identifier}', {GetDefaultDataValue(field.Type)}"));

            // Generate the contents of the class file
            return GetTextFromResource("ECAClientUtilities.Template.Matlab.UDTTemplate.txt")
                .Replace("{ProjectName}", ProjectName)
                .Replace("{Category}", type.Category)
                .Replace("{Identifier}", type.Identifier)
                .Replace("{Fields}", fieldList.Trim());
        }

        protected override string ConstructMetaModel(UserDefinedType type)
        {
            string fieldList = string.Join(", ", type.Fields.Select(field => $"'{field.Identifier}', {GetDefaultMetaValue(field.Type)}"));

            // Generate the contents of the class file
            return GetTextFromResource("ECAClientUtilities.Template.Matlab.UDTTemplate.txt")
                .Replace("{ProjectName}", ProjectName)
                .Replace("{Category}", type.Category)
                .Replace("{Identifier}", GetMetaIdentifier(type.Identifier))
                .Replace("{Fields}", fieldList.Trim());
        }

        protected override string ConstructMapping(UserDefinedType type, bool isMetaType)
        {
            StringBuilder mappingCode = new StringBuilder();

            foreach (UDTField field in type.Fields)
            {
                // Get the field type and its
                // underlying type if it is an array
                DataType fieldType = field.Type;
                DataType underlyingType = (fieldType as ArrayType)?.UnderlyingType;
                string fieldIdentifier = field.Identifier;

                // For user-defined types, call the method to generate an object of their corresponding data type
                // For primitive types, call the method to get the values of the mapped measurements
                // ReSharper disable once PossibleNullReferenceException
                if (fieldType.IsArray && underlyingType.IsUserDefined)
                {
                    string arrayTypeName = GetTypeName(underlyingType, isMetaType);

                    mappingCode.AppendLine($"            % Create {arrayTypeName} UDT array for \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"            self.m_helper.PushCurrentFrame();");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"            arrayMapping = fieldLookup.Item('{fieldIdentifier}');");
                    mappingCode.AppendLine($"            count = self.m_helper.GetUDTArrayTypeMappingCount(arrayMapping);");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"            for i = 1:count");
                    mappingCode.AppendLine($"                nestedMapping = self.m_helper.GetUDTArrayTypeMapping(arrayMapping, i - 1);");
                    mappingCode.AppendLine($"                udt.{fieldIdentifier}(i) = self.Create{underlyingType.Category}{GetIdentifier(underlyingType, isMetaType)}(nestedMapping));");
                    mappingCode.AppendLine($"            end");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"            self.m_helper.PopCurrentFrame();");
                }
                else if (fieldType.IsUserDefined)
                {
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    mappingCode.AppendLine($"            % Create {fieldTypeName} UDT for \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"            fieldMapping = fieldLookup.Item('{fieldIdentifier}');");
                    mappingCode.AppendLine($"            nestedMapping = self.m_helper.GetTypeMapping(fieldMapping);");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"            self.m_helper.PushRelativeFrame(fieldMapping);");
                    mappingCode.AppendLine($"            udt.{fieldIdentifier} = self.Create{fieldType.Category}{GetIdentifier(fieldType, isMetaType)}(nestedMapping);");
                    mappingCode.AppendLine($"            self.m_helper.PopRelativeFrame(fieldMapping);");
                }
                else if (fieldType.IsArray)
                {
                    string conversionFunction = GetConversionFunction(underlyingType, out bool forceToString);
                    string arrayTypeName = GetTypeName(underlyingType, isMetaType);

                    mappingCode.AppendLine($"            % Create {arrayTypeName} array for \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"            arrayMapping = fieldLookup.Item('{fieldIdentifier}');");
                    mappingCode.AppendLine($"            count = this.m_helper.GetArrayMeasurementCount(arrayMapping);");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"            for i = 1:count");
                    mappingCode.AppendLine($"                measurement = this.m_helper.GetArrayMeasurement(i - 1);");
                    if (isMetaType)
                        mappingCode.AppendLine($"                udt.{fieldIdentifier}(i) = this.m_helper.GetMetaValues(measurement);");
                    else
                        mappingCode.AppendLine($"                udt.{fieldIdentifier}(i) = {conversionFunction}(measurement.Value{(forceToString ? ".ToString()" : "")});");
                    mappingCode.AppendLine($"            end");
                }
                else
                {
                    string conversionFunction = GetConversionFunction(fieldType, out bool forceToString);
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    mappingCode.AppendLine($"            % Assign {fieldTypeName} value to \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"            fieldMapping = fieldLookup.Item('{fieldIdentifier}');");
                    mappingCode.AppendLine($"            measurement = self.m_helper.GetMeasurement(fieldMapping);");
                    if (isMetaType)
                        mappingCode.AppendLine($"            udt.{fieldIdentifier} = self.m_helper.GetMetaValues(measurement);");
                    else
                        mappingCode.AppendLine($"            udt.{fieldIdentifier} = {conversionFunction}(measurement.Value{(forceToString ? ".ToString()" : "")});");
                }

                mappingCode.AppendLine();
            }

            return mappingCode.ToString();
        }

        protected override string ConstructFillFunction(UserDefinedType type, bool isMetaType)
        {
            StringBuilder fillCode = new StringBuilder();

            foreach (UDTField field in type.Fields)
            {
                // Get the field type and its
                // underlying type if it is an array
                DataType fieldType = field.Type;
                DataType underlyingType = (fieldType as ArrayType)?.UnderlyingType;
                string fieldIdentifier = field.Identifier;

                // For user-defined types, call the method to generate an object of their corresponding data type
                // For primitive types, do nothing; but in the case of meta types, generate meta value structures
                // ReSharper disable once PossibleNullReferenceException
                if (fieldType.IsArray && underlyingType.IsUserDefined)
                {
                    string arrayTypeName = GetTypeName(underlyingType, isMetaType);

                    fillCode.AppendLine($"            % Initialize {arrayTypeName} UDT array for \"{fieldIdentifier}\" field");
                    fillCode.AppendLine($"            arrayMapping = fieldLookup.Item('{fieldIdentifier}');");
                    fillCode.AppendLine($"            self.m_helper.PushWindowFrameTime(arrayMapping);");
                    fillCode.AppendLine($"            list = [];");
                    fillCode.AppendLine($"            count = this.m_helper.GetUDTArrayTypeMappingCount(arrayMapping);");
                    fillCode.AppendLine();
                    fillCode.AppendLine($"            for i = 1:count");
                    fillCode.AppendLine($"                nestedMapping = this.m_helper.GetUDTArrayTypeMapping(arrayMapping, i - 1);");
                    fillCode.AppendLine($"                append(list, this.Fill{underlyingType.Category}{GetIdentifier(underlyingType, isMetaType)}(nestedMapping));");
                    fillCode.AppendLine($"            end");
                    fillCode.AppendLine();
                    fillCode.AppendLine($"            obj.{fieldIdentifier} = list;");
                    fillCode.AppendLine($"            self.m_helper.PopWindowFrameTime(arrayMapping);");
                }
                else if (fieldType.IsUserDefined)
                {
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    fillCode.AppendLine($"            % Initialize {fieldTypeName} UDT for \"{fieldIdentifier}\" field");
                    fillCode.AppendLine($"            fieldMapping = fieldLookup.Item('{fieldIdentifier}');");
                    fillCode.AppendLine($"            nestedMapping = self.m_helper.GetTypeMapping(fieldMapping);");
                    fillCode.AppendLine();
                    fillCode.AppendLine($"            self.m_helper.PushRelativeFrame(fieldMapping);");
                    fillCode.AppendLine($"            obj.{fieldIdentifier} = this.Fill{fieldType.Category}{GetIdentifier(fieldType, isMetaType)}(nestedMapping);");
                    fillCode.AppendLine($"            self.m_helper.PopRelativeFrame(fieldMapping);");
                }
                else if (fieldType.IsArray)
                {
                    string arrayTypeName = GetTypeName(underlyingType, isMetaType);

                    fillCode.AppendLine($"            % Initialize {arrayTypeName} array for \"{fieldIdentifier}\" field");
                    fillCode.AppendLine($"            arrayMapping = fieldLookup.Item('{fieldIdentifier}');");
                    if (isMetaType)
                    {
                        fillCode.AppendLine($"            obj.{fieldIdentifier}(i) = self.m_helper.CreateMetaValues(arrayMapping);");
                    }
                    else
                    {
                        fillCode.AppendLine($"            list = [];");
                        fillCode.AppendLine($"            count = self.m_helper.GetArrayMeasurementCount(arrayMapping);");
                        fillCode.AppendLine();
                        fillCode.AppendLine($"            for i = 1:count");
                        fillCode.AppendLine($"                append(list, {GetDefaultDataValue(underlyingType)});");
                        fillCode.AppendLine($"            end");
                        fillCode.AppendLine();
                        fillCode.AppendLine($"            obj.{fieldIdentifier}(i) = list;");
                    }
                }
                else
                {
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    if (isMetaType)
                    {
                        fillCode.AppendLine($"            % Assign {fieldTypeName} value to \"{fieldIdentifier}\" field");
                        fillCode.AppendLine($"            fieldMapping = fieldLookup.Item('{fieldIdentifier}');");
                        fillCode.AppendLine($"            obj.{fieldIdentifier} = self.m_helper.Unmapper.CreateMetaValues(fieldMapping)");
                    }
                    else
                    {
                        fillCode.AppendLine($"            % We don't need to do anything, but we burn a key index to keep our");
                        fillCode.AppendLine($"            % array index in sync with where we are in the data structure");
                        fillCode.AppendLine($"            self.m_helper.Unmapper.BurnKeyIndex()");
                    }
                }

                fillCode.AppendLine();
            }

            return fillCode.ToString();
        }

        protected override string ConstructUnmapping(UserDefinedType type)
        {
            StringBuilder unmappingCode = new StringBuilder();

            foreach (UDTField field in type.Fields)
            {
                // Get the field type and its
                // underlying type if it is an array
                DataType fieldType = field.Type;
                DataType underlyingType = (fieldType as ArrayType)?.UnderlyingType;
                string fieldIdentifier = field.Identifier;

                // For user-defined types, call the method to collect measurements from object of their corresponding data type
                // For primitive types, call the method to get the values of the mapped measurements
                // ReSharper disable once PossibleNullReferenceException
                if (fieldType.IsArray && underlyingType.IsUserDefined)
                {
                    string arrayTypeName = GetTypeName(underlyingType, false);

                    unmappingCode.AppendLine($"            % Convert values from {arrayTypeName} UDT array for \"{fieldIdentifier}\" field to measurements");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"            arrayMapping = fieldLookup.Item('{fieldIdentifier}');");
                    unmappingCode.AppendLine($"            dataLength = length(data.{fieldIdentifier});");
                    unmappingCode.AppendLine($"            metaLength = length(meta.{fieldIdentifier});");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"            if dataLength ~= metaLength");
                    unmappingCode.AppendLine($"                throw(MException('Unmap:{fieldIdentifier}', strcat('Values array length (', num2str(dataLength), ') and MetaValues array length (', num2str(metaLength), ') for field \"{fieldIdentifier}\" must be the same.'))");
                    unmappingCode.AppendLine($"            end");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"            self.m_helper.PushWindowFrameTime(arrayMapping);");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"            for i = 1:dataLength");
                    unmappingCode.AppendLine($"                nestedMapping = self.m_helper.GetUDTArrayTypeMapping(arrayMapping, i - 1);");
                    unmappingCode.AppendLine($"                self.CollectFrom{underlyingType.Category}{underlyingType.Identifier}(measurements, nestedMapping, data.{fieldIdentifier}(i), meta.{fieldIdentifier}(i));");
                    unmappingCode.AppendLine($"            end");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"            self.m_helper.PopWindowFrameTime();");
                }
                else if (fieldType.IsUserDefined)
                {
                    string fieldTypeName = GetTypeName(fieldType, false);

                    unmappingCode.AppendLine($"            % Convert values from {fieldTypeName} UDT for \"{fieldIdentifier}\" field to measurements");
                    unmappingCode.AppendLine($"            fieldMapping = fieldLookup.Item('{fieldIdentifier}');");
                    unmappingCode.AppendLine($"            nestedMapping = self.m_helper.GetTypeMapping(fieldMapping);");
                    unmappingCode.AppendLine($"            self.CollectFrom{fieldType.Category}{fieldType.Identifier}(measurements, nestedMapping, data.{fieldIdentifier}, meta.{fieldIdentifier});");
                }
                else if (fieldType.IsArray)
                {
                    unmappingCode.AppendLine($"            % Convert values from array in \"{fieldIdentifier}\" field to measurements");
                    unmappingCode.AppendLine($"            arrayMapping = fieldLookup.Item('{fieldIdentifier}');");
                    unmappingCode.AppendLine($"            dataLength = length(data.{fieldIdentifier});");
                    unmappingCode.AppendLine($"            metaLength = length(meta.{fieldIdentifier});");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"            if dataLength ~= metaLength");
                    unmappingCode.AppendLine($"                throw(MException('Unmap:{fieldIdentifier}', strcat('Values array length (', num2str(dataLength), ') and MetaValues array length (', num2str(metaLength), ') for field \"{fieldIdentifier}\" must be the same.'))");
                    unmappingCode.AppendLine($"            end");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"            for i = 1:count");
                    unmappingCode.AppendLine($"                measurement = self.m_helper.Unmapper.MakeMeasurement(meta.{fieldIdentifier}(i), data.{fieldIdentifier}(i));");
                    unmappingCode.AppendLine($"                measurements.Add(measurement);");
                    unmappingCode.AppendLine($"            end");
                }
                else
                {
                    unmappingCode.AppendLine($"            % Convert value from \"{fieldIdentifier}\" field to measurement");
                    unmappingCode.AppendLine($"            measurement = self.m_helper.Unmapper.MakeMeasurement(meta.{fieldIdentifier}, data.{fieldIdentifier});");
                    unmappingCode.AppendLine($"            measurements.Add(measurement);");
                }

                unmappingCode.AppendLine();
            }

            return unmappingCode.ToString();
        }

        protected override string ConstructUsing(UserDefinedType type)
        {
            return $"addpath('Model/{type.Category}/');";
        }

        protected override Dictionary<string, string> GetPrimitiveTypeMap()
        {
            return new Dictionary<string, string>
            {
                { "Integer.Byte", "uint8" },
                { "Integer.Int16", "int16" },
                { "Integer.Int32", "int32" },
                { "Integer.Int64", "int64" },
                { "Integer.UInt16", "uint16" },
                { "Integer.UInt32", "uint32" },
                { "Integer.UInt64", "uint64" },
                { "FloatingPoint.Decimal", "double" },
                { "FloatingPoint.Double", "double" },
                { "FloatingPoint.Single", "single" },
                { "DateTime.Date", "System.DateTime" },
                { "DateTime.DateTime", "System.DateTime" },
                { "DateTime.Time", "System.DateTime" },
                { "DateTime.TimeSpan", "System.TimeSpan" },
                { "Text.Char", "char" },
                { "Text.String", "char" },
                { "Other.Boolean", "logical" },
                { "Other.Guid", "System.Guid" }
            };
        }

        protected override void UpdateLibraryProjectFile(string projectPath, List<UserDefinedType> orderedInputTypes)
        {
            // openECA templates for MATLAB code do not use project files...
        }

        protected override void UpdateTestHarnessProjectFile(string projectPath)
        {
            // openECA templates for MATLAB code do not use project files...
        }

        private string GetDefaultDataValue(DataType type)
        {
            DataType underlyingType = (type as ArrayType)?.UnderlyingType ?? type;

            // Namespace: ProjectName.Model.Category
            // TypeName: Identifier
            if (!m_primitiveDefaultValues.TryGetValue($"{underlyingType.Category}.{underlyingType.Identifier}", out string defaultValue))
                defaultValue = $"{underlyingType.Identifier}()";

            return defaultValue;
        }

        private string GetDefaultMetaValue(DataType type)
        {
            DataType underlyingType = (type as ArrayType)?.UnderlyingType ?? type;

            // Namespace: ProjectName.Model.Category
            // TypeName: Identifier
            if (!m_primitiveDefaultValues.TryGetValue($"{underlyingType.Category}.{underlyingType.Identifier}", out _))
                return GetMetaIdentifier(underlyingType.Identifier);

            return "MetaValues()";
        }

        private string GetConversionFunction(DataType type, out bool forceToString)
        {
            DataType underlyingType = (type as ArrayType)?.UnderlyingType ?? type;

            // Namespace: ProjectName.Model.Category
            // TypeName: Identifier
            if (!m_primitiveConversionFunctions.TryGetValue($"{underlyingType.Category}.{underlyingType.Identifier}", out Tuple<string, bool> conversion))
                throw new InvalidOperationException($"Unexpected primitive type encountered: \"{underlyingType.Category}.{underlyingType.Identifier}\"");

            forceToString = conversion.Item2;
            return conversion.Item1;
        }

        protected override string GetMetaIdentifier(string identifier) => $"{identifier}Meta";


        #endregion
    }
}
