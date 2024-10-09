// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;
using Microsoft.OpenApi.Writers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using OpenAPIService.Common;
using UtilityService;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using OpenAPIService.Interfaces;
using System.Text.Json;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.OpenApi.Any;
using System.Threading;
using Microsoft.Extensions.Configuration;
using FileService.Common;
using Microsoft.IO;

namespace OpenAPIService
{
    public enum OpenApiStyle
    {
        PowerShell,
        PowerPlatform,
        Plain,
        GEAutocomplete
    }

    public class OpenApiService : IOpenApiService
    {
        private static readonly ConcurrentDictionary<string, OpenApiDocument> _OpenApiDocuments = new();
        private static readonly ConcurrentDictionary<string, DateTime> _OpenApiDocumentsDateCreated = new();
        private static readonly ConcurrentDictionary<string, string> _openApiTraceProperties =
                        new();
        private readonly TelemetryClient _telemetryClient;
        private static readonly SemaphoreSlim _openApiDocumentAccess = new(2, 2); // only 2 threads can be granted access at a time.
        private const string CacheRefreshTimeConfig = "FileCacheRefreshTimeInMinutes:OpenAPIDocuments";
        private readonly int _defaultForceRefreshTime; // time span for allowable forceRefresh of the OpenAPI document
        private readonly Queue<string> _graphUriQueue = new();
        private static readonly RecyclableMemoryStreamManager _streamManager = new();
        private static readonly Dictionary<OpenApiStyle, string> _fileNames = new() {
            { OpenApiStyle.GEAutocomplete, "graphexplorer"},
            { OpenApiStyle.Plain, "default"},
            { OpenApiStyle.PowerShell, "powershell"},
            { OpenApiStyle.PowerPlatform, "default"},
         };

