﻿using Microsoft.OData.UriParser;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Microsoft.OData.Edm;
using CodeSnippetsReflection.OData.TypeProperties;

namespace CodeSnippetsReflection.OData.LanguageGenerators
{
    public class CSharpGenerator
    {
        /// <summary>
        /// CommonGenerator instance
        /// </summary>
        private readonly CommonGenerator CommonGenerator;

        /// <summary>
        /// Represents the types that need a trailing ? to make it nullable.
        /// The other Edm types have the corresponding types that are nullable by default:
        ///     such as String and Stream (System namespace)
        ///     or Duration, Date, TimeOfDay (Microsoft.Graph namespace)
        /// </summary>
        private static readonly HashSet<string> EdmTypesNonNullableByDefault = new() { "Int32", "Single", "Double", "Boolean", "Guid", "DateTimeOffset", "Byte" };

        /// <summary>
        /// Determines whether the snippet generation is running through the command line interface
        /// (as opposed to in DevX HTTP API)
        /// </summary>
        private readonly bool IsCommandLine;

        /// <summary>
        /// CSharpGenerator constructor
        /// </summary>
        /// <param name="model">Model representing metadata</param>
        public CSharpGenerator(IEdmModel model, bool isCommandLine = false)
        {
            IsCommandLine = isCommandLine;
            CommonGenerator = new CommonGenerator(model);
        }

