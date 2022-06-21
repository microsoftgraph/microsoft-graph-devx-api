using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators;

public class PhpGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
{
    private const string clientVarName = "$graphClient";
    private const string clientVarType = "GraphClient";
    private const string httpCoreVarName = "$requestAdapter";
    public string GenerateCodeSnippet(SnippetModel snippetModel)
    {
        var indentManager = new IndentManager();
        var snippetBuilder = new StringBuilder(
            "//THIS SNIPPET IS A PREVIEW FOR THE KIOTA BASED SDK. NON-PRODUCTION USE ONLY" + Environment.NewLine +
            $"{clientVarName} = new {clientVarType}({httpCoreVarName});{Environment.NewLine}{Environment.NewLine}");
        var (requestPayload, payloadVarName) = GetRequestPayloadAndVariableName(snippetModel, indentManager);
        snippetBuilder.Append(requestPayload);
        var responseAssignment = "$result = ";
        // have a return type if we have a response schema that is not an error
        if (snippetModel.ResponseSchema == null || (snippetModel.ResponseSchema.Properties.Count == 1 && snippetModel.ResponseSchema.Properties.First().Key.Equals("error",StringComparison.OrdinalIgnoreCase)))
            responseAssignment = string.Empty;
        var requestConfiguration = GetRequestConfiguration(snippetModel, indentManager);
        if (!string.IsNullOrEmpty(requestConfiguration.Item1))
            snippetBuilder.AppendLine(requestConfiguration.Item1);
        var parametersList = GetActionParametersList(payloadVarName, requestConfiguration.Item2);
        var methodName = snippetModel.Method.ToString().ToLower();
        snippetBuilder.AppendLine($"{responseAssignment} {clientVarName}->{GetFluentApiPath(snippetModel.PathNodes)}->{methodName}({parametersList});");
        return snippetBuilder.ToString();
    }
    
