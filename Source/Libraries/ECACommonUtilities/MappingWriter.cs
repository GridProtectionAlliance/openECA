//******************************************************************************************************
//  MappingWriter.cs - Gbtc
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
using ECACommonUtilities.Model;

namespace ECACommonUtilities
{
    /// <summary>
    /// Writes type mappings to files and streams.
    /// </summary>
    public class MappingWriter
    {
        #region [ Members ]

        // Fields
        private List<TypeMapping> m_mappings;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="MappingWriter"/> class.
        /// </summary>
        public MappingWriter()
        {
            m_mappings = new List<TypeMapping>();
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the list of types to be written to the file.
        /// </summary>
        public List<TypeMapping> Mappings
        {
            get
            {
                return m_mappings;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Writes the list of mappings to separate files under the given path.
        /// </summary>
        /// <param name="directoryPath">The path to the directory containing the mappings.</param>
        public void WriteFiles(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);

            foreach (TypeMapping typeMapping in m_mappings)
            {
                string mappingPath = Path.Combine(directoryPath, typeMapping.Identifier + ".ecamap");

                using (TextWriter writer = File.CreateText(mappingPath))
                {
                    Write(writer, typeMapping);
                }
            }
        }

        /// <summary>
        /// Writes the list of mappings to the given file.
        /// </summary>
        /// <param name="filePath">The path to the file to be written.</param>
        public void Write(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);

            if ((object)directory != null)
                Directory.CreateDirectory(directory);

            using (TextWriter writer = File.CreateText(filePath))
            {
                Write(writer);
            }
        }

        /// <summary>
        /// Writes the list of mappings to the given stream.
        /// </summary>
        /// <param name="stream">The stream to which the mappings will be written.</param>
        public void Write(Stream stream)
        {
            using (TextWriter writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
            {
                Write(writer);
            }
        }

        /// <summary>
        /// Writes the list of mappings using the given writer.
        /// </summary>
        /// <param name="writer">The writer used to write the mappings.</param>
        public void Write(TextWriter writer)
        {
            foreach (TypeMapping typeMapping in m_mappings)
                Write(writer, typeMapping);
        }

        private void Write(TextWriter writer, TypeMapping typeMapping)
        {
            writer.WriteLine($"{typeMapping.Type.Category} {typeMapping.Type.Identifier} {typeMapping.Identifier} {{");

            foreach (FieldMapping fieldMapping in typeMapping.FieldMappings)
            {
                ArrayMapping arrayMapping = fieldMapping as ArrayMapping;
                string expression = fieldMapping.Expression.Any(char.IsWhiteSpace) ? $"{{ {fieldMapping.Expression} }}" : fieldMapping.Expression;
                string fieldMappingText;

                if ((object)arrayMapping == null)
                    fieldMappingText = $"    {fieldMapping.Field.Identifier}: {expression} {ToRelativeTimeText(fieldMapping)}";
                else if (arrayMapping.WindowSize == 0.0M)
                    fieldMappingText = $"    {fieldMapping.Field.Identifier}: {{ {fieldMapping.Expression} }} {ToRelativeTimeText(fieldMapping)}";
                else if (fieldMapping.RelativeTime == 0.0M)
                    fieldMappingText = $"    {fieldMapping.Field.Identifier}: {expression} last {ToTimeSpanText(arrayMapping)}";
                else if (fieldMapping.RelativeTime != arrayMapping.WindowSize || fieldMapping.RelativeUnit != arrayMapping.WindowUnit)
                    fieldMappingText = $"    {fieldMapping.Field.Identifier}: {expression} from {ToRelativeTimeText(fieldMapping)} for {ToTimeSpanText(arrayMapping)}";
                else
                    fieldMappingText = $"    {fieldMapping.Field.Identifier}: {expression} last {ToTimeSpanText(arrayMapping)}";

                if (fieldMapping.SampleRate != 0.0M)
                    writer.WriteLine($"{fieldMappingText} {ToSampleRateText(fieldMapping)}");
                else
                    writer.WriteLine(fieldMappingText);
            }

            writer.WriteLine("}");
            writer.WriteLine();
        }

        private string ToRelativeTimeText(FieldMapping fieldMapping)
        {
            decimal relativeTime = fieldMapping.RelativeTime;
            TimeSpan relativeUnit = fieldMapping.RelativeUnit;

            if (relativeTime == 0.0M)
                return string.Empty;

            if (relativeUnit != TimeSpan.Zero)
            {
                return (relativeTime != 1.0M)
                    ? $"{relativeTime} {ToUnitText(relativeUnit)}s ago"
                    : $"{relativeTime} {ToUnitText(relativeUnit)} ago";
            }

            return (relativeTime != 1.0M)
                ? $"{relativeTime} points ago"
                : $"{relativeTime} point ago";
        }

        private string ToTimeSpanText(ArrayMapping arrayMapping)
        {
            decimal windowSize = arrayMapping.WindowSize;
            TimeSpan windowUnit = arrayMapping.WindowUnit;

            if (windowSize == 0.0M)
                return string.Empty;

            if (windowUnit != TimeSpan.Zero)
            {
                return (windowSize != 1.0M)
                    ? $"{windowSize} {ToUnitText(windowUnit)}s"
                    : $"{windowSize} {ToUnitText(windowUnit)}";
            }

            return (windowSize != 1.0M)
                ? $"{windowSize} points"
                : $"{windowSize} point";
        }

        private string ToSampleRateText(FieldMapping fieldMapping)
        {
            decimal rate = fieldMapping.SampleRate;
            TimeSpan unit = fieldMapping.SampleUnit;
            return $"@ {rate} per {ToUnitText(unit)}";
        }

        private string ToUnitText(TimeSpan unit)
        {
            switch (unit.Ticks)
            {
                case 10L: return "microsecond";
                case TimeSpan.TicksPerMillisecond: return "millisecond";
                case TimeSpan.TicksPerSecond: return "second";
                case TimeSpan.TicksPerMinute: return "minute";
                case TimeSpan.TicksPerHour: return "hour";
                default: return "day";
            }
        }

        #endregion
    }
}
