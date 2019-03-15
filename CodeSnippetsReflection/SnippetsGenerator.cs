using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Xml;
using System.Configuration;

//ODataUriParser and Dependancies
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.UriParser;
using System.Linq;

namespace CodeSnippetsReflection
{
    /// <summary>
    /// Snippets Generator Class with all the logic for code generation
    /// </summary>
    public class SnippetsGenerator : ISnippetsGenerator
    {
        private IEdmModel iedmModel;

        public SnippetsGenerator()
        {
            GetGraphMetadataContainer();
        }


        /// <summary>
        /// Load the IEdmModel for both V1 and Beta
        /// </summary>
        private void GetGraphMetadataContainer()
        {
            Uri serviceRootV1 = new Uri("https://graph.microsoft.com/v1.0");
            Uri serviceRootBeta = new Uri("https://graph.microsoft.com/beta");

            IEdmModel iedmModelV1 = CsdlReader.Parse(XmlReader.Create(serviceRootV1 + "/$metadata"));
            IEdmModel iedmModelBeta = CsdlReader.Parse(XmlReader.Create(serviceRootBeta + "/$metadata"));

            GraphMetadataContainer.graphMetadataVersion1 = iedmModelV1;
            GraphMetadataContainer.graphMetadataVersionBeta = iedmModelBeta;
        }

        /// <summary>
        /// Check the graph api veesion and the snippet language requested 
        /// </summary>
        /// <param name="requestPayload"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public string ProcessPayloadRequest(HttpRequestMessage requestPayload, string language)
        {
            string serviceRootUrl = "";

            if (language == null || language == String.Empty)
            {
                language = "c#";
            }          

            if (requestPayload.RequestUri.Segments[1].Equals("v1.0/"))
            {
                serviceRootUrl = "https://graph.microsoft.com/v1.0";
                this.iedmModel = GraphMetadataContainer.graphMetadataVersion1;

            }
            else if (requestPayload.RequestUri.Segments[1].Equals("beta/"))
            {
                serviceRootUrl = "https://graph.microsoft.com/beta";
                this.iedmModel = GraphMetadataContainer.graphMetadataVersionBeta;
            }
            else
            {
                throw new Exception("Unsuported Graph version in url");
            }

            if(language.ToLower() == "c#")
            {
                return GenerateCsharpSnippet(requestPayload, serviceRootUrl);
            }            

            return "No code snippet generated";
        }

