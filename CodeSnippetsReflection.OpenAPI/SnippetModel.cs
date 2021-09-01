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
        public OpenApiUrlTreeNode EndPathNode => PathNodes.LastOrDefault();
        public OpenApiUrlTreeNode RootPathNode => PathNodes.FirstOrDefault();
        public List<OpenApiUrlTreeNode> PathNodes { get; private set; } = new List<OpenApiUrlTreeNode>();
		public SnippetModel(HttpRequestMessage requestPayload, string serviceRootUrl, OpenApiUrlTreeNode treeNode) : base(requestPayload, serviceRootUrl)
		{
			if (treeNode == null) throw new ArgumentNullException(nameof(treeNode));

			var splatPath = requestPayload.RequestUri
										.AbsolutePath
										.TrimStart(pathSeparator)
										.Split(pathSeparator)
										.Skip(1); //skipping the version
			LoadPathNodes(treeNode, splatPath);
			InitializeModel(requestPayload);
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
											.RequestBody
											.Content
											.TryGetValue(contentType, out var mediaType) ? mediaType.Schema : null;
				}
				return _requestSchema;
			}
		}
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
		private static readonly char pathSeparator = '/';
		private void LoadPathNodes(OpenApiUrlTreeNode node, IEnumerable<string> pathSegments)
		{
			if (!pathSegments.Any())
				return;
			var pathSegment = HttpUtility.UrlDecode(pathSegments.First());
			if (node.Children.TryGetValue(pathSegment, out var childNode))
			{
				LoadNextNode(childNode, pathSegments);
				return;
			}
            if (node.Children.Keys.Any(x => x.IsFunction()))
			{
				var actionChildNode = node.Children.FirstOrDefault(x => x.Key.Split('.').Last().Equals(pathSegment, StringComparison.OrdinalIgnoreCase));
				if(actionChildNode.Value != null)
				{
					LoadNextNode(actionChildNode.Value, pathSegments);
					return;
				}
			}
			if (node.Children.Any(x => x.Key.IsCollectionIndex()))
			{
				var collectionIndexNode = node.Children.FirstOrDefault(x => x.Key.IsCollectionIndex());
				if (collectionIndexNode.Value != null)
				{
					LoadNextNode(collectionIndexNode.Value, pathSegments);
					return;
				}
			}
			throw new EntryPointNotFoundException($"Path segment '{pathSegment}' not found in path");
		}
		private void LoadNextNode(OpenApiUrlTreeNode node, IEnumerable<string> pathSegments)
		{
			PathNodes.Add(node);
			LoadPathNodes(node, pathSegments.Skip(1));
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
		private static readonly Regex searchValueRegex = new(@"\$?search=""([^\""]*)""", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

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