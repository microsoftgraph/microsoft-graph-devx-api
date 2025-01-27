// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators
{
    public class PowerShellGenerator(IList<PowerShellCommandInfo> psCommands)
        : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
    {
        private const string requestBodyVarName = "params";
        private const string modulePrefix = "Microsoft.Graph";
        private static readonly Regex meSegmentRegex = new("^/me($|(?=/))", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        private static readonly Regex encodedQueryParamsPayLoad = new(@"\w*\+", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        private static readonly Regex wrongQoutesInStringLiterals = new(@"""\{", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        private static readonly Regex functionWithParams = new(@"^[0-9a-zA-Z\- \/_?:.,\s]+\([\w*='{\w*}\',]*\)|^[0-9a-zA-Z\- \/_?:.,\s]+\([\w*='\w*\',]*\)|^[0-9a-zA-Z\- \/_?:.,\s]+\([\w*={w*},]*\)|^[0-9a-zA-Z\- \/_?:.,\s]+\([\w*=<w*>,]*\)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            var indentManager = new IndentManager();
            var snippetBuilder = new StringBuilder();
            var cleanPath = snippetModel.EndPathNode.Path.Replace("\\", "/");
            var isMeSegment = meSegmentRegex.IsMatch(cleanPath);
            var hasGraphPrefix = cleanPath.Contains("graph", StringComparison.OrdinalIgnoreCase);
            var isIdentityProvider = snippetModel.RootPathNode.Path.StartsWith("\\identityProviders", StringComparison.OrdinalIgnoreCase);
            var lastPathSegment = snippetModel.EndPathNode.Segment;
            var hasMicrosoftPrefix = lastPathSegment.StartsWith("microsoft", StringComparison.OrdinalIgnoreCase);
            cleanPath = SubstituteIdentityProviderSegment(cleanPath, isIdentityProvider);
            cleanPath = SubstituteGraphSegment(cleanPath, hasGraphPrefix);
            cleanPath = SubstituteMicrosoftSegment(cleanPath, hasMicrosoftPrefix, lastPathSegment);
            var (path, additionalKeySegmentParmeter) = SubstituteMeSegment(isMeSegment, cleanPath, lastPathSegment);
            IList<PowerShellCommandInfo> matchedCommands = GetCommandForRequest(path, snippetModel.Method.ToString(), snippetModel.ApiVersion);
            var targetCommand = matchedCommands.FirstOrDefault();
            if (targetCommand != null)
            {
                string moduleName = targetCommand.Module;
                if (!string.IsNullOrEmpty(moduleName))
                    snippetBuilder.AppendLine($"Import-Module {modulePrefix}.{moduleName}");
                var (requestPayload, payloadVarName) = GetRequestPayloadAndVariableName(snippetModel, indentManager);
                if (!string.IsNullOrEmpty(requestPayload))
                {
                    if (wrongQoutesInStringLiterals.IsMatch(requestPayload))
                    {
                        requestPayload = requestPayload.Replace("\"{", "'{").Replace("}\"", "}'");
                    }
                    snippetBuilder.Append($"{Environment.NewLine}{requestPayload}");
                }


                if (isMeSegment)
                    snippetBuilder.Append($"{Environment.NewLine}# A UPN can also be used as -UserId.");

                snippetBuilder.Append($"{Environment.NewLine}{targetCommand.Command}");

                if (!string.IsNullOrEmpty(additionalKeySegmentParmeter))
                    snippetBuilder.Append($"{additionalKeySegmentParmeter}");

                var commandParameters = GetCommandParameters(snippetModel, payloadVarName);
                if (!string.IsNullOrEmpty(commandParameters))
                    snippetBuilder.Append($"{commandParameters}");
                if (RequiresMIMEContentOutPut(snippetModel, path))
                {
                    //Allows genration of an output file for MIME content of the message
                    snippetBuilder.Append(" -OutFile $outFileId");
                }
            }
            else
            {
                throw new NotImplementedException($"{path} and {snippetModel.Method} operation is not supported in the sdk");
            }
            return snippetBuilder.ToString();
        }
        /// <summary>
        /// Checks if the path has an optional query parameter. e.g $value
        /// The parameter can be used to get the mime content of a message
        /// Mime content should be stored in a file, therefore powershell snippets will have '-OutFile $outFileId' appended at the end of the snipper body
        /// </summary>
        /// <param name="snippetModel"></param>
        /// <param name="path"></param>
        /// <returns>true/false</returns>
        private static bool RequiresMIMEContentOutPut(SnippetModel snippetModel, string path)
        {
            int lastIndex = path.LastIndexOf('/');
            var lastValue = path.Substring(lastIndex + 1);
            if (lastValue.Equals("$value") && snippetModel.Method == HttpMethod.Get) return true;
            return false;
        }
        private static string GetCommandParameters(SnippetModel snippetModel, string payloadVarName)
        {
            var payloadSB = new StringBuilder();
            string keySegmentParameter = GetKeySegmentParameters(snippetModel.PathNodes);
            if (!string.IsNullOrEmpty(keySegmentParameter))
                payloadSB.Append($"{keySegmentParameter}");

            var queryParamsPayload = GetRequestQueryParameters(snippetModel);
            if (!string.IsNullOrEmpty(queryParamsPayload))
                payloadSB.Append($" {ReturnCleanParamsPayload(queryParamsPayload)}");

            var parameterList = GetActionParametersList(payloadVarName);
            if (!string.IsNullOrEmpty(parameterList))
                payloadSB.Append($" {parameterList}");

            var functionParameterList = GetFunctionParameterList(snippetModel);
            if (!string.IsNullOrEmpty(functionParameterList))
                payloadSB.Append($" {functionParameterList}");

            var requestHeadersPayload = GetSupportedRequestHeaders(snippetModel);
            if (!string.IsNullOrEmpty(requestHeadersPayload))
                payloadSB.Append(requestHeadersPayload);

            return payloadSB.ToString();
        }

        public static string ReturnCleanParamsPayload(string queryParamsPayload)
        {
            if (encodedQueryParamsPayLoad.IsMatch(queryParamsPayload))
                return queryParamsPayload.Replace("+", " ");
            return queryParamsPayload;
        }
        private static (string, string) SubstituteMeSegment(bool isMeSegment, string path, string lastPathSegment)
        {
            string additionalKeySegmentParmeter = default;
            if (isMeSegment)
            {
                path = meSegmentRegex.Replace(path, "/users/{user-id}");
                additionalKeySegmentParmeter = $" -UserId $userId";
            }
            if (lastPathSegment.Contains("()"))
            {
                path = path.RemoveFunctionBraces();
            }
            return (path, additionalKeySegmentParmeter);
        }
        private static string SubstituteGraphSegment(string path, bool hasGraphPrefix)
        {
            if (hasGraphPrefix)
                path = path.Replace("graph.", string.Empty);
            return path;
        }
        private static string SubstituteMicrosoftSegment(string path, bool hasMicrosoftSegment, string lastSegmentPath)
        {
            if (hasMicrosoftSegment)
            {
                var splittedPath = path.Split('/');
                path = path.Replace(splittedPath[splittedPath.Length - 1], lastSegmentPath);
            }

            return path;
        }
        private static string SubstituteIdentityProviderSegment(string path, bool isIdentityProvider)
        {
            if (isIdentityProvider)
                path = path.Replace("identityProviders", "identity/identityProviders");
            return path;
        }

        private static string GetSupportedRequestHeaders(SnippetModel snippetModel)
        {
            var payloadSB = new StringBuilder();
            if (Enum.TryParse(snippetModel.Method.Method, true, out OperationType method))
            {
                var operation = snippetModel.EndPathNode.PathItems.Select(p => p.Value.Operations[method]).FirstOrDefault();
                foreach (var header in snippetModel.RequestHeaders)
                {
                    var parameter = operation?.Parameters.FirstOrDefault(p => p.Name.Equals(header.Key, StringComparison.OrdinalIgnoreCase));
                    if (parameter != null)
                    {
                        var headerValue = header.Value.FirstOrDefault();
                        var headerName = parameter.Name.Replace("-", string.Empty);
                        var collection = Regex.Matches(headerValue, "\\\"(.*?)\\\"", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

                        string quotedString = collection.Count > 0 ? collection.First().Value : string.Empty;
                        headerValue = (!string.IsNullOrEmpty(quotedString) && !string.IsNullOrEmpty(headerValue)) ? headerValue.Replace(quotedString, "'" + quotedString + "'") : headerValue;

                        payloadSB.AppendLine($" -{headerName} {headerValue} ");
                    }
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
            if (normalizedParameterName.Equals("Search"))
                return $"'\"{originalValue.Replace("+", " ")}\"'";
            if (originalValue.Equals("true", StringComparison.OrdinalIgnoreCase) || originalValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                return originalValue.ToLowerInvariant();
            else if (int.TryParse(originalValue, out var intValue))
                return intValue.ToString();
            else
            {
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
            return psParameterName switch
            {
                "Select" => "Property",
                "Expand" => "ExpandProperty",
                "Count" => "CountVariable",
                "Orderby" => "Sort",
                _ => psParameterName
            };
        }

        private static readonly Regex nestedStatementRegex = new(@"(\w+|\w+\/\w+)(\([^)]+\))", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        private static (string, Dictionary<string, string>) ReplaceNestedOdataQueryParameters(string queryParams)
        {
            var replacements = new Dictionary<string, string>();
            var matches = nestedStatementRegex.Matches(queryParams);
            if (matches.Count != 0)
                foreach (GroupCollection groupCollection in matches.Select(x => x.Groups))
                {
                    var key = groupCollection[1].Value;
                    var value = groupCollection[2].Value;
                    if (value.Contains('=') && replacements.TryAdd(key, value))
                        queryParams = queryParams.Replace(value, string.Empty);
                }
            return (queryParams, replacements);
        }

        private static readonly Regex keyIndexRegex = new(@"(?<={)(.*?)(?=})", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        private List<PowerShellCommandInfo> GetCommandForRequest(string path, string method, string apiVersion)
        {
            if (psCommands.Count == 0)
                return [];
            path = Regex.Escape(SnippetModel.TrimNamespace(path));
            // Tokenize uri by substituting parameter values with "{.*}" e.g, "/users/{user-id}" to "/users/{.*}".
            path = $"^{keyIndexRegex.Replace(path, "(\\w*-\\w*|\\w*)")}$";
            return psCommands.Where(c => c.Method == method && c.ApiVersion == apiVersion && Regex.Match(c.Uri,
                path, RegexOptions.None, TimeSpan.FromSeconds(5)).Success).ToList();
        }
        private static (string, string) GetRequestPayloadAndVariableName(SnippetModel snippetModel, IndentManager indentManager)
        {
            if (string.IsNullOrWhiteSpace(snippetModel?.RequestBody)
                || "undefined".Equals(snippetModel?.RequestBody, StringComparison.OrdinalIgnoreCase)) // graph explorer sends "undefined" as request body for some reason
                return (default, default);

            ArgumentNullException.ThrowIfNull(indentManager);

            if (isValidJson(snippetModel?.RequestBody) && string.IsNullOrWhiteSpace(snippetModel?.ContentType))
            {
                snippetModel.ContentType = "application/json";
            }

            var payloadSB = new StringBuilder();
            switch (snippetModel.ContentType?.Split(';').First().ToLowerInvariant())
            {
                case "application/json":
                    var parsedBody = JsonSerializer.Deserialize<JsonElement>(snippetModel.RequestBody, JsonHelper.JsonSerializerOptions);
                    var schema = snippetModel.RequestSchema;
                    payloadSB.AppendLine($"{indentManager.GetIndent()}${requestBodyVarName} = @{{");
                    WriteJsonObjectValue(payloadSB, parsedBody, schema, indentManager);
                    payloadSB.AppendLine("}");
                    break;
                case "image/jpeg":
                    payloadSB.AppendLine($"{indentManager.GetIndent()}${requestBodyVarName} = Binary data for the image");
                    break;
                case "application/zip":
                    payloadSB.AppendLine($"{indentManager.GetIndent()}${requestBodyVarName} = {snippetModel?.RequestBody}");
                    break;
                case "text/plain":
                    payloadSB.AppendLine($"{indentManager.GetIndent()}${requestBodyVarName} = {snippetModel?.RequestBody}");
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported content type: {snippetModel.ContentType}");
            }
            return (payloadSB.ToString(), $"-BodyParameter ${requestBodyVarName}");
        }

        private static bool isValidJson(string requestBody)
        {
            try
            {
                JsonSerializer.Deserialize<JsonElement>(requestBody, JsonHelper.JsonSerializerOptions);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void WriteJsonObjectValue(StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager, bool includePropertyAssignment = true)
        {
            if (value.ValueKind != JsonValueKind.Object) throw new InvalidOperationException($"Expected JSON object and got {value.ValueKind}");
            indentManager.Indent();
            var propertiesAndSchema = value.EnumerateObject()
                                            .Select(x => new Tuple<JsonProperty, OpenApiSchema>(x, schema?.GetPropertySchema(x.Name)));
            foreach (var propertyAndSchema in propertiesAndSchema)
            {
                var propertyName = propertyAndSchema.Item1.Name;
                // Enclose in quotes if property name contains a non-word character.
                if (Regex.IsMatch(propertyName, "\\W", RegexOptions.None, TimeSpan.FromSeconds(5))) { propertyName = $"\"{propertyName}\""; }
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
                    payloadSB.AppendLine($"{propertyAssignment}@{{");
                    WriteJsonObjectValue(payloadSB, value, propSchema, indentManager);
                    payloadSB.AppendLine($"{indentManager.GetIndent()}}}{propertySuffix}");
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

            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    indentManager.Indent(1);
                    payloadSB.AppendLine($"{indentManager.GetIndent()}@{{");
                    WriteJsonObjectValue(payloadSB, item, schema?.Items, indentManager);
                    payloadSB.AppendLine($"{indentManager.GetIndent()}}}");

                }
                else
                {
                    WriteProperty(payloadSB, item, schema?.Items, indentManager, indentManager.GetIndent());

                }
                indentManager.Indent(-1);
            }
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

        private static string GetFunctionParameterList(SnippetModel snippetModel)
        {
            var snippetPaths = snippetModel.Path;
            var paramBuilder = new StringBuilder();
            if (functionWithParams.IsMatch(snippetPaths))
            {
                var paths = snippetPaths.Split("/");
                var function = paths.Last();
                var functionItems = function.Split("(");
                var functionParameters = functionItems[1].Split(",");
                foreach (var param in functionParameters)
                {
                    var paramKeys = param.Split("=")[0];
                    var paramKey = $"-{paramKeys.ToFirstCharacterUpperCase()} ${paramKeys}Id ";
                    paramBuilder.Append(paramKey);
                }

            }
            return paramBuilder.ToString();
        }

    }
}
