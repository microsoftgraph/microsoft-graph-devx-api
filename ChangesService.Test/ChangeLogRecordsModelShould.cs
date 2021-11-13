// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Linq;
using ChangesService.Interfaces;
using ChangesService.Models;
using Xunit;

namespace ChangesService.Test
{
    public class ChangeLogRecordsModelShould
    {
        private readonly IChangesService _changesService;

        public ChangeLogRecordsModelShould()
        {
            _changesService = new Services.ChangesService();
        }

        [Fact]
        public void UpdateTotalItemsOnChangeLogsPropertySetter()
        {
            // Arrange & Act
            var changeLogRecords = new ChangeLogRecords
            {
                ChangeLogs = GetChangeLogRecords().ChangeLogs
            };

            // 1st Assert
            Assert.Equal(8, changeLogRecords.TotalItems);
            Assert.Equal(8, changeLogRecords.ChangeLogs.Count());

            /* Take only first two changelog items from list,
             * e.g. in a pagination scenario.
             */
            changeLogRecords.ChangeLogs = changeLogRecords.ChangeLogs
                                       .Take(2)
                                       .ToList();

            // 2nd Assert - TotalItems value should not change
            Assert.Equal(8, changeLogRecords.TotalItems);
            Assert.Equal(2, changeLogRecords.ChangeLogs.Count());
        }

        [Fact]
        public void UpdateCurrentItemsOnChangeLogsPropertySetter()
        {
            // Arrange & Act
            var changeLogRecords = new ChangeLogRecords
            {
                ChangeLogs = GetChangeLogRecords().ChangeLogs
            };

            /* 1st Assert - CurrentItems should always be equal
             * to the current count of changelog items in list.
            */
            Assert.Equal(8, changeLogRecords.CurrentItems);
            Assert.Equal(changeLogRecords.CurrentItems, changeLogRecords.ChangeLogs.Count());

            /* Take only first two changelog items from list,
             * e.g. in a pagination scenario.
             */
            changeLogRecords.ChangeLogs = changeLogRecords.ChangeLogs
                                       .Take(2)
                                       .ToList();

            /* 2nd Assert - CurrentItems value should always change
             * with current count of changelog items in list.
            */
            Assert.Equal(2, changeLogRecords.CurrentItems);
            Assert.Equal(changeLogRecords.CurrentItems, changeLogRecords.ChangeLogs.Count());
        }

        [Fact]
        public void UpdateTotalPagesOnPageLimitPropertySetter()
        {
            // Arrange & Act
            var changeLogRecords = new ChangeLogRecords
            {
                ChangeLogs = GetChangeLogRecords().ChangeLogs
            };

            // Act
            changeLogRecords.PageLimit = 1;

            // Assert
            Assert.Equal(8, changeLogRecords.TotalPages);

            // Act
            changeLogRecords.PageLimit = 2;

            // Assert
            Assert.Equal(4, changeLogRecords.TotalPages);

            // Act
            changeLogRecords.PageLimit = 3;

            // Assert
            Assert.Equal(3, changeLogRecords.TotalPages);
        }

