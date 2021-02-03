using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CodeSnippetsReflection.LanguageGenerators;
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
        public ODataUriParser ODataUriParser { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public string ApiVersion { get; set; }
        public string ResponseVariableName { get; set; }
        public string SearchExpression { get; set; }
        public List<ODataPathSegment> Segments { get; set; }
        public List<string> SelectFieldList { get; set; }
        public string ExpandFieldExpression { get; set; }
        public List<string> FilterFieldList { get; set; }
        public List<string> OrderByFieldList { get; set; }
        public IEnumerable<KeyValuePair<string, IEnumerable<string>>> RequestHeaders { get; set; }
        public IEnumerable<KeyValuePair<string, string>> CustomQueryOptions { get; set; }
        public string RequestBody { get; set; }
        public string ContentType { get; set; }

        /// <summary>
        /// Model for the information needed to create a snippet from the request message
        /// </summary>
        /// <param name="requestPayload">The request message to generate a snippet from</param>
        /// <param name="serviceRootUrl">The service root URI</param>
        /// <param name="edmModel">The EDM model used for this request</param>
        public SnippetModel(HttpRequestMessage requestPayload, string serviceRootUrl, IEdmModel edmModel)
        {
            this._edmModel = edmModel;
            this.Method = requestPayload.Method;
            this.ODataUriParser = GetODataUriParser(new Uri(serviceRootUrl), requestPayload.RequestUri);
            this.ODataUri = ODataUriParser.ParseUri();
            this.CustomQueryOptions = ODataUriParser.CustomQueryOptions;
            this.Segments = ODataUri.Path.ToList();
            this.Path = Uri.UnescapeDataString(requestPayload.RequestUri.AbsolutePath.Substring(5));
            this.QueryString = requestPayload.RequestUri.Query;
            this.ApiVersion = serviceRootUrl.Substring(serviceRootUrl.Length - 4);
            this.SelectFieldList = new List<string>();
            this.FilterFieldList = new List<string>();
            this.OrderByFieldList = new List<string>();
            this.RequestHeaders = requestPayload.Headers;

            // replace the response variable name with generic response when URL has placeholder
            // e.g. {size} in GET graph.microsoft.com/v1.0/me/drive/items/{item-id}/thumbnails/{thumb-id}/{size}
            var responseVariableName = GetResponseVariableName(ODataUri.Path.LastOrDefault());
            this.ResponseVariableName = responseVariableName.StartsWith("{") ? "response" : responseVariableName;

            PopulateQueryFieldLists(QueryString);
            GetRequestBody(requestPayload);
        }

        /// <summary>
        ///Get a string showing the name variable to be manipulated by the segment
        /// </summary>
        /// <param name="oDataPathSegment">The path segment in question</param>
        /// <returns> string to be used as return variable name in this call.</returns>
        private string GetResponseVariableName(ODataPathSegment oDataPathSegment)
        {
            var edmType = oDataPathSegment.EdmType;

            // when we are trying to create an entity(method == Post) make sure we try to get the name
            // of a single item in the collection as we will be posting a single item.
            if ((this.Method == HttpMethod.Post) && (edmType is IEdmCollectionType innerCollection))
            {
                edmType = innerCollection.ElementType.Definition;
            }

            if (edmType is IEdmNamedElement edmNamedElement)
            {
                return CommonGenerator.LowerCaseFirstLetter(edmNamedElement.Name);
            }

            //its not a collection/or named type so the identifier can do
            var identifier = oDataPathSegment.Identifier.Contains(".")
                ? oDataPathSegment.Identifier.Split(".").Last()
                : oDataPathSegment.Identifier;
            return CommonGenerator.LowerCaseFirstLetter(identifier);
        }


        /// <summary>
        /// This function creates a Odata Uri Parser object from the serviceRootUri and the RequestUri
        /// </summary>
        /// <param name="serviceRootUri">The service root URI</param>
        /// <param name="requestUri">The request URI</param>
        private ODataUriParser GetODataUriParser(Uri serviceRootUri, Uri requestUri)
        {
            var parser = new ODataUriParser(this._edmModel, serviceRootUri, requestUri)
            {
                Resolver = new UnqualifiedODataUriResolver {EnableCaseInsensitive = true}
            };

            return parser;
        }

        /// <summary>
        /// This function populates the Select and Expand field lists in the class from reading the data from the
        /// Odata URI. <see cref="SelectExpandClause"/>
        /// </summary>
        private void PopulateSelectAndExpandQueryFields(string queryString)
        {
            var pathSelectedItems = ODataUri.SelectAndExpand.SelectedItems;
            foreach (var item in pathSelectedItems)
            {
                switch (item)
                {
                    //its a select query
                    case PathSelectItem pathSelectItem:
                        SelectFieldList.Add(pathSelectItem.SelectedPath.FirstSegment.Identifier);
                        break;
                    //its an expand query
                    case ExpandedNavigationSelectItem expandedNavigationSelectItem:
                        //get the string from the start of the navigation source name.
                        ExpandFieldExpression = queryString.Substring(queryString.IndexOf( expandedNavigationSelectItem.NavigationSource.Name, StringComparison.Ordinal));
                        //check if there are other queries present and chunk them off to remain with only the expand parameter
                        var index = ExpandFieldExpression.IndexOf("&", StringComparison.Ordinal);
                        if (index > 0)
                        {
                            ExpandFieldExpression = ExpandFieldExpression.Substring(0, index);
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// This function splits the query part of the url to individual query section(filter, search etc)
        /// and then populates the relevant field list lists in the class data structures in th
        /// </summary>
        /// <param name="queryString">Query section of url as a string</param>
        private void PopulateQueryFieldLists(string queryString)
        {
            var queryStrings = System.Web.HttpUtility.ParseQueryString(queryString);
            foreach (var key in queryStrings.AllKeys)
            {
                // in beta, $ is optional for OData queries.
                // https://docs.microsoft.com/en-us/graph/query-parameters
                var optionalKey = key.StartsWith("$") ? key[1..] : key;
                if (optionalKey.ToLowerInvariant() == "filter")
                {
                    FilterFieldList = new List<string> { queryStrings[key] };
                }
                else if (optionalKey.ToLowerInvariant() == "orderby")
                {
                    OrderByFieldList = new List<string> { queryStrings[key] };
                }
            }

            if (ODataUri.Search != null)
            {
                var searchTerm = (SearchTermNode) ODataUri.Search.Expression;
                SearchExpression = searchTerm.Text;
            }

            if (ODataUri.SelectAndExpand != null)
            {
                PopulateSelectAndExpandQueryFields(queryString);
            }

        }

        /// <summary>
        /// Reads the request body from the request payload to save it as a string for later processing
        /// </summary>
        /// <param name="requestPayload"><see cref="HttpRequestMessage"/> to read the body from</param>
        private void GetRequestBody(HttpRequestMessage requestPayload)
        {
            //do not try to read in the content if there isn't any
            if (null != requestPayload.Content)
            {
                this.RequestBody = requestPayload.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if(null != requestPayload.Content.Headers.ContentType)
                    this.ContentType = requestPayload.Content.Headers.ContentType.MediaType;
            }
            else
            {
                this.RequestBody = string.Empty;
            }
        }
    }

}
