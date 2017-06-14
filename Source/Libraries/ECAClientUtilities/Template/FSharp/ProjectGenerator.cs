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
//  05/31/2016 - Stephen Wills
//       Generated original version of source code.
//  07/03/2016 - J. Ritchie Carroll
//       Converted from C# project generator
//  03/15/2017 - Matt Donnelly
//		 Patch bug: added closing paren for fieldType.IsArray section at GetMetaValues.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ECACommonUtilities;
using ECACommonUtilities.Model;

// ReSharper disable RedundantStringInterpolation
namespace ECAClientUtilities.Template.FSharp
{
    public class ProjectGenerator : DotNetProjectGeneratorBase
    {
        #region [ Members ]

        private readonly Dictionary<string, string> m_primitiveDefaultValues;

        #endregion

        #region [ Constructors ]

        public ProjectGenerator(string projectName, MappingCompiler compiler) : base(projectName, compiler, "fs", "FSharp")
        {
            m_primitiveDefaultValues = new Dictionary<string, string>()
            {
                { "Integer.Byte", "0uy" },
                { "Integer.Int16", "0s" },
                { "Integer.Int32", "0" },
                { "Integer.Int64", "0L" },
                { "Integer.UInt16", "0us" },
                { "Integer.UInt32", "0u" },
                { "Integer.UInt64", "0UL" },
                { "FloatingPoint.Decimal", "0.0M" },
                { "FloatingPoint.Double", "0.0" },
                { "FloatingPoint.Single", "0.0F" },
                { "DateTime.Date", "System.DateTime.MinValue" },
                { "DateTime.DateTime", "System.DateTime.MinValue" },
                { "DateTime.Time", "System.DateTime.MinValue" },
                { "DateTime.TimeSpan", "System.TimeSpan.MinValue" },
                { "Text.Char", "System.Char.MinValue" },
                { "Text.String", "\"\"" },
                { "Other.Boolean", "false" },
                { "Other.Guid", "System.Guid.Empty" }
            };
        }

        #endregion

        #region [ Methods ]

        protected override string ConstructDataModel(UserDefinedType type)
        {
            // Build the list of fields as properties of the generated class
            StringBuilder fieldList = new StringBuilder();
            StringBuilder propertyList = new StringBuilder();

            foreach (UDTField field in type.Fields)
            {
                string fieldName = GetParameterName(field.Identifier);
                fieldList.AppendLine($"    let mutable m_{fieldName} : {GetDataTypeName(field.Type)} = {fieldName}");
                propertyList.AppendLine($"    member public this.{field.Identifier} with get() = m_{fieldName} and set(value) = m_{fieldName} <- value");
            }

            string constructorParams = string.Join(", ", type.Fields
                .Select(param => $"{GetParameterName(param.Identifier)} : {GetDataTypeName(param.Type)}"));

            string defaultConstructorParams = string.Join(", ", type.Fields
                .Select(param => $"{GetDefaultDataValue(param.Type)}"));

            // Generate the contents of the class file
            return GetTextFromResource("ECAClientUtilities.Template.FSharp.UDTDataTemplate.txt")
                .Replace("{ProjectName}", ProjectName)
                .Replace("{Category}", type.Category)
                .Replace("{Identifier}", type.Identifier)
                .Replace("{Fields}", $"{fieldList}{Environment.NewLine}{propertyList}")
                .Replace("{ConstructorParams}", constructorParams)
                .Replace("{DefaultConstructorParams}", defaultConstructorParams);
        }

