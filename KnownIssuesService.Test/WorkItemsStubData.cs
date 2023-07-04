// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;

namespace KnownIssuesService.Test
{
    /// <summary>
    /// Contains work item stub data for testing purposes
    /// </summary>
    public static class WorkItemsStubData
    {
        public static List<WorkItemReference> GetWorkItemReferences()
        {
            return new List<WorkItemReference>()
            {
                new WorkItemReference(){  Id = 9075 , Url = "https://microsoftgraph.visualstudio.com/_apis/wit/workItems/9075"},
                new WorkItemReference(){  Id = 9076 , Url = "https://microsoftgraph.visualstudio.com/_apis/wit/workItems/9076"},
                new WorkItemReference(){  Id = 9077 , Url = "https://microsoftgraph.visualstudio.com/_apis/wit/workItems/9077"},
                new WorkItemReference(){  Id = 9078 , Url = "https://microsoftgraph.visualstudio.com/_apis/wit/workItems/9078"},
                new WorkItemReference(){  Id = 9079 , Url = "https://microsoftgraph.visualstudio.com/_apis/wit/workItems/9079"}
            };
        }

        public static List<WorkItem> GetWorkItems()
        {
            return new List<WorkItem>()
            {
                new WorkItem(){  Id = 9075 , Url = "https://microsoftgraph.visualstudio.com/_apis/wit/workItems/9075",
                    Fields = new Dictionary<string, object>() {
                       {"System.State","Active"},
                       {"System.Title","Issue A"},
                       {"Custom.MicrosoftGraphArea","Calendar"},
                       {"Custom.Workaround","Test"},
                       {"Custom.PublicIssue", true}
                    }
                },
                new WorkItem(){  Id = 9076 , Url = "https://microsoftgraph.visualstudio.com/_apis/wit/workItems/9076",
                  Fields = new Dictionary<string, object>() {
                       {"System.State","Active"},
                       {"System.Title","Issue B"},
                       {"Custom.MicrosoftGraphArea","Notifications"},
                       {"Custom.Workaround","Test"},
                       {"Custom.APIPathLink", "/foo/bar"},
                       {"Custom.Dateissuewasraised", DateTime.Parse("01/06/2022 00:00:00")},
                       {"Custom.Lastupdate", DateTime.Parse("01/07/2022 00:00:00")},
                       {"Custom.PublicIssue", true}
                    }

                },
                new WorkItem(){  Id = 9077 , Url = "https://microsoftgraph.visualstudio.com/_apis/wit/workItems/9077",
                  Fields = new Dictionary<string, object>() {
                       {"System.State","Resolved"},
                       {"System.Title","Issue K"},
                       {"Custom.MicrosoftGraphArea","Mail"},
                       {"Custom.Workaround","Limit number of requests"},
                       {"Custom.PublicIssue", true}
                    }
                },
                new WorkItem(){  Id = 9078 , Url = "https://microsoftgraph.visualstudio.com/_apis/wit/workItems/9078",
                 Fields = new Dictionary<string, object>() {
                       {"System.State","New"},
                       {"System.Title","Issue F"},
                       {"Custom.MicrosoftGraphArea","Mail"},
                       {"Custom.Workaround","Limit number of requests"},
                       {"Custom.PublicIssue", false}
                    }
                },
                new WorkItem()
                {  Id = 9079 , Url = "https://microsoftgraph.visualstudio.com/_apis/wit/workItems/9079",
                    Fields = new Dictionary<string, object>() {
                       {"System.State","New"},
                       {"System.Title","Issue A"},
                       {"Custom.MicrosoftGraphArea","Calendar"},
                       {"Custom.Workaround","Test"},
                       {"Custom.PublicIssue", true}
                    }
                }
            };
        }
    }
}
