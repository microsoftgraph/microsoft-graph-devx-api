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

        var (queryParamsPayload, queryParamsVarName) = GetRequestQueryParameters(snippetModel, indentManager);
        if(!string.IsNullOrEmpty(queryParamsPayload))
            snippetBuilder.Append(queryParamsPayload);
        var (requestHeadersPayload, requestHeadersVarName) = GetRequestHeaders(snippetModel, indentManager);
        if(!string.IsNullOrEmpty(requestHeadersPayload))
            snippetBuilder.Append(requestHeadersPayload);
        var parametersList = GetActionParametersList(payloadVarName, queryParamsVarName, requestHeadersVarName);
        var methodName = snippetModel.Method.ToString().ToLower();
        snippetBuilder.AppendLine($"{responseAssignment} {clientVarName}->{GetFluentApiPath(snippetModel.PathNodes)}->{methodName}({parametersList});");
        return snippetBuilder.ToString();
    }
    
    private const string requestParametersVarName = "$requestParameters";
    private static (string, string) GetRequestQueryParameters(SnippetModel model, IndentManager indentManager) {
        var payloadSB = new StringBuilder();
        if(!string.IsNullOrEmpty(model.QueryString)) {
            payloadSB.AppendLine($"{indentManager.GetIndent()}var {requestParametersVarName} = (q) =>");
            payloadSB.AppendLine($"{indentManager.GetIndent()}{{");
            indentManager.Indent();
            var (queryString, replacements) = ReplaceNestedOdataQueryParameters(model.QueryString);
            foreach(var queryParam in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)) {
                if(queryParam.Contains("=")) {
                    var kvPair = queryParam.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    payloadSB.AppendLine($"q.{indentManager.GetIndent()}{NormalizeQueryParameterName(kvPair[0])} = {GetQueryParameterValue(kvPair[1], replacements)};");
                } else
                    payloadSB.AppendLine($"q.{indentManager.GetIndent()}{NormalizeQueryParameterName(queryParam)} = string.Empty;");
            }
            indentManager.Unindent();
            payloadSB.AppendLine($"{indentManager.GetIndent()}}};");
            return (payloadSB.ToString(), requestParametersVarName);
        }
        return (default, default);
    }
    
    private static Regex nestedStatementRegex = new(@"(\w+)(\([^)]+\))", RegexOptions.IgnoreCase);
    private static (string, Dictionary<string, string>) ReplaceNestedOdataQueryParameters(string queryParams) {
        var replacements = new Dictionary<string, string>();
        var matches = nestedStatementRegex.Matches(queryParams);
        if(matches.Any())
            foreach(Match match in matches) {
                var key = match.Groups[1].Value;
                var value = match.Groups[2].Value;
                replacements.Add(key, value);
                queryParams = queryParams.Replace(value, string.Empty);
            }
        return (queryParams, replacements);
    }
    
    private static string GetQueryParameterValue(string originalValue, Dictionary<string, string> replacements) {
        if(originalValue.Equals("true", StringComparison.OrdinalIgnoreCase) || originalValue.Equals("false", StringComparison.OrdinalIgnoreCase))
            return originalValue.ToLowerInvariant();
        else if(int.TryParse(originalValue, out var intValue))
            return intValue.ToString();
        else {
            var valueWithNested = originalValue.Split(',')
                .Select(v => replacements.ContainsKey(v) ? v + replacements[v] : v)
                .Aggregate((a, b) => $"{a},{b}");
            return $"\"{valueWithNested}\"";
        }
    }
    private static string GetActionParametersList(params string[] parameters) {
        var nonEmptyParameters = parameters.Where(p => !string.IsNullOrEmpty(p));
        if(nonEmptyParameters.Any())
            return string.Join(", ", nonEmptyParameters.Aggregate((a, b) => $"{a}, {b}"));
        else return string.Empty;
    }
    
    private const string requestHeadersVarName = "$headers";
    private static (string, string) GetRequestHeaders(SnippetModel snippetModel, IndentManager indentManager) {
        var payloadSB = new StringBuilder();
        var filteredHeaders = snippetModel.RequestHeaders.Where(h => !h.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if(filteredHeaders.Any()) {
            payloadSB.AppendLine($"{indentManager.GetIndent()}var {requestHeadersVarName} = (h) =>");
            payloadSB.AppendLine($"{indentManager.GetIndent()}{{");
            indentManager.Indent();
            filteredHeaders.ForEach(h =>
                payloadSB.AppendLine($"{indentManager.GetIndent()}h.Add(\"{h.Key}\", \"{h.Value.FirstOrDefault()}\");")
            );
            indentManager.Unindent();
            payloadSB.AppendLine($"{indentManager.GetIndent()}}};");
            return (payloadSB.ToString(), requestHeadersVarName);
        }
        return (default, default);
    }
    private static string NormalizeQueryParameterName(string queryParam) => queryParam.TrimStart('$').ToFirstCharacterUpperCase();
	private const string RequestBodyVarName = "body";
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
				payloadSB.AppendLine($"{RequestBodyVarName} = new MemoryStream(); //stream to upload");
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
                payloadSB.AppendLine($"${RequestBodyVarName} = new {className}();");
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
        //indentManager.Indent();
		var propertiesAndSchema = value.EnumerateObject()
										.Select(x => new Tuple<JsonProperty, OpenApiSchema>(x, schema.GetPropertySchema(x.Name)));
		foreach(var propertyAndSchema in propertiesAndSchema.Where(x => x.Item2 != null)) {
			var propertyName = propertyAndSchema.Item1.Name.ToFirstCharacterUpperCase();
			var propertyAssignment = includePropertyAssignment ? $"${variableName.ToFirstCharacterLowerCase()}->set{indentManager.GetIndent()}{propertyName}(" : string.Empty;
			WriteProperty(payloadSB, propertyAndSchema.Item1.Value, propertyAndSchema.Item2, indentManager, propertyAssignment, string.Empty, propertyName);
		}
		var propertiesWithoutSchema = propertiesAndSchema.Where(x => x.Item2 == null).Select(x => x.Item1);
		if(propertiesWithoutSchema.Any()) {
			payloadSB.AppendLine($"{indentManager.GetIndent()}$additionalData = [");
            payloadSB.AppendLine($"{indentManager.GetIndent()}[");
			indentManager.Indent();
			foreach(var property in propertiesWithoutSchema) {
				var propertyAssignment = $"{indentManager.GetIndent()}[\"{property.Name}\", ";
				WriteProperty(payloadSB, property.Value, null, indentManager, propertyAssignment , "]");
			}
			indentManager.Unindent();
			payloadSB.AppendLine($"{indentManager.GetIndent()}]");
		}
        
		indentManager.Unindent();
    }
	private static void WriteProperty(StringBuilder payloadSB, JsonElement value, OpenApiSchema propSchema, IndentManager indentManager, string propertyAssignment, string propertySuffix = default, string propertyName = default) {
		switch (value.ValueKind) {
			case JsonValueKind.String:
				if(propSchema?.Format?.Equals("base64url", StringComparison.OrdinalIgnoreCase) ?? false)
					payloadSB.AppendLine($"{propertyAssignment}base64_decode(\"{value.GetString()}\"){propertySuffix});");
				else if (propSchema?.Format?.Equals("date-time", StringComparison.OrdinalIgnoreCase) ?? false)
					payloadSB.AppendLine($"{propertyAssignment}new DateTime(\"{value.GetString()}\"){propertySuffix});");
				else
					payloadSB.AppendLine($"{propertyAssignment}\"{value.GetString()}\"{propertySuffix});");
				break;
			case JsonValueKind.Number:
				payloadSB.AppendLine($"{propertyAssignment}{propertySuffix});");
				break;
			case JsonValueKind.False:
			case JsonValueKind.True:
				payloadSB.AppendLine($"{propertyAssignment}{value.GetBoolean().ToString().ToLowerInvariant()}{propertySuffix});");
				break;
			case JsonValueKind.Null:
				payloadSB.AppendLine($"{propertyAssignment}null{propertySuffix});");
				break;
			case JsonValueKind.Object:
				if(propSchema != null)
                {
                    payloadSB.AppendLine();
					payloadSB.AppendLine($"${propertyName?.ToFirstCharacterLowerCase()} = new {propSchema.GetSchemaTitle().ToFirstCharacterUpperCase()}();");
                    payloadSB.AppendLine($"{propertyAssignment}${propertyName.ToFirstCharacterLowerCase()});");
                    WriteJsonObjectValue(payloadSB, value, propSchema, indentManager, true, propertyName);
                    payloadSB.AppendLine();
                }
				break;
			case JsonValueKind.Array:
				WriteJsonArrayValue(payloadSB, value, propSchema, indentManager, propertyAssignment);
			break;
			default:
				throw new NotImplementedException($"Unsupported JsonValueKind: {value.ValueKind}");
		}
	}
    private static void WriteJsonArrayValue(StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager, string propertyAssignment) 
    {
        var genericType = schema.GetSchemaTitle().ToFirstCharacterUpperCase() ?? value.EnumerateArray().First().ValueKind.ToString();
        payloadSB.AppendLine($"{propertyAssignment} []");
        payloadSB.AppendLine($"{indentManager.GetIndent()}[");
        indentManager.Indent();
        foreach(var item in value.EnumerateArray())
            WriteProperty(payloadSB, item, schema, indentManager, indentManager.GetIndent());
        indentManager.Unindent();
        payloadSB.AppendLine($"{indentManager.GetIndent()}]");
    }
    
    private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes)
    {
        if (!(nodes?.Any() ?? false)) return string.Empty;
        return nodes.Select(x => {
                if (x.Segment.IsCollectionIndex())
                    return $"ById{x.Segment.Replace("{", "('").Replace("}", "')")}";
                if (x.Segment.IsFunction())
                    return x.Segment.Split('.').Last().ToFirstCharacterLowerCase();
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
