// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using ChangesService.Common;
using ChangesService.Models;
using Microsoft.Extensions.Configuration;

namespace ChangesService.Test
{
    /// <summary>
    /// Class used to initialize the values of the MicrosoftGraphProxyConfigs class with test values.
    /// </summary>
    public class MicrosoftGraphProxyConfigTestInitializer
    {
        private readonly IConfigurationRoot _configuration;
        public MicrosoftGraphProxyConfigs GraphProxyConfigs
        {
            get; private set;
        }

        public MicrosoftGraphProxyConfigTestInitializer(string graphVersion)
        {
            UtilityService.UtilityFunctions.CheckArgumentNullOrEmpty(graphVersion, nameof(graphVersion));

            _configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Join(Environment.CurrentDirectory, "TestFiles", "appsettingstest.json"))
                .Build();

            GraphProxyConfigs = new MicrosoftGraphProxyConfigs()
            {
                GraphProxyBaseUrl = _configuration[ChangesServiceConstants.GraphProxyBaseUrlConfigPath],
                GraphProxyRelativeUrl = _configuration[ChangesServiceConstants.GraphProxyRelativeUrlConfigPath],
                GraphProxyAuthorization = _configuration[ChangesServiceConstants.GraphProxyAuthorization],
                GraphVersion = graphVersion
            };
        }
    }
}
