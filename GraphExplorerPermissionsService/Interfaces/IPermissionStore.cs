// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GraphExplorerPermissionsService.Models;
using System.Collections.Generic;

namespace GraphExplorerPermissionsService.Interfaces
{
    /// <summary>
    /// Defines an interface that provides a method for fetching permission scopes.
    /// </summary>
    public interface IPermissionsStore
    {
        List<ScopeInformation> GetScopes(string scopeType = "DelegatedWork", string requestUrl = null, string method = null, string localeCode = null);
    }
}
