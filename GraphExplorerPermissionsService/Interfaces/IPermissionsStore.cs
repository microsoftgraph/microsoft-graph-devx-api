using System;
using System.Collections.Generic;
using System.Text;

namespace GraphExplorerPermissionsService.Interfaces
{
    public interface IPermissionsStore
    {
        string[] GetScopes(string requestUrl, string method = "GET", string scopeType = "DelegatedWork");
    }
}
