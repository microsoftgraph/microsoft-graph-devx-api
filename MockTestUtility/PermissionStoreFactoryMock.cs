// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Interfaces;
using MemoryCache.Testing.Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using PermissionsService;
using PermissionsService.Interfaces;
using System;

namespace MockTestUtility
{
    /// <summary>
    /// Factory class that creates mocked IPermissionsStore objects
    /// </summary>
    public static class PermissionStoreFactoryMock
    {
        /// <summary>
        /// Creates a mocked IPermissionsStore object.
        /// </summary>
        /// <param name="configFilePath">Optional: The path of the config file.</param>
        /// <param name="configuration">Optional: Mock <see cref="IConfigurationRoot"/> object.</param>
        /// <param name="fileUtility">Optional: Mock <see cref="IFileUtility"/> object.</param>
        /// <param name="httpClientUtility">Optional: Mock <see cref="IHttpClientUtility"/> object.</param>
        /// <param name="permissionsCache">Optional: Mock <see cref="IMemoryCache"/> object.</param>
        /// <returns>A mocked IPermissionsStore object.</returns>
        public static IPermissionsStore GetPermissionStore(string configFilePath = null,
                                                           IConfigurationRoot configuration = null,
                                                           IFileUtility fileUtility = null,
                                                           IHttpClientUtility httpClientUtility = null,
                                                           IMemoryCache permissionsCache = null)
        {
            if (string.IsNullOrEmpty(configFilePath) && configuration == null)
            {
                // Either the configFilePath or the configuration parameters needs to be specified,
                // that is, they can't both be null.
                throw new ArgumentNullException($"{nameof(configFilePath)} and {nameof(configuration)}",
                                                $"Specify values for either {nameof(configFilePath)} or {nameof(configuration)}");
            }

            fileUtility ??= new FileUtilityMock();
            httpClientUtility ??= new FileUtilityMock();
            permissionsCache ??= Create.MockedMemoryCache();
            configuration ??= new ConfigurationBuilder()
                                    .AddJsonFile(configFilePath)
                                    .Build();

            return new PermissionsStore(configuration, httpClientUtility, fileUtility, permissionsCache);
        }
    }
}
