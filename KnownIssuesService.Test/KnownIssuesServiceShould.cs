// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using KnownIssuesService.Interfaces;
using KnownIssuesService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UtilityService;
using Xunit;

namespace KnownIssuesService.Test
{
    public class KnownIssuesServiceShould
    {
        private readonly IKnownIssuesService _knownIssuesService;
        private readonly WorkItemTrackingHttpClientMock _workItemTrackingHttpClientMock;
        private readonly WorkItemTrackingHttpClient _workItemTrackingHttpClient;
        private readonly Mock<Wiql> _wiqlTest = new();
        private readonly IConfigurationRoot _configuration;

        public KnownIssuesServiceShould()
        {
            _configuration = new ConfigurationBuilder()
                            .AddJsonFile("appsettingstest.json")
                            .Build();
            _workItemTrackingHttpClientMock = new WorkItemTrackingHttpClientMock();
            _workItemTrackingHttpClient = _workItemTrackingHttpClientMock.MockWorkItemTrackingHttpClient(_wiqlTest);
            _knownIssuesService = new Services.KnownIssuesService(_configuration, httpQueryClient: _workItemTrackingHttpClient);
        }

        [Fact]
        public async Task GetQueryByWiqlAsync()
        {
            //Arrange
            int expectedNoOfWorkItems = 5;

            //Act
            WorkItemQueryResult workItemQueryResult = await _knownIssuesService.GetQueryByWiqlAsync(_wiqlTest.Object);
            int actualNoOfWorkItems = workItemQueryResult.WorkItems.ToList().Count;

            //Assert
            Assert.Equal(expectedNoOfWorkItems, actualNoOfWorkItems);
        }

        [Fact]
        public async Task GetWorkItemsQueryAsync()
        {
            //Arrange
            int expectedNoOfWorkItems = 5;

            //Act
            WorkItemQueryResult workItemQueryResult = await _knownIssuesService.GetQueryByWiqlAsync(_wiqlTest.Object);
            int[] ids = workItemQueryResult.WorkItems.Select(item => item.Id).ToArray();
            List<WorkItem> items = await _knownIssuesService.GetWorkItemsQueryAsync(ids, workItemQueryResult);

            //Assert
            Assert.Equal(expectedNoOfWorkItems, items.Count);
        }

        [Fact]
        public async Task QueryBugsAsync()
        {
            //Arrange
            int expectedNoOfWorkItems = 3;
            var contract = new KnownIssue()
            {
                Title = "Issue B",
                State = "Active",
                WorkLoadArea = "Notifications",
                WorkAround = "Test",
                Link = "/foo/bar",
                CreatedDateTime = DateTime.Parse("01/06/2022 00:00:00"),
                LastUpdatedDateTime = DateTime.Parse("01/07/2022 00:00:00"),
                SubArea = "Test notifications",
                IsPublicIssue = true
            };

            //Act
            List<KnownIssue> items = await _knownIssuesService.QueryBugsAsync(EnvironmentType.Staging, this._wiqlTest.Object);

            //Assert
            foreach (var item in items)
            {
                Assert.True(item.State != "New" || item.State != "Resolved");
                Assert.IsType<DateTime>(item.CreatedDateTime);
                Assert.IsType<DateTime>(item.LastUpdatedDateTime);
            }

            Assert.Equal(expectedNoOfWorkItems, items.Count);
            Assert.Equal(contract.Title, items[1].Title);
            Assert.Equal(contract.State, items[1].State);            
            Assert.Equal(contract.WorkLoadArea, items[1].WorkLoadArea);
            Assert.Equal(contract.WorkAround, items[1].WorkAround);
            Assert.Equal(contract.CreatedDateTime, items[1].CreatedDateTime);
            Assert.Equal(contract.LastUpdatedDateTime, items[1].LastUpdatedDateTime);
            Assert.True(items[1].IsDateUpdated);
        }
    }
}
