// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace MockTestUtility
{
    /// <summary>
    /// Contains constants used for Http tests
    /// </summary>
    public static class MockHttpConstants
    {
        public static readonly Dictionary<string, string> UriContentDictionary = new()
        {
            {
                "http://api/test",
                "Content as string"
            },
            {
                "https://cdn.graph.office.net/en-us/graph/api/proxy/endpoint",
                "\"https://graph.office.net/en-us/graph/api/proxy\""
            },
            {
                "https://graph.office.net/en-us/graph/api/proxy?url=https://graph.microsoft.com/v1.0/me/calendar?$whatif",
                @"{
   ""Description"":""Runs main request and in case of NotFound, discovers the uri and runs the request again."",
   ""Uri"":""https://outlook.office365.com/api/gv1.0/Users('48d31887-5fad-4d73-a9f5-3c356e68a038%40dcd219dd-bc68-4b9b-bf0b-4a33a796be35')/messages?$whatif"",
   ""HttpMethod"":""GET"",
   ""TargetWorkloadId"":""Microsoft.Exchange.Places""
  }"
            },
            {
                "https://graph.office.net/en-us/graph/api/proxy?url=https://graph.microsoft.com/v1.0/me/messages?$whatif",
                @"{
   ""Description"":""Runs main request and in case of NotFound, discovers the uri and runs the request again."",
   ""Uri"":""https://outlook.office365.com/api/gv1.0/Users('48d31887-5fad-4d73-a9f5-3c356e68a038%40dcd219dd-bc68-4b9b-bf0b-4a33a796be35')/messages?$whatif"",
   ""HttpMethod"":""GET"",
   ""TargetWorkloadId"":""Microsoft.Exchange""
  }"
            },
            {
                "https://graph.office.net/en-us/graph/api/proxy?url=https://graph.microsoft.com/v1.0/appCatalogs/teamsApps?$whatif",
                @"{
   ""Description"":""Runs main request and in case of NotFound, discovers the uri and runs the request again."",
   ""Uri"":""https://outlook.office365.com/api/gv1.0/foobar('48d31887-5fad-4d73-a9f5-3c356e68a038%40dcd219dd-bc68-4b9b-bf0b-4a33a796be35')/messages?$whatif"",
   ""HttpMethod"":""GET"",
   ""TargetWorkloadId"":""Microsoft.Teams.GraphSvc""
  }"
            }
        };

        public const string HttpRequestError =
@"{
   ""error"":
    {
     ""code"":""BadRequest"",
     ""message"":""Resource not found for the segment."",
     ""innerError"":
      {
       ""date"":""2021-10-20T08:00:00"",
       ""request-id"":""cabe7009-afbc-4777-b2f0-ec6c84940d4e"",
       ""client-request-id"":""cabe7009-afbc-4777-b2f0-ec6c84940d4e""
      }
    }
  }";
    }
}
