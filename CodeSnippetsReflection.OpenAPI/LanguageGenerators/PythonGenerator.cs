using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators;

public class PythonGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
{
    private const string ClientVarName = "client";
    private const string ClientVarType = "GraphServiceClient";
    private const string HttpCoreVarName = "request_adapter";
    private const string RequestBodyVarName = "request_body";
    private const string QueryParametersVarName = "query_params";
    private const string RequestConfigurationVarName = "request_configuration";
    private const string RequestHeadersVarName = "headers";
    public string GenerateCodeSnippet(SnippetModel snippetModel)
    {
        var indentManager = new IndentManager();
        var codeGraph = new SnippetCodeGraph(snippetModel);
        var snippetBuilder = new StringBuilder(
                                    "// THE PYTHON SDK IS IN PREVIEW. NON-PRODUCTION USE ONLY" + Environment.NewLine +
                    $"{ClientVarName} =  {ClientVarType}({HttpCoreVarName}){Environment.NewLine}{Environment.NewLine}");

        if (codeGraph.HasBody())
        {
            WriteObjectProperty(RequestBodyVarName, snippetBuilder, codeGraph.Body, indentManager);
        
        }
        WriteRequestExecutionPath(codeGraph, snippetBuilder, indentManager);
        return snippetBuilder.ToString();
    }
    private static void WriteRequestExecutionPath(SnippetCodeGraph codeGraph, StringBuilder snippetBuilder, IndentManager indentManager)
    {
        var method = codeGraph.HttpMethod.Method.ToLower();
        var configParameter = codeGraph.HasHeaders() || codeGraph.HasParameters() || codeGraph.HasOptions()
            ? $"{RequestConfigurationVarName} = {RequestConfigurationVarName}"
            : string.Empty;
        var bodyParameter = codeGraph.HasBody()
            ? $"{RequestBodyVarName} = {RequestBodyVarName}"
            : string.Empty;
            /*
            body = body
            */
        var optionsParameter = codeGraph.HasOptions() ? $"options =" : string.Empty;
        var returnVar = codeGraph.HasReturnedBody() ? "result = " : string.Empty;
        var parameterList = GetActionParametersList(bodyParameter, configParameter, optionsParameter);
        snippetBuilder.AppendLine(GetRequestConfiguration(codeGraph, indentManager));
        snippetBuilder.AppendLine();
        snippetBuilder.AppendLine($"{returnVar}await {ClientVarName}.{GetFluentApiPath(codeGraph.Nodes)}.{method}({parameterList})");
    }
    private static string GetRequestQueryParameters(SnippetCodeGraph model, IndentManager indentManager, string classNameQueryParameters = default)
        {
            var snippetBuilder = new StringBuilder();
            if (!model.HasParameters())
                return default;
            snippetBuilder.AppendLine($"query_params = {classNameQueryParameters}(");
            indentManager.Indent(2);
            foreach (var queryParam in model.Parameters)
            {
                var queryParameterName = NormalizeQueryParameterName(queryParam.Name).ToFirstCharacterLowerCase();
                snippetBuilder.AppendLine($"{indentManager.GetIndent()}{queryParameterName} = {EvaluateParameter(queryParam)},");
            }
            indentManager.Unindent();
            snippetBuilder.AppendLine(")");
            return snippetBuilder.ToString();
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
            var snippetBuilder = new StringBuilder();
            var requestBuilderName = codeGraph.Nodes.Last().GetClassName("RequestBuilder").ToFirstCharacterUpperCase();
            var className = $"{requestBuilderName}{codeGraph.HttpMethod.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}RequestConfiguration";
            
            var classNameQueryParameters = $"{requestBuilderName}.{requestBuilderName}{codeGraph.HttpMethod.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}QueryParameters";
            
            var queryParamsPayload = GetRequestQueryParameters(codeGraph, indentManager, classNameQueryParameters);
            if (codeGraph.HasParameters() || codeGraph.HasParameters()){
                snippetBuilder.AppendLine(queryParamsPayload); 
                snippetBuilder.AppendLine($"{RequestConfigurationVarName} = {requestBuilderName}.{className}(");
                indentManager.Indent(); 

                var requestHeadersPayload = GetRequestHeaders(codeGraph, indentManager);
                if (codeGraph.HasParameters()){
                    snippetBuilder.AppendLine("query_parameters = query_params,");
                    indentManager.Unindent();
                    snippetBuilder.AppendLine(")");
                }
                if (codeGraph.HasHeaders()) {
                    snippetBuilder.AppendLine(requestHeadersPayload);
                    indentManager.Unindent();
                    snippetBuilder.AppendLine(")");
                }
            }    
            return snippetBuilder.ToString();
        }
    private static string GetActionParametersList(params string[] parameters) {
        var nonEmptyParameters = parameters.Where(static p => !string.IsNullOrEmpty(p));
        var emptyParameters = nonEmptyParameters.ToList();
        if(emptyParameters.Any())
            return string.Join(", ", emptyParameters.Select(static x => $"{x}").Aggregate(static (a, b) => $"{a}, {b}"));
        return string.Empty;
    }
    
