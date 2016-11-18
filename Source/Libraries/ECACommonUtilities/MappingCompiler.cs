//******************************************************************************************************
//  MappingCompiler.cs - Gbtc
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
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using ECACommonUtilities.Model;
using GSF.Annotations;
using GSF.Collections;
using GSF.TimeSeries.Adapters;

// ReSharper disable PossibleNullReferenceException
namespace ECACommonUtilities
{
    /// <summary>
    /// Compiles user defined types from an IDL into an object
    /// structure that can be used for lookups and serialization.
    /// </summary>
    public class MappingCompiler
    {
        #region [ Members ]

        // Nested Types
        private class TypeMappingInfo
        {
            public bool Visited;
            public bool IsValid;
            public bool HasBufferedDescendant;
            public bool HasError;
        }

        private class SuppressedValidationException : Exception
        {
        }

        // Fields
        private readonly UDTCompiler m_udtCompiler;
        private readonly Dictionary<string, TypeMapping> m_definedMappings;
        private readonly List<InvalidMappingException> m_batchErrors;
        private DataTable m_mappingTable;

        private string m_mappingFile;
        private TextReader m_reader;
        private char m_currentChar;
        private bool m_endOfFile;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="MappingCompiler"/> class.
        /// </summary>
        public MappingCompiler(UDTCompiler udtCompiler)
        {
            m_udtCompiler = udtCompiler;
            m_definedMappings = new Dictionary<string, TypeMapping>(StringComparer.OrdinalIgnoreCase);
            m_batchErrors = new List<InvalidMappingException>();
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets a list of all the mappings defined by
        /// mapping definitions parsed by this compiler.
        /// </summary>
        public List<TypeMapping> DefinedMappings => m_definedMappings.Values.ToList();

        /// <summary>
        /// Returns a list of errors encountered while parsing
        /// types during a directory scan or type resolution.
        /// </summary>
        public List<InvalidMappingException> BatchErrors => m_batchErrors;

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

            foreach (string idlFile in Directory.EnumerateFiles(directory, "*.ecamap", SearchOption.AllDirectories))
            {
                try
                {
                    Compile(idlFile);
                }
                catch (InvalidMappingException ex)
                {
                    m_batchErrors.Add(ex);
                }
            }
        }

