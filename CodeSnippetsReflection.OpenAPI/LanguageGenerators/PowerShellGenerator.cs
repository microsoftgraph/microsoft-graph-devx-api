using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators
{
    public class PowerShellGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
    {
        private const string requestBodyVarName = "params";
        private const string modulePrefix = "Microsoft.Graph";
        private const string mgCommandMetadataUrl = "https://raw.githubusercontent.com/microsoftgraph/msgraph-sdk-powershell/dev/src/Authentication/Authentication/custom/common/MgCommandMetadata.json";
        private readonly Lazy<IList<PowerShellCommandInfo>> psCommands = new(
            () => {
                using var httpClient = new HttpClient();
                using var stream = httpClient.GetStreamAsync(mgCommandMetadataUrl).GetAwaiter().GetResult();
                return JsonSerializer.Deserialize<IList<PowerShellCommandInfo>>(stream);
            }
        );
        private static Regex meSegmentRegex = new("^/me($|(?=/))", RegexOptions.Compiled);
        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            var indentManager = new IndentManager();
            var snippetBuilder = new StringBuilder();
            var cleanPath = snippetModel.EndPathNode.Path.Replace("\\", "/");
            bool isMeSegment = meSegmentRegex.IsMatch(cleanPath);
            var (path, additionalKeySegmentParmeter) = SubstituteMeSegment(isMeSegment, cleanPath);
            IList<PowerShellCommandInfo> matchedCommands = GetCommandForRequest(path, snippetModel.Method.ToString(), snippetModel.ApiVersion);
            var targetCommand = matchedCommands.FirstOrDefault();
            if (targetCommand != null)
            {
                string moduleName = targetCommand.Module;
                if (!string.IsNullOrEmpty(moduleName))
                    snippetBuilder.AppendLine($"Import-Module {modulePrefix}.{moduleName}");
                var (requestPayload, payloadVarName) = GetRequestPayloadAndVariableName(snippetModel, indentManager);
                if (!string.IsNullOrEmpty(requestPayload))
                    snippetBuilder.Append($"{Environment.NewLine}{requestPayload}");

                if (isMeSegment)
                    snippetBuilder.Append($"{Environment.NewLine}# A UPN can also be used as -UserId.");

                snippetBuilder.Append($"{Environment.NewLine}{targetCommand.Command}");

                if (!string.IsNullOrEmpty(additionalKeySegmentParmeter))
                    snippetBuilder.Append($"{additionalKeySegmentParmeter}");

                string keySegmentParameter = GetKeySegmentParameters(snippetModel.PathNodes);
                if (!string.IsNullOrEmpty(keySegmentParameter))
                    snippetBuilder.Append($"{keySegmentParameter}");

                var queryParamsPayload = GetRequestQueryParameters(snippetModel);
                if (!string.IsNullOrEmpty(queryParamsPayload))
                    snippetBuilder.Append($" {queryParamsPayload}");

                var parameterList = GetActionParametersList(payloadVarName);
                if (!string.IsNullOrEmpty(parameterList))
                    snippetBuilder.Append($" {parameterList}");

                var requestHeadersPayload = GetSupportedRequestHeaders(snippetModel);
                if (!string.IsNullOrEmpty(requestHeadersPayload))
                    snippetBuilder.Append(requestHeadersPayload);
            }
            return snippetBuilder.ToString();
        }

        private static (string, string) SubstituteMeSegment(bool isMeSegment, string path)
        {
            string additionalKeySegmentParmeter = default;
            if (isMeSegment)
            {
                path = meSegmentRegex.Replace(path, "/users/{user-id}");
                additionalKeySegmentParmeter = $" -UserId $userId";
            }
            return (path, additionalKeySegmentParmeter);
        }

        private static string GetSupportedRequestHeaders(SnippetModel snippetModel)
        {
            var payloadSB = new StringBuilder();
            if (Enum.TryParse(snippetModel.Method.Method, true, out OperationType method))
            {
                var operation = snippetModel.EndPathNode.PathItems.Select(p => p.Value.Operations[method]).FirstOrDefault();
                foreach (var header in snippetModel.RequestHeaders)
                {
                    var parameter = operation.Parameters.FirstOrDefault(p => p.Name.Equals(header.Key, StringComparison.OrdinalIgnoreCase));
                    if (parameter != null)
                        payloadSB.AppendLine($"-{parameter.Name} {header.Value.FirstOrDefault()} ");
                }
            }
            return payloadSB.ToString();
        }

        private static string GetKeySegmentParameters(IEnumerable<OpenApiUrlTreeNode> pathNodes)
        {
            if (!pathNodes.Any()) return string.Empty;
            return pathNodes.Where(x => x.Segment.IsCollectionIndex()).Select(p =>
            {
                string parameterName = (p.Segment.Replace("{", string.Empty).Replace("}", string.Empty).ToFirstCharacterUpperCaseAfterCharacter('-').Replace("-", string.Empty));
                return $"-{parameterName.ToFirstCharacterUpperCase()} ${parameterName}";
            }).Aggregate(string.Empty, (x, y) =>
            {
                return $"{x} {y}";
            });
        }

        private static string GetRequestQueryParameters(SnippetModel model)
        {
            var payloadSB = new StringBuilder();
            if (!string.IsNullOrEmpty(model.QueryString))
            {
                var (queryString, replacements) = ReplaceNestedOdataQueryParameters(Uri.UnescapeDataString(model.QueryString));
                foreach (var queryParam in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (queryParam.Contains('='))
                    {
                        var kvPair = queryParam.Split('=', StringSplitOptions.RemoveEmptyEntries);
                        string parameterName = NormalizeQueryParameterName(kvPair[0]);
                        payloadSB.Append($"-{parameterName} {GetQueryParameterValue(parameterName, kvPair[1].Trim('"'), replacements)} ");
                    }
                    else
                        payloadSB.Append($"-{NormalizeQueryParameterName(queryParam)} = {""} ");
                }
                return payloadSB.ToString();
            }
            return default;
        }

        private static string GetQueryParameterValue(string normalizedParameterName, string originalValue, Dictionary<string, string> replacements)
        {
            if (normalizedParameterName.Equals("CountVariable"))
                return "CountVar";

            if (originalValue.Equals("true", StringComparison.OrdinalIgnoreCase) || originalValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                return originalValue.ToLowerInvariant();
            else if (int.TryParse(originalValue, out var intValue))
                return intValue.ToString();
            else {
                var valueWithNested = originalValue.Split(',')
                                                    .Select(v => replacements.ContainsKey(v) ? v + replacements[v] : v)
                                                    .Aggregate((a, b) => $"{a},{b}");
                // Replace '$' with '`$' since '$' is a reserved character in powershell.
                return $"\"{valueWithNested.Replace("$", "`$")}\"";
            }
        }
        private static string NormalizeQueryParameterName(string queryParam)
        {
            string psParameterName = queryParam.TrimStart('$').ToLower().ToFirstCharacterUpperCase();
            return psParameterName switch {
                "Select" => "Property",
                "Expand" => "ExpandProperty",
                "Count" => "CountVariable",
                "Orderby" => "Sort",
                _ => psParameterName
            };
        }

        private static Regex nestedStatementRegex = new(@"(\w+|\w+\/\w+)(\([^)]+\))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static (string, Dictionary<string, string>) ReplaceNestedOdataQueryParameters(string queryParams)
        {
            var replacements = new Dictionary<string, string>();
            var matches = nestedStatementRegex.Matches(queryParams);
            if (matches.Any())
                foreach (GroupCollection groupCollection in matches.Select(x => x.Groups))
                {
                    var key = groupCollection[1].Value;
                    var value = groupCollection[2].Value;
                    if (value.Contains('=') && replacements.TryAdd(key, value))
                        queryParams = queryParams.Replace(value, string.Empty);
                }
            return (queryParams, replacements);
        }

        private static Regex keyIndexRegex = new(@"(?<={)(.*?)(?=})", RegexOptions.Compiled);
        private IList<PowerShellCommandInfo> GetCommandForRequest(string path, string method, string apiVersion)
        {
            if (psCommands.Value.Count == 0)
                return default;
            path = Regex.Escape(SnippetModel.TrimNamespace(path));
            // Tokenize uri by substituting parameter values with "{.*}" e.g, "/users/{user-id}" to "/users/{.*}".
            path = $"^{keyIndexRegex.Replace(path, "(\\w*-\\w*|\\w*)")}$";
            return psCommands.Value.Where(c => c.Method == method && c.ApiVersion == apiVersion && Regex.Match(c.Uri, path).Success).ToList();
        }

        private static (string, string) GetRequestPayloadAndVariableName(SnippetModel snippetModel, IndentManager indentManager)
        {
            if (string.IsNullOrWhiteSpace(snippetModel?.RequestBody) 
                || "undefined".Equals(snippetModel?.RequestBody, StringComparison.OrdinalIgnoreCase)) // graph explorer sends "undefined" as request body for some reason
                return (default, default);
            if (indentManager == null) throw new ArgumentNullException(nameof(indentManager));

            var payloadSB = new StringBuilder();
            switch (snippetModel.ContentType?.Split(';').First().ToLowerInvariant())
            {
                case "application/json":
                    using (var parsedBody = JsonDocument.Parse(snippetModel.RequestBody, new JsonDocumentOptions { AllowTrailingCommas = true }))
                    {
                        var schema = snippetModel.RequestSchema;
                        payloadSB.AppendLine($"{indentManager.GetIndent()}${requestBodyVarName} = @{{");
                        WriteJsonObjectValue(payloadSB, parsedBody.RootElement, schema, indentManager);
                        payloadSB.AppendLine("}");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported content type: {snippetModel.ContentType}");
            }
            return (payloadSB.ToString(), $"-BodyParameter ${requestBodyVarName}");
        }

        private static void WriteJsonObjectValue(StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager, bool includePropertyAssignment = true)
        {
            if (value.ValueKind != JsonValueKind.Object) throw new InvalidOperationException($"Expected JSON object and got {value.ValueKind}");
            indentManager.Indent();
            var propertiesAndSchema = value.EnumerateObject()
                                            .Select(x => new Tuple<JsonProperty, OpenApiSchema>(x, schema?.GetPropertySchema(x.Name)));
            foreach (var propertyAndSchema in propertiesAndSchema)
            {
                var propertyName = propertyAndSchema.Item1.Name.ToFirstCharacterUpperCase();
                // Enclose in quotes if property name contains a non-word character.
                if (Regex.IsMatch(propertyName, "\\W")) { propertyName = $"\"{propertyName}\""; }
                var propertyAssignment = includePropertyAssignment ? $"{indentManager.GetIndent()}{propertyName} = " : string.Empty;
                WriteProperty(payloadSB, propertyAndSchema.Item1.Value, propertyAndSchema.Item2, indentManager, propertyAssignment);
            }
            indentManager.Unindent();
        }

        private static void WriteProperty(StringBuilder payloadSB, JsonElement value, OpenApiSchema propSchema, IndentManager indentManager, string propertyAssignment, string propertySuffix = default)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    if (propSchema?.Format?.Equals("base64url", StringComparison.OrdinalIgnoreCase) ?? false)
                        payloadSB.AppendLine($"{propertyAssignment}[System.Text.Encoding]::ASCII.GetBytes(\"{value.GetString()}\"){propertySuffix}");
                    else if (propSchema?.Format?.Equals("date-time", StringComparison.OrdinalIgnoreCase) ?? false)
                        payloadSB.AppendLine($"{propertyAssignment}[System.DateTime]::Parse(\"{value.GetString()}\"){propertySuffix}");
                    else
                        payloadSB.AppendLine($"{propertyAssignment}\"{value.GetString()}\"{propertySuffix}");
                    break;
                case JsonValueKind.Number:
                    payloadSB.AppendLine($"{propertyAssignment}{GetNumberLiteral(propSchema, value)}{propertySuffix}");
                    break;
                case JsonValueKind.False:
                case JsonValueKind.True:
                    payloadSB.AppendLine($"{propertyAssignment}${value.GetBoolean().ToString().ToLowerInvariant()}{propertySuffix}");
                    break;
                case JsonValueKind.Null:
                    payloadSB.AppendLine($"{propertyAssignment}$null{propertySuffix}");
                    break;
                case JsonValueKind.Object:
                    // OpenTypes will have a null propSchemas. Lets see if the object contains '@odata.type'.
                    if (propSchema != null || value.ToString().Contains("@odata.type"))
                    {
                        payloadSB.AppendLine($"{propertyAssignment}@{{");
                        WriteJsonObjectValue(payloadSB, value, propSchema, indentManager);
                        payloadSB.AppendLine($"{indentManager.GetIndent()}}}{propertySuffix}");
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
            payloadSB.AppendLine($"{propertyAssignment}@(");
            indentManager.Indent();
            foreach (var item in value.EnumerateArray())
                WriteProperty(payloadSB, item, schema?.Items, indentManager, indentManager.GetIndent());
            indentManager.Unindent();
            payloadSB.AppendLine($"{indentManager.GetIndent()})");
        }

        private static string GetNumberLiteral(OpenApiSchema schema, JsonElement value)
        {
            if (schema == default) return default;
            return schema.Type switch
            {
                "integer" when schema.Format.Equals("int64") => $"{value.GetInt64()}",
                _ when schema.Format.Equals("float") => $"{value.GetDecimal()}",
                _ when schema.Format.Equals("double") => $"{value.GetDouble()}",
                _ => value.GetInt32().ToString(),
            };
        }

        private static string GetActionParametersList(params string[] parameters)
        {
            var nonEmptyParameters = parameters.Where(p => !string.IsNullOrEmpty(p));
            if (nonEmptyParameters.Any())
                return string.Join(" ", nonEmptyParameters.Aggregate((a, b) => $"{a}, {b}"));
            else return string.Empty;
        }
    }
}
