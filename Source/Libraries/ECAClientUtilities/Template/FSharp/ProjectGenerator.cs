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
                    mappingCode.AppendLine($"            base.PushCurrentFrame()");
                    mappingCode.AppendLine($"            let arrayMapping = fieldLookup.Item(\"{fieldIdentifier}\") :?> ArrayMapping");
                    mappingCode.AppendLine($"            let count = base.GetUDTArrayTypeMappingCount(arrayMapping)");
                    mappingCode.AppendLine();
                    // This loop from 1 to count is properly offset in the local member methods that shadow mapper base functions,
                    // this is required for calling base functions from within an F# lambda expression (see MapperTemplate.txt)
                    mappingCode.AppendLine($"            let list = [1..count] |> List.map(fun i ->");
                    mappingCode.AppendLine($"                let nestedMapping = this.GetUDTArrayTypeMapping(arrayMapping, i)");
                    mappingCode.AppendLine($"                this.Create{underlyingType.Category}{GetIdentifier(underlyingType, isMetaType)}(nestedMapping))");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"            obj.{fieldIdentifier} <- list.ToArray()");
                    mappingCode.AppendLine($"            base.PopCurrentFrame()");
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
                        mappingCode.AppendLine($"                this.GetMetaValues(measurement)");
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