        public OpenApiService(IConfiguration configuration, TelemetryClient telemetryClient = null)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration),
                $"Value cannot be null: {nameof(configuration)}");
            _defaultForceRefreshTime = FileServiceHelper.GetFileCacheRefreshTime(configuration[CacheRefreshTimeConfig]);
            _telemetryClient = telemetryClient;
            _openApiTraceProperties.TryAdd(UtilityConstants.TelemetryPropertyKey_OpenApi, nameof(OpenApiService));
        }

        /// <summary>
        /// Create partial OpenAPI document based on the provided predicate.
        /// </summary>
        /// <param name="source">The target <see cref="OpenApiDocument"/>.</param>
        /// <param name="title">The OpenAPI document title.</param>
        /// <param name="graphVersion">Version of the target Microsoft Graph API.</param>
        /// <param name="predicate">A predicate function.</param>
        /// <returns>A partial OpenAPI document.</returns>
        public OpenApiDocument CreateFilteredDocument(OpenApiDocument source, string title, string graphVersion, Func<OpenApiOperation, bool> predicate)
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

            subset.SecurityRequirements.Add(new OpenApiSecurityRequirement() { { aadv2Scheme, Array.Empty<string>() } });

            subset.Servers.Add(new OpenApiServer() { Description = "Core", Url = string.Format(Constants.GraphConstants.GraphUrl, graphVersion) });

            var results = FindOperations(source, predicate);
            foreach (var result in results)
            {
                OpenApiPathItem pathItem;
                var pathKey = result.CurrentKeys.Path;
                if (result.Operation.Extensions.TryGetValue("x-ms-docs-operation-type", out var value) && value is OpenApiString oasString && oasString.Value.Equals("function", StringComparison.Ordinal))
                {
                    pathKey = FormatPathFunctions(pathKey, result.Operation.Parameters);
                }

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

            return CloneOpenApiDocument(subset);
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
        public Func<OpenApiOperation, bool> CreatePredicate(string operationIds, string tags, string url,
            OpenApiDocument source, string graphVersion = Constants.OpenApiConstants.GraphVersion_V1, bool forceRefresh = false)
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
                    // Specify timeout to prevent DOS. See https://docs.microsoft.com/en-us/dotnet/standard/base-types/best-practices.
                    var regex = new Regex(tagsArray[0], RegexOptions.None, TimeSpan.FromSeconds(1));

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
                var sources = new ConcurrentDictionary<string, OpenApiDocument>();
                sources.TryAdd(graphVersion, source);
                var rootNode = CreateOpenApiUrlTreeNode(sources);

                url = url.BaseUriPath()
                         .UriTemplatePathFormat();

                OpenApiOperation[] openApiOps = GetOpenApiOperations(rootNode, url, graphVersion);

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
        /// Creates an <see cref="OpenApiUrlTreeNode"/> from a collection of <see cref="OpenApiDocument"/>.
        /// </summary>
        /// <param name="sources">Dictionary of labels and their corresponding <see cref="OpenApiDocument"/> objects.</param>
        /// <returns>The created <see cref="OpenApiUrlTreeNode"/>.</returns>
        public OpenApiUrlTreeNode CreateOpenApiUrlTreeNode(ConcurrentDictionary<string, OpenApiDocument> sources)
        {
            UtilityFunctions.CheckArgumentNull(sources, nameof(sources));

            _telemetryClient?.TrackTrace("Creating OpenApiUrlTreeNode",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            var rootNode = OpenApiUrlTreeNode.Create();
            foreach (var source in sources)
            {
                rootNode.Attach(source.Value, source.Key);
            }

            _openApiTraceProperties.TryAdd(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, nameof(OpenApiService));
            _telemetryClient?.TrackTrace($"Finished creating OpenApiUrlTreeNode",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);
            _openApiTraceProperties.TryRemove(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, out string _);

            return rootNode;
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
        private OpenApiOperation[] GetOpenApiOperations(OpenApiUrlTreeNode rootNode, string relativeUrl, string label)
        {
            UtilityFunctions.CheckArgumentNull(rootNode, nameof(rootNode));
            UtilityFunctions.CheckArgumentNullOrEmpty(relativeUrl, nameof(relativeUrl));
            UtilityFunctions.CheckArgumentNullOrEmpty(label, nameof(label));

            _telemetryClient?.TrackTrace($"Fetching OpenApiOperations for url path '{relativeUrl}' from the OpenApiUrlTreeNode",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            if (relativeUrl.Equals("/", StringComparison.Ordinal) && rootNode.HasOperations(label))
            {
                return rootNode.PathItems[label].Operations.Values.ToArray();
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
                var tempTargetChild = targetChild?.Children?
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
                    tempTargetChild = targetChild?.Children?
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
        /// Converts a <see cref="OpenApiUrlTreeNode"/> object to JSON text.
        /// </summary>
        /// <param name="rootNode">The target <see cref="OpenApiUrlTreeNode"/> root node.</param>
        /// <param name="stream">The destination for writing the JSON text to.</param>
        public async Task ConvertOpenApiUrlTreeNodeToJsonAsync(OpenApiUrlTreeNode rootNode, Stream stream)
        {
            using Utf8JsonWriter writer = new Utf8JsonWriter(stream);
            ConvertOpenApiUrlTreeNodeToJson(writer, rootNode);
            await writer.FlushAsync();
        }

        /// <summary>
        /// Converts a <see cref="OpenApiUrlTreeNode"/> object to JSON text.
        /// </summary>
        /// <param name="writer">An instance of the <see cref="Utf8JsonWriter"/> class that
        /// uses a specified stream to write the JSON output to.</param>
        /// <param name="rootNode">The target <see cref="OpenApiUrlTreeNode"/> object.</param>
        public static void ConvertOpenApiUrlTreeNodeToJson(Utf8JsonWriter writer, OpenApiUrlTreeNode rootNode)
        {
            writer.WriteStartObject();
            writer.WriteString("segment", rootNode.Segment);
            writer.WriteStartArray("labels");

            foreach (var pathItem in rootNode.PathItems)
            {
                writer.WriteStartObject();
                writer.WriteString("name", pathItem.Key);
                writer.WriteStartArray("methods");
                var methods = pathItem.Value.Operations.Select(x => x.Key.ToString()).ToList();

                foreach (var method in methods)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", method);
                    var url = pathItem.Value.Operations.FirstOrDefault(x => x.Key.ToString().Equals(method, StringComparison.OrdinalIgnoreCase) &&
                        x.Value.ExternalDocs != null).Value?.ExternalDocs?.Url?.OriginalString;
                    writer.WriteString("documentationUrl", url);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            if (rootNode.Children.Count > 0)
            {
                writer.WriteStartArray("children");
                foreach (var childNode in rootNode.Children.ToImmutableSortedDictionary().Values)
                {
                    ConvertOpenApiUrlTreeNodeToJson(writer, childNode);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// Create a representation of the OpenApiDocument to return from an API
        /// </summary>
        /// <param name="subset">OpenAPI document.</param>
        /// <param name="styleOptions">The modal object containing the required styling options.</param>
        /// <returns>A memory stream.</returns>
        public MemoryStream SerializeOpenApiDocument(OpenApiDocument subset, OpenApiStyleOptions styleOptions)
        {
            _telemetryClient?.TrackTrace($"Serializing the subset OpenApiDocument document for '{styleOptions.OpenApiFormat}' format",
                                            SeverityLevel.Information,
                                            _openApiTraceProperties);

            var stream = _streamManager.GetStream($"{nameof(OpenApiService)}.{nameof(SerializeOpenApiDocument)}");
            var sr = new StreamWriter(stream);
            OpenApiWriterBase writer;

            if (styleOptions.OpenApiFormat == Constants.OpenApiConstants.Format_Yaml)
            {
                if (styleOptions.InlineLocalReferences)
                {
                    writer = new OpenApiYamlWriter(sr,
                        new OpenApiWriterSettings { InlineLocalReferences = true });
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
                        new OpenApiWriterSettings { InlineLocalReferences = true });
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
        /// Gets an OpenAPI document version of Microsoft Graph based on OpenApiStyles
        /// from a dictionary cache or gets a new instance.
        /// </summary>
        /// <param name="graphUri">The uri of the Microsoft Graph metadata doc.</param>
        /// <param name="forceRefresh">Whether to reload the OpenAPI document from source.</param>
        /// <param name="fileName">Optional: Overrides the OpenAPI file name for the specified <paramref name="openApiStyle"/>.</param>
        /// <returns>A task of the value of an OpenAPI document.</returns>
        public async Task<OpenApiDocument> GetGraphOpenApiDocumentAsync(string graphUri, OpenApiStyle openApiStyle, bool forceRefresh, string fileName = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = _fileNames[openApiStyle];
            }
            else
            {
                if (!UtilityFunctions.IsUrlSafe(fileName))
                {
                    throw new ArgumentException("The value is not URL-safe", nameof(fileName));
                }
            }

            var cachedDoc = $"{graphUri}/{openApiStyle}/{fileName}";
            if (!forceRefresh && _OpenApiDocuments.TryGetValue(cachedDoc, out OpenApiDocument doc))
            {
                _telemetryClient?.TrackTrace("Fetch the OpenApi document from the cache",
                                             SeverityLevel.Information,
                                             _openApiTraceProperties);

                return doc;
            }

            /* OpenAPI document not cached;
             * Ensure only one thread is running the fetch process.
             */
            await _openApiDocumentAccess.WaitAsync();
            try
            {
                _telemetryClient?.TrackTrace("Fetch lock successfully acquired.",
                                             SeverityLevel.Information,
                                             _openApiTraceProperties);

                // Check whether previous thread already cached the OpenAPI document.                
                if (!forceRefresh && _OpenApiDocuments.TryGetValue(cachedDoc, out doc))
                {
                    _telemetryClient?.TrackTrace("OpenAPI document cached by another thread. " +
                                                 "Retrieving cached document.",
                                                 SeverityLevel.Information,
                                                 _openApiTraceProperties);

                    return doc;
                }

                // Check whether forceRefresh is requested before the allowable refresh period has elapsed.
                if (_OpenApiDocumentsDateCreated.TryGetValue(cachedDoc, out DateTime dateCreated))
                {
                    var minutesElapsed = (DateTime.UtcNow - dateCreated).Minutes;
                    if (minutesElapsed < _defaultForceRefreshTime)
                    {
                        // Return the cached document.
                        _openApiTraceProperties.TryAdd(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, nameof(OpenApiService));
                        _telemetryClient?.TrackTrace($"forceRefresh requested within {minutesElapsed} minutes. " +
                                                     $"Retrieving cached OpenAPI document.",
                                                     SeverityLevel.Information,
                                                     _openApiTraceProperties);
                        _openApiTraceProperties.TryRemove(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, out string _);
                        _OpenApiDocuments.TryGetValue(cachedDoc, out doc);
                        return doc;
                    }
                }

                if (_graphUriQueue.Contains(graphUri))
                {
                    // Only 1 thread per Graph version allowed to fetch an OpenAPI document.
                    _telemetryClient?.TrackTrace("OpenAPI document fetch attempted by a new thread while a separate thread is still " +
                                                 "performing a fetch for a similar Graph version. New thread requeued.",
                                                 SeverityLevel.Information,
                                                 _openApiTraceProperties);
                    return await GetGraphOpenApiDocumentAsync(graphUri, openApiStyle, forceRefresh, fileName);
                }

                // Refresh the OpenAPI document.
                _openApiTraceProperties.TryAdd(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, nameof(OpenApiService));
                _telemetryClient?.TrackTrace($"Fetch the OpenApi document from the source: {graphUri}",
                                             SeverityLevel.Information,
                                             _openApiTraceProperties);
                _openApiTraceProperties.TryRemove(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, out string _);

                _graphUriQueue.Enqueue(graphUri);

                graphUri += $"/{fileName}.yaml";

                OpenApiDocument source = await GetOpenApiDocumentAsync(new Uri(graphUri));
                _OpenApiDocuments[cachedDoc] = source;
                _OpenApiDocumentsDateCreated[cachedDoc] = DateTime.UtcNow;
                return source;
            }
            finally
            {
                if (_graphUriQueue.Count != 0)
                {
                    _graphUriQueue.Dequeue();
                }

                _openApiDocumentAccess.Release();
                _telemetryClient?.TrackTrace("Fetch lock successfully released.",
                                             SeverityLevel.Information,
                                             _openApiTraceProperties);
            }
        }

        /// <summary>
        /// Update the OpenAPI document based on the style option.
        /// </summary>
        /// <param name="style">The OpenApiStyle value.</param>
        /// <param name="subsetOpenApiDocument">The subset of an OpenAPI document.</param>
        /// <returns>An OpenAPI doc with the respective style applied.</returns>
        public OpenApiDocument ApplyStyle(
            OpenApiStyle style,
            OpenApiDocument subsetOpenApiDocument,
            bool includeRequestBody = false,
            bool singularizeOperationIds = false)
        {
            _telemetryClient?.TrackTrace($"Applying style for '{style}'",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            var openApiDoc = CloneOpenApiDocument(subsetOpenApiDocument);

            if (style == OpenApiStyle.GEAutocomplete && !includeRequestBody)
            {
                // The Content property and its schema $refs are unnecessary for autocomplete
                RemoveContent(openApiDoc);
            }
            if (style == OpenApiStyle.PowerShell || style == OpenApiStyle.PowerPlatform)
            {
                // Remove AnyOf and OneOf since AutoREST does not support them. See https://github.com/Azure/autorest/issues/4118. 
                var anyOfRemover = new AnyOfOneOfRemover();
                var walker = new OpenApiWalker(anyOfRemover);
                walker.Walk(openApiDoc);

                if (style == OpenApiStyle.PowerShell)
                {
                    // Format the OperationId for Powershell cmdlet names generation
                    var powershellFormatter = new PowershellFormatter(singularizeOperationIds);
                    walker = new OpenApiWalker(powershellFormatter);
                    walker.Walk(openApiDoc);

                    var version = openApiDoc.Info.Version;
                    if (!new Regex("v\\d\\.\\d", RegexOptions.None, TimeSpan.FromSeconds(5)).Match(version).Success)
                    {
                        openApiDoc.Info.Version = "v1.0-" + version;
                    }

                    // Remove the root path to make AutoREST happy
                    openApiDoc.Paths.Remove("/");

                    // Temp. fix - Escape the # character from description in
                    // 'microsoft.graph.networkInterface' schema
                    EscapePoundCharacter(openApiDoc.Components);
                }
            }

            if (openApiDoc.Paths == null ||
                !openApiDoc.Paths.Any())
            {
                throw new ArgumentException("No paths found for the supplied parameters.");
            }

            _telemetryClient?.TrackTrace($"Finished applying style for '{style}'",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);

            return openApiDoc;
        }

        private async Task<OpenApiDocument> GetOpenApiDocumentAsync(Uri openAPIHref)
        {
            var stopwatch = new Stopwatch();
            var httpClient = CreateHttpClient();
            stopwatch.Start();
            await using Stream stream = await httpClient.GetStreamAsync(openAPIHref.OriginalString);
            stopwatch.Stop();

            _openApiTraceProperties.TryAdd(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, nameof(OpenApiService));
            _telemetryClient?.TrackTrace($"Success getting OpenAPI document for {openAPIHref} in {stopwatch.ElapsedMilliseconds}ms",
                                         SeverityLevel.Information,
                                         _openApiTraceProperties);
            _openApiTraceProperties.TryRemove(UtilityConstants.TelemetryPropertyKey_SanitizeIgnore, out string _);

            var streamValue = await new OpenApiStreamReader().ReadAsync(stream);
            return streamValue.OpenApiDocument;
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
            moreStuff |= AddComponentsSchemas(newComponents, target);
            moreStuff |= AddComponentsParameters(newComponents, target);
            moreStuff |= AddComponentsResponses(newComponents, target);
            moreStuff |= AddComponentsRequestBodies(newComponents, target);
            return moreStuff;
        }

        private static bool AddComponentsSchemas(OpenApiComponents newComponents, OpenApiComponents target)
        {
            bool moreStuff = false;
            foreach (var item in newComponents.Schemas)
            {
                if (!target.Schemas.ContainsKey(item.Key))
                {
                    moreStuff = true;
                    target.Schemas.Add(item);
                }
            }
            return moreStuff;
        }

        private static bool AddComponentsParameters(OpenApiComponents newComponents, OpenApiComponents target)
        {
            bool moreStuff = false;
            foreach (var item in newComponents.Parameters)
            {
                if (!target.Parameters.ContainsKey(item.Key))
                {
                    moreStuff = true;
                    target.Parameters.Add(item);
                }
            }
            return moreStuff;
        }

        private static bool AddComponentsResponses(OpenApiComponents newComponents, OpenApiComponents target)
        {
            bool moreStuff = false;
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

        private static bool AddComponentsRequestBodies(OpenApiComponents newComponents, OpenApiComponents target)
        {
            bool moreStuff = false;
            foreach (var item in newComponents.RequestBodies)
            {
                if (!target.RequestBodies.ContainsKey(item.Key))
                {
                    moreStuff = true;
                    target.RequestBodies.Add(item);
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
        /// Creates a clone of an OpenAPI document
        /// </summary>
        /// <param name="document">The source document to clone.</param>
        /// <returns>A clone of the source document.</returns>
        public OpenApiDocument CloneOpenApiDocument(OpenApiDocument document)
        {
            using var stream = new MemoryStream();
            var writer = new OpenApiYamlWriter(new StreamWriter(stream));
            document.SerializeAsV3(writer);
            writer.Flush();
            stream.Position = 0;
            var reader = new OpenApiStreamReader();
            return reader.Read(stream, out _);
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
                if (string.IsNullOrEmpty(parameter.Schema?.Format) &&
                    !string.IsNullOrEmpty(parameter.Schema?.Type))
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
                string paramName = $"{output[2..^1]}"; // e.g. ---> period

                if (parameterTypes.TryGetValue(paramName, out string type) && type.Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    // Only format function parameters with string data types
                    output = $"='{{{paramName}}}'";
                    return output;
                }
                return output;
            }
            return Regex.Replace(pathKey, pattern, evaluator, RegexOptions.None, TimeSpan.FromSeconds(5));
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
            if (components.Schemas.TryGetValue("microsoft.graph.networkInterface", out OpenApiSchema parentSchema)
                && parentSchema.Properties.TryGetValue("description", out OpenApiSchema descriptionSchema))
            {
                // PowerShell uses ` to escape special characters
                descriptionSchema.Description = descriptionSchema.Description?.Replace("<#>", "<#/>");
            }
        }
    }
}
