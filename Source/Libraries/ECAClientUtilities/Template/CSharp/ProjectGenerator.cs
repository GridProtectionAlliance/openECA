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
                    mappingCode.AppendLine($"                ArrayMapping arrayMapping = (ArrayMapping)fieldLookup[\"{fieldIdentifier}\"];");
                    mappingCode.AppendLine($"                PushWindowFrame(arrayMapping);");
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
                    mappingCode.AppendLine($"                PopWindowFrame(arrayMapping);");
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

                    fillCode.AppendLine($"            {{");
                    fillCode.AppendLine($"                // Initialize {arrayTypeName} UDT array for \"{fieldIdentifier}\" field");
                    fillCode.AppendLine($"                ArrayMapping arrayMapping = (ArrayMapping)fieldLookup[\"{fieldIdentifier}\"];");
                    fillCode.AppendLine($"                PushWindowFrameTime(arrayMapping);");
                    fillCode.AppendLine();
                    fillCode.AppendLine($"                List<{arrayTypeName}> list = new List<{arrayTypeName}>();");
                    fillCode.AppendLine($"                int count = GetUDTArrayTypeMappingCount(arrayMapping);");
                    fillCode.AppendLine();
                    fillCode.AppendLine($"                for (int i = 0; i < count; i++)");
                    fillCode.AppendLine($"                {{");
                    fillCode.AppendLine($"                    TypeMapping nestedMapping = GetUDTArrayTypeMapping(arrayMapping, i);");
                    // MKD 6/14/2017: Added self identifier "this" to Fill{underlyingType.Category}{GetIdentifier(fieldType, isMetaType)} method.
                    fillCode.AppendLine($"                    list.Add(this.Fill{underlyingType.Category}{GetIdentifier(underlyingType, isMetaType)}(nestedMapping));");
                    fillCode.AppendLine($"                }}");
                    fillCode.AppendLine();
                    fillCode.AppendLine($"                obj.{fieldIdentifier} = list.ToArray();");
                    fillCode.AppendLine($"                PopWindowFrameTime(arrayMapping);");
                    fillCode.AppendLine($"            }}");
                }
                else if (fieldType.IsUserDefined)
                {
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    fillCode.AppendLine($"            {{");
                    fillCode.AppendLine($"                // Initialize {fieldTypeName} UDT for \"{fieldIdentifier}\" field");
                    fillCode.AppendLine($"                FieldMapping fieldMapping = fieldLookup[\"{fieldIdentifier}\"];");
                    fillCode.AppendLine($"                TypeMapping nestedMapping = GetTypeMapping(fieldMapping);");
                    fillCode.AppendLine();
                    fillCode.AppendLine($"                PushRelativeFrameTime(fieldMapping);");
                    // MKD 6/14/2017: Added self identifier "this" to Fill{fieldType.Category}{GetIdentifier(fieldType, isMetaType)} method.
                    fillCode.AppendLine($"                obj.{fieldIdentifier} = this.Fill{fieldType.Category}{GetIdentifier(fieldType, isMetaType)}(nestedMapping);");
                    fillCode.AppendLine($"                PopRelativeFrameTime(fieldMapping);");
                    fillCode.AppendLine($"            }}");
                }
                else if (fieldType.IsArray)
                {
                    string fieldTypeName = GetTypeName(underlyingType, isMetaType);

                    fillCode.AppendLine($"            {{");
                    fillCode.AppendLine($"                // Initialize array for \"{fieldIdentifier}\" field");
                    fillCode.AppendLine($"                ArrayMapping arrayMapping = (ArrayMapping)fieldLookup[\"{fieldIdentifier}\"];");
                    if (isMetaType)
                        fillCode.AppendLine($"                obj.{fieldIdentifier} = CreateMetaValues(arrayMapping).ToArray();");
                    else
                        fillCode.AppendLine($"                obj.{fieldIdentifier} = new {fieldTypeName}[GetArrayMeasurementCount(arrayMapping)];");
                    fillCode.AppendLine($"            }}");
                }
                else
                {
                    string fieldTypeName = GetTypeName(fieldType, isMetaType);

                    fillCode.AppendLine($"            {{");
                    if (isMetaType)
                    {
                        fillCode.AppendLine($"                // Initialize meta value structure to \"{fieldIdentifier}\" field");
                        fillCode.AppendLine($"                FieldMapping fieldMapping = fieldLookup[\"{fieldIdentifier}\"];");
                        fillCode.AppendLine($"                obj.{fieldIdentifier} = CreateMetaValues(fieldMapping);");
                    }
                    else
                    {
                        fillCode.AppendLine($"                // We don't need to do anything, but we burn a key index to keep our");
                        fillCode.AppendLine($"                // array index in sync with where we are in the data structure");
                        fillCode.AppendLine($"                BurnKeyIndex();");
                    }
                    fillCode.AppendLine($"            }}");
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

                    unmappingCode.AppendLine($"            {{");
                    unmappingCode.AppendLine($"                // Convert values from {arrayTypeName} UDT array for \"{fieldIdentifier}\" field to measurements");
                    unmappingCode.AppendLine($"                ArrayMapping arrayMapping = (ArrayMapping)fieldLookup[\"{fieldIdentifier}\"];");
                    unmappingCode.AppendLine($"                int dataLength = data.{fieldIdentifier}.Length;");
                    unmappingCode.AppendLine($"                int metaLength = meta.{fieldIdentifier}.Length;");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"                if (dataLength != metaLength)");
                    unmappingCode.AppendLine($"                    throw new InvalidOperationException($\"Values array length ({{dataLength}}) and MetaValues array length ({{metaLength}}) for field \\\"{fieldIdentifier}\\\" must be the same.\");");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"                PushWindowFrameTime(arrayMapping);");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"                for (int i = 0; i < dataLength; i++)");
                    unmappingCode.AppendLine($"                {{");
                    unmappingCode.AppendLine($"                    TypeMapping nestedMapping = GetUDTArrayTypeMapping(arrayMapping, i);");
                    unmappingCode.AppendLine($"                    CollectFrom{underlyingType.Category}{underlyingType.Identifier}(measurements, nestedMapping, data.{fieldIdentifier}[i], meta.{fieldIdentifier}[i]);");
                    unmappingCode.AppendLine($"                }}");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"                PopWindowFrameTime(arrayMapping);");
                    unmappingCode.AppendLine($"            }}");
                }
                else if (fieldType.IsUserDefined)
                {
                    string fieldTypeName = GetTypeName(fieldType, false);

                    unmappingCode.AppendLine($"            {{");
                    unmappingCode.AppendLine($"                // Convert values from {fieldTypeName} UDT for \"{fieldIdentifier}\" field to measurements");
                    unmappingCode.AppendLine($"                FieldMapping fieldMapping = fieldLookup[\"{fieldIdentifier}\"];");
                    unmappingCode.AppendLine($"                TypeMapping nestedMapping = GetTypeMapping(fieldMapping);");
                    unmappingCode.AppendLine($"                CollectFrom{fieldType.Category}{fieldType.Identifier}(measurements, nestedMapping, data.{fieldIdentifier}, meta.{fieldIdentifier});");
                    unmappingCode.AppendLine($"            }}");
                }
                else if (fieldType.IsArray)
                {
                    unmappingCode.AppendLine($"            {{");
                    unmappingCode.AppendLine($"                // Convert values from array in \"{fieldIdentifier}\" field to measurements");
                    unmappingCode.AppendLine($"                ArrayMapping arrayMapping = (ArrayMapping)fieldLookup[\"{fieldIdentifier}\"];");
                    unmappingCode.AppendLine($"                int dataLength = data.{fieldIdentifier}.Length;");
                    unmappingCode.AppendLine($"                int metaLength = meta.{fieldIdentifier}.Length;");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"                if (dataLength != metaLength)");
                    unmappingCode.AppendLine($"                    throw new InvalidOperationException($\"Values array length ({{dataLength}}) and MetaValues array length ({{metaLength}}) for field \\\"{fieldIdentifier}\\\" must be the same.\");");
                    unmappingCode.AppendLine();
                    unmappingCode.AppendLine($"                for (int i = 0; i < dataLength; i++)");
                    unmappingCode.AppendLine($"                {{");
                    unmappingCode.AppendLine($"                    IMeasurement measurement = MakeMeasurement(meta.{fieldIdentifier}[i], (double)data.{fieldIdentifier}[i]);");
                    unmappingCode.AppendLine($"                    measurements.Add(measurement);");
                    unmappingCode.AppendLine($"                }}");
                    unmappingCode.AppendLine($"            }}");
                }
                else
                {
                    unmappingCode.AppendLine($"            {{");
                    unmappingCode.AppendLine($"                // Convert value from \"{fieldIdentifier}\" field to measurement");
                    unmappingCode.AppendLine($"                FieldMapping fieldMapping = fieldLookup[\"{fieldIdentifier}\"];");
                    unmappingCode.AppendLine($"                IMeasurement measurement = MakeMeasurement(meta.{fieldIdentifier}, (double)data.{fieldIdentifier});");
                    unmappingCode.AppendLine($"                measurements.Add(measurement);");
                    unmappingCode.AppendLine($"            }}");
                }

                unmappingCode.AppendLine();
            }

            return unmappingCode.ToString();
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
