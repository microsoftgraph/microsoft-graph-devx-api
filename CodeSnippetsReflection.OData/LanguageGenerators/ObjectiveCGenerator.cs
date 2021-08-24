using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: InternalsVisibleTo("CodeSnippetsReflection.Test")]
namespace CodeSnippetsReflection.OData.LanguageGenerators
{
    public class ObjectiveCGenerator
    {
        /// <summary>
        /// CommonGenerator instance
        /// </summary>
        private readonly CommonGenerator CommonGenerator;

        /// <summary>
        /// ObjectiveCGenerator constructor
        /// </summary>
        /// <param name="model">Model representing metadata</param>
        public ObjectiveCGenerator(IEdmModel model)
        {
            CommonGenerator = new CommonGenerator(model);
        }

        /// <summary>
        /// Formulates the requested Graph snippets and returns it as string for Objective C
        /// </summary>
        /// <param name="snippetModel">Model of the Snippets info <see cref="SnippetModel"/></param>
        /// <param name="languageExpressions">The language expressions to be used for code Gen</param>
        /// <returns>String of the snippet in Objective C code</returns>
        public string GenerateCodeSnippet(SnippetModel snippetModel, LanguageExpressions languageExpressions)
        {
            StringBuilder snippetBuilder = new StringBuilder();
            var segment = snippetModel.Segments.Last();

            /*Auth provider section*/
            snippetBuilder.Append("MSHTTPClient *httpClient = [MSClientFactory createHTTPClientWithAuthenticationProvider:authenticationProvider];\r\n\r\n");

            /*Request generation section*/
            snippetBuilder.Append($"NSString *MSGraphBaseURL = @\"{snippetModel.ODataUriParser.ServiceRoot}\";\r\n");
            snippetBuilder.Append($"NSMutableURLRequest *urlRequest = [NSMutableURLRequest requestWithURL:[NSURL URLWithString:[MSGraphBaseURL stringByAppendingString:@\"{snippetModel.Path}{snippetModel.QueryString}\"]]];\r\n");
            snippetBuilder.Append($"[urlRequest setHTTPMethod:@\"{snippetModel.Method}\"];\r\n");

            //add header to the Request headers list so it used in the request builder
            if (snippetModel.ContentType != null)
            {
                snippetModel.RequestHeaders = snippetModel.RequestHeaders.Append(new KeyValuePair<string, IEnumerable<string>>("Content-Type", new[] { snippetModel.ContentType }));
            }

            snippetBuilder.Append(CommonGenerator.GenerateQuerySection(snippetModel, languageExpressions));
            snippetBuilder.Append("\r\n");

            //create object to send out now that we have a payload it is not null
            if (!string.IsNullOrEmpty(snippetModel.RequestBody))
            {

                if (segment is OperationSegment)
                {
                    snippetBuilder.Append("NSMutableDictionary *payloadDictionary = [[NSMutableDictionary alloc] init];\r\n\r\n");
                    //deserialize the object since the json top level contains the list of parameter objects
                    if (JsonConvert.DeserializeObject(snippetModel.RequestBody) is JObject testObj)
                    {
                        foreach (var (key, jToken) in testObj)
                        {
                            var jsonString = JsonConvert.SerializeObject(jToken);
                            snippetBuilder.Append(ObjectiveCGenerateObjectFromJson(segment, jsonString, new List<string> { CommonGenerator.LowerCaseFirstLetter(key) }));
                            snippetBuilder.Append(jToken.Type == JTokenType.Array
                                ? $"payloadDictionary[@\"{key}\"] = {CommonGenerator.LowerCaseFirstLetter(key)}List;\r\n\r\n"
                                : $"payloadDictionary[@\"{key}\"] = {CommonGenerator.LowerCaseFirstLetter(key)};\r\n\r\n");
                        }
                    }
                    snippetBuilder.Append("NSData *data = [NSJSONSerialization dataWithJSONObject:payloadDictionary options:kNilOptions error:&error];\r\n");
                    snippetBuilder.Append("[urlRequest setHTTPBody:data];\r\n\r\n");
                }
                else
                {
                    snippetBuilder.Append($"{ObjectiveCGenerateObjectFromJson(snippetModel.Segments.Last(), snippetModel.RequestBody, new List<string> {snippetModel.ResponseVariableName})}\r\n");
                    snippetBuilder.Append("NSError *error;\r\n");
                    snippetBuilder.Append($"NSData *{snippetModel.ResponseVariableName}Data = [{snippetModel.ResponseVariableName} getSerializedDataWithError:&error];\r\n");
                    snippetBuilder.Append($"[urlRequest setHTTPBody:{snippetModel.ResponseVariableName}Data];\r\n\r\n");
                }
            }

            /*Task/Response generation section*/
            snippetBuilder.Append("MSURLSessionDataTask *meDataTask = [httpClient dataTaskWithRequest:urlRequest \r\n");
            snippetBuilder.Append("\tcompletionHandler: ^(NSData *data, NSURLResponse *response, NSError *nserror) {\r\n\r\n");

            //typically a GET request a json payload of the entity coming back
            if (snippetModel.Method == HttpMethod.Get)
            {
                if (segment.EdmType is IEdmCollectionType collectionType)
                {
                    //Get underlying model if it is an entity
                    if (collectionType.ElementType.Definition is IEdmNamedElement edmNamedElement)
                    {
                        snippetModel.ResponseVariableName = edmNamedElement.Name;
                    }
                    snippetBuilder.Append("\t\tNSError *jsonError = nil;\r\n");
                    snippetBuilder.Append("\t\tMSCollection *collection = [[MSCollection alloc] initWithData:data error:&jsonError];\r\n");
                    snippetBuilder.Append($"\t\tMSGraph{CommonGenerator.UppercaseFirstLetter(snippetModel.ResponseVariableName)} *{snippetModel.ResponseVariableName} " +
                                          $"= [[MSGraph{CommonGenerator.UppercaseFirstLetter(snippetModel.ResponseVariableName)} alloc] " +
                                          "initWithDictionary:[[collection value] objectAtIndex: 0] error:&nserror];\r\n\r\n");
                }
                else
                {
                    snippetBuilder.Append($"\t\tMSGraph{CommonGenerator.UppercaseFirstLetter(snippetModel.ResponseVariableName)} *{snippetModel.ResponseVariableName} " +
                                          $"= [[MSGraph{CommonGenerator.UppercaseFirstLetter(snippetModel.ResponseVariableName)} alloc] initWithData:data error:&nserror];\r\n\r\n");
                }
            }
            else
            {
                snippetBuilder.Append("\t\t//Request Completed\r\n\r\n");
            }
            snippetBuilder.Append("}];\r\n\r\n");
            snippetBuilder.Append("[meDataTask execute];");

            return snippetBuilder.ToString();
        }


