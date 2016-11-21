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

// ReSharper disable RedundantStringInterpolation
namespace ECAClientUtilities.Template.IronPython
{
    public class ProjectGenerator : DotNetProjectGeneratorBase
    {
        #region [ Members ]

        private readonly Dictionary<string, Tuple<string, bool>> m_primitiveConversionFunctions;

        #endregion

        #region [ Constructors ]

        public ProjectGenerator(string projectName, MappingCompiler compiler) : base(projectName, compiler, "py", "IronPython")
        {
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
            string fieldList = string.Join(Environment.NewLine, type.Fields.Select(field =>
                $"    def get_{field.Identifier}(self): # {GetDataTypeName(field.Type)}{Environment.NewLine}" + 
                $"        def set_{field.Identifier}(self, value):{Environment.NewLine}" + 
                $"            {field.Identifier} = property(fget=get_{field.Identifier}, fset=set_{field.Identifier})"));

            // Generate the contents of the class file
            return GetTextFromResource("ECAClientUtilities.Template.IronPython.UDTTemplate.txt")
                .Replace("{ProjectName}", ProjectName)
                .Replace("{Category}", type.Category)
                .Replace("{Identifier}", type.Identifier)
                .Replace("{Fields}", fieldList.Trim());
        }

        protected override string ConstructMetaModel(UserDefinedType type)
        {
            string fieldList = string.Join(Environment.NewLine, type.Fields.Select(field =>
                $"    def get_{field.Identifier}(self): # {GetMetaTypeName(field.Type)}{Environment.NewLine}" +
                $"        def set_{field.Identifier}(self, value):{Environment.NewLine}" +
                $"            {field.Identifier} = property(fget=get_{field.Identifier}, fset=set_{field.Identifier})"));

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

        #endregion
    }
}
