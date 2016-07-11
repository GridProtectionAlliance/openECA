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
//  07/05/2016 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ECAClientUtilities.Model;

namespace ECAClientUtilities.Template.VisualBasic
{
    public class ProjectGenerator : DotNetProjectGeneratorBase
    {
        #region [ Members ]

        private readonly Dictionary<string, Tuple<string, bool>> m_primitiveConversionFunctions;

        #endregion

        #region [ Constructors ]

        public ProjectGenerator(string projectName, MappingCompiler compiler) : base(projectName, compiler, "vb", "VisualBasic", "()")
        {
            m_primitiveConversionFunctions = new Dictionary<string, Tuple<string, bool>>
            {
                { "Integer.Byte", new Tuple<string, bool>("CByte", false) },
                { "Integer.Int16", new Tuple<string, bool>("CShort", false) },
                { "Integer.Int32", new Tuple<string, bool>("CInt", false) },
                { "Integer.Int64", new Tuple<string, bool>("CLng", false) },
                { "Integer.UInt16", new Tuple<string, bool>("CUShort", false) },
                { "Integer.UInt32", new Tuple<string, bool>("CUInt", false) },
                { "Integer.UInt64", new Tuple<string, bool>("CULng", false) },
                { "FloatingPoint.Decimal", new Tuple<string, bool>("CDec", false) },
                { "FloatingPoint.Double", new Tuple<string, bool>("CDbl", false) },
                { "FloatingPoint.Single", new Tuple<string, bool>("CSng", false) },
                { "DateTime.Date", new Tuple<string, bool>("CDate", false) },
                { "DateTime.DateTime", new Tuple<string, bool>("CDate", false) },
                { "DateTime.Time", new Tuple<string, bool>("CDate", true) },
                { "DateTime.TimeSpan", new Tuple<string, bool>("TimeSpan.Parse", true) },
                { "Text.Char", new Tuple<string, bool>("CChar", false) },
                { "Text.String", new Tuple<string, bool>("CStr", false) },
                { "Other.Boolean", new Tuple<string, bool>("CBool", false) },
                { "Other.Guid", new Tuple<string, bool>("Guid.Parse", true) }
            };
        }

        #endregion

        #region [ Methods ]

        protected override string ConstructModel(UserDefinedType type)
        {
            string fieldList = string.Join(Environment.NewLine, type.Fields
                .Select(field => $"    Public Property {field.Identifier} As {GetTypeName(field.Type)}"));

            // Generate the contents of the class file
            return GetTextFromResource("ECAClientUtilities.Template.VisualBasic.UDTTemplate.txt")
                .Replace("{ProjectName}", ProjectName)
                .Replace("{Category}", type.Category)
                .Replace("{Identifier}", type.Identifier)
                .Replace("{Fields}", fieldList.Trim());
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
                    mappingCode.AppendLine($"            obj.{field.Identifier} = MappingCompiler.EnumerateTypeMappings(fieldLookup(\"{field.Identifier}\").Expression).Select(AddressOf Create{underlyingType.Category}{underlyingType.Identifier}).ToArray()");
                }
                else if (fieldType.IsUserDefined)
                {
                    mappingCode.AppendLine($"            obj.{field.Identifier} = Create{field.Type.Category}{field.Type.Identifier}(MappingCompiler.GetTypeMapping(fieldLookup(\"{field.Identifier}\").Expression))");
                }
                else if (fieldType.IsArray)
                {
                    bool forceToString;
                    string conversionFunction = GetConversionFunction(underlyingType, out forceToString);

                    mappingCode.AppendLine($"            obj.{field.Identifier} = SignalLookup.GetMeasurements(Keys(m_index)).Select(Function(measurement) {conversionFunction}(measurement.Value{(forceToString ? ".ToString()" : "")})).ToArray()");
                    mappingCode.AppendLine("            m_index = m_index + 1");
                }
                else
                {
                    bool forceToString;
                    string conversionFunction = GetConversionFunction(field.Type, out forceToString);

                    mappingCode.AppendLine($"            obj.{field.Identifier} = {conversionFunction}(SignalLookup.GetMeasurement(Keys(m_index)(0)).Value{(forceToString ? ".ToString()" : "")})");
                    mappingCode.AppendLine("            m_index = m_index + 1");
                }

                mappingCode.AppendLine();
            }

            return mappingCode.ToString();
        }

        protected override string ConstructUsing(UserDefinedType type)
        {
            return $"Imports {ProjectName}.Model.{type.Category}";
        }

        protected override Dictionary<string, string> GetPrimitiveTypeMap()
        {
            return new Dictionary<string, string>
            {
                { "Integer.Byte", "Byte" },
                { "Integer.Int16", "Short" },
                { "Integer.Int32", "Integer" },
                { "Integer.Int64", "Long" },
                { "Integer.UInt16", "UShort" },
                { "Integer.UInt32", "UInteger" },
                { "Integer.UInt64", "ULong" },
                { "FloatingPoint.Decimal", "Decimal" },
                { "FloatingPoint.Double", "Double" },
                { "FloatingPoint.Single", "Single" },
                { "DateTime.Date", "Date" },
                { "DateTime.DateTime", "Date" },
                { "DateTime.Time", "Date" },
                { "DateTime.TimeSpan", "TimeSpan" },
                { "Text.Char", "Char" },
                { "Text.String", "String" },
                { "Other.Boolean", "Boolean" },
                { "Other.Guid", "Guid" }
            };
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

        #endregion
    }
}
