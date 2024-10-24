using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.MicrosoftExtensions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.ModelGraph
{
    public record SnippetCodeGraph
    {
        private static readonly Regex splitCommasExcludingBracketsRegex = new(@"([^,\(\)]+(\(.*?\))*)+", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(5));

        private static readonly CodeProperty EMPTY_PROPERTY = new() { Name = null, Value = null, Children = null, PropertyType = PropertyType.Default };

        private static readonly Dictionary<string, PropertyType> _formatPropertyTypes = new (StringComparer.OrdinalIgnoreCase)
        {
            {"int32", PropertyType.Int32},
            {"int64", PropertyType.Int64},
            {"double", PropertyType.Double},
            {"float32", PropertyType.Float32},
            {"float64", PropertyType.Float64},
            {"duration", PropertyType.Duration},
            {"boolean", PropertyType.Boolean},
            {"base64url", PropertyType.Base64Url},
            {"guid", PropertyType.Guid},
            {"uuid", PropertyType.Guid},
            {"date-time", PropertyType.DateTime},
            {"time", PropertyType.TimeOnly},
            {"date", PropertyType.DateOnly}
        };

        private static readonly char NamespaceNameSeparator = '.';

        public SnippetCodeGraph(SnippetModel snippetModel)
        {
            ResponseSchema = snippetModel.ResponseSchema;
            RequestSchema = snippetModel.RequestSchema;
            HttpMethod = snippetModel.Method;
            Nodes = snippetModel.PathNodes;
            Headers = parseHeaders(snippetModel);
            Options = Enumerable.Empty<CodeProperty>();
            Parameters = parseParameters(snippetModel);
            PathParameters = parsePathParameters(snippetModel);
            Body = parseBody(snippetModel);
            ApiVersion = snippetModel.ApiVersion;
            RequestUrl = $"https://graph.microsoft.com/{snippetModel.ApiVersion}{snippetModel.Path}{snippetModel.QueryString}";
        }

        public OpenApiSchema ResponseSchema
        {
            get; set;
        }

        public OpenApiSchema RequestSchema
        {
            get; set;
        }

        public string RequestUrl
        {
            get; set;
        }

        public string ApiVersion
        {
            get; set;
        }

        public HttpMethod HttpMethod
        {
            get; set;
        }

        public IEnumerable<CodeProperty> Headers
        {
            get; set;
        }
        public IEnumerable<CodeProperty> Options
        {
            get; set;
        }

        public IEnumerable<CodeProperty> Parameters
        {
            get; set;
        }

        public IEnumerable<CodeProperty> PathParameters
        {
            get; set;
        }

        public CodeProperty Body
        {
            get; set;
        }

        public IEnumerable<OpenApiUrlTreeNode> Nodes
        {
            get; set;
        }

        public bool HasHeaders()
        {
            return Headers.Any();
        }

        public bool HasOptions()
        {
            return Options.Any();
        }

        public bool HasParameters()
        {
            return Parameters.Any();
        }

        public bool HasPathParameters()
        {
            return PathParameters.Any();
        }

        public bool HasBody()
        {
            return Body.PropertyType != PropertyType.Default;
        }

        public bool HasReturnedBody(){
            return !(ResponseSchema == null || (ResponseSchema.Properties.Count == 1 && ResponseSchema.Properties.First().Key.Equals("error", StringComparison.OrdinalIgnoreCase)));
        }
        public bool RequiresRequestConfig()
        {
            return HasHeaders() || HasOptions() || HasParameters();
        }

        public bool HasJsonBody(){
            return HasBody() && Body.PropertyType != PropertyType.Binary;
        }

        ///
        /// Parses Headers Filtering Out 'Host'
        ///
        private static IEnumerable<CodeProperty> parseHeaders(SnippetModel snippetModel)
        {
            return snippetModel.RequestHeaders.Where(h => !h.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                .Select(h => new CodeProperty { Name = h.Key, Value = h.Value?.FirstOrDefault(), Children = null, PropertyType = PropertyType.String })
                .ToList();
        }

        private static List<CodeProperty> parseParameters(SnippetModel snippetModel)
        {

            var queryParameters = snippetModel.EndPathNode
                .PathItems
                .SelectMany(static pathItem => pathItem.Value.Operations)
                .Where(operation => operation.Key.ToString().Equals(snippetModel.Method.ToString(), StringComparison.OrdinalIgnoreCase)) // get the operations that match the method
                .SelectMany(static operation => operation.Value.Parameters)
                .Where(static parameter => parameter.In == ParameterLocation.Query); // find the parameters in the path

            var ArrayParameters = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase,"select", "expand", "orderby");
            var NumberParameters = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase,"skip", "top");

            var parameters = new List<CodeProperty>();
            if (!string.IsNullOrEmpty(snippetModel.QueryString))
            {
                NameValueCollection queryCollection = HttpUtility.ParseQueryString(snippetModel.QueryString);
                foreach (String key in queryCollection.AllKeys)
                {
                    //try to lookup the parameter from the schema
                    var queryParam = queryParameters.FirstOrDefault(param =>
                        NormalizeQueryParameterName(param.Name).Equals(NormalizeQueryParameterName(key),
                            StringComparison.OrdinalIgnoreCase));

                    //setup defaults
                    var name = NormalizeQueryParameterName(key).Trim();
                    var value = GetQueryParameterValue(queryCollection[key]);
                    var schema = "string";
                    if (NumberParameters.Contains(name))
                        schema = "integer";
                    else if(ArrayParameters.Contains(name))
                        schema = "array";

                    // use values from the schema if match is found
                    if (queryParam != null)
                    {
                        schema = queryParam.Schema.Type.ToLowerInvariant();
                        name = NormalizeQueryParameterName(queryParam.Name).Trim();
                    }

                    switch (schema)
                    {
                        case "string":
                            parameters.Add(evaluateStringProperty(name, value, queryParam?.Schema));
                            break;
                        case "integer":
                            parameters.Add(new CodeProperty { Name = name, Value = int.TryParse(value, out _) ? value : "1", PropertyType = PropertyType.Int32, Children = new List<CodeProperty>() });
                            break;
                        case "double":
                            parameters.Add(new CodeProperty { Name = name, Value = double.TryParse(value, out _) ? value : "1.0d", PropertyType = PropertyType.Double, Children = new List<CodeProperty>() });
                            break;
                        case "boolean":
                            parameters.Add(new CodeProperty { Name = name, Value = bool.TryParse(value, out _) ? value : "false", PropertyType = PropertyType.Boolean, Children = new List<CodeProperty>() });
                            break;
                        case "array":
                            var children = splitCommasExcludingBracketsRegex.Split(GetQueryParameterValue(queryCollection[key]))
                                .Where(x => !String.IsNullOrEmpty(x) && !x.StartsWith('(') && !x.Equals(","))
                                .Select(x => new CodeProperty() { Name = null, Value = x, PropertyType = PropertyType.String }).ToList();
                            parameters.Add(new CodeProperty { Name = name, Value = null, PropertyType = PropertyType.Array, Children = children });
                            break;
                    }
                }
            }
            return parameters;
        }

        private static List<CodeProperty> parsePathParameters(SnippetModel snippetModel)
        {

            var pathParameters = snippetModel.EndPathNode
                                             .PathItems
                                             .SelectMany(static pathItem => pathItem.Value.Operations)
                                             .Where(operation => operation.Key.ToString().Equals(snippetModel.Method.ToString(), StringComparison.OrdinalIgnoreCase)) // get the operations that match the method
                                             .SelectMany(static operation => operation.Value.Parameters)
                                             .Where(static parameter => parameter.In == ParameterLocation.Path)// find the parameters in the path based on operation
                                             .Union(snippetModel.EndPathNode.PathItems.Values.SelectMany(static pathItem => pathItem.Parameters)
                                                 .Where(parameter => parameter.In == ParameterLocation.Path &&
                                                                     snippetModel.EndPathNode.Segment.IsFunctionWithParameters() && // don't include indexers
                                                                     snippetModel.EndPathNode.Segment.Contains(parameter.Name,StringComparison.OrdinalIgnoreCase))// alternate keys will show up in the path item and not the operation
                                             );

            var parameters = new List<CodeProperty>();
            foreach (var parameter in pathParameters)
            {
                switch (parameter.Schema.Type.ToLowerInvariant(), parameter.Schema.Format?.ToLowerInvariant())
                {
                    case ("string", _):
                        var codeProperty = evaluateStringProperty(parameter.Name, $"{{{parameter.Name}}}", parameter.Schema);
                        // At the moment, enums in path parameters are passed as strings, so pull a string equivalent of the enum
                        if (codeProperty.PropertyType == PropertyType.Enum)
                        {
                            codeProperty.PropertyType = PropertyType.String;
                            codeProperty.Value = codeProperty.Children?.FirstOrDefault().Value ?? parameter.Name;
                        }
                        parameters.Add(codeProperty);
                        break;
                    case ("integer",_):
                    case (_,"int32"):
                    case (_,"int16"):
                    case (_,"int8"):
                        parameters.Add(new CodeProperty { Name = parameter.Name, Value = int.TryParse(parameter.Name, out _) ? parameter.Name : "1", PropertyType = PropertyType.Int32, Children = new List<CodeProperty>() });
                        break;
                    case (_,"int64"):
                        parameters.Add(new CodeProperty { Name = parameter.Name, Value = int.TryParse(parameter.Name, out _) ? parameter.Name : "1", PropertyType = PropertyType.Int64, Children = new List<CodeProperty>() });
                        break;
                    case ("double",_):
                        parameters.Add(new CodeProperty { Name = parameter.Name, Value = double.TryParse(parameter.Name, out _) ? parameter.Name : "1.0d", PropertyType = PropertyType.Double, Children = new List<CodeProperty>() });
                        break;
                    case ("boolean",_):
                        parameters.Add(new CodeProperty { Name = parameter.Name, Value = bool.TryParse(parameter.Name, out _) ? parameter.Name : "false", PropertyType = PropertyType.Boolean, Children = new List<CodeProperty>() });
                        break;
                }
            }
            return parameters;
        }

        private static string NormalizeQueryParameterName(string queryParam) => HttpUtility.UrlDecode(queryParam.TrimStart('$').ToFirstCharacterLowerCase());

        private static string GetQueryParameterValue(string originalValue)
        {
            var escapedParam = System.Web.HttpUtility.UrlDecode(originalValue);
            if (escapedParam.Equals("true", StringComparison.OrdinalIgnoreCase) || escapedParam.Equals("false", StringComparison.OrdinalIgnoreCase))
                return escapedParam.ToLowerInvariant();
            else if (int.TryParse(escapedParam, out var intValue))
                return intValue.ToString();
            return escapedParam;
        }

        private static CodeProperty parseBody(SnippetModel snippetModel)
        {
            if (string.IsNullOrWhiteSpace(snippetModel?.RequestBody))
                return EMPTY_PROPERTY;

            switch (snippetModel.ContentType?.Split(';').First().ToLowerInvariant())
            {
                case "application/json":
                    return TryParseBody(snippetModel);
                case "application/octet-stream":
                    return new() { Name = null, Value = snippetModel.RequestBody, Children = null, PropertyType = PropertyType.Binary };
                default:
                    return TryParseBody(snippetModel);//in case the content type header is missing but we still have a json payload
            }
        }

        private static string ComputeRequestBody(SnippetModel snippetModel)
        {
            var requestBodySuffix = $"{snippetModel.Method.Method.ToLower().ToFirstCharacterUpperCase()}RequestBody"; // calculate the suffix using the HttpMethod
            var name = snippetModel.RequestSchema?.Reference?.GetClassName();

            if(!string.IsNullOrEmpty(name))
                return name?.ToFirstCharacterUpperCase();

            var nodes = snippetModel.PathNodes;
            if (!(nodes?.Any() ?? false)) return string.Empty;

            var nodeName = nodes.Where(x => !x.Segment.IsCollectionIndex())
                .Select(x =>
                {
                    if (x.Segment.IsFunction())
                        return x.Segment.Split('.')[^1];
                    else
                        return x.Segment;
                })
                .Last()
                .ToFirstCharacterUpperCase();

            var singularNodeName = nodeName[^1] == 's' ? nodeName[..^1] : nodeName;

            if (nodes.Last()?.Segment?.IsCollectionIndex() == true)
                return singularNodeName;
            else
                return nodeName?.Append(requestBodySuffix);
        }

        private static CodeProperty TryParseBody(SnippetModel snippetModel)
        {
            if (!snippetModel.IsRequestBodyValid)
                throw new InvalidOperationException($"Unsupported content type: {snippetModel.ContentType}");

            var parsedBody = JsonSerializer.Deserialize<JsonElement>(snippetModel.RequestBody, JsonHelper.JsonSerializerOptions);
            var schema = snippetModel.RequestSchema;
            var className = schema.GetSchemaTitle().ToFirstCharacterUpperCase() ?? ComputeRequestBody(snippetModel);
            return parseJsonObjectValue(className, parsedBody, schema, snippetModel.Schemas, snippetModel.EndPathNode);
        }

        private static CodeProperty parseJsonObjectValue(string rootPropertyName, JsonElement value, OpenApiSchema schema, IDictionary<string, OpenApiSchema> snippetModelSchemas, OpenApiUrlTreeNode currentNode = null)
        {
            var children = new List<CodeProperty>();

            if (value.ValueKind != JsonValueKind.Object) throw new InvalidOperationException($"Expected JSON object and got {value.ValueKind}");

            if (value.TryGetProperty("@odata.type", out var odataTypeProperty))
            {
                var discriminatorValue = odataTypeProperty.GetString()?.TrimStart('#');
                if (!string.IsNullOrEmpty(discriminatorValue) && snippetModelSchemas.TryGetValue(discriminatorValue, out OpenApiSchema specifiedSchema))
                {
                    // use the schema explictly stated as the payload may be a derived type and the metadata defines the base type which does not define all properties.
                    schema = specifiedSchema;
                }
            }

            var propertiesAndSchema = value.EnumerateObject()
                                            .Select(x => new Tuple<JsonProperty, OpenApiSchema>(x, schema.GetPropertySchema(x.Name)));
            foreach (var propertyAndSchema in propertiesAndSchema.Where(x => x.Item2 != null))
            {
                var propertyName = propertyAndSchema.Item1.Name.ToFirstCharacterLowerCase();
                children.Add(parseProperty(propertyName, propertyAndSchema.Item1.Value, propertyAndSchema.Item2, snippetModelSchemas));
            }

            var propertiesWithoutSchema = propertiesAndSchema.Where(x => x.Item2 == null).Select(x => x.Item1);
            if (propertiesWithoutSchema.Any())
            {
                var additionalChildren = new List<CodeProperty>();
                foreach (var property in propertiesWithoutSchema)
                {
                    OpenApiSchema openApiSchema = null;
                    if (property.Value.ValueKind == JsonValueKind.Object &&
                        property.Value.TryGetProperty("@odata.type", out var discriminatorValue) &&
                        snippetModelSchemas.TryGetValue(discriminatorValue.GetString()?.TrimStart('#') ?? string.Empty, out OpenApiSchema specifiedSchema))
                    {
                        openApiSchema = specifiedSchema; //property object may have a discriminator value that can be used to determine the schema for additionalData items.
                    }
                    additionalChildren.Add(parseProperty(property.Name, property.Value, openApiSchema,snippetModelSchemas));
                }
                if (additionalChildren.Count != 0)
                    children.Add(new CodeProperty { Name = "additionalData", PropertyType = PropertyType.Map, Children = additionalChildren });
            }

            return new CodeProperty
            {
                Name = rootPropertyName,
                PropertyType = PropertyType.Object,
                Children = children,
                TypeDefinition = schema.GetSchemaTitle()?.ToFirstCharacterUpperCase() ?? GetComposedSchema(schema).GetSchemaTitle()?.ToFirstCharacterUpperCase() ?? rootPropertyName,
                NamespaceName = GetNamespaceFromSchema(schema) ??  currentNode.GetNodeNamespaceFromPath(string.Empty)
            };
        }

        private static OpenApiSchema GetComposedSchema(OpenApiSchema schema)
        {
            if (schema == null)
                return null;

            var typesCount = schema.AnyOf?.Count ?? schema.OneOf?.Count ?? 0;
            if ((typesCount == 1 && schema.Nullable &&
                 schema.IsAnyOf()) || // nullable on the root schema outside of anyOf
                typesCount == 2 && schema.IsAnyOf() && schema.AnyOf.Any(static x => // nullable on a schema in the anyOf
                    x.Nullable &&
                    !x.Properties.Any() &&
                    !x.IsOneOf() &&
                    !x.IsAnyOf() &&
                    !x.IsAllOf() &&
                    !x.IsArray() &&
                    !x.IsReferencedSchema())) // once openAPI 3.1 is supported, there will be a third case oneOf with Ref and type null.
            {
                return schema.AnyOf.FirstOrDefault(static x => !string.IsNullOrEmpty(x.GetSchemaTitle()));
            }

            return null;
        }

        private static string GetNamespaceFromSchema(OpenApiSchema schema)
        {
            if (schema == null)
                return null;

            return GetModelsNamespaceNameFromReferenceId(schema.IsReferencedSchema() ? schema.Reference?.Id : GetComposedSchema(schema)?.Reference?.Id);
        }

        private static String escapeSpecialCharacters(string value)
        {
            return value?.EscapeQuotes()?.Replace("\n", "\\n")?.Replace("\r", "\\r");
        }

        private static string GetModelsNamespaceNameFromReferenceId(string referenceId) {
            if (string.IsNullOrEmpty(referenceId))
                return referenceId;

            referenceId = referenceId.Trim(NamespaceNameSeparator);
            var lastDotIndex = referenceId.LastIndexOf(NamespaceNameSeparator);
            var namespaceSuffix = lastDotIndex != -1 ? $".{referenceId[..lastDotIndex]}" : string.Empty;
            return $"models{namespaceSuffix}";
        }

        private static CodeProperty evaluateStringProperty(string propertyName, string value, OpenApiSchema propSchema)
        {
            if ((propSchema?.Type?.Equals("boolean", StringComparison.OrdinalIgnoreCase) ?? false))
                return new CodeProperty { Name = propertyName, Value = value, PropertyType = PropertyType.Boolean, Children = new List<CodeProperty>() };
            var formatString = propSchema?.Format;
            if (!string.IsNullOrEmpty(formatString) && _formatPropertyTypes.TryGetValue(formatString, out var type))
                return new CodeProperty { Name = propertyName, Value = value, PropertyType = type, Children = new List<CodeProperty>() };
            var enumSchema = propSchema?.AnyOf.FirstOrDefault(x => x.Enum.Count > 0);
            if ((propSchema?.Enum.Count ?? 0) == 0 && enumSchema == null)
                return new CodeProperty { Name = propertyName, Value = escapeSpecialCharacters(value), PropertyType = PropertyType.String, Children = new List<CodeProperty>() };
            enumSchema ??= propSchema;

            bool isFlagsEnum = enumSchema.Extensions.TryGetValue(OpenApiEnumFlagsExtension.Name, out var rawExtension) && rawExtension is OpenApiEnumFlagsExtension { IsFlags: true };

            // Pass the list of options in the enum as children so that the language generators may use them for validation if need be,
            var enumValueOptions = enumSchema?.Enum.Where(option => option is OpenApiString)
                                                                .Select(option => new CodeProperty{Name = ((OpenApiString)option).Value,Value = ((OpenApiString)option).Value,PropertyType = PropertyType.String})
                                                                .ToList() ?? new List<CodeProperty>();
            var propValue = String.IsNullOrWhiteSpace(value) ? $"{enumSchema?.Title.ToFirstCharacterUpperCase()}.{enumValueOptions.FirstOrDefault().Value.ToFirstCharacterUpperCase()}" : $"{enumSchema?.Title.ToFirstCharacterUpperCase()}.{value.ToFirstCharacterUpperCase()}";

            return new CodeProperty { Name = propertyName, Value = propValue, PropertyType = PropertyType.Enum, Children = enumValueOptions, NamespaceName = GetNamespaceFromSchema(enumSchema), isFlagsEnum = isFlagsEnum};
        }

        private static CodeProperty evaluateNumericProperty(string propertyName, JsonElement value, OpenApiSchema propSchema)
        {
             if(propSchema == null)
                return new CodeProperty { Name = propertyName, Value = $"{value}", PropertyType = PropertyType.Int32, Children = new List<CodeProperty>() };

             var schemas = (propSchema.AnyOf ?? Enumerable.Empty<OpenApiSchema>())
                 .Union(propSchema.AllOf ?? Enumerable.Empty<OpenApiSchema>())
                 .Union(propSchema.OneOf ?? Enumerable.Empty<OpenApiSchema>())
                 .ToList();

             schemas.Add(propSchema);

             var types = schemas.Select(item => item.Type).Where(static x => !string.IsNullOrEmpty(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
             var formats = schemas.Select(item => item.Format).Where(static x => !string.IsNullOrEmpty(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var (propertyType, propertyValue) = types switch
            {
                _ when (types.Contains("integer") || types.Contains("number")) && formats.Contains("int32") => (PropertyType.Int32 , value.GetInt32().ToString()),
                _ when (types.Contains("integer") || types.Contains("number")) && formats.Contains("int64") => (PropertyType.Int64 , value.GetInt64().ToString()),
                _ when formats.Contains("float")  || formats.Contains("float32")  => (PropertyType.Float32, value.GetDecimal().ToString()),
                _ when formats.Contains("float64") => (PropertyType.Float64, value.GetDecimal().ToString()),
                _ when formats.Contains("double") => (PropertyType.Double, value.GetDouble().ToString()), //in MS Graph float & double are any of number, string and enum
                _ => (PropertyType.Int32, $"{value.GetInt32()}"),
            };

            return new CodeProperty { Name = propertyName, Value = propertyValue, PropertyType = propertyType, Children = new List<CodeProperty>() };
        }

        private static CodeProperty parseProperty(string propertyName, JsonElement value, OpenApiSchema propSchema, IDictionary<string, OpenApiSchema> snippetModelSchemas)
        {
            if (propSchema?.IsUntypedNode() ?? false)
            {
                return ParseUntypedProperty(value, propertyName);
            }
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    return evaluateStringProperty(propertyName, value.GetString(), propSchema);
                case JsonValueKind.Number:
                    return evaluateNumericProperty(propertyName, value, propSchema);
                case JsonValueKind.False:
                case JsonValueKind.True:
                    return new CodeProperty { Name = propertyName, Value = value.GetBoolean().ToString(), PropertyType = PropertyType.Boolean, Children = new List<CodeProperty>() };
                case JsonValueKind.Null:
                    return new CodeProperty { Name = propertyName, Value = "null", PropertyType = PropertyType.Null, Children = new List<CodeProperty>() };
                case JsonValueKind.Object:
                    if (propSchema != null)
                        return parseJsonObjectValue(propertyName, value, propSchema,snippetModelSchemas);
                    else
                        return parseAnonymousObjectValues(propertyName, value, propSchema, snippetModelSchemas);
                case JsonValueKind.Array:
                    return parseJsonArrayValue(propertyName, value, propSchema, snippetModelSchemas);
                default:
                    throw new NotImplementedException($"Unsupported JsonValueKind: {value.ValueKind}");
            }
        }

        public const string UntypedNodeName = "UntypedNode";

        private static CodeProperty ParseUntypedProperty(JsonElement value, string propertyName = "")
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    return new CodeProperty { Name = propertyName, Value = value.GetString(), PropertyType = PropertyType.String, Children = new List<CodeProperty>() , TypeDefinition = UntypedNodeName};
                case JsonValueKind.Number:
                    return new CodeProperty { Name = propertyName, Value = value.GetDouble().ToString(), PropertyType = PropertyType.Double, Children = new List<CodeProperty>(), TypeDefinition = UntypedNodeName };
                case JsonValueKind.False:
                case JsonValueKind.True:
                    return new CodeProperty { Name = propertyName, Value = value.GetBoolean().ToString(), PropertyType = PropertyType.Boolean, Children = new List<CodeProperty>(), TypeDefinition = UntypedNodeName };
                case JsonValueKind.Null:
                    return new CodeProperty { Name = propertyName, Value = "null", PropertyType = PropertyType.Null, Children = new List<CodeProperty>(), TypeDefinition = UntypedNodeName };
                case JsonValueKind.Object:
                    var objectProperty = new CodeProperty { Name = propertyName, PropertyType = PropertyType.Object, Children = new List<CodeProperty>(), TypeDefinition = UntypedNodeName };
                    foreach (var jsonProp in value.EnumerateObject())
                    {
                        objectProperty.Children.Add(ParseUntypedProperty( jsonProp.Value, jsonProp.Name));
                    }
                    return objectProperty;
                case JsonValueKind.Array:
                    var arrayProperty = new CodeProperty { Name = propertyName, PropertyType = PropertyType.Array, Children = new List<CodeProperty>(), TypeDefinition = UntypedNodeName };
                    foreach (var jsonProp in value.EnumerateArray())
                    {
                        arrayProperty.Children.Add(ParseUntypedProperty(jsonProp));
                    }
                    return arrayProperty;
                case JsonValueKind.Undefined:
                default:
                    throw new NotSupportedException($"Unsupported Json value Kind {value.ValueKind}");
            }
        }
        private static CodeProperty parseJsonArrayValue(string propertyName, JsonElement value, OpenApiSchema schema, IDictionary<string, OpenApiSchema> snippetModelSchemas)
        {
            var alternativeType = schema?.Items?.AnyOf?.FirstOrDefault()?.AllOf?.LastOrDefault()?.Title;
            // uuid schemas
            var genericType = schema.GetSchemaTitle().ToFirstCharacterUpperCase() ??
                              (value.EnumerateArray().Any() ?
                                  evaluatePropertyTypeDefinition(value.EnumerateArray().First().ValueKind.ToString(), schema?.Items) :
                                  schema?.Items?.Type);

            var typeDefinition = string.IsNullOrEmpty(genericType) || genericType.Equals("Object", StringComparison.OrdinalIgnoreCase) // try to use alternativeType for objects if we couldn't find a useful name.
                ? alternativeType
                : genericType;
            var children = value.EnumerateArray().Select(item =>
            {
                var prop = parseProperty(schema.GetSchemaTitle() ?? alternativeType?.ToFirstCharacterUpperCase(), item, schema?.Items, snippetModelSchemas);
                prop.TypeDefinition ??= typeDefinition;
                return prop;
            }).ToList();
            return new CodeProperty { Name = propertyName, Value = null, PropertyType = PropertyType.Array, Children = children, TypeDefinition = typeDefinition ,NamespaceName = GetNamespaceFromSchema(schema?.Items) };
        }

        private static string evaluatePropertyTypeDefinition(String typeInfo, OpenApiSchema propSchema)
        {
            if (!typeInfo.Equals("String", StringComparison.CurrentCultureIgnoreCase))
                return typeInfo;

            if ((propSchema?.Type?.Equals("boolean", StringComparison.OrdinalIgnoreCase) ?? false))
                return "boolean";
            var formatString = propSchema?.Format;
            if (!string.IsNullOrEmpty(formatString) && _formatPropertyTypes.TryGetValue(formatString, out var type))
                return formatString;

            return typeInfo;
        }

        private static CodeProperty parseAnonymousObjectValues(string propertyName, JsonElement value, OpenApiSchema schema, IDictionary<string, OpenApiSchema> snippetModelSchemas)
        {
            if (value.ValueKind != JsonValueKind.Object) throw new InvalidOperationException($"Expected JSON object and got {value.ValueKind}");

            var children = new List<CodeProperty>();
            var propertiesAndSchema = value.EnumerateObject()
                                            .Select(x => new Tuple<JsonProperty, OpenApiSchema>(x, schema.GetPropertySchema(x.Name)));
            foreach (var propertyAndSchema in propertiesAndSchema)
            {
                children.Add(parseProperty(propertyAndSchema.Item1.Name.ToFirstCharacterLowerCase(), propertyAndSchema.Item1.Value, propertyAndSchema.Item2, snippetModelSchemas));
            }

            return new CodeProperty { Name = propertyName, Value = null, PropertyType = PropertyType.Object, Children = children };
        }

        public string AggregatePathParametersIntoString()
        {
            if (!PathParameters.Any())
                return string.Empty;
            var parameterString = PathParameters.Select(
                static s => $"With{s.Name.ToFirstCharacterUpperCase()}"
            ).Aggregate(
                static (a, b) => $"{a}{b}"
            );
            return parameterString;
        }
        public string GetSchemaFunctionCallPrefix(){
            var methodNameinPascalCase = HttpMethod.Method.ToLowerInvariant().ToFirstCharacterUpperCase();
            var functionNamePrefix = methodNameinPascalCase;
            //if codeGraph.ResponseSchema.Reference is null then recreate functionNamePrefix for inline schema with the following format
            //methodNameinPascalCase + "As" + functionName + methodNameinPascalCase + "Response"
            bool someNodeInPathHasReference = ResponseSchema?.AnyOf?.Any(static x => x.Reference is not null) ?? false;
            if (ResponseSchema?.Reference is null && !someNodeInPathHasReference)
            {
                var lastItemInPath = Nodes.Last();
                var functionName = new StringBuilder();
                if (lastItemInPath.Segment.Contains('.'))
                {
                    functionName.Append(
                        lastItemInPath.Segment.GetPartFunctionNameFromNameSpacedSegmentString()
                    );
                }
                else
                {
                    functionName.Append(lastItemInPath.Segment.Split('(')[0].ToFirstCharacterUpperCase());
                }
                var parameters = AggregatePathParametersIntoString();
                functionName = functionName.Append(parameters);
                functionNamePrefix = methodNameinPascalCase + "As" + functionName + methodNameinPascalCase + "Response";
            }
            return functionNamePrefix;
        }
    }

}