        /// <summary>
        /// Formulates the requested Graph snippets and returns it as string
        /// </summary>
        /// <returns></returns>
        public string GenerateCsharpSnippet(HttpRequestMessage requestPayload, string serviceRootUrl)
        {
            try
            {
                if (requestPayload.Method == HttpMethod.Get)
                {
                    Uri serviceRoot = new Uri(serviceRootUrl);
                    Uri fullUri = requestPayload.RequestUri;

                    //We include the UnqualifiedODataUriResolver 
                    var parser = new ODataUriParser(this.iedmModel, serviceRoot, fullUri)
                    {
                        Resolver = new UnqualifiedODataUriResolver { EnableCaseInsensitive = true }
                    };

                    ODataUri odatauri = parser.ParseUri();
                    StringBuilder snippet = new StringBuilder();

                    //Fomulate all resources path
                    snippet = GenerateResourcesPath(odatauri);

                    /********************************/
                    /**Formulate the Query options**/
                    /*******************************/

                    if (odatauri.Filter != null)
                    {
                        snippet.Append(FilterExpression(odatauri, fullUri).ToString());
                    }

                    if (odatauri.SelectAndExpand != null)
                    {
                        snippet.Append(SelectExpression(odatauri, fullUri).ToString());
                    }

                    if (odatauri.Search != null)
                    {
                        snippet.Append(SearchExpression(odatauri).ToString());
                    }

                    if (odatauri.OrderBy != null)
                    {
                        snippet.Append(OrderbyExpression(odatauri, fullUri).ToString());
                    }

                    if (odatauri.Skip != null)
                    {
                        snippet.Append(SkipExpression(odatauri).ToString());
                    }

                    if (odatauri.Top != null)
                    {
                        snippet.Append(TopExpression(odatauri).ToString());
                    }

                    snippet.Append(".GetAsync();");

                    return snippet.ToString();
                }
                else if (requestPayload.Method == HttpMethod.Post)
                {
                    //TODO
                    return "POST Result";
                }
                else
                {
                    //TODO
                    return "Uknown HttpMethod Result";
                }
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
        }



        public string GenerateJavascriptSnippet(HttpRequestMessage requestPayload, string serviceRootUrl) {

            var metamodel = GetModel(serviceRootUrl);
            var model = new SnippetModel(requestPayload, serviceRootUrl, metamodel);

            var sb = new StringBuilder();

            sb.Append("var client = Client.Init(...);\n");
            sb.Append($"var {model.responseVariableName} = client.api('{model.Path}'Request({GetJavascriptOptions(model)})) ");

            return sb.ToString();
        }

        public object GetJavascriptOptions(SnippetModel model)
        {
            throw new NotImplementedException();
        }

        private IEdmModel GetModel(string serviceRootUrl)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Formulates the resources part of the generated snippets
        /// </summary>
        /// <param name="odatauri"></param>
        /// <returns></returns>
        private StringBuilder GenerateResourcesPath(ODataUri odatauri)
        {
            StringBuilder resourcesPath = new StringBuilder();
            resourcesPath.Append("GraphServiceClient graphClient = new GraphServiceClient();\n");
            //resourcesPath.Append("graphClient");

            string resourceReturnType = odatauri.Path.LastOrDefault().Identifier;
            resourcesPath.Append($"var {resourceReturnType} = await graphClient");

            // lets append all resources
            foreach (var item in odatauri.Path)
            {
                if (item.GetType() == typeof(KeySegment))
                {
                    KeySegment keySegment = (KeySegment)item;
                    resourcesPath.Append($@"[");

                    foreach (KeyValuePair<string, object> keyValuePair in keySegment.Keys)
                    {
                        // query by entity key
                        resourcesPath.Append($"\"{keyValuePair.Value}\"");
                        break;
                    }

                    resourcesPath.Append($@"]");
                }
                else
                {
                    resourcesPath.Append("." + UppercaseFirstLetter(item.Identifier));
                }
            }

            resourcesPath.Append(".Request()");           
            return resourcesPath;
        }


        #region OData Query Options Expresssions

        /// <summary>
        /// Formulates Select query options and its respective parameters
        /// </summary>
        /// <param name="odatauri"></param>
        /// <returns></returns>
        private StringBuilder SelectExpression(ODataUri odatauri, Uri fullUri)
        {
            StringBuilder selectExpandExpression = new StringBuilder();

            string[] querySegmentList = GetODataQuerySegments(fullUri.Query);
            var pathSelectedItems = odatauri.SelectAndExpand.SelectedItems;
            bool expand = false;
            bool select = false;

            foreach (string x in querySegmentList)
            {
                if (x.ToLower().Contains("expand="))
                {
                    expand = true;
                }
                else if(x.ToLower().Contains("select="))
                {
                    select = true;
                }
            }



            if (expand)
            {
                selectExpandExpression.Append(".Expand(\"");                            

                foreach (PathSelectItem item in pathSelectedItems)
                {
                    foreach (var si in item.SelectedPath)
                    {
                        selectExpandExpression.Append(si.Identifier + ",");
                    }
                }
                //selectExpandExpression.Remove(selectExpandExpression.Length - 1, 1);
                //selectExpandExpression.Append("\")");
            }

            if(select)
            {
                selectExpandExpression.Append(".Select(\"");

                //var pathSelectedItems = odatauri.SelectAndExpand.SelectedItems;

                foreach (PathSelectItem item in pathSelectedItems)
                {
                    foreach (var si in item.SelectedPath)
                    {
                        selectExpandExpression.Append(si.Identifier + ",");
                    }
                }
                //selectExpandExpression.Remove(selectExpandExpression.Length - 1, 1);
                //selectExpandExpression.Append("\")");
            }

            selectExpandExpression.Remove(selectExpandExpression.Length - 1, 1);
            selectExpandExpression.Append("\")");

            return selectExpandExpression;
        }       

        /// <summary>
        /// Formulates Filter query options and its respective parameters
        /// </summary>
        /// <param name="odatauri"></param>
        /// <returns></returns>
        private StringBuilder FilterExpression(ODataUri odatauri, Uri fullUri)
        {
            StringBuilder filterExpression = new StringBuilder();
            string filterResult = "";

            //split by the $ symbol to get each OData Query Parser
            string[] querySegmentList = GetODataQuerySegments(fullUri.Query);

            //Iterate through to find the filter option with the array
            foreach (var queryOption in querySegmentList)
            {
                if (queryOption.ToLower().Contains("filter"))
                {
                    string[] filterQueryOptionParts = queryOption.Split('=');
                    string filterQueryOption = filterQueryOptionParts.Last();

                    //there are 2 characters we dont need in this segment (=,&)
                    filterResult = filterQueryOption.Replace('=', ' ');
                    filterResult = filterQueryOption.Replace('&', ' ').Trim();                  
                }
            }

            filterExpression.Append($"\n.Filter(\"{filterResult}\")");
            return filterExpression;
        }

        /// <summary>
        /// Formulates Orderby query options and its respective parameters
        /// </summary>
        /// <param name="odatauri"></param>
        /// <returns></returns>
        private StringBuilder OrderbyExpression(ODataUri odatauri, Uri fullUri)
        {
            StringBuilder orderbyExpression = new StringBuilder();

            string orderByResult = "";

            //split by the $ symbol to get each OData Query Parser
            string[] querySegmentList = GetODataQuerySegments(fullUri.Query);

            //Iterate through to find the filter option with the array
            foreach (var queryOption in querySegmentList)
            {
                if (queryOption.ToLower().Contains("orderby"))
                {
                    string[] orderByQueryOptionParts = queryOption.Split('=');
                    string orderByQueryOption = orderByQueryOptionParts.Last();

                    //there are 2 characters we dont need in this segment (=,&)
                    orderByResult = orderByQueryOption.Replace('=', ' ');
                    orderByResult = orderByQueryOption.Replace('&', ' ').Trim();
                }
            }

            orderbyExpression.Append($"\n.OrderBy(\"{orderByResult}\")");

            //TODO
            return orderbyExpression;
        }
                      

        /// <summary>
        /// 
        /// </summary>
        /// <param name="odatauri"></param>
        /// <returns></returns>
        private StringBuilder SearchExpression(ODataUri odatauri)
        {
            StringBuilder searchExpression = new StringBuilder();
           
            //TODO
            return searchExpression;
        }

        /// <summary>
        /// Formulates Skip query options and its respective value
        /// </summary>
        /// <param name="odatauri"></param>
        /// <returns></returns>
        private StringBuilder SkipExpression(ODataUri odatauri)
        {
            StringBuilder skipExpression = new StringBuilder();
            skipExpression.Append($"\n.Skip({odatauri.Skip})");

            return skipExpression;
        }

        /// <summary>
        /// Formulates SkipToken query options and its respective value
        /// </summary>
        /// <param name="odatauri"></param>
        /// <returns></returns>
        private StringBuilder SkipTokenExpression(ODataUri odatauri)
        {
            StringBuilder skipTokenExpression = new StringBuilder();
            skipTokenExpression.Append($"\n.SkipToken({odatauri.SkipToken})");

            return skipTokenExpression;
        }


        /// <summary>
        /// Formulates Top query options and its respective value
        /// </summary>
        /// <param name="odatauri"></param>
        /// <returns></returns>
        private StringBuilder TopExpression(ODataUri odatauri)
        {
            StringBuilder topExpression = new StringBuilder();
            topExpression.Append($"\n.Top({odatauri.Top})");

            return topExpression;
        }

        /// <summary>
        /// Splits the Query part of a full uri
        /// </summary>
        /// <returns></returns>
        private string[] GetODataQuerySegments(string oDataQuery)
        {
            //Get all the query parts from the uri
            string fullUriQuerySegment = oDataQuery;

            //Escape all special characters in the uri
            fullUriQuerySegment = Uri.UnescapeDataString(fullUriQuerySegment);

            //split by the $ symbol to get each OData Query Parser
            string[] querySegmentList = fullUriQuerySegment.Split('$');

            return querySegmentList;
        }


        #endregion


        private static string UppercaseFirstLetter(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }
    }
}