        /// <summary>
        /// Objective C function to generate Object constructor section of a code snippet. In the event that another object is needed in the middle of generation,
        /// a recursive call is made to sort out the needed object.
        /// </summary>
        /// <param name="pathSegment">Odata Function/Entity from which the object is needed</param>
        /// <param name="jsonBody">Json string from which the information of the object to be initialized is held</param>
        /// <param name="path">List of strings/identifier showing the path through the Edm/json structure to reach the Class Identifier from the segment</param>
        private string ObjectiveCGenerateObjectFromJson(ODataPathSegment pathSegment, string jsonBody, ICollection<string> path)
        {
            var stringBuilder = new StringBuilder();
            var jsonObject = JsonConvert.DeserializeObject(jsonBody);

            switch (jsonObject)
            {
                case JObject jObject:
                    {
                        var className = GetObjectiveCModelName(pathSegment, path);
                        stringBuilder.Append($"{className} *{path.Last()} = [[{className} alloc] init];\r\n");
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
                                    //new nested object needs to be constructed so call this function recursively to make it
                                    var newObject = ObjectiveCGenerateObjectFromJson(pathSegment, value, newPath).TrimEnd();
                                    stringBuilder.Append($"{newObject}\r\n");
                                    stringBuilder.Append(jToken.Type == JTokenType.Array
                                        ? $"[{path.Last()} set{CommonGenerator.UppercaseFirstLetter(key)}:{newPath.Last()}List];\r\n"
                                        : $"[{path.Last()} set{CommonGenerator.UppercaseFirstLetter(key)}:{newPath.Last()}];\r\n");
                                    break;
                                case JTokenType.String:
                                    var enumString = GenerateEnumString(jToken.ToString(), pathSegment, newPath);
                                    //check if the type is an enum and handle it
                                    stringBuilder.Append(!string.IsNullOrEmpty(enumString)
                                        ? $"[{path.Last()} set{CommonGenerator.UppercaseFirstLetter(key)}: [{enumString}]];\r\n"
                                        : $"[{path.Last()} set{CommonGenerator.UppercaseFirstLetter(key)}:@{value.Replace("\n", "").Replace("\r", "")}];\r\n");
                                    break;
                                default:
                                    stringBuilder.Append($"[{path.Last()} set{CommonGenerator.UppercaseFirstLetter(key)}: {value.Replace("\n", "").Replace("\r", "")}];\r\n");
                                    break;
                            }
                        }
                    }
                    break;
                case JArray array:
                    {
                        var objectList = array.Children<JObject>();
                        stringBuilder.Append($"NSMutableArray *{path.Last()}List = [[NSMutableArray alloc] init];\r\n");
                        if (objectList.Any())
                        {
                            foreach (var item in objectList)
                            {
                                var jsonString = JsonConvert.SerializeObject(item);
                                var objectStringFromJson = ObjectiveCGenerateObjectFromJson(pathSegment, jsonString, path).TrimEnd();
                                stringBuilder.Append($"{objectStringFromJson}\r\n");
                                stringBuilder.Append($"[{path.Last()}List addObject: {path.Last()}];\r\n");
                            }
                        }
                        else
                        {
                            //append list of strings
                            foreach (var element in array)
                            {
                                stringBuilder.Append($"[{path.Last()}List addObject: @\"{element.Value<string>()}\"];\r\n");
                            }
                        }
                    }
                    break;
                case string jsonString:
                    {
                        var enumString = GenerateEnumString(jsonString, pathSegment, path);
                        if (!string.IsNullOrEmpty(enumString))
                        {
                            var className = GetObjectiveCModelName(pathSegment, path);
                            stringBuilder.Append($"{className} *{path.Last()} = [{enumString}];\r\n");
                        }
                        else if (jsonString.Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            stringBuilder.Append($"BOOL {path.Last()} = YES;\r\n");
                        }
                        else if (jsonString.Equals("false", StringComparison.OrdinalIgnoreCase))
                        {
                            stringBuilder.Append($"BOOL {path.Last()} = NO;\r\n");
                        }
                        else
                        {
                            stringBuilder.Append($"NSString *{path.Last()} = @\"{jsonString}\";\r\n");
                        }
                    }
                    break;
                case DateTime dateTime:
                    {
                        stringBuilder.Append($"NSString *{path.Last()}DateTimeString = @\"{dateTime.ToString(CultureInfo.InvariantCulture)}\";\r\n");
                        stringBuilder.Append($"NSDate *{path.Last()} = [NSDate ms_dateFromString: {path.Last()}DateTimeString];\r\n");
                    }
                    break;
                case null:
                    //do nothing
                    break;
                default:
                    var primitive = jsonObject.ToString();
                    //json deserializer capitalizes the bool types so undo that
                    if (primitive.Equals("True", StringComparison.OrdinalIgnoreCase))
                    {
                        stringBuilder.Append( $"BOOL {path.Last()} = YES;\r\n");
                    }
                    else if (primitive.Equals("False", StringComparison.OrdinalIgnoreCase))
                    {
                        stringBuilder.Append($"BOOL {path.Last()} = NO;\r\n");
                    }
                    else
                    {
                        stringBuilder.Append($"int32_t {path.Last()} = {primitive};\r\n");
                    }
                    break;
            }

