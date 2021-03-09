// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace GraphWebApi.Telemetry.Interfaces
{
    public interface ITelemetryHelper
    {
        public Stopwatch BeginRequest();
        public void EndRequest(Stopwatch stopwatch, HttpContext httpContext, string eventName);
    }
}
