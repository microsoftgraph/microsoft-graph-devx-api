using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("CodeSnippetsReflection.Test")]
namespace CodeSnippetsReflection.LanguageGenerators
{
    public class JavaGenerator
    {
        /// <summary>
        /// CommonGenerator instance
        /// </summary>
        private readonly CommonGenerator CommonGenerator;

        /// <summary>
        /// JavaGenerator constructor
        /// </summary>
        /// <param name="model">Model representing metadata</param>
        public JavaGenerator(IEdmModel model)
        {
            CommonGenerator = new CommonGenerator(model);
        }

        /// <summary>
        /// Formulates the requested Graph snippets and returns it as string for Java
        /// </summary>
        /// <param name="snippetModel">Model of the Snippets info <see cref="SnippetModel"/></param>
        /// <param name="languageExpressions">The language expressions to be used for code Gen</param>
        /// <returns>String of the snippet in Java code</returns>
        public string GenerateCodeSnippet(SnippetModel snippetModel, LanguageExpressions languageExpressions)
        {
            var snippetBuilder = new StringBuilder();
            try
            {
                var segment = snippetModel.Segments.Last();
                snippetModel.ResponseVariableName = CommonGenerator.EnsureVariableNameIsNotReserved(snippetModel.ResponseVariableName, languageExpressions);

                /*Auth provider section*/
                snippetBuilder.Append("IGraphServiceClient graphClient = GraphServiceClient.builder().authenticationProvider( authProvider ).buildClient();\r\n\r\n");
                //append any request options present
                snippetBuilder.Append(GenerateRequestOptionsSection(snippetModel, languageExpressions));
                /*Generate the query section of the request*/
                var requestActions = CommonGenerator.GenerateQuerySection(snippetModel, languageExpressions);

                if (snippetModel.Method == HttpMethod.Get)
                {
                    var typeName = GetJavaReturnTypeName(segment);
                    snippetBuilder.Append($"{typeName} {snippetModel.ResponseVariableName} = ");

                    if (segment is PropertySegment)
                        return GenerateCustomRequestForPropertySegment(snippetBuilder, snippetModel);

                    snippetBuilder.Append(GenerateRequestSection(snippetModel, $"{requestActions}\n\t.get();"));
                }
                else if (snippetModel.Method == HttpMethod.Post)
                {
                    switch (segment)
                    {
                        case NavigationPropertySegment _:
                        case EntitySetSegment _:
                        case NavigationPropertyLinkSegment _:
                            if (string.IsNullOrEmpty(snippetModel.RequestBody))
                                throw new Exception("No request Body present for Java POST request");

                            snippetBuilder.Append(JavaGenerateObjectFromJson(segment, snippetModel.RequestBody, new List<string> { snippetModel.ResponseVariableName }));
                            snippetBuilder.Append(GenerateRequestSection(snippetModel, $"{requestActions}\n\t.post({snippetModel.ResponseVariableName});"));
                            break;

                        case OperationSegment _:
                            //deserialize the object since the json top level contains the list of parameter objects
                            if (JsonConvert.DeserializeObject(snippetModel.RequestBody) is JObject testObj)
                            {
                                foreach (var (key, jToken) in testObj)
                                {
                                    var jsonString = JsonConvert.SerializeObject(jToken);
                                    snippetBuilder.Append(JavaGenerateObjectFromJson(segment, jsonString, new List<string> { CommonGenerator.LowerCaseFirstLetter(key) }));
                                }
                            }
                            snippetBuilder.Append(GenerateRequestSection(snippetModel, $"{requestActions}\n\t.post();"));
                            break;
                        default:
                            throw new Exception("Unknown Segment Type in URI for method POST");
                    }
                }
                else if (snippetModel.Method == HttpMethod.Patch)
                {
                    if (string.IsNullOrEmpty(snippetModel.RequestBody))
                        throw new Exception("No request Body present for Java Patch request");

                    snippetBuilder.Append(JavaGenerateObjectFromJson(segment, snippetModel.RequestBody, new List<string> { snippetModel.ResponseVariableName }));

                    if (segment is PropertySegment)
                        return GenerateCustomRequestForPropertySegment(snippetBuilder, snippetModel);

                    snippetBuilder.Append(GenerateRequestSection(snippetModel, $"{requestActions}\n\t.patch({snippetModel.ResponseVariableName});"));
                }
                else if (snippetModel.Method == HttpMethod.Put)
                {
                    if (string.IsNullOrEmpty(snippetModel.RequestBody))
                        throw new Exception("No request Body present for Java Put request");

                    if (snippetModel.ContentType.Contains("json"))
                        snippetBuilder.Append(JavaGenerateObjectFromJson(segment, snippetModel.RequestBody, new List<string> { snippetModel.ResponseVariableName }));
                    else
                        snippetBuilder.Append($"byte[] {snippetModel.ResponseVariableName} = Base64.getDecoder().decode({AddQuotesIfMising(snippetModel.RequestBody?.Replace("\n", string.Empty)?.Replace("\r", string.Empty))});\n\t");
                    snippetBuilder.Append(GenerateRequestSection(snippetModel, $"{requestActions}\n\t.put({snippetModel.ResponseVariableName});"));
                }
                else if (snippetModel.Method == HttpMethod.Delete)
                {
                    snippetBuilder.Append(GenerateRequestSection(snippetModel, $"{requestActions}\n\t.delete();"));
                }
                else
                {
                    throw new NotImplementedException("HTTP method not implemented for Java");
                }

                return snippetBuilder.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        /// <summary>
        /// This function helps to generate custom requests for properties inside objects which currently are not supported to be accessed in a url fashion in Java
        /// </summary>
        /// <param name="stringBuilder">Current state of the snippet built</param>
        /// <param name="snippetModel">model containing info about snippet to be generated</param>
        /// <returns></returns>
        private string GenerateCustomRequestForPropertySegment(StringBuilder stringBuilder, SnippetModel snippetModel)
        {
            stringBuilder.Append($"graphClient.customRequest(\"{snippetModel.Path}\", { GetJavaReturnTypeName(snippetModel.Segments.Last()) }.class)");
            stringBuilder.Append(JavaModelHasRequestOptionsParameters(snippetModel) ? "\n\t.buildRequest( requestOptions )" : "\n\t.buildRequest()");
            stringBuilder.Append(snippetModel.Method == HttpMethod.Get
                ? "\n\t.get();"
                : $"\n\t.patch({snippetModel.ResponseVariableName});");

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Java specific function to generate Object constructor section of a code snippet. In the event that another object is needed in the middle of generation,
        /// a recursive call is made to sort out the needed object.
        /// </summary>
        /// <param name="pathSegment">Odata Function/Entity from which the object is needed</param>
        /// <param name="jsonString">Json string from which the information of the object to be initialized is held</param>
        /// <param name="path">List of strings/identifier showing the path through the Edm/json structure to reach the Class Identifier from the segment</param>
        /// <param name="usedVarNames">List to keep track of variable names used to prevent use of the same variable name</param>
        private string JavaGenerateObjectFromJson(ODataPathSegment pathSegment, string jsonString, List<string> path, List<string> usedVarNames = null)
        {
            var stringBuilder = new StringBuilder();
            var jsonObject = JsonConvert.DeserializeObject(jsonString);
            usedVarNames = usedVarNames ?? new List<string>();//make sure list is not null
            var className = GetJavaReturnTypeName(pathSegment, path);

            switch (jsonObject)
            {
                case string _:
                    {
                        var enumIsFlags = CommonGenerator.GetEdmTypeFromIdentifier(pathSegment, path) is IEdmEnumType enumType && enumType.IsFlags;
                        var enumString = GenerateEnumString(jsonObject.ToString(), pathSegment, path);
                        var currentVarName = EnsureJavaVariableNameIsUnique(path.Last(), usedVarNames);
                        if (className == "String")
                        {
                            stringBuilder.Append($"String {currentVarName} = \"{jsonObject}\";\r\n");
                        }
                        else if (!string.IsNullOrEmpty(enumString))
                        {
                            //Enum is accessed as the Classname then enum type e.g Importance.LOW
                            var enumTypeName = enumString.Split(".").First();
                            if (enumIsFlags)
                                stringBuilder.Append($"EnumSet<{enumTypeName}> {currentVarName} = EnumSet.of({enumString});\r\n");
                            else
                                stringBuilder.Append($"{enumTypeName} {currentVarName} = {enumString.Split(",", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()};\r\n");
                        }
                        else if (jsonObject.Equals("true") || jsonObject.Equals("false"))
                        {
                            stringBuilder.Append($"boolean {currentVarName} = {jsonObject};\r\n");//boolean primitives values masquerading as strings.
                        }
                        else
                        {
                            stringBuilder.Append($"{GetJavaReturnTypeName(pathSegment, path)} {currentVarName} = {GenerateSpecialClassString($"{jsonObject}", pathSegment, path)};\r\n");
                        }
                    }
                    break;
                case JObject jObject:
                    {
                        var currentVarName = EnsureJavaVariableNameIsUnique(path.Last(), usedVarNames);
                        var localClassNameOverride = className.Equals("JsonElement") ? "JsonObject" : className; //jsonelements are abstract and cannot be instanciated
                        stringBuilder.Append($"{className} { currentVarName } = new {localClassNameOverride}();\r\n");
                        //initialize each member/property of the object
                        foreach (var (key, jToken) in jObject)
                        {
                            var value = JsonConvert.SerializeObject(jToken);
                            var newPath = path.Append(key).ToList();//add new identifier to the path

                            if (key.Contains("@odata") || key.StartsWith("@"))//sometimes @odata maybe in the middle e.g."invoiceStatus@odata.type"
                            {
                                stringBuilder = GenerateJavaAdditionalDataSection(stringBuilder, key, jToken.ToString(), className, currentVarName);
                                continue;
                            }
                            switch (jToken.Type)
                            {
                                case JTokenType.Array:
                                case JTokenType.Object:
                                    // in the case we have a collection page property, we need to reference the collection page variable instead
                                    var collectionType = CommonGenerator.GetEdmTypeFromIdentifier(pathSegment, newPath);
                                    var referenceSuffix = collectionType.TypeKind == EdmTypeKind.Entity ? $"Collection{page}" : "List";
                                    var variableName = (referenceSuffix.Contains(page) ? collectionType.FullTypeName().Split('.').Last() : newPath.Last()) + referenceSuffix;
                                    //new nested object needs to be constructed so call this function recursively to make it
                                    stringBuilder.Append($"{JavaGenerateObjectFromJson(pathSegment, value, newPath, usedVarNames)}");
                                    stringBuilder.Append(jToken.Type == JTokenType.Array
                                        ? $"{ currentVarName }.{ newPath.Last() } = { EnsureJavaVariableNameIsUnique(variableName, usedVarNames) };\r\n"
                                        : $"{ currentVarName }.{ newPath.Last() } = { EnsureJavaVariableNameIsUnique(newPath.Last(), usedVarNames) };\r\n");
                                    break;
                                case JTokenType.String:
                                    var enumString = GenerateEnumString(jToken.ToString(), pathSegment, newPath);
                                    var enumIsFlags = CommonGenerator.GetEdmTypeFromIdentifier(pathSegment, newPath) is IEdmEnumType enumType && enumType.IsFlags;
                                    stringBuilder.Append($"{ currentVarName }.{newPath.Last()} = ");
                                    if (string.IsNullOrEmpty(enumString))
                                        stringBuilder.Append($"{GenerateSpecialClassString($"{value}", pathSegment, newPath)};\r\n");
                                    else if(enumIsFlags)
                                        stringBuilder.Append($"EnumSet.of({enumString});\r\n");
                                    else
                                        stringBuilder.Append($"{enumString.Split(",", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()};\r\n");
                                    break;
                                default:
                                    stringBuilder.Append($"{ currentVarName }.{newPath.Last()} = { GenerateSpecialClassString($"{value}", pathSegment, newPath)};\r\n");
                                    break;
                            }
                            usedVarNames.Add(jToken.Type == JTokenType.Array ? $"{newPath.Last()}List" : newPath.Last());//add used variable name to used list
                        }
                    }
                    break;
                case JArray array:
                    {
                        var objectList = array.Children<JObject>();
                        var currentListName = EnsureJavaVariableNameIsUnique(path.Last() + "List", usedVarNames);
                        var collectionType = CommonGenerator.GetEdmTypeFromIdentifier(pathSegment, path);
                        if (objectList.Any())
                        {
                            //Item is a list/array so declare a typed list
                            stringBuilder.Append($"LinkedList<{className}> {currentListName} = new LinkedList<{className}>();\r\n");
                            foreach (var item in objectList)
                            {
                                var currentListItemName = EnsureJavaVariableNameIsUnique(path.Last(), usedVarNames);
                                var jsonItemString = JsonConvert.SerializeObject(item);
                                //we need to create a new object
                                var objectStringFromJson = JavaGenerateObjectFromJson(pathSegment, jsonItemString, path, usedVarNames);
                                stringBuilder.Append($"{objectStringFromJson}");
                                stringBuilder.Append($"{currentListName}.add({currentListItemName});\r\n");
                                usedVarNames.Add(path.Last());//add used variable name to used list
                            }
                            if (collectionType.TypeKind == EdmTypeKind.Entity)
                            {
                                var currentPageTypeName = CommonGenerator.UppercaseFirstLetter($"{CommonGenerator.UppercaseFirstLetter(collectionType.FullTypeName().Split('.').Last())}Collection{page}");
                                var currentPageCollectionName = EnsureJavaVariableNameIsUnique(CommonGenerator.LowerCaseFirstLetter(currentPageTypeName), usedVarNames);
                                var currentResponseTypeName = currentPageTypeName.Replace(page, "Response");
                                var currentResponseCollectionName = EnsureJavaVariableNameIsUnique(CommonGenerator.LowerCaseFirstLetter(currentResponseTypeName), usedVarNames);
                                stringBuilder.Append($"{currentResponseTypeName} {currentResponseCollectionName} = new {currentResponseTypeName}();\r\n");
                                stringBuilder.Append($"{currentResponseCollectionName}.value = {currentListName};\r\n");
                                stringBuilder.Append($"{currentPageTypeName} {currentPageCollectionName} = new {currentPageTypeName}({currentResponseCollectionName}, null);\r\n");
                            }
                        } 
                        else if (collectionType.TypeKind == EdmTypeKind.Enum)
                        {
                            stringBuilder.Append($"LinkedList<{className}> {currentListName} = new LinkedList<{className}>();\r\n");
                            array?.Select(x => x.Value<string>())?.
                                SelectMany(x => GenerateEnumString(x, pathSegment, path)?.
                                            Split(",", StringSplitOptions.RemoveEmptyEntries))?.
                                ToList()?.
                                ForEach(x => stringBuilder.Append($"{currentListName}.add({x});\r\n"));
                        }
                        else
                        {
                            stringBuilder.Append($"LinkedList<{className}> {currentListName} = new LinkedList<{className}>();\r\n");
                            //its not nested objects but a string collection
                            foreach (var element in array)
                            {
                                stringBuilder.Append($"{currentListName}.add({(className == "String" ? AddQuotesIfMising(element.Value<string>()) : GenerateSpecialClassString(element.Value<string>(), pathSegment, path))});\r\n");
                            }
                        }
                    }
                    break;
                case null:
                    //do nothing
                    break;
                default:
                    var primitive = jsonObject.ToString();
                    var curVarName = EnsureJavaVariableNameIsUnique(path.Last(), usedVarNames);
                    stringBuilder.Append($"{className} {curVarName} = {(className == "String" ? AddQuotesIfMising(primitive) : GenerateSpecialClassString(primitive, pathSegment, path))};\r\n");//item is a primitive print as is
                    break;
            }

            //check if this is the outermost object in a potential nested object structure and needs the semicolon termination character.
            return path.Count == 1 ? $"{stringBuilder.ToString().TrimEnd()}\r\n\r\n" : stringBuilder.ToString();
        }

        /// <summary>
        /// Get the Csharp representation of a string and add any parsing calls that may be required.
        /// </summary>
        /// <param name="stringParameter">String parameter that may need parsing</param>
        /// <param name="pathSegment">Odata Function/Entity from which the object is needed</param>
        /// <param name="path">List of strings/identifier showing the path through the Edm/json structure to reach the Class Identifier from the segment</param>
        private string GenerateSpecialClassString(string stringParameter, ODataPathSegment pathSegment, ICollection<string> path)
        {
            try
            {
                var className = GetJavaClassNameFromOdataPath(pathSegment, path);
                //check the classes and parse them appropriately
                switch (className)
                {
                    case "DateTimeOffset":
                        return $"CalendarSerializer.deserialize({AddQuotesIfMising(stringParameter)})";

                    case "Guid":
                        return $"UUID.fromString({AddQuotesIfMising(stringParameter)})";

                    case "Date"://try to parse the date to get the day,month and year params
                        string parsedDate = DateTime.TryParse(stringParameter, out var dateTime)
                            ? $"{dateTime.Year},{dateTime.Month},{dateTime.Day}"
                            : "1900,1,1";//use default params on parse failure
                        return $"new DateOnly({parsedDate})";

                    case "Duration":
                        return $"DatatypeFactory.newInstance().newDuration({AddQuotesIfMising(stringParameter)})";

                    case "Binary":
                        return $"Base64.getDecoder().decode({AddQuotesIfMising(stringParameter)})";

                    case "Double":
                        return $"{DefaultNumericValues(stringParameter)}d";

                    case "Int64":
                        return $"{DefaultNumericValues(stringParameter)}L";

                    case "TimeOfDay":
                        return DateTime.TryParse(stringParameter, out var timeOfDay)
                            ? $"new TimeOfDay({timeOfDay.Hour}, {timeOfDay.Minute}, {timeOfDay.Second})"
                            : "new TimeOfDay(0, 0, 0)";

                    case "Boolean":
                        return bool.TryParse(stringParameter, out var boolValue)
                            ? boolValue.ToString().ToLower()
                            : "false";
                    case "Json":
                        return $"JsonParser.parseString({AddQuotesIfMising(ClearStringLiteralsFromBreakingCharacters(stringParameter))})";

                    case "Int16":
                    case "Int32":
                        return DefaultNumericValues(stringParameter);
                    default:
                        return stringParameter;
                }
            }
            catch
            {
                return stringParameter;
            }
        }

        private const string quote = "\"";
        private const string escapedQuote = "\\\"";
        private string AddQuotesIfMising(string parameter) => $"{(parameter.StartsWith(quote) ? string.Empty : quote)}{parameter}{(parameter.EndsWith(quote) && !parameter.EndsWith(escapedQuote) ? string.Empty : quote)}";
        private string DefaultNumericValues(string parameter) => parameter.Equals("null", StringComparison.InvariantCultureIgnoreCase) ? "0" : parameter;


        /// <summary>
        /// Generates language specific code to add special properties to the AdditionalData dictionary
        /// </summary>
        /// <param name="stringBuilder">The original string builder containing code generated so far</param>
        /// <param name="key">The odata key/property</param>
        /// <param name="value">The value related to the odata key/property</param>
        /// <param name="className">The class name for the entity needing modification</param>
        /// <param name="currentVarName">The variable name of the current object in use </param>
        /// <returns>a string builder with the relevant odata code added</returns>
        private static StringBuilder GenerateJavaAdditionalDataSection(StringBuilder stringBuilder, string key, string value, string className, string currentVarName)
        {
            switch (key)
            {
                case "@odata.id" when className.Equals("DirectoryObject"):
                    try
                    {
                        var uriLastSegmentString = new Uri(value).Segments.Last();
                        uriLastSegmentString = Uri.UnescapeDataString(uriLastSegmentString);
                        stringBuilder.Append($"{currentVarName}.id = \"{uriLastSegmentString}\";\r\n");
                    }
                    catch (UriFormatException)
                    {
                        stringBuilder.Append($"{currentVarName}.id = \"{value}\";\r\n");//its not really a URI
                    }
                    break;

                case "@odata.type":
                    var proposedType = CommonGenerator.UppercaseFirstLetter(value.Split(".").Last());
                    // check if the odata type specified is different
                    // maybe due to the declaration of a subclass of the type specified from the url.
                    if (!className.Equals(proposedType))
                    {
                        stringBuilder.Replace(className, proposedType);
                    }
                    break;

                default:
                    //just append the property as part of the additionalData of the object
                    stringBuilder.Append($"{currentVarName}.additionalDataManager().put(\"{key}\", new JsonPrimitive(\"{ClearStringLiteralsFromBreakingCharacters(value)}\"));\r\n");
                    break;
            }

            return stringBuilder;
        }

        private static string ClearStringLiteralsFromBreakingCharacters(string input) => input?.
                                                            Replace(quote, escapedQuote)?.
                                                            Replace("\r", string.Empty)?.
                                                            Replace("\n", string.Empty);

        /// <summary>
        /// Java specific function to check how many times a variableName has been used and append a number at the end to make it unique
        /// </summary>
        /// <param name="variableName">Variable name to be used</param>
        /// <param name="usedList">List of variable names that have been already used</param>
        private static string EnsureJavaVariableNameIsUnique(string variableName, IEnumerable<string> usedList)
        {
            var count = usedList.Count(x => x.Equals(variableName));
            if (count > 0)
            {
                return $"{variableName}{count}";//append the count to the end of string since we've used it before
            }
            return CommonGenerator.EnsureVariableNameIsNotReserved(variableName, new JavaExpressions());//make sure its not reserved
        }

        /// <summary>
        /// Java specific function to generate the Enum string representation of an enum
        /// </summary>
        /// <param name="enumHint">Name of the enum needing to change to java fashion(typically camel cased)</param>
        /// <param name="pathSegment">Odata path segment containing needed parent information to know the owner of the enum</param>
        /// <param name="path">List of string showing the path of traversal through the tree structure</param>
        /// <returns>Java representation of an enum type e.g. Importance.LOW</returns>
        private string GenerateEnumString(string enumHint, ODataPathSegment pathSegment, List<string> path)
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
                var typeName = GetJavaClassNameFromOdataPath(pathSegment, path);
                var temp = enumHint?.
                    Split(", |".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)?.
                    Select(x => x.Trim())?.
                    Join(edmEnumType.Members.Select(x => x.Name),
                            x => x,
                            x => x,
                            (x, y) => $"{typeName}.{ GetSnakeCaseFromCamelCase(y) }",
                            StringComparer.OrdinalIgnoreCase);
                if (temp.Any())
                    return temp.Aggregate((x, y) => $"{x},{y}");
                else //return the first value of the enum as a fallback
                    return $"{typeName}.{ GetSnakeCaseFromCamelCase(edmEnumType.Members.First().Name) }";
            }

            return string.Empty;
        }

        /// <summary>
        /// Helper function that takes in a camel case to return a snake case string in all caps for java enums
        /// </summary>
        /// <param name="camelCaseString">String that is in camel case</param>
        /// <returns>Snake case string in all caps e.g myBad => MY_BAD</returns>
        private static string GetSnakeCaseFromCamelCase(string camelCaseString)
        {
            var snakeCaseBuilder = new StringBuilder();
            foreach (var c in CommonGenerator.LowerCaseFirstLetter(camelCaseString))
            {
                if (char.IsUpper(c))
                {
                    snakeCaseBuilder.Append("_");//insert underscore between words
                }
                snakeCaseBuilder.Append(char.ToUpper(c));//convert the character to uppercase
            }
            return snakeCaseBuilder.ToString();
        }

        /// <summary>
        /// Return string representation of the classname for Java
        /// </summary>
        /// <param name="pathSegment">The OdataPathSegment in use</param>
        /// <param name="path">Path to follow to get find the classname</param>
        /// <returns>String representing the type in use</returns>
        private string GetJavaClassNameFromOdataPath(ODataPathSegment pathSegment, ICollection<string> path)
        {
            var edmType = CommonGenerator.GetEdmTypeFromIdentifier(pathSegment, path);
            //we need to split the string and get last item //eg microsoft.graph.data => Data
            return CommonGenerator.UppercaseFirstLetter(edmType.ToString().Split(".").Last());
        }

        private const string page = "Page";
        /// <summary>
        /// Java specific function that infers the return type for java that is going to be returned
        /// </summary>
        /// <param name="pathSegment">Segment from the URI</param>
        /// <param name="path">query path</param>
        /// <returns>String representing the return type</returns>
        private string GetJavaReturnTypeName(ODataPathSegment pathSegment, List<string> path = null)
        {
            var edmType = (path == null ? null : CommonGenerator.GetEdmTypeFromIdentifier(pathSegment, path)) ?? pathSegment?.EdmType;
            if (edmType is IEdmCollectionType collectionType)
                if (pathSegment is OperationSegment opSegment)
                {
                    var typeName = opSegment.Operations?.FirstOrDefault()?.Parameters?.FirstOrDefault()?.Type?.Definition?.FullTypeName()?.Split(".")?.LastOrDefault()?.Replace(")", string.Empty); //last replace is for the case we have collections
                    return $"{CommonGenerator.UppercaseFirstLetter(typeName)}{CommonGenerator.UppercaseFirstLetter(opSegment.Identifier)}Collection{page}";
                }
                else if (collectionType.ElementType.Definition is IEdmNamedElement edmNamedElement)
                    return $"{CommonGenerator.UppercaseFirstLetter(edmNamedElement.Name)}Collection{(pathSegment is NavigationPropertySegment navPropPathSeg && !navPropPathSeg.NavigationProperty.ContainsTarget ? "WithReferences" : string.Empty)}{page}";
                else
                    return $"{CommonGenerator.UppercaseFirstLetter(edmType.FullTypeName().Split(".").Last())}Collection{page}";
            else
                switch (edmType?.FullTypeName())
                {
                    case "Edm.Stream":
                        return "InputStream";
                    case "Edm.Guid":
                        return "UUID";
                    case "Edm.DateTimeOffset":
                        return "Calendar";
                    case "Edm.Date":
                        return "DateOnly";
                    case "Edm.Double":
                        return "Double";
                    case "Edm.Int16":
                        return "Short";
                    case "Edm.Int32":
                        return "int";
                    case "Edm.Int64":
                        return "Long";
                    case "microsoft.graph.Json":
                        return "JsonElement";
                    default:
                        return edmType == null ? "Content" : CommonGenerator.UppercaseFirstLetter(edmType.FullTypeName().Split(".").Last());
                }
        }

        /// <summary>
        /// Generate the snippet section that makes the call to the api
        /// </summary>
        /// <param name="snippetModel">Snippet model built from the request</param>
        /// <param name="requestActions">String of requestActions to be done inside the code block</param>
        private static string GenerateRequestSection(SnippetModel snippetModel, string requestActions)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("graphClient");
            stringBuilder.Append(JavaGenerateResourcesPath(snippetModel));//Generate the Resources path for Csharp
            //check if there are any custom query options appended
            stringBuilder.Append(JavaModelHasRequestOptionsParameters(snippetModel) ? "\n\t.buildRequest( requestOptions )" : "\n\t.buildRequest()");
            stringBuilder.Append(requestActions);//Append footers
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Formulates the resources part of the generated snippets
        /// </summary>
        /// <param name="snippetModel">Model of the Snippets info <see cref="SnippetModel"/></param>
        /// <returns>String of the resources in Csharp code</returns>
        private static string JavaGenerateResourcesPath(SnippetModel snippetModel)
        {
            var resourcesPath = new StringBuilder();
            var resourcesPathSuffix = string.Empty;

            // lets append all resources
            foreach (var item in snippetModel.Segments)
            {
                switch (item)
                {
                    //handle indexing into collections
                    case KeySegment keySegment:
                        resourcesPath.Remove(resourcesPath.Length - 2, 2);//first remove the preceding curly braces
                        resourcesPath.Append($"(\"{keySegment.Keys.FirstOrDefault().Value}\")");
                        break;
                    //handle functions/requestActions and any parameters present into collections
                    case OperationSegment operationSegment:
                        var paramList = CommonGenerator.GetParameterListFromOperationSegmentWithNames(operationSegment, snippetModel, "List");
                        var operationBoundTypeName = (operationSegment?.EdmType?.FullTypeName() ?? 
                                                    operationSegment?.Operations?.FirstOrDefault()?.Parameters?.FirstOrDefault()?.Type?.FullName())
                                                        ?.Split(".")
                                                        ?.Last()
                                                        ?.Trim(')'); // in case it's a collection
                        var paramSetBuilderName = $"{CommonGenerator.UppercaseFirstLetter(CommonGenerator.UppercaseFirstLetter(operationBoundTypeName))}{CommonGenerator.UppercaseFirstLetter(operationSegment.Identifier)}ParameterSet";
                        var paramSetBuilder = paramList.Any() ? 
                                                $"{paramSetBuilderName}\n\t\t.newBuilder()\n\t\t" +
                                                    paramList.Select(x => $".with{CommonGenerator.UppercaseFirstLetter(x.Key)}({x.Value})").Aggregate((x, y) => $"{x}\n\t\t{y}") +
                                                    "\n\t\t.build()"
                                                : string.Empty;
                        resourcesPath.Append($"\n\t.{CommonGenerator.LowerCaseFirstLetter(operationSegment.Identifier)}({paramSetBuilder})");
                        break;
                    case ReferenceSegment _:
                        resourcesPath.Append(".reference()");
                        break;
                    case ValueSegment _:
                        resourcesPath.Append(".content()");
                        break;
                    case PropertySegment _:
                        //do nothing we cant access it directly
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
                                nextSegmentIndex = snippetModel.Segments.Count - 1;

                            var nextSegment = snippetModel.Segments[nextSegmentIndex];
                            //check if the next segment is a KeySegment to know if we will be accessing a single entity of a collection.
                            resourcesPathSuffix = (item.EdmType is IEdmCollectionType) && !(nextSegment is KeySegment) ? ".references()" : ".reference()";
                        }
                        resourcesPath.Append($".{CommonGenerator.LowerCaseFirstLetter(item.Identifier)}()");
                        break;
                    case CountSegment _:
                        resourcesPath.Append(".count()");
                        break;
                    default:
                        //its most likely just a resource so append it with lower case first letter and `()` at the end
                        resourcesPath.Append($".{CommonGenerator.LowerCaseFirstLetter(item.Identifier)}()");
                        break;
                }
            }

            if (!string.IsNullOrEmpty(resourcesPathSuffix))
            {
                resourcesPath.Append(resourcesPathSuffix);
            }

            return resourcesPath.ToString();
        }


