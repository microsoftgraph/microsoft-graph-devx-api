using Newtonsoft.Json;
using System.Collections.Generic;

namespace GraphExplorerSamplesService.Models
{
    /// <summary>
    /// Defines a representation of the access rights allowable for a category.
    /// </summary>
    public class CategoryRights
    {
        [JsonProperty(PropertyName = "categoryName")]
        public string CategoryName { get; set; }
        [JsonProperty(PropertyName = "userRights")]
        public List<UserRight> UserRights { get; set; }
    }
}
