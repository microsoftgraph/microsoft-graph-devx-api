using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace CodeSnippetsReflection
{
    public class SnippetModel
    {
        private readonly IEdmModel _edmModel;
        public HttpMethod Method { get; set; }
        public ODataUri ODataUri { get; set; }
        public string resourceReturnType { get; set; }
        public SnippetModel(HttpRequestMessage requestPayload, string serviceRootUrl, IEdmModel edmModel)
        {
            this._edmModel = edmModel;
            this.Method = requestPayload.Method;
            this.ODataUri = GetODataUri(new Uri(serviceRootUrl), requestPayload.RequestUri);
            this.resourceReturnType = ODataUri.Path.LastOrDefault()?.Identifier;
        }

        private ODataUri GetODataUri(Uri serviceRootUri, Uri requestUri)
        {
            var parser = new ODataUriParser(this._edmModel, serviceRootUri, requestUri)
            {
                Resolver = new UnqualifiedODataUriResolver { EnableCaseInsensitive = true }
            };

            return parser.ParseUri();
        }
    }
}
