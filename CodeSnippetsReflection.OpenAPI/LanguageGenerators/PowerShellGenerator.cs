﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private const string modelNameSpace = modulePrefix + ".PowerShell.Models";
        private const string authModuleName = modulePrefix + ".Authentication";
        private static IList<PowerShellCommandInfo> psCommands = default;
        private static Regex meSegmentRegex = new Regex("/me/", RegexOptions.Compiled);
        public PowerShellGenerator()
        {
            if (psCommands == default)
            {
                string authModulePath = default;
                // Load MgCommandMetadata file.
                foreach(string modulePath in Environment.GetEnvironmentVariable("PSModulePath")?.Split(";")){
                    if (Directory.Exists($"{modulePath}/{authModuleName}"))
                        authModulePath = Directory.GetDirectories($"{modulePath}/{authModuleName}").Max();
                }
                if (authModulePath == default)
                    throw new Exception("Microsoft.Graph PowerShell SDK could not be found on this machine. Please install the SDK using 'Install-Module Microsoft.Graph'.");
                string MgCommandMetadataPath = Directory.GetFiles($"{authModulePath}/custom/common", "MgCommandMetadata.json").FirstOrDefault();
                string jsonString = File.ReadAllText(MgCommandMetadataPath);
                psCommands = JsonSerializer.Deserialize<IList<PowerShellCommandInfo>>(jsonString);
            }
        }
        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            var indentManager = new IndentManager();
            var snippetBuilder = new StringBuilder();

            string additionalKeySegmentParmeter = default;
            var path = snippetModel.EndPathNode.Path.Replace("\\", "/");
            if (path.StartsWith("/me/"))
            {
                path = meSegmentRegex.Replace(path, "/users/{user-id}/", 1);
                additionalKeySegmentParmeter = $" -UserId $userId";
            }
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

                snippetBuilder.Append($"{Environment.NewLine}{targetCommand.Command}");

                if (!string.IsNullOrEmpty(additionalKeySegmentParmeter))
                    snippetBuilder.Append($"{additionalKeySegmentParmeter}");

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
                var (queryString, replacements) = ReplaceNestedOdataQueryParameters(Uri.UnescapeDataString(model.QueryString));
                foreach (var queryParam in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (queryParam.Contains("="))
                    {
                        var kvPair = queryParam.Split('=', StringSplitOptions.RemoveEmptyEntries);
                        string parameterName = NormalizeQueryParameterName(kvPair[0]);
                        // Add -ConsistencyLevel eventual when CountVariable is present.
                        payloadSB.Append($"-{parameterName} {GetQueryParameterValue(parameterName, kvPair[1].Trim('"'), replacements)} ");
                    }
                    else
                        payloadSB.Append($"-{NormalizeQueryParameterName(queryParam)} = {""} ");
                }
                return (payloadSB.ToString(), requestParametersVarName);
            }
            return (default, default);
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
                return $"\"{valueWithNested}\"";
            }
        }
        private static string NormalizeQueryParameterName(string queryParam)
        {
            string psParameterName = queryParam.TrimStart('$').ToFirstCharacterUpperCase();
            return psParameterName switch {
                "Select" => "Property",
                "Expand" => "ExpandProperty",
                "Count" => "CountVariable",
                _ => psParameterName
            };
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

        private IList<PowerShellCommandInfo> GetCommandForRequest(string path, string method, string apiVersion)
        {
            if (psCommands.Count == 0)
                return default;
            path = Regex.Escape(TrimNamespace(path));
            // Tokenize uri by substituting parameter values with "{.*}".
            path = $"^{Regex.Replace(path, "(?<={)(.*?)(?=})", "(\\w*-\\w*|\\w*)")}$";
            return psCommands.Where(c => c.Method == method && c.ApiVersion == apiVersion && Regex.Match(c.Uri, path).Success).ToList();
        }

        private static Regex namespaceRegex = new Regex("\\/Microsoft.Graph.(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private string TrimNamespace(string path)
        {
            Match namespaceMatch = namespaceRegex.Match(path);
            if (namespaceMatch.Success)
            {
                string fqnAction = namespaceMatch.Groups[0].Value;
                // Trim nested namespace segments.
                string[] nestedActionNamespaceSegments = namespaceMatch.Groups[1].Value.Split("/.");
                // Remove trailing '()' from functions.
                string actionName  = nestedActionNamespaceSegments[nestedActionNamespaceSegments.Length - 1].Replace("()", "");
                path = Regex.Replace(path, Regex.Escape(fqnAction), $"/{actionName}");
            }
            return path;
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
            var genericType = schema.GetSchemaTitle().ToFirstCharacterUpperCase() ?? value.EnumerateArray().First().ValueKind.ToString();
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