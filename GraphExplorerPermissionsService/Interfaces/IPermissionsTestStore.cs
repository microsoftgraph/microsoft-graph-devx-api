using GraphExplorerPermissionsService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphExplorerPermissionsService.Interfaces
{
    public interface IPermissionsTestStore
    {
        Task<List<ScopeInformation>> FetchPermissionsDescriptionsFromGithub(string locale, string org, string branchName);
    }
}
