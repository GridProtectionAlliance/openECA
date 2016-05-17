﻿//*******************************************************************************************************
//  IHistorianMetadataService.cs - Gbtc
//
//  Tennessee Valley Authority, 2009
//  No copyright is claimed pursuant to 17 USC § 105.  All Other Rights Reserved.
//
//  Code Modification History:
//  -----------------------------------------------------------------------------------------------------
//  11/25/2009 - Pinal C. Patel
//       Generated original version of source code.
//
//*******************************************************************************************************

using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace openECAServices
{
    [ServiceContract()]
    public interface IHistorianMetadataService
    {
        #region [ Methods ]

        [OperationContract(),
        WebGet(UriTemplate = "/historianmetadata/{historianInstance}")]
        Stream GetMetadata(string historianInstance);

        #endregion
    }
}
