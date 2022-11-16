using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators;

public partial class GraphCliGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
{
    private static readonly Regex camelCaseRegex = CamelCaseRegex();
    private static readonly Regex delimitedRegex = DelimitedRegex();

    public string GenerateCodeSnippet(SnippetModel snippetModel)
    {
        var pathNodes = snippetModel.PathNodes;
        // List has an initial capacity of 4. Reserve more based on the number of nodes.
        // Reduces reallocations at the expense of more memory used.
        var initialCapacity = pathNodes.Count < 20 ? 20 : pathNodes.Count;
        var command = new List<string>(initialCapacity);
        var parameters = new Dictionary<string, string>(capacity: initialCapacity);
        var lastNodeInPath = snippetModel.PathNodes.LastOrDefault(n => !n.Segment.IsCollectionIndex());
        var operationName = snippetModel.Method?.Method?.ToLower();
        foreach (var node in snippetModel.PathNodes)
        {
            var segment = node.Segment;
            if (segment.IsCollectionIndex())
            {
                var paramName = segment.Remove(0, 1);
                paramName = paramName.Remove(paramName.Length - 1, 1);
                var key = $"--{NormalizeToOption(paramName)}";
                if (parameters.ContainsKey(key))
                {
                    parameters[key] = segment;
                }
                else
                {
                    parameters.Add(key, segment);
                }
                command.Add("item");
            }
            else
            {
                command.Add(NormalizeToOption(segment).Replace("$count", "count"));
                if (node == lastNodeInPath) {
                    var isList = node == snippetModel.EndPathNode && segment != "$count";
                    var matchedOperation = GetOperationTypeFromHttpMethod(snippetModel.Method);
                    var method = node.PathItems.Select(it=> it.Value);
                    if (isList && matchedOperation == OperationType.Get) {
                        operationName = "list";
                    }
                }
            }
        }

        command.Add(operationName);

        command.AddRange(parameters.Select(p => $"{p.Key} {p.Value}"));
        return "mgc " + command.Aggregate("", (accum, val) => string.IsNullOrWhiteSpace(accum) ? val : $"{accum} {val}");
    }

    private static OperationType? GetOperationTypeFromHttpMethod(HttpMethod method) {
        if (method == HttpMethod.Delete) {
            return OperationType.Delete;
        } else if (method == HttpMethod.Get) {
            return OperationType.Get;
        } else if (method == HttpMethod.Head) {
            return OperationType.Head;
        } else if (method == HttpMethod.Options) {
            return OperationType.Options;
        } else if (method == HttpMethod.Patch) {
            return OperationType.Patch;
        } else if (method == HttpMethod.Post) {
            return OperationType.Post;
        } else if (method == HttpMethod.Put) {
            return OperationType.Put;
        } else if (method == HttpMethod.Trace) {
            return OperationType.Trace;
        }

        return null;
    }

    /// <summary>
    /// Converts camel-case or delimited string to '-' delimited string for use as a command option
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static string NormalizeToOption(string input)
    {
        var result = camelCaseRegex.Replace(input, "-$1");
        // 2 passes for cases like "singleValueLegacyExtendedProperty_id"
        result = delimitedRegex.Replace(result, "-$1");

        return result.ToLower();
    }

    [GeneratedRegex("(?<=[a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex CamelCaseRegex();

    [GeneratedRegex("(?<=[a-z])[-_\\.]+([A-Za-z])", RegexOptions.Compiled)]
    private static partial Regex DelimitedRegex();
}
