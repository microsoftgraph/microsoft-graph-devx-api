// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OData.Edm.Csdl;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;
using Microsoft.OpenApi.Writers;
using Microsoft.OpenApi.OData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text;
using OpenAPIService.Common;
using UtilityService;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace OpenAPIService
{
    public enum OpenApiStyle
    {
        PowerShell,
        PowerPlatform,
        Plain,
        GEAutocomplete
    }

    public class OpenApiService
    {
        private static readonly ConcurrentDictionary<Uri, OpenApiDocument> _OpenApiDocuments = new ConcurrentDictionary<Uri, OpenApiDocument>();
        private static OpenApiUrlTreeNode _openApiRootNode = OpenApiUrlTreeNode.Create();
        private static readonly IDictionary<string, string> _openApiTraceProperties = new Dictionary<string, string> { { "OpenApi", "OpenApiService" } };
        private static readonly object _telemetrySetLock = new();
        private static TelemetryClient _telemetryClient;
        public static TelemetryClient TelemetryClient
        {
            set
            {
                lock (_telemetrySetLock)
                {
                    if (_telemetryClient == null)
                    {
                        _telemetryClient = value;
                    }
                }
            }
        }

        /// <summary>
        /// Create partial OpenAPI document based on the provided predicate.
        /// </summary>
        /// <param name="source">The target <see cref="OpenApiDocument"/>.</param>
        /// <param name="title">The OpenAPI document title.</param>
        /// <param name="graphVersion">Version of the target Microsoft Graph API.</param>
        /// <param name="predicate">A predicate function.</param>
        /// <returns>A partial OpenAPI document.</returns>
        public static OpenApiDocument CreateFilteredDocument(OpenApiDocument source, string title, string graphVersion, Func<OpenApiOperation, bool> predicate)
        {
            _telemetryClient?.TrackTrace("Creating subset OpenApi document",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            var subset = new OpenApiDocument
            {
                Info = new OpenApiInfo()
                {
                    Title = title,
                    Version = graphVersion
                },

                Components = new OpenApiComponents()
            };
            var aadv2Scheme = new OpenApiSecurityScheme()
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows()
                {
                    AuthorizationCode = new OpenApiOAuthFlow()
                    {
                        AuthorizationUrl = new Uri(Constants.GraphConstants.GraphAuthorizationUrl),
                        TokenUrl = new Uri(Constants.GraphConstants.GraphTokenUrl)
                    }
                },
                Reference = new OpenApiReference() { Id = "azureaadv2", Type = ReferenceType.SecurityScheme },
                UnresolvedReference = false
            };
            subset.Components.SecuritySchemes.Add("azureaadv2", aadv2Scheme);

            subset.SecurityRequirements.Add(new OpenApiSecurityRequirement() { { aadv2Scheme, new string[] { } } });

            subset.Servers.Add(new OpenApiServer() { Description = "Core", Url = string.Format(Constants.GraphConstants.GraphUrl, graphVersion) });

            var results = FindOperations(source, predicate);
            foreach (var result in results)
            {
                OpenApiPathItem pathItem;
                string pathKey = FormatPathFunctions(result.CurrentKeys.Path, result.Operation.Parameters);

                if (subset.Paths == null)
                {
                    subset.Paths = new OpenApiPaths();
                    pathItem = new OpenApiPathItem();
                    subset.Paths.Add(pathKey, pathItem);
                }
                else
                {
                    if (!subset.Paths.TryGetValue(pathKey, out pathItem))
                    {
                        pathItem = new OpenApiPathItem();
                        subset.Paths.Add(pathKey, pathItem);
                    }
                }

                pathItem.Operations.Add((OperationType)result.CurrentKeys.Operation, result.Operation);
            }

            if (subset.Paths == null)
            {
                throw new ArgumentException("No paths found for the supplied parameters.");
            }

            CopyReferences(subset);

            _telemetryClient?.TrackTrace("Finished creating subset OpenApi document",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            return subset;
        }

        /// <summary>
        /// Create predicate function based on passed query parameters
        /// </summary>
        /// <param name="operationIds">Comma delimited list of operationIds or * for all operations.</param>
        /// <param name="tags">Comma delimited list of tags or a single regex.</param>
        /// <param name="url">Url path to match with Operation Ids.</param>
        /// <param name="source">The target <see cref="OpenApiDocument"/>.</param>
        /// <param name="graphVersion">Version of the target Microsoft Graph API.</param>
        /// <param name="forceRefresh">Whether to reload the OpenAPI document from source.</param>
        /// <returns>A predicate.</returns>
        public static Func<OpenApiOperation, bool> CreatePredicate(string operationIds, string tags, string url,
            OpenApiDocument source, string graphVersion = "v1.0", bool forceRefresh = false)
        {
            string predicateSource = null;
            _telemetryClient?.TrackTrace("Creating predicate",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            if (url != null && (operationIds != null || tags != null))
            {
                throw new InvalidOperationException("Cannot filter by url and either operationIds and tags at the same time.");
            }
            else if (operationIds != null && tags != null)
            {
                throw new InvalidOperationException("Cannot filter by operationIds and tags at the same time.");
            }

            Func<OpenApiOperation, bool> predicate;
            if (operationIds != null)
            {
                if (operationIds == "*")
                {
                    predicate = (o) => true;  // All operations
                }
                else
                {
                    var operationIdsArray = operationIds.Split(',');
                    predicate = (o) => operationIdsArray.Contains(o.OperationId);
                }

                predicateSource = $"operationIds: {operationIds}";
            }
            else if (tags != null)
            {
                var tagsArray = tags.Split(',');
                if (tagsArray.Length == 1)
                {
                    var regex = new Regex(tagsArray[0]);

                    predicate = (o) => o.Tags.Any(t => regex.IsMatch(t.Name));
                }
                else
                {
                    predicate = (o) => o.Tags.Any(t => tagsArray.Contains(t.Name));
                }

                predicateSource = $"tags: {tags}";
            }
            else if (url != null)
            {
                if (forceRefresh)
                {
                    _telemetryClient?.TrackTrace($"{nameof(forceRefresh)} requested; creating new OpenApiUrlTreeNode",
                                                SeverityLevel.Information,
                                                _openApiTraceProperties);

                    _openApiRootNode = CreateOpenApiUrlTreeNode(source, graphVersion);

                    _telemetryClient?.TrackTrace("Finished creating new OpenApiUrlTreeNode",
                                                SeverityLevel.Information,
                                                _openApiTraceProperties);
                }
                else if (!_openApiRootNode.PathItems.ContainsKey(graphVersion))
                {
                    _telemetryClient?.TrackTrace($"Attaching '{graphVersion}' source document to the OpenApiUrlTreeNode",
                                               SeverityLevel.Information,
                                               _openApiTraceProperties);

                    _openApiRootNode.Attach(source, graphVersion);

                    _telemetryClient?.TrackTrace($"Finished attaching '{graphVersion}' source document to the OpenApiUrlTreeNode",
                                                SeverityLevel.Information,
                                                _openApiTraceProperties);
                }

                url = url.BaseUriPath()
                         .UriTemplatePathFormat();

                OpenApiOperation[] openApiOps = GetOpenApiOperations(_openApiRootNode, url, graphVersion);

                if (!(openApiOps?.Any() ?? false))
                {
                    throw new ArgumentException("The url supplied could not be found.");
                }

                // Fetch the corresponding Operations Id(s) for the matched url
                string[] operationIdsArray = openApiOps.Select(x => x.OperationId).ToArray();

                predicate = (o) => operationIdsArray.Contains(o.OperationId);

                predicateSource = $"url: {url}";
            }
            else
            {
                throw new InvalidOperationException("Either operationIds, tags or url need to be specified.");
            }

            _telemetryClient?.TrackTrace($"Finished creating predicate for {predicateSource}",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);
            return predicate;
        }

        /// <summary>
        /// Creates an <see cref="OpenApiUrlTreeNode"/> from an <see cref="OpenApiDocument"/>.
        /// </summary>
        /// <param name="source">The target <see cref="OpenApiDocument"/>.</param>
        /// <param name="label">Name tag for labelling the nodes in the directory structure.</param>
        /// <returns>The created <see cref="OpenApiUrlTreeNode"/>.</returns>
        private static OpenApiUrlTreeNode CreateOpenApiUrlTreeNode(OpenApiDocument source, string label)
        {
            return source == null ? null : OpenApiUrlTreeNode.Create(source, label);
        }

        /// <summary>
        /// Retrieves an array of <see cref="OpenApiOperation"/> for a given url path from an
        /// <see cref="OpenApiUrlTreeNode"/>.
        /// </summary>
        /// <param name="rootNode">The target <see cref="OpenApiUrlTreeNode"/> root node.</param>
        /// <param name="relativeUrl">The relative url path to retrieve
        /// the array of <see cref="OpenApiOperation"/> from.</param>
        /// <param name="label">The name of the key for the target operations in the node's PathItems dictionary.</param>
        /// <returns>The array of <see cref="OpenApiOperation"/> for a given url path.</returns>
        private static OpenApiOperation[] GetOpenApiOperations(OpenApiUrlTreeNode rootNode, string relativeUrl, string label)
        {
            Utils.CheckArgumentNull(rootNode, nameof(rootNode));
            Utils.CheckArgumentNullOrEmpty(relativeUrl, nameof(relativeUrl));
            Utils.CheckArgumentNullOrEmpty(label, nameof(label));

            _telemetryClient?.TrackTrace($"Fetching OpenApiOperations for url path '{relativeUrl}' from the OpenApiUrlTreeNode",
                                        SeverityLevel.Information,
                                        _openApiTraceProperties);

            if (relativeUrl.Equals("/", StringComparison.Ordinal))
            {
                // root path
                if (rootNode.HasOperations(label))
                {
                    return rootNode.PathItems[label].Operations.Values.ToArray();
                }
            }

            var urlSegments = relativeUrl.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            OpenApiOperation[] operations = null;
            var targetChild = rootNode;

            /* This will help keep track of whether we've skipped a segment
             * in the target url due to a possible parameter naming mismatch
             * with the corresponding OpenApiUrlTreeNode target child segment.
             */
            int parameterNameOffset = 0;
            bool matchFound = false;

            for (int i = 0; i < urlSegments?.Length; i++)
            {
                var tempTargetChild = targetChild?.Children
                                                  .FirstOrDefault(x => x.Key.Equals(urlSegments[i],
                                                                    StringComparison.OrdinalIgnoreCase)).Value;

                // Segment name mismatch
                if (tempTargetChild == null)
                {
                    if (i == 0)
                    {
                        /* If no match and we are at the 1st segment of the relative url,
                         * exit; no need to continue matching subsequent segments.
                         */
                        break;
                    }

                    /* Attempt to get the parameter segment from the children of the current node:
                     * We are assuming a failed match because of different parameter namings
                     * between the relative url segment and the corresponding OpenApiUrlTreeNode segment name
                     * ex.: matching '/users/12345/messages' with '/users/{user-id}/messages'
                     */
                    tempTargetChild = targetChild.Children
                                                 .FirstOrDefault(x => x.Value.IsParameter).Value;

                    /* If no parameter segment exists in the children of the
                     * current node or we've already skipped a parameter
                     * segment in the relative url from the last pass,
                     * then exit; there's no match.
                     */
                    if (tempTargetChild == null || parameterNameOffset > 0)
                    {
                        break;
                    }

                    /* To help us know we've skipped a
                     * corresponding segment in the relative url.
                     */
                    parameterNameOffset++;
                }
                else
                {
                    parameterNameOffset = 0;
                }

                // Move to the next segment
                targetChild = tempTargetChild;

                // We want the operations of the last segment of the path.
                if (i == urlSegments.Length - 1)
                {
                    if (targetChild.HasOperations(label))
                    {
                        operations = targetChild.PathItems[label].Operations.Values.ToArray();
                    }

                    matchFound = true;
                }
            }

            if (matchFound)
            {
                _telemetryClient?.TrackTrace($"Finished fetching OpenApiOperations for url path '{relativeUrl}' from the OpenApiUrlTreeNode." +
                                             $"Matched path: {targetChild.Path}",
                                             SeverityLevel.Information,
                                             _openApiTraceProperties);
            }
            else
            {

                _telemetryClient?.TrackTrace($"No match found in the OpenApiUrlTreeNode for url '{relativeUrl}'",
                                SeverityLevel.Information,
                                _openApiTraceProperties);
            }

            return operations;
        }

		/// <summary>
		/// Create a representation of the OpenApiDocument to return from an API
		/// </summary>
		/// <param name="subset">OpenAPI document.</param>
		/// <param name="styleOptions">The modal object containing the required styling options.</param>
		/// <returns>A memory stream.</returns>
		public static MemoryStream SerializeOpenApiDocument(OpenApiDocument subset, OpenApiStyleOptions styleOptions)
        {
            _telemetryClient?.TrackTrace($"Serializing the subset OpenApiDocument document for '{styleOptions.OpenApiFormat}' format",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            var stream = new MemoryStream();
            var sr = new StreamWriter(stream);
            OpenApiWriterBase writer;

            if (styleOptions.OpenApiFormat == Constants.OpenApiConstants.Format_Yaml)
            {
                if (styleOptions.InlineLocalReferences)
                {
                    writer = new OpenApiYamlWriter(sr,
                        new OpenApiWriterSettings { ReferenceInline = ReferenceInlineSetting.InlineLocalReferences });
                }
                else
                {
                    writer = new OpenApiYamlWriter(sr);
                }
            }
            else // json
            {
                if (styleOptions.InlineLocalReferences)
                {
                    writer = new OpenApiJsonWriter(sr,
                        new OpenApiWriterSettings { ReferenceInline = ReferenceInlineSetting.InlineLocalReferences });
                }
                else
                {
                    writer = new OpenApiJsonWriter(sr);
                }
            }

            if (styleOptions.OpenApiVersion == Constants.OpenApiConstants.OpenApiVersion_2)
            {
                subset.SerializeAsV2(writer);
            }
            else
            {
                subset.SerializeAsV3(writer);
            }
            sr.Flush();
            stream.Position = 0;

            _telemetryClient?.TrackTrace($"Finished serializing the subset OpenApiDocument document for '{styleOptions.OpenApiFormat}' format",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            return stream;
        }

        /// <summary>
        /// Gets an OpenAPI document version of Microsoft Graph based on CSDL document
        /// from a dictionary cache or gets a new instance.
        /// </summary>
        /// <param name="graphUri">The uri of the Microsoft Graph metadata doc.</param>
        /// <param name="forceRefresh">Whether to reload the OpenAPI document from source.</param>
        /// <returns>A task of the value of an OpenAPI document.</returns>
        public static async Task<OpenApiDocument> GetGraphOpenApiDocumentAsync(string graphUri, bool forceRefresh)
        {
            var csdlHref = new Uri(graphUri);
            if (!forceRefresh && _OpenApiDocuments.TryGetValue(csdlHref, out OpenApiDocument doc))
            {
                _telemetryClient?.TrackTrace("Fetch the OpenApi document from the cache",
                                             SeverityLevel.Information,
                                             _openApiTraceProperties);

                return doc;
            }

            _telemetryClient?.TrackTrace($"Fetch the OpenApi document from the source: {graphUri}",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            OpenApiDocument source = await CreateOpenApiDocumentAsync(csdlHref);
            _OpenApiDocuments[csdlHref] = source;
            return source;
        }

        /// <summary>
        /// Update the OpenAPI document based on the style option
        /// </summary>
        /// <param name="style">The OpenApiStyle value.</param>
        /// <param name="subsetOpenApiDocument">The subset of an OpenAPI document.</param>
        /// <returns>An OpenAPI doc with the respective style applied.</returns>
        public static OpenApiDocument ApplyStyle(OpenApiStyle style, OpenApiDocument subsetOpenApiDocument)
        {
            _telemetryClient?.TrackTrace($"Applying style for '{style}'",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            if (style == OpenApiStyle.GEAutocomplete)
            {
                // Clone doc before making changes
                subsetOpenApiDocument = Clone(subsetOpenApiDocument);

                // The Content property and its schema $refs are unnecessary for autocomplete
                RemoveContent(subsetOpenApiDocument);
            }
            else if (style == OpenApiStyle.PowerShell || style == OpenApiStyle.PowerPlatform)
            {
                /* For Powershell and PowerPlatform Styles */

                // Clone doc before making changes
                subsetOpenApiDocument = Clone(subsetOpenApiDocument);

                // Remove AnyOf
                var anyOfRemover = new AnyOfRemover();
                var walker = new OpenApiWalker(anyOfRemover);
                walker.Walk(subsetOpenApiDocument);

                if (style == OpenApiStyle.PowerShell)
                {
                    // Format the OperationId for Powershell cmdlet names generation
                    var powershellFormatter = new PowershellFormatter();
                    walker = new OpenApiWalker(powershellFormatter);
                    walker.Walk(subsetOpenApiDocument);

                    var version = subsetOpenApiDocument.Info.Version;
                    if (!new Regex("v\\d\\.\\d").Match(version).Success)
                    {
                        subsetOpenApiDocument.Info.Version = "v1.0-" + version;
                    }

                    // Remove the root path to make AutoREST happy
                    subsetOpenApiDocument.Paths.Remove("/");

                    // Temp. fix - Escape the # character from description in
                    // 'microsoft.graph.networkInterface' schema
                    EscapePoundCharacter(subsetOpenApiDocument.Components);
                }
            }

            if (subsetOpenApiDocument.Paths == null ||
                !subsetOpenApiDocument.Paths.Any())
            {
                throw new ArgumentException("No paths found for the supplied parameters.");
            }

            _telemetryClient?.TrackTrace($"Finished applying style for '{style}'",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            return subsetOpenApiDocument;
        }

        private static OpenApiDocument Clone(OpenApiDocument subsetOpenApiDocument)
        {
            var stream = new MemoryStream();
            var writer = new OpenApiYamlWriter(new StreamWriter(stream));
            subsetOpenApiDocument.SerializeAsV3(writer);
            writer.Flush();
            stream.Position = 0;
            var reader = new OpenApiStreamReader();
            return reader.Read(stream, out _);
        }

        private static async Task<OpenApiDocument> CreateOpenApiDocumentAsync(Uri csdlHref)
        {
            var httpClient = CreateHttpClient();

            Stream csdl = await httpClient.GetStreamAsync(csdlHref.OriginalString);

            OpenApiDocument document = await ConvertCsdlToOpenApiAsync(csdl);

            return document;
        }

        /// <summary>
        /// Converts CSDL to OpenAPI
        /// </summary>
        /// <param name="csdl">The CSDL stream.</param>
        /// <returns>An OpenAPI document.</returns>
        public static async Task<OpenApiDocument> ConvertCsdlToOpenApiAsync(Stream csdl)
        {
            _telemetryClient?.TrackTrace("Converting CSDL stream to an OpenApi document",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            using var reader = new StreamReader(csdl);
            var csdlTxt = await reader.ReadToEndAsync();
            var edmModel = CsdlReader.Parse(XElement.Parse(csdlTxt).CreateReader());

            var settings = new OpenApiConvertSettings()
            {
                EnableKeyAsSegment = true,
                EnableOperationId = true,
                PrefixEntityTypeNameBeforeKey = true,
                TagDepth = 2,
                EnablePagination = true,
                EnableDiscriminatorValue = false,
                EnableDerivedTypesReferencesForRequestBody = false,
                EnableDerivedTypesReferencesForResponses = false,
                ShowRootPath = true,
                ShowLinks = true
            };
            OpenApiDocument document = edmModel.ConvertToOpenApi(settings);

            document = FixReferences(document);

            _telemetryClient?.TrackTrace("Finished converting CSDL stream to an OpenApi document",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            return document;
        }

        public static OpenApiDocument FixReferences(OpenApiDocument document)
        {
            // This method is only needed because the output of ConvertToOpenApi isn't quite a valid OpenApiDocument instance.
            // So we write it out, and read it back in again to fix it up.

            var sb = new StringBuilder();
            document.SerializeAsV3(new OpenApiYamlWriter(new StringWriter(sb)));
            var doc = new OpenApiStringReader().Read(sb.ToString(), out _);

            return doc;
        }

        private static IList<SearchResult> FindOperations(OpenApiDocument graphOpenApi, Func<OpenApiOperation, bool> predicate)
        {
            var search = new OperationSearch(predicate);
            var walker = new OpenApiWalker(search);
            walker.Walk(graphOpenApi);
            return search.SearchResults;
        }

        private static HttpClient CreateHttpClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var httpClient = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip
            });
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("graphslice", "1.0"));
            return httpClient;
        }

        private static void CopyReferences(OpenApiDocument target)
        {
            bool morestuff;
            do
            {
                var copy = new CopyReferences(target);
                var walker = new OpenApiWalker(copy);
                walker.Walk(target);

                morestuff = AddReferences(copy.Components, target.Components);

            } while (morestuff);
        }

        private static bool AddReferences(OpenApiComponents newComponents, OpenApiComponents target)
        {
            var moreStuff = false;
            foreach (var item in newComponents.Schemas)
            {
                if (!target.Schemas.ContainsKey(item.Key))
                {
                    moreStuff = true;
                    target.Schemas.Add(item);
                }
            }

            foreach (var item in newComponents.Parameters)
            {
                if (!target.Parameters.ContainsKey(item.Key))
                {
                    moreStuff = true;
                    target.Parameters.Add(item);
                }
            }

            foreach (var item in newComponents.Responses)
            {
                if (!target.Responses.ContainsKey(item.Key))
                {
                    moreStuff = true;
                    target.Responses.Add(item);
                }
            }
            return moreStuff;
        }

        private static void RemoveContent(OpenApiDocument target)
        {
            ContentRemover contentRemover = new ContentRemover();
            OpenApiWalker walker = new OpenApiWalker(contentRemover);
            walker.Walk(target);
        }

        /// <summary>
        /// Formats path functions, where present, by surrounding placeholder values of string data types
        /// with single quotation marks.
        /// </summary>
        /// <param name="pathKey">The path key in which the function placeholder(s) need to be formatted
        /// with single quotation marks.</param>
        /// <returns>The path key with its function placeholder(s) of string data types, where applicable,
        /// formatted with single quotation marks.</returns>
        private static string FormatPathFunctions(string pathKey, IList<OpenApiParameter> parameters)
        {
            var parameterTypes = new Dictionary<string, string>();
            foreach (var parameter in parameters)
            {
                /* The Type and Format properties describe the data type of the function parameters.
                 * For string data types the Format property is usually undefined.
                 */
                if (string.IsNullOrEmpty(parameter.Schema.Format) &&
                    !string.IsNullOrEmpty(parameter.Schema.Type))
                {
                    parameterTypes.Add(parameter.Name, parameter.Schema.Type);
                }
            }

            /* Example:
             * Actual ---->  /reports/microsoft.graph.getTeamsUserActivityCounts(period={period})
             * Expected -->  /reports/microsoft.graph.getTeamsUserActivityCounts(period='{period}')
             */
            string pattern = @"(=\{.*?\})";
            string evaluator(Match match)
            {
                string output = match.ToString(); // e.g. ---> ={period}
                string paramName = $"{output.Substring(2, output.Length - 3)}"; // e.g. ---> period

                if (parameterTypes.TryGetValue(paramName, out string type))
                {
                    if (type.Equals("string", StringComparison.OrdinalIgnoreCase))
                    {
                        // Only format function parameters with string data types
                        output = $"='{{{paramName}}}'";
                        return output;
                    }
                }
                return output;
            }
            return Regex.Replace(pathKey, pattern, evaluator);
        }

        /// <summary>
        /// Escapes the # character from the description of the 'microsoft.graph.networkInterface' schema.
        /// </summary>
        /// <remarks>
        /// This particular schema has a '#' character within the description of one of
        /// its schema definitions that breaks the PowerShell client code gen.
        /// Below is a temporary fix awaiting a permanent solution from AutoRest
        /// </remarks>
        /// <param name="components">The <see cref="OpenApiComponents"/> object with the target schema.</param>
        private static void EscapePoundCharacter(OpenApiComponents components)
        {
            if (components.Schemas.TryGetValue("microsoft.graph.networkInterface", out OpenApiSchema parentSchema))
            {
                if (parentSchema.Properties.TryGetValue("description", out OpenApiSchema descriptionSchema))
                {
                    // PowerShell uses ` to escape special characters
                    descriptionSchema.Description = descriptionSchema.Description.Replace("<#>", "<#/>");
                }
            }
        }
    }
}
