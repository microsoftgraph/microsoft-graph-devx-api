using System.Collections.Generic;
using System.Net;

namespace PermissionsService.Models
{
    public class PermissionResult
    {
        public List<ScopeInformation> Results
        {
            get; set;
        } = new();

        public List<PermissionError> Errors
        {
            get; set;
        } = new();
    }

    public class PermissionError
    {
        public string Url
        {
            get; set;
        } = string.Empty;

        public string Message
        {
            get; set;
        } = string.Empty;
    }
}
