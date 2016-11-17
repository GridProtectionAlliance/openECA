//******************************************************************************************************
//  ArrayType.cs - Gbtc
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

namespace ECACommonUtilities.Model
{
    /// <summary>
    /// Represents a data type which is an array of another underlying type.
    /// </summary>
    public class ArrayType : DataType
    {
        #region [ Members ]

        // Fields
        private DataType m_underlyingType;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="ArrayType"/> class.
        /// </summary>
        /// <param name="underlyingType">The underlying type being made into an array.</param>
        public ArrayType(DataType underlyingType)
        {
            m_underlyingType = underlyingType;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the category of the underlying type.
        /// </summary>
        public override string Category
        {
            get
            {
                return UnderlyingType.Category;
            }

            set
            {
                UnderlyingType.Category = value;
            }
        }

        /// <summary>
        /// Gets or sets the identifier of the underlying type.
        /// </summary>
        public override string Identifier
        {
            get
            {
                return UnderlyingType.Identifier + "[]";
            }

            set
            {
                if (!value.EndsWith("[]"))
                    throw new ArgumentException($"Invalid identifier: {value}. Array type identifiers must be annotated with square brackets.", nameof(value));

                base.Identifier = value.Remove(value.Length - 3);
            }
        }

        /// <summary>
        /// Gets the underlying type of the array.
        /// </summary>
        public DataType UnderlyingType
        {
            get
            {
                return m_underlyingType;
            }
        }

        /// <summary>
        /// Returns true for array types.
        /// </summary>
        public override bool IsArray
        {
            get
            {
                return true;
            }
        }

        #endregion
    }
}
