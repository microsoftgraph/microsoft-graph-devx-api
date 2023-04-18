using Newtonsoft.Json;

namespace PermissionsService.Models
{
    public class RequestInfo
    {
        [JsonProperty(PropertyName = "requestUrl")]
        public string RequestUrl
        {
            get; set;
        }

        [JsonProperty(PropertyName = "method")]
        public string HttpMethod
        {
            get; set;
        }
    }
}
