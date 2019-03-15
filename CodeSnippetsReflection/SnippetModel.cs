using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace CodeSnippetsReflection
{
    public class SnippetModel
    {
        private readonly IEdmModel edmModel;

        public HttpMethod Method { get; set; }
        public ODataUri  ParsedUrl { get; set; } 

        public List<ODataPathSegment> Segments { get; set; }
        public string Path { get; set; }
        public string responseVariableName { get; set; }

        public List<string> SelectField { get; set; }
        public List<string> ExpandField { get; set; }

        public string FilterString { get; set; }

        public List<KeyValuePair<string,ODataPathSegment>> Parameters { get; set; }

        public SnippetModel()
        {

        }

        public SnippetModel(HttpRequestMessage requestPayload, string serviceRootUrl, IEdmModel edmModel)
        {
            Method = requestPayload.Method;
            ParsedUrl = ParseUrl(new Uri(serviceRootUrl),requestPayload.RequestUri);
            this.edmModel = edmModel;
        }

        private ODataUri ParseUrl(Uri fullUrl, Uri requestUri)
        {

            //We include the UnqualifiedODataUriResolver 
            var parser = new ODataUriParser(this.edmModel, serviceRoot, fullUrl)
            {
                Resolver = new UnqualifiedODataUriResolver { EnableCaseInsensitive = true }
            };

            return parser.ParseUri();
        }
    }
}
