// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Interfaces;
using GraphExplorerPermissionsService;
using GraphExplorerPermissionsService.Interfaces;
using MemoryCache.Testing.Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;

namespace MockTestUtility
{
    public static class PermissionStoreMock
    {
        public static IPermissionsStore GetPermissionStore(string relativeFilePath)
        {
            if (string.IsNullOrEmpty(relativeFilePath))
            {
                throw new ArgumentNullException(nameof(relativeFilePath), $"{ relativeFilePath }: { nameof(relativeFilePath) }");
            }

            IFileUtility fileUtility = new FileUtilityMock();
            IHttpClientUtility httpClientUtility = new FileUtilityMock();
            using IMemoryCache permissionsCache = Create.MockedMemoryCache();
            IConfigurationRoot configuration = new ConfigurationBuilder()
               .AddJsonFile(relativeFilePath)
               .Build();

            return new PermissionsStore(configuration, httpClientUtility, fileUtility, permissionsCache);
        }
    }
}
