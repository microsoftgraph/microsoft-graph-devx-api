// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

// THIS CLASS IS COPIED FROM KIOTA TO GET THE SAME NAMING CONVENTIONS, WE SHOULD FIND A WAY TO MUTUALIZE THE CODE
namespace CodeSnippetsReflection.OpenAPI {

    public static class KiotaOpenApiUrlTreeNodeExtensions {
        private static readonly Regex PathParametersRegex = new(@"(?:\w+)?=?'?\{(?<paramName>\w+)\}'?,?", RegexOptions.Compiled);
        private static readonly char requestParametersChar = '{';
        private static readonly char requestParametersEndChar = '}';
        private static readonly char requestParametersSectionChar = '(';
        private static readonly char requestParametersSectionEndChar = ')';
        private static readonly MatchEvaluator requestParametersMatchEvaluator = (match) => {
            return "With" + match.Groups["paramName"].Value.ToFirstCharacterUpperCase();
        };
        private static readonly Regex idClassNameCleanup = new(@"Id\d?$", RegexOptions.Compiled);
        ///<summary>
        /// Returns the class name for the node with more or less precision depending on the provided arguments
        ///</summary>
        public static string GetClassName(this OpenApiUrlTreeNode currentNode, string suffix = default, string prefix = default, OpenApiOperation operation = default) {
            var rawClassName = (operation?.GetResponseSchema()?.Reference?.GetClassName() ?? 
                                CleanupParametersFromPath(currentNode.Segment)?.ReplaceValueIdentifier())
                                .TrimEnd(requestParametersEndChar)
                                .TrimStart(requestParametersChar)
                                .TrimStart('$') //$ref from OData
                                .Split('-')
                                .First();
            if((currentNode?.DoesNodeBelongToItemSubnamespace() ?? false) && idClassNameCleanup.IsMatch(rawClassName))
                rawClassName = idClassNameCleanup.Replace(rawClassName, string.Empty);
            return prefix + rawClassName?.Split('.', StringSplitOptions.RemoveEmptyEntries)?.LastOrDefault() + suffix;
        }
        public static bool DoesNodeBelongToItemSubnamespace(this OpenApiUrlTreeNode currentNode) => currentNode.IsPathSegmentWithSingleSimpleParameter();
        public static bool IsPathSegmentWithSingleSimpleParameter(this OpenApiUrlTreeNode currentNode) =>
            currentNode?.Segment.IsPathSegmentWithSingleSimpleParameter() ?? false;
        private static bool IsPathSegmentWithSingleSimpleParameter(this string currentSegment)
        {
            return (currentSegment?.StartsWith(requestParametersChar) ?? false) &&
                    currentSegment.EndsWith(requestParametersEndChar) &&
                    currentSegment.Count(x => x == requestParametersChar) == 1;
        }
        private static string CleanupParametersFromPath(string pathSegment) {
            if((pathSegment?.Contains(requestParametersChar) ?? false) ||
                (pathSegment?.Contains(requestParametersSectionChar) ?? false))
                return PathParametersRegex.Replace(pathSegment, requestParametersMatchEvaluator)
                                        .TrimEnd(requestParametersSectionEndChar)
                                        .Replace(requestParametersSectionChar.ToString(), string.Empty);
            return pathSegment;
        }
        public static string GetNamespaceFromPath(this string currentPath, string prefix) => 
            prefix + 
            ((currentPath?.Contains(pathNameSeparator) ?? false) ?
                (string.IsNullOrEmpty(prefix) ? string.Empty : ".")
                + currentPath
                    ?.Split(pathNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                    ?.Select(replaceSingleParameterSegmentByItem)
                    ?.Select(static x => CleanupParametersFromPath((x ?? string.Empty).Split('.', StringSplitOptions.RemoveEmptyEntries)
                        ?.Select(static x => x.TrimStart('$')) //$ref from OData
                        .Last()))
                    ?.Select(static x => x.CleanupSymbolName())
                    ?.Aggregate(string.Empty, 
                        static (x, y) => $"{x}{GetDotIfBothNotNullOfEmpty(x, y)}{y}") :
                string.Empty)
            .ReplaceValueIdentifier();
        public static string GetNodeNamespaceFromPath(this OpenApiUrlTreeNode currentNode, string prefix) =>
            currentNode?.Path?.GetNamespaceFromPath(prefix);
        private static readonly char pathNameSeparator = '\\';
        private static readonly Func<string, string> replaceSingleParameterSegmentByItem =
            x => x.IsPathSegmentWithSingleSimpleParameter() ? "item" : x;
        private static string GetDotIfBothNotNullOfEmpty(string x, string y) => string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) ? string.Empty : ".";
    }
}
