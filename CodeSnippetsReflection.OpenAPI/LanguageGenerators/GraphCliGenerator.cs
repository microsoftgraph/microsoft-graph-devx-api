using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators;

public partial class GraphCliGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
{
    private static readonly Regex camelCaseRegex = CamelCaseRegex();
    private static readonly Regex delimitedRegex = DelimitedRegex();
    private static readonly Regex overloadedBoundedFunctionWithSingleOrMultipleParameters = new(@"\w+\([a-zA-Z,={}'-]+\)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
    private static readonly Regex overloadedBoundedHyphenatedFunctionWithSingleOrMultipleParameters = new(@"(?:\w+-)+\w+\([a-zA-Z,={}'-]+\)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
    private static readonly Regex unBoundFunctionRegex = new(@"^[0-9a-zA-Z\- \/_?:.,\s]+\(\)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    private const string PathItemsKey = "default";

    public string GenerateCodeSnippet(SnippetModel snippetModel)
    {
        // Check if path item has the requested operation.
        var operation = GetMatchingOperation(snippetModel);

        // If operation does not exist, return an empty string
        if (operation == null)
        {
            return string.Empty;
        }

        // List has an initial capacity of 4. Reserve more based on the number of nodes.
        // Reduces reallocations at the expense of more memory used.
        var initialCapacity = Math.Max(snippetModel.PathNodes.Count, 20);
        var commandSegments = new List<string>(initialCapacity)
        {
            GetCommandName(snippetModel)
        };

        var parameters = new Dictionary<string, string>(capacity: initialCapacity);

        // Adds command segment names to the commandSegments list then adds the
        // parameters to the parameters dictionary.
        ProcessCommandSegmentsAndParameters(snippetModel, ref commandSegments, ref operation, ref parameters);

        return Environment.NewLine + commandSegments.Aggregate("", (accum, val) => string.IsNullOrWhiteSpace(accum) ? val : $"{accum} {val}")
                    .Replace("\n", "\\\n", StringComparison.Ordinal)
                    .Replace("\r\n", "\\\r\n", StringComparison.Ordinal);
    }

    private static string GetOperationName([NotNull] in SnippetModel snippetModel)
    {
        // Check if the last node has a child that is a collection index.
        // Get & Post requests will be changed to list & create respectively)
        // If the last node is a collection index, the operation names are not
        // changed
        var isLastNodeCollection = !snippetModel.EndPathNode.Segment.IsCollectionIndex()
                        && snippetModel.EndPathNode.Children.Any(c => c.Key.IsCollectionIndex());

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

        return operationName;
    }

    private static void ProcessCommandSegmentsAndParameters([NotNull] in SnippetModel snippetModel, [NotNull] ref List<string> commandSegments, [NotNull] ref OpenApiOperation operation, [NotNull] ref Dictionary<string, string> parameters)
    {
        // Cache the last 2 path nodes for conflict checking
        OpenApiUrlTreeNode prevPrevNode = null;
        OpenApiUrlTreeNode prevNode = null;
        foreach (var node in snippetModel.PathNodes)
        {
            var segment = node.Segment.ReplaceValueIdentifier().TrimStart('$');

            // Handle path operation conflicts
            // GET /users/{user-id}/directReports/graph.orgContact
            // GET /users/{user-id}/directReports/{directoryObject-id}/graph.orgContact
            if (prevPrevNode is not null && prevNode is not null && prevNode.IsParameter &&
                prevPrevNode.Children.TryGetValue(node.Segment, out var prevPrevNodeMatch) &&
                node.PathItems.TryGetValue(PathItemsKey, out var nodeDefaultItem) &&
                prevPrevNodeMatch.PathItems.TryGetValue(PathItemsKey, out var prevPrevNodeDefaultItem) &&
                nodeDefaultItem.Operations.Any(x => prevPrevNodeDefaultItem.Operations.ContainsKey(x.Key)))
            {
                segment += "ById";
            }

            if (segment.IsCollectionIndex())
            {
                AddParameterToDictionary(ref parameters, segment);
            }
            else
            {
                commandSegments.Add(NormalizeToOption(segment));
            }
            prevPrevNode = prevNode;
            prevNode = node;
        }

        // Adds query parameters from the request into the parameters dictionary
        var processedQuery = ProcessQueryParameters(snippetModel);
        PostProcessParameters(processedQuery, operation, ParameterLocation.Query, ref parameters);

        // Adds header parameters from the request into the parameters dictionary
        var processedHeaders = ProcessHeaderParameters(snippetModel);
        PostProcessParameters(processedHeaders, operation, ParameterLocation.Header, ref parameters);

        var operationName = GetOperationName(snippetModel);

        commandSegments.Add(operationName);

        commandSegments.AddRange(parameters.Select(p => $"{p.Key} {p.Value}"));

        // Gets the request payload
        var payload = GetRequestPayLoad(snippetModel);
        if (!string.IsNullOrWhiteSpace(payload))
        {
            commandSegments.Add(payload);
        }

        FetchUnBoundFunctions(commandSegments);
        ProcessMeSegments(commandSegments, operationName);
        if (commandSegments.Any(static u => u.Contains("=")))
            FetchOverLoadedBoundFunctions(commandSegments, operationName, snippetModel);

    }

    /// <summary>
    /// Checks for segments that have unbound functions
    /// Example "identity-providers available-provider-types() get"
    /// will be reconstructed to identity-providers available-provider-types get
    /// </summary>
    /// <param name="commandSegments"></param>
    private static void FetchUnBoundFunctions(List<string> commandSegments)
    {
        int unboundedFunctionIndex = commandSegments.FindIndex(static u => unBoundFunctionRegex.IsMatch(u));
        if (unboundedFunctionIndex != -1)
        {
            var segment = commandSegments[unboundedFunctionIndex];
            commandSegments[unboundedFunctionIndex] = segment.Replace("(", "").Replace(")", "");
        }
    }

    /// <summary>
    /// Checks for segments that have overloaded bound functions with date parameter
    /// Example of such a segment would be: getYammerDeviceUsageUserDetail(date=2018-03-05).
    /// ProcessOverloadedBoundFunction is called to reconstruct the segment to the expected command segment.
    /// </summary>
    /// <param name="commandSegments"></param>
    /// <param name="operationName"></param>
    private static void FetchOverLoadedBoundFunctions(List<string> commandSegments, string operationName, SnippetModel snippetModel)
    {
        int boundedFunctionIndex = commandSegments.FindIndex(static u => overloadedBoundedFunctionWithSingleOrMultipleParameters.IsMatch(u) || overloadedBoundedHyphenatedFunctionWithSingleOrMultipleParameters.IsMatch(u));

        if (boundedFunctionIndex != -1)
        {
            int operationIndex = commandSegments.FindIndex(o => operationName.Equals(o, StringComparison.OrdinalIgnoreCase));
            var (updatedSegment, updatedOperation) = ProcessOverloadedBoundFunctions(commandSegments[boundedFunctionIndex], operationName);
            commandSegments[boundedFunctionIndex] = updatedSegment;
            commandSegments[operationIndex] = updatedOperation;
        }
    }

    /// <summary>
    /// Reconstructs segments with overloaded bound functions to the expected command segments.
    /// For example; "get-yammer-device-usage-user-detail(date={date})" get will be reconstructed to
    /// "get-yammer-device-usage-user-detail-with-date get --date {date_id}" as expected by cli for it
    /// to execute successfully.
    /// </summary>
    /// <param name="segment"></param>
    /// <param name="operation"></param>
    /// <returns></returns>
    private static (string, string) ProcessOverloadedBoundFunctions(string segment, string operation)
    {
        StringBuilder parameterBuilder = new StringBuilder();
        StringBuilder SegmentBuilder = new StringBuilder();
        var functionItems = segment.Split("(");
        var functionParams = functionItems[1];
        var functionName = functionItems[0];
        SegmentBuilder.Append(functionName);
        var parameters = functionParams.Split(",");

        foreach (var parameter in parameters)
        {
            var parameterValue = parameter.Split("=")[0];
            var updateSegmentDetails = !parameterValue.Contains("-id") ? parameterValue + " {" + parameterValue + "-id}" : parameterValue + " {" + parameterValue + "}";
            parameterBuilder.Append(parameters.Length > 1 ? $"--{updateSegmentDetails} " : $"--{updateSegmentDetails}");
            var updatedSegment = $"-with-{parameterValue}";
            SegmentBuilder.Append(updatedSegment);
        }

        return (SegmentBuilder.ToString(), $"{operation} {parameterBuilder.ToString()}");
    }

    /// <summary>
    /// Replaces /me endpoints with users and appends --user-id parameter to the command
    /// See issue: https://github.com/microsoftgraph/msgraph-cli/issues/278
    /// </summary>
    /// <param name="commandSegments"></param>
    /// <param name="operationName"></param>
    private static void ProcessMeSegments(List<string> commandSegments, string operationName)
    {
        if (commandSegments[1].Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            commandSegments[1] = "users";
            int operationIndex = commandSegments.FindIndex(o => o.Equals(operationName, StringComparison.OrdinalIgnoreCase));
            commandSegments[operationIndex] = $"{operationName} --user-id {{user-id}}";
        }
    }

    private static IDictionary<string, string> ProcessHeaderParameters([NotNull] in SnippetModel snippetModel)
    {
        return snippetModel.RequestHeaders.ToDictionary(x => x.Key, x => string.Join(',', x.Value));
    }

    private static IDictionary<string, string> ProcessQueryParameters([NotNull] in SnippetModel snippetModel)
    {
        IDictionary<string, string> splitQueryString = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(snippetModel.QueryString))
        {
            splitQueryString = snippetModel.QueryString
                    .Remove(0, 1)
                    .Split('&')
                    .Select(static q =>
                    {
                        if (q.Contains("("))
                        {
                            var qs = q;
                            int fl = q.Length;
                            qs = qs.Substring(0, qs.IndexOf("("));
                            int isl = qs.Length;
                            StringBuilder sb = new();
                            for (int i = isl; i < fl; i++)
                            {
                                sb.Append(q[i]);
                            }
                            var xs = qs.Split("=");
                            return xs.Length > 1 ? (xs[0], xs[1] + sb.Replace("$", "\\$")) : (xs[0], string.Empty);

                        }

                        var x = q.Split('=');
                        return x.Length > 1 ? (x[0], x[1]) : (x[0], string.Empty);
                    })
                    .Where(static t => !string.IsNullOrWhiteSpace(t.Item2))
                    .ToDictionary(static t => t.Item1, static t => t.Item2);

        }

        return splitQueryString;
    }

    private static void PostProcessParameters([NotNull] in IDictionary<string, string> processedParameters, in OpenApiOperation operation, ParameterLocation? location, [NotNull] ref Dictionary<string, string> parameters)
    {
        var processed = processedParameters;
        var matchingParams = operation.Parameters
                    .Where(p => p.In == location && processed
                        .Any(s => s.Key
                            .Equals(p.Name, StringComparison.OrdinalIgnoreCase)))
                    .Where(p => !string.IsNullOrWhiteSpace(p.Name));

        foreach (var param in matchingParams)
        {
            AddParameterToDictionary(ref parameters, $"{{{param.Name}}}", processed[param.Name], param.In);
        }
    }

    /// <summary>
    /// Adds a new parameter to the dictionary or replaces an existing one.
    /// This function handles surrounding braces in the following ways:
    /// <list type="number">
    /// <item>
    /// <description>
    /// If the name has surrounding braces, then the dictionary entry's key
    /// will have the braces trimmed and the value will contain the braces as
    /// they appear. e.g. if <c>name</c> is <c>{test}</c>, then the key will be
    /// <c>--test</c> and the value will be <c>{test}</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// If the name has no surrounding braces, then the dictionary entry's key
    /// will appear as provided. e.g. if <c>name</c> is <c>test</c>, then the key will be
    /// <c>--test</c> and the value will be <c>test</c>
    /// </description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// NOTE: This function modifies the input dictionary.
    /// NOTE 2: If the parameters are duplicated, then this function applies a
    /// deduplication logic so that all parameters will appear in the dictionary.
    /// For example, if this function is called twice as follows:
    /// <list type="number">
    /// <item>
    /// <description>
    /// with name <c>test</c> and location in <c>Path</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// with name <c>test</c> and location in <c>Query</c>
    /// </description>
    /// </item>
    /// </list>
    /// then the dictionary will contain the data:
    /// <c>{"test": "test", "test-query": "test-query"}
    /// </remarks>
    /// <param name="parameters">The input dictionary.</param>
    /// <param name="name">The name of the new parameter.</param>
    /// <param name="value">
    /// The value of the new parameter. The value in the dictionary will
    /// exactly match this value. If it's null or empty, the parameter is added
    /// to the dictionary with a generated value.
    /// </param>
    /// <param name="location">The location of the parameter. This is used to construct deduplicated CLI options</param>
    private static void AddParameterToDictionary([NotNull] ref Dictionary<string, string> parameters, in string name, in string value = null, in ParameterLocation? location = ParameterLocation.Path)
    {
        // Snippet path parameter values are replaced with a placeholder, but
        // other parameters aren't.
        // e.g. mgc tests --id 120 instead of mgc tests --id {id}
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        // Remove surrounding braces i.e. { and }
        var paramName = name.StartsWith('{') && name.EndsWith('}')
                        ? name.TrimStart('{').TrimEnd('}')
                        : name;
        var key = $"--{NormalizeToOption(paramName.CleanupSymbolName())}";
        var paramValue = string.IsNullOrWhiteSpace(value) ? name : value;

        if (parameters.ContainsKey(key))
        {
            // In the case of conflicting keys, this code will deduplicate
            // the parameter names by adding a suffix to the non-path
            // parameters.
            // In the OpenAPI spec, parameters must be unique on the
            // name+location fields.
            // Due to this, kiota deduplicates the parameters by adding a
            // suffix to the name.
            // /users/{id}/tasks?id={id} should have 2 parameters in the CLI:
            // --id for the path parameter, and --id-query for the location.
            // The logic is: if any parameter conflicts with a path parameter,
            // the CLI option becomes --{name}-{location} where location is
            // either query or header.
            //
            // See https://github.com/microsoft/kiota/pull/2138
            // Note: If the location is a path and the code is in this branch,
            // then it means 2 paths have the same name+location which is
            // forbidden in the OpenAPI spec. Additionally, if the location
            // parameter is null, we can't create a deduplicated parameter. The
            // location parameter being empty indicates a problem in the
            // OpenAPI library since parameter locations are required in the OpenAPI spec.

            var loc = location switch
            {
                ParameterLocation.Query => "query",
                ParameterLocation.Header => "header",
                _ => null,
            };

            // Don't attempt to deduplicate invalid parameters.
            if (location == ParameterLocation.Path || string.IsNullOrEmpty(loc))
            {
                return;
            }

            // add the suffix
            key = $"{key}-{loc}";

            // Check if the deduplicated key already exists.
            if (parameters.ContainsKey(key))
            {
                // Should this throw an exception instead of returning?
                // Exceptions will need to be handled
                return;
            }

            parameters[key] = paramValue;
        }
        else
        {
            parameters.Add(key, !paramValue.Contains("{") ? $"\"{paramValue}\"" : paramValue);
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
        if (snippetModel == null)
        {
            return null;
        }

        var pathItemOperations = snippetModel.EndPathNode.PathItems.SelectMany(p => p.Value.Operations);
        var httpMethod = $"{snippetModel.Method}";

        return pathItemOperations.FirstOrDefault(o =>
        {
            return httpMethod.Equals($"{o.Key}", StringComparison.OrdinalIgnoreCase);
        }).Value;
    }

    private static string GetCommandName([NotNull] in SnippetModel snippetModel)
    {
        return snippetModel.ApiVersion switch
        {
            "v1.0" => "mgc",
            "beta" => "mgc-beta", // Coverage on this will be possible once the beta CLI is ready. See L183.
            _ => throw new ArgumentException("Unsupported API version"),
        };
    }

    /// <summary>
    /// Converts camel-case or delimited string to '-' delimited string for use as a command option
    /// </summary>
    /// <param name="input"></param>
    /// <returns>A '-' delimited string for use as a command option</returns>
    private static string NormalizeToOption(in string input)
    {
        var result = camelCaseRegex.Replace(input, "-$1");
        // 2 passes for cases like "singleValueLegacyExtendedProperty_id"
        result = delimitedRegex.Replace(result, "-$1");

        return result.ToLower();
    }

    [GeneratedRegex("(?<=[0-9a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex CamelCaseRegex();

    [GeneratedRegex("(?<=[a-z])[-_\\.]+([A-Za-z])", RegexOptions.Compiled)]
    private static partial Regex DelimitedRegex();
}
