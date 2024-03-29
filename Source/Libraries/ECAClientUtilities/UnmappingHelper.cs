﻿//******************************************************************************************************
//  UnmappingHelper.cs - Gbtc
//
//  Copyright © 2017, Grid Protection Alliance.  All Rights Reserved.
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
//  01/16/2017 - Stephen C. Wills
//       Generated original version of source code.
//  09/12/2019 - Christoph Lackner
//       Added Methods from UnmapperBase for Matlab support.
//
//******************************************************************************************************

using ECAClientFramework;
using ECACommonUtilities;
using ECACommonUtilities.Model;
using GSF.TimeSeries;

namespace ECAClientUtilities
{
    public class UnmappingHelper : UnmapperBase
    {
        public UnmappingHelper(Framework framework, MappingCompiler mappingCompiler)
            : base(framework, mappingCompiler, SystemSettings.OutputMapping)
        {
        }

        // Matlab doesn't see base class members, so we "re-expose" them here...

        public new void BurnKeyIndex()
        {
            base.BurnKeyIndex();
        }
        
        public new MetaValues CreateMetaValues(FieldMapping fieldMapping)
        {
            return base.CreateMetaValues(fieldMapping);
        }

        public new void Reset()
        {
            base.Reset();
        }

        public new IMeasurement MakeMeasurement(MetaValues meta, double value)
        {
            return base.MakeMeasurement(meta, value);
        }

    }
}
