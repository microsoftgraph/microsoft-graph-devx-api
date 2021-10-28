using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators
{
    public class PowerShellGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
    {
        private const string requestBodyVarName = "params";
        private const string modulePrefix = "Microsoft.Graph";
        private const string modelNameSpace = modulePrefix + ".PowerShell.Models";
        private const string powerShellModulePath = "C:/Program Files/WindowsPowerShell/Modules/Microsoft.Graph.Authentication"; // TODO: load dynamically from PowerShell module path. Can fetched from `$env:PSModulePath`.
        private static IList<PowerShellCommandInfo> psCommands;
        public PowerShellGenerator()
        {
            if (psCommands == null)
            {
                // Load MgCommandMetadata file.
                string latestVersion = Directory.GetDirectories(powerShellModulePath).Max();
                string MgCommandMetadataPath = Directory.GetFiles($"{latestVersion}/custom/common", "MgCommandMetadata.json").FirstOrDefault();
                string jsonString = File.ReadAllText(MgCommandMetadataPath);
                psCommands = JsonSerializer.Deserialize<IList<PowerShellCommandInfo>>(jsonString);
            }
        }
        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            var indentManager = new IndentManager();
            var snippetBuilder = new StringBuilder();

            IList<PowerShellCommandInfo> matchedCommands = GetCommandForRequest(snippetModel, snippetModel.EndPathNode.Path, snippetModel.Method.ToString(), snippetModel.ApiVersion);
            var targetCommand = matchedCommands.FirstOrDefault();
            if (targetCommand != null)
            {
                string moduleName = targetCommand.Module;
                if (!string.IsNullOrEmpty(moduleName))
                    snippetBuilder.AppendLine($"Import-Module {modulePrefix}.{moduleName}");
                var (requestPayload, payloadVarName) = GetRequestPayloadAndVariableName(snippetModel, indentManager);
                if (!string.IsNullOrEmpty(requestPayload))
                    snippetBuilder.Append($"{Environment.NewLine}{requestPayload}");

                snippetBuilder.Append($"{Environment.NewLine}{targetCommand.Command}");

                string keySegmentParameter = GetKeySegmentParameters(snippetModel.PathNodes);
                if (!string.IsNullOrEmpty(keySegmentParameter))
                    snippetBuilder.Append($"{keySegmentParameter}");

                var (queryParamsPayload, queryParamsVarName) = GetRequestQueryParameters(snippetModel, indentManager);
                if (!string.IsNullOrEmpty(queryParamsPayload))
                    snippetBuilder.Append($" {queryParamsPayload}");

                var parameterList = GetActionParametersList(payloadVarName);
                if (!string.IsNullOrEmpty(parameterList))
                    snippetBuilder.Append($" {parameterList}");
            }

            return snippetBuilder.ToString();
        }

        private string GetKeySegmentParameters(IEnumerable<OpenApiUrlTreeNode> pathNodes)
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

        private static (string, string) GetRequestQueryParameters(SnippetModel model, IndentManager indentManager)
        {
            string requestParametersVarName = "requestParameters";
            var payloadSB = new StringBuilder();
            if (!string.IsNullOrEmpty(model.QueryString))
            {
                var (queryString, replacements) = ReplaceNestedOdataQueryParameters(model.QueryString);
                foreach (var queryParam in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (queryParam.Contains("="))
                    {
                        var kvPair = queryParam.Split('=', StringSplitOptions.RemoveEmptyEntries);
                        string parameterName = NormalizeQueryParameterName(kvPair[0]);
                        payloadSB.Append($"-{parameterName} {GetQueryParameterValue(parameterName, kvPair[1], replacements)}");
                    }
                    else
                        payloadSB.Append($"-{NormalizeQueryParameterName(queryParam)} = {""}");
                }
                return (payloadSB.ToString(), requestParametersVarName);
            }
            return (default, default);
        }

        private static string GetQueryParameterValue(string normalizedParameterName, string originalValue, Dictionary<string, string> replacements)
        {
            if (normalizedParameterName.Equals("CountVariable"))
            {
                return "CountVar";
            }

            if (originalValue.Equals("true", StringComparison.OrdinalIgnoreCase) || originalValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                return originalValue.ToLowerInvariant();
            else if (int.TryParse(originalValue, out var intValue))
                return intValue.ToString();
            else
            {
                var valueWithNested = originalValue.Split(',')
                                                    .Select(v => replacements.ContainsKey(v) ? v + replacements[v] : v)
                                                    .Aggregate((a, b) => $"{a},{b}");
                return $"\"{valueWithNested}\"";
            }
        }
        private static string NormalizeQueryParameterName(string queryParam)
        {
            string psParameterName = queryParam.TrimStart('$').ToFirstCharacterUpperCase();
            switch (psParameterName)
            {
                case "Select":
                    return "Property";
                case "Expand":
                    return "ExpandProperty";
                case "Count":
                    return "CountVariable";
                default:
                    return psParameterName;
            }
        }

        private static Regex nestedStatementRegex = new Regex(@"(\w+)(\([^)]+\))", RegexOptions.IgnoreCase);
        private static (string, Dictionary<string, string>) ReplaceNestedOdataQueryParameters(string queryParams)
        {
            var replacements = new Dictionary<string, string>();
            var matches = nestedStatementRegex.Matches(queryParams);
            if (matches.Any())
                foreach (Match match in matches)
                {
                    var key = match.Groups[1].Value;
                    var value = match.Groups[2].Value;
                    replacements.Add(key, value);
                    queryParams = queryParams.Replace(value, string.Empty);
                }
            return (queryParams, replacements);
        }

        private IList<PowerShellCommandInfo> GetCommandForRequest(SnippetModel snippetModel,string path, string method, string apiVersion)
        {
            if (psCommands.Count == 0)
                return default;

            path = path.Replace("\\", "/");
            //TODO: Remove namespace from actions and functions for matches to succeed.
            // Tokenize uri by substituting parameter values with "{.*}".
            path = $"^{Regex.Replace(path, "(?<={)(.*?)(?=})", "(\\w*-\\w*|\\w*)")}$";
            return psCommands.Where(c => c.Method == method && c.ApiVersion == apiVersion && Regex.Match(c.Uri, path).Success).ToList();
        }
                                                                                                                 
        private static (string, string) GetRequestPayloadAndVariableName(SnippetModel snippetModel, IndentManager indentManager)
        {
            if (string.IsNullOrWhiteSpace(snippetModel?.RequestBody))
                return (default, default);
            if (indentManager == null) throw new ArgumentNullException(nameof(indentManager));

            var payloadSB = new StringBuilder();
            switch (snippetModel.ContentType?.Split(';').First().ToLowerInvariant())
            {
                case "application/json":
                    if (!string.IsNullOrEmpty(snippetModel.RequestBody) &&
                        !"undefined".Equals(snippetModel.RequestBody, StringComparison.OrdinalIgnoreCase)) // graph explorer sends "undefined" as request body for some reason
                        using (var parsedBody = JsonDocument.Parse(snippetModel.RequestBody))
                        {
                            var schema = snippetModel.RequestSchema;
                            payloadSB.AppendLine($"{indentManager.GetIndent()}${requestBodyVarName} = @{{");
                            WriteJsonObjectValue(payloadSB, parsedBody.RootElement, schema, indentManager);
                            payloadSB.AppendLine("}");
                        }
                    break;
                case "application/octect-stream":
                    // TODO: Handle streams. Support is limited.
                    //payloadSB.AppendLine($"using var {requestBodyVarName} = new MemoryStream(); //stream to upload");
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported content type: {snippetModel.ContentType}");
            }
            return (payloadSB.ToString(), $"@{requestBodyVarName}");
        }

        private static void WriteJsonObjectValue(StringBuilder payloadSB, JsonElement value, Microsoft.OpenApi.Models.OpenApiSchema schema, IndentManager indentManager, bool includePropertyAssignment = true)
        {
            if (value.ValueKind != JsonValueKind.Object) throw new InvalidOperationException($"Expected JSON object and got {value.ValueKind}");
            indentManager.Indent();
            var propertiesAndSchema = value.EnumerateObject()
                                            .Select(x => new Tuple<JsonProperty, OpenApiSchema>(x, schema.GetPropertySchema(x.Name)));
            foreach (var propertyAndSchema in propertiesAndSchema)
            {
                var propertyName = propertyAndSchema.Item1.Name.ToFirstCharacterUpperCase();
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
                    // TODO: Update to reflect current state of PowerShell SDK.
                    if (propSchema?.Format?.Equals("base64url", StringComparison.OrdinalIgnoreCase) ?? false)
                        payloadSB.AppendLine($"{propertyAssignment}[System.Text.Encoding]::ASCII.GetBytes(\"{value.GetString()}\"){propertySuffix}");
                    else if (propSchema?.Format?.Equals("date-time", StringComparison.OrdinalIgnoreCase) ?? false)
                        payloadSB.AppendLine($"{propertyAssignment}[System.DateTimeOffset]::Parse(\"{value.GetString()}\"){propertySuffix}");
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
                    if (propSchema != null)
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
            var genericType = schema.GetSchemaTitle().ToFirstCharacterUpperCase() ?? value.EnumerateArray().First().ValueKind.ToString();
            payloadSB.AppendLine($"{propertyAssignment}@(");
            indentManager.Indent();
            foreach (var item in value.EnumerateArray())
                WriteProperty(payloadSB, item, schema, indentManager, indentManager.GetIndent());
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