        /// <summary>
        /// Formulates the requested Graph snippets and returns it as string for Csharp
        /// </summary>
        /// <param name="snippetModel">Model of the Snippets info <see cref="SnippetModel"/></param>
        /// <param name="languageExpressions">The language expressions to be used for code Gen</param>
        /// <returns>String of the snippet in Csharp code</returns>
        public string GenerateCodeSnippet(SnippetModel snippetModel, LanguageExpressions languageExpressions)
        {
            var snippetBuilder = new StringBuilder();

            try
            {
                snippetBuilder.Append("GraphServiceClient graphClient = new GraphServiceClient( authProvider );\r\n\r\n");
                var segment = snippetModel.Segments.Last();
                snippetModel.ResponseVariableName = CommonGenerator.EnsureVariableNameIsNotReserved(snippetModel.ResponseVariableName , languageExpressions);
                var actions = CommonGenerator.GenerateQuerySection(snippetModel, languageExpressions);

                //append any custom queries present
                snippetBuilder.Append(GenerateCustomQuerySection(snippetModel));

                if (snippetModel.Method == HttpMethod.Get)
                {
                    var extraSnippet = "";
                    if (segment is PropertySegment && !segment.EdmType.IsStream())//streams can be sorted out normally
                    {
                        extraSnippet = GeneratePropertySectionSnippet(snippetModel);
                        snippetModel.SelectFieldList = snippetModel.SelectFieldList.Select(CommonGenerator.UppercaseFirstLetter).ToList();
                        actions = CommonGenerator.GenerateQuerySection(snippetModel, languageExpressions);
                    }
                    snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = ");
                    snippetBuilder.Append(GenerateRequestSection(snippetModel, $"{actions}\r\n\t.GetAsync();"));
                    snippetBuilder.Append(extraSnippet);

                }
                else if (snippetModel.Method == HttpMethod.Post)
                {
                    switch (segment)
                    {
                        case NavigationPropertySegment _:
                        case EntitySetSegment _:
                        case NavigationPropertyLinkSegment navigationPropertyLinkSegment when CommonGenerator.CanGetServiceCollectionNavigationPropertyForProperty(navigationPropertyLinkSegment):
                            if (string.IsNullOrEmpty(snippetModel.RequestBody))
                                throw new Exception($"No request Body present for POST of entity {snippetModel.ResponseVariableName}");

                            snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = ");
                            snippetBuilder.Append(CSharpGenerateObjectFromJson(segment, snippetModel.RequestBody, new List<string> { snippetModel.ResponseVariableName }));
                            snippetBuilder.Append(GenerateRequestSection(snippetModel, $"{actions}\r\n\t.AddAsync({snippetModel.ResponseVariableName});"));
                            break;

                        case NavigationPropertyLinkSegment _:
                            if (string.IsNullOrEmpty(snippetModel.RequestBody))
                                throw new InvalidOperationException($"No request Body present for POST of entity {snippetModel.ResponseVariableName}");

                            if (JObject.Parse(snippetModel.RequestBody).TryGetValue("@odata.id",out JToken odataId))
                            {
                                snippetModel.ResponseVariableName += "Reference"; // append suffix
                                snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = new ReferenceRequestBody\n{{\n");
                                snippetBuilder.Append($"\tODataId = \"{odataId}\"\n}};\n\n");
                                snippetBuilder.Append(GenerateRequestSection(snippetModel, $"{actions}\r\n\t.AddAsync({snippetModel.ResponseVariableName});"));
                            }
                            break;

                        case OperationSegment os:
                            //deserialize the object since the json top level contains the list of parameter objects
                            if (JsonConvert.DeserializeObject(snippetModel.RequestBody) is JObject testObj)
                            {
                                foreach (var (key, jToken) in testObj)
                                {
                                    var jsonString = JsonConvert.SerializeObject(jToken);

                                    // see examples in TypeIsInferredFromActionParametersForNonNullableType
                                    // and TypeIsInferredFromActionParametersForNullableType tests.

                                    var parameter = CommonGenerator.LowerCaseFirstLetter(key);

                                    var path = new List<string> { CommonGenerator.LowerCaseFirstLetter(key) };
                                    var value = CSharpGenerateObjectFromJson(segment, jsonString, path);
                                    var typeHintOnTheLeftHandSide = "var";

                                    // If the value is null we can't resolve the type by using var on the left hand side.
                                    // For example, we can't say "var index = null;". That won't be a compilable snippet.
                                    // In these cases, we look for the type of "index" in action parameters.
                                    if (value.Trim() == "null;")
                                    {
                                        var parameterType = os.Operations?
                                            .SingleOrDefault(o => o.Name == os.Identifier)?
                                            .Parameters?
                                            .SingleOrDefault(p => p.Name == parameter)?
                                            .Type;

                                        if (parameterType == null)
                                        {
                                            throw new NotSupportedException("Parameter type from URL is not found in metadata!");
                                        }

                                        if (parameterType.IsNullable)
                                        {
                                            typeHintOnTheLeftHandSide = new CSharpTypeProperties(parameterType.Definition, false).ClassName;
                                            if (EdmTypesNonNullableByDefault.Contains(typeHintOnTheLeftHandSide))
                                            {
                                                typeHintOnTheLeftHandSide += "?";
                                            }
                                        }
                                        else
                                        {
                                            throw new NotSupportedException("Not nullable type is set to null in the sample!");
                                        }
                                    }

                                    snippetBuilder.Append($"{typeHintOnTheLeftHandSide} {parameter} = {value}");
                                }
                            }
                            snippetBuilder.Append(GenerateRequestSection(snippetModel, $"{actions}\r\n\t.PostAsync();"));
                            break;
                        default:
                            throw new Exception("Unknown Segment Type in URI for method POST");
                    }
                }
                else if (snippetModel.Method == HttpMethod.Patch)
                {
                    if (string.IsNullOrEmpty(snippetModel.RequestBody))
                        throw new Exception($"No request Body present for Patch of entity {snippetModel.ResponseVariableName}");

                    snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = ");
                    snippetBuilder.Append(CSharpGenerateObjectFromJson(segment, snippetModel.RequestBody, new List<string> { snippetModel.ResponseVariableName }));

                    if (segment is PropertySegment)
                    {
                        snippetBuilder.Append(GeneratePropertySectionSnippet(snippetModel));
                    }

                    snippetBuilder.Append(GenerateRequestSection(snippetModel, $"{actions}\r\n\t.UpdateAsync({snippetModel.ResponseVariableName});"));
                }
                else if (snippetModel.Method == HttpMethod.Delete)
                {
                    snippetBuilder.Append(GenerateRequestSection(snippetModel, $"{actions}\r\n\t.DeleteAsync();"));
                }
                else if (snippetModel.Method == HttpMethod.Put)
                {
                    if (string.IsNullOrEmpty(snippetModel.RequestBody))
                        throw new Exception($"No request Body present for PUT of entity {snippetModel.ResponseVariableName}");

                    var genericType = string.Empty;
                    var objectToBePut = snippetModel.ResponseVariableName;
                    if (snippetModel.ContentType?.Equals("application/json", StringComparison.OrdinalIgnoreCase) ?? false)
                    {
                        if (snippetModel.Segments.Last() is NavigationPropertyLinkSegment)
                        {
                            // if we are putting reference, we should send id to that object in PutAsync()
                            // and the request body should contain a JSON with @odata.id key
                            var body = JsonConvert.DeserializeObject(snippetModel.RequestBody) as JObject;

                            // HTTP sample is in this format: https://graph.microsoft.com/v1.0/users/{id}
                            // but C# SDK reconstructs the URL from {id}
                            var id = body["@odata.id"].ToString().Split("/").Last();
                            objectToBePut = $"\"{id}\"";
                        }
                        else
                        {
                            snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = ");
                            snippetBuilder.Append(CSharpGenerateObjectFromJson(segment, snippetModel.RequestBody, new List<string> { snippetModel.ResponseVariableName }));
                        }
                    }
                    else
                    {
                        snippetBuilder.Append($"using var {snippetModel.ResponseVariableName} = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(@\"{snippetModel.RequestBody.Replace("\"", "\"\"").Trim()}\"));\r\n\r\n");

                        // resolve type for PutAsync<T>
                        var genericEdmType = snippetModel.Segments[snippetModel.Segments.Count - 2].EdmType;
                        if ((genericEdmType as IEdmEntityType)?.HasStream == false)
                        {
                            genericType = "<" + CommonGenerator.UppercaseFirstLetter(genericEdmType.ToString().Split(".").Last()) + ">";
                        }
                    }

                    snippetBuilder.Append(GenerateRequestSection(snippetModel, $"{actions}\r\n\t.PutAsync{genericType}({objectToBePut});"));

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
        private string CSharpGenerateResourcesPath(SnippetModel snippetModel)
        {
            var resourcesPath = new StringBuilder();
            var resourcesPathSuffix = string.Empty;
            ODataPathSegment previousSegment = null;

            // lets append all resources
            foreach (var item in snippetModel.Segments)
            {
                switch (item)
                {
                    //handle indexing into collections
                    case KeySegment keySegment:
                        resourcesPath.Append($"[\"{GetIDPlaceholder(keySegment, previousSegment)}\"]");
                        break;
                    // handle special case of indexing on a property through extensions, e.g.
                    // IThumbnailSetRequestBuilder has a manually written extension which allows indexing on {size}
                    // https://github.com/microsoftgraph/msgraph-sdk-dotnet/blob/dev/src/Microsoft.Graph/Requests/Extensions/IThumbnailSetRequestBuilderExtensions.cs
                    case DynamicPathSegment pathSegment when pathSegment.Identifier.Contains("{"):
                        resourcesPath.Append($"[\"{GetIDPlaceholder(pathSegment, previousSegment)}\"]");
                        break;
                    //handle functions/actions and any parameters present into collections
                    case OperationSegment operationSegment:
                        var paramList = CommonGenerator.GetParameterListFromOperationSegment(
                            operationSegment, snippetModel, returnEnumTypeIfEnum:true);
                        var parameters = string.Join(",", paramList.Select(x =>
                        {
                            if (x.Contains("'"))
                            {
                                // handle enums, e.g. microsoft.graph.timeZoneStandard'Iana'
                                // do we have other special types that show up in URLs?
                                var split = x.Split("'");
                                var enumType = CommonGenerator.UppercaseFirstLetter(split[0].Split(".").Last()); // TimeZoneStandard
                                var enumValue = CommonGenerator.UppercaseFirstLetter(split[1]);
                                return $"{enumType}.{enumValue}";
                            }
                            else
                            {
                                return x;
                            }
                        }));
                        resourcesPath.Append($"\r\n\t.{CommonGenerator.UppercaseFirstLetter(operationSegment.Identifier)}({parameters})");
                        break;
                    case ValueSegment _:
                        resourcesPath.Append(".Content");
                        break;
                    case PropertySegment propertySegment:
                        if (propertySegment.EdmType.IsStream() //don't append anything that is not a stream since this is not accessed directly in C#
                           && snippetModel.Method != HttpMethod.Patch) // while patching we pass the encapsulating object in the request
                        {
                            resourcesPath.Append($".{CommonGenerator.UppercaseFirstLetter(item.Identifier)}");
                        }
                        break;
                    case ReferenceSegment _:
                        resourcesPath.Append(".Reference");
                        break;
                    case NavigationPropertyLinkSegment _:
                        /*
                         * The ODataURIParser may sometimes not create and add a ReferenceSegment object to the end of
                         * the segments collection in the event that there is a valid NavigationPropertySegment in the
                         * collection. It will replace this NavigationPropertySegment object with a NavigationPropertyLinkSegment
                         * object. Therefore we modify the suffix so that it may be appended to show the Reference section since
                         * the $ref should always be last in a valid Odata URI.
                        */
                        if (snippetModel.Path.Contains("$ref") && !(snippetModel.Segments.Last() is ReferenceSegment))
                        {

                            var nextSegmentIndex = snippetModel.Segments.IndexOf(item) + 1;
                            if (nextSegmentIndex >= snippetModel.Segments.Count)
                                nextSegmentIndex = snippetModel.Segments.Count-1;

                            var nextSegment = snippetModel.Segments[nextSegmentIndex];
                            //check if the next segment is a KeySegment to know if we will be accessing a single entity of a collection.
                            resourcesPathSuffix = (item.EdmType is IEdmCollectionType) && !(nextSegment is KeySegment) ? ".References" : ".Reference";
                        }
                        resourcesPath.Append($".{CommonGenerator.UppercaseFirstLetter(item.Identifier)}");
                        break;
                    case TypeSegment _:
                        break; // type segment is not part of the resource path.
                    default:
                        //its most likely just a resource so append it
                        resourcesPath.Append($".{CommonGenerator.UppercaseFirstLetter(item.Identifier)}");
                        break;
                }

                previousSegment = item;
            }

            if (!string.IsNullOrEmpty(resourcesPathSuffix))
            {
                resourcesPath.Append(resourcesPathSuffix);
            }

            return resourcesPath.ToString();
        }

        /// <summary>
        /// For command line callers: constructs a string {type-id} if previous segment has a type.
        /// For DevX API callers: uses ID from the original http snippet
        /// </summary>
        /// <param name="currentSegment">current odata path segment</param>
        /// <param name="previousSegment">previous odata path segment</param>
        /// <returns>
        /// For command line callers:
        ///   {UNKNOWN-id} if previous segment doesn't have type information
        ///   {segmentType-id} if previous segment's type is segmentType.
        /// For DevX API callers:
        ///   ID value from the given HTTP snippet
        /// </returns>
        private string GetIDPlaceholder(ODataPathSegment currentSegment, ODataPathSegment previousSegment)
        {
            if (IsCommandLine)
            {
                const string unknownId = "{UNKNOWN-id}";
                if (previousSegment is null || previousSegment.EdmType is null)
                {
                    return unknownId;
                }

                var currentType = previousSegment.EdmType.FullTypeName()
                    .Replace("Collection(", string.Empty)           // remove collection wrapper
                    .Replace("microsoft.graph.", string.Empty)      // assumption is that all namespaces are under microsoft.graph
                    .Replace(")", string.Empty);                    // remove collection wrapper

                return $"{{{currentType}-id}}";
            }
            else // HTTP API calls should preserve the ID from original snippet
            {
                return currentSegment switch
                {
                    KeySegment keySegment => keySegment.Keys.FirstOrDefault().Value.ToString(),
                    DynamicPathSegment pathSegment => pathSegment.Identifier,
                    _ => throw new ArgumentException("currentSegment is expected to be either a key or dynamic path segment!", nameof(currentSegment)),
                };
            }
        }

        /// <summary>
        /// Csharp function to generate Object constructor section of a code snippet. In the event that another object is needed in the middle of generation,
        /// a recursive call is made to sort out the needed object.
        /// </summary>
        /// <param name="pathSegment">Odata Function/Entity from which the object is needed</param>
        /// <param name="jsonBody">Json string from which the information of the object to be initialized is held</param>
        /// <param name="path">List of strings/identifier showing the path through the Edm/json structure to reach the Class Identifier from the segment</param>
        private string CSharpGenerateObjectFromJson(ODataPathSegment pathSegment, string jsonBody , ICollection<string> path)
        {
            if (pathSegment is PropertySegment && pathSegment.EdmType?.FullTypeName() == "Edm.Stream")
            {
                // special case where the full request body should be converted into a stream object
                var encodedMultilineString = jsonBody.Replace("\"", "\"\"");
                return $"new System.IO.MemoryStream(Encoding.UTF8.GetBytes(@\"{encodedMultilineString}\"));\r\n\r\n";
            }

            var stringBuilder = new StringBuilder();
            var jsonObject = JsonConvert.DeserializeObject(jsonBody);
            var tabSpace = new string('\t', path.Count -1);//d
            var typeProperties = GetCSharpTypeProperties(pathSegment, path);
            var className = typeProperties.ClassName;
            if (className == "JToken") // handle special case of JToken
            {
                var jToken = jsonObject is string _
                    ? $"JsonDocument.Parse(@\"\"{jsonBody}\"\")"     // parse string differently
                    : $"JsonDocument.Parse(\"{jsonBody}\")";    // complex objects need JToken.Parse()
                stringBuilder.Append($"{jToken}\r\n");
            }
            else if (jsonObject is JObject _ && className == "String") // special case where empty string is represented as {}
            {
                stringBuilder.Append("\"\"\r\n");
            }
            else
            {
                switch (jsonObject)
                {
                    case string _:
                        {
                            var enumString = GenerateEnumString(jsonObject.ToString(), pathSegment, path);
                            if (className == "String")
                            {
                                stringBuilder.Append($"{jsonBody}\r\n");
                            }
                            else if (!string.IsNullOrEmpty(enumString))
                            {
                                //Enum is accessed as the Classname then enum type e.g Importance.Low
                                stringBuilder.Append($"{tabSpace}{enumString}\r\n");
                            }
                            else if (jsonObject.Equals("true") || jsonObject.Equals("false"))
                            {
                                stringBuilder.Append($"{tabSpace}{jsonObject}\r\n");//boolean primitives values masquerading as strings.
                            }
                            else
                            {
                                stringBuilder.Append($"{tabSpace}{GenerateSpecialClassString($"{jsonObject}", pathSegment, path)}");
                            }
                        }
                        break;
                    case JObject jObject:
                        {
                            stringBuilder.Append($"new {className}\r\n");
                            stringBuilder.Append($"{tabSpace}{{\r\n");//opening curly brace

                            var additionalData = new Dictionary<string, string>();

                            //initialize each member/property of the object
                            foreach (var (key, jToken) in jObject)
                            {
                                var value = JsonConvert.SerializeObject(jToken);
                                var newPath = path.Append(key).ToList();//add new identifier to the path

                                if (key.Contains("@odata") || key.StartsWith("@")) // sometimes @odata maybe in the middle e.g."invoiceStatus@odata.type"
                                {
                                    switch (key)
                                    {
                                        case "@odata.id" when className.Equals("DirectoryObject"):
                                        case "@odata.type":
                                            stringBuilder = ProcessOdataIdAndType(stringBuilder, key, jToken.Value<string>(), className, tabSpace);
                                            break;
                                        default:
                                            additionalData.Add(key, value);
                                            break;
                                    }
                                    continue;
                                }

                                // determine if property is found in type
                                // allow arbitrary properties for OpenTypes through AdditionalData section
                                // otherwise throw type not found error
                                try
                                {
                                    var edmType = CommonGenerator.GetEdmTypeFromIdentifier(pathSegment, newPath);
                                }
                                catch
                                {
                                    if (typeProperties.IsOpenType)
                                    {
                                        additionalData.Add(key, value);
                                        continue;
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }

                                // following rename logic is taken from:
                                // https://github.com/microsoftgraph/MSGraph-SDK-Code-Generator/blob/a5dffbf6db77c3830e45530f5c29622e06078b7e/src/GraphODataTemplateWriter/CodeHelpers/CSharp/TypeHelperCSharp.cs#L254
                                var propertyName = CommonGenerator.UppercaseFirstLetter(key);
                                if (propertyName == className) // one such example is list->list
                                {
                                    var propertyClassName = GetCsharpClassName(pathSegment, newPath);
                                    if (propertyName == propertyClassName)
                                    {
                                        propertyName += "Property";
                                    }
                                    else
                                    {
                                        propertyName = propertyClassName;
                                    }
                                }

                                switch (jToken.Type)
                                {
                                    case JTokenType.Array:
                                    case JTokenType.Object:
                                        //new nested object needs to be constructed so call this function recursively to make it
                                        var newObject = CSharpGenerateObjectFromJson(pathSegment, value, newPath);
                                        stringBuilder.Append($"{tabSpace}\t{propertyName} = {newObject}".TrimEnd() + ",\r\n");
                                        break;
                                    default:
                                        // we can call the function recursively to handle the other states of string/enum/special classes
                                        stringBuilder.Append($"{tabSpace}\t{propertyName} = {CSharpGenerateObjectFromJson(pathSegment, value, newPath).Trim()},\r\n");
                                        break;
                                }
                            }

                            // add AdditionalData segment
                            if (additionalData.Count > 0)
                            {
                                // make it readable with the constants below, otherwise it becomes an escape character mess
                                const string quote = "\"";
                                const string escape = "\\";
                                const string tab = "\t";
                                const string openCurly = "{";
                                const string closeCurly = "}";

                                var additionalDataTabSpace = tabSpace + tab;
                                var dataRowTabSpace = tabSpace + tab + tab;

                                // transform a key value pair into an AdditionalData row string
                                string dataRow(KeyValuePair<string, string> kvp)
                                {
                                    string value;
                                    if (kvp.Value.StartsWith($"{quote}")) // simple string
                                    {
                                        value = kvp.Value;
                                    }
                                    else // complex JSON object, needs to be escaped
                                    {
                                        var escapedValue = kvp.Value.Replace(quote, escape + quote);
                                        value = quote + escapedValue + quote;
                                    }

                                    return dataRowTabSpace + openCurly + $"{quote}{kvp.Key}{quote}, " + value + closeCurly; // e.g. {"Quantity", "934"}
                                };

                                var additionalDataRows = string.Join(",\r\n", additionalData.Select(kvp => dataRow(kvp)));

                                stringBuilder.Append($"{additionalDataTabSpace}AdditionalData = new Dictionary<string, object>()\r\n" +
                                                     $"{additionalDataTabSpace}{{\r\n" +
                                                     $"{additionalDataRows}\r\n" +
                                                     $"{additionalDataTabSpace}}}\r\n");
                            }

                            //remove the trailing comma if we appended anything
                            if (stringBuilder[stringBuilder.Length - 3].Equals(','))
                            {
                                stringBuilder.Remove(stringBuilder.Length - 3, 1);
                            }

                            //closing brace
                            stringBuilder.Append($"{tabSpace}}}\r\n");
                        }
                        break;
                    case JArray array:
                        {
                            var objectList = array.Children<JObject>();

                            if (typeProperties.IsNavigationProperty)
                            {
                                stringBuilder.Append($"new {GetCollectionPageClassName(pathSegment, path)}()\r\n{tabSpace}{{\r\n");
                            }
                            else
                            {
                                stringBuilder.Append($"new List<{typeProperties.ClassName}>()\r\n{tabSpace}{{\r\n");
                            }

                            if (objectList.Any())
                            {
                                foreach (var item in objectList)
                                {
                                    var jsonString = JsonConvert.SerializeObject(item);
                                    //we need to create a new object
                                    var objectStringFromJson = CSharpGenerateObjectFromJson(pathSegment, jsonString, path).TrimEnd(";\r\n".ToCharArray());
                                    //indent it one tab level then append it to the string builder
                                    objectStringFromJson = $"{tabSpace}\t{objectStringFromJson.Replace("\r\n", "\r\n\t")}";
                                    stringBuilder.Append($"{objectStringFromJson},\r\n");
                                }
                                stringBuilder.Remove(stringBuilder.Length - 3, 1);//remove the trailing comma
                            }
                            else //we don't have object nested but something else like empty list/strings/enums
                            {
                                foreach (var element in array)
                                {
                                    var listItem = CSharpGenerateObjectFromJson(pathSegment, JsonConvert.SerializeObject(element), path).TrimEnd(";\r\n".ToCharArray());
                                    stringBuilder.Append($"{tabSpace}\t{listItem.TrimStart()},\r\n");
                                }
                                //remove the trailing comma if we appended anything
                                if (stringBuilder[stringBuilder.Length - 3].Equals(','))
                                {
                                    stringBuilder.Remove(stringBuilder.Length - 3, 1);
                                }
                            }
                            stringBuilder.Append($"{tabSpace}}}\r\n");
                        }
                        break;
                    case DateTime _:
                        stringBuilder.Append($"{tabSpace}{GenerateSpecialClassString(jsonBody.Replace("\"", ""), pathSegment, path)}");
                        break;
                    case null:
                        stringBuilder.Append($"{tabSpace}null");
                        break;
                    default:
                        switch (GetCsharpClassName(pathSegment, path))
                        {
                            case "Single": // float
                                stringBuilder.Append($"{tabSpace}{jsonObject}f");
                                break;
                            default:
                                var primitive = jsonObject.ToString();
                                //json deserializer capitalizes the bool types so undo that
                                if (primitive.Equals("True", StringComparison.Ordinal) || primitive.Equals("False", StringComparison.Ordinal))
                                {
                                    primitive = CommonGenerator.LowerCaseFirstLetter(primitive);
                                }
                                //item is a primitive print as is
                                stringBuilder.Append($"{tabSpace}{primitive}\r\n");
                                break;
                        }
                        break;
                }
            }

            //check if this is the outermost object in a potential nested object structure and needs the semicolon termination character.
            return path.Count == 1 ? $"{stringBuilder.ToString().TrimEnd()};\r\n\r\n" : stringBuilder.ToString();
        }

        /// <summary>
        /// Processes @odata.id and @odata.type properties
        /// </summary>
        /// <param name="stringBuilder">The original string builder containing code generated so far</param>
        /// <param name="key">The odata key/property</param>
        /// <param name="value">The value related to the odata key/property</param>
        /// <param name="className">The class name for the entity needing modification</param>
        /// <param name="tabSpace">Tab space to use for formatting the code generated</param>
        /// <returns>a string builder with the relevant odata code added</returns>
        private static StringBuilder ProcessOdataIdAndType(StringBuilder stringBuilder, string key, string value, string className, string tabSpace)
        {
            if(String.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value), "Value for @odata.type cannot be empty");
            }

            switch (key)
            {
                case "@odata.id" when className.Equals("DirectoryObject") :
                    try
                    {
                        var uriLastSegmentString = new Uri(value).Segments.Last();
                        uriLastSegmentString = Uri.UnescapeDataString(uriLastSegmentString);
                        stringBuilder.Append($"{tabSpace}\tId = \"{uriLastSegmentString}\",\r\n");
                    }
                    catch (UriFormatException)
                    {
                        stringBuilder.Append($"{tabSpace}\tId = \"{value}\",\r\n");//its not really a URI
                    }
                    break;

                case "@odata.type":
                    string proposedType;
                    // If type contains subnamespace of Microsoft.Graph e.g Microsoft.Graph.Ediscovery.LegalHold, prefix the namespace to the typeName
                    // else if type not in graph namespace e.g Microsoft.OutlookServices or is a type directly within the graph subnamespace e.g Microsoft.Graph.IdentitySet
                    // then proposedType is the <typeName> without prefix
                    var graphSubspaces = value.StartsWith("#microsoft.graph.") ? value["#microsoft.graph.".Length..].Split(".".ToCharArray()) : new string[] {} ;
                    if (graphSubspaces.Length > 1) {
                        // Remove the prefixed `#`, then uppercase the individual strings in the odata path
                        var namespacedTypeName = value.Split("#").Last().Split(".").Select((item, _) => CommonGenerator.UppercaseFirstLetter(item));
                        proposedType = String.Join(".", namespacedTypeName);
                    }
                    else{
                        proposedType = CommonGenerator.UppercaseFirstLetter(value.Split(".").Last());
                    }
                    proposedType = proposedType.EndsWith("Request") ? proposedType + "Object" : proposedType; // disambiguate class names that end with Request
                    //check if the odata type specified is different
                    // maybe due to the declaration of a subclass of the type specified from the url.
                    if (!className.Equals(proposedType))
                    {
                        stringBuilder.Replace(className, proposedType);
                    }
                    break;

                default:
                    throw new ArgumentException(nameof(key), "Key should be either @odata.id or @odata.type");
            }

            return stringBuilder;
        }

