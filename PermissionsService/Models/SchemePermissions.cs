// -------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PermissionsService.Models
{
    internal class SchemePermissions
    {
        [JsonPropertyName("leastPrivilegePermissions")]
        public HashSet<string> LeastPrivilegePermissions
        {
            get; set;
        } = new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("allPermissions")]
        public HashSet<string> AllPermissions
        {
            get; set;
        } = new(StringComparer.OrdinalIgnoreCase);

    }
}
