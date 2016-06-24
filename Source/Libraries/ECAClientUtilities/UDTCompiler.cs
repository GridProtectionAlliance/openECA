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
using GSF.Collections;
using ECAClientUtilities.Model;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace ECAClientUtilities
{
    public class InvalidUDTException : Exception
    {
        #region [ Members ]

        // Fields
        private string m_filePath;
        private string m_fileContents;

        #endregion

        #region [ Constructors ]

        public InvalidUDTException(string message)
            : base(message)
        {
        }

        public InvalidUDTException(string message, string filePath, string fileContents)
            : base(message)
        {
            m_filePath = filePath;
            m_fileContents = fileContents;
        }

        public InvalidUDTException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public InvalidUDTException(string message, string filePath, string fileContents, Exception innerException)
            : base(message, innerException)
        {
            m_filePath = filePath;
            m_fileContents = fileContents;
        }

        protected InvalidUDTException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            m_filePath = info.GetString("FilePath");
            m_fileContents = info.GetString("FileContents");
        }

        #endregion

        #region [ Properties ]

        public string FilePath
        {
            get
            {
                return m_filePath;
            }
        }

        public string FileContents
        {
            get
            {
                return m_fileContents;
            }
        }

        #endregion

        #region [ Methods ]

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("FilePath", m_filePath);
            info.AddValue("FileContents", m_fileContents);

            base.GetObjectData(info, context);
        }

        #endregion
    }

    /// <summary>
    /// Compiles user defined types from an IDL into an object
    /// structure that can be used for lookups and serialization.
    /// </summary>
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

        /// <summary>
        /// The default category for UDTs that do not define an explicit category.
        /// </summary>
        public const string DefaultUDTCategory = "UDT";

        // Fields
        private Dictionary<string, List<DataType>> m_definedTypes;
        private Dictionary<UDTField, TypeReference> m_typeReferences;
        private HashSet<UserDefinedType> m_resolvedTypes;
        private List<InvalidUDTException> m_batchErrors;

        private string m_idlFile;
        private TextReader m_reader;
        private string m_currentCategory;
        private char m_currentChar;
        private bool m_endOfFile;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="UDTCompiler"/> class.
        /// </summary>
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
                m_definedTypes.Values
                    .SelectMany(list => list)
                    .Where(type => type.IsUserDefined)
                    .ToList()
                    .ForEach(type =>
                    {
                        try
                        {
                            ResolveReferences((UserDefinedType)type);
                        }
                        catch (InvalidUDTException ex)
                        {
                            BatchErrors.Add(ex);
                        }
                    });

                return m_definedTypes.Values
                    .SelectMany(list => list)
                    .ToList();
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
        /// <exception cref="ArgumentNullException"><paramref name="directory"/> is null</exception>
        public void Scan(string directory)
        {
            if ((object)directory == null)
                throw new ArgumentNullException(nameof(directory));

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
        /// <exception cref="ArgumentNullException"><paramref name="idlFile"/> is null</exception>
        /// <exception cref="InvalidUDTException">An error occurs during compilation.</exception>
        public void Compile(string idlFile)
        {
            if ((object)idlFile == null)
                throw new ArgumentNullException(nameof(idlFile));

            try
            {
                m_idlFile = idlFile;

                using (TextReader reader = File.OpenText(idlFile))
                {
                    Compile(reader);
                }
            }
            finally
            {
                m_idlFile = null;
            }
        }

        /// <summary>
        /// Compiles UDTs by reading from the given stream.
        /// </summary>
        /// <param name="stream">The stream in which the UDTs are defined.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null</exception>
        public void Compile(Stream stream)
        {
            if ((object)stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (TextReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
            {
                Compile(reader);
            }
        }

        /// <summary>
        /// Compiles UDTs by reading from the given reader.
        /// </summary>
        /// <param name="reader">The reader used to read the UDT definitions.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null</exception>
        public void Compile(TextReader reader)
        {
            if ((object)reader == null)
                throw new ArgumentNullException(nameof(reader));

            m_reader = reader;
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

        /// <summary>
        /// Gets the type identified by the given category and identifier.
        /// </summary>
        /// <param name="category">The category in which the data type resides.</param>
        /// <param name="identifier">The identifier for the data type.</param>
        /// <returns>The data type identified by the category and identifier or null if no type is found.</returns>
        /// <exception cref="InvalidUDTException">
        /// There is an error resolving the type or its referenced types.
        /// <br/> - OR - <br/>
        /// The identifier identifies more than one type and no type is defined in the default category.
        /// </exception>
        /// <remarks>
        /// The first time a user defined type is accessed, the type references
        /// made by that type and all of its referenced types are resolved.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="category"/> is null or <paramref name="identifier"/> is null</exception>
        public DataType GetType(string category, string identifier)
        {
            List<DataType> types;
            DataType type;

            if ((object)category == null)
                throw new ArgumentNullException(nameof(category));

            if ((object)identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (!m_definedTypes.TryGetValue(identifier, out types))
                return null;

            type = types.FirstOrDefault(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

            if ((object)type != null && type.IsUserDefined)
                ResolveReferences((UserDefinedType)type);

            return type;
        }

        /// <summary>
        /// Gets the type identified by the given identifier.
        /// </summary>
        /// <param name="identifier">The identifier for the data type.</param>
        /// <returns>The data type identified by the identifier or null if no type is found.</returns>
        /// <exception cref="InvalidUDTException">
        /// There is an error resolving the type or its referenced types.
        /// <br/> - OR - <br/>
        /// The identifier identifies more than one type and no type is defined in the default category.
        /// </exception>
        /// <remarks>
        /// The first time a user defined type is accessed, the type references
        /// made by that type and all of its referenced types are resolved.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="identifier"/> is null</exception>
        public DataType GetType(string identifier)
        {
            List<DataType> types;
            DataType type;

            if ((object)identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (!m_definedTypes.TryGetValue(identifier, out types))
                return null;

            if (types.Count == 1)
                type = types[0];
            else
                type = types.FirstOrDefault(t => t.Category.Equals(DefaultUDTCategory, StringComparison.OrdinalIgnoreCase));

            if ((object)type == null)
                RaiseCompileError($"Ambiguous reference to type {identifier}. Type found in {types.Count} categories: {string.Join(", ", types)}.");

            if (type.IsUserDefined)
                ResolveReferences((UserDefinedType)type);

            return type;
        }

        /// <summary>
        /// Enumerates over the collection of defined data types that reference the given type.
        /// </summary>
        /// <param name="type">The type being referenced.</param>
        /// <returns>The enumerable used to enumerate over referencing types.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is null</exception>
        public IEnumerable<DataType> EnumerateReferencingTypes(DataType type)
        {
            List<DataType> types;

            if ((object)type == null)
                throw new ArgumentNullException(nameof(type));

            if (m_definedTypes.TryGetValue(type.Identifier, out types) && types.Contains(type))
            {
                foreach (UserDefinedType definedType in m_definedTypes.Values.SelectMany(list => list).OfType<UserDefinedType>().ToList())
                {
                    // If the type has already been resolved, we can simply
                    // check the type of its fields for matching data types
                    bool referencesType = definedType.Fields.Any(field => ((field.Type as ArrayType)?.UnderlyingType ?? field.Type) == type);

                    // If the type has not been resolved, check its type references
                    if (!referencesType && !m_resolvedTypes.Contains(definedType))
                    {
                        TypeReference typeReference = null;
                        
                        // Get the type references with a matching identifier
                        List<TypeReference> typeReferences = definedType.Fields
                            .Where(field => (object)field.Type == null)
                            .Where(field => m_typeReferences.TryGetValue(field, out typeReference))
                            .Select(field => typeReference)
                            .Where(reference => reference.Identifier.Equals(type.Identifier, StringComparison.OrdinalIgnoreCase) || reference.Identifier.Equals(type.Identifier + "[]", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        // Check whether the type is referenced
                        // based on the logic for type resolution
                        referencesType =
                            typeReferences.Any(reference => reference.Category.Equals(type.Category, StringComparison.OrdinalIgnoreCase)) ||
                            typeReferences.Any(reference => (object)reference.Category == null && types.Count == 1) ||
                            typeReferences.Any(reference => (object)reference.Category == null && types.Count(t => !t.IsUserDefined) == 1) ||
                            typeReferences.Any(reference => (object)reference.Category == null && types.Any(t => t.Category.Equals(DefaultUDTCategory, StringComparison.OrdinalIgnoreCase)));
                    }

                    // If the defined type does not
                    // reference the given type, skip it
                    if (!referencesType)
                        continue;

                    try
                    {
                        // Attempt to resolve references
                        // before returning the type
                        ResolveReferences(definedType);
                    }
                    catch (InvalidUDTException ex)
                    {
                        // Save the error to the
                        // collection of batch errors
                        BatchErrors.Add(ex);
                        continue;
                    }

                    // Return the defined type
                    yield return definedType;
                }
            }
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

                // Look up the list of candidate types based on the type identifier
                typeIdentifier = reference.Identifier;

                if (!m_definedTypes.TryGetValue(typeIdentifier, out types))
                {
                    if (!typeIdentifier.EndsWith("[]"))
                        RaiseCompileError($"No definition found for type {typeIdentifier} referenced by field {field.Identifier} of type {type.Identifier}.");

                    // If an array type is not yet defined, we can attempt to
                    // resolve the underlying type and then define a new array type
                    typeIdentifier = reference.Identifier.TrimEnd('[', ']');

                    if (!m_definedTypes.TryGetValue(typeIdentifier, out types))
                        RaiseCompileError($"No definition found for type {typeIdentifier} referenced by field {field.Identifier} of type {type.Identifier}.");
                }

                if (string.IsNullOrEmpty(reference.Category))
                {
                    // If no explicit category is referenced, the compiler
                    // attempts to infer the type based on just the identifier
                    //  - If there is exactly one defined type with a matching identifier, use that type
                    //  - If there is exactly one primitive type with a matching identifier, use that type
                    //  - If there is exactly one user defined type in the default UDT category, use that type
                    //  - Otherwise, raise an error indicating that the reference is ambiguous
                    if (types.Count == 1)
                        field.Type = types[0];
                    else if (types.Count(definedType => !definedType.IsUserDefined) == 1)
                        field.Type = types.Single(definedType => !definedType.IsUserDefined);
                    else if (types.Any(definedType => definedType.Category.Equals(DefaultUDTCategory, StringComparison.OrdinalIgnoreCase)))
                        field.Type = types.Single(definedType => definedType.Category.Equals(DefaultUDTCategory, StringComparison.OrdinalIgnoreCase));
                    else
                        RaiseCompileError($"Ambiguous reference to type {typeIdentifier} on field {field.Identifier} of type {type.Identifier}. Type found in {types.Count} categories: {string.Join(", ", types)}.");
                }
                else
                {
                    // If the category is specified, simply search for the type with a matching category
                    field.Type = types.FirstOrDefault(t => t.Category.Equals(reference.Category, StringComparison.OrdinalIgnoreCase));

                    if ((object)field.Type == null)
                        RaiseCompileError($"No definition found for type \"{reference.Category} {typeIdentifier}\" referenced by field {field.Identifier} of type {type.Identifier}.");
                }

                // If the referenced type is a UDT,
                // resolve its references as well
                if (field.Type.IsUserDefined)
                    ResolveReferences((UserDefinedType)field.Type);

                // If the referenced type is an array but the array type is not defined,
                // create a new array type and add it to the defined types
                if (reference.Identifier.EndsWith("[]") && !field.Type.IsArray)
                {
                    field.Type = new ArrayType(field.Type)
                    {
                        Category = field.Type.Category,
                        Identifier = field.Type.Identifier + "[]"
                    };

                    types = m_definedTypes.GetOrAdd(reference.Identifier, ident => new List<DataType>());
                    types.Add(field.Type);
                }
            }

            // If we made it here, the type must have
            // successfully resolved so we can add it
            // to the collection of resolved types
            m_resolvedTypes.Add(type);
        }

        private void ParseUDT()
        {
            List<TypeReference> typeReferences = new List<TypeReference>();
            UserDefinedType type;
            List<DataType> definedTypes;

            // Assume the first token we encounter is the type identifier
            type = new UserDefinedType();
            type.Identifier = ParseIdentifier();
            SkipWhitespace();

            // Check for EOF
            if (m_endOfFile && type.Identifier.Equals("category", StringComparison.OrdinalIgnoreCase))
                RaiseCompileError("Unexpected end of file. Expected '{' or category identifier.");

            // If the next character we encounter is not '{'
            // and the first token was the category keyword,
            // parse the category identifier next,
            // then parse the actual type identifier
            if (!m_endOfFile && m_currentChar != '{' && type.Identifier.Equals("category", StringComparison.OrdinalIgnoreCase))
            {
                m_currentCategory = ParseIdentifier();
                SkipToNewline();
                Assert('\n');
                SkipWhitespace();

                type.Identifier = ParseIdentifier();
                SkipWhitespace();
            }

            type.Category = m_currentCategory;

            // Check for errors
            Assert('{');

            if (!m_definedTypes.TryGetValue(type.Identifier, out definedTypes))
                definedTypes = new List<DataType>();

            if (definedTypes.Any(t => t.Category.Equals(type.Category, StringComparison.OrdinalIgnoreCase)))
                RaiseCompileError($"Type \"{type.Category} {type.Identifier}\" has already been defined.");

            // Scan ahead to search
            // for the next newline
            ReadNextChar();
            SkipToNewline();
            Assert('\n');
            SkipWhitespace();

            // Parse fields
            while (!m_endOfFile && m_currentChar != '}')
            {
                type.Fields.Add(ParseUDTField(typeReferences));
                SkipWhitespace();
            }

            Assert('}');
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
                Assert(']');
                ReadNextChar();
                reference.Identifier += "[]";
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
                Assert(']');
                ReadNextChar();
                reference.Field.Identifier += "[]";
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
            Assert('\n');

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

            if (!m_endOfFile && char.IsDigit(m_currentChar))
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

        private void Assert(params char[] expectedChars)
        {
            Func<char[], string> getExpectedCharsText = chars =>
            {
                StringBuilder builder = new StringBuilder();

                if (chars.Length == 1)
                    return $"'{GetCharText(chars[0])}'";

                if (chars.Length == 2)
                    return $"'{GetCharText(chars[0])}' or '{GetCharText(chars[1])}'";

                for (int i = 0; i < chars.Length; i++)
                {
                    if (i > 0)
                        builder.Append(", ");

                    if (i == chars.Length - 1)
                        builder.Append("or ");

                    builder.Append($"'{GetCharText(chars[i])}'");
                }

                return builder.ToString();
            };

            if (m_endOfFile)
                RaiseCompileError($"Unexpected end of file. Expected {getExpectedCharsText(expectedChars)}");

            if (expectedChars.All(c => m_currentChar != c))
                RaiseCompileError($"Unexpected character: {GetCharText(m_currentChar)}. Expected {getExpectedCharsText(expectedChars)}");
        }

        [ContractAnnotation("=> halt")]
        private void RaiseCompileError(string message)
        {
            if ((object)m_idlFile == null)
                throw new InvalidUDTException(message);

            string fileName = Path.GetFileName(m_idlFile);
            string exceptionMessage = $"Error compiling {fileName}: {message}";
            throw new InvalidUDTException(exceptionMessage, m_idlFile, File.ReadAllText(m_idlFile));
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
            return PrimitiveTypes.ToDictionary(type => type.Identifier, type => new List<DataType>() { type }, StringComparer.OrdinalIgnoreCase);
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
