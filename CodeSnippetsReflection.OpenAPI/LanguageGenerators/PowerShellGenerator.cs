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
            // Load MgCommandMetadata file.
            string latestVersion = Directory.GetDirectories(powerShellModulePath).Max();
            string MgCommandMetadataPath = Directory.GetFiles($"{latestVersion}/custom/common", "MgCommandMetadata.json").FirstOrDefault();
            string jsonString = File.ReadAllText(MgCommandMetadataPath);
            psCommands = JsonSerializer.Deserialize<IList<PowerShellCommandInfo>>(jsonString);
        }
        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            var indentManager = new IndentManager();
            var snippetBuilder = new StringBuilder();

            IList<PowerShellCommandInfo> matchedCommands = GetCommandForRequest(snippetModel, snippetModel.Path, snippetModel.Method.ToString(), snippetModel.ApiVersion);
            var targetCommand = matchedCommands.FirstOrDefault();
            if (targetCommand != null)
            {
                string moduleName = targetCommand.Module;
                if (!string.IsNullOrEmpty(moduleName))
                    snippetBuilder.AppendLine($"Import-Module {modulePrefix}.{moduleName}{Environment.NewLine}");
                var (requestPayload, payloadVarName) = GetRequestPayloadAndVariableName(snippetModel, indentManager);
                if (!string.IsNullOrEmpty(requestPayload))
                    snippetBuilder.Append(requestPayload);
                //TODO: Handle query parameters.
                //var (queryParamsPayload, queryParamsVarName) = GetRequestQueryParameters(snippetModel, indentManager);
                //if (!string.IsNullOrEmpty(queryParamsPayload))
                //    snippetBuilder.Append(queryParamsPayload);

                var parameterList = GetActionParametersList(payloadVarName);

                snippetBuilder.AppendLine($"{targetCommand.Command} {parameterList}");
            }

            return snippetBuilder.ToString();
        }

        private static (string, string) GetRequestQueryParameters(SnippetModel model, IndentManager indentManager)
        {
            string requestParametersVarName = "requestParameters";
            var payloadSB = new StringBuilder();
            if (!string.IsNullOrEmpty(model.QueryString))
            {
                payloadSB.AppendLine($"{indentManager.GetIndent()}var {requestParametersVarName} = (q) =>");
                payloadSB.AppendLine($"{indentManager.GetIndent()}{{");
                indentManager.Indent();
                //var (queryString, replacements) = ReplaceNestedOdataQueryParameters(model.QueryString);
                //foreach (var queryParam in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                //{
                //    //if (queryParam.Contains("="))
                //    //{
                //    //    var kvPair = queryParam.Split('=', StringSplitOptions.RemoveEmptyEntries);
                //    //    payloadSB.AppendLine($"q.{indentManager.GetIndent()}{NormalizeQueryParameterName(kvPair[0])} = {GetQueryParameterValue(kvPair[1], replacements)};");
                //    //}
                //    //else
                //    //    payloadSB.AppendLine($"q.{indentManager.GetIndent()}{NormalizeQueryParameterName(queryParam)} = string.Empty;");
                //}
                indentManager.Unindent();
                payloadSB.AppendLine($"{indentManager.GetIndent()}}};");
                return (payloadSB.ToString(), requestParametersVarName);
            }
            return (default, default);
        }

        private IList<PowerShellCommandInfo> GetCommandForRequest(SnippetModel snippetModel,string path, string method, string apiVersion)
        {
            //TODO: Remove namespace from actions and functions for matches to succeed.
            // Tokenize uri by substituting parameter values with "{.*}".
            path = $"^{Regex.Replace(path, "(?<={)(.*?)(?=})", "(\\w*-\\w*|\\w*)")}$";
            if (psCommands.Count == 0)
                return default;

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
                    if (propSchema?.Format?.Equals("base64url", StringComparison.OrdinalIgnoreCase) ?? false)
                        payloadSB.AppendLine($"{propertyAssignment}Encoding.ASCII.GetBytes(\"{value.GetString()}\"){propertySuffix}");
                    else if (propSchema?.Format?.Equals("date-time", StringComparison.OrdinalIgnoreCase) ?? false)
                        payloadSB.AppendLine($"{propertyAssignment}DateTimeOffset.Parse(\"{value.GetString()}\"){propertySuffix}");
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
                "integer" when schema.Format.Equals("int64") => $"{value.GetInt64()}L",
                _ when schema.Format.Equals("float") => $"{value.GetDecimal()}f",
                _ when schema.Format.Equals("double") => $"{value.GetDouble()}d", //in MS Graph float & double are any of number, string and enum
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
