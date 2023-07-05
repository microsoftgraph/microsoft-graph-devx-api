using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators;

public class PhpGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
{
    private const string ClientVarName = "$graphServiceClient";
    private const string ClientVarType = "GraphServiceClient";
    private const string HttpCoreVarName = "$requestAdapter";
    private const string ScopesVarName = "$scopes";
    private const string TokenContextVarName = "$tokenRequestContext";
    private const string RequestBodyVarName = "requestBody";
    private const string QueryParametersVarName = "queryParameters";
    private const string RequestConfigurationVarName = "requestConfiguration";

    public string GenerateCodeSnippet(SnippetModel snippetModel)
    {
        var indentManager = new IndentManager();
        var codeGraph = new SnippetCodeGraph(snippetModel);
        var payloadSb = new StringBuilder(
            "<?php" + Environment.NewLine + Environment.NewLine +
            "// THIS SNIPPET IS A PREVIEW FOR THE KIOTA BASED SDK. NON-PRODUCTION USE ONLY" + Environment.NewLine +
            $"{ClientVarName} = new {ClientVarType}({TokenContextVarName}, {ScopesVarName});{Environment.NewLine}{Environment.NewLine}");
        if (codeGraph.HasBody())
        {
            WriteObjectProperty(RequestBodyVarName, payloadSb, codeGraph.Body, indentManager);
        }
        WriteRequestExecutionPath(codeGraph, payloadSb, indentManager);
        return payloadSb.ToString();
    }

    private static void WriteRequestExecutionPath(SnippetCodeGraph codeGraph, StringBuilder payloadSb, IndentManager indentManager)
    {
        var method = codeGraph.HttpMethod.Method.ToLower();
        var configParameter = codeGraph.HasHeaders() || codeGraph.HasParameters()
            ? $"{RequestConfigurationVarName}"
            : string.Empty;
        var bodyParameter = codeGraph.HasBody()
            ? $"{RequestBodyVarName}"
            : string.Empty;
        var optionsParameter = codeGraph.HasOptions() ? "options" : string.Empty;
        var returnVar = codeGraph.HasReturnedBody() ? "$result = " : string.Empty;
        var parameterList = GetActionParametersList(bodyParameter, configParameter, optionsParameter);
        payloadSb.AppendLine(GetRequestConfiguration(codeGraph, indentManager));
        payloadSb.AppendLine($"{returnVar}{ClientVarName}->{GetFluentApiPath(codeGraph.Nodes)}->{method}({parameterList});");
    }
    private static string GetRequestQueryParameters(SnippetCodeGraph model, string configClassName) 
    {
        var payloadSb = new StringBuilder();
        if (!model.HasParameters()) return default;
        
        payloadSb.AppendLine($"${QueryParametersVarName} = {configClassName}::createQueryParameters();");
        var requestQueryParameters = model.Parameters.ToDictionary(x => x.Name);
        
        foreach(var (key, value) in requestQueryParameters)
        {
            payloadSb.AppendLine(
                $"${QueryParametersVarName}->{NormalizeVariableName(key)} = {EvaluateParameter(value)};");
        }

        payloadSb.AppendLine($"${RequestConfigurationVarName}->queryParameters = ${QueryParametersVarName};");
        return payloadSb.ToString();

    }
    
