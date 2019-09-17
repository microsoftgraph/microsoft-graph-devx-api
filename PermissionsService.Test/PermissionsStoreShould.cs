using FileService.Interfaces;
using GraphExplorerPermissionsService;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using Xunit;

namespace PermissionsService.Test
{
    public class PermissionsStoreShould
    {
        [Fact]
        public void GetRequiredPermissionScopesGivenAnExistingRequestUrl()
        {
            /* Arrange */

            Mock<IFileUtility> moqFileUtility = new Mock<IFileUtility>();
            Mock<IConfiguration> moqConfiguration = new Mock<IConfiguration>();
            string permissionsFilePathSource = ".\\Permissions\\apiPermissionsAndScopes.json";

            moqFileUtility.Setup(x => x.ReadFromFile(permissionsFilePathSource)).ReturnsAsync(() => 
                            @"{
                            ""ApiPermissions"": {
                                ""/security/alerts/{alert_id}"": [
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""PATCH"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    }
                                ],
                                ""/security/alerts"": [
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    }
                                ]
                                }
                            }");  
            
            moqConfiguration.Setup(a => a["Permissions:PermissionsAndScopesFilePathName"]).Returns(permissionsFilePathSource);

            PermissionsStore permissionsStore = new PermissionsStore(moqFileUtility.Object, moqConfiguration.Object);

            // Act
            string[] result = permissionsStore.GetScopes("/security/alerts/{alert_id}");

            // Assert
            Assert.Collection(result,
                item =>
                {
                    item.Equals("SecurityEvents.Read.All");
                },
                item => 
                {
                    item.Equals("SecurityEvents.ReadWrite.All");
                });
        }

        [Fact]
        public void ReturnNullGivenANonExistentRequestUrl()
        {
            /* Arrange */

            Mock<IFileUtility> moqFileUtility = new Mock<IFileUtility>();
            Mock<IConfiguration> moqConfiguration = new Mock<IConfiguration>();
            string permissionsFilePathSource = ".\\Permissions\\apiPermissionsAndScopes.json";

            moqFileUtility.Setup(x => x.ReadFromFile(permissionsFilePathSource)).ReturnsAsync(() =>
                            @"{
                            ""ApiPermissions"": {
                                ""/security/alerts/{alert_id}"": [
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""PATCH"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    }
                                ],
                                ""/security/alerts"": [
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    }
                                ]
                                }
                            }");

            moqConfiguration.Setup(a => a["Permissions:PermissionsAndScopesFilePathName"]).Returns(permissionsFilePathSource);

            PermissionsStore permissionsStore = new PermissionsStore(moqFileUtility.Object, moqConfiguration.Object);

            // Act
            string[] result = permissionsStore.GetScopes("/foo/bar/{alert_id}"); // non-existent request url

            // Assert that returned result is null
            Assert.Null(result);
        }

        [Fact]
        public void ThrowInvalidOperationExceptionIfTablesNotPopulatedDueToIncorrectPermissionsFilePathName()
        {
            /* Arrange */

            Mock<IFileUtility> moqFileUtility = new Mock<IFileUtility>();
            Mock<IConfiguration> moqConfiguration = new Mock<IConfiguration>();
            string permissionsFilePathSource = ".\\Permissions\\apiPermissionsAndScopes.json";
            
            moqFileUtility.Setup(x => x.ReadFromFile(permissionsFilePathSource)).ReturnsAsync(() =>
                            @"{
                            ""ApiPermissions"": {
                                ""/security/alerts/{alert_id}"": [
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""PATCH"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    }
                                ],
                                ""/security/alerts"": [
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    }
                                ]
                                }
                            }");

            // Incorrect file path source name --> missing '.json' ext. in the return value
            moqConfiguration.Setup(a => a["Permissions:PermissionsAndScopesFilePathName"]).Returns(".\\Permissions\\apiPermissionsAndScopes");

            PermissionsStore permissionsStore = new PermissionsStore(moqFileUtility.Object, moqConfiguration.Object);

            // Act and Assert
            Assert.Throws<InvalidOperationException>(() => permissionsStore.GetScopes("/security/alerts/{alert_id}"));
        }

        [Fact]
        public void ThrowInvalidOperationExceptionIfTablesNotPopulatedDueToEmptyPermissionsFile()
        {
            /* Arrange */

            Mock<IFileUtility> moqFileUtility = new Mock<IFileUtility>();
            Mock<IConfiguration> moqConfiguration = new Mock<IConfiguration>();
            string permissionsFilePathSource = ".\\Permissions\\apiPermissionsAndScopes.json";

            // Empty permissions file
            moqFileUtility.Setup(x => x.ReadFromFile(permissionsFilePathSource)).ReturnsAsync(() => string.Empty);

            moqConfiguration.Setup(a => a["Permissions:PermissionsAndScopesFilePathName"]).Returns(permissionsFilePathSource);

            PermissionsStore permissionsStore = new PermissionsStore(moqFileUtility.Object, moqConfiguration.Object);

            // Act and Assert
            Assert.Throws<InvalidOperationException>(() => permissionsStore.GetScopes("/security/alerts/{alert_id}"));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfGetScopesRequestUrlParameterIsNullOrEmpty()
        {

            /* Arrange */

            Mock<IFileUtility> moqFileUtility = new Mock<IFileUtility>();
            Mock<IConfiguration> moqConfiguration = new Mock<IConfiguration>();
            string permissionsFilePathSource = ".\\Permissions\\apiPermissionsAndScopes.json";

            moqFileUtility.Setup(x => x.ReadFromFile(permissionsFilePathSource)).ReturnsAsync(() =>
                            @"{
                            ""ApiPermissions"": {
                                ""/security/alerts/{alert_id}"": [
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""PATCH"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    }
                                ],
                                ""/security/alerts"": [
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    },
                                    {
                                    ""HttpVerb"": ""GET"",
                                    ""DelegatedWork"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ],
                                    ""DelegatedPersonal"": [
                                        ""Not supported.""
                                    ],
                                    ""Application"": [
                                        ""SecurityEvents.Read.All"",
                                        ""SecurityEvents.ReadWrite.All""
                                    ]
                                    }
                                ]
                                }
                            }");

            moqConfiguration.Setup(a => a["Permissions:PermissionsAndScopesFilePathName"]).Returns(permissionsFilePathSource);

            PermissionsStore permissionsStore = new PermissionsStore(moqFileUtility.Object, moqConfiguration.Object);

            string nullRequestUrl = null;
            string emptyRequestUrl = string.Empty;

            /* Act and Assert */

            Assert.Throws<ArgumentNullException>(() => permissionsStore.GetScopes(nullRequestUrl)); // null request url arg.
            Assert.Throws<ArgumentNullException>(() => permissionsStore.GetScopes(emptyRequestUrl)); // empty request url arg.
        }
    }
}