   private static string GetRequestHeaders(SnippetCodeGraph snippetModel, IndentManager indentManager)
    {
        var snippetBuilder = new StringBuilder();
        var filteredHeaders = snippetModel.Headers?.Where(static h => !h.Name.Equals("Host", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (!snippetModel.HasHeaders())
            return "headers = {}";
        
        var headersvar = new StringBuilder();
        headersvar.AppendLine("headers = {");
        indentManager.Indent();
        foreach (var header in snippetModel.Headers)
        {
            headersvar.AppendLine($"{indentManager.GetIndent()}'{header.Name}' : {EvaluateParameter(header)},");
        }
        indentManager.Unindent();
        headersvar.AppendLine("}");
        /*
          headers= {

            "Content-Type": "application/json",
            "Accept": "Value",
          }
        */

        return headersvar.ToString();
    }
    private static string NormalizeQueryParameterName(string queryParam) => queryParam?.TrimStart('$').ToFirstCharacterLowerCase();
    private static void WriteObjectProperty(string propertyAssignment, StringBuilder snippetBuilder, CodeProperty codeProperty, IndentManager indentManager, string childPropertyName = default, SnippetCodeGraph codeGraph = default)
    {
        var childPosition = 0;
        var objectType = (codeProperty.TypeDefinition ?? codeProperty.Name).ToFirstCharacterUpperCase();
        snippetBuilder.AppendLine($"{(childPropertyName ?? propertyAssignment).ToFirstCharacterLowerCase()} = {objectType}()");
        foreach(CodeProperty child in codeProperty.Children)
        {
            var newChildName = (childPropertyName ?? "") + child.Name.ToFirstCharacterUpperCase();
            WriteCodeProperty(childPropertyName ?? propertyAssignment, snippetBuilder, codeProperty, child, indentManager, ++childPosition, newChildName);
            if (child.PropertyType != PropertyType.Object) 
                snippetBuilder.AppendLine();
        }
        snippetBuilder.AppendLine();
    }
    private static void WriteCodeProperty(string propertyAssignment, StringBuilder snippetBuilder, CodeProperty parent, CodeProperty child, IndentManager indentManager, int childPosition = 0, string childPropertyName = default)
    {
        var isArray = parent.PropertyType == PropertyType.Array;
        var isMap = parent.PropertyType == PropertyType.Map;
        var fromArray = parent.PropertyType == PropertyType.Array;

        var propertyName = NormalizeQueryParameterName(child.Name.ToFirstCharacterLowerCase());
        switch (child.PropertyType) {
			case PropertyType.String:
                WriteStringProperty(propertyAssignment, parent, snippetBuilder, indentManager, child);
                break;
			case PropertyType.Int32:
            case PropertyType.Int64:
            case PropertyType.Double:
            case PropertyType.Float32:
            case PropertyType.Float64:
                if (!isMap && !isArray)
                    snippetBuilder.AppendLine(
                        $"{indentManager.GetIndent()}{propertyAssignment.ToFirstCharacterLowerCase()}.{NormalizeVariableName(child.Name)} = {child.Value}");
                else
                    snippetBuilder.Append($"{child.Value},");
                break;
			case PropertyType.Boolean:
                if (!isMap && !isArray) {
                    snippetBuilder.AppendLine(
                        $"{indentManager.GetIndent()}{propertyAssignment.ToFirstCharacterLowerCase()}.{NormalizeVariableName(child.Name)} = {child.Value.ToLower()}");
                }
                else
                {
                    snippetBuilder.Append($"{child.Value.ToLower()},");
                }
                break;
			case PropertyType.Null:
                WriteNullProperty(propertyAssignment, parent, snippetBuilder, indentManager, child);
                break;
            case PropertyType.Object: 
                WriteObjectProperty(propertyAssignment.ToFirstCharacterLowerCase(), snippetBuilder, child, indentManager, childPropertyName);
                if (!fromArray)
                    snippetBuilder.AppendLine(
                        $"{propertyAssignment.ToFirstCharacterLowerCase()}.{NormalizeVariableName(child.Name)} = {(childPropertyName ?? propertyName).ToFirstCharacterLowerCase()}");
                break;
			case PropertyType.Array:
				WriteArrayProperty(propertyAssignment.ToFirstCharacterLowerCase(), child.Name, snippetBuilder, parent, child, indentManager); 
                break;
            case PropertyType.Enum:
                WriteEnumValue(snippetBuilder, propertyAssignment.ToFirstCharacterLowerCase(), child);
                break;
            case PropertyType.Base64Url:
                WriteBase64Url(propertyAssignment, parent, snippetBuilder, indentManager, child);
                break;
            case PropertyType.Map:
                WriteMapValue(snippetBuilder, propertyAssignment.ToFirstCharacterLowerCase(), parent, child, indentManager);
                break;
            case PropertyType.DateTime:
                WriteDateTimeValue(snippetBuilder, propertyAssignment.ToFirstCharacterLowerCase(), parent, child, indentManager);
                break;
            case PropertyType.Duration:
                WriteDurationValue(snippetBuilder, propertyAssignment.ToFirstCharacterLowerCase(), parent, child,
                    indentManager);
                break;
			default:
				throw new NotImplementedException($"Unsupported PropertyType: {child.PropertyType.GetDisplayName()}");
        }
	}
    
    private static void WriteDurationValue(StringBuilder snippetBuilder, string propertyAssignment, CodeProperty parent,
        CodeProperty child, IndentManager indentManager) {
        var fromObject = parent.PropertyType == PropertyType.Object;
        var assignmentValue = $" \\DateInterval(\'{child.Value}\')";
        if (fromObject)
            snippetBuilder.AppendLine(
                $"{indentManager.GetIndent()}{propertyAssignment}.{NormalizeVariableName(child.Name.ToLower())} = {assignmentValue}");
        else
            snippetBuilder.Append($"{indentManager.GetIndent()}{assignmentValue},");
    }
    private static void WriteDateTimeValue(StringBuilder snippetBuilder, string propertyAssignment, CodeProperty parent,
        CodeProperty child, IndentManager indentManager)
    {
        var fromObject = parent.PropertyType == PropertyType.Object;
        var assignmentValue = $"DateTime(\'{child.Value}\')";
        if (fromObject)
            snippetBuilder.AppendLine(
                $"{indentManager.GetIndent()}{propertyAssignment}.{NormalizeVariableName(child.Name)} = {assignmentValue}");
        else
            snippetBuilder.Append($"{indentManager.GetIndent()}{assignmentValue},");
    }
    private static void WriteNullProperty(string propertyAssignment, CodeProperty parent, StringBuilder snippetBuilder, IndentManager indentManager, CodeProperty child)
    {
        var fromObject = parent.PropertyType == PropertyType.Object;
        if (fromObject)
            snippetBuilder.AppendLine(
                $"{indentManager.GetIndent()}{propertyAssignment}.{NormalizeVariableName(child.Name)}=null");
        else
            snippetBuilder.Append($"{indentManager.GetIndent()}null,");
    }

    private static void WriteMapValue(StringBuilder snippetBuilder, string propertyAssignment, CodeProperty parent, CodeProperty currentProperty, IndentManager indentManager)
    {
        snippetBuilder.AppendLine($"{currentProperty.Name} = [");
        var childPosition = 0;
        indentManager.Indent(2);
        foreach (var child in currentProperty.Children)
        {
            snippetBuilder.Append($"\'{child.Name}\' => ");
            WriteCodeProperty(propertyAssignment, snippetBuilder, currentProperty, child, indentManager, ++childPosition);
            snippetBuilder.AppendLine();
        }
        indentManager.Unindent();
        indentManager.Unindent();
        snippetBuilder.AppendLine("];");
        if (parent.PropertyType == PropertyType.Object)
            snippetBuilder.AppendLine(
                $"{propertyAssignment}.{NormalizeVariableName(currentProperty.Name.ToLower())}({NormalizeVariableName(currentProperty.Name).ToFirstCharacterLowerCase()})");
        snippetBuilder.AppendLine();
    }
    private static void WriteArrayProperty(string propertyAssignment, string objectName, StringBuilder snippetBuilder, CodeProperty parentProperty, CodeProperty codeProperty, IndentManager indentManager)
    {
        var hasSchema = codeProperty.PropertyType == PropertyType.Object;
        var arrayName = $"{objectName.ToFirstCharacterLowerCase()}Array";
        StringBuilder builder = new StringBuilder();
        if (hasSchema) 
            builder.AppendLine($"{arrayName} = []");
        else if (codeProperty.Children.FirstOrDefault().PropertyType != PropertyType.Object)
             builder.Append('[');
        int childPosition = 0;
        CodeProperty lastProperty = default;
        foreach (var property in codeProperty.Children)
        {
            var childPropertyName = $"{NormalizeVariableName(codeProperty.Name.ToFirstCharacterLowerCase())}{property.Name.ToFirstCharacterUpperCase()}{++childPosition}";
            WriteCodeProperty(propertyAssignment, builder, codeProperty, property, indentManager, childPosition, childPropertyName);
            if (property.PropertyType == PropertyType.Object && codeProperty.PropertyType == PropertyType.Array)
            {
                builder.AppendLine($"{arrayName} []= {childPropertyName};");
            }

            lastProperty = property;
        }

        if (lastProperty.PropertyType == PropertyType.Object && codeProperty.PropertyType == PropertyType.Array)
        {
            builder.AppendLine(
                $"{propertyAssignment}.{NormalizeVariableName(codeProperty.Name).ToLower()}({arrayName})");
            snippetBuilder.AppendLine(builder.ToString());
        }
        else
        {
            builder.Append(']');
            if (parentProperty.PropertyType == PropertyType.Object)
                snippetBuilder.AppendLine(
                    $"{propertyAssignment}.{NormalizeVariableName(codeProperty.Name).ToFirstCharacterUpperCase()}({builder.ToString()})");
            else
                snippetBuilder.Append($"{builder.ToString()},");
        }

        indentManager.Unindent();
    }

    private static void WriteEnumValue(StringBuilder snippetBuilder,string parentPropertyName, CodeProperty currentProperty)
    {
        var enumParts = currentProperty.Value.Split('.');
        var enumClass = enumParts.First();
        var enumValue = enumParts.Last().ToLower();
        snippetBuilder.AppendLine(
            $"{parentPropertyName}.{NormalizeVariableName(currentProperty.Name).ToFirstCharacterUpperCase()}({enumClass}('{enumValue}'))");
    }

    private static void WriteBase64Url(string propertyAssignment, CodeProperty parent, StringBuilder snippetBuilder, IndentManager indentManager, CodeProperty child)
    {
        var fromObject = parent.PropertyType == PropertyType.Object;
        if (fromObject)
            snippetBuilder.AppendLine(
                $"{indentManager.GetIndent()}{propertyAssignment}.{NormalizeVariableName(child.Name.ToFirstCharacterUpperCase())}(base64_decode(\'{child.Value}\'))");
        else
            snippetBuilder.Append("null,");
    }

    private static void WriteStringProperty(string propertyAssignment, CodeProperty parent, StringBuilder snippetBuilder, IndentManager indentManager, CodeProperty codeProperty)
    {
        var fromObject = parent.PropertyType == PropertyType.Object;

        if (fromObject)
        {
            snippetBuilder.AppendLine(
                $"{indentManager.GetIndent()}{propertyAssignment.ToFirstCharacterLowerCase()}.{NormalizeVariableName(codeProperty.Name)} = '{codeProperty.Value.EscapeQuotesInLiteral("\"", "\\'")}'");
        }
        else
            snippetBuilder.Append($"\'{codeProperty.Value.EscapeQuotesInLiteral("\"", "\\'")}\', ");
    }

    private static string NormalizeVariableName(string variable) =>
        variable.Replace(".", String.Empty).Replace("-", string.Empty);
    
    private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes)
        {
            var openApiUrlTreeNodes = nodes.ToList();
            if(!(openApiUrlTreeNodes?.Any() ?? false)) 
                return string.Empty;

            return openApiUrlTreeNodes.Select(static x => {
                if(x.Segment.IsCollectionIndex())
                    return $"by_id{x.Segment.Replace("{", "('").Replace("}", "')")}";
                if (x.Segment.IsFunction())
                    return x.Segment.RemoveFunctionBraces().Split('.')
                        .Select(static s => s.ToFirstCharacterLowerCase())
                        .Aggregate(static (a, b) => $"{a}{b}");
                return x.Segment.ReplaceValueIdentifier().TrimStart('$').RemoveFunctionBraces().ToFirstCharacterLowerCase();
                })
        .Aggregate(static (x, y) =>
            {
                var dot = y.StartsWith("by_id") ? "_" : ".";
                return $"{x.Trim('$')}{dot}{y.Trim('$')}";
            }).Replace("()ById(", "by_id(")
              .Replace("()().", "().");
        }
}