        /// <summary>
        /// Gets a sample of <see cref="ChangeLogRecords"/>
        /// </summary>
        /// <param name="variableDate">Optional. CreatedDateTime value for Reports
        /// workload.</param>
        /// <returns>Sample <see cref="ChangeLogRecords"/></returns>
        public ChangeLogRecords GetChangeLogRecords(string variableDate = "2020-12-31T00:00:00.000Z")
        {
            // variableDate param will be used for specifying custom CreatedDateTime
            // value for Reports workload

            var changeLogRecords = @"{
              ""changelog"": [
                 {
                   ""ChangeList"": [
                      {
                        ""Id"": ""6a6c7aa0-4b67-4d07-9ebf-c2bc1bcef553"",
                        ""ApiChange"": ""Resource"",
                        ""ChangedApiName"": ""ediscoveryCase,reviewSet,reviewSetQuery"",
                        ""ChangeType"": ""Addition"",
                        ""Description"": ""Introduced the compliance eDiscovery API, including the [ediscoveryCase](https://docs.microsoft.com/en-us/graph/api/resources/ediscoverycase?view=graph-rest-beta), [reviewSet](https://docs.microsoft.com/en-us/graph/api/resources/reviewset?view=graph-rest-beta), and [reviewSetQuery](https://docs.microsoft.com/en-us/graph/api/resources/reviewsetquery?view=graph-rest-beta), and operations."",
                        ""Target"": ""ediscoveryCase,reviewSet,reviewSetQuery""
                      }
                    ],
                   ""Id"": ""6a6c7aa0-4b67-4d07-9ebf-c2bc1bcef553"",
                   ""Cloud"": ""prd"",
                   ""Version"": ""beta"",
                   ""CreatedDateTime"": ""2020-06-01T00:00:00.000Z"",
                   ""WorkloadArea"": ""Compliance"",
                   ""SubArea"": ""eDiscovery""
                 },
                 {
                   ""ChangeList"": [
                      {
                        ""Id"": ""2d94636a-2d78-44d6-8b08-ff2a9121214b"",
                        ""ApiChange"": ""Resource"",
                        ""ChangedApiName"": ""schema extensions,Microsoft Cloud for US Government."",
                        ""ChangeType"": ""Addition"",
                        ""Description"": ""The [schema extensions](https://docs.microsoft.com/en-us/graph/api/resources/schemaextension) feature is now generally available in [Microsoft Cloud for US Government](https://docs.microsoft.com/en-us/graph/deployments)."",
                        ""Target"": ""schema extensions,Microsoft Cloud for US Government""
                      }
                    ],
                   ""Id"": ""2d94636a-2d78-44d6-8b08-ff2a9121214b"",
                   ""Cloud"": ""prd"",
                   ""Version"": ""v1.0"",
                   ""CreatedDateTime"": ""2020-08-15T00:00:00.000Z"",
                   ""WorkloadArea"": ""Extensions"",
                   ""SubArea"": ""Schema extensions""
                 },
                 {
                   ""ChangeList"": [
                      {
                        ""Id"": ""dca6467b-d026-4316-8353-2c6c02598af3"",
                        ""ApiChange"": ""Resource"",
                        ""ChangedApiName"": ""relyingPartyDetailedSummary,listing"",
                        ""ChangeType"": ""Addition"",
                        ""Description"": ""Added a new resource type [relyingPartyDetailedSummary](https://docs.microsoft.com/en-us/graph/api/resources/relyingpartydetailedsummary?view=graph-rest-beta). This resource type supports [listing](https://docs.microsoft.com/en-us/graph/api/relyingpartydetailedsummary-list?view=graph-rest-beta) the relying parties configured in Active Directory Federation Services."",
                        ""Target"": ""relyingPartyDetailedSummary,listing""
                      }
                    ],
                   ""Id"": ""dca6467b-d026-4316-8353-2c6c02598af3"",
                   ""Cloud"": ""prd"",
                   ""Version"": ""beta"",
                   ""CreatedDateTime"": ""variableDate"",
                   ""WorkloadArea"": ""Reports"",
                   ""SubArea"": ""Identity and access reports""
                 },
                 {
                  ""ChangeList"": [
                     {
                        ""Id"": ""937000e6-2cab-4a40-a5ea-58e20dab9fbb"",
                        ""ApiChange"": ""Resource"",
                        ""ChangedApiName"": ""Scoping application permissions to specific Exchange Online mailboxes"",
                        ""ChangeType"": ""Ajout"",
                        ""Description"": ""Added the capability for administrators to limit app access to only specific mailboxes, even when an app has been granted application permissions to mail, mailbox settings, calendars, or contacts. For more details, see [Scoping application permissions to specific Exchange Online mailboxes](auth-limit-mailbox-access.md)."",
                        ""Target"": ""Scoping application permissions to specific Exchange Online mailboxes""
                     }
                    ],
                    ""Id"": ""937000e6-2cab-4a40-a5ea-58e20dab9fbb"",
                    ""Cloud"": ""prd"",
                    ""Version"": ""v1.0"",
                    ""CreatedDateTime"": ""2019-11-01T00:00:00.000Z"",
                    ""WorkloadArea"": ""Calendar, mail, personal contacts"",
                    ""SubArea"": """"
                 },
                 {
                    ""ChangeList"": [
                        {
                        ""Id"": ""656072a4-9a39-4731-8322-13932419895a"",
                        ""ApiChange"": ""Property"",
                        ""ChangedApiName"": ""transactionId"",
                        ""ChangeType"": ""Addition"",
                        ""Description"": ""Added the **transactionId** property to the [event](https://docs.microsoft.com/en-us/graph/api/resources/event?view=graph-rest-1.0) entity."",
                        ""Target"": ""events""
                        }
                        ],
                    ""Id"": ""656072a4-9a39-4731-8322-13932419895a"",
                    ""Cloud"": ""prd"",
                    ""Version"": ""v1.0"",
                    ""CreatedDateTime"": ""2020-10-01T00:00:00.000Z"",
                    ""WorkloadArea"": ""Calendar"",
                    ""SubArea"": """"
                   },
                   {
                      ""ChangeList"": [
                        {
                          ""Id"": ""8a32aa94-ac63-42d3-b323-5312bfc80a9d"",
                          ""ApiChange"": ""Property"",
                          ""ChangedApiName"": ""cancelledOccurrences,exceptionOccurrences,occurrenceId"",
                          ""ChangeType"": ""Addition"",
                          ""Description"": ""Added the **cancelledOccurrences**, **exceptionOccurrences**, and **occurrenceId** properties to the [event](https://docs.microsoft.com/en-us/graph/api/resources/event?view=graph-rest-beta) entity."",
                          ""Target"": ""event""
                        }
                      ],
                      ""Id"": ""8a32aa94-ac63-42d3-b323-5312bfc80a9d"",
                      ""Cloud"": ""prd"",
                      ""Version"": ""beta"",
                      ""CreatedDateTime"": ""2020-10-01T00:00:00.000Z"",
                      ""WorkloadArea"": ""Calendar"",
                      ""SubArea"": """"
                   },
                   {
                       ""ChangeList"": [
                         {
                            ""Id"": ""f5824007-1349-4ab9-b1c3-1b294d2c5bd4"",
                            ""ApiChange"": ""Property"",
                            ""ChangedApiName"": ""allowNewTimeProposals"",
                            ""ChangeType"": ""Addition"",
                            ""Description"": ""Added the **allowNewTimeProposals** property to the [event](https://docs.microsoft.com/en-us/graph/api/resources/event?view=graph-rest-1.0) and [eventMessageRequest](https://docs.microsoft.com/en-us/graph/api/resources/eventmessagerequest?view=graph-rest-1.0) entities."",
                            ""Target"": ""event,eventMessageRequest""
                         },
                         {
                            ""Id"": ""f5824007-1349-4ab9-b1c3-1b294d2c5bd4"",
                            ""ApiChange"": ""Parameter"",
                            ""ChangedApiName"": ""proposedNewTime,event"",
                            ""ChangeType"": ""Addition"",
                            ""Description"": ""Added the **proposedNewTime** optional parameter to the [tentativelyAccept](https://docs.microsoft.com/en-us/graph/api/event-tentativelyaccept?view=graph-rest-1.0) and [decline](https://docs.microsoft.com/en-us/graph/api/event-decline?view=graph-rest-1.0) methods of **event**."",
                            ""Target"": ""tentativelyAccept,decline""
                         },
                         {
                            ""Id"": ""f5824007-1349-4ab9-b1c3-1b294d2c5bd4"",
                            ""ApiChange"": ""Property"",
                            ""ChangedApiName"": ""proposedNewTime,responseType"",
                            ""ChangeType"": ""Addition"",
                            ""Description"": ""Added the [eventMessageResponse](https://docs.microsoft.com/en-us/graph/api/resources/eventmessageresponse?view=graph-rest-1.0) entity that is based on [eventMessage](https://docs.microsoft.com/en-us/graph/api/resources/eventmessage?view=graph-rest-1.0), and in addition, includes the **proposedNewTime** and **responseType** properties."",
                            ""Target"": ""eventMessageResponse,eventMessage""
                         },
                         {
                            ""Id"": ""f5824007-1349-4ab9-b1c3-1b294d2c5bd4"",
                            ""ApiChange"": ""Property"",
                            ""ChangedApiName"": ""proposedNewTime"",
                            ""ChangeType"": ""Addition"",
                            ""Description"": ""Added the **proposedNewTime** property to the [attendee](https://docs.microsoft.com/en-us/graph/api/resources/attendee?view=graph-rest-1.0) complex type."",
                            ""Target"": ""calendar""
                         }
                        ],
                        ""Id"": ""f5824007-1349-4ab9-b1c3-1b294d2c5bd4"",
                        ""Cloud"": ""prd"",
                        ""Version"": ""v1.0"",
                        ""CreatedDateTime"": ""2020-10-01T00:00:00.000Z"",
                        ""WorkloadArea"": ""Calendar"",
                        ""SubArea"": """"
                     },
                     {
                        ""ChangeList"": [
                          {
                             ""Id"": ""13064a4f-262d-40eb-ac18-5c6396974ed7"",
                             ""ApiChange"": ""Resource"",
                             ""ChangedApiName"": ""delta"",
                             ""ChangeType"": ""Addition"",
                             ""Description"": ""The [delta](https://docs.microsoft.com/en-us/graph/api/event-delta?view=graph-rest-beta) function supports an additional scenario to get incremental changes (new, updated, or removed) of events in a user calendar without necessarily being bounded by a date range."",
                             ""Target"": ""delta""
                           }
                        ],
                        ""Id"": ""13064a4f-262d-40eb-ac18-5c6396974ed7"",
                        ""Cloud"": ""prd"",
                        ""Version"": ""beta"",
                        ""CreatedDateTime"": ""2020-10-01T00:00:00.000Z"",
                        ""WorkloadArea"": ""Calendar"",
                        ""SubArea"": """"
                     }
                 ]
            }";

            changeLogRecords = changeLogRecords.Replace("variableDate", variableDate);

            return _changesService.DeserializeChangeLogRecords(changeLogRecords);
        }
    }
}
