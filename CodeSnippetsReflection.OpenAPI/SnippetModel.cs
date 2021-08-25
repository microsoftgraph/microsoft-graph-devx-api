using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI
{
	public class SnippetModel : SnippetBaseModel<OpenApiUrlTreeNode>
	{
		public OpenApiUrlTreeNode CurrentNode { get; set; }
		public OpenApiUrlTreeNode RootNode { get; set; }
		public SnippetModel(HttpRequestMessage requestPayload, string serviceRootUrl, OpenApiUrlTreeNode treeNode) : base(requestPayload, serviceRootUrl)
		{
			if(treeNode == null) throw new ArgumentNullException(nameof(treeNode));
			RootNode = treeNode;

			var splatPath = requestPayload.RequestUri
										.AbsolutePath
										.TrimStart(pathSeparator)
										.Split(pathSeparator)
										.Skip(1); //skipping the version
			CurrentNode = GetTreeNode(treeNode, splatPath);
			InitializeModel(requestPayload);
		}
		private static char pathSeparator = '/';
		private static OpenApiUrlTreeNode GetTreeNode(OpenApiUrlTreeNode node, IEnumerable<string> pathSegments) {
			if(!pathSegments.Any())
				return node;
			var found = node.Children.TryGetValue(pathSegments.First(), out var childNode);
			if(found)
				return GetTreeNode(childNode, pathSegments.Skip(1));
			else
				throw new EntryPointNotFoundException($"Path segment '{pathSegments.First()}' not found in path");
		}
		protected override OpenApiUrlTreeNode GetLastPathSegment()
		{
			return CurrentNode;
		}
		protected override string GetResponseVariableName(OpenApiUrlTreeNode pathSegment)
		{
			var pathSegmentidentifier = pathSegment.Segment.TrimStart('{').TrimEnd('}');
			var identifier = pathSegmentidentifier.Contains(".")
                ? pathSegmentidentifier.Split(".").Last()
                : pathSegmentidentifier;
            return identifier.ToFirstCharacterLowerCase();
		}
		private static Regex searchValueRegex = new Regex(@"\$?search=""([^\""]*)""", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

		protected override string GetSearchExpression(string queryString)
		{
			var match = searchValueRegex.Match(queryString);
			if(match.Success)
				return match.Groups[1].Value;
			else
				return null;
		}
		private const string selectQsName = "select";
		private const string expandQsName = "expand";
		protected override void PopulateSelectAndExpandQueryFields(string queryString)
		{//note: this might fail on nested expands (e.g. ?expand=settings(select=name))
			if(string.IsNullOrEmpty(queryString)) return;
			
			var selectAndExpandQS = queryString.Trim('?')
						.Split('&')
						.Select(x => x.Split('='))
						.Select(x => new { Key = x[0], Value = x.Length > 1 ? x[1] : null })
						.Where(x => x.Key.Contains(selectQsName, StringComparison.OrdinalIgnoreCase) ||
								x.Key.Contains(expandQsName, StringComparison.OrdinalIgnoreCase))
						.ToList();
			var selectFields = selectAndExpandQS.FirstOrDefault(x => x.Key.Contains(selectQsName, StringComparison.OrdinalIgnoreCase));
			if(selectFields != null && !string.IsNullOrEmpty(selectFields.Value))
				SelectFieldList.AddRange(selectFields.Value.Split(','));
			var expandFields = selectAndExpandQS.FirstOrDefault(x => x.Key.Contains(expandQsName, StringComparison.OrdinalIgnoreCase));
			if(expandFields != null && !string.IsNullOrEmpty(expandFields.Value))
				ExpandFieldExpression = expandFields.Value;
		}
	}
}