using System.Collections.Generic;
using Newtonsoft.Json;

namespace PermissionsService.Models
{
    public class PermissionResult
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<ScopeInformation> Results
        {
            get; set;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<PermissionError> Errors
        {
            get; set;
        }
    }

    public class PermissionError
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RequestUrl
        {
            get; set;
        }

        public string Message
        {
            get; set;
        } = string.Empty;
    }
}
