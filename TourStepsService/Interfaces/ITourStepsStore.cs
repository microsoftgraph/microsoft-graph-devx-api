using System;
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
