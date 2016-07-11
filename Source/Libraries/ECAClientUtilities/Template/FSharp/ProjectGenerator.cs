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
using ECAClientUtilities.Model;

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
                { "Text.Char", "System.Char.MinValue" },
                { "Text.String", "\"\"" },
                { "Other.Boolean", "false" },
                { "Other.Guid", "System.Guid.Empty" }
            };
        }

        #endregion

        #region [ Methods ]

        protected override string ConstructModel(UserDefinedType type)
        {
            // Build the list of fields as properties of the generated class
            StringBuilder fieldList = new StringBuilder();
            StringBuilder propertyList = new StringBuilder();

            foreach (UDTField field in type.Fields)
            {
                string fieldName = GetParameterName(field.Identifier);
                fieldList.AppendLine($"    let mutable m_{fieldName} : {GetTypeName(field.Type)} = {fieldName}");
                propertyList.AppendLine($"    member public this.{field.Identifier} with get() = m_{fieldName} and set(value) = m_{fieldName} <- value");
            }

            string constructorParams = string.Join(", ", type.Fields
                .Select(param => $"{GetParameterName(param.Identifier)} : {GetTypeName(param.Type)}"));

            string defaultConstructorParams = string.Join(", ", type.Fields
                .Select(param => $"{GetDefaultValue(param.Type)}"));

            // Generate the contents of the class file
            return GetTextFromResource("ECAClientUtilities.Template.FSharp.UDTTemplate.txt")
                .Replace("{ProjectName}", ProjectName)
                .Replace("{Category}", type.Category)
                .Replace("{Identifier}", type.Identifier)
                .Replace("{Fields}", $"{fieldList}{Environment.NewLine}{propertyList}")
                .Replace("{ConstructorParams}", constructorParams)
                .Replace("{DefaultConstructorParams}", defaultConstructorParams);
        }

        protected override string ConstructMapping(UserDefinedType type)
        {
            StringBuilder mappingCode = new StringBuilder();

            foreach (UDTField field in type.Fields)
            {
                // Get the field type and its
                // underlying type if it is an array
                DataType fieldType = field.Type;
                DataType underlyingType = (field.Type as ArrayType)?.UnderlyingType;

                // For user-defined types, call the method to generate an object of their corresponding data type
                // For primitive types, call the method to get the values of the mapped measurements
                // ReSharper disable once PossibleNullReferenceException
                if (fieldType.IsArray && underlyingType.IsUserDefined)
                {
                    mappingCode.AppendLine($"        obj.{field.Identifier} <- this.MappingCompiler.EnumerateTypeMappings(fieldLookup.Item(\"{field.Identifier}\").Expression).Select(fun typeMapping -> this.Create{underlyingType.Category}{underlyingType.Identifier}(typeMapping)).ToArray()");
                }
                else if (fieldType.IsUserDefined)
                {
                    mappingCode.AppendLine($"        obj.{field.Identifier} <- this.Create{field.Type.Category}{field.Type.Identifier}(this.MappingCompiler.GetTypeMapping(fieldLookup.Item(\"{field.Identifier}\").Expression))");
                }
                else if (fieldType.IsArray)
                {
                    string arrayTypeName = GetTypeName(underlyingType);
                    mappingCode.AppendLine($"        obj.{field.Identifier} <- this.SignalLookup.GetMeasurements(this.Keys.[m_index]).Select(fun measurement -> Convert.ChangeType(measurement.Value, typedefof<{arrayTypeName}>) :?> {arrayTypeName}).ToArray()");
                    mappingCode.AppendLine("        m_index <- m_index + 1");
                }
                else
                {
                    string fieldTypeName = GetTypeName(field.Type);
                    mappingCode.AppendLine($"        obj.{field.Identifier} <- Convert.ChangeType(this.SignalLookup.GetMeasurement(this.Keys.[m_index].[0]).Value, typedefof<{fieldTypeName}>) :?> {fieldTypeName}");
                    mappingCode.AppendLine("        m_index <- m_index + 1");
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

        private string GetDefaultValue(DataType type)
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

        #endregion
    }
}