        protected override string ConstructMetaModel(UserDefinedType type)
        {
            // Build the list of fields as properties of the generated class
            StringBuilder fieldList = new StringBuilder();
            StringBuilder propertyList = new StringBuilder();

            foreach (UDTField field in type.Fields)
            {
                string fieldName = GetParameterName(field.Identifier);
                fieldList.AppendLine($"    let mutable m_{fieldName} : {GetMetaTypeName(field.Type)} = {fieldName}");
                propertyList.AppendLine($"    member public this.{field.Identifier} with get() = m_{fieldName} and set(value) = m_{fieldName} <- value");
            }

            string constructorParams = string.Join(", ", type.Fields
                .Select(param => $"{GetParameterName(param.Identifier)} : {GetMetaTypeName(param.Type)}"));

            string defaultConstructorParams = string.Join(", ", type.Fields
                .Select(param => $"{GetDefaultMetaValue(param.Type)}"));

            // Generate the contents of the class file
            return GetTextFromResource("ECAClientUtilities.Template.FSharp.UDTMetaTemplate.txt")
                .Replace("{ProjectName}", ProjectName)
                .Replace("{Category}", type.Category)
                .Replace("{Identifier}", GetMetaIdentifier(type.Identifier))
                .Replace("{Fields}", $"{fieldList}{Environment.NewLine}{propertyList}")
                .Replace("{ConstructorParams}", constructorParams)
                .Replace("{DefaultConstructorParams}", defaultConstructorParams);
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

                    mappingCode.AppendLine($"        do");
                    mappingCode.AppendLine($"            // Create {arrayTypeName} UDT array for \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"            let arrayMapping = fieldLookup.Item(\"{fieldIdentifier}\") :?> ArrayMapping");
                    mappingCode.AppendLine($"            base.PushWindowFrame(arrayMapping)");
                    mappingCode.AppendLine($"            let count = base.GetUDTArrayTypeMappingCount(arrayMapping)");
                    mappingCode.AppendLine();
                    // This loop from 1 to count is properly offset in the local member methods that shadow mapper base functions,
                    // this is required for calling base functions from within an F# lambda expression (see MapperTemplate.txt)
                    mappingCode.AppendLine($"            let list = [1..count] |> List.map(fun i ->");
                    mappingCode.AppendLine($"                let nestedMapping = this.GetUDTArrayTypeMapping(arrayMapping, i)");
                    mappingCode.AppendLine($"                this.Create{underlyingType.Category}{GetIdentifier(underlyingType, isMetaType)}(nestedMapping))");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"            obj.{fieldIdentifier} <- list.ToArray()");
                    mappingCode.AppendLine($"            base.PopWindowFrame(arrayMapping)");
                }
                else if (fieldType.IsUserDefined)
                {
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    mappingCode.AppendLine($"        do");
                    mappingCode.AppendLine($"            // Create {fieldTypeName} UDT for \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"            let fieldMapping = fieldLookup.Item(\"{fieldIdentifier}\")");
                    mappingCode.AppendLine($"            let nestedMapping = base.GetTypeMapping(fieldMapping)");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"            base.PushRelativeFrame(fieldMapping)");
                    mappingCode.AppendLine($"            obj.{fieldIdentifier} <- this.Create{fieldType.Category}{GetIdentifier(fieldType, isMetaType)}(nestedMapping)");
                    mappingCode.AppendLine($"            base.PopRelativeFrame(fieldMapping)");
                }
                else if (fieldType.IsArray)
                {
                    string arrayTypeName = GetTypeName(underlyingType, isMetaType);

                    mappingCode.AppendLine($"        do");
                    mappingCode.AppendLine($"            // Create {arrayTypeName} array for \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"            let arrayMapping = fieldLookup.Item(\"{fieldIdentifier}\") :?> ArrayMapping");
                    mappingCode.AppendLine($"            let count = base.GetArrayMeasurementCount(arrayMapping)");
                    mappingCode.AppendLine();
                    // This loop from 1 to count is properly offset in the local member methods that shadow mapper base functions,
                    // this is required for calling base functions from within an F# lambda expression (see MapperTemplate.txt)
                    mappingCode.AppendLine($"            let list = [1..count] |> List.map(fun i ->");
                    mappingCode.AppendLine($"                let measurement = this.GetArrayMeasurement(i)");
                    if (isMetaType)
                        mappingCode.AppendLine($"                this.GetMetaValues(measurement))");
                    else
                        mappingCode.AppendLine($"                Convert.ChangeType(measurement.Value, typedefof<{arrayTypeName}>) :?> {arrayTypeName})");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"            obj.{fieldIdentifier} <- list.ToArray()");
                }
                else
                {
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    mappingCode.AppendLine($"        do");
                    mappingCode.AppendLine($"            // Assign {fieldTypeName} value to \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"            let fieldMapping = fieldLookup.Item(\"{fieldIdentifier}\")");
                    mappingCode.AppendLine($"            let measurement = base.GetMeasurement(fieldMapping)");
                    if (isMetaType)
                        mappingCode.AppendLine($"            obj.{fieldIdentifier} <- base.GetMetaValues(measurement)");
                    else
                        mappingCode.AppendLine($"            obj.{fieldIdentifier} <- Convert.ChangeType(measurement.Value, typedefof<{fieldTypeName}>) :?> {fieldTypeName}");
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
                DataType underlyingType = (field.Type as ArrayType)?.UnderlyingType;
                string fieldIdentifier = field.Identifier;

                // For user-defined types, call the method to generate an object of their corresponding data type
                // For primitive types, do nothing; but in the case of meta types, generate meta value structures
                // ReSharper disable once PossibleNullReferenceException
                if (fieldType.IsArray && underlyingType.IsUserDefined)
                {
                    string arrayTypeName = GetTypeName(underlyingType, isMetaType);

                    fillCode.AppendLine($"        do");
                    fillCode.AppendLine($"            // Initialize {arrayTypeName} UDT array for \"{fieldIdentifier}\" field");
                    fillCode.AppendLine($"            let arrayMapping = fieldLookup.Item(\"{fieldIdentifier}\") :?> ArrayMapping");
                    fillCode.AppendLine($"            base.PushWindowFrameTime(arrayMapping)");
                    fillCode.AppendLine($"            let count = base.GetUDTArrayTypeMappingCount(arrayMapping)");
                    fillCode.AppendLine();
                    // This loop from 1 to count is properly offset in the local member methods that shadow mapper base functions,
                    // this is required for calling base functions from within an F# lambda expression (see UnmapperTemplate.txt)
                    fillCode.AppendLine($"            let list = [1..count] |> List.map(fun i ->");
                    fillCode.AppendLine($"                let nestedMapping = this.GetUDTArrayTypeMapping(arrayMapping, i)");
                    fillCode.AppendLine($"                this.Fill{underlyingType.Category}{GetIdentifier(underlyingType, isMetaType)}(nestedMapping))");
                    fillCode.AppendLine();
                    fillCode.AppendLine($"            obj.{fieldIdentifier} <- list.ToArray()");
                    fillCode.AppendLine($"            base.PopWindowFrameTime(arrayMapping)");
                }
                else if (fieldType.IsUserDefined)
                {
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    fillCode.AppendLine($"        do");
                    fillCode.AppendLine($"            // Initialize {fieldTypeName} UDT for \"{fieldIdentifier}\" field");
                    fillCode.AppendLine($"            let fieldMapping = fieldLookup.Item(\"{fieldIdentifier}\")");
                    fillCode.AppendLine($"            let nestedMapping = base.GetTypeMapping(fieldMapping)");
                    fillCode.AppendLine();
                    fillCode.AppendLine($"            base.PushRelativeFrameTime(fieldMapping)");
                    // MKD 6/14/2017: Added self identifier "this" to Fill{fieldType.Category}{GetIdentifier(fieldType, isMetaType)} method.
                    fillCode.AppendLine($"            obj.{fieldIdentifier} <- this.Fill{fieldType.Category}{GetIdentifier(fieldType, isMetaType)}(nestedMapping)");
                    fillCode.AppendLine($"            base.PopRelativeFrameTime(fieldMapping)");
                }
                else if (fieldType.IsArray)
                {
                    string defaultDataValue = GetDefaultDataValue(underlyingType);

                    fillCode.AppendLine($"        do");
                    fillCode.AppendLine($"            // Initialize array for \"{fieldIdentifier}\" field");
                    fillCode.AppendLine($"            let arrayMapping = fieldLookup.Item(\"{fieldIdentifier}\") :?> ArrayMapping");
                    if (isMetaType)
                        fillCode.AppendLine($"            obj.{fieldIdentifier} <- base.CreateMetaValues(arrayMapping).ToArray()");
                    else
                        fillCode.AppendLine($"            obj.{fieldIdentifier} <- Array.create (base.GetArrayMeasurementCount arrayMapping) {defaultDataValue}");
                }
                else
                {
                    fillCode.AppendLine($"        do");
                    if (isMetaType)
                    {
                        fillCode.AppendLine($"            // Initialize meta value structure to \"{fieldIdentifier}\" field");
                        fillCode.AppendLine($"            let fieldMapping = fieldLookup.Item(\"{fieldIdentifier}\")");
                        fillCode.AppendLine($"            obj.{fieldIdentifier} <- base.CreateMetaValues(fieldMapping)");
                    }
                    else
                    {
                        fillCode.AppendLine($"            // We don't need to do anything, but we burn a key index to keep our");
                        fillCode.AppendLine($"            // array index in sync with where we are in the data structure");
                        fillCode.AppendLine($"            base.BurnKeyIndex()");
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
                DataType underlyingType = (field.Type as ArrayType)?.UnderlyingType;
                string fieldIdentifier = field.Identifier;

                // For user-defined types, call the method to collect measurements from object of their corresponding data type
                // For primitive types, call the method to get the values of the mapped measurements
                // ReSharper disable once PossibleNullReferenceException
                if (fieldType.IsArray && underlyingType.IsUserDefined)
                {
                    string arrayTypeName = GetTypeName(underlyingType, false);

                    unmappingCode.AppendLine($"        do");
                    unmappingCode.AppendLine($"            // Convert values from {arrayTypeName} UDT array for \"{fieldIdentifier}\" field to measurements");
                    unmappingCode.AppendLine($"            let arrayMapping = fieldLookup.Item(\"{fieldIdentifier}\") :?> ArrayMapping");
                    unmappingCode.AppendLine($"            let dataLength = data.{fieldIdentifier}.Length");
                    unmappingCode.AppendLine($"            let metaLength = meta.{fieldIdentifier}.Length");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"            if dataLength <> metaLength then raise (new InvalidOperationException(\"Values array length (\" + dataLength.ToString() + \") and MetaValues array length (\" + metaLength.ToString() + \") for field \\\"{fieldIdentifier}\\\" must be the same.\"))");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"            base.PushWindowFrameTime(arrayMapping)");
                    unmappingCode.AppendLine();
                    // This loop from 1 to count is properly offset in the local member methods that shadow mapper base functions,
                    // this is required for calling base functions from within an F# lambda expression (see UnmapperTemplate.txt)
                    unmappingCode.AppendLine($"            let list = [1..dataLength] |> List.map(fun j ->");
                    unmappingCode.AppendLine($"                let i = j - 1");
                    unmappingCode.AppendLine($"                let nestedMapping = this.GetUDTArrayTypeMapping(arrayMapping, j)");
                    unmappingCode.AppendLine($"                this.CollectFrom{underlyingType.Category}{underlyingType.Identifier}(measurements, nestedMapping, data.{fieldIdentifier}.[i], meta.{fieldIdentifier}.[i]))");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"            base.PopWindowFrameTime(arrayMapping)");
                }
                else if (fieldType.IsUserDefined)
                {
                    string fieldTypeName = GetTypeName(fieldType, false);

                    unmappingCode.AppendLine($"        do");
                    unmappingCode.AppendLine($"            // Convert values from {fieldTypeName} UDT for \"{fieldIdentifier}\" field to measurements");
                    unmappingCode.AppendLine($"            let fieldMapping = fieldLookup.Item(\"{fieldIdentifier}\")");
                    // MKD 6/14/2017: Added base class identifier "base" to GetTypeMapping method.
                    unmappingCode.AppendLine($"            let nestedMapping = base.GetTypeMapping(fieldMapping)");
                    unmappingCode.AppendLine($"            this.CollectFrom{fieldType.Category}{fieldType.Identifier}(measurements, nestedMapping, data.{fieldIdentifier}, meta.{fieldIdentifier})");
                }
                else if (fieldType.IsArray)
                {
                    unmappingCode.AppendLine($"        do");
                    unmappingCode.AppendLine($"            // Convert values from array in \"{fieldIdentifier}\" field to measurements");
                    unmappingCode.AppendLine($"            let arrayMapping = fieldLookup.Item(\"{fieldIdentifier}\") :?> ArrayMapping");
                    unmappingCode.AppendLine($"            let dataLength = data.{fieldIdentifier}.Length");
                    unmappingCode.AppendLine($"            let metaLength = meta.{fieldIdentifier}.Length");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"            if dataLength <> metaLength then raise (new InvalidOperationException(\"Values array length (\" + dataLength.ToString() + \") and MetaValues array length (\" + metaLength.ToString() + \") for field \\\"{fieldIdentifier}\\\" must be the same.\"))");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"            for j in [1..dataLength] do");
                    unmappingCode.AppendLine($"                let i = j - 1");
                    unmappingCode.AppendLine($"                let measurement = this.MakeMeasurement(meta.{fieldIdentifier}.[i], (double)data.{fieldIdentifier}.[i])");
                    unmappingCode.AppendLine($"                measurements.Add(measurement)");
                }
                else
                {
                    unmappingCode.AppendLine($"        do");
                    unmappingCode.AppendLine($"            // Convert value from \"{fieldIdentifier}\" field to measurement");
                    unmappingCode.AppendLine($"            let fieldMapping = fieldLookup.Item(\"{fieldIdentifier}\")");
                    unmappingCode.AppendLine($"            let measurement = this.MakeMeasurement(meta.{fieldIdentifier}, (double)data.{fieldIdentifier})");
                    unmappingCode.AppendLine($"            measurements.Add(measurement)");
                }