    private static string EvaluateParameter(CodeProperty param)
    {
        return param.PropertyType switch
        {
            PropertyType.Array =>
                $"[{string.Join(",", param.Children.Select(static x => $"\"{x.Value}\"").ToList())}]",
            PropertyType.Boolean or PropertyType.Int32 or PropertyType.Double or PropertyType.Float32 or PropertyType.Float64 or PropertyType.Int64 => param.Value,
            _ => $"\"{param.Value.EscapeQuotes()}\""
        };
    }
    private static string GetRequestConfiguration(SnippetCodeGraph codeGraph, IndentManager indentManager)
    {
        var payloadSb = new StringBuilder();

        if (codeGraph.HasParameters() || codeGraph.HasHeaders() || codeGraph.HasOptions())
        {
            var className = $"{codeGraph.Nodes.Last().GetClassName("RequestBuilder").ToFirstCharacterUpperCase()}{codeGraph.HttpMethod.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}RequestConfiguration";
            payloadSb.AppendLine($"${RequestConfigurationVarName} = new {className}();");
            var requestHeadersPayload = GetRequestHeaders(codeGraph, indentManager);
            var queryParamsPayload = GetRequestQueryParameters(codeGraph, className);
            if (!string.IsNullOrEmpty(requestHeadersPayload))
                payloadSb.AppendLine($"{requestHeadersPayload}");
            if (!string.IsNullOrEmpty(queryParamsPayload))
                payloadSb.AppendLine($"{queryParamsPayload}");
        }
        
        return (payloadSb.Length > 0 ? payloadSb.ToString() : default);
    }
    private static string GetActionParametersList(params string[] parameters) {
        var nonEmptyParameters = parameters.Where(static p => !string.IsNullOrEmpty(p));
        var emptyParameters = nonEmptyParameters.ToList();
        if(emptyParameters.Any())
            return string.Join(", ", emptyParameters.Select(static x => $"${x}").Aggregate(static (a, b) => $"{a}, {b}"));
        return string.Empty;
    }
    
    private static string GetRequestHeaders(SnippetCodeGraph snippetModel, IndentManager indentManager) {
        var payloadSb = new StringBuilder();
        var filteredHeaders = snippetModel.Headers?.Where(static h => !h.Name.Equals("Host", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if(filteredHeaders != null && filteredHeaders.Any()) {
            payloadSb.AppendLine("$headers = [");
            indentManager.Indent(2);
            filteredHeaders.ForEach(h =>
                payloadSb.AppendLine($"{indentManager.GetIndent()}'{h.Name}' => '{h.Value.Replace("\'", "\\'")}',")
            );
            indentManager.Unindent();
            payloadSb.AppendLine($"{indentManager.GetIndent()}];");
            payloadSb.AppendLine($"${RequestConfigurationVarName}->headers = $headers;");
            return payloadSb.ToString();
        }
        return default;
    }
    private static string NormalizeQueryParameterName(string queryParam) => queryParam?.TrimStart('$').ToFirstCharacterLowerCase();
    private static void WriteObjectProperty(string propertyAssignment, StringBuilder payloadSb, CodeProperty codeProperty, IndentManager indentManager, string childPropertyName = default, SnippetCodeGraph codeGraph = default)
    {
        var childPosition = 0;
        var objectType = (codeProperty.TypeDefinition ?? codeProperty.Name).ToFirstCharacterUpperCase();
        payloadSb.AppendLine($"${(childPropertyName ?? propertyAssignment).ToFirstCharacterLowerCase()} = new {objectType}();");
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
        var isArray = parent.PropertyType == PropertyType.Array;
        var isMap = parent.PropertyType == PropertyType.Map;
        var fromArray = parent.PropertyType == PropertyType.Array;

        var propertyName = NormalizeQueryParameterName(child.Name.ToFirstCharacterLowerCase());
        switch (child.PropertyType) {
			case PropertyType.String:
                WriteStringProperty(propertyAssignment, parent, payloadSb, indentManager, child);
                break;
			case PropertyType.Int32:
            case PropertyType.Int64:
            case PropertyType.Double:
            case PropertyType.Float32:
            case PropertyType.Float64:
                if (!isMap && !isArray)
                    payloadSb.AppendLine(
                        $"{indentManager.GetIndent()}${propertyAssignment.ToFirstCharacterLowerCase()}->set{EscapePropertyNameForSetterAndGetter(propertyName.ToFirstCharacterUpperCase())}({child.Value});");
                else
                    payloadSb.Append($"{child.Value},");
                break;
			case PropertyType.Boolean:
                if (!isMap && !isArray) {
                    payloadSb.AppendLine(
                        $"{indentManager.GetIndent()}${propertyAssignment.ToFirstCharacterLowerCase()}->set{EscapePropertyNameForSetterAndGetter(propertyName.ToFirstCharacterUpperCase())}({child.Value.ToLower()});");
                }
                else
                {
                    payloadSb.Append($"{child.Value.ToLower()},");
                }
                break;
			case PropertyType.Null:
                WriteNullProperty(propertyAssignment, parent, payloadSb, indentManager, child);
                break;
            case PropertyType.Object: 
                WriteObjectProperty(propertyAssignment.ToFirstCharacterLowerCase(), payloadSb, child, indentManager, childPropertyName);
                if (!fromArray)
                    payloadSb.AppendLine(
                        $"${propertyAssignment.ToFirstCharacterLowerCase()}->set{EscapePropertyNameForSetterAndGetter(child.Name.ToFirstCharacterUpperCase())}(${(childPropertyName ?? propertyName).ToFirstCharacterLowerCase()});");
                break;
			case PropertyType.Array:
				WriteArrayProperty(propertyAssignment.ToFirstCharacterLowerCase(), child.Name, payloadSb, parent, child, indentManager); 
                break;
            case PropertyType.Enum:
                WriteEnumValue(payloadSb, propertyAssignment.ToFirstCharacterLowerCase(), child, parent);
                break;
            case PropertyType.Base64Url:
                WriteBase64Url(propertyAssignment, parent, payloadSb, indentManager, child);
                break;
            case PropertyType.Map:
                WriteMapValue(payloadSb, propertyAssignment.ToFirstCharacterLowerCase(), parent, child, indentManager);
                break;
            case PropertyType.DateTime:
                WriteDateTimeValue(payloadSb, propertyAssignment.ToFirstCharacterLowerCase(), parent, child, indentManager);
                break;
            case PropertyType.Duration:
                WriteDurationValue(payloadSb, propertyAssignment.ToFirstCharacterLowerCase(), parent, child,
                    indentManager);
                break;
			default:
				throw new NotImplementedException($"Unsupported PropertyType: {child.PropertyType.GetDisplayName()}");
        }
	}
    
    private static void WriteDurationValue(StringBuilder payloadSb, string propertyAssignment, CodeProperty parent,
        CodeProperty child, IndentManager indentManager) {
        var fromObject = parent.PropertyType == PropertyType.Object;
        var assignmentValue = $"new \\DateInterval(\'{child.Value}\')";
        if (fromObject)
            payloadSb.AppendLine(
                $"{indentManager.GetIndent()}${propertyAssignment}->set{EscapePropertyNameForSetterAndGetter(child.Name)}({assignmentValue});");
        else
            payloadSb.Append($"{indentManager.GetIndent()}{assignmentValue},");
    }
    private static void WriteDateTimeValue(StringBuilder payloadSb, string propertyAssignment, CodeProperty parent,
        CodeProperty child, IndentManager indentManager)
    {
        var fromObject = parent.PropertyType == PropertyType.Object;
        var assignmentValue = $"new \\DateTime(\'{child.Value}\')";
        if (fromObject)
            payloadSb.AppendLine(
                $"{indentManager.GetIndent()}${propertyAssignment}->set{EscapePropertyNameForSetterAndGetter(child.Name)}({assignmentValue});");
        else
            payloadSb.Append($"{indentManager.GetIndent()}{assignmentValue},");
    }
    private static void WriteNullProperty(string propertyAssignment, CodeProperty parent, StringBuilder payloadSb, IndentManager indentManager, CodeProperty child)
    {
        var fromObject = parent.PropertyType == PropertyType.Object;
        if (fromObject)
            payloadSb.AppendLine(
                $"${propertyAssignment}->set{EscapePropertyNameForSetterAndGetter(child.Name)}(null);");
        else
            payloadSb.Append($"{indentManager.GetIndent()}null,");
    }

    private static void WriteMapValue(StringBuilder payloadSb, string propertyAssignment, CodeProperty parent, CodeProperty currentProperty, IndentManager indentManager)
    {
        payloadSb.AppendLine($"${currentProperty.Name} = [");
        var childPosition = 0;
        indentManager.Indent(2);
        foreach (var child in currentProperty.Children)
        {
            payloadSb.Append($"{indentManager.GetIndent()}\'{child.Name}\' => ");
            WriteCodeProperty(propertyAssignment, payloadSb, currentProperty, child, indentManager, ++childPosition);
            payloadSb.AppendLine();
        }
        indentManager.Unindent();
        indentManager.Unindent();
        payloadSb.AppendLine("];");
        if (parent.PropertyType == PropertyType.Object)
            payloadSb.AppendLine(
                $"${propertyAssignment}->set{EscapePropertyNameForSetterAndGetter(currentProperty.Name)}(${EscapePropertyNameForSetterAndGetter(currentProperty.Name).ToFirstCharacterLowerCase()});");
        payloadSb.AppendLine();
    }
    private static void WriteArrayProperty(string propertyAssignment, string objectName, StringBuilder payloadSb, CodeProperty parentProperty, CodeProperty codeProperty, IndentManager indentManager)
    {
        var hasSchema = codeProperty.PropertyType == PropertyType.Object;
        var arrayName = $"{objectName.ToFirstCharacterLowerCase()}Array";
        StringBuilder builder = new StringBuilder();
        if (hasSchema) 
            builder.AppendLine($"${arrayName} = [];");
        else if (codeProperty.Children.FirstOrDefault().PropertyType != PropertyType.Object)
             builder.Append('[');
        int childPosition = 0;
        CodeProperty lastProperty = default;
        foreach (var property in codeProperty.Children)
        {
            var childPropertyName = $"{EscapePropertyNameForSetterAndGetter(codeProperty.Name)}{EscapePropertyNameForSetterAndGetter(property.Name)}{++childPosition}".ToFirstCharacterLowerCase();
            WriteCodeProperty(propertyAssignment, builder, codeProperty, property, indentManager, childPosition, childPropertyName);
            if (property.PropertyType == PropertyType.Object && codeProperty.PropertyType == PropertyType.Array)
            {
                builder.AppendLine($"${arrayName} []= ${childPropertyName};");
            }

            lastProperty = property;
        }

        if (lastProperty.PropertyType == PropertyType.Object && codeProperty.PropertyType == PropertyType.Array)
        {
            builder.AppendLine(
                $"${propertyAssignment}->set{EscapePropertyNameForSetterAndGetter(codeProperty.Name)}(${arrayName});");
            payloadSb.AppendLine(builder.ToString());
        }
        else
        {
            builder.Append(']');
            if (parentProperty.PropertyType == PropertyType.Object)
                payloadSb.AppendLine(
                    $"${propertyAssignment}->set{EscapePropertyNameForSetterAndGetter(codeProperty.Name)}({builder.ToString()});");
            else
                payloadSb.Append($"{builder.ToString()},");
        }

        indentManager.Unindent();
    }

    private static void WriteEnumValue(StringBuilder payloadSb,string parentPropertyName, CodeProperty currentProperty, CodeProperty parent)
    {
        var enumParts = currentProperty.Value.Split('.');
        var enumClass = enumParts.First();
        var enumValue = enumParts.Last().ToLower();
        var fromObject = parent.PropertyType == PropertyType.Object;
        var value = $"new {enumClass}('{enumValue}')";
        if (fromObject)
            payloadSb.AppendLine(
                $"${parentPropertyName}->set{EscapePropertyNameForSetterAndGetter(currentProperty.Name)}({value});");
        else
            payloadSb.Append($"{value},");
    }

    private static void WriteBase64Url(string propertyAssignment, CodeProperty parent, StringBuilder payloadSb, IndentManager indentManager, CodeProperty child)
    {
        var fromObject = parent.PropertyType == PropertyType.Object;
        var value = $"base64_decode(\'{child.Value}\')";
        if (fromObject)
            payloadSb.AppendLine(
                $"{indentManager.GetIndent()}${propertyAssignment}->set{EscapePropertyNameForSetterAndGetter(child.Name)}({value});");
        else
            payloadSb.Append($"{value},");
    }

    private static void WriteStringProperty(string propertyAssignment, CodeProperty parent, StringBuilder payloadSb, IndentManager indentManager, CodeProperty codeProperty)
    {
        var fromObject = parent.PropertyType == PropertyType.Object;

        if (fromObject)
        {
            payloadSb.AppendLine(
                $"${indentManager.GetIndent()}{propertyAssignment.ToFirstCharacterLowerCase()}->set{EscapePropertyNameForSetterAndGetter(codeProperty.Name)}('{codeProperty.Value.EscapeQuotesInLiteral("\"", "\\'")}');");
        }
        else
            payloadSb.Append($"\'{codeProperty.Value.EscapeQuotesInLiteral("\"", "\\'")}\', ");
    }

    private static string NormalizeVariableName(string variable) =>
        variable.Replace(".", String.Empty).Replace("-", string.Empty);

    private static string EscapePropertyNameForSetterAndGetter(string propertyName)
    {
        return propertyName?.Split('.', '@', '-').Select(x => x.ToFirstCharacterUpperCase()).Aggregate((a, b) => a + b);
    }
    private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes)
    {
        var openApiUrlTreeNodes = nodes.ToList();
        if (!(openApiUrlTreeNodes?.Any() ?? false)) return string.Empty;
        var result = openApiUrlTreeNodes.Select(static x =>
            {
                if (x.Segment.IsCollectionIndex())
                    return $"ById{x.Segment.Replace("{", "('").Replace("}", "')")}";
                if (x.Segment.IsFunction())
                    return x.Segment.Split('.')
                        .Select(static s => s.ToFirstCharacterUpperCase())
                        .Aggregate(static (a, b) => $"{a}{b}").ToFirstCharacterLowerCase() + "()";
                return x.Segment.ToFirstCharacterLowerCase() + "()";
            }).Aggregate(new List<String>(), (current, next) =>
            {
                if (next.StartsWith("ById"))
                {
                    var prev = current.Last();
                    var inBrackets = next[4..];
                    if (prev.EndsWith("s()"))
                    {
                        prev = prev[..^3];
                    }

                    next = $"by{prev.ToFirstCharacterUpperCase()}Id{inBrackets}";
                }
                current.Add(next);
                return current;
            }).Aggregate(static (x, y) => $"{x.Trim('$')}->{y.Trim('$')}")
            .Replace("()()->", "()->");

        return result.EndsWith("()()") ? result[..^2] : result;
    }
}
