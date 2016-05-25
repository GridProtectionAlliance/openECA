//******************************************************************************************************
//  UDTCompiler.cs - Gbtc
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
//  05/25/2016 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GSF.Annotations;
using openECAClient.Model;

namespace openECAClient
{
    public class InvalidUDTException : Exception
    {
        public InvalidUDTException(string message)
            : base(message)
        {
        }

        public InvalidUDTException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class UDTCompiler
    {
        #region [ Members ]

        // Nested Types
        private class TypeReference
        {
            public string Category;
            public string Identifier;
            public UDTField Field;
        }

        // Constants
        public const string DefaultUDTCategory = "UDT";

        // Fields
        private static Dictionary<string, List<DataType>> m_definedTypes;
        private static Dictionary<UDTField, TypeReference> m_typeReferences;
        private static HashSet<UserDefinedType> m_resolvedTypes;
        private List<InvalidUDTException> m_batchErrors;

        private string m_idlFile;
        private TextReader m_reader;
        private string m_currentCategory;
        private char m_currentChar;
        private bool m_endOfFile;

        #endregion

        #region [ Constructors ]

        public UDTCompiler()
        {
            m_definedTypes = GetPrimitiveTypes();
            m_typeReferences = new Dictionary<UDTField, TypeReference>();
            m_resolvedTypes = new HashSet<UserDefinedType>();
            m_batchErrors = new List<InvalidUDTException>();
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets a list of all the types defined by
        /// UDT definitions parsed by this compiler.
        /// </summary>
        /// <remarks>
        /// Calls to this property will cause the compiler to resolve all
        /// type references before returning the list of defined types.
        /// Errors during type resolution will be added to the
        /// <see cref="BatchErrors"/> list.
        /// </remarks>
        public List<DataType> DefinedTypes
        {
            get
            {
                List<DataType> definedTypes = new List<DataType>();

                foreach (DataType type in m_definedTypes.Values.SelectMany(list => list))
                {
                    try
                    {
                        if (type.IsUserDefined)
                            ResolveReferences((UserDefinedType)type);

                        definedTypes.Add(type);
                    }
                    catch (InvalidUDTException ex)
                    {
                        BatchErrors.Add(ex);
                    }
                }

                return definedTypes;
            }
        }

        /// <summary>
        /// Returns a list of errors encountered while parsing
        /// types during a directory scan or type resolution.
        /// </summary>
        public List<InvalidUDTException> BatchErrors
        {
            get
            {
                return m_batchErrors;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Scans the directory for .ecaidl files and compiles the UDTs defined in them.
        /// </summary>
        /// <param name="directory">The directory to scan for UDTs.</param>
        public void Scan(string directory)
        {
            m_batchErrors.Clear();

            foreach (string idlFile in Directory.EnumerateFiles(directory, "*.ecaidl", SearchOption.AllDirectories))
            {
                try
                {
                    Compile(idlFile);
                }
                catch (InvalidUDTException ex)
                {
                    m_batchErrors.Add(ex);
                }
            }
        }

        /// <summary>
        /// Compiles the given IDL file.
        /// </summary>
        /// <param name="idlFile">The file to be compiled.</param>
        /// <exception cref="InvalidUDTException">An error occurs during compilation.</exception>
        public void Compile(string idlFile)
        {
            try
            {
                m_idlFile = idlFile;

                using (m_reader = File.OpenText(idlFile))
                {
                    m_currentCategory = DefaultUDTCategory;

                    // Read the first character from the file
                    ReadNextChar();
                    SkipWhitespace();

                    while (!m_endOfFile)
                    {
                        ParseUDT();
                        SkipWhitespace();
                    }
                }
            }
            finally
            {
                m_idlFile = null;
            }
        }

        /// <summary>
        /// Gets the type identified by the given category and identifier.
        /// </summary>
        /// <param name="category">The category in which the data type resides.</param>
        /// <param name="identifier">The identifier for the data type.</param>
        /// <returns>The data type identified by the category and identifier or null if no type is found.</returns>
        public DataType GetType(string category, string identifier)
        {
            List<DataType> types;

            if (!m_definedTypes.TryGetValue(identifier, out types))
                return null;

            return types.FirstOrDefault(t => t.Category == category);
        }

        /// <summary>
        /// Gets the type identified by the given identifier.
        /// </summary>
        /// <param name="identifier">The identifier for the data type.</param>
        /// <returns>The data type identified by the identifier or null if no type is found.</returns>
        /// <exception cref="InvalidUDTException">The identifier identifies more than one type and no type is defined in the default category.</exception>
        public DataType GetType(string identifier)
        {
            List<DataType> types;
            DataType type;

            if (!m_definedTypes.TryGetValue(identifier, out types))
                return null;

            if (types.Count == 1)
                type = types[0];
            else
                type = types.FirstOrDefault(t => t.Category == DefaultUDTCategory);

            if ((object)type == null)
                RaiseCompileError($"Ambiguous reference to type {identifier}. Type found in {types.Count} categories: {string.Join(", ", types)}.");

            if (type.IsUserDefined)
                ResolveReferences((UserDefinedType)type);

            return type;
        }

        private void ResolveReferences(UserDefinedType type)
        {
            string typeIdentifier;
            TypeReference reference;
            List<DataType> types;

            if (m_resolvedTypes.Contains(type))
                return;

            foreach (UDTField field in type.Fields)
            {
                if (!m_typeReferences.TryGetValue(field, out reference))
                    RaiseCompileError($"Type reference not found for field {field.Identifier} of type {type.Identifier}.");

                typeIdentifier = reference.Identifier.TrimEnd('[', ']');

                if (!m_definedTypes.TryGetValue(typeIdentifier, out types))
                    RaiseCompileError($"No definition found for type {typeIdentifier} referenced by field {field.Identifier} of type {type.Identifier}.");

                if (string.IsNullOrEmpty(reference.Category))
                {
                    if (types.Count > 1)
                        RaiseCompileError($"Ambiguous reference to type {typeIdentifier} on field {field.Identifier} of type {type.Identifier}. Type found in {types.Count} categories: {string.Join(", ", types)}.");

                    field.Type = types[0];
                }
                else
                {
                    field.Type = types.FirstOrDefault(t => t.Category == reference.Category);

                    if ((object)field.Type == null)
                        RaiseCompileError($"No definition found for type \"{reference.Category} {typeIdentifier}\" referenced by field {field.Identifier} of type {type.Identifier}.");
                }

                if (field.Type.IsUserDefined)
                    ResolveReferences((UserDefinedType)field.Type);

                if (reference.Identifier.EndsWith("[]"))
                {
                    field.Type = new ArrayType()
                    {
                        Category = field.Type.Category,
                        Identifier = field.Type.Identifier,
                        UnderlyingType = field.Type
                    };
                }
            }

            m_resolvedTypes.Add(type);
        }

        private void ParseUDT()
        {
            List<TypeReference> typeReferences = new List<TypeReference>();
            UserDefinedType type;
            List<DataType> definedTypes;

            // Check for EOF
            if (m_endOfFile)
                RaiseCompileError("Unexpected end of file. Expected 'category' keyword or UDT identifier.");

            // Assume the first token we encounter is the type identifier
            type = new UserDefinedType();
            type.Identifier = ParseIdentifier();
            SkipWhitespace();

            // Check for EOF
            if (m_endOfFile && type.Identifier == "category")
                RaiseCompileError("Unexpected end of file. Expected '{' or category identifier.");

            // If the next character we encounter is not '{'
            // and the first token was the category keyword,
            // parse the category identifier next,
            // then parse the actual type identifier
            if (!m_endOfFile && m_currentChar != '{' && type.Identifier == "category")
            {
                m_currentCategory = ParseIdentifier();
                SkipWhitespace();
                type.Identifier = ParseIdentifier();
                SkipWhitespace();
            }

            type.Category = m_currentCategory;

            // Check for errors
            if (m_endOfFile)
                RaiseCompileError("Unexpected end of file. Expected '{'.");

            if (m_currentChar != '{')
                RaiseCompileError($"Unexpected character: {GetCharText(m_currentChar)}. Expected '{{'.");

            if (!m_definedTypes.TryGetValue(type.Identifier, out definedTypes))
                definedTypes = new List<DataType>();

            if (definedTypes.Any(t => t.Category == type.Category))
                RaiseCompileError($"Type \"{type.Category} {type.Identifier}\" has already been defined.");

            // Scan ahead to search
            // for the next newline
            ReadNextChar();
            SkipToNewline();

            // Check for errors
            if (m_endOfFile)
                RaiseCompileError("Unexpected end of file. Expected newline.");

            if (m_currentChar != '\n')
                RaiseCompileError($"Unexpected character: {GetCharText(m_currentChar)}. Expected newline.");

            SkipWhitespace();

            // Parse fields
            while (!m_endOfFile && m_currentChar != '}')
            {
                type.Fields.Add(ParseUDTField(typeReferences));
                SkipWhitespace();
            }

            if (m_endOfFile)
                RaiseCompileError("Unexpected end of file. Expected '}'.");

            ReadNextChar();

            // Add the UDT to the lookup table for defined types
            if (definedTypes.Count == 0)
                m_definedTypes.Add(type.Identifier, definedTypes);

            definedTypes.Add(type);

            // Add type references to the type reference lookup table
            foreach (TypeReference reference in typeReferences)
                m_typeReferences.Add(reference.Field, reference);
        }

        private UDTField ParseUDTField(List<TypeReference> typeReferences)
        {
            TypeReference reference = new TypeReference();

            // Assume the first identifier is the type identifier
            reference.Identifier = ParseIdentifier();

            if (!m_endOfFile && m_currentChar == '[')
            {
                ReadNextChar();

                if (m_endOfFile)
                    RaiseCompileError("Unexpected end of file. Expected ']'.");

                if (m_currentChar != ']')
                    RaiseCompileError($"Unexpected character: {GetCharText(m_currentChar)}. Expected ']'.");

                reference.Identifier += "[]";
                ReadNextChar();
            }

            SkipToNewline();

            // Assume the second identifier is the field identifier
            reference.Field = new UDTField();
            reference.Field.Identifier = ParseIdentifier();

            if (m_currentChar == '[')
            {
                if (reference.Identifier.EndsWith("[]"))
                    RaiseCompileError("Unexpected character: '['. Expected newline.");

                ReadNextChar();

                if (m_endOfFile)
                    RaiseCompileError("Unexpected end of file. Expected ']'.");

                if (m_currentChar != ']')
                    RaiseCompileError($"Unexpected character: {GetCharText(m_currentChar)}. Expected ']'.");

                reference.Field.Identifier += "[]";
                ReadNextChar();
            }

            SkipToNewline();

            // If there is a third identifier then
            // the first identifier is the category identifier,
            // the second identifier is the type identifier,
            // and the third identifier is the field identifier
            if (!m_endOfFile && m_currentChar != '\n')
            {
                if (reference.Identifier.EndsWith("[]"))
                    RaiseCompileError($"Unexpected character: {GetCharText(m_currentChar)}. Expected newline.");

                reference.Category = reference.Identifier;
                reference.Identifier = reference.Field.Identifier;
                reference.Field.Identifier = ParseIdentifier();
                SkipToNewline();
            }

            // Check for errors
            if (m_endOfFile)
                RaiseCompileError("Unexpected end of file. Expected newline.");

            if (m_currentChar != '\n')
                RaiseCompileError($"Unexpected character: {GetCharText(m_currentChar)}. Expected newline.");

            if (reference.Field.Identifier.EndsWith("[]"))
                RaiseCompileError($"Unexpected character: {GetCharText(m_currentChar)}. Expected identifier.");

            // Add the reference to the list of type references
            typeReferences.Add(reference);

            return reference.Field;
        }

        private string ParseIdentifier()
        {
            StringBuilder builder = new StringBuilder();

            Func<char, bool> isIdentifierChar = c =>
                char.IsLetterOrDigit(c) ||
                c == '_';

            if (char.IsDigit(m_currentChar))
                RaiseCompileError($"Invalid character for start of identifier: '{GetCharText(m_currentChar)}'. Expected letter or underscore.");
            
            while (!m_endOfFile && isIdentifierChar(m_currentChar))
            {
                builder.Append(m_currentChar);
                ReadNextChar();
            }

            if (builder.Length == 0)
            {
                if (m_endOfFile)
                    RaiseCompileError($"Unexpected end of file. Expected identifier.");
                else
                    RaiseCompileError($"Unexpected character: '{GetCharText(m_currentChar)}'. Expected identifier.");
            }

            return builder.ToString();
        }

        private void SkipWhitespace()
        {
            while (!m_endOfFile && char.IsWhiteSpace(m_currentChar))
                ReadNextChar();
        }

        private void SkipToNewline()
        {
            while (!m_endOfFile && char.IsWhiteSpace(m_currentChar) && m_currentChar != '\n')
                ReadNextChar();
        }

        private void ReadNextChar()
        {
            int c = m_reader.Read();

            m_endOfFile = (c == -1);

            if (!m_endOfFile)
                m_currentChar = (char)c;
        }

        [ContractAnnotation("=> halt")]
        private void RaiseCompileError(string message)
        {
            if ((object)m_idlFile == null)
                throw new InvalidUDTException(message);

            string fileName = Path.GetFileName(m_idlFile);
            string exceptionMessage = $"Error compiling {fileName}: {message}";
            throw new InvalidUDTException(exceptionMessage);
        }

        #endregion

        #region [ Static ]

        // Static Fields
        public static readonly IList<DataType> PrimitiveTypes;

        // Static Constructor
        static UDTCompiler()
        {
            List<DataType> primitiveTypes = new List<DataType>();

            primitiveTypes.Add(new DataType() { Category = "Integer", Identifier = "Byte" });
            primitiveTypes.Add(new DataType() { Category = "Integer", Identifier = "Int16" });
            primitiveTypes.Add(new DataType() { Category = "Integer", Identifier = "Int32" });
            primitiveTypes.Add(new DataType() { Category = "Integer", Identifier = "Int64" });
            primitiveTypes.Add(new DataType() { Category = "Integer", Identifier = "UInt16" });
            primitiveTypes.Add(new DataType() { Category = "Integer", Identifier = "UInt32" });
            primitiveTypes.Add(new DataType() { Category = "Integer", Identifier = "UInt64" });
            
            primitiveTypes.Add(new DataType() { Category = "FloatingPoint", Identifier = "Decimal" });
            primitiveTypes.Add(new DataType() { Category = "FloatingPoint", Identifier = "Double" });
            primitiveTypes.Add(new DataType() { Category = "FloatingPoint", Identifier = "Single" });
            
            primitiveTypes.Add(new DataType() { Category = "DateTime", Identifier = "Date" });
            primitiveTypes.Add(new DataType() { Category = "DateTime", Identifier = "DateTime" });
            primitiveTypes.Add(new DataType() { Category = "DateTime", Identifier = "Time" });
            primitiveTypes.Add(new DataType() { Category = "DateTime", Identifier = "TimeSpan" });
            
            primitiveTypes.Add(new DataType() { Category = "Text", Identifier = "Char" });
            primitiveTypes.Add(new DataType() { Category = "Text", Identifier = "String" });
            
            primitiveTypes.Add(new DataType() { Category = "Other", Identifier = "Boolean" });
            primitiveTypes.Add(new DataType() { Category = "Other", Identifier = "Guid" });

            PrimitiveTypes = primitiveTypes.AsReadOnly();
        }

        // Static Methods
        private static Dictionary<string, List<DataType>> GetPrimitiveTypes()
        {
            return PrimitiveTypes.ToDictionary(type => type.Identifier, type => new List<DataType>() { type });
        }

        private static string GetCharText(char c)
        {
            return
                (c == '\r') ? @"\r" :
                (c == '\n') ? @"\n" :
                (c == '\t') ? @"\t" :
                (c == '\0') ? @"\0" :
                c.ToString();
        }

        #endregion
    }
}
