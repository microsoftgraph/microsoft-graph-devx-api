using Newtonsoft.Json;

namespace GraphExplorerSamplesService.Models
{
    /// <summary>
    /// Defines a representation of an access permission.
    /// </summary>
    public class Permission
    {
        [JsonProperty (PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty (PropertyName = "value")]
        public string Value { get; set; }
    }
}
