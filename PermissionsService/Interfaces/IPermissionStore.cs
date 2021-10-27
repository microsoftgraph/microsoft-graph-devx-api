// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using PermissionsService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using UriMatchingService;

namespace PermissionsService.Interfaces
{
    /// <summary>
    /// Defines an interface that provides a method for fetching permissions scopes.
    /// </summary>
    public interface IPermissionsStore
    {
        /// <summary>
        /// Retrieves permissions scopes information.
        /// </summary>
        /// <param name="scopeType">The type of scope to be retrieved for the target request url.</param>
        /// <param name="locale">Optional: The language code for the preferred localized file.</param>
        /// <param name="requestUrl">Optional: The target request url whose scopes are to be retrieved.</param>
        /// <param name="method">Optional: The target http verb of the request url whose scopes are to be retrieved.</param>
        /// <param name="org">Optional: The name of the org/owner of the repo.</param>
        /// <param name="branchName">Optional: The name of the branch containing the files.</param>
        /// <returns>A list of <see cref="ScopeInformation"/>.</returns>
        Task<List<ScopeInformation>> GetScopesAsync(string scopeType = "DelegatedWork",
                                                    string locale = null,
                                                    string requestUrl = null,
                                                    string method = null,
                                                    string org = null,
                                                    string branchName = null);

        /// <summary>
        /// Gets an instance of <see cref="UriTemplateMatcher"/> seeded with url templates.
        /// </summary>
        /// <returns>An instance of <see cref="UriTemplateMatcher"/> seeded with url templates.</returns>
        UriTemplateMatcher GetUriTemplateMatcher();
    }
}
