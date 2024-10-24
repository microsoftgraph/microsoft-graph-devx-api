using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CodeSnippetsReflection.OData.LanguageGenerators;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace CodeSnippetsReflection
{
    public class SnippetModel : SnippetBaseModel<ODataPathSegment>
    {
        private readonly IEdmModel _edmModel;
        public ODataUri ODataUri { get; set; }
        public ODataUriParser ODataUriParser { get; set; }
        public List<ODataPathSegment> Segments { get; set; }

        /// <summary>
        /// Model for the information needed to create a snippet from the request message
        /// </summary>
        /// <param name="requestPayload">The request message to generate a snippet from</param>
        /// <param name="serviceRootUrl">The service root URI</param>
        /// <param name="edmModel">The EDM model used for this request</param>
        public SnippetModel(HttpRequestMessage requestPayload, string serviceRootUrl, IEdmModel edmModel):base(requestPayload, serviceRootUrl)
        {
            this._edmModel = edmModel;
            this.ODataUriParser = GetODataUriParser(new Uri(serviceRootUrl), requestPayload.RequestUri);
            this.ODataUri = ODataUriParser.ParseUri();
            this.CustomQueryOptions = ODataUriParser.CustomQueryOptions;
            this.Segments = ODataUri.Path.ToList();
        }

        /// <summary>
        ///Get a string showing the name variable to be manipulated by the segment
        /// </summary>
        /// <param name="oDataPathSegment">The path segment in question</param>
        /// <returns> string to be used as return variable name in this call.</returns>
        protected override string GetResponseVariableName(ODataPathSegment oDataPathSegment)
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
        protected override string GetSearchExpression(string queryString) {
            if (ODataUri.Search == null)
                return string.Empty;
            var searchTerm = (SearchTermNode) ODataUri.Search.Expression;
            return searchTerm.Text;
        }

        /// <summary>
        /// This function populates the Select and Expand field lists in the class from reading the data from the
        /// Odata URI. <see cref="SelectExpandClause"/>
        /// </summary>
        protected override void PopulateSelectAndExpandQueryFields(string queryString)
        {
            if(ODataUri.SelectAndExpand == null) return;

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
                        if (!string.IsNullOrEmpty(ExpandFieldExpression))
                            break; // multiple expand parameters will result in this case being processed multiple times. So don't do it again if we already did it the first time.

                        //get the string from the start of the navigation source name.
                        ExpandFieldExpression = queryString.Substring(queryString.IndexOf( expandedNavigationSelectItem.PathToNavigationProperty.First().Identifier, StringComparison.Ordinal));
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
		protected override ODataPathSegment GetLastPathSegment()
		{
			return ODataUri.Path.LastOrDefault();
		}
	}
}