                unmappingCode.AppendLine();
            }

            return unmappingCode.ToString();
        }

        protected override string ConstructUsing(UserDefinedType type)
        {
            return $"open {ProjectName}.Model.{type.Category}";
        }

        protected override Dictionary<string, string> GetPrimitiveTypeMap()
        {
            return new Dictionary<string, string>
            {
                { "Integer.Byte", "byte" },
                { "Integer.Int16", "int16" },
                { "Integer.Int32", "int" },
                { "Integer.Int64", "int64" },
                { "Integer.UInt16", "uint16" },
                { "Integer.UInt32", "uint32" },
                { "Integer.UInt64", "uint64" },
                { "FloatingPoint.Decimal", "decimal" },
                { "FloatingPoint.Double", "float" },
                { "FloatingPoint.Single", "float32" },
                { "DateTime.Date", "System.DateTime" },
                { "DateTime.DateTime", "System.DateTime" },
                { "DateTime.Time", "System.TimeSpan" },
                { "DateTime.TimeSpan", "System.TimeSpan" },
                { "Text.Char", "char" },
                { "Text.String", "string" },
                { "Other.Boolean", "bool" },
                { "Other.Guid", "System.Guid" }
            };
        }

        private string GetParameterName(string fieldName)
        {
            return char.ToLower(fieldName[0]) + fieldName.Substring(1);
        }

        private string GetDefaultDataValue(DataType type)
        {
            DataType underlyingType = (type as ArrayType)?.UnderlyingType ?? type;
            string defaultValue;

            // Namespace: ProjectName.Model.Category
            // TypeName: Identifier
            if (!m_primitiveDefaultValues.TryGetValue($"{underlyingType.Category}.{underlyingType.Identifier}", out defaultValue))
                defaultValue = $"new {ProjectName}.Model.{underlyingType.Category}.{underlyingType.Identifier}()";

            if (type.IsArray)
                defaultValue = $"[| {defaultValue} |]";

            return defaultValue;
        }

        private string GetDefaultMetaValue(DataType type)
        {
            DataType underlyingType = (type as ArrayType)?.UnderlyingType ?? type;
            string defaultValue;

            // Namespace: ProjectName.Model.Category
            // TypeName: Identifier
            if (!m_primitiveDefaultValues.TryGetValue($"{underlyingType.Category}.{underlyingType.Identifier}", out defaultValue))
            {
                if (type.IsArray)
                    return $"[| new {ProjectName}.Model.{underlyingType.Category}.{GetMetaIdentifier(underlyingType.Identifier)}() |]";

                return $"new {ProjectName}.Model.{underlyingType.Category}.{GetMetaIdentifier(underlyingType.Identifier)}()";
            }

            if (type.IsArray)
                return "[| new MetaValues() |]";

            return "new MetaValues()";
        }

        #endregion
    }
}