        /// <summary>
        /// Get the Csharp representation of a string and add any parsing calls that may be required.
        /// </summary>
        /// <param name="stringParameter">String parameter that may need parsing</param>
        /// <param name="pathSegment">Odata Function/Entity from which the object is needed</param>
        /// <param name="path">List of strings/identifier showing the path through the Edm/json structure to reach the Class Identifier from the segment</param>
        private string GenerateSpecialClassString(string stringParameter, ODataPathSegment pathSegment, ICollection<string> path)
        {
            string specialClassString = $"\"{stringParameter.Replace("\"", "\\\"")}\"";//double quoted string.
            try
            {
                var className = GetCsharpClassName(pathSegment, path);
                //check the classes and parse them appropriately
                switch (className)
                {
                    case "DateTimeOffset":
                        return $"DateTimeOffset.Parse({specialClassString})";

                    case "Guid":
                        return $"Guid.Parse({specialClassString})";

                    case "Date"://try to parse the date to get the day,month and year params
                        string parsedDate = DateTime.TryParse(stringParameter,out var dateTime)
                            ? $"{dateTime.Year},{dateTime.Month},{dateTime.Day}"
                            : "1900,1,1";//use default params on parse failure
                        return $"new Date({parsedDate})";

                    case "Duration":
                        return $"new Duration({specialClassString})";

                    case "Binary":
                        return $"Convert.FromBase64String({specialClassString})";

                    case "Double":
                        return $"(double){stringParameter}";

                    case "TimeOfDay":
                        return DateTime.TryParse(stringParameter, out var timeOfDay)
                            ? $"new TimeOfDay({timeOfDay.Hour}, {timeOfDay.Minute}, {timeOfDay.Second})"
                            : "new TimeOfDay(0, 0, 0)";

                    case "Boolean":
                        return bool.TryParse(stringParameter, out var boolValue)
                            ? boolValue.ToString().ToLower()
                            : "false";

                    default:
                        return specialClassString;
                }
            }
            catch
            {
                return specialClassString;
            }
        }

        /// <summary>
        /// Generate the snippet section that makes the call to the api
        /// </summary>
        /// <param name="snippetModel">Snippet model built from the request</param>
        /// <param name="actions">String of actions to be done inside the code block</param>
        private string GenerateRequestSection(SnippetModel snippetModel, string actions)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append("await graphClient");
            //Generate the Resources path for Csharp
            stringBuilder.Append(CSharpGenerateResourcesPath(snippetModel));
            //check if there are any custom query options appended
            stringBuilder.Append(snippetModel.CustomQueryOptions.Any() ? "\r\n\t.Request( queryOptions )" : "\r\n\t.Request()");
            //Append footers
            stringBuilder.Append(actions);

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Return type properties such as CSharp class name, open type etc.
        /// </summary>
        /// <param name="pathSegment">The OdataPathSegment in use</param>
        /// <param name="path">Path to follow to get find the classname</param>
        /// <returns>Type properties such as CSharp class name, open type etc.</returns>
        private CSharpTypeProperties GetCSharpTypeProperties(ODataPathSegment pathSegment, ICollection<string> path)
        {
            var (edmType, isNavigationProperty) = CommonGenerator.GetEdmTypeFromIdentifierAndNavigationProperty(pathSegment, path);
            return new CSharpTypeProperties(edmType, isNavigationProperty);
        }

        /// <summary>
        /// Return string representation of the classname for CSharp
        /// </summary>
        /// <param name="pathSegment">The OdataPathSegment in use</param>
        /// <param name="path">Path to follow to get find the classname</param>
        /// <returns>String representing the type in use</returns>
        private string GetCsharpClassName(ODataPathSegment pathSegment, ICollection<string> path)
        {
            return GetCSharpTypeProperties(pathSegment, path).ClassName;
        }

        /// <summary>
        /// Generates CollectionPage and CollectionWithReferencesPage class name
        /// </summary>
        /// <param name="pathSegment">The OdataPathSegment in use</param>
        /// <param name="path">path in edm model</param>
        /// <returns>ICollectionPage Interface name</returns>
        private string GetCollectionPageClassName(ODataPathSegment pathSegment, ICollection<string> path)
        {
            var type = GetCsharpClassName(pathSegment, path.Take(path.Count-1).ToList());
            var property = path.Last();
            var uppercasedProperty = CommonGenerator.UppercaseFirstLetter(property);

            var structuredType = (pathSegment.EdmType as IEdmCollectionType)?.ElementType?.Definition as IEdmStructuredType;
            var containsTarget = (structuredType?.FindProperty(property) as IEdmNavigationProperty)?.ContainsTarget
                ?? throw new InvalidOperationException("Path doesn't match with the metadata property description!" +
                    $"path: { string.Join(' ', path) }");

            var navCollectionType = containsTarget ? "CollectionPage" : "CollectionWithReferencesPage";
            return $"{type}{uppercasedProperty}{navCollectionType}";
        }

        /// <summary>
        /// Generate the snippet section for the Property segment for CSharp code as this section cannot be directly accessed in \
        /// a URL fashion
        /// </summary>
        /// <param name="snippetModel">Snippet model built from the request</param>
        private string GeneratePropertySectionSnippet(SnippetModel snippetModel)
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
                properties = properties + "." + CommonGenerator.UppercaseFirstLetter(snippetModel.Segments[++segmentIndex].Identifier);
            }

            //modify the responseVariable name while registering the object created above.
            var propertyName = snippetModel.ResponseVariableName;
            snippetModel.ResponseVariableName = desiredSegment.Identifier;
            var variableName = snippetModel.Segments.Last().Identifier;
            var parentClassName = GetCsharpClassName(desiredSegment, new List<string> { snippetModel.ResponseVariableName });


            if (snippetModel.Method == HttpMethod.Get)
            {
                //we are retrieving the value
                snippetModel.SelectFieldList.Add(selectField);
                stringBuilder.Append($"\r\n\r\nvar {variableName} = {snippetModel.ResponseVariableName}{properties};");
            }
            else
            {
                //we are modifying the value
                stringBuilder.Append($"var {snippetModel.ResponseVariableName} = new {parentClassName}();");//initialise the classname
                stringBuilder.Append($"\r\n{snippetModel.ResponseVariableName}{properties} = {propertyName};\r\n\r\n");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Generate the snippet section for the Property segment for CSharp code as this section cannot be directly accessed in \
        /// a URL fashion
        /// </summary>
        /// <param name="snippetModel">Snippet model built from the request</param>
        private static string GenerateCustomQuerySection(SnippetModel snippetModel)
        {
            if (!snippetModel.CustomQueryOptions.Any()
            && string.IsNullOrEmpty(snippetModel.ODataUri.SkipToken))
            {
                return string.Empty;//nothing to do here
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("var queryOptions = new List<QueryOption>()\r\n");
            stringBuilder.Append("{\r\n");//opening brace

            foreach (var (key, value) in snippetModel.CustomQueryOptions)
            {
                stringBuilder.Append($"\tnew QueryOption(\"{key}\", \"{value}\"),\r\n");
            }

            //Append any skip token queries
            if (!string.IsNullOrEmpty(snippetModel.ODataUri.SkipToken))
            {
                stringBuilder.Append($"\tnew QueryOption(\"$skiptoken\", \"{snippetModel.ODataUri.SkipToken}\"),\r\n");
            }

            stringBuilder.Remove(stringBuilder.Length - 3, 1);//remove the trailing comma
            stringBuilder.Append("};\r\n\r\n");//closing brace
            //return custom query options section
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Get the Csharp representation of an enum type from a string hint and the odata path segment
        /// </summary>
        /// <param name="enumHint">string representing the hint to use for enum lookup</param>
        /// <param name="pathSegment">Odata Function/Entity from which the object is needed</param>
        /// <param name="path">List of strings/identifier showing the path through the Edm/json structure to reach the Class Identifier from the segment</param>
        private string GenerateEnumString(string enumHint, ODataPathSegment pathSegment, ICollection<string> path)
        {
            IEdmType nestEdmType;

            try
            {
                nestEdmType = CommonGenerator.GetEdmTypeFromIdentifier(pathSegment, path);
            }
            catch (Exception)
            {
                return string.Empty;
            }

            if (nestEdmType is IEdmEnumType edmEnumType)
            {
                var typeName = GetCsharpClassName(pathSegment, path);
                var temp = enumHint.Split(", ".ToCharArray(),StringSplitOptions.RemoveEmptyEntries);//split incase we need to 'or' the members
                var enumStringList = new List<string>();

                //look for the proper name of the enum in the members
                foreach (var member in edmEnumType.Members)
                {
                    if (temp.Contains(member.Name,StringComparer.OrdinalIgnoreCase))
                    {
                        enumStringList.Add($"{typeName}.{CommonGenerator.UppercaseFirstLetter(member.Name)}");
                    }
                }

                //if search failed default to first element
                if (!enumStringList.Any())
                {
                    enumStringList.Add($"{typeName}.{CommonGenerator.UppercaseFirstLetter(edmEnumType.Members.First().Name)}");
                }

                //return the enum type "ORed" together
                return string.Join(" | ", enumStringList);
            }

            return string.Empty;
        }

    }

    internal class CSharpExpressions : LanguageExpressions
    {
        public override string FilterExpression => "\r\n\t.Filter(\"{0}\")";
        public override string SearchExpression => "\r\n\t.Search(\"{0}\")";
        public override string ExpandExpression => "\r\n\t.Expand(\"{0}\")";
        public override string SelectExpression => "\r\n\t.Select(\"{0}\")";
        public override string OrderByExpression => "\r\n\t.OrderBy(\"{0}\")";
        public override string SkipExpression => "\r\n\t.Skip({0})";
        public override string SkipTokenExpression => "";
        public override string TopExpression => "\r\n\t.Top({0})";
        public override string FilterExpressionDelimiter => ",";
        public override string SelectExpressionDelimiter => ",";
        public override string OrderByExpressionDelimiter => " ";
        public override string HeaderExpression => "\r\n\t.Header(\"{0}\",\"{1}\")";

        public override string[] ReservedNames => new string[] {
            "abstract","as","base","bool","break","byte","case","catch","char",
            "checked","class","const","continue","decimal","default","delegate",
            "do","double","else","enum","event","explicit","extern","false",
            "finally","fixed","float","for","foreach","goto","if","implicit","in",
            "int","interface","internal","is","lock","long","namespace","new","null",
            "object","operator","out","override","params","private","protected","public",
            "readonly","ref","return","sbyte","sealed","short","sizeof","stackalloc",
            "static","string","struct","switch","this","throw","true","try","typeof","uint",
            "ulong","unchecked","unsafe","ushort","using","using","static","virtual","void",
            "volatile","while" };

        public override string ReservedNameEscapeSequence => "@";

        public override string DoubleQuotesEscapeSequence => "\\\"";

        public override string SingleQuotesEscapeSequence => "'";
    }
}
