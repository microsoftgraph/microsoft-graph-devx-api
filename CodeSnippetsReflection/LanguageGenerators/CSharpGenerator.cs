using Microsoft.OData.UriParser;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace CodeSnippetsReflection.LanguageGenerators
{
    public static class CSharpGenerator
    {
        /// <summary>
        /// Formulates the requested Graph snippets and returns it as string for Csharp
        /// </summary>
        /// <param name="snippetModel">Model of the Snippets info <see cref="SnippetModel"/></param>
        /// <param name="languageExpressions">The language expressions to be used for code Gen</param>
        /// <returns>String of the snippet in Csharp code</returns>
        public static string GenerateCodeSnippet(SnippetModel snippetModel, LanguageExpressions languageExpressions)
        {
            var snippetBuilder = new StringBuilder();

            try
            {
                snippetBuilder.Append("GraphServiceClient graphClient = new GraphServiceClient();\n");

                if (snippetModel.Method == HttpMethod.Get)
                {
                    snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = await graphClient");
                    //Generate the Resources path for Csharp
                    snippetBuilder.Append(CSharpGenerateResourcesPath(snippetModel));
                    snippetBuilder.Append("\n\t.Request()");
                    snippetBuilder.Append(CommonGenerator.GenerateQuerySection(snippetModel, languageExpressions));
                    //Append footers
                    snippetBuilder.Append("\n\t.GetAsync();");

                    return snippetBuilder.ToString();
                }
                else if (snippetModel.Method == HttpMethod.Post)
                {
                    //init the object in the post
                    snippetBuilder.Append(CSharpConstructorGenerator(snippetModel.RequestBody));

                    snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = await graphClient");
                    //Generate the Resources path for Csharp
                    snippetBuilder.Append(CSharpGenerateResourcesPath(snippetModel));
                    snippetBuilder.Append("\n\t.Request()");
                    //Append footers
                    snippetBuilder.Append("\n\t.PostAsync();");
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
        private static string CSharpGenerateResourcesPath(SnippetModel snippetModel)
        {
            var resourcesPath = new StringBuilder();
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
        /// Language agnostic function to generate Object contructor section of a code snippet 
        /// </summary>
        public static string CSharpConstructorGenerator(string jsonBody)
        {
            dynamic testObj = JsonConvert.DeserializeObject(jsonBody);
            StringBuilder stringBuilder = new StringBuilder();
            //new lines :)
            stringBuilder.Append("\r\n{\r\n");

            foreach (var item in testObj)
            {
                string value = JsonConvert.SerializeObject(item.Value);
                if (item.Value.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    value = "{" + value.Replace("[", "").Replace("]", "") + "}";
                }
                stringBuilder.Append("\t" + item.Name + " = " + value.Replace("\n", "").Replace("\r", "") + ",\r\n");
            }
            //closing statement
            stringBuilder.Append("};");
            //new lines :)
            stringBuilder.Append("\r\n\r\n");
            return stringBuilder.ToString();
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

    internal class CSharpExpressions : LanguageExpressions
    {
        public override string FilterExpression => "\n\t.Filter(\"{0}\")"; 
        public override string SearchExpression => "\n\t.Search(\"{0}\")"; 
        public override string ExpandExpression => "\n\t.Expand(\"{0}\")"; 
        public override string SelectExpression => "\n\t.Select(\"{0}\")"; 
        public override string OrderByExpression => "\n\t.OrderBy(\"{0}\")"; 
        public override string SkipExpression => "\n\t.Skip(\"{0}\")"; 
        public override string SkipTokenExpression => "\n\t.SkipToken(\"{0}\")"; 
        public override string TopExpression => "\n\t.Top(\"{0}\")";
        public override string FilterExpressionDelimiter => ",";
        public override string ExpandExpressionDelimiter => ",";
        public override string SelectExpressionDelimiter => ",";
        public override string OrderByExpressionDelimiter => " ";
        public override string HeaderExpression => "\n\t.Header(\"{0}\",\"{1}\")";
    }
}
