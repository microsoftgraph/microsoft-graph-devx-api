using System;
using System.Collections.Generic;
using System.Net.Http;

namespace CodeSnippetsReflection
{
    public abstract class SnippetBaseModel<PathSegment>
    {
        public HttpMethod Method { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public string ApiVersion { get; set; }
        public string ResponseVariableName { get; set; }
        public string SearchExpression { get; set; }
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
        protected SnippetBaseModel(HttpRequestMessage requestPayload, string serviceRootUrl)
        {
            if(requestPayload == null || requestPayload.RequestUri == null) throw new ArgumentNullException(nameof(requestPayload));
			if(string.IsNullOrWhiteSpace(serviceRootUrl)) throw new ArgumentNullException(nameof(serviceRootUrl));

            this.Method = requestPayload.Method;
            this.Path = Uri.UnescapeDataString(requestPayload.RequestUri.AbsolutePath.Substring(5));
            this.QueryString = Uri.UnescapeDataString(requestPayload.RequestUri.Query);
            this.ApiVersion = serviceRootUrl.Substring(serviceRootUrl.Length - 4);
            this.SelectFieldList = new List<string>();
            this.FilterFieldList = new List<string>();
            this.OrderByFieldList = new List<string>();
            this.RequestHeaders = requestPayload.Headers;
        }
        /// <summary>
        /// Initializes the model from the request message. This method MUST be called in the derived type constructor
        /// </summary>
        /// <param name="requestPayload">The request message to initialize the snippet from</param>
        public async System.Threading.Tasks.Task InitializeModelAsync(HttpRequestMessage requestPayload)
        {
            // replace the response variable name with generic response when URL has placeholder
            // e.g. {size} in GET graph.microsoft.com/v1.0/me/drive/items/{item-id}/thumbnails/{thumb-id}/{size}
            var lastPathSegment = GetLastPathSegment();
            var responseVariableName = GetResponseVariableName(lastPathSegment);
            this.ResponseVariableName = responseVariableName.StartsWith("{") ? "response" : responseVariableName;

            PopulateQueryFieldLists(QueryString);
            await GetRequestBodyAsync(requestPayload);
        }
        protected abstract PathSegment GetLastPathSegment();
        /// <summary>
        ///Get a string showing the name variable to be manipulated by the segment
        /// </summary>
        /// <param name="pathSegment">The path segment in question</param>
        /// <returns> string to be used as return variable name in this call.</returns>
        protected abstract string GetResponseVariableName(PathSegment pathSegment);

        /// <summary>
        /// This function populates the Select and Expand field lists in the class from reading the data from the
        /// Odata URI. <see cref="SelectExpandClause"/>
        /// </summary>
        protected abstract void PopulateSelectAndExpandQueryFields(string queryString);
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
                // key can be null if HTTP request is not formated correctly e.g., '/users?&$OrderBy=DisplayName'
                if (key == null) { continue; }
                // in beta, $ is optional for OData queries.
                // https://docs.microsoft.com/en-us/graph/query-parameters
                if (key.Contains("filter", StringComparison.OrdinalIgnoreCase))
                {
                    FilterFieldList = new List<string> { queryStrings[key] };
                }
                else if (key.Contains("orderby", StringComparison.OrdinalIgnoreCase))
                {
                    OrderByFieldList = new List<string> { queryStrings[key] };
                }
            }

            SearchExpression = GetSearchExpression(queryString);

            PopulateSelectAndExpandQueryFields(queryString);
        }
        protected abstract string GetSearchExpression(string queryString);

        /// <summary>
        /// Reads the request body from the request payload to save it as a string for later processing
        /// </summary>
        /// <param name="requestPayload"><see cref="HttpRequestMessage"/> to read the body from</param>
        private async System.Threading.Tasks.Task GetRequestBodyAsync(HttpRequestMessage requestPayload)
        {
            //do not try to read in the content if there isn't any
            if (null != requestPayload.Content)
            {
                this.RequestBody = await requestPayload.Content.ReadAsStringAsync();
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
