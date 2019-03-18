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
        public string Path { get; set; }
        public string ApiVersion { get; set; }
        public string ResponseVariableName { get; set; }
        public string SearchExpression { get; set; }
        public List<ODataPathSegment> Segments { get; set; }
        public List<string> SelectFieldList { get; set; }
        public List<string> ExpandFieldList { get; set; }
        public List<string> FilterFieldList { get; set; }
        public List<string> OrderByFieldList { get; set; }

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
            this.ODataUri = GetODataUri(new Uri(serviceRootUrl), requestPayload.RequestUri);
            this.ResponseVariableName = ODataUri.Path.LastOrDefault()?.Identifier;
            this.Segments = ODataUri.Path.ToList();
            this.Path = Uri.UnescapeDataString(requestPayload.RequestUri.AbsolutePath.Substring(5));
            this.ApiVersion = serviceRootUrl.Substring(serviceRootUrl.Length - 4);
            this.SelectFieldList = new List<string>();
            this.ExpandFieldList = new List<string>();
            this .FilterFieldList = new List<string>();
            this.OrderByFieldList = new List<string>();

            PopulateQueryFieldLists(requestPayload.RequestUri.Query);
        }

        /// <summary>
        /// This function creates a Odata Uri object from the serviceRootUri and the RequestUri
        /// </summary>
        /// <param name="serviceRootUri">The service root URI</param>
        /// <param name="requestUri">The request URI</param>
        private ODataUri GetODataUri(Uri serviceRootUri, Uri requestUri)
        {
            var parser = new ODataUriParser(this._edmModel, serviceRootUri, requestUri)
            {
                Resolver = new UnqualifiedODataUriResolver {EnableCaseInsensitive = true}
            };

            return parser.ParseUri();
        }

        /// <summary>
        /// This function populates the Select and Expand field lists in the class from reading the data from the
        /// Odata URI. <see cref="SelectExpandClause"/>
        /// </summary>
        private void PopulateSelectAndExpandQueryFieldLists()
        {
            var pathSelectedItems = ODataUri.SelectAndExpand.SelectedItems;
            //TODO the implementation of this is abit naive. Ideally the queries can be nested
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
                        ExpandFieldList.Add(expandedNavigationSelectItem.PathToNavigationProperty.FirstSegment.Identifier);
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
            var querySegmentList = GetODataQuerySegments(queryString);

            foreach (var queryOption in querySegmentList)
            {
                //split the string and get the second half with the fields
                var filterQueryOption = queryOption.Split('=').Last();
                //split the string with the & character and get the list of params
                var queryParams = filterQueryOption.Replace('=', ' ').Split("&").ToList();
                if (queryOption.ToLower().Contains("filter"))
                {
                    FilterFieldList = queryParams;
                }
                else if (queryOption.ToLower().Contains("orderby"))
                {
                    OrderByFieldList = queryParams;
                }
            }

            if (ODataUri.Search != null)
            {
                var searchTerm = (SearchTermNode) ODataUri.Search.Expression;
                SearchExpression = searchTerm.Text;
            }

            if (ODataUri.SelectAndExpand != null)
            {
                PopulateSelectAndExpandQueryFieldLists();
            }

        }

        /// <summary>
        /// Splits the Query part of a full uri into different query segments i.e. select, expand etc
        /// </summary>
        /// <param name="queryString">Query section of url as a string</param>
        /// <returns>A string collection with the query segments</returns>
        private IEnumerable<string> GetODataQuerySegments(string queryString)
        {
            //Escape all special characters in the uri
            var fullUriQuerySegment = Uri.UnescapeDataString(queryString);
            //split by the $ symbol to get each OData Query Parser
            var querySegmentList = fullUriQuerySegment.Split('$');

            return querySegmentList;
        }
    }

}
