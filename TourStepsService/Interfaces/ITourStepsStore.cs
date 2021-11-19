// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using TourStepsService.Models;

namespace TourStepsService.Interfaces
{
    public interface ITourStepsStore
    {
        Task<TourStepsList> FetchTourStepsListAsync(string locale);
        Task<TourStepsList> FetchTourStepsListAsync(string locale, string org, string branchName);
    }
}
