﻿//******************************************************************************************************
//  FieldMapping.cs - Gbtc
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
    public class FieldMapping
    {
        public UDTField Field { get; set; }

        public string Expression { get; set; }

        public decimal RelativeTime { get; set; }

        public TimeSpan RelativeUnit { get; set; }

        public decimal SampleRate { get; set; }

        public TimeSpan SampleUnit { get; set; }

        public string TimeWindowExpression { get; set; }

        public bool IsBuffered => RelativeTime != 0.0M;
    }
}