            return  stringBuilder.ToString();
        }


        /// <summary>
        /// Get the Objective C representation of an enum type from a string hint and the odata path segment
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
                var typeName = GetObjectiveCModelName(pathSegment, path);
                var temp = enumHint.Split(", ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).First();

                //look for the proper name of the enum in the members
                foreach (var member in edmEnumType.Members)
                {
                    if (temp.Equals(member.Name,StringComparison.OrdinalIgnoreCase))
                    {
                        return $"{typeName} {member.Name}";
                    }
                }

                //if search failed default to first element
                return $"{typeName} {edmEnumType.Members.First().Name}";
            }

            //not an enum
            return string.Empty;
        }

        /// <summary>
        /// Return string representation of the classname for Objective C
        /// </summary>
        /// <param name="pathSegment">The OdataPathSegment in use</param>
        /// <param name="path">Path to follow to get find the classname</param>
        /// <returns>String representing the type in use</returns>
        private string GetObjectiveCModelName(ODataPathSegment pathSegment, ICollection<string> path)
        {
            var edmType = CommonGenerator.GetEdmTypeFromIdentifier(pathSegment, path);
            var namespaceString = (string)edmType.GetType().GetProperty("Namespace")?.GetValue(edmType, null) ?? string.Empty;
            var typeString = CommonGenerator.UppercaseFirstLetter(edmType.ToString().Split(".").Last());
            var stringBuilder = new StringBuilder();
            
            switch (namespaceString)
            {
                case "microsoft.graph":
                case "Edm":
                    //Start with MSGraph
                    stringBuilder.Append("MSGraph");
                    // no namespaces inserted
                    // Append the type
                    stringBuilder.Append(CommonGenerator.UppercaseFirstLetter(typeString)) ;
                    break;

                default:
                    // extract the namespace/sub namespaces to obtain the name
                    // microsoft.graph.termStore.set => MSGraphTermStoreSet
                    // alpha.beta.omega.theta => AlphaBetaOmegaTheta
                    var nameSpaceStringChunks = namespaceString.Split(".");
                    if ((nameSpaceStringChunks.Length > 2) && nameSpaceStringChunks[0].Equals("microsoft") && nameSpaceStringChunks[1].Equals("graph"))
                    {
                        //Start with MSGraph
                        stringBuilder.Append("MSGraph");
                        // skip the microsoft and the graph in the full string
                        nameSpaceStringChunks = nameSpaceStringChunks.Skip(2).ToArray();
                    }
                    //Uppercase each sub namespace and append it.
                    foreach (var chunk in nameSpaceStringChunks)
                    {
                        stringBuilder.Append(CommonGenerator.UppercaseFirstLetter(chunk));
                    }
                    // Append the type
                    stringBuilder.Append(CommonGenerator.UppercaseFirstLetter(typeString));
                    break;
            }
            return stringBuilder.ToString();
        }
    }
    public class ObjectiveCExpressions : LanguageExpressions
    {
        public override string FilterExpression => string.Empty;
        public override string SearchExpression => string.Empty;
        public override string ExpandExpression => string.Empty;
        public override string SelectExpression => string.Empty;
        public override string OrderByExpression => string.Empty;
        public override string SkipExpression => string.Empty;
        public override string SkipTokenExpression => string.Empty;
        public override string TopExpression => string.Empty;

        public override string FilterExpressionDelimiter => string.Empty;

        public override string SelectExpressionDelimiter => string.Empty;

        public override string OrderByExpressionDelimiter => string.Empty;

        public override string HeaderExpression => "[urlRequest setValue:@\"{1}\" forHTTPHeaderField:@\"{0}\"];\r\n";

        public override string[] ReservedNames => new string[] {
            "auto", "break", "case", "char", "const", "continue", "default", "do", "double", "else",
            "enum", "extern", "float", "for", "goto", "if", "inline", "int", "long", "register",
            "restrict", "return", "short", "signed", "sizeof", "static", "struct", "switch", "typedef",
            "union", "unsigned", "void", "volatile", "while", "_Bool", "_Complex", "_Imaginery" };

        public override string ReservedNameEscapeSequence => "_";

        public override string DoubleQuotesEscapeSequence => "\\\"";

        public override string SingleQuotesEscapeSequence => "'";
    }
}