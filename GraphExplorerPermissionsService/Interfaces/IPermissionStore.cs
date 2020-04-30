// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GraphExplorerPermissionsService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphExplorerPermissionsService.Interfaces
{
    /// <summary>
    /// Defines an interface that provides a method for fetching permissions scopes.
    /// </summary>
    public interface IPermissionsStore
    {
        Task<List<ScopeInformation>> GetScopesAsync(string scopeType = "DelegatedWork",
                                                    string localeCode = null,
                                                    string requestUrl = null,
                                                    string method = null);
    }
}
