﻿using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace OpenAPIService
{
    public class SearchResult
    {
        public CurrentKeys CurrentKeys { get; set; }
        public OpenApiOperation Operation { get; set; }
    }

}
