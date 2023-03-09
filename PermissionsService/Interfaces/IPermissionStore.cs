// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using PermissionsService.Models;
using UriMatchingService;

namespace PermissionsService.Interfaces
{
    /// <summary>
    /// Defines an interface that provides a method for fetching permissions scopes.
    /// </summary>
    public interface IPermissionsStore
    {
        /// <summary>
        /// Retrieves permissions scopes information for a set of URLs.
        /// </summary>
        /// <param name="requestUrls">The list of request URLs to fetch permissions for.</param>
        /// <param name="locale">Optional: The language code for the preferred localized file.</param>
        /// <param name="scopeType">Optional: The type of scope to be retrieved for the target request url.</param>
        /// <param name="method">Optional: The target http verb of the request url whose scopes are to be retrieved.</param>
        /// <param name="includeHidden">Optional: Whether to include hidden permissions or not: Defaults to false.</param>
        /// <param name="isLeastPrivilege">Optional: Whether to only return least privilege permissions on not. Defaults to false.</param>
        /// <param name="org">Optional: The name of the org/owner of the repo.</param>
        /// <param name="branchName">Optional: The name of the branch containing the files</param>
        /// <returns></returns>
        Task<PermissionResult> GetScopesAsync(List<string> requestUrls = null,
                                                   string locale = null,
                                                   ScopeType? scopeType = null,
                                                   string method = null,
                                                   bool includeHidden = false,
                                                   bool leastPrivilegeOnly = false,
                                                   string org = null,
                                                   string branchName = null);

        /// <summary>
        /// Gets an instance of <see cref="UriTemplateMatcher"/> seeded with url templates.
        /// </summary>
        /// <returns>An instance of <see cref="UriTemplateMatcher"/> seeded with url templates.</returns>
        UriTemplateMatcher GetUriTemplateMatcher();
    }
}
