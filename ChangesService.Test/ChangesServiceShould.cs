// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Models;
using System;
using Xunit;

namespace ChangesService.Test
{
    public class ChangesServiceShould
    {
        private readonly MicrosoftGraphProxyConfigs _graphProxyConfigs = new MicrosoftGraphProxyConfigs();
        private Services.ChangesService _changesService;
        private ChangeLogRecordsModelShould _changeLogRecordsModelShould;

        public ChangesServiceShould()
        {
            _changesService = new Services.ChangesService();
            _changeLogRecordsModelShould = new ChangeLogRecordsModelShould();
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
                _changesService.FilterChangeLogRecords(null, new ChangeLogSearchOptions(), new MicrosoftGraphProxyConfigs()));
        }

        [Fact]
        public void ThrowArgumentNullExceptionOnFilterChangeLogRecordsIfSearchOptionsArgumentIsNull()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
               _changesService.FilterChangeLogRecords(new ChangeLogRecords(), null, new MicrosoftGraphProxyConfigs()));
        }

        [Fact]
        public void ThrowArgumentNullExceptionOnFilterChangeLogRecordsIfGraphProxyConfigsArgumentIsNull()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                _changesService.FilterChangeLogRecords(new ChangeLogRecords(), new ChangeLogSearchOptions(), null));
        }

        [Fact]
        public void FilterChangeLogRecordsByWorkload()
        {
            // Arrange
            var changeLogRecords = new ChangeLogRecords
            {
                ChangeLogs = _changeLogRecordsModelShould.GetChangeLogRecords().ChangeLogs
            };

            var searchOptions = new ChangeLogSearchOptions(workload: "Compliance");

            // Act
            var changeLog = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs);

            // Assert
            Assert.NotNull(changeLog);
            Assert.Collection(changeLog.ChangeLogs,
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
        public void FilterChangeLogRecordsByDates()
        {
            // Arrange
            var changeLogRecords = new ChangeLogRecords
            {
                ChangeLogs = _changeLogRecordsModelShould.GetChangeLogRecords().ChangeLogs
            };
            var startDate = DateTime.Parse("2020-01-01");
            var endDate = DateTime.Parse("2020-10-01");
            var searchOptions = new ChangeLogSearchOptions(startDate: startDate, endDate: endDate);

            // Act
            var changeLog = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs);

            // Assert
            Assert.NotNull(changeLog);
            Assert.Collection(changeLog.ChangeLogs,
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
                ChangeLogs = _changeLogRecordsModelShould.GetChangeLogRecords(varDate.ToString("yyyy-MM-ddTHH:mm:ss")).ChangeLogs
            };

            var searchOptions = new ChangeLogSearchOptions(daysRange: 60);

            // Act
            var changeLog = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs);

            // Assert
            Assert.NotNull(changeLog);
            Assert.Collection(changeLog.ChangeLogs,
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
                ChangeLogs = _changeLogRecordsModelShould.GetChangeLogRecords().ChangeLogs
            };
            var startDate = DateTime.Parse("2020-06-01");

            var searchOptions = new ChangeLogSearchOptions(startDate: startDate, daysRange: 120);

            // Act
            var changeLog = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs);

            // Assert
            Assert.NotNull(changeLog);
            Assert.Collection(changeLog.ChangeLogs,
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
                ChangeLogs = _changeLogRecordsModelShould.GetChangeLogRecords().ChangeLogs
            };
            var endDate = DateTime.Parse("2021-01-01");

            var searchOptions = new ChangeLogSearchOptions(endDate: endDate, daysRange: 30);

            // Act
            var changeLog = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs);

            // Assert
            Assert.NotNull(changeLog);
            Assert.Collection(changeLog.ChangeLogs,
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
                ChangeLogs = _changeLogRecordsModelShould.GetChangeLogRecords().ChangeLogs
            };

            var searchOptions = new ChangeLogSearchOptions
            {
                Page = 1,
                PageLimit = 2
            };

            // Act
            var changeLog = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs);

            // Assert -- fetch first two items from the changelog sample
            Assert.NotNull(changeLog);
            Assert.Collection(changeLog.ChangeLogs,
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
                ChangeLogs = _changeLogRecordsModelShould.GetChangeLogRecords().ChangeLogs
            };

            var searchOptions = new ChangeLogSearchOptions
            {
                Page = 2,
                PageLimit = 1
            };

            // Act
            var changeLog = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs);

            // Assert -- fetch middle item from the changelog sample
            Assert.NotNull(changeLog);
            Assert.Collection(changeLog.ChangeLogs,
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
                ChangeLogs = _changeLogRecordsModelShould.GetChangeLogRecords().ChangeLogs
            };

            var searchOptions = new ChangeLogSearchOptions
            {
                Page = 2,
                PageLimit = 2
            };

            // Act
            var changeLog = _changesService.FilterChangeLogRecords(changeLogRecords, searchOptions, _graphProxyConfigs);

            // Assert -- fetch last item from the changelog sample
            Assert.NotNull(changeLog);
            Assert.Collection(changeLog.ChangeLogs,
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
