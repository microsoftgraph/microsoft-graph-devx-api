using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace OpenAPIService
{
    public class SearchResult
    {
        public CurrentKeys CurrentKeys { get; set; }
        public OpenApiOperation Operation { get; set; }
        public IList<OpenApiParameter> Parameters { get; set; }
    }

}