    private const string queryParametersvarName = "queryParameters";
    private static (string, string) GetRequestQueryParameters(SnippetModel model, IndentManager indentManager, string requestConfigVarName) {
        var payloadSB = new StringBuilder();
        if(!string.IsNullOrEmpty(model.QueryString))
        {
            var className = $"{model.PathNodes.Last().GetClassName("RequestBuilder").ToFirstCharacterUpperCase()}{model.Method.ToString().ToLowerInvariant().ToFirstCharacterUpperCase()}QueryParameters";
            payloadSB.AppendLine($"${queryParametersvarName} = new {className}();");
            var (queryString, replacements) = ReplaceNestedOdataQueryParameters(model.QueryString);
            foreach(var queryParam in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)) {
                if(queryParam.Contains('=')) {
                    var kvPair = queryParam.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    payloadSB.AppendLine($"${queryParametersvarName}->{indentManager.GetIndent()}{NormalizeQueryParameterName(kvPair[0])} = {GetQueryParameterValue(kvPair[1], replacements)};");
                } else
                    payloadSB.AppendLine($"${queryParametersvarName}->{indentManager.GetIndent()}{NormalizeQueryParameterName(queryParam)} = \"\";");
            }
            indentManager.Unindent();
            payloadSB.AppendLine();
            return (payloadSB.ToString(), queryParametersvarName);
        }
        return (default, default);
    }
    
    private static Regex nestedStatementRegex = new(@"(\w+)(\([^)]+\))", RegexOptions.IgnoreCase);
    private static (string, Dictionary<string, string>) ReplaceNestedOdataQueryParameters(string queryParams) {
        var replacements = new Dictionary<string, string>();
        var matches = nestedStatementRegex.Matches(queryParams);
        if (!matches.Any())
            return (queryParams, replacements);
        
        foreach(Match match in matches) {
            var key = match.Groups[1].Value;
            var value = match.Groups[2].Value;
            replacements.Add(key, value);
            queryParams = queryParams.Replace(value, string.Empty);
        }
        return (queryParams, replacements);
    }
    
    private const string requestConfigurationVarName = "requestConfiguration";
    private static (string, string) GetRequestConfiguration(SnippetModel snippetModel, IndentManager indentManager)
    {
        var payloadSB = new StringBuilder();
        var queryParamsPayload = GetRequestQueryParameters(snippetModel, indentManager, requestConfigurationVarName);
        var requestHeadersPayload = GetRequestHeaders(snippetModel, indentManager);

        if (!string.IsNullOrEmpty(queryParamsPayload.Item1) || !string.IsNullOrEmpty(requestHeadersPayload.Item1))
        {
            var className = $"{snippetModel.PathNodes.Last().GetClassName("RequestBuilder").ToFirstCharacterUpperCase()}{snippetModel.Method.ToString().ToLowerInvariant().ToFirstCharacterUpperCase()}RequestConfiguration";
            payloadSB.AppendLine($"${requestConfigurationVarName} = new {className}();");
            payloadSB.AppendLine();
            payloadSB.Append(queryParamsPayload.Item1);
            payloadSB.Append(requestHeadersPayload.Item1);
            if (!string.IsNullOrEmpty(queryParamsPayload.Item1))
                payloadSB.AppendLine($"${requestConfigurationVarName}->queryParameters = ${queryParamsPayload.Item2};");
            if (!string.IsNullOrEmpty(requestHeadersPayload.Item1))
                payloadSB.AppendLine($"${requestConfigurationVarName}->headers = ${requestHeadersPayload.Item2};");
            payloadSB.AppendLine();
        }
        
        return (payloadSB.Length > 0 ? (payloadSB.ToString(), requestConfigurationVarName) : (default, default));
    }
    private static string GetQueryParameterValue(string originalValue, Dictionary<string, string> replacements)
    {
        if(originalValue.Equals("true", StringComparison.OrdinalIgnoreCase) || originalValue.Equals("false", StringComparison.OrdinalIgnoreCase))
            return originalValue.ToLowerInvariant();
        if(int.TryParse(originalValue, out var intValue))
            return intValue.ToString();
        var valueWithNested = originalValue.Split(',')
                .Select(v => replacements.ContainsKey(v) ? v + replacements[v] : v)
                .Aggregate((a, b) => $"{a},{b}");
        return $"'{valueWithNested.Replace("'", "\\'")}'";
    }
    private static string GetActionParametersList(params string[] parameters) {
        var nonEmptyParameters = parameters.Where(p => !string.IsNullOrEmpty(p));
        var emptyParameters = nonEmptyParameters.ToList();
        if(emptyParameters.Any())
            return string.Join(", ", emptyParameters.Select(x => $"${x}").Aggregate((a, b) => $"{a}, {b}"));
        return string.Empty;
    }
    
    private const string requestHeadersVarName = "headers";
    private static (string, string) GetRequestHeaders(SnippetModel snippetModel, IndentManager indentManager) {
        var payloadSB = new StringBuilder();
        var filteredHeaders = snippetModel.RequestHeaders.Where(h => !h.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if(filteredHeaders.Any()) {
            payloadSB.AppendLine($"{indentManager.GetIndent()}${requestHeadersVarName} = [");
            indentManager.Indent();
            filteredHeaders.ForEach(h =>
                payloadSB.AppendLine($"{indentManager.GetIndent()}\"{h.Key}\" => \"{h.Value.FirstOrDefault().EscapeQuotes()}\",")
            );
            indentManager.Unindent();
            payloadSB.AppendLine($"{indentManager.GetIndent()}];");
            payloadSB.AppendLine();
            return (payloadSB.ToString(), requestHeadersVarName);
        }
        return (default, default);
    }
    private static string NormalizeQueryParameterName(string queryParam) => queryParam.TrimStart('$').ToFirstCharacterLowerCase();
	private const string RequestBodyVarName = "requestRequestBody";
	private static (string, string) GetRequestPayloadAndVariableName(SnippetModel snippetModel, IndentManager indentManager) {
		if(string.IsNullOrWhiteSpace(snippetModel?.RequestBody))
			return (default, default);
		if(indentManager == null) throw new ArgumentNullException(nameof(indentManager));

		var payloadSB = new StringBuilder();
        switch (snippetModel.ContentType?.Split(';').First().ToLowerInvariant()) {
			case "application/json":
				TryParseBody(snippetModel, payloadSB, indentManager);
			break;
			case "application/octet-stream":
				payloadSB.AppendLine($"{RequestBodyVarName} = Utils::streamFrom(''); //stream to upload");
			break;
			default:
                if(TryParseBody(snippetModel, payloadSB, indentManager)) //in case the content type header is missing but we still have a json payload
                    break;
                else
					throw new InvalidOperationException($"Unsupported content type: {snippetModel.ContentType}");
		}
		var result = payloadSB.ToString();
		return (result, string.IsNullOrEmpty(result) ? string.Empty : RequestBodyVarName);
	}
    private static bool TryParseBody(SnippetModel snippetModel, StringBuilder payloadSB, IndentManager indentManager) {
        if(snippetModel.IsRequestBodyValid)
            try {
                using var parsedBody = JsonDocument.Parse(snippetModel.RequestBody, new JsonDocumentOptions { AllowTrailingCommas = true });
                var schema = snippetModel.RequestSchema;
                var className = schema.GetSchemaTitle().ToFirstCharacterUpperCase();
                if (string.IsNullOrEmpty(className) && schema != null && schema.Properties.Count == 1)
                    className = $"{schema.Properties.First().Key.ToFirstCharacterUpperCase()}RequestBody"; // edge case for odata actions with a single parameter
                if (string.IsNullOrEmpty(className))
                    className = $"{snippetModel.ResponseVariableName.ToFirstCharacterUpperCase()}RequestBody";
                payloadSB.AppendLine($"${RequestBodyVarName.ToFirstCharacterLowerCase()} = new {className}();");
                payloadSB.AppendLine();
                WriteJsonObjectValue(payloadSB, parsedBody.RootElement, schema, indentManager, true, RequestBodyVarName);
            } catch (Exception ex) when (ex is JsonException || ex is ArgumentException) {
                // the payload wasn't json or poorly formatted
            }
        return false;
    }
    private static void WriteJsonObjectValue(StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager, bool includePropertyAssignment = true, string variableName = default) 
    {
		if (value.ValueKind != JsonValueKind.Object) throw new InvalidOperationException($"Expected JSON object and got {value.ValueKind}");
		var propertiesAndSchema = value.EnumerateObject()
										.Select(x => new Tuple<JsonProperty, OpenApiSchema>(x, schema.GetPropertySchema(x.Name)));
        var andSchema = propertiesAndSchema.ToList();
        int i = 0;
        payloadSB.AppendLine();
        foreach(var propertyAndSchema in andSchema.Where(x => x.Item2 != null)) {
			var propertyName = propertyAndSchema.Item1.Name.ToFirstCharacterUpperCase();
            var propertyAssignment = includePropertyAssignment ? $"${variableName.ToFirstCharacterLowerCase()}->set{propertyName}(" : string.Empty;
			WriteProperty(payloadSB, propertyAndSchema.Item1.Value, propertyAndSchema.Item2, indentManager, propertyAssignment, ");", propertyName ?? $"{variableName}{++i}");
		}
		var propertiesWithoutSchema = andSchema.Where(x => x.Item2 == null).Select(x => x.Item1);
        var jsonProperties = propertiesWithoutSchema.ToList();
        if(jsonProperties.Any())
        {
            var additionalDataVarName = $"${variableName.ToFirstCharacterLowerCase()}AdditionalData";
			payloadSB.AppendLine($"{additionalDataVarName} = [");
            indentManager.Indent();
            foreach(var property in jsonProperties) {
				var propertyAssignment = $"{indentManager.GetIndent()}\"{property.Name}\" => ";
				WriteProperty(payloadSB, property.Value, null, indentManager, propertyAssignment , ",", variableName);
			}
			indentManager.Unindent();
            payloadSB.AppendLine($"{indentManager.GetIndent()}];");
            payloadSB.AppendLine($"${variableName.ToFirstCharacterLowerCase()}->setAdditionalData({additionalDataVarName});");
        }
        
		indentManager.Unindent();
    }
	private static void WriteProperty(StringBuilder payloadSB, JsonElement value, OpenApiSchema propSchema, IndentManager indentManager, string propertyAssignment, string propertySuffix = default, string propertyName = default, Action<string> func = default, bool fromCollection = false)
    {
        func ??= delegate(string s)
        {
            payloadSB.AppendLine(s);
        };
        switch (value.ValueKind) {
			case JsonValueKind.String:
				if(propSchema?.Format?.Equals("base64url", StringComparison.OrdinalIgnoreCase) ?? false)
					func.Invoke($"{propertyAssignment}base64_decode(\"{value.GetString()}\"){propertySuffix});");
				else if (propSchema?.Format?.Equals("date-time", StringComparison.OrdinalIgnoreCase) ?? false)
					func.Invoke($"{propertyAssignment}new DateTime(\"{value.GetString()}\"){propertySuffix}");
				else
					func.Invoke($"{propertyAssignment}'{value.GetString()?.Replace("'", "\\'")}'{propertySuffix}");
				break;
			case JsonValueKind.Number:
				func.Invoke($"{propertyAssignment}{value.GetInt64()}{propertySuffix}");
				break;
			case JsonValueKind.False:
			case JsonValueKind.True:
				func.Invoke($"{propertyAssignment}{value.GetBoolean()}{propertySuffix}");
				break;
			case JsonValueKind.Null:
				func.Invoke($"{propertyAssignment}null{propertySuffix}");
				break;
            case JsonValueKind.Object:
				if(propSchema != null)
                {
                    payloadSB.AppendLine();
					func.Invoke($"${propertyName?.ToFirstCharacterLowerCase()} = new {propSchema.GetSchemaTitle().ToFirstCharacterUpperCase()}();");
                    if (!fromCollection) 
                        payloadSB.AppendLine($"{propertyAssignment}${propertyName.ToFirstCharacterLowerCase()});");
                    payloadSB.AppendLine();
                    WriteJsonObjectValue(payloadSB, value, propSchema, indentManager, true, propertyName);
                    payloadSB.AppendLine();
                }
				break;
			case JsonValueKind.Array:
				WriteJsonArrayValue(payloadSB, value, propSchema, indentManager, propertyAssignment, propertyName);
			break;
			default:
				throw new NotImplementedException($"Unsupported JsonValueKind: {value.ValueKind}");
        }
	}
    private static void WriteJsonArrayValue(StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager, string propertyAssignment, string propertyName = default)
    {
        var genericType = schema.GetSchemaTitle().ToFirstCharacterUpperCase();
        var hasSchema = !string.IsNullOrEmpty(genericType);
        var arrayName = $"{propertyName.ToFirstCharacterLowerCase()}Array";
        if (hasSchema) 
            payloadSB.AppendLine($"${arrayName} = [];");
        else 
            payloadSB.AppendLine($"{propertyAssignment} ["); 
        indentManager.Indent();
        int i = 0;
        foreach (var item in value.EnumerateArray())
        {
            var propName = $"{propertyName.ToFirstCharacterLowerCase()}";
            WriteProperty(payloadSB, item, schema, indentManager, indentManager.GetIndent(), ",",
                $"{propName}{++i}", s => payloadSB.Append(s.Trim()), true);
            if (hasSchema) 
                payloadSB.AppendLine($"${arrayName} []= ${propName}{i};");
        }

        indentManager.Unindent();
        if (!hasSchema)
            payloadSB.AppendLine($"{indentManager.GetIndent()}],");
        else
            payloadSB.AppendLine($"{propertyAssignment}${arrayName});");
    }
    
    private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes)
    {
        var openApiUrlTreeNodes = nodes.ToList();
        if (!(openApiUrlTreeNodes?.Any() ?? false)) return string.Empty;
        return openApiUrlTreeNodes.Select(x => {
                if (x.Segment.IsCollectionIndex())
                    return $"ById{x.Segment.Replace("{", "('").Replace("}", "')")}";
                if (x.Segment.IsFunction())
                    return x.Segment.Split('.').Last().ToFirstCharacterLowerCase()+"()";
                return x.Segment.ToFirstCharacterLowerCase()+"()";
            })
            .Aggregate((x, y) => {
                var dot = y.StartsWith("ById") ?
                    string.Empty :
                    "->";
                return $"{x}{dot}{y}";
            }).Replace("()ById(", "ById(");
    }
}
