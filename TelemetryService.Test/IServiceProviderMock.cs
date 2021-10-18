// -------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Moq;
using PermissionsService.Interfaces;
using System;
using System.IO;

namespace MockTestUtility
{
    public class IServiceProviderMock
    {
        private static string ConfigFilePath => Path.Join(Environment.CurrentDirectory, "TestFiles", "Permissions", "appsettings.json");
        private readonly IPermissionsStore _permissionsStore;
        private readonly Mock<IServiceProvider> _serviceProvider;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;

        public IServiceProviderMock()
        {
            _permissionsStore = PermissionStoreFactoryMock.GetPermissionStore(ConfigFilePath);
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceScope = new Mock<IServiceScope>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
        }

        public IServiceProvider MockServiceProvider()
        {
            _serviceProvider.Setup(x => x.GetService(typeof(IPermissionsStore))).Returns(_permissionsStore);

            _serviceScope.Setup(x => x.ServiceProvider).Returns(_serviceProvider.Object);

            _serviceScopeFactory.Setup(x => x.CreateScope()).Returns(_serviceScope.Object);

            _serviceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(_serviceScopeFactory.Object);

            return _serviceProvider.Object;
        }
    }
}
