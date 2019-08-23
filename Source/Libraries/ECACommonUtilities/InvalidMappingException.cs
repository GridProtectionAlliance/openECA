//******************************************************************************************************
//  InvalidMappingException.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace ECACommonUtilities
{
    public class InvalidMappingException : Exception
    {
        #region [ Constructors ]

        public InvalidMappingException(string message)
            : base(message)
        {
        }

        public InvalidMappingException(string message, string filePath, string fileContents)
            : base(message)
        {
            FilePath = filePath;
            FileContents = fileContents;
        }

        public InvalidMappingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public InvalidMappingException(string message, string filePath, string fileContents, Exception innerException)
            : base(message, innerException)
        {
            FilePath = filePath;
            FileContents = fileContents;
        }

        protected InvalidMappingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            FilePath = info.GetString("FilePath");
            FileContents = info.GetString("FileContents");
        }

        #endregion

        #region [ Properties ]

        public string FilePath { get; }

        public string FileContents { get; }

        #endregion

        #region [ Methods ]

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if ((object)info == null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("FilePath", FilePath);
            info.AddValue("FileContents", FileContents);

            base.GetObjectData(info, context);
        }

        #endregion
    }
}