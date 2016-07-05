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
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ECAClientUtilities.Model;

namespace ECAClientUtilities.Template.CSharp
{
    public class ProjectGenerator : DotNetProjectGeneratorBase
    {
        #region [ Constructors ]

        public ProjectGenerator(string projectName, MappingCompiler compiler) : base(projectName, compiler, "cs", "CSharp")
        {
        }

        #endregion

        #region [ Methods ]

        protected override string ConstructModel(UserDefinedType type)
        {
            string fieldList = string.Join(Environment.NewLine, type.Fields
                .Select(field => $"        public {GetTypeName(field.Type)} {field.Identifier} {{ get; set; }}"));

            // Generate the contents of the class file
            return GetTextFromResource("ECAClientUtilities.Template.CSharp.UDTTemplate.txt")
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
                    mappingCode.AppendLine($"			obj.{field.Identifier} = m_mappingCompiler.EnumerateTypeMappings(fieldLookup[\"{field.Identifier}\"].Expression).Select(Create{underlyingType.Category}{underlyingType.Identifier}).ToArray();");
                else if (fieldType.IsUserDefined)
                    mappingCode.AppendLine($"			obj.{field.Identifier} = Create{field.Type.Category}{field.Type.Identifier}(m_mappingCompiler.GetTypeMapping(fieldLookup[\"{field.Identifier}\"].Expression));");
                else if (fieldType.IsArray)
                    mappingCode.AppendLine($"			obj.{field.Identifier} = m_lookup.GetMeasurements(m_keys[m_index++]).Select(measurement => ({GetTypeName(underlyingType)})measurement.Value).ToArray();");
                else
                    mappingCode.AppendLine($"			obj.{field.Identifier} = ({GetTypeName(field.Type)})m_lookup.GetMeasurement(m_keys[m_index++][0]).Value;");
            }

            return mappingCode.ToString();
        }

        protected override string ConstructUsing(UserDefinedType type)
        {
            return $"using {ProjectName}.Model.{type.Category};";
        }

        protected override Dictionary<string, string> GetPrimitiveTypeMap()
        {
            return new Dictionary<string, string>
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
        }

        #endregion
    }
}
