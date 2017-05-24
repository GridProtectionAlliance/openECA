//******************************************************************************************************
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
//  07/06/2016 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ECACommonUtilities;
using ECACommonUtilities.Model;
using GSF.Collections;

// ReSharper disable RedundantStringInterpolation
namespace ECAClientUtilities.Template.IronPython
{
    public class ProjectGenerator : DotNetProjectGeneratorBase
    {
        #region [ Members ]

        private readonly Dictionary<string, string> m_primitiveDefaultValues;
        private readonly Dictionary<string, Tuple<string, bool>> m_primitiveConversionFunctions;

        #endregion

        #region [ Constructors ]

        public ProjectGenerator(string projectName, MappingCompiler compiler) : base(projectName, compiler, "py", "IronPython")
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
                { "DateTime.Date", "DateTime.MinValue" },
                { "DateTime.DateTime", "DateTime.MinValue" },
                { "DateTime.Time", "DateTime.MinValue" },
                { "DateTime.TimeSpan", "TimeSpan.MinValue" },
                { "Text.Char", "chr(0)" },
                { "Text.String", "\"\"" },
                { "Other.Boolean", "false" },
                { "Other.Guid", "Guid.Empty" }
            };

            m_primitiveConversionFunctions = new Dictionary<string, Tuple<string, bool>>
            {
                { "Integer.Byte", new Tuple<string, bool>("int", false) },
                { "Integer.Int16", new Tuple<string, bool>("int", false) },
                { "Integer.Int32", new Tuple<string, bool>("int", false) },
                { "Integer.Int64", new Tuple<string, bool>("long", false) },
                { "Integer.UInt16", new Tuple<string, bool>("int", false) },
                { "Integer.UInt32", new Tuple<string, bool>("long", false) },
                { "Integer.UInt64", new Tuple<string, bool>("long", false) },
                { "FloatingPoint.Decimal", new Tuple<string, bool>("float", false) },
                { "FloatingPoint.Double", new Tuple<string, bool>("float", false) },
                { "FloatingPoint.Single", new Tuple<string, bool>("float", false) },
                { "DateTime.Date", new Tuple<string, bool>("DateTime.Parse", true) },
                { "DateTime.DateTime", new Tuple<string, bool>("DateTime.Parse", true) },
                { "DateTime.Time", new Tuple<string, bool>("DateTime.Parse", true) },
                { "DateTime.TimeSpan", new Tuple<string, bool>("TimeSpan.Parse", true) },
                { "Text.Char", new Tuple<string, bool>("chr", false) },
                { "Text.String", new Tuple<string, bool>("str", false) },
                { "Other.Boolean", new Tuple<string, bool>("int", false) },
                { "Other.Guid", new Tuple<string, bool>("Guid.Parse", true) }
            };
        }

        #endregion

        #region [ Methods ]

        protected override string ConstructDataModel(UserDefinedType type)
        {
            string fieldList = string.Join(Environment.NewLine, type.Fields
                .Select(field => $"    {field.Identifier} = {GetDefaultValue(field.Type)}"));

            // Generate the contents of the class file
            return GetTextFromResource("ECAClientUtilities.Template.IronPython.UDTTemplate.txt")
                .Replace("{ProjectName}", ProjectName)
                .Replace("{Category}", type.Category)
                .Replace("{Identifier}", type.Identifier)
                .Replace("{Fields}", fieldList.Trim());
        }

        protected override string ConstructMetaModel(UserDefinedType type)
        {
            string fieldList = string.Join(Environment.NewLine, type.Fields
                .Select(field => $"    {field.Identifier} = None"));

            // Generate the contents of the class file
            return GetTextFromResource("ECAClientUtilities.Template.IronPython.UDTTemplate.txt")
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

                    mappingCode.AppendLine($"        # Create {arrayTypeName} UDT array for \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"        MapperBase.PushCurrentFrame(self)");
                    mappingCode.AppendLine($"        arrayMapping = fieldLookup[\"{fieldIdentifier}\"]");
                    mappingCode.AppendLine($"        list = []");
                    mappingCode.AppendLine($"        count = MapperBase.GetUDTArrayTypeMappingCount(self, arrayMapping)");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"        for i in range(0, count):");
                    mappingCode.AppendLine($"            nestedMapping = MapperBase.GetUDTArrayTypeMapping(self, arrayMapping, i)");
                    mappingCode.AppendLine($"            list.append(self.Create{underlyingType.Category}{GetIdentifier(underlyingType, isMetaType)}(nestedMapping))");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"        obj.{fieldIdentifier} = list");
                    mappingCode.AppendLine($"        MapperBase.PopCurrentFrame(self)");
                }
                else if (fieldType.IsUserDefined)
                {
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    mappingCode.AppendLine($"        # Create {fieldTypeName} UDT for \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"        fieldMapping = fieldLookup[\"{fieldIdentifier}\"]");
                    mappingCode.AppendLine($"        nestedMapping = MapperBase.GetTypeMapping(self, fieldMapping)");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"        MapperBase.PushRelativeFrame(self, fieldMapping)");
                    mappingCode.AppendLine($"        obj.{fieldIdentifier} = self.Create{fieldType.Category}{GetIdentifier(fieldType, isMetaType)}(nestedMapping)");
                    mappingCode.AppendLine($"        MapperBase.PopRelativeFrame(self, fieldMapping)");
                }
                else if (fieldType.IsArray)
                {
                    bool forceToString;
                    string conversionFunction = GetConversionFunction(underlyingType, out forceToString);
                    string arrayTypeName = GetTypeName(underlyingType, isMetaType);

                    mappingCode.AppendLine($"        # Create {arrayTypeName} array for \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"        arrayMapping = fieldLookup[\"{fieldIdentifier}\"]");
                    mappingCode.AppendLine($"        list = []");
                    mappingCode.AppendLine($"        count = MapperBase.GetArrayMeasurementCount(self, arrayMapping)");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"        for i in range(0, count):");
                    mappingCode.AppendLine($"            measurement = MapperBase.GetArrayMeasurement(self, i)");
                    if (isMetaType)
                        mappingCode.AppendLine($"            list.append(MapperBase.GetMetaValues(self, measurement))");
                    else
                        mappingCode.AppendLine($"            list.append({conversionFunction}(measurement.Value{(forceToString ? ".ToString()" : "")}))");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"        obj.{fieldIdentifier} = list");
                }
                else
                {
                    bool forceToString;
                    string conversionFunction = GetConversionFunction(fieldType, out forceToString);
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    mappingCode.AppendLine($"        # Assign {fieldTypeName} value to \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"        fieldMapping = fieldLookup[\"{fieldIdentifier}\"]");
                    mappingCode.AppendLine($"        measurement = MapperBase.GetMeasurement(self, fieldMapping)");
                    if (isMetaType)
                        mappingCode.AppendLine($"        obj.{fieldIdentifier} = MapperBase.GetMetaValues(self, measurement)");
                    else
                        mappingCode.AppendLine($"        obj.{fieldIdentifier} = {conversionFunction}(measurement.Value{(forceToString ? ".ToString()" : "")})");
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

                    fillCode.AppendLine($"        # Initialize {arrayTypeName} UDT array for \"{fieldIdentifier}\" field");
                    fillCode.AppendLine($"        arrayMapping = fieldLookup[\"{fieldIdentifier}\"]");
                    fillCode.AppendLine($"        UnmapperBase.PushWindowFrameTime(self, arrayMapping)");
                    fillCode.AppendLine($"        list = []");
                    fillCode.AppendLine($"        count = UnmapperBase.GetUDTArrayTypeMappingCount(self, arrayMapping)");
                    fillCode.AppendLine();
                    fillCode.AppendLine($"        for i in range(0, count):");
                    fillCode.AppendLine($"            nestedMapping = UnmapperBase.GetUDTArrayTypeMapping(self, arrayMapping, i)");
                    fillCode.AppendLine($"            list.append(self.Fill{underlyingType.Category}{GetIdentifier(underlyingType, isMetaType)}(nestedMapping))");
                    fillCode.AppendLine();
                    fillCode.AppendLine($"        obj.{fieldIdentifier} = list");
                    fillCode.AppendLine($"        UnmapperBase.PopWindowFrameTime(self, arrayMapping)");
                }
                else if (fieldType.IsUserDefined)
                {
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    fillCode.AppendLine($"        # Initialize {fieldTypeName} UDT for \"{fieldIdentifier}\" field");
                    fillCode.AppendLine($"        fieldMapping = fieldLookup[\"{fieldIdentifier}\"]");
                    fillCode.AppendLine($"        nestedMapping = UnmapperBase.GetTypeMapping(self, fieldMapping)");
                    fillCode.AppendLine();
                    fillCode.AppendLine($"        UnmapperBase.PushRelativeFrame(self, fieldMapping)");
                    fillCode.AppendLine($"        obj.{fieldIdentifier} = self.Fill{fieldType.Category}{GetIdentifier(fieldType, isMetaType)}(nestedMapping)");
                    fillCode.AppendLine($"        UnmapperBase.PopRelativeFrame(self, fieldMapping)");
                }
                else if (fieldType.IsArray)
                {
                    bool forceToString;
                    string conversionFunction = GetConversionFunction(underlyingType, out forceToString);
                    string arrayTypeName = GetTypeName(underlyingType, isMetaType);

                    fillCode.AppendLine($"        # Initialize {arrayTypeName} array for \"{fieldIdentifier}\" field");
                    fillCode.AppendLine($"        arrayMapping = fieldLookup[\"{fieldIdentifier}\"]");
                    if (isMetaType)
                        fillCode.AppendLine($"        obj.{fieldIdentifier} = UnmapperBase.CreateMetaValues(self, arrayMapping)");
                    else
                        fillCode.AppendLine($"        obj.{fieldIdentifier} = [{GetDefaultValue(underlyingType)}]*UnmapperBase.GetArrayMeasurementCount(self, arrayMapping)");
                }
                else
                {
                    bool forceToString;
                    string conversionFunction = GetConversionFunction(fieldType, out forceToString);
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    if (isMetaType)
                    {
                        fillCode.AppendLine($"        # Assign {fieldTypeName} value to \"{fieldIdentifier}\" field");
                        fillCode.AppendLine($"        fieldMapping = fieldLookup[\"{fieldIdentifier}\"]");
                        fillCode.AppendLine($"        obj.{fieldIdentifier} = UnmapperBase.CreateMetaValues(self, fieldMapping)");
                    }
                    else
                    {
                        fillCode.AppendLine($"        # We don't need to do anything, but we burn a key index to keep our");
                        fillCode.AppendLine($"        # array index in sync with where we are in the data structure");
                        fillCode.AppendLine($"        UnmapperBase.BurnKeyIndex(self)");
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

                    unmappingCode.AppendLine($"        # Convert values from {arrayTypeName} UDT array for \"{fieldIdentifier}\" field to measurements");
                    unmappingCode.AppendLine($"        arrayMapping = fieldLookup[\"{fieldIdentifier}\"]");
                    unmappingCode.AppendLine($"        dataLength = len(data.{fieldIdentifier})");
                    unmappingCode.AppendLine($"        metaLength = len(meta.{fieldIdentifier})");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"        if dataLength != metaLength:");
                    unmappingCode.AppendLine($"            raise InvalidOperationException(\"Values array length (\" + dataLength + \") and MetaValues array length (\" + metaLength + \") for field \\\"{fieldIdentifier}\\\" must be the same.\")");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"        UnmapperBase.PushWindowFrameTime(self, arrayMapping)");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"        for i in range(0, dataLength):");
                    unmappingCode.AppendLine($"            nestedMapping = UnmapperBase.GetUDTArrayTypeMapping(self, arrayMapping, i)");
                    unmappingCode.AppendLine($"            self.CollectFrom{underlyingType.Category}{underlyingType.Identifier}(measurements, nestedMapping, data.{fieldIdentifier}[i], meta.{fieldIdentifier}[i])");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"        UnmapperBase.PopWindowFrameTime(self, arrayMapping)");
                }
                else if (fieldType.IsUserDefined)
                {
                    string fieldTypeName = GetTypeName(fieldType, false);

                    unmappingCode.AppendLine($"        # Convert values from {fieldTypeName} UDT for \"{fieldIdentifier}\" field to measurements");
                    unmappingCode.AppendLine($"        fieldMapping = fieldLookup[\"{fieldIdentifier}\"]");
                    unmappingCode.AppendLine($"        nestedMapping = MapperBase.GetTypeMapping(self, fieldMapping)");
                    unmappingCode.AppendLine($"        self.CollectFrom{fieldType.Category}{fieldType.Identifier}(measurements, nestedMapping, data.{fieldIdentifier}, meta.{fieldIdentifier})");
                }
                else if (fieldType.IsArray)
                {
                    unmappingCode.AppendLine($"        # Convert values from array in \"{fieldIdentifier}\" field to measurements");
                    unmappingCode.AppendLine($"        arrayMapping = fieldLookup[\"{fieldIdentifier}\"]");
                    unmappingCode.AppendLine($"        dataLength = len(data.{fieldIdentifier})");
                    unmappingCode.AppendLine($"        metaLength = len(meta.{fieldIdentifier})");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"        if dataLength != metaLength:");
                    unmappingCode.AppendLine($"            raise InvalidOperationException(\"Values array length (\" + dataLength + \") and MetaValues array length (\" + metaLength + \") for field \\\"{fieldIdentifier}\\\" must be the same.\")");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"        for i in range(0, dataLength):");
                    unmappingCode.AppendLine($"            measurement = UnmapperBase.MakeMeasurement(self, meta.{fieldIdentifier}[i], data.{fieldIdentifier}[i])");
                    unmappingCode.AppendLine($"            measurements.Add(measurement)");
                }
                else
                {
                    unmappingCode.AppendLine($"        # Convert value from \"{fieldIdentifier}\" field to measurement");
                    unmappingCode.AppendLine($"        fieldMapping = fieldLookup[\"{fieldIdentifier}\"]");
                    unmappingCode.AppendLine($"        measurement = UnmapperBase.MakeMeasurement(self, meta.{fieldIdentifier}, data.{fieldIdentifier})");
                    unmappingCode.AppendLine($"        measurements.Add(measurement)");
                }

                unmappingCode.AppendLine();
            }

            return unmappingCode.ToString();
        }

        protected override string ConstructUsing(UserDefinedType type)
        {
            return $"from Model.{type.Category} import";
        }

        protected override Dictionary<string, string> GetPrimitiveTypeMap()
        {
            return new Dictionary<string, string>
            {
                { "Integer.Byte", "int" },
                { "Integer.Int16", "int" },
                { "Integer.Int32", "int" },
                { "Integer.Int64", "long" },
                { "Integer.UInt16", "int" },
                { "Integer.UInt32", "long" },
                { "Integer.UInt64", "long" },
                { "FloatingPoint.Decimal", "float" },
                { "FloatingPoint.Double", "float" },
                { "FloatingPoint.Single", "float" },
                { "DateTime.Date", "DateTime" },
                { "DateTime.DateTime", "DateTime" },
                { "DateTime.Time", "DateTime" },
                { "DateTime.TimeSpan", "TimeSpan" },
                { "Text.Char", "int" },
                { "Text.String", "str" },
                { "Other.Boolean", "int" },
                { "Other.Guid", "Guid" }
            };
        }

        protected override string[] ExtraModelCategoryFiles(string modelPath, string categoryName)
        {
            const string ModuleFile = "__init__.py";

            string moduleFilePath;

            if (string.IsNullOrEmpty(categoryName))
                moduleFilePath = Path.Combine(modelPath, ModuleFile);
            else
                moduleFilePath = Path.Combine(modelPath, categoryName, ModuleFile);

            if (!File.Exists(moduleFilePath))
                using (File.Create(moduleFilePath)) { }

            if (string.IsNullOrEmpty(categoryName))
                return new [] { ModuleFile };

            return new [] { $@"{categoryName}\{ModuleFile}" };
        }

        private string GetConversionFunction(DataType type, out bool forceToString)
        {
            DataType underlyingType = (type as ArrayType)?.UnderlyingType ?? type;
            Tuple<string, bool> conversion;

            // Namespace: ProjectName.Model.Category
            // TypeName: Identifier
            if (!m_primitiveConversionFunctions.TryGetValue($"{underlyingType.Category}.{underlyingType.Identifier}", out conversion))
                throw new InvalidOperationException($"Unexpected primitive type encountered: \"{underlyingType.Category}.{underlyingType.Identifier}\"");

            forceToString = conversion.Item2;
            return conversion.Item1;
        }

        private string GetDefaultValue(DataType type)
        {
            return m_primitiveDefaultValues.GetOrDefault($"{type.Category}.{type.Identifier}", key => "None");
        }

        #endregion
    }
}
