using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Extensions;
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
        var codeGraph = new SnippetCodeGraph(snippetModel);
        var payloadSb = new StringBuilder(
            "//THIS SNIPPET IS A PREVIEW FOR THE KIOTA BASED SDK. NON-PRODUCTION USE ONLY" + Environment.NewLine +
            $"{clientVarName} = new {clientVarType}({httpCoreVarName});{Environment.NewLine}{Environment.NewLine}");
        if (codeGraph.HasBody())
        {
            WriteObjectProperty(RequestBodyVarName, payloadSb, codeGraph.Body, indentManager);
        }

        payloadSb.AppendLine($"{GetFluentApiPath(codeGraph.Nodes)};");

        return payloadSb.ToString();
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
    
    private static readonly Regex NestedStatementRegex = new(@"(\w+)(\([^)]+\))", RegexOptions.IgnoreCase);
    private static (string, Dictionary<string, string>) ReplaceNestedOdataQueryParameters(string queryParams) {
        var replacements = new Dictionary<string, string>();
        var matches = NestedStatementRegex.Matches(queryParams);
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
    
    private const string RequestConfigurationVarName = "requestConfiguration";
    private static (string, string) GetRequestConfiguration(SnippetModel snippetModel, IndentManager indentManager)
    {
        var payloadSb = new StringBuilder();
        var queryParamsPayload = GetRequestQueryParameters(snippetModel, indentManager, RequestConfigurationVarName);
        var requestHeadersPayload = GetRequestHeaders(snippetModel, indentManager);

        if (!string.IsNullOrEmpty(queryParamsPayload.Item1) || !string.IsNullOrEmpty(requestHeadersPayload.Item1))
        {
            var className = $"{snippetModel.PathNodes.Last().GetClassName("RequestBuilder").ToFirstCharacterUpperCase()}{snippetModel.Method.ToString().ToLowerInvariant().ToFirstCharacterUpperCase()}RequestConfiguration";
            payloadSb.AppendLine($"${RequestConfigurationVarName} = new {className}();");
            payloadSb.AppendLine();
            payloadSb.Append(queryParamsPayload.Item1);
            payloadSb.Append(requestHeadersPayload.Item1);
            if (!string.IsNullOrEmpty(queryParamsPayload.Item1))
                payloadSb.AppendLine($"${RequestConfigurationVarName}->queryParameters = ${queryParamsPayload.Item2};");
            if (!string.IsNullOrEmpty(requestHeadersPayload.Item1))
                payloadSb.AppendLine($"${RequestConfigurationVarName}->headers = ${requestHeadersPayload.Item2};");
            payloadSb.AppendLine();
        }
        
        return (payloadSb.Length > 0 ? (payloadSb.ToString(), RequestConfigurationVarName) : (default, default));
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
    private static string NormalizeQueryParameterName(string queryParam) => queryParam?.TrimStart('$').ToFirstCharacterLowerCase();
	private const string RequestBodyVarName = "requestBody";
    private static void WriteObjectProperty(string propertyAssignment, StringBuilder payloadSb, CodeProperty codeProperty, IndentManager indentManager, string childPropertyName = default)
    {
        var childPosition = 0;
        payloadSb.AppendLine($"${(childPropertyName ?? propertyAssignment).ToFirstCharacterLowerCase()} = new {codeProperty.TypeDefinition.ToFirstCharacterUpperCase()}();");
        foreach(CodeProperty child in codeProperty.Children)
        {
            var newChildName = (childPropertyName ?? "") + child.Name.ToFirstCharacterUpperCase();
            WriteCodeProperty(childPropertyName ?? propertyAssignment, payloadSb, codeProperty, child, indentManager, ++childPosition, newChildName);
            if (child.PropertyType != PropertyType.Object) 
                payloadSb.AppendLine();
        }
        payloadSb.AppendLine();
    }
    private static void WriteCodeProperty(string propertyAssignment, StringBuilder payloadSb, CodeProperty parent, CodeProperty child, IndentManager indentManager, int childPosition = 0, string childPropertyName = default)
    {
        Action<string> func;
        var propertySuffix = string.Empty;
        func = delegate(string s)
        {
            payloadSb.AppendLine(s);
        };
        var isArray = child.PropertyType == PropertyType.Array;
        var isMap = child.PropertyType == PropertyType.Map;
        var fromArray = parent.PropertyType == PropertyType.Array;

        var propertyName = NormalizeQueryParameterName(child.Name.ToFirstCharacterLowerCase());
        var objectName = isArray ? propertyName?.IndexSuffix(childPosition) : propertyName;
        switch (child.PropertyType) {
			case PropertyType.String:
                WriteStringProperty(propertyAssignment, parent, payloadSb, indentManager, child);
                break;
			case PropertyType.Int32:
            case PropertyType.Int64:
            case PropertyType.Double:
            case PropertyType.Float32:
            case PropertyType.Float64:
                payloadSb.AppendLine($"{indentManager.GetIndent()}${propertyAssignment.ToFirstCharacterLowerCase()}->set{propertyName.ToFirstCharacterUpperCase()}({objectName});");
				break;
			case PropertyType.Boolean:
                payloadSb.AppendLine($"{indentManager.GetIndent()}${propertyAssignment.ToFirstCharacterLowerCase()}->set{propertyName.ToFirstCharacterUpperCase()}({child.Value.ToFirstCharacterLowerCase()});");
				break;
			case PropertyType.Null:
                //WriteNullProperty(propertyAssignment, parent, payloadSb, indentManager, child);
                break;
            case PropertyType.Object: 
                WriteObjectProperty(propertyAssignment.ToFirstCharacterLowerCase(), payloadSb, child, indentManager, childPropertyName);
                if (!fromArray)
                    payloadSb.AppendLine(
                        $"${propertyAssignment.ToFirstCharacterLowerCase()}->set{child.Name.ToFirstCharacterUpperCase()}(${(childPropertyName ?? propertyName).ToFirstCharacterLowerCase()});");
                break;
			case PropertyType.Array:
				WriteArrayProperty(propertyAssignment.ToFirstCharacterLowerCase(), child.Name, payloadSb, parent, child, indentManager); 
                break;
            case PropertyType.Enum:
                WriteEnumValue(payloadSb, propertyAssignment.ToFirstCharacterLowerCase(), parent, child);
                break;
            case PropertyType.Base64Url:
                WriteBase64Url(payloadSb);
                break;
            case PropertyType.Map:
                break;
			default:
				throw new NotImplementedException($"Unsupported PropertyType: {child.PropertyType.GetDisplayName()}");
        }
	}
    private static void WriteArrayProperty(string propertyAssignment, string objectName, StringBuilder payloadSb, CodeProperty parentProperty, CodeProperty codeProperty, IndentManager indentManager)
    {
        var genericType = codeProperty.TypeDefinition.ToFirstCharacterUpperCase();
        var hasSchema = parentProperty.PropertyType == PropertyType.Object;
        var arrayName = $"{objectName.ToFirstCharacterLowerCase()}Array";
        if (hasSchema) 
            payloadSb.AppendLine($"${arrayName} = [];");
        else 
            payloadSb.Append($"{propertyAssignment}[");
        int childPosition = 0;
        foreach (var property in codeProperty.Children)
        {
            var childPropertyName = $"{codeProperty.Name.ToFirstCharacterLowerCase()}{property.Name.ToFirstCharacterUpperCase()}{++childPosition}";
            WriteCodeProperty(propertyAssignment, payloadSb, codeProperty, property, indentManager, childPosition, childPropertyName);
            if (property.PropertyType == PropertyType.Object && codeProperty.PropertyType == PropertyType.Array)
            {
                payloadSb.AppendLine($"${arrayName} []= ${childPropertyName};");
            }
        }

        indentManager.Unindent();
    }

    private static void WriteEnumValue(StringBuilder payloadSb,string parentPropertyName, CodeProperty parent, CodeProperty currentProperty)
    {
        var enumParts = currentProperty.Value.Split('.');
        var enumClass = enumParts.First();
        var enumValue = enumParts.Last().ToLower();
        payloadSb.AppendLine(
            $"${parentPropertyName}->set{currentProperty.Name.ToFirstCharacterUpperCase()}(new {enumClass}('{enumValue}'));");
    }

    private static void WriteBase64Url(StringBuilder payloadSb)
    {
        
    }

    private static void WriteStringProperty(string propertyAssignment, CodeProperty parent, StringBuilder payloadSb, IndentManager indentManager, CodeProperty codeProperty)
    {
        var fromObject = parent.PropertyType == PropertyType.Object;

        if (fromObject)
        {
            payloadSb.AppendLine(
                $"${propertyAssignment.ToFirstCharacterLowerCase()}->set{NormalizeQueryParameterName(codeProperty.Name)?.ToFirstCharacterUpperCase()}('{codeProperty.Value.EscapeQuotesInLiteral("\"", "\\'")}');");
        }
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
                return $"{x.Trim('$')}{dot}{y.Trim('$')}";
            }).Replace("()ById(", "ById(");
    }
}
