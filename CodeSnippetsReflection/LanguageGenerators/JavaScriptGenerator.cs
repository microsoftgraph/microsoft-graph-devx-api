using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace CodeSnippetsReflection.LanguageGenerators
{
    class JavaScriptGenerator: ILanguageGenerator
    {
        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            try
            {
                var snippetBuilder = new StringBuilder();
                snippetBuilder.Append("var client = Client.init(...);\n");
                snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = client.api('{snippetModel.Path}')");

                if (snippetModel.Method == HttpMethod.Get)
                {
                    //append the api version if we are working with beta version.
                    if (snippetModel.ApiVersion.Equals("beta"))
                    {
                        snippetBuilder.Append($"\n\t.version('{snippetModel.ApiVersion}')");
                    }

                    //Append any search queries
                    if (snippetModel.FilterFieldList.Any())
                    {
                        var filterResult = new StringBuilder();
                        foreach (var queryOption in snippetModel.FilterFieldList)
                        {
                            filterResult.Append(queryOption);
                        }

                        //append the filter to the snippet
                        snippetBuilder.Append($"\n\t.filter(\"{filterResult}\")");
                    }

                    //Append any search queries
                    if (!string.IsNullOrEmpty(snippetModel.SearchExpression))
                    {
                        snippetBuilder.Append($"\n\t.search(\"{snippetModel.SearchExpression}\")");
                    }

                    //Append any expand queries
                    if (snippetModel.ExpandFieldList.Any())
                    {
                        var expandResult = new StringBuilder();
                        foreach (var queryOption in snippetModel.ExpandFieldList)
                        {
                            expandResult.Append(queryOption + ",");
                        }

                        expandResult.Remove(expandResult.Length - 1, 1);
                        //append the expand result to the snippet
                        snippetBuilder.Append($"\n\t.expand(\"{expandResult}\")");
                    }

                    //Append any select queries
                    if (snippetModel.SelectFieldList.Any())
                    {
                        var selectResult = new StringBuilder();
                        foreach (var queryOption in snippetModel.SelectFieldList)
                        {
                            selectResult.Append(queryOption + ",");
                        }

                        selectResult.Remove(selectResult.Length - 1, 1);
                        //append the select result to the snippet
                        snippetBuilder.Append($"\n\t.select(\"{selectResult}\")");
                    }

                    //Append any orderby queries
                    if (snippetModel.OrderByFieldList.Any())
                    {
                        var orderByResult = new StringBuilder();
                        foreach (var queryOption in snippetModel.OrderByFieldList)
                        {
                            orderByResult.Append(queryOption + " ");
                        }

                        //append the orderby result to the snippet
                        snippetBuilder.Append($"\n\t.orderby(\"{orderByResult}\")");
                    }

                    //Append any skip queries
                    if (snippetModel.ODataUri.Skip.HasValue)
                    {
                        snippetBuilder.Append($"\n\t.skip({snippetModel.ODataUri.Skip})");
                    }

                    //Append any skip token queries
                    if (!string.IsNullOrEmpty(snippetModel.ODataUri.SkipToken))
                    {
                        snippetBuilder.Append($"\n\t.skiptoken({snippetModel.ODataUri.SkipToken})");
                    }

                    //Append any top queries
                    if (snippetModel.ODataUri.Top.HasValue)
                    {
                        snippetBuilder.Append($"\n\t.top({snippetModel.ODataUri.Top})");
                    }

                    snippetBuilder.Append("\n\t.get(...);");

                    return snippetBuilder.ToString();
                }
                else
                {
                    throw new NotImplementedException("HTTP method not implemented");
                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
