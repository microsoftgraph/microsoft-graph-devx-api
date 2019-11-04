// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GraphExplorerPermissionsService.Models;
using System.Collections.Generic;

namespace GraphExplorerPermissionsService.Interfaces
{
    public interface IPermissionsStore
    {
        List<ScopeInformation> GetScopes(string requestUrl, string method = "GET", string scopeType = "DelegatedWork");
    }
}
