using Microsoft.OData.UriParser;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

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
                snippetBuilder.Append("GraphServiceClient graphClient = new GraphServiceClient();\r\n\r\n");

                if (snippetModel.Method == HttpMethod.Get)
                {
                    snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = await graphClient");
                    //Generate the Resources path for Csharp
                    snippetBuilder.Append(CSharpGenerateResourcesPath(snippetModel));
                    snippetBuilder.Append("\n\t.Request()");
                    snippetBuilder.Append(CommonGenerator.GenerateQuerySection(snippetModel, languageExpressions));
                    //Append footers
                    snippetBuilder.Append("\n\t.GetAsync();");

                }
                else if (snippetModel.Method == HttpMethod.Post)
                {
                    switch (snippetModel.ODataUri.Path.LastSegment)
                    {
                        case NavigationPropertySegment _:
                        case EntitySetSegment _:
                        
                            var name = snippetModel.ResponseVariableName.Substring(0, snippetModel.ResponseVariableName.Length - 1);

                            snippetBuilder.Append(CSharpConstructorGenerator(snippetModel.RequestBody, name));
                            snippetBuilder.Append("await graphClient");
                        
                            //Generate the Resources path for Csharp
                            snippetBuilder.Append(CSharpGenerateResourcesPath(snippetModel));
                            snippetBuilder.Append("\n\t.Request()");
                            //Append footers
                            snippetBuilder.Append($"\n\t.AddAsync({name});");
                            break;

                        case OperationSegment operationSegment:
                            //deserialize the object as the top level contains the list of parameter objects
                            if (JsonConvert.DeserializeObject(snippetModel.RequestBody) is JObject testObj)
                            {
                                foreach (var (key, jToken) in testObj)
                                {
                                    var value = JsonConvert.SerializeObject(jToken);
                                    snippetBuilder.Append(CSharpConstructorGenerator(value, key));
                                }
                            }

                            
                            snippetBuilder.Append("await graphClient");

                            //Generate the Resources path for Csharp
                            snippetBuilder.Append(CSharpGenerateResourcesPath(snippetModel));
                            //Append footers
                            snippetBuilder.Append("\n\t.Request()");
                            snippetBuilder.Append("\n\t.PostAsync()");
                            break;
                    }
                }
                else
                {
                    throw new NotImplementedException("HTTP method not implemented for C#");
                }

                return snippetBuilder.ToString();
                
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
                        if (snippetModel.Method == HttpMethod.Post)
                        {
                            //read parameters from request body
                            var parameters = operationSegment.Operations.First().Parameters;
                            var paramList = "";
                            foreach (var parameter in parameters)
                            {
                                if ((parameter.Name.ToLower().Equals("bindingparameter")) || (parameter.Name.ToLower().Equals("bindparameter")))
                                    continue;
                                paramList = paramList + "," + LowerCaseFirstLetter(parameter.Name);
                            }
                            paramList = paramList.Substring(1);
                            resourcesPath.Append($"\n\t.{operationSegment.Identifier}({paramList});");
                        }
                        else
                        {
                            //read parameters from url
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
                        }
                        
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
        /// Csharp function to generate Object constructor section of a code snippet 
        /// </summary>
        public static string CSharpConstructorGenerator(string jsonBody , string name)
        {
            var stringBuilder = new StringBuilder();
            var testObj = JsonConvert.DeserializeObject(jsonBody);

            switch (testObj)
            {
                case string _:
                    stringBuilder.Append("var " + name + " = \"" + testObj + "\";");
                    break;
                case JObject jObject:
                    stringBuilder.Append("var " + name + " = new " + UppercaseFirstLetter(name));
                    //new lines with opening curly brace
                    stringBuilder.Append("\r\n{\r\n");

                    foreach (var (key, jToken) in jObject)
                    {
                        var value = JsonConvert.SerializeObject(jToken);
                        switch (jToken.Type)
                        {
                            case JTokenType.Array:
                            case JTokenType.Object:
                                //new object needs to be constructed so call this function recursively to make it and append it before the current snippet
                                var newObject = CSharpConstructorGenerator(value,key);
                                stringBuilder.Append("\t" + UppercaseFirstLetter(key) + " = " + key + ",\r\n");
                                stringBuilder.Insert(0, newObject);
                                break;
                            default:
                                stringBuilder.Append("\t" + UppercaseFirstLetter(key) + " = " + value.Replace("\n", "").Replace("\r", "") + ",\r\n");
                                break;
                        }
                    }
                    //closing statement
                    stringBuilder.Append("};");
                    break;
                case JArray array:
                    stringBuilder.Append($"var {name} = new List<{UppercaseFirstLetter(name)}>();\r\n");
                    var objectList = array.Children<JObject>();
                    if (objectList.Any())
                    {
                        foreach (var item in objectList)
                        {
                            foreach (var className in item.Properties())
                            {
                                stringBuilder.Append($"{name}.Add(new {UppercaseFirstLetter(className.Name)}());\r\n");
                                //TODO resolve the JSON printed out here
                                //stringBuilder.Append($"{name}.Add(new {UppercaseFirstLetter(className.Name)}({className.Value}));\r\n");
                            }
                        }
                    }
                    else
                    {
                        //TODO resolve the JSON printed out here
                        stringBuilder.Append($"{name}.Add({array});");
                    }

                    break;
                default:
                    stringBuilder.Append("var " + name + " = " + testObj + ";");
                    break;

            }
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
            var a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        /// <summary>
        /// Helper function to make the first character of a string to be small letter
        /// </summary>
        /// <param name="s">Input string to modified</param>
        /// <returns>Modified string</returns>
        private static string LowerCaseFirstLetter(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            var a = s.ToCharArray();
            a[0] = char.ToLower(a[0]);
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
