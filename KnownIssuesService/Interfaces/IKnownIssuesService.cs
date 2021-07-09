// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using KnownIssuesService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KnownIssuesService.Interfaces
{
    public interface IKnownIssuesService
    {
        Task<List<KnownIssuesContract>> QueryBugs();

    }
}
