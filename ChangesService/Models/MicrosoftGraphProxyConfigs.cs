// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

namespace ChangesService.Models
{
    /// <summary>
    /// Configs for connecting to the Microsoft Graph Proxy to
    /// fetch workload names from Graph urls.
    /// </summary>
    public class MicrosoftGraphProxyConfigs
    {
        public string GraphVersion { get; set; } = "v1.0";
        public string GraphProxyBaseUrl { get; set; } = "https://proxy.apisandbox.msdn.microsoft.com/";
        public string GraphProxyRelativeUrl { get; set; } = "svc?url=https://graph.microsoft.com/{0}{1}?$whatif";
        public string GraphProxyAuthorization { get; set; }
    }
}
