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
        private Lazy<IEdmModel> iedmModelV1 { get;  set; }
        private Lazy<IEdmModel> iedmModelBeta { get;  set; }
        private Uri serviceRootV1 { get; set; }
        private Uri serviceRootBeta { get; set; }

        public SnippetsGenerator()
        {
            LoadGraphMetadata();
        }

        /// <summary>
        /// Load the IEdmModel for both V1 and Beta
        /// </summary>
        private void LoadGraphMetadata()
        {
            serviceRootV1 = new Uri("https://graph.microsoft.com/v1.0");
            serviceRootBeta = new Uri("https://graph.microsoft.com/beta");

            iedmModelV1 = new Lazy<IEdmModel>(() => CsdlReader.Parse(XmlReader.Create(serviceRootV1 + "/$metadata")));
            iedmModelBeta = new Lazy<IEdmModel>(() => CsdlReader.Parse(XmlReader.Create(serviceRootBeta + "/$metadata")));
        }

        /// <summary>
        /// Check the graph api veesion and the snippet language requested 
        /// </summary>
        /// <param name="requestPayload"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public string ProcessPayloadRequest(HttpRequestMessage httpRequestMessage, string language)
        {
            ODataUriParser oDataUriParser = null;

            if (httpRequestMessage.RequestUri.Segments[1].Equals("v1.0/"))
            {
                oDataUriParser = new ODataUriParser(iedmModelV1.Value, serviceRootV1, httpRequestMessage.RequestUri)
                {
                    Resolver = new UnqualifiedODataUriResolver { EnableCaseInsensitive = true }
                };
            }
            else if (httpRequestMessage.RequestUri.Segments[1].Equals("beta/"))
            {   
                oDataUriParser = new ODataUriParser(iedmModelBeta.Value, serviceRootBeta, httpRequestMessage.RequestUri)
                {
                    Resolver = new UnqualifiedODataUriResolver { EnableCaseInsensitive = true }
                };

            }
            else
            {
                throw new Exception("Unsuported Graph version in url");
            }

            ODataUri odatauri = oDataUriParser.ParseUri();

            if (language.ToLower() == "c#")
            {
                return GenerateCsharpSnippet(odatauri, httpRequestMessage.RequestUri , httpRequestMessage.Method);
            }

            throw new Exception("No code snippet generated"); 
        }

        /// <summary>
        /// Formulates the requested Graph snippets and returns it as string
        /// </summary>
        /// <returns></returns>
        public string GenerateCsharpSnippet(ODataUri oDataUri, Uri requestUri , HttpMethod httpMethod)
        {
            try
            {
                if (httpMethod == HttpMethod.Get)
                {
                    StringBuilder snippet = new StringBuilder();

                    //Fomulate all resources path
                    snippet = GenerateResourcesPath(oDataUri);

                    /********************************/
                    /**Formulate the Query options**/
                    /*******************************/

                    if (oDataUri.Filter != null)
                    {
                        snippet.Append(FilterExpression(oDataUri, requestUri).ToString());
                    }

                    if (oDataUri.SelectAndExpand != null)
                    {
                        snippet.Append(SelectExpression(oDataUri, requestUri).ToString());
                    }

                    if (oDataUri.Search != null)
                    {
                        snippet.Append(SearchExpression(oDataUri).ToString());
                    }

                    if (oDataUri.OrderBy != null)
                    {
                        snippet.Append(OrderbyExpression(oDataUri, requestUri).ToString());
                    }

                    if (oDataUri.Skip != null)
                    {
                        snippet.Append(SkipExpression(oDataUri).ToString());
                    }

                    if (oDataUri.Top != null)
                    {
                        snippet.Append(TopExpression(oDataUri).ToString());
                    }

                    snippet.Append(".GetAsync();");

                    return snippet.ToString();
                }
                else if (httpMethod == HttpMethod.Post)
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
                if (item is KeySegment keySegment)
                {
                    resourcesPath.Append($@"[");

                    foreach (KeyValuePair<string, object> keyValuePair in keySegment.Keys)
                    {
                        // query by entity key
                        resourcesPath.Append($"\"{keyValuePair.Value}\"");
                        break;
                    }

                    resourcesPath.Append($@"]");
                }
                else if (item is OperationSegment operationSegment)
                {
                    resourcesPath.Append("." + UppercaseFirstLetter(operationSegment.Identifier) + "(");
                    foreach (var parameter in operationSegment.Parameters)
                    {
                        if (parameter.Value is ConvertNode convertNode)
                        {
                            if (convertNode.Source is ConstantNode constantNode)
                            {
                                resourcesPath.Append(constantNode.LiteralText + ", ");
                            }
                        }
                        else if(parameter.Value is ConstantNode constantNode)
                        {
                            resourcesPath.Append(constantNode.LiteralText + ", ");
                        }
                    }

                    if (operationSegment.Parameters.Any())
                    {
                        //remove any parameters
                        resourcesPath.Remove(resourcesPath.Length - 2, 2);
                    }

                    resourcesPath.Append(")");
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
