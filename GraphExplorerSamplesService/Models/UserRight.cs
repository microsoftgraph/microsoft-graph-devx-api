using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace GraphExplorerSamplesService.Models
{
    /// <summary>
    /// Defines a representation of a user access right.
    /// </summary>
    public class UserRight
    {
        [JsonProperty(PropertyName = "userId")]
        public Guid UserId { get; set; }
        [JsonProperty(PropertyName = "userName")]
        public string UserName { get; set; }
        [JsonProperty(PropertyName = "userEmail")]
        public string UserEmail { get; set; }
        [JsonProperty(PropertyName = "userPermissions")]
        public List<Permission> UserPermissions { get; set; }
    }
}
