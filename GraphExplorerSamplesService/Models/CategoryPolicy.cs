using Newtonsoft.Json;
using System.Collections.Generic;

namespace GraphExplorerSamplesService.Models
{
    /// <summary>
    /// Defines a representation of a category policy.
    /// </summary>
    public class CategoryPolicy
    {
        [JsonProperty(PropertyName = "categoryName")]
        public string CategoryName { get; set; }
        [JsonProperty(PropertyName = "userClaims")]
        public List<UserClaim> UserClaims { get; set; }

        public CategoryPolicy()
        {
            CategoryName = "Unspecified";
            UserClaims = new List<UserClaim>();
        }
    }
}
