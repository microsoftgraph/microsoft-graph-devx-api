using Microsoft.OData.UriParser;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CodeSnippetsReflection.Test")]
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
                snippetBuilder.Append("GraphServiceClient graphClient = new GraphServiceClient( authProvider );\r\n\r\n");

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
                    var segment = snippetModel.Segments.Last();

                    switch (segment)
                    {
                        case NavigationPropertySegment _:
                        case EntitySetSegment _:
                            if (!string.IsNullOrEmpty(snippetModel.RequestBody))
                            {
                                snippetBuilder.Append(CSharpGenerateObjectFromJson(segment, snippetModel.RequestBody, new List<string> { snippetModel.ResponseVariableName }));

                                snippetBuilder.Append("await graphClient");
                                //Generate the Resources path for Csharp
                                snippetBuilder.Append(CSharpGenerateResourcesPath(snippetModel));

                                snippetBuilder.Append("\n\t.Request()");
                                //Append footers
                                snippetBuilder.Append($"\n\t.AddAsync({snippetModel.ResponseVariableName});");
                            }
                            else
                            {
                                throw new Exception($"No request Body present for POST of entity {snippetModel.ResponseVariableName}");
                            }
                            break;

                        case OperationSegment _:
                            //deserialize the object since the json top level contains the list of parameter objects
                            if (JsonConvert.DeserializeObject(snippetModel.RequestBody) is JObject testObj)
                            {
                                foreach (var (key, jToken) in testObj)
                                {
                                    var jsonString = JsonConvert.SerializeObject(jToken);
                                    snippetBuilder.Append(CSharpGenerateObjectFromJson(segment, jsonString, new List<string> { key }));
                                }
                            }
                            
                            snippetBuilder.Append("await graphClient");
                            //Generate the Resources path for Csharp
                            snippetBuilder.Append(CSharpGenerateResourcesPath(snippetModel));
                            
                            //Append footers
                            snippetBuilder.Append("\n\t.Request()");
                            snippetBuilder.Append("\n\t.PostAsync()");
                            break;
                        default:
                            throw new Exception("Unknown Segment Type in URI");
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
                        resourcesPath.Append($"[\"{keySegment.Keys.FirstOrDefault().Value}\"]");
                        break;

                    //handle functions/actions and any parameters present into collections
                    case OperationSegment operationSegment:
                        if (snippetModel.Method == HttpMethod.Post)
                        {
                            //read parameters from request body
                            var paramList = new List<string>();
                            foreach (var parameter in operationSegment.Operations.First().Parameters)
                            {
                                if ((parameter.Name.ToLower().Equals("bindingparameter")) || (parameter.Name.ToLower().Equals("bindparameter")))
                                    continue;
                                paramList.Add(LowerCaseFirstLetter(parameter.Name));
                            }
                            
                            resourcesPath.Append($"\n\t.{UppercaseFirstLetter(operationSegment.Identifier)}({CommonGenerator.GetListAsStringForSnippet(paramList,",")})");
                        }
                        else
                        {
                            var paramList = new List<string>();
                            foreach (var parameter in operationSegment.Parameters)
                            {
                                switch (parameter.Value)
                                {
                                    case ConvertNode convertNode:
                                    {
                                        if (convertNode.Source is ConstantNode constantNode)
                                        {
                                            paramList.Add(constantNode.LiteralText);
                                        }
                                        break;
                                    }
                                    case ConstantNode constantNode:
                                        paramList.Add(constantNode.LiteralText);
                                        break;
                                }
                            }
                            //read parameters from url
                            //opening section
                            resourcesPath.Append($".{UppercaseFirstLetter(operationSegment.Identifier)}({CommonGenerator.GetListAsStringForSnippet(paramList,",")})");
                        }
                        break;

                    default:
                        //its most likely just a resource so append it
                        resourcesPath.Append($".{UppercaseFirstLetter(item.Identifier)}");
                        break;
                }
            }

            return resourcesPath.ToString();
        }

        /// <summary>
        /// Csharp function to generate Object constructor section of a code snippet. In the event that another object is needed in the middle of generation,
        /// a recursive call is made to sort out the needed object.
        /// </summary>
        /// <param name="pathSegment">Odata Function/Entity from which the object is needed</param>
        /// <param name="jsonBody">Json string from which the information of the object to be initialized is held</param>
        /// <param name="path">List of strings/identifier showing the path through the Edm/json structure to reach the Class Identifier from the segment</param>
        private static string CSharpGenerateObjectFromJson(ODataPathSegment pathSegment, string jsonBody , ICollection<string> path )
        {
            var stringBuilder = new StringBuilder();
            var variableName = path.Last();
            var jsonObject = JsonConvert.DeserializeObject(jsonBody);
            var className = CommonGenerator.GetClassNameFromIdentifier(pathSegment, path);

            //we need to split the string and get last item and capitalize it in accordance the first character
            //eg microsoft.graph.data => Data
            className = UppercaseFirstLetter( className.Split(".").Last());

            switch (jsonObject)
            {
                case string _:
                    stringBuilder.Append($"var {variableName} = \"{jsonObject}\";\r\n");
                    break;
                case JObject jObject:
                    stringBuilder.Append($"var {variableName} = new {className}\r\n");
                    stringBuilder.Append("{\r\n");//opening curly brace
                    //initialize each member of the class
                    foreach (var (key, jToken) in jObject)
                    {
                        var value = JsonConvert.SerializeObject(jToken);
                        switch (jToken.Type)
                        {
                            case JTokenType.Array:
                            case JTokenType.Object:
                                //we need to create a new object make sure variable names are unique
                                stringBuilder = EnsureVariableNameIsUnique(stringBuilder,key);
                                //new nested object needs to be constructed so call this function recursively to make it and append it before the current snippet
                                var newPath = path.Append(key).ToList();
                                //append this at the start since it needs to be declared before this object is constructed
                                var newObject = CSharpGenerateObjectFromJson(pathSegment, value, newPath);
                                stringBuilder.Insert(0, newObject);
                                //append the usage of declared variable
                                stringBuilder.Append($"\t{UppercaseFirstLetter(key)} = {key},\r\n");
                                break;
                            default:
                                stringBuilder.Append($"\t{UppercaseFirstLetter(key)} = { value.Replace("\n", "").Replace("\r", "") },\r\n");
                                break;
                        }
                    }
                    //closing brace
                    stringBuilder.Append("};\r\n");
                    break;
                case JArray array:
                    //Item is a list/array so declare a typed list
                    stringBuilder.Append($"var {variableName} = new List<{className}>();\r\n");
                    var objectList = array.Children<JObject>();
                    if (objectList.Any())
                    {
                        foreach (var item in objectList)
                        {
                            var paramList = new List<string>();
                            //Add each object item to the list
                            foreach (var (key, jToken) in item)
                            {
                                //we need to create a new object make sure variable names are unique   
                                stringBuilder = EnsureVariableNameIsUnique(stringBuilder, key );
                                //nested object needs to be constructed so call this function recursively to make it and append it before the current snippet
                                var newPath = path.Append(key).ToList();
                                //create the new object and place it at the start
                                var jsonString = JsonConvert.SerializeObject(jToken);
                                var newObject = CSharpGenerateObjectFromJson(pathSegment, jsonString, newPath);
                                stringBuilder.Insert(0, newObject);
                                paramList.Add(key);
                            }

                            //append nested object to the list
                            stringBuilder.Append($"{variableName}.Add(new {className}({CommonGenerator.GetListAsStringForSnippet(paramList,",")}));\r\n");
                        }
                    }
                    break;
                case null:
                    //do nothing
                    break;
                default:
                    //item is a primitive print as is
                    stringBuilder.Append($"var {variableName} = {jsonObject};\r\n");
                    break;
            }

            //add a blank line
            stringBuilder.Append("\r\n");
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Helper function to make check and ensure that a variable name has not been used in declaring another instance variable.
        /// If variable name exists, return string with modified name by appending "Var"
        /// </summary>
        /// <param name="stringBuilder">String builder instance to check for unique declaration</param>
        /// <param name="variableName">variable name to check for uniqueness</param>
        /// <returns>Modified string builder instance</returns>
        private static StringBuilder EnsureVariableNameIsUnique(StringBuilder stringBuilder, string variableName)
        {
            if (stringBuilder.ToString().Contains($"var {variableName} = "))
            {
                stringBuilder.Replace(variableName, variableName + "Var");
            }
            return stringBuilder;
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
        public override string SkipExpression => "\n\t.Skip({0})"; 
        public override string SkipTokenExpression => "\n\t.SkipToken(\"{0}\")"; 
        public override string TopExpression => "\n\t.Top({0})";
        public override string FilterExpressionDelimiter => ",";
        public override string ExpandExpressionDelimiter => ",";
        public override string SelectExpressionDelimiter => ",";
        public override string OrderByExpressionDelimiter => " ";
        public override string HeaderExpression => "\n\t.Header(\"{0}\",\"{1}\")";
    }
}
