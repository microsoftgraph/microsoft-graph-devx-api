using Microsoft.OData.UriParser;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace CodeSnippetsReflection.LanguageGenerators
{
    public class CSharpGenerator : ILanguageGenerator
    {
        /// <summary>
        /// Formulates the requested Graph snippets and returns it as string
        /// </summary>
        /// <param name="snippetModel">Model of the Snippets info <see cref="SnippetModel"/></param>
        /// <returns>String of the snippet in Csharp code</returns>
        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            var snippetBuilder = new StringBuilder();

            try
            {
                if (snippetModel.Method == HttpMethod.Get)
                {
                    snippetBuilder.Append("GraphServiceClient graphClient = new GraphServiceClient();\n");
                    snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = await graphClient");

                    //Generate the Resources path for Csharp
                    snippetBuilder.Append(CSharpGenerateResourcesPath(snippetModel));

                    //Append any filter queries
                    if (snippetModel.FilterFieldList.Any())
                    {
                        var filterResult = new StringBuilder();
                        foreach (var queryOption in snippetModel.FilterFieldList)
                        {
                            filterResult.Append(queryOption);
                        }

                        //append the filter to the snippet
                        snippetBuilder.Append($"\n\t.Filter(\"{filterResult}\")");
                    }

                    //Append any search queries
                    if (!string.IsNullOrEmpty(snippetModel.SearchExpression))
                    {
                        snippetBuilder.Append($"\n\t.Search(\"{snippetModel.SearchExpression}\")");
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
                        snippetBuilder.Append($"\n\t.Expand(\"{expandResult}\")");
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
                        snippetBuilder.Append($"\n\t.Select(\"{selectResult}\")");
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
                        snippetBuilder.Append($"\n\t.OrderBy(\"{orderByResult}\")");
                    }

                    //Append any skip queries
                    if (snippetModel.ODataUri.Skip.HasValue)
                    {
                        snippetBuilder.Append($"\n\t.Skip({snippetModel.ODataUri.Skip})");
                    }

                    //Append any skip token queries
                    if (!string.IsNullOrEmpty(snippetModel.ODataUri.SkipToken))
                    {
                        snippetBuilder.Append($"\n\t.SkipToken({snippetModel.ODataUri.SkipToken})");
                    }

                    //Append any top queries
                    if (snippetModel.ODataUri.Top.HasValue)
                    {
                        snippetBuilder.Append($"\n\t.Top({snippetModel.ODataUri.Top})");
                    }

                    //Append footers
                    snippetBuilder.Append("\n\t.Request()");
                    snippetBuilder.Append(".GetAsync();");

                    return snippetBuilder.ToString();
                }

                throw new NotImplementedException("HTTP method not implemented");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Formulates the resources part of the generated snippets
        /// </summary>
        /// <param name="snippetModel">Model of the Snippets info <see cref="SnippetModel"/></param>
        /// <returns>String of the resources in Csharp code</returns>
        private string CSharpGenerateResourcesPath(SnippetModel snippetModel)
        {
            StringBuilder resourcesPath = new StringBuilder();
            // lets append all resources
            foreach (var item in snippetModel.Segments)
            {
                switch (item)
                {
                    //handle indexing into collections
                    case KeySegment keySegment:
                        //append opening brace
                        resourcesPath.Append(@"[");

                        foreach (var keyValuePair in keySegment.Keys)
                        {
                            // query by entity key
                            resourcesPath.Append($"\"{keyValuePair.Value}\"");
                            break;
                        }
                        //append closing brace
                        resourcesPath.Append(@"]");
                        break;

                    //handle functions/actions and any parameters present into collections
                    case OperationSegment operationSegment:
                        //opening bracket
                        resourcesPath.Append("." + UppercaseFirstLetter(operationSegment.Identifier) + "(");
                        foreach (var parameter in operationSegment.Parameters)
                        {
                            switch (parameter.Value)
                            {
                                case ConvertNode convertNode:
                                    {
                                        if (convertNode.Source is ConstantNode constantNode)
                                        {
                                            resourcesPath.Append(constantNode.LiteralText + ", ");
                                        }
                                        break;
                                    }
                                case ConstantNode constantNode:
                                    resourcesPath.Append(constantNode.LiteralText + ", ");
                                    break;
                            }
                        }

                        //remove extra characters added after last parameter if we had any params
                        if (operationSegment.Parameters.Any())
                        {
                            resourcesPath.Remove(resourcesPath.Length - 2, 2);
                        }

                        //closing bracket
                        resourcesPath.Append(")");
                        break;

                    default:
                        //its most likely just a resource so append it
                        resourcesPath.Append("." + UppercaseFirstLetter(item.Identifier));
                        break;
                }
            }

            return resourcesPath.ToString();
        }

        /// <summary>
        /// Helper function to make the first character of a string to be capitalized
        /// </summary>
        /// <param name="s">Input string to modified</param>
        /// <returns>Modified string</returns>
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
