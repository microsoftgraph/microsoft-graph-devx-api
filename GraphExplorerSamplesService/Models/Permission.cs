using Newtonsoft.Json;

namespace GraphExplorerSamplesService.Models
{
    /// <summary>
    /// Defines a representation of a claim permission.
    /// </summary>
    public class Permission
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "value")]
        public bool Value { get; set; }
    }
}
