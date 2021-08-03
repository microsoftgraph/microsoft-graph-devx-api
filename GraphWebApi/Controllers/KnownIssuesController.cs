// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using KnownIssuesService.Interfaces;
using KnownIssuesService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using UtilityService;

namespace GraphWebApi.Controllers
{
    [ApiController]
    public class KnownIssuesController : ControllerBase
    {
        private readonly IKnownIssuesService _knownIssuesService;

        public KnownIssuesController(IKnownIssuesService knownIssuesService)
        {
            UtilityFunctions.CheckArgumentNull(knownIssuesService, nameof(knownIssuesService));
            _knownIssuesService = knownIssuesService;
        }

        // GET: api/<KnownIssuesController>
        [Route("api/[controller]")]
        [Route("knownissues")]
        [HttpGet]
        public Task<List<KnownIssue>> Get()
        {
            return _knownIssuesService.QueryBugsAsync();
        }
    }
}
