﻿// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Interfaces;
using ChangesService.Models;
using FileService.Interfaces;
using FileService.Services;
using MockTestUtility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace ChangesService.Test
{
    public class ChangesServiceShould
    {
        private readonly MicrosoftGraphProxyConfigs _graphProxyConfigs;
        private readonly IChangesService _changesService;
        private readonly ChangeLogRecordsModelShould _changeLogRecordsModel;
        private readonly ChangesStoreShould _changesStore;
        private readonly Dictionary<string, string> _workloadServiceMappings = new();
        private readonly HttpClient _httpClientMock;
        private readonly IHttpClientUtility _httpClientUtility;


        public ChangesServiceShould()
        {
            _changesService = new Services.ChangesService();
            _changeLogRecordsModel = new ChangeLogRecordsModelShould();
            _changesStore = new ChangesStoreShould();
            _workloadServiceMappings = _changesStore.GetWorkloadServiceMappingsFile().GetAwaiter().GetResult();
            _httpClientMock = new HttpClient(new MockHttpMessageHandler());
            _httpClientUtility = new HttpClientUtility(_httpClientMock);
            var graphProxyConfigsInitializer = new MicrosoftGraphProxyConfigTestInitializer("v1.0");
            _graphProxyConfigs = graphProxyConfigsInitializer.GraphProxyConfigs;
        }

        [Fact]
        public void ThrowArgumentNullExceptionOnDeserializeChangeLogRecordsIfJsonStringArgumentIsNull()
        {
            // Arrange
            string nullArgument = "";

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                _changesService.DeserializeChangeLogRecords(nullArgument));
        }

        [Fact]
        public void ThrowArgumentNullExceptionOnFilterChangeLogRecordsIfChangeLogRecordsArgumentIsNull()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                _changesService.FilterChangeLogRecords(null, new ChangeLogSearchOptions(), new MicrosoftGraphProxyConfigs(), new Dictionary<string, string>()));
        }

        [Fact]
        public void ThrowArgumentNullExceptionOnFilterChangeLogRecordsIfSearchOptionsArgumentIsNull()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                _changesService.FilterChangeLogRecords(new ChangeLogRecords(), null, new MicrosoftGraphProxyConfigs(), new Dictionary<string, string>()));
        }

        [Fact]
        public void ThrowArgumentNullExceptionOnFilterChangeLogRecordsIfGraphProxyConfigsArgumentIsNull()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                _changesService.FilterChangeLogRecords(new ChangeLogRecords(), new ChangeLogSearchOptions(), null, new Dictionary<string, string>()));
        }

        [Fact]
        public void ThrowArgumentNullExceptionOnFilterChangeLogRecordsIfworkloadServiceMappingsIsNull()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                _changesService.FilterChangeLogRecords(new ChangeLogRecords(), new ChangeLogSearchOptions(), new MicrosoftGraphProxyConfigs(), null));
        }

        [Fact]
        public void FilterChangeLogRecordsByWorkload()
        {
            // Arrange
            var changeLogRecords = new ChangeLogRecords
            {
                ChangeLogs = _changeLogRecordsModel.GetChangeLogRecords().ChangeLogs
            };

            var searchOptions = new ChangeLogSearchOptions(service: "Compliance", graphVersion: "beta");

            // Act
            var filteredChangeLogRecords = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs, _workloadServiceMappings);

            // Assert
            Assert.NotNull(filteredChangeLogRecords);
            Assert.Collection(filteredChangeLogRecords.ChangeLogs,
                item =>
                {
                    Assert.Equal("6a6c7aa0-4b67-4d07-9ebf-c2bc1bcef553", item.Id);
                    Assert.Equal("Compliance", item.WorkloadArea);
                    Assert.Equal("eDiscovery", item.SubArea);
                    Assert.Collection(item.ChangeList,
                        item =>
                        {
                            Assert.Equal("6a6c7aa0-4b67-4d07-9ebf-c2bc1bcef553", item.Id);
                            Assert.Equal("Resource", item.ApiChange);
                            Assert.Equal("ediscoveryCase,reviewSet,reviewSetQuery", item.ChangedApiName);
                            Assert.Equal("Addition", item.ChangeType);
                            Assert.Equal("ediscoveryCase,reviewSet,reviewSetQuery", item.Target);
                        });
                });
        }

        [Fact]
        public async Task FilterChangeLogRecordsByRequestUrlReturnsRecordsForExistingChanges()
        {
            // Arrange
            var changeLogRecords = await _changesStore.FetchChangeLogRecordsAsync(new CultureInfo("en-US"));

            var searchOptions = new ChangeLogSearchOptions(requestUrl: "/me/calendar", graphVersion: "v1.0");

            // Act
            var filteredChangeLogRecords = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs, _workloadServiceMappings, _httpClientUtility);

            // Assert
            Assert.NotNull(filteredChangeLogRecords);
            Assert.Equal(2, filteredChangeLogRecords.ChangeLogs.Count());
            Assert.Collection(filteredChangeLogRecords.ChangeLogs,
                item =>
                {
                    Assert.Equal(4, item.ChangeList.Count());
                },
                item =>
                {
                    Assert.Single(item.ChangeList);
                });
        }

        [Fact]
        public async Task FilterChangeLogRecordsByRequestUrlReturnsNoChangeLogsForNonExistingChanges()
        {
            // Arrange
            var changeLogRecords = await _changesStore.FetchChangeLogRecordsAsync(new CultureInfo("en-US"));

            var searchOptions = new ChangeLogSearchOptions(requestUrl: "/me/messages", graphVersion: "v1.0");

            // Act
            var filteredChangeLogRecords = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs, _workloadServiceMappings, _httpClientUtility);

            // Assert
            Assert.NotNull(filteredChangeLogRecords);
            Assert.False(filteredChangeLogRecords.ChangeLogs.Any());
            Assert.Equal(0, filteredChangeLogRecords.TotalItems);
        }

        [Fact]
        public async Task ThrowExceptionWhenServiceNameIsNotFoundForReturnedWorkloadId()
        {
            // Arrange
            var changeLogRecords = await _changesStore.FetchChangeLogRecordsAsync(new CultureInfo("en-US"));

            var searchOptions = new ChangeLogSearchOptions(requestUrl: "/appCatalogs/teamsApps", graphVersion: "v1.0");

            // Act and Assert
            Assert.Throws<Exception>(() =>
            _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs, _workloadServiceMappings, _httpClientUtility));
        }

        [Fact]
        public async Task ThrowInvalidOperationExceptionWhenFetchWorkloadInfoFromGraphReturnsError()
        {
            // Arrange
            var changeLogRecords = await _changesStore.FetchChangeLogRecordsAsync(new CultureInfo("en-US"));

            var searchOptions = new ChangeLogSearchOptions(requestUrl: "/admin/windows/updates", graphVersion: "v1.0");

            // Act and Assert
            Assert.Throws<InvalidOperationException>(() =>
            _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs, _workloadServiceMappings, _httpClientUtility));
        }

        [Fact]
        public void FilterChangeLogRecordsByDates()
        {
            // Arrange
            var changeLogRecords = new ChangeLogRecords
            {
                ChangeLogs = _changeLogRecordsModel.GetChangeLogRecords().ChangeLogs
            };
            var startDate = DateTime.Parse("2020-01-01");
            var endDate = DateTime.Parse("2020-10-01");
            var searchOptions = new ChangeLogSearchOptions(startDate: startDate, endDate: endDate);

            // Act
            var filteredChangeLogRecords = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs, _workloadServiceMappings);

            // Assert
            Assert.NotNull(filteredChangeLogRecords);
            Assert.Collection(filteredChangeLogRecords.ChangeLogs,
                item =>
                {
                    Assert.Equal("6a6c7aa0-4b67-4d07-9ebf-c2bc1bcef553", item.Id);
                    Assert.Equal("Compliance", item.WorkloadArea);
                    Assert.Equal("eDiscovery", item.SubArea);
                    Assert.Collection(item.ChangeList,
                        item =>
                        {
                            Assert.Equal("6a6c7aa0-4b67-4d07-9ebf-c2bc1bcef553", item.Id);
                            Assert.Equal("Resource", item.ApiChange);
                            Assert.Equal("ediscoveryCase,reviewSet,reviewSetQuery", item.ChangedApiName);
                            Assert.Equal("Addition", item.ChangeType);
                            Assert.Equal("ediscoveryCase,reviewSet,reviewSetQuery", item.Target);
                        });
                },
                item =>
                {
                    Assert.Equal("2d94636a-2d78-44d6-8b08-ff2a9121214b", item.Id);
                    Assert.Equal("Extensions", item.WorkloadArea);
                    Assert.Equal("Schema extensions", item.SubArea);
                    Assert.Collection(item.ChangeList,
                        item =>
                        {
                            Assert.Equal("2d94636a-2d78-44d6-8b08-ff2a9121214b", item.Id);
                            Assert.Equal("Resource", item.ApiChange);
                            Assert.Equal("schema extensions,Microsoft Cloud for US Government.", item.ChangedApiName);
                            Assert.Equal("Addition", item.ChangeType);
                            Assert.Equal("schema extensions,Microsoft Cloud for US Government", item.Target);
                        });
                });
        }

        [Fact]
        public void FilterChangeLogRecordsByDaysRange()
        {
            // Arrange

            DateTime varDate = DateTime.Today.AddDays(-30);
            var changeLogRecords = new ChangeLogRecords
            {
                ChangeLogs = _changeLogRecordsModel.GetChangeLogRecords(varDate.ToString("yyyy-MM-ddTHH:mm:ss")).ChangeLogs
            };

            var searchOptions = new ChangeLogSearchOptions(daysRange: 60);

            // Act
            var filteredChangeLogRecords = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs, _workloadServiceMappings);

            // Assert
            Assert.NotNull(filteredChangeLogRecords);
            Assert.Collection(filteredChangeLogRecords.ChangeLogs,
                item =>
                {
                    Assert.Equal("dca6467b-d026-4316-8353-2c6c02598af3", item.Id);
                    Assert.Equal("Reports", item.WorkloadArea);
                    Assert.Equal("Identity and access reports", item.SubArea);
                    Assert.Collection(item.ChangeList,
                        item =>
                        {
                            Assert.Equal("dca6467b-d026-4316-8353-2c6c02598af3", item.Id);
                            Assert.Equal("Resource", item.ApiChange);
                            Assert.Equal("relyingPartyDetailedSummary,listing", item.ChangedApiName);
                            Assert.Equal("Addition", item.ChangeType);
                            Assert.Equal("relyingPartyDetailedSummary,listing", item.Target);
                        });
                });
        }

        // Filter by StartDate & DaysRange
        [Fact]
        public void FilterChangeLogRecordsByStartDateAndDaysRange()
        {
            // Arrange
            var changeLogRecords = new ChangeLogRecords
            {
                ChangeLogs = _changeLogRecordsModel.GetChangeLogRecords().ChangeLogs
            };
            var startDate = DateTime.Parse("2020-06-01");

            var searchOptions = new ChangeLogSearchOptions(startDate: startDate, daysRange: 120);

            // Act
            var filteredChangeLogRecords = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs, _workloadServiceMappings);

            // Assert
            Assert.NotNull(filteredChangeLogRecords);
            Assert.Collection(filteredChangeLogRecords.ChangeLogs,
                item =>
                {
                    Assert.Equal("6a6c7aa0-4b67-4d07-9ebf-c2bc1bcef553", item.Id);
                    Assert.Equal("Compliance", item.WorkloadArea);
                    Assert.Equal("eDiscovery", item.SubArea);
                    Assert.Collection(item.ChangeList,
                        item =>
                        {
                            Assert.Equal("6a6c7aa0-4b67-4d07-9ebf-c2bc1bcef553", item.Id);
                            Assert.Equal("Resource", item.ApiChange);
                            Assert.Equal("ediscoveryCase,reviewSet,reviewSetQuery", item.ChangedApiName);
                            Assert.Equal("Addition", item.ChangeType);
                            Assert.Equal("ediscoveryCase,reviewSet,reviewSetQuery", item.Target);
                        });
                },
                item =>
                {
                    Assert.Equal("2d94636a-2d78-44d6-8b08-ff2a9121214b", item.Id);
                    Assert.Equal("Extensions", item.WorkloadArea);
                    Assert.Equal("Schema extensions", item.SubArea);
                    Assert.Collection(item.ChangeList,
                        item =>
                        {
                            Assert.Equal("2d94636a-2d78-44d6-8b08-ff2a9121214b", item.Id);
                            Assert.Equal("Resource", item.ApiChange);
                            Assert.Equal("schema extensions,Microsoft Cloud for US Government.", item.ChangedApiName);
                            Assert.Equal("Addition", item.ChangeType);
                            Assert.Equal("schema extensions,Microsoft Cloud for US Government", item.Target);
                        });
                });
        }

        [Fact]
        public void FilterChangeLogRecordsByEndDateAndDaysRange()
        {
            // Arrange
            var changeLogRecords = new ChangeLogRecords
            {
                ChangeLogs = _changeLogRecordsModel.GetChangeLogRecords().ChangeLogs
            };
            var endDate = DateTime.Parse("2021-01-01");

            var searchOptions = new ChangeLogSearchOptions(endDate: endDate, daysRange: 30);

            // Act
            var filteredChangeLogRecords = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs, _workloadServiceMappings);

            // Assert
            Assert.NotNull(filteredChangeLogRecords);
            Assert.Collection(filteredChangeLogRecords.ChangeLogs,
                item =>
                {
                    Assert.Equal("dca6467b-d026-4316-8353-2c6c02598af3", item.Id);
                    Assert.Equal("Reports", item.WorkloadArea);
                    Assert.Equal("Identity and access reports", item.SubArea);
                    Assert.Collection(item.ChangeList,
                        item =>
                        {
                            Assert.Equal("dca6467b-d026-4316-8353-2c6c02598af3", item.Id);
                            Assert.Equal("Resource", item.ApiChange);
                            Assert.Equal("relyingPartyDetailedSummary,listing", item.ChangedApiName);
                            Assert.Equal("Addition", item.ChangeType);
                            Assert.Equal("relyingPartyDetailedSummary,listing", item.Target);
                        });
                });
        }

        [Fact]
        public void PaginateChangeLogRecordsFirstPage()
        {
            // Arrange
            var changeLogRecords = new ChangeLogRecords
            {
                ChangeLogs = _changeLogRecordsModel.GetChangeLogRecords().ChangeLogs
            };

            var searchOptions = new ChangeLogSearchOptions
            {
                Page = 1,
                PageLimit = 2
            };

            // Act
            var filteredChangeLogRecords = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs, _workloadServiceMappings);

            // Assert -- fetch first two items from the changelog sample
            Assert.NotNull(filteredChangeLogRecords);
            Assert.Collection(filteredChangeLogRecords.ChangeLogs,
                item =>
                {
                    Assert.Equal("6a6c7aa0-4b67-4d07-9ebf-c2bc1bcef553", item.Id);
                    Assert.Equal("Compliance", item.WorkloadArea);
                    Assert.Equal("eDiscovery", item.SubArea);
                    Assert.Collection(item.ChangeList,
                        item =>
                        {
                            Assert.Equal("6a6c7aa0-4b67-4d07-9ebf-c2bc1bcef553", item.Id);
                            Assert.Equal("Resource", item.ApiChange);
                            Assert.Equal("ediscoveryCase,reviewSet,reviewSetQuery", item.ChangedApiName);
                            Assert.Equal("Addition", item.ChangeType);
                            Assert.Equal("ediscoveryCase,reviewSet,reviewSetQuery", item.Target);
                        });
                },
                item =>
                {
                    Assert.Equal("2d94636a-2d78-44d6-8b08-ff2a9121214b", item.Id);
                    Assert.Equal("Extensions", item.WorkloadArea);
                    Assert.Equal("Schema extensions", item.SubArea);
                    Assert.Collection(item.ChangeList,
                        item =>
                        {
                            Assert.Equal("2d94636a-2d78-44d6-8b08-ff2a9121214b", item.Id);
                            Assert.Equal("Resource", item.ApiChange);
                            Assert.Equal("schema extensions,Microsoft Cloud for US Government.", item.ChangedApiName);
                            Assert.Equal("Addition", item.ChangeType);
                            Assert.Equal("schema extensions,Microsoft Cloud for US Government", item.Target);
                        });
                });
        }

        [Fact]
        public void PaginateChangeLogRecordsMiddlePage()
        {
            // Arrange
            var changeLogRecords = new ChangeLogRecords
            {
                ChangeLogs = _changeLogRecordsModel.GetChangeLogRecords().ChangeLogs
            };

            var searchOptions = new ChangeLogSearchOptions
            {
                Page = 2,
                PageLimit = 1
            };

            // Act
            var filteredChangeLogRecords = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs, _workloadServiceMappings);

            // Assert -- fetch middle item from the changelog sample
            Assert.NotNull(filteredChangeLogRecords);
            Assert.Collection(filteredChangeLogRecords.ChangeLogs,
                item =>
                {
                    Assert.Equal("2d94636a-2d78-44d6-8b08-ff2a9121214b", item.Id);
                    Assert.Equal("Extensions", item.WorkloadArea);
                    Assert.Equal("Schema extensions", item.SubArea);
                    Assert.Collection(item.ChangeList,
                        item =>
                        {
                            Assert.Equal("2d94636a-2d78-44d6-8b08-ff2a9121214b", item.Id);
                            Assert.Equal("Resource", item.ApiChange);
                            Assert.Equal("schema extensions,Microsoft Cloud for US Government.", item.ChangedApiName);
                            Assert.Equal("Addition", item.ChangeType);
                            Assert.Equal("schema extensions,Microsoft Cloud for US Government", item.Target);
                        });
                });
        }

        [Fact]
        public void PaginateChangeLogRecordsLastPage()
        {
            // Arrange
            var changeLogRecords = new ChangeLogRecords
            {
                ChangeLogs = _changeLogRecordsModel.GetChangeLogRecords().ChangeLogs
            };

            var searchOptions = new ChangeLogSearchOptions
            {
                Page = 2,
                PageLimit = 2
            };

            // Act
            var filteredChangeLogRecords = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs, _workloadServiceMappings);

            // Assert -- fetch last item from the changelog sample
            Assert.NotNull(filteredChangeLogRecords);
            Assert.Collection(filteredChangeLogRecords.ChangeLogs,
                item =>
                {
                    Assert.Equal("dca6467b-d026-4316-8353-2c6c02598af3", item.Id);
                    Assert.Equal("Reports", item.WorkloadArea);
                    Assert.Equal("Identity and access reports", item.SubArea);
                    Assert.Collection(item.ChangeList,
                        item =>
                        {
                            Assert.Equal("dca6467b-d026-4316-8353-2c6c02598af3", item.Id);
                            Assert.Equal("Resource", item.ApiChange);
                            Assert.Equal("relyingPartyDetailedSummary,listing", item.ChangedApiName);
                            Assert.Equal("Addition", item.ChangeType);
                            Assert.Equal("relyingPartyDetailedSummary,listing", item.Target);
                        });
                });
        }
    }
}
