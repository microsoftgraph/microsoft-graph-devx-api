// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace PermissionsService.Models
{
    /// <summary>
    /// Defines a list which holds a collection of delegated and application <see cref="ScopeInformation"/> objects.
    /// </summary>
    internal class ScopesInformationList
    {
        public List<ScopeInformation> DelegatedScopesList { get; set; }
        public List<ScopeInformation> ApplicationScopesList { get; set; }

        public ScopesInformationList()
        {
            DelegatedScopesList = new List<ScopeInformation>();
            ApplicationScopesList = new List<ScopeInformation>();
        }
    }
}
