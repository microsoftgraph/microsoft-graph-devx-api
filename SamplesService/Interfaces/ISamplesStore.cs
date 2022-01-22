// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using SamplesService.Models;
using System.Threading.Tasks;

namespace SamplesService.Interfaces
{
    public interface ISamplesStore
    {
        Task<SampleQueriesList> FetchSampleQueriesListAsync(string locale);
        Task<SampleQueriesList> FetchSampleQueriesListAsync(string locale, string org, string branchName);
    }
}