        /// <summary>
        /// Generate a list of Request options and populate them as needed in a java specific function.
        /// Java does not have helper functions for all the odata parameter types and therefore the missing ones are inserted as query options here.
        /// </summary>
        /// <param name="snippetModel">Snippet model built from the request</param>
        /// <param name="languageExpressions">Language Expressions needed for the language</param>
        private static string GenerateRequestOptionsSection(SnippetModel snippetModel, LanguageExpressions languageExpressions)
        {
            if (!JavaModelHasRequestOptionsParameters(snippetModel))
                return string.Empty; //nothing to do here

            var stringBuilder = new StringBuilder();
            var requestOptionsPattern = "requestOptions.add(new QueryOption(\"{0}\", \"{1}\"));\r\n";
            stringBuilder.Append("LinkedList<Option> requestOptions = new LinkedList<Option>();\r\n");

            //insert any header options options
            foreach (var (key, value) in snippetModel.RequestHeaders)
            {
                if (key.ToLower().Equals("host", StringComparison.Ordinal))//no need to generate source for the host header
                    continue;
                //append the header to the snippet
                var valueString = value.First().Replace("\"", languageExpressions.DoubleQuotesEscapeSequence);
                stringBuilder.Append($"requestOptions.add(new HeaderOption(\"{key}\", \"{valueString}\"));\r\n");
            }

            //insert any custom query options
            foreach (var (key, value) in snippetModel.CustomQueryOptions)
            {
                stringBuilder.Append(string.Format(requestOptionsPattern, key, value));
            }

            //Append any search queries
            if (!string.IsNullOrEmpty(snippetModel.SearchExpression))
            {
                stringBuilder.Append(string.Format(requestOptionsPattern, "$search", snippetModel.SearchExpression));
            }

            //return request options section with a new line appended
            return $"{stringBuilder}\r\n";
        }

