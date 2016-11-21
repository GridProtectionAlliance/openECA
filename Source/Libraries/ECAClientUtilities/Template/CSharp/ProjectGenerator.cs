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
using ECACommonUtilities;
using ECACommonUtilities.Model;

// ReSharper disable RedundantStringInterpolation
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

        protected override string ConstructDataModel(UserDefinedType type)
        {
            string fieldList = string.Join(Environment.NewLine, type.Fields
                .Select(field => $"        public {GetDataTypeName(field.Type)} {field.Identifier} {{ get; set; }}"));

            // Generate the contents of the class file
            return GetTextFromResource("ECAClientUtilities.Template.CSharp.UDTDataTemplate.txt")
                .Replace("{ProjectName}", ProjectName)
                .Replace("{Category}", type.Category)
                .Replace("{Identifier}", type.Identifier)
                .Replace("{Fields}", fieldList.Trim());
        }

        protected override string ConstructMetaModel(UserDefinedType type)
        {
            string fieldList = string.Join(Environment.NewLine, type.Fields
                .Select(field => $"        public {GetMetaTypeName(field.Type)} {field.Identifier} {{ get; set; }}"));

            // Generate the contents of the class file
            return GetTextFromResource("ECAClientUtilities.Template.CSharp.UDTMetaTemplate.txt")
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
                DataType underlyingType = (field.Type as ArrayType)?.UnderlyingType;
                string fieldIdentifier = field.Identifier;

                // For user-defined types, call the method to generate an object of their corresponding data type
                // For primitive types, call the method to get the values of the mapped measurements
                // ReSharper disable once PossibleNullReferenceException
                if (fieldType.IsArray && underlyingType.IsUserDefined)
                {
                    string arrayTypeName = GetTypeName(underlyingType, isMetaType);

                    mappingCode.AppendLine($"            {{");
                    mappingCode.AppendLine($"                // Create {arrayTypeName} UDT array for \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"                PushCurrentFrame();");
                    mappingCode.AppendLine($"                ArrayMapping arrayMapping = (ArrayMapping)fieldLookup[\"{fieldIdentifier}\"];");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"                List<{arrayTypeName}> list = new List<{arrayTypeName}>();");
                    mappingCode.AppendLine($"                int count = GetUDTArrayTypeMappingCount(arrayMapping);");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"                for (int i = 0; i < count; i++)");
                    mappingCode.AppendLine($"                {{");
                    mappingCode.AppendLine($"                    TypeMapping nestedMapping = GetUDTArrayTypeMapping(arrayMapping, i);");
                    mappingCode.AppendLine($"                    list.Add(Create{underlyingType.Category}{GetIdentifier(underlyingType, isMetaType)}(nestedMapping));");
                    mappingCode.AppendLine($"                }}");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"                obj.{fieldIdentifier} = list.ToArray();");
                    mappingCode.AppendLine($"                PopCurrentFrame();");
                    mappingCode.AppendLine($"            }}");
                }
                else if (fieldType.IsUserDefined)
                {
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    mappingCode.AppendLine($"            {{");
                    mappingCode.AppendLine($"                // Create {fieldTypeName} UDT for \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"                FieldMapping fieldMapping = fieldLookup[\"{fieldIdentifier}\"];");
                    mappingCode.AppendLine($"                TypeMapping nestedMapping = GetTypeMapping(fieldMapping);");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"                PushRelativeFrame(fieldMapping);");
                    mappingCode.AppendLine($"                obj.{fieldIdentifier} = Create{fieldType.Category}{GetIdentifier(fieldType, isMetaType)}(nestedMapping);");
                    mappingCode.AppendLine($"                PopRelativeFrame(fieldMapping);");
                    mappingCode.AppendLine($"            }}");
                }
                else if (fieldType.IsArray)
                {
                    string arrayTypeName = GetTypeName(underlyingType, isMetaType);

                    mappingCode.AppendLine($"            {{");
                    mappingCode.AppendLine($"                // Create {arrayTypeName} array for \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"                ArrayMapping arrayMapping = (ArrayMapping)fieldLookup[\"{fieldIdentifier}\"];");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"                List<{arrayTypeName}> list = new List<{arrayTypeName}>();");
                    mappingCode.AppendLine($"                int count = GetArrayMeasurementCount(arrayMapping);");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"                for (int i = 0; i < count; i++)");
                    mappingCode.AppendLine($"                {{");
                    mappingCode.AppendLine($"                    IMeasurement measurement = GetArrayMeasurement(i);");
                    if (isMetaType)
                        mappingCode.AppendLine($"                    list.Add(GetMetaValues(measurement));");
                    else
                        mappingCode.AppendLine($"                    list.Add(({arrayTypeName})measurement.Value);");
                    mappingCode.AppendLine($"                }}");
                    mappingCode.AppendLine();
                    mappingCode.AppendLine($"                obj.{fieldIdentifier} = list.ToArray();");
                    mappingCode.AppendLine($"            }}");
                }
                else
                {
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    mappingCode.AppendLine($"            {{");
                    mappingCode.AppendLine($"                // Assign {fieldTypeName} value to \"{fieldIdentifier}\" field");
                    mappingCode.AppendLine($"                FieldMapping fieldMapping = fieldLookup[\"{fieldIdentifier}\"];");
                    mappingCode.AppendLine($"                IMeasurement measurement = GetMeasurement(fieldMapping);");
                    if (isMetaType)
                        mappingCode.AppendLine($"                obj.{fieldIdentifier} = GetMetaValues(measurement);");
                    else
                        mappingCode.AppendLine($"                obj.{fieldIdentifier} = ({fieldTypeName})measurement.Value;");
                    mappingCode.AppendLine($"            }}");
                }

                mappingCode.AppendLine();
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
                { "DateTime.Time", "System.DateTime" },
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
