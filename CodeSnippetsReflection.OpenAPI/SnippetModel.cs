using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI
{
    public class SnippetModel : SnippetBaseModel<OpenApiUrlTreeNode>
    {
        /// <summary>
        /// An OpenAPI node that represents the last segment in the request URL.
        /// </summary>
        /// <remarks>
        /// For example, if the request URL is /users/100 and it matches the
        /// template <c>/users/{user-id}</c>, the <c>EndPathNode</c> will be a
        /// node representing the <c>{user-id}</c> segment.
        /// </remarks>
        public OpenApiUrlTreeNode EndPathNode => PathNodes.LastOrDefault();

        /// <summary>
        /// An OpenAPI node that represents the root segment in the request URL.
        /// </summary>
        /// <remarks>
        /// For example, if the request URL is /users/100 and it matches the
        /// template <c>/users/{user-id}</c>, the <c>RootPathNode</c> will be a
        /// node representing the <c>users</c> segment.
        /// </remarks>
        public OpenApiUrlTreeNode RootPathNode => PathNodes.FirstOrDefault();
        public List<OpenApiUrlTreeNode> PathNodes { get; private set; } = new List<OpenApiUrlTreeNode>();
        public IDictionary<string, OpenApiSchema> Schemas{ get; private set; }

        public SnippetModel(HttpRequestMessage requestPayload, string serviceRootUrl, OpenApiSnippetMetadata openApiSnippetMetadata) : base(requestPayload, serviceRootUrl)
        {
            ArgumentNullException.ThrowIfNull(openApiSnippetMetadata);

            var remappedPayload = RemapKnownPathsIfNeeded(requestPayload);
            var splatPath = ReplaceIndexParametersByPathSegment(remappedPayload.RequestUri
                                        .AbsolutePath
                                        .TrimStart(pathSeparator))
                                        .Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries)
                                        .Skip(1); //skipping the version
            LoadPathNodes(openApiSnippetMetadata.OpenApiUrlTreeNode, splatPath, requestPayload.Method);
            Schemas = openApiSnippetMetadata.Schemas ?? new Dictionary<string, OpenApiSchema>();
        }

        private static readonly Dictionary<Regex, string> KnownReMappings = new()
        {
            { new Regex(@"/me/drive/root/",RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)), "/drives/driveId/items/root/" },
            { new Regex(@"/me/drive/",RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)), "/drives/driveId/" },
            { new Regex(@"/groups/[A-z0-9{}\-]+/drive/root",RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)), "/drives/driveId/items/root/" },
            { new Regex(@"/groups/[A-z0-9{}\-]+/drive/",RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)), "/drives/driveId/" },
            { new Regex(@"/sites/[A-z0-9{}\-]+/drive/root",RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)), "/drives/driveId/items/root/" },
            { new Regex(@"/sites/[A-z0-9{}\-]+/drive/",RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)), "/drives/driveId/" },
            { new Regex(@"/users/[A-z0-9{}\-]+/drive/root",RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)), "/drives/driveId/items/root/" },
            { new Regex(@"/users/[A-z0-9{}\-]+/drive/",RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)), "/drives/driveId/" },
            { new Regex(@"/drive/",RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)), "/drives/driveId/" },
        };

        private static HttpRequestMessage RemapKnownPathsIfNeeded(HttpRequestMessage originalRequest)
        {
            var originalUri = originalRequest.RequestUri.OriginalString;
            var regexMatch = KnownReMappings.Keys.FirstOrDefault(regex => regex.Match(originalUri).Success);
            if (regexMatch == null)
            {
                return originalRequest;
            }

            originalUri = regexMatch.Replace(originalUri, KnownReMappings[regexMatch]);
            originalRequest.RequestUri = new Uri(originalUri);
            return originalRequest;
        }

        private static Regex oDataIndexReplacementRegex = new(@"\('([\w=]+)'\)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        /// <summary>
        /// Replaces OData style ids to path segments
        /// events('AAMkAGI1AAAt9AHjAAA=') to events/AAMkAGI1AAAt9AHjAAA=
        /// </summary>
        private static string ReplaceIndexParametersByPathSegment(string original) {
            if (string.IsNullOrEmpty(original)) return original;
            return oDataIndexReplacementRegex.Replace(original, match => $"/{match.Groups[1].Value}");
        }
        private const string defaultContentType = "application/json";
        private OpenApiSchema _responseSchema;
        public OpenApiSchema ResponseSchema
        {
            get
            {
                if (_responseSchema == null)
                {
                    var contentType = ContentType ?? defaultContentType;
                    var operationType = GetOperationType(Method);
                    //GET requests don't have a request body, so ignore the content type if set
                    if (operationType == OperationType.Get && contentType != defaultContentType)
                    {
                        contentType = defaultContentType;
                    }
                    _responseSchema = GetOperation(operationType)
                                        ?.Responses
                                        ?.Where(x => !x.Key.Equals("204", StringComparison.OrdinalIgnoreCase) && //204 doesn't have content
                                                    !x.Key.Equals("default", StringComparison.OrdinalIgnoreCase))// default is the error response
                                        ?.Select(x => x.Value.Content.TryGetValue(contentType, out var mediaType) ? mediaType.Schema : null)
                                        ?.FirstOrDefault(x => x != null);
                }
                return _responseSchema;
            }
        }
        private OpenApiOperation GetOperation(OperationType type) {
            EndPathNode.PathItems[OpenApiSnippetsGenerator.treeNodeLabel].Operations.TryGetValue(type, out var operation);
            return operation;
        }
        private OpenApiSchema _requestSchema;
        public OpenApiSchema RequestSchema
        {
            get
            {
                if (_requestSchema == null)
                {
                    var contentType = ContentType ?? defaultContentType;
                    var operationType = GetOperationType(Method);
                    var operation = GetOperation(operationType);
                    if(operation != default)
                        _requestSchema = operation
                                            .RequestBody?
                                            .Content?
                                            .TryGetValue(contentType, out var mediaType) ?? false ? mediaType.Schema : null;
                }
                return _requestSchema;
            }
        }
        public bool IsRequestBodyValid { get {
            return !string.IsNullOrEmpty(RequestBody) && !RandomGEEmptyValues.Contains(RequestBody);
        }}
        private static HashSet<string> RandomGEEmptyValues = new(StringComparer.OrdinalIgnoreCase)  {
            "undefined",
            "\"\"",
            "{}"
        }; // graph explorer sends these random values as request body for some reason
        private static OperationType GetOperationType(HttpMethod method)
        {
            if (method == HttpMethod.Get) return OperationType.Get;
            else if (method == HttpMethod.Post) return OperationType.Post;
            else if (method == HttpMethod.Put) return OperationType.Put;
            else if (method == HttpMethod.Delete) return OperationType.Delete;
            else if (method == HttpMethod.Patch) return OperationType.Patch;
            else if (method == HttpMethod.Head) return OperationType.Head;
            else if (method == HttpMethod.Options) return OperationType.Options;
            else if (method == HttpMethod.Trace) return OperationType.Trace;
            else throw new ArgumentOutOfRangeException(nameof(method));
        }
        private static Regex namespaceRegex = new Regex("Microsoft.Graph.(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        public static string TrimNamespace(string path)
        {
            Match namespaceMatch = namespaceRegex.Match(path);
            if (namespaceMatch.Success)
            {
                string fqnAction = namespaceMatch.Groups[0].Value;
                // Trim nested namespace segments.
                string[] nestedActionNamespaceSegments = namespaceMatch.Groups[1].Value.Split(".");
                // Remove trailing '()' from functions.
                string actionName = nestedActionNamespaceSegments[nestedActionNamespaceSegments.Length - 1].Replace("()", "");
                path = Regex.Replace(path, Regex.Escape(fqnAction), actionName, RegexOptions.None, TimeSpan.FromSeconds(5));
            }
            return path;
        }
        private static readonly char pathSeparator = '/';
        private void LoadPathNodes(OpenApiUrlTreeNode node, IEnumerable<string> pathSegments, HttpMethod httpMethod)
        {
            if (!pathSegments.Any())// we've found a mathing url path.
            {
                var operationType = GetOperationType(httpMethod);
                if (node.PathItems[OpenApiSnippetsGenerator.treeNodeLabel].Operations.ContainsKey(operationType))
                    return;// Verify if the method exists before returning.

                throw new EntryPointNotFoundException($"HTTP Method '{httpMethod}' not found for path.");//path exists but Method does not
            }


            var pathSegment = HttpUtility.UrlDecode(pathSegments.First());
            var childNode = node.Children.FirstOrDefault(x => TrimNamespace(x.Key).Equals(pathSegment)).Value;
            if (childNode != null)
            {
                LoadNextNode(childNode, pathSegments, httpMethod);
                return;
            }
            if (node.Children.Keys.Any(x => x.RemoveFunctionBraces().Equals(pathSegment, StringComparison.OrdinalIgnoreCase)))
            { // the casing in the description might be different than the casing in the snippet and this dictionary is CS
                var caseChildNode = node.Children.First(x => x.Key.RemoveFunctionBraces().Equals(pathSegment, StringComparison.OrdinalIgnoreCase)).Value;
                LoadNextNode(caseChildNode, pathSegments, httpMethod);
                return;
            }
            if (node.Children.Keys.Any(x => x.IsFunction()) || pathSegment.IsFunction())
            {
                var actionChildNode = node.Children.FirstOrDefault(x => x.Key.Split('.').Last().Equals(pathSegment.Split('.').Last(), StringComparison.OrdinalIgnoreCase));
                if(actionChildNode.Value != null)
                {
                    LoadNextNode(actionChildNode.Value, pathSegments, httpMethod);
                    return;
                }
            }
            if (node.Children.Keys.Any(static x => x.IsFunctionWithParameters()) && pathSegment.IsFunctionWithParameters())
            {
                var functionWithParametersNode = node.Children.FirstOrDefault(function => function.Key.Split('.')[^1].IsFunctionWithParametersMatch(pathSegment.Split('.')[^1]));
                if (functionWithParametersNode.Value != null)
                {
                    LoadNextNode(functionWithParametersNode.Value, pathSegments, httpMethod);
                    return;
                }
            }
            // always match indexer last as functions on collections may be interpreted as indexers if processed after.
            var collectionIndices = node.Children.Keys.Where(static x => x.IsCollectionIndex()).ToArray();
            if (collectionIndices.Length != 0)
            {
                var collectionIndexValue = node.Children[collectionIndices[0]];//lookup the node using the key
                if (collectionIndexValue != null)
                {
                    LoadNextNode(collectionIndexValue, pathSegments, httpMethod);
                    return;
                }
            }

            throw new EntryPointNotFoundException($"Path segment '{pathSegment}' not found in path");
        }
        private void LoadNextNode(OpenApiUrlTreeNode node, IEnumerable<string> pathSegments, HttpMethod httpMethod)
        {
            PathNodes.Add(node);
            LoadPathNodes(node, pathSegments.Skip(1),httpMethod);
        }
        protected override OpenApiUrlTreeNode GetLastPathSegment()
        {
            return EndPathNode;
        }
        protected override string GetResponseVariableName(OpenApiUrlTreeNode pathSegment)
        {
            var pathSegmentidentifier = pathSegment.Segment.TrimStart('{').TrimEnd('}');
            var identifier = pathSegmentidentifier.Contains(".")
                ? pathSegmentidentifier.Split(".").Last()
                : pathSegmentidentifier;
            return identifier.ToFirstCharacterLowerCase();
        }
        private static readonly Regex searchValueRegex = new(@"\$?search=""([^\""]*)""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(5));

        protected override string GetSearchExpression(string queryString)
        {
            var match = searchValueRegex.Match(queryString);
            if (match.Success)
                return match.Groups[1].Value;
            else
                return null;
        }
        private const string selectQsName = "select";
        private const string expandQsName = "expand";
        protected override void PopulateSelectAndExpandQueryFields(string queryString)
        {//note: this might fail on nested expands (e.g. ?expand=settings(select=name))
            if (string.IsNullOrEmpty(queryString)) return;

            var selectAndExpandQS = queryString.Trim('?')
                        .Split('&')
                        .Select(x => x.Split('='))
                        .Select(x => new { Key = x[0], Value = x.Length > 1 ? x[1] : null })
                        .Where(x => x.Key.Contains(selectQsName, StringComparison.OrdinalIgnoreCase) ||
                                x.Key.Contains(expandQsName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
            var selectFields = selectAndExpandQS.FirstOrDefault(x => x.Key.Contains(selectQsName, StringComparison.OrdinalIgnoreCase));
            if (selectFields != null && !string.IsNullOrEmpty(selectFields.Value))
                SelectFieldList.AddRange(selectFields.Value.Split(','));
            var expandFields = selectAndExpandQS.FirstOrDefault(x => x.Key.Contains(expandQsName, StringComparison.OrdinalIgnoreCase));
            if (expandFields != null && !string.IsNullOrEmpty(expandFields.Value))
                ExpandFieldExpression = expandFields.Value;
        }
    }
}