        /// <summary>
        /// Helper Function to quickly tell if one needs request options added for snippet generation to take place.
        /// </summary>
        /// <param name="snippetModel">Model to used for snippet generation</param>
        /// <returns>boolean value showing whether or not the snippet generation needs to have request options</returns>
        private static bool JavaModelHasRequestOptionsParameters(SnippetModel snippetModel)
        {
            if (snippetModel.CustomQueryOptions.Any()
                || snippetModel.RequestHeaders.Any(x => !x.Key.ToLower().Equals("host"))
                || !string.IsNullOrEmpty(snippetModel.SearchExpression))
            {
                return true;
            }

            return false;
        }
    }

    internal class JavaExpressions : LanguageExpressions
    {
        public override string ExpandExpression => "\n\t.expand(\"{0}\")";
        public override string SelectExpression => "\n\t.select(\"{0}\")";
        public override string SelectExpressionDelimiter => ",";
        public override string TopExpression => "\n\t.top({0})";
        public override string FilterExpression => "\n\t.filter(\"{0}\")";
        public override string FilterExpressionDelimiter => ",";
        public override string SearchExpression => string.Empty;
        public override string SkipExpression => "\n\t.skip({0})";
        public override string HeaderExpression => string.Empty;
        public override string SkipTokenExpression => "\n\t.skipToken(\"{0}\")";
        public override string OrderByExpression => "\n\t.orderBy(\"{0}\")";
        public override string OrderByExpressionDelimiter => " ";
        public override string[] ReservedNames => new[] {
            "abstract","assert","boolean","break","byte","case","catch","char",
            "class","const","continue","default","do","double","else","enum",
            "extends","final","finally","float","for","goto","if","implements",
            "import","instanceof","int","interface","long","native","new",
            "package","private","protected","public","return","short","static",
            "strictfp","super","switch","synchronized","this","throw","throws",
            "transient","try","void","volatile","while","true","false","null"   };
        public override string ReservedNameEscapeSequence => "_";
        public override string DoubleQuotesEscapeSequence => "\\\"";
    }
}
