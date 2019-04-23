using Microsoft.OData.UriParser;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.OData.Edm;

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
                var segment = snippetModel.Segments.Last();
                if (snippetModel.Method == HttpMethod.Get)
                {
                    var extraSnippet = "";
                    if (segment is PropertySegment)
                    { 
                        extraSnippet = GeneratePropertySectionSnippet(snippetModel);
                    }
                    snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = ");
                    //Csharp properties are uppercase so replace with list with uppercase version
                    snippetModel.SelectFieldList = snippetModel.SelectFieldList.Select(x => UppercaseFirstLetter(x)).ToList();
                    var actions = CommonGenerator.GenerateQuerySection(snippetModel, languageExpressions) +"\n\t.GetAsync();";
                    snippetBuilder.Append(GenerateRequestSection(snippetModel, actions));
                    snippetBuilder.Append(extraSnippet);

                }
                else if (snippetModel.Method == HttpMethod.Post)
                {
                    switch (segment)
                    {
                        case NavigationPropertySegment _:
                        case EntitySetSegment _:
                            if (string.IsNullOrEmpty(snippetModel.RequestBody))
                                throw new Exception($"No request Body present for POST of entity {snippetModel.ResponseVariableName}");

                            snippetBuilder.Append(CSharpGenerateObjectFromJson(segment, snippetModel.RequestBody, new List<string> { snippetModel.ResponseVariableName }));
                            snippetBuilder.Append(GenerateRequestSection(snippetModel, $"\n\t.AddAsync({snippetModel.ResponseVariableName});"));

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
                            
                            snippetBuilder.Append(GenerateRequestSection(snippetModel, "\n\t.PostAsync()"));
                            break;
                        default:
                            throw new Exception("Unknown Segment Type in URI for method POST");
                    }
                }
                else if (snippetModel.Method == HttpMethod.Patch)
                {
                    if (string.IsNullOrEmpty(snippetModel.RequestBody))
                        throw new Exception($"No request Body present for Patch of entity {snippetModel.ResponseVariableName}");
                    
                    snippetBuilder.Append(CSharpGenerateObjectFromJson(segment, snippetModel.RequestBody, new List<string> { snippetModel.ResponseVariableName }));
                   
                    if (segment is PropertySegment)
                    {
                        snippetBuilder.Append(GeneratePropertySectionSnippet(snippetModel));
                    }

                    snippetBuilder.Append(GenerateRequestSection(snippetModel,$"\n\t.UpdateAsync({snippetModel.ResponseVariableName});"));
                }
                else if(snippetModel.Method == HttpMethod.Delete)
                {
                    snippetBuilder.Append(GenerateRequestSection(snippetModel, "\n\t.DeleteAsync();"));
                }
                else if (snippetModel.Method == HttpMethod.Put)
                {
                    if (string.IsNullOrEmpty(snippetModel.RequestBody))
                        throw new Exception($"No request Body present for PUT of entity {snippetModel.ResponseVariableName}");

                    if (snippetModel.ContentType.Equals("application/json"))
                    {
                        snippetBuilder.Append(CSharpGenerateObjectFromJson(segment, snippetModel.RequestBody, new List<string> { snippetModel.ResponseVariableName }));
                    }
                    else
                    {
                        snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = \"{snippetModel.RequestBody.Trim()}\"\n\n");
                    }

                    snippetBuilder.Append(GenerateRequestSection(snippetModel, $"\n\t.PutAsync({snippetModel.ResponseVariableName});"));

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
                                //append the suffix List if its a collection
                                paramList.Add(parameter.Type.Definition is IEdmCollectionType
                                    ? LowerCaseFirstLetter(parameter.Name + "List")
                                    : LowerCaseFirstLetter(parameter.Name));
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
                    case ValueSegment _:
                        resourcesPath.Append(".Content");
                        break;
                    case PropertySegment _:
                        //dont append anything since this is not accessed directly in C#
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

            switch (jsonObject)
            {
                case string _:
                    stringBuilder.Append($"var {variableName} = \"{jsonObject}\";\r\n");
                    break;
                case JObject jObject:
                    {
                        var className = GetCsharpClassName(pathSegment,path);
                        stringBuilder.Append($"var {variableName} = new {className}\r\n");
                        stringBuilder.Append("{\r\n");//opening curly brace
                        //initialize each member/property of the object
                        foreach (var (key, jToken) in jObject)
                        {
                            var value = JsonConvert.SerializeObject(jToken);
                            var newPath = path.Append(key).ToList();//add new identifier to the path
                            if (key.Contains("@odata"))
                            {
                                continue;
                            }
                            switch (jToken.Type)
                            {
                                case JTokenType.Array:
                                case JTokenType.Object:
                                    //we need to create a new object make sure variable names are unique
                                    stringBuilder = EnsureVariableNameIsUnique(stringBuilder, key);
                                    //new nested object needs to be constructed so call this function recursively to make it and append it before the current snippet
                                    var newObject = CSharpGenerateObjectFromJson(pathSegment, value, newPath);
                                    //append this at the start since it needs to be declared before this object is constructed
                                    stringBuilder.Insert(0, newObject);
                                    //append the usage of declared variable depending on whether its an array or simple object
                                    stringBuilder.Append(jToken.Type == JTokenType.Object
                                        ? $"\t{UppercaseFirstLetter(key)} = {key},\r\n"
                                        : $"\t{UppercaseFirstLetter(key)} = {key}List,\r\n");
                                    break;
                                case JTokenType.String:
                                    var nestedEdmType = CommonGenerator.GetEdmTypeFromIdentifier(pathSegment, newPath);
                                    //check if the type is an enum and handle it
                                    if (nestedEdmType is IEdmEnumType edmEnumType)
                                    {
                                        var typeName = GetCsharpClassName(pathSegment, newPath);
                                        var enumName = UppercaseFirstLetter(edmEnumType.Members.First().Name);//default to first member incase serach fails
                                        //look for the proper name of the enum in the members
                                        foreach (var member in edmEnumType.Members)
                                        {
                                            if (member.Name.Equals(jToken.Value<string>(), StringComparison.OrdinalIgnoreCase))
                                            {
                                                enumName = UppercaseFirstLetter(member.Name);
                                            }
                                        }
                                        //Enum is accessed as the Classname then enum type e.g Importance.Low
                                        stringBuilder.Append($"\t{UppercaseFirstLetter(key)} = { typeName }.{enumName},\r\n");
                                    }
                                    else
                                    {
                                        //its just a normal string. Declare as is
                                        stringBuilder.Append($"\t{UppercaseFirstLetter(key)} = { value.Replace("\n", "").Replace("\r", "") },\r\n");
                                    }
                                    break;
                                default:
                                    stringBuilder.Append($"\t{UppercaseFirstLetter(key)} = { value.Replace("\n", "").Replace("\r", "") },\r\n");
                                    break;
                            }
                        }
                        //closing brace
                        stringBuilder.Append("};\r\n");
                    }
                    break;
                case JArray array:
                    {
                        var className = GetCsharpClassName(pathSegment , path);
                        //Item is a list/array so declare a typed list
                        stringBuilder.Append($"var {variableName}List = new List<{className}>();\r\n");
                        var objectList = array.Children<JObject>();
                        if (objectList.Any())
                        {
                            foreach (var item in objectList)
                            {
                                //append nested object to the list
                                var jsonString = JsonConvert.SerializeObject(item);
                                stringBuilder = EnsureVariableNameIsUnique(stringBuilder, path.Last());
                                //we need to create a new object
                                var objectItem = CSharpGenerateObjectFromJson(pathSegment, jsonString, path);
                                //prepend the new object created before this declaration
                                stringBuilder.Insert(0, objectItem);
                                //add declared object to the list
                                stringBuilder.Append($"{variableName}List.Add( {path.Last()} );\r\n");
                            }
                        }
                        else
                        {
                            //its not nested objects but a string collection
                            foreach (var element in array)
                            {
                                stringBuilder.Append($"{variableName}List.Add( \"{element.Value<string>()}\" );\r\n");
                            }
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
        /// Generate the snippet section that makes the call to the api
        /// </summary>
        /// <param name="snippetModel">Snippet model built from the request</param>
        /// <param name="actions">String of actions to be done inside the code block</param>
        private static string GenerateRequestSection(SnippetModel snippetModel, string actions)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append("await graphClient");
            //Generate the Resources path for Csharp
            stringBuilder.Append(CSharpGenerateResourcesPath(snippetModel));
            stringBuilder.Append("\n\t.Request()");
            //Append footers
            stringBuilder.Append(actions);

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Return string representation of the classname for CSharp
        /// </summary>
        /// <param name="pathSegment">The OdataPathSegment in use</param>
        /// <param name="path">Path to follow to get find the classname</param>
        /// <returns>String representing the type in use</returns>
        private static string GetCsharpClassName(ODataPathSegment pathSegment, ICollection<string> path)
        {
            var edmType = CommonGenerator.GetEdmTypeFromIdentifier(pathSegment, path);
            //we need to split the string and get last item
            //eg microsoft.graph.data => Data
            return UppercaseFirstLetter( edmType.ToString().Split(".").Last() );
        }

        /// <summary>
        /// Generate the snippet section for the Property segment for CSharp code as this section cannot be directly accessed in \
        /// a URL fashion
        /// </summary>
        /// <param name="snippetModel">Snippet model built from the request</param>
        private static string GeneratePropertySectionSnippet(SnippetModel snippetModel)
        {
            if (snippetModel.Segments.Count < 2)
                return "";

            var stringBuilder = new StringBuilder();
            var desiredSegment = snippetModel.Segments.Last();
            var segmentIndex = snippetModel.Segments.Count - 1;

            //move back to find first segment that is not a PropertySegment
            while ((desiredSegment is PropertySegment) && (segmentIndex > 0))
            {
                desiredSegment = snippetModel.Segments[--segmentIndex];
            }

            var selectField = snippetModel.Segments[segmentIndex+1].Identifier;

            //generate path to the property
            var properties = "";
            while (segmentIndex < snippetModel.Segments.Count - 1)
            {
                properties = properties + "." + UppercaseFirstLetter(snippetModel.Segments[++segmentIndex].Identifier);
            }

            //modify the responseVarible name
            snippetModel.ResponseVariableName = desiredSegment.Identifier;
            var variableName = snippetModel.Segments.Last().Identifier;
            var parentClassName = GetCsharpClassName(desiredSegment, new List<string> { snippetModel.ResponseVariableName });

            
            if (snippetModel.Method == HttpMethod.Get)
            {
                //we are retrieving the value
                snippetModel.SelectFieldList.Add(selectField);
                stringBuilder.Append($"\n\nvar {variableName} = {snippetModel.ResponseVariableName}{properties};");
            }
            else
            {
                //we are modifying the value
                stringBuilder.Append($"var {snippetModel.ResponseVariableName} = new {parentClassName}();");//initialise the classname
                stringBuilder.Append($"\n{snippetModel.ResponseVariableName}{properties} = {variableName};\n\n");
            }

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
                stringBuilder.Replace(variableName+" ", "_" + variableName+" " );
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
        public override string SelectExpression => "\n\t.Select( e => new {{\n\t\t\t e.{0} \n\t\t\t }})"; 
        public override string OrderByExpression => "\n\t.OrderBy(\"{0}\")"; 
        public override string SkipExpression => "\n\t.Skip({0})"; 
        public override string SkipTokenExpression => "\n\t.SkipToken(\"{0}\")"; 
        public override string TopExpression => "\n\t.Top({0})";
        public override string FilterExpressionDelimiter => ",";
        public override string SelectExpressionDelimiter => ",\n\t\t\t e.";
        public override string OrderByExpressionDelimiter => " ";
        public override string HeaderExpression => "\n\t.Header(\"{0}\",\"{1}\")";
    }
}
