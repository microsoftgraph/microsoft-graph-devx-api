using Newtonsoft.Json;
using System.Collections.Generic;

namespace GraphExplorerSamplesService.Models
{
    /// <summary>
    /// Defines a representation of a user claim.
    /// </summary>
    public class UserClaim
    {
        [JsonProperty(PropertyName = "userName")]
        public string UserName { get; set; }
        [JsonProperty(PropertyName = "userPrincipalName")]
        public string UserPrincipalName { get; set; }
        [JsonProperty(PropertyName = "userPermissions")]
        public List<Permission> UserPermissions { get; set; }

        public UserClaim()
        {
            UserName = "Unspecified";
            UserPrincipalName = "unspecified@xyz.com";
            UserPermissions = new List<Permission>();
        }
    }
}