        /// <summary>
        /// Compiles the given mapping file.
        /// </summary>
        /// <param name="mappingFile">The file to be compiled.</param>
        /// <exception cref="ArgumentNullException"><paramref name="mappingFile"/> is null</exception>
        /// <exception cref="InvalidUDTException">An error occurs during compilation.</exception>
        public void Compile(string mappingFile)
        {
            if ((object)mappingFile == null)
                throw new ArgumentNullException(nameof(mappingFile));

            try
            {
                m_mappingFile = mappingFile;

                using (TextReader reader = File.OpenText(mappingFile))
                {
                    Compile(reader);
                }
            }
            finally
            {
                m_mappingFile = null;
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

            // Read the first character from the file
            ReadNextChar();
            SkipWhitespace();

            while (!m_endOfFile)
            {
                ParseTypeMapping();
                SkipWhitespace();
            }
        }

        /// <summary>
        /// Gets the type identified by the given identifier.
        /// </summary>
        /// <param name="identifier">The identifier for the data type.</param>
        /// <returns>The data type identified by the identifier or null if no type is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="identifier"/> is null</exception>
        public TypeMapping GetTypeMapping(string identifier)
        {
            TypeMapping typeMapping;

            if ((object)identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            if (!m_definedMappings.TryGetValue(identifier, out typeMapping))
                return null;

            return typeMapping;
        }

        /// <summary>
        /// Returns list of mappings when presented with a User Defined Type
        /// </summary>
        /// <param name="udt">The identifier for the data type.</param>
        /// <returns>The data type identified by the identifier or null if no type is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="udt"/> is null</exception>
        public List<TypeMapping> GetMappings(UserDefinedType udt)
        {
            if ((object)udt == null)
                throw new ArgumentNullException(nameof(udt));

            List<TypeMapping> mappings = new List<TypeMapping>();

            foreach(TypeMapping tm in DefinedMappings)
            {
                if(udt.Category == tm.Type.Category && udt.Identifier == tm.Type.Identifier)
                    mappings.Add(tm);
            }

            return mappings;
        }

        /// <summary>
        /// Returns a collection of mappings that match the given filter expression.
        /// </summary>
        /// <param name="filterExpression">The filter expression to be matched.</param>
        /// <returns>The collection of mappings that match the given expression.</returns>
        public IEnumerable<TypeMapping> EnumerateTypeMappings(string filterExpression)
        {
            string tableName;
            string whereExpression;
            string sortField;
            int takeCount;

            if ((object)m_mappingTable == null)
                m_mappingTable = GetMappingTable();

            if (!AdapterBase.ParseFilterExpression(filterExpression, out tableName, out whereExpression, out sortField, out takeCount))
            {
                return filterExpression
                    .Split(';')
                    .Select(str => str.Trim())
                    .Where(str => !string.IsNullOrEmpty(str))
                    .Select(GetTypeMapping);
            }

            if (!tableName.Equals("Mappings", StringComparison.OrdinalIgnoreCase))
                return Enumerable.Empty<TypeMapping>();

            return m_mappingTable
                .Select(whereExpression, sortField)
                .Take(takeCount)
                .Select(row => row.Field<string>("MappingIdentifier"))
                .Select(GetTypeMapping);
        }

        /// <summary>
        /// Recursively traverses the data structures referenced by the given field
        /// mapping to find the full list of fields that map directly to signals.
        /// </summary>
        /// <param name="fieldMapping">The field mapping at which to begin the recursive traversal.</param>
        /// <returns>The full list of fields in types referenced by the given field that map directly to signals.</returns>
        /// <exception cref="NotSupportedException">A nested buffer is detected during recursive traversal.</exception>
        public IEnumerable<FieldMapping> TraverseSignalMappings(FieldMapping fieldMapping)
        {
            DataType fieldType = fieldMapping.Field.Type;
            DataType underlyingType = (fieldType as ArrayType)?.UnderlyingType;

            // It's okay if fieldMapping is buffered at this point because it could be the root node

            // Recursive case :: Collection of user-defined types :: Enumerate the fields of each nested type
            // Recursive case :: A single user-defined type       :: Recursively enumerate the fields of that single type
            //      Base case :: Not a user-defined type          :: Defer resolution to the SignalLookup by returning the field mapping
            if (fieldType.IsArray && underlyingType.IsUserDefined && !fieldMapping.IsBuffered)
                return EnumerateTypeMappings(fieldMapping.Expression).SelectMany(TraverseSignalMappings);

            if ((underlyingType ?? fieldType).IsUserDefined)
                return TraverseSignalMappings(fieldMapping.Expression);

            return new [] { fieldMapping };
        }

        /// <summary>
        /// Recursively traverses the data structures referenced by the type which is identified
        /// by the given identifier to find the full list of fields that map directly to signals.
        /// </summary>
        /// <param name="typeIdentifier">The identifier for the type mapping at which to begin the recursive search.</param>
        /// <returns>The full list of fields in types referenced by the type identified by the given identifier that map directly to signals.</returns>
        /// <exception cref="NotSupportedException">A nested buffer is detected during recursive traversal.</exception>
        public IEnumerable<FieldMapping> TraverseSignalMappings(string typeIdentifier)
        {
            return TraverseSignalMappings(GetTypeMapping(typeIdentifier));
        }

        /// <summary>
        /// Recursively traverses the data structures referenced by the given
        /// type to find the full list of fields that map directly to signals.
        /// </summary>
        /// <param name="typeMapping">The type at which to begin the recursive search.</param>
        /// <returns>The full list of fields in types referenced by the given type that map directly to signals.</returns>
        /// <exception cref="NotSupportedException">A nested buffer is detected during recursive traversal.</exception>
        public IEnumerable<FieldMapping> TraverseSignalMappings(TypeMapping typeMapping)
        {
            // The assumption here is that the root node of the recursive search
            // tree is a buffered mapping so any potential subtrees or leaf nodes
            // that are themselves buffered would represent a nested buffer
            foreach (FieldMapping fieldMapping in typeMapping.FieldMappings)
            {
                if (fieldMapping.IsBuffered)
                    throw new NotSupportedException("Nested buffering is not supported.");
            }

            return typeMapping.FieldMappings.SelectMany(TraverseSignalMappings);
        }

        /// <summary>
        /// Runs a validation routine to check for circular references and nested windows.
        /// </summary>
        public void ValidateDefinedMappings()
        {
            Dictionary<TypeMapping, TypeMappingInfo> infoLookup = new Dictionary<TypeMapping, TypeMappingInfo>();

            m_batchErrors.Clear();

            foreach (TypeMapping typeMapping in DefinedMappings)
            {
                try
                {
                    if (!infoLookup.ContainsKey(typeMapping))
                        ValidateFieldMappings(typeMapping, infoLookup);
                }
                catch (InvalidMappingException ex)
                {
                    m_batchErrors.Add(ex);
                }
                catch (SuppressedValidationException)
                {
                    // This represents a situation where a type mapping references
                    // another type mapping that has already been invalidated.
                    // The error was already accounted for the first
                    // time the referenced mapping was traversed
                }
            }
        }

        private bool ValidateFieldMappings(TypeMapping typeMapping, Dictionary<TypeMapping, TypeMappingInfo> infoLookup)
        {
            TypeMappingInfo info = infoLookup.GetOrAdd(typeMapping, mapping => new TypeMappingInfo());

            try
            {
                // Quit validation if this type is already known to be invalid
                // and we have already thrown an appropriate exception
                if (info.HasError)
                    throw new SuppressedValidationException();

                // If a mapping's fields have already been fully traversed,
                // there is no need to traverse them again
                if (info.IsValid)
                    return info.HasBufferedDescendant;

                // If a mapping has only been partially traversed,
                // then we must have arrived here via a circular reference
                if (info.Visited)
                    RaiseCompileError($"Circular reference detected when validating type mapping '{typeMapping.Identifier}'.");

                // Set visited flag to true to indicate
                // that we have begun field traversal
                info.Visited = true;

                foreach (FieldMapping fieldMapping in typeMapping.FieldMappings)
                {
                    DataType fieldType = fieldMapping.Field.Type;

                    // Make sure to check whether the field mapping
                    // is buffered before checking the base case
                    info.HasBufferedDescendant |= fieldMapping.IsBuffered;

                    // Base case: signal mappings do not
                    //            require additional validation
                    if (!fieldType.IsUserDefined)
                        continue;

                    if (fieldType.IsArray && !fieldMapping.IsBuffered)
                    {
                        // We know the field mapping is not buffered so we only need to check
                        // whether the referenced type mappings have buffered descendants
                        info.HasBufferedDescendant |= EnumerateTypeMappings(fieldMapping.Expression)
                            .Any(nestedMapping => ValidateFieldMappings(nestedMapping, infoLookup));
                    }
                    else
                    {
                        // Because we checked fieldMapping.IsBuffered at the beginning of the foreach loop,
                        // we do not need to check it again except to validate that there are no nested windows
                        if (ValidateFieldMappings(GetTypeMapping(fieldMapping.Expression), infoLookup))
                        {
                            if (fieldMapping.IsBuffered)
                                RaiseCompileError($"Nested window detected when validating field mapping '{typeMapping.Identifier}.{fieldMapping.Field.Identifier}'.");

                            // We know for sure that this type mapping has a buffered
                            // descendant if its referenced type mapping also has one
                            info.HasBufferedDescendant = true;
                        }
                    }
                }

                // Set is-valid flag to true to indicate
                // we have completed field traversal
                info.IsValid = true;

                return info.HasBufferedDescendant;
            }
            catch
            {
                info.HasError = true;
                throw;
            }
        }

        private void ParseTypeMapping()
        {
            string typeCategory;
            string typeIdentifier;
            DataType mappingType;

            TypeMapping typeMapping;
            Dictionary<string, UDTField> fieldLookup;

            // Assume the first token we encounter is the type identifier
            typeIdentifier = ParseIdentifier();
            SkipWhitespace();

            // Assume the second token we encounter is the mapping identifier
            typeMapping = new TypeMapping();
            typeMapping.Identifier = ParseIdentifier();
            SkipWhitespace();

            // If the next character we encounter is not '{' then
            // the first identifier was the type category,
            // the second identifier was the type identifier
            // the third identifier is the mapping identifier
            if (!m_endOfFile && m_currentChar != '{')
            {
                typeCategory = typeIdentifier;
                typeIdentifier = typeMapping.Identifier;
                typeMapping.Identifier = ParseIdentifier();
                SkipWhitespace();
            }
            else
            {
                typeCategory = null;
            }

            Assert('{');

            // Scan ahead to search
            // for the next newline
            ReadNextChar();
            SkipToNewline();
            Assert('\n');

            // Find the type used for the mapping
            if (!string.IsNullOrEmpty(typeCategory))
                mappingType = m_udtCompiler.GetType(typeCategory, typeIdentifier);
            else
                mappingType = m_udtCompiler.GetType(typeIdentifier);

            if ((object)mappingType == null)
                RaiseCompileError($"Type {typeIdentifier} was not found but is used in the definition for mapping {typeMapping.Identifier}.");

            if (!mappingType.IsUserDefined)
                RaiseCompileError($"Type {typeIdentifier} is not a user defined type but is used in the definition for mapping {typeMapping.Identifier}.");

            typeMapping.Type = (UserDefinedType)mappingType;
            fieldLookup = typeMapping.Type.Fields.ToDictionary(field => field.Identifier);

            // Parse field mappings
            SkipWhitespace();

            while (!m_endOfFile && m_currentChar != '}')
            {
                typeMapping.FieldMappings.Add(ParseFieldMapping(typeMapping, fieldLookup));
                SkipWhitespace();
            }

            Assert('}');
            ReadNextChar();

            // Identifiers are required to be unique
            if (m_definedMappings.ContainsKey(typeMapping.Identifier))
                RaiseCompileError($"Type {typeMapping.Identifier} has already been defined.");

            // Add the type mapping to the lookup table for defined mappings
            m_definedMappings.Add(typeMapping.Identifier, typeMapping);
            m_mappingTable = null;
        }

        private FieldMapping ParseFieldMapping(TypeMapping typeMapping, Dictionary<string, UDTField> fieldLookup)
        {
            UDTField field;
            FieldMapping fieldMapping;
            string fieldIdentifier;

            // Read the field identifier
            fieldIdentifier = ParseIdentifier();
            SkipWhitespace();
            Assert(':');
            ReadNextChar();
            SkipWhitespace();

            if (m_endOfFile)
                RaiseCompileError("Unexpected end of file. Expected '{', identifier, or signal ID.");

            // Look up the field based on the field identifier
            if (!fieldLookup.TryGetValue(fieldIdentifier, out field))
                RaiseCompileError($"Field {fieldIdentifier} not defined for type {typeMapping.Type.Identifier} but is used in the definition for mapping {typeMapping.Identifier}.");

            if (!field.Type.IsArray && !field.Type.IsUserDefined)
                fieldMapping = ParseSignalMapping();
            else if (!field.Type.IsArray)
                fieldMapping = ParseMappingMapping();
            else if (!field.Type.IsUserDefined)
                fieldMapping = ParseArraySignalMapping();
            else
                fieldMapping = ParseArrayMappingMapping();

            SkipToNewline();
            Assert('\n');
            fieldMapping.Field = field;

            return fieldMapping;
        }

        private FieldMapping ParseSignalMapping()
        {
            FieldMapping fieldMapping = new FieldMapping();

            if (m_endOfFile)
                RaiseCompileError("Unexpected end of file. Expected '{' or signal ID.");

            if (m_currentChar == '{')
                fieldMapping.Expression = ReadToClosingBrace().Trim();
            else
                fieldMapping.Expression = ReadToWhiteSpace();

            SkipToNewline();

            if (!m_endOfFile && m_currentChar != '\n')
                ParseRelativeTime(fieldMapping);

            SkipToNewline();

            if (!m_endOfFile && m_currentChar == '@')
                ParseSampleRate(fieldMapping);

            return fieldMapping;
        }

        private FieldMapping ParseMappingMapping()
        {
            FieldMapping fieldMapping = new FieldMapping();

            if (m_endOfFile)
                RaiseCompileError("Unexpected end of file. Expected '{' or identifier.");

            if (m_currentChar == '{')
                fieldMapping.Expression = ReadToClosingBrace().Trim();
            else
                fieldMapping.Expression = ParseIdentifier();

            SkipToNewline();

            if (!m_endOfFile && m_currentChar != '\n')
                ParseRelativeTime(fieldMapping);

            SkipToNewline();

            if (!m_endOfFile && m_currentChar == '@')
                ParseSampleRate(fieldMapping);

            return fieldMapping;
        }

        private ArrayMapping ParseArraySignalMapping()
        {
            ArrayMapping arrayMapping = new ArrayMapping();

            if (m_endOfFile)
                RaiseCompileError("Unexpected end of file. Expected '{' or signal ID.");

            if (m_currentChar == '{')
                arrayMapping.Expression = ReadToClosingBrace().Trim();
            else
                arrayMapping.Expression = ReadToWhiteSpace();

            SkipToNewline();

            if (m_endOfFile)
                RaiseCompileError("Unexpected end of file. Expected 'last' keyword, 'from' keyword, number, or newline.");

            if (char.IsDigit(m_currentChar))
                ParseRelativeTime(arrayMapping);
            else if (m_currentChar != '\n')
                ParseWindowExpression(arrayMapping);

            SkipToNewline();

            if (!m_endOfFile && m_currentChar == '@')
                ParseSampleRate(arrayMapping);

            return arrayMapping;
        }

        private ArrayMapping ParseArrayMappingMapping()
        {
            ArrayMapping arrayMapping = new ArrayMapping();

            if (m_endOfFile)
                RaiseCompileError("Unexpected end of file. Expected '{' or identifier.");

            if (m_currentChar == '{')
                arrayMapping.Expression = ReadToClosingBrace().Trim();
            else
                arrayMapping.Expression = ParseIdentifier();

            SkipToNewline();

            if (m_endOfFile)
                RaiseCompileError("Unexpected end of file. Expected 'last' keyword, 'from' keyword, number, or newline.");

            if (char.IsDigit(m_currentChar))
                ParseRelativeTime(arrayMapping);
            else if (m_currentChar != '\n')
                ParseWindowExpression(arrayMapping);

            SkipToNewline();

            if (!m_endOfFile && m_currentChar == '@')
                ParseSampleRate(arrayMapping);

            return arrayMapping;
        }

        private void ParseWindowExpression(ArrayMapping arrayMapping)
        {
            string identifier;

            // Read the next token as an identifier
            identifier = ParseIdentifier();
            arrayMapping.TimeWindowExpression = identifier;
            SkipWhitespace();

            if (identifier.Equals("last", StringComparison.OrdinalIgnoreCase))
            {
                // The "last" keyword is followed by a
                // timespan and an optional sample rate
                ParseTimeSpan(arrayMapping);
                arrayMapping.RelativeTime = arrayMapping.WindowSize;
                arrayMapping.RelativeUnit = arrayMapping.WindowUnit;
            }
            else if (identifier.Equals("from", StringComparison.OrdinalIgnoreCase))
            {
                // The from keyword is followed by
                // a relative time and a time span
                ParseRelativeTime(arrayMapping);
                SkipWhitespace();

                // Read the next token as an identifier
                identifier = ParseIdentifier();
                arrayMapping.TimeWindowExpression += " " + identifier;
                SkipWhitespace();

                // The "for" keyword is expected to exist
                // between the relative time and the time span
                if (identifier != "for")
                    RaiseCompileError($"Unexpected identifier: {identifier}. Expected 'for' keyword.");

                // Parse the time span
                ParseTimeSpan(arrayMapping);
            }
            else
            {
                // If the "last" and "from" keywords are not specified, raise an error
                RaiseCompileError($"Unexpected identifier: {identifier}. Expected 'last' keyword or 'from' keyword.");
            }
        }

        private void ParseRelativeTime(FieldMapping fieldMapping)
        {
            string identifier;
            TimeSpan timeUnit;

            // Parse the relative time
            fieldMapping.RelativeTime = ParseNumber();

            if ((object)fieldMapping.TimeWindowExpression == null)
                fieldMapping.TimeWindowExpression = fieldMapping.RelativeTime.ToString();
            else
                fieldMapping.TimeWindowExpression += " " + fieldMapping.RelativeTime;

            SkipWhitespace();

            // Parse the next token as an identifier
            identifier = ParseIdentifier();
            fieldMapping.TimeWindowExpression += " " + identifier;
            timeUnit = ToTimeUnit(identifier);

            if (identifier.Equals("point", StringComparison.OrdinalIgnoreCase) || identifier.Equals("points", StringComparison.OrdinalIgnoreCase))
            {
                // The "points" keyword is followed by the
                // "ago" keyword and an optional sample rate
                SkipWhitespace();

                identifier = ParseIdentifier();
                fieldMapping.TimeWindowExpression += " " + identifier;

                if (identifier != "ago")
                    RaiseCompileError($"Unexpected identifier: {identifier}. Expected 'ago' keyword.");
            }
            else if (timeUnit != TimeSpan.Zero)
            {
                // If the identifier was a time unit,
                // set RelativeUnit and parse the "ago" keyword
                fieldMapping.RelativeUnit = timeUnit;
                SkipWhitespace();

                identifier = ParseIdentifier();
                fieldMapping.TimeWindowExpression += " " + identifier;

                if (identifier != "ago")
                    RaiseCompileError($"Unexpected identifier: {identifier}. Expected 'ago' keyword.");
            }
            else
            {
                // If the identifier was not the ago keyword, raise an error
                RaiseCompileError($"Unexpected identifier: {identifier}. Expected 'points' keyword or time unit.");
            }
        }

        private void ParseTimeSpan(ArrayMapping arrayMapping)
        {
            string identifier;
            TimeSpan timeUnit;

            // Parse the relative time
            arrayMapping.WindowSize = ParseNumber();
            arrayMapping.TimeWindowExpression += " " + arrayMapping.WindowSize;
            SkipWhitespace();

            // Parse the next token as an identifier
            identifier = ParseIdentifier();
            arrayMapping.TimeWindowExpression += " " + identifier;
            timeUnit = ToTimeUnit(identifier);

            if (timeUnit != TimeSpan.Zero)
            {
                // If the identifier was a time unit, set WindowUnit
                arrayMapping.WindowUnit = timeUnit;
            }
            else if (!identifier.Equals("point", StringComparison.OrdinalIgnoreCase) && !identifier.Equals("points", StringComparison.OrdinalIgnoreCase))
            {
                // If the identifier was not the ago keyword, raise an error
                RaiseCompileError($"Unexpected identifier: {identifier}. Expected 'points' keyword or time unit.");
            }
        }

        private void ParseSampleRate(FieldMapping fieldMapping)
        {
            string identifier;

            // Check for the '@' character
            Assert('@');
            ReadNextChar();
            SkipWhitespace();

            // Parse the next token as the sample rate
            fieldMapping.SampleRate = ParseNumber();
            fieldMapping.TimeWindowExpression += " @ " + fieldMapping.SampleRate;
            SkipWhitespace();

            // Parse the "per" keyword
            identifier = ParseIdentifier();
            fieldMapping.TimeWindowExpression += " " + identifier;
            SkipWhitespace();

            if (identifier != "per")
                RaiseCompileError($"Unexpected identifier: {identifier}. Expected 'per' keyword.");

            // Parse the time unit
            identifier = ParseIdentifier();
            fieldMapping.TimeWindowExpression += " " + identifier;
            fieldMapping.SampleUnit = ToTimeUnit(identifier);

            if (fieldMapping.SampleUnit == TimeSpan.Zero)
                RaiseCompileError($"Unexpected identifier: {identifier}. Expected time unit.");
        }

        private string ParseIdentifier()
        {
            StringBuilder builder = new StringBuilder();

            Func<char, bool> isIdentifierChar = c =>
                char.IsLetterOrDigit(c) ||
                c == '_';

            // If the identifier starts with a digit, raise an error
            if (!m_endOfFile && char.IsDigit(m_currentChar))
                RaiseCompileError($"Invalid character for start of identifier: '{GetCharText(m_currentChar)}'. Expected letter or underscore.");

            // Read characters as long as they are valid for an identifier
            while (!m_endOfFile && isIdentifierChar(m_currentChar))
            {
                builder.Append(m_currentChar);
                ReadNextChar();
            }

            // If no valid characters were encountered, raise an error
            if (builder.Length == 0)
            {
                if (m_endOfFile)
                    RaiseCompileError("Unexpected end of file. Expected identifier.");
                else
                    RaiseCompileError($"Unexpected character: '{GetCharText(m_currentChar)}'. Expected identifier.");
            }

            return builder.ToString();
        }

        private decimal ParseNumber()
        {
            StringBuilder builder = new StringBuilder();
            decimal number;

            // Read characters as long as they are valid for a number
            while (!m_endOfFile && (char.IsDigit(m_currentChar) || m_currentChar == '.'))
            {
                builder.Append(m_currentChar);
                ReadNextChar();
            }

            // If the characters that were read cannot
            // be interpreted as a number, raise an error
            if (!decimal.TryParse(builder.ToString(), out number))
                RaiseCompileError($"Invalid format for number: '{builder}'.");

            return number;
        }

        private TimeSpan ToTimeUnit(string identifier)
        {
            switch (identifier)
            {
                case "microsecond":
                case "microseconds":
                    return TimeSpan.FromTicks(10L);

                case "millisecond":
                case "milliseconds":
                    return TimeSpan.FromMilliseconds(1.0D);

                case "second":
                case "seconds":
                    return TimeSpan.FromSeconds(1.0D);

                case "minute":
                case "minutes":
                    return TimeSpan.FromMinutes(1.0D);

                case "hour":
                case "hours":
                    return TimeSpan.FromHours(1.0D);

                case "day":
                case "days":
                    return TimeSpan.FromDays(1.0D);

                default:
                    return TimeSpan.Zero;
            }
        }

        private string ReadToClosingBrace()
        {
            StringBuilder builder = new StringBuilder();

            Assert('{');

            do
            {
                ReadNextChar();

                while (!m_endOfFile && m_currentChar != '}')
                {
                    builder.Append(m_currentChar);
                    ReadNextChar();
                }

                ReadNextChar();

                if (!m_endOfFile && m_currentChar == '}')
                    builder.Append(m_currentChar);
            }
            while (!m_endOfFile && m_currentChar == '}');

            return builder.ToString();
        }

        private string ReadToWhiteSpace()
        {
            StringBuilder builder = new StringBuilder();

            while (!m_endOfFile && !char.IsWhiteSpace(m_currentChar))
            {
                builder.Append(m_currentChar);
                ReadNextChar();
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
            if ((object)m_mappingFile == null)
                throw new InvalidMappingException(message);

            string fileName = Path.GetFileName(m_mappingFile);
            string exceptionMessage = $"Error compiling {fileName}: {message}";
            throw new InvalidMappingException(exceptionMessage, m_mappingFile, File.ReadAllText(m_mappingFile));
        }

        private DataTable GetMappingTable()
        {
            DataTable mappingTable = new DataTable();

            mappingTable.Columns.Add("TypeCategory", typeof(string));
            mappingTable.Columns.Add("TypeIdentifier", typeof(string));
            mappingTable.Columns.Add("MappingIdentifier", typeof(string));

            foreach (TypeMapping mapping in DefinedMappings)
                mappingTable.Rows.Add(mapping.Type.Category, mapping.Type.Identifier, mapping.Identifier);

            return mappingTable;
        }

        #endregion

        #region [ Static ]

        // Static Methods

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
