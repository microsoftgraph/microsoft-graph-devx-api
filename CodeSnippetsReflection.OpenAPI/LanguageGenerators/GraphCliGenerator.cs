using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        if (snippetModel == null)
        {
            return string.Empty;
        }

        // Check if path item has the requested operation.
        var operation = GetMatchingOperation(snippetModel);

        // If operation does not exist, return an empty string
        if (operation == null || snippetModel.ApiVersion == "beta")
        {
            return string.Empty;
        }

        // List has an initial capacity of 4. Reserve more based on the number of nodes.
        // Reduces reallocations at the expense of more memory used.
        var initialCapacity = Math.Max(snippetModel.PathNodes.Count, 20);
        var command = new List<string>(initialCapacity)
        {
            GetCommandName(snippetModel)
        };

        var parameters = new Dictionary<string, string>(capacity: initialCapacity);

        // Check if the last node has a child that is a collection index.
        // Get & Post requests will be changed to list & create respectively)
        // If the last node is a collection index, the operation names are not
        // changed
        var isLastNodeCollection = !snippetModel.EndPathNode.Segment.IsCollectionIndex()
                        && snippetModel.EndPathNode.Children.Any(c => c.Key.IsCollectionIndex()); ;

        var matchedOperation = $"{snippetModel.Method}".ToLowerInvariant();
        var operationName = matchedOperation;
        if (isLastNodeCollection)
        {
            switch (matchedOperation)
            {
                case "get":
                    operationName = "list";
                    break;
                case "post":
                    operationName = "create";
                    break;
            }
        }

        foreach (var node in snippetModel.PathNodes)
        {
            var segment = node.Segment.Replace("$value", "content").TrimStart('$');
            if (segment.IsCollectionIndex())
            {
                command.Add("item");
                AddParameterToDictionary(ref parameters, segment);
            }
            else
            {
                command.Add(NormalizeToOption(segment));
            }
        }

        IEnumerable<(string, string)> splitQueryString = Array.Empty<(string, string)>();
        if (!string.IsNullOrWhiteSpace(snippetModel.QueryString))
        {
            splitQueryString = snippetModel.QueryString
                    .Remove(0, 1)
                    .Split('&')
                    .Select(q =>
                    {
                        var x = q.Split('=');
                        return x.Length > 1 ? (x[0], x[1]) : (x[0], string.Empty);
                    });
        }

        var matchingParams = operation.Parameters
                    .Where(p => p.In != ParameterLocation.Path && splitQueryString
                        .Any(s => s.Item1
                            .Equals(p.Name, StringComparison.OrdinalIgnoreCase)));

        foreach (var param in matchingParams)
        {
            AddParameterToDictionary(ref parameters, param.Name);
        }

        command.Add(operationName);

        command.AddRange(parameters.Select(p => $"{p.Key} {p.Value}"));
        var payload = GetRequestPayLoad(snippetModel);
        if (!string.IsNullOrWhiteSpace(payload))
        {
            command.Add(payload);
        }
        return command.Aggregate("", (accum, val) => string.IsNullOrWhiteSpace(accum) ? val : $"{accum} {val}");
    }

    /// <summary>
    /// Adds a new parameter to the dictionary or replaces an existing one.
    /// </summary>
    /// <remarks>
    /// NOTE: This function modifies the input dictionary.
    /// </remarks>
    /// <param name="parameters">The input dictionary.</param>
    /// <param name="name">The name of the new parameter.</param>
    private static void AddParameterToDictionary(ref Dictionary<string, string> parameters, in string name)
    {
        // TODO: Should the snippets contain the values entered in the URL as well?
        // e.g. mgc tests --id 120 instead of mgc tests --id {id}
        // Remove surrounding braces i.e. { and }
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var paramName = name;
        var addBraces = (true, true);

        if (paramName.StartsWith('{'))
        {
            paramName = paramName.Remove(0, 1);
            addBraces.Item1 = false;
        }

        if (paramName.EndsWith('}'))
        {
            paramName = paramName.Remove(paramName.Length - 1, 1);
            addBraces.Item2 = false;
        }

        var key = $"--{NormalizeToOption(paramName)}";
        var value = $"{(addBraces.Item1 ? '{' : "")}{name}{(addBraces.Item2 ? '}' : "")}";
        if (parameters.ContainsKey(key))
        {
            // In the case of conflicting keys, this code will replace
            // the value with the latest one.
            // OpenAPI documents should not have duplicate path objects
            // Each template expression in OpenAPI must have a
            // corresponsing path parameter. Path parameters must also
            // be unique on the name+location.
            // TODO: is there a good way to handle duplicate names? The
            // The CLI's current mapping of param name => option does
            // not allow a way to have multiple parameters with the
            // same name even when the locations of the 2 parameters
            // are different.
            //
            // So, /users/{id}/tasks?id={id} will result in 1 option on
            // the CLI.
            // See https://github.com/microsoftgraph/msgraph-cli/issues/206
            parameters[key] = value;
        }
        else
        {
            parameters.Add(key, value);
        }
    }

    private static string GetRequestPayLoad([NotNull] in SnippetModel snippetModel)
    {
        if (string.IsNullOrWhiteSpace(snippetModel.RequestBody)
                || "undefined".Equals(snippetModel.RequestBody, StringComparison.OrdinalIgnoreCase)) // graph explorer sends "undefined" as request body for some reason
        {
            return null;
        }

        var payload = (snippetModel.ContentType?.Split(';').First().ToLowerInvariant()) switch
        {
            // Do other types of content exist that can be handled by the body parameter? Currently, JSON, plain text are supported
            "application/json" or "text/plain" => $"--body '{snippetModel.RequestBody}'",
            "application/octet-stream" => $"--file <file path>",
            _ => null, // Unsupported ContentType
        };
        return payload;
    }

    private static OpenApiOperation GetMatchingOperation(in SnippetModel snippetModel)
    {
        var pathItemOperations = snippetModel.EndPathNode.PathItems.SelectMany(p => p.Value.Operations);
        var httpMethod = $"{snippetModel.Method}";

        return pathItemOperations.FirstOrDefault(o =>
        {
            return httpMethod.Equals($"{o.Key}", StringComparison.OrdinalIgnoreCase);
        }).Value;
    }

    private static string GetCommandName(in SnippetModel snippetModel)
    {
        return snippetModel.ApiVersion switch
        {
            "v1.0" => "mgc",
            "beta" => "mgc-beta", // Coverage on this will be possible once the beta CLI is ready. See L27.
            _ => throw new ArgumentException("Unsupported API version"),
        };
    }

    /// <summary>
    /// Converts camel-case or delimited string to '-' delimited string for use as a command option
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static string NormalizeToOption(in string input)
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
