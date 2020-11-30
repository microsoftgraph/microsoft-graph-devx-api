// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GraphExplorerSamplesService.Models;
using System.Threading.Tasks;

namespace GraphExplorerSamplesService.Interfaces
{
    public interface ISamplesTestStore
    {
        Task<SampleQueriesList> FetchSampleQueriesListAsync(string locale, string org, string branchName);
    }
}
