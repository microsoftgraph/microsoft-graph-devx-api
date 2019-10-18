namespace GraphExplorerPermissionsService.Interfaces
{
    public interface IPermissionsStore
    {
        string[] GetScopes(string requestUrl, string method = "GET", string scopeType = "DelegatedWork");
    }
}
