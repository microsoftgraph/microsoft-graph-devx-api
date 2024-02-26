using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.StringExtensions;

namespace CodeSnippetsReflection.OpenAPI;

internal static class StringExtensions
{
    internal static bool IsCollectionIndex(this string pathSegment) =>
        !string.IsNullOrEmpty(pathSegment) && pathSegment.StartsWith('{') && pathSegment.EndsWith('}');
    internal static bool IsFunction(this string pathSegment) => !string.IsNullOrEmpty(pathSegment) && pathSegment.Contains('.');

    private static readonly Regex FunctionWithParameterRegex = new(@"\([\w\s\d=':${}<>|\-,""@]+\)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
    internal static bool IsFunctionWithParameters(this string pathSegment) => !string.IsNullOrEmpty(pathSegment)
                                                                              && FunctionWithParameterRegex.Match(pathSegment).Success;

    internal static bool IsFunctionWithParametersMatch(this string pathSegment, string segment)
    {
        // verify both have parameters
        if (!pathSegment.IsFunctionWithParameters() || !segment.IsFunctionWithParameters())
            return false;

        // verify both have same prefix/name
        if (!pathSegment.Split('(')[0].Equals(segment.Split('(')[0], StringComparison.OrdinalIgnoreCase))
            return false;

        var originalParameters = pathSegment.Split('(')[^1].TrimEnd(')').Split(',').Select(static s => s.Split('=')[0]);
        var compareParameters = segment.Split('(')[^1].TrimEnd(')').Split(',').Select(static s => s.Split('=')[0]);

        return compareParameters.All(parameter => originalParameters.Contains(parameter.Split('=')[0], StringComparer.OrdinalIgnoreCase));
    }
    internal static string RemoveFunctionBraces(this string pathSegment) => pathSegment.TrimEnd('(',')');
    internal static string ReplaceValueIdentifier(this string original) =>
        original?.Replace("$value", "Content", StringComparison.Ordinal);

    private static readonly Regex FunctionWithParams = new(@"^(\w+)\(([^)]+)\)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

    /// <summary>
    /// Returns a Tuple with values
    /// 1. Boolean -> Whether the syntax maches a func with parameters
    /// 2. String -> Name of the function if it is a function, this value is nullable
    /// 3. Dictionary<String,String> -> Key value pair of the variables if its a function
    /// </summary>
    /// <param name="pathSegment"></param>
    /// <returns></returns>
    internal static Tuple<Boolean, String, Dictionary<String, String>> GetFunctionWithParameters(this string pathSegment)
    {
        Dictionary<string, string> variables = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(pathSegment)) return new Tuple<Boolean, string, Dictionary<string, string>>(false, null, variables);

        // Use Regex.Match to find the matches in the function declaration
        Match match = FunctionWithParams.Match(pathSegment);

        if (match.Success)
        {
            string funcName = match.Groups[1].Value;
            string varsContent = match.Groups[2].Value;

            string[] vars = varsContent.Split(',');
            foreach (string variable in vars)
            {
                // Split each variable into name and value
                string[] parts = variable.Split('=');
                if (parts.Length == 2)
                {
                    string varName = parts[0].Trim();
                    string varValue = parts[1].Trim().Trim('\'');

                    variables[varName] = varValue;
                }
            }

            return new Tuple<Boolean, string, Dictionary<string, string>>(true, funcName, variables);
        }
        else
        {
            return new Tuple<Boolean, string, Dictionary<string, string>>(false, null, variables);
        }
    }

    internal static string Append(this string original, string suffix) =>
        string.IsNullOrEmpty(original) ? original : original + suffix;

    private static readonly Regex PropertyCleanupRegex = new(@"[""\s!#$%&'()*+,./:;<=>?@\[\]\\^`{}|~-](?<followingLetter>\w)?", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
    private const string CleanupGroupName = "followingLetter";
    internal static string CleanupSymbolName(this string original)
    {
        if (string.IsNullOrEmpty(original))
            return original;

        var result = PropertyCleanupRegex.Replace(original, static x => x.Groups.Keys.Contains(CleanupGroupName) ?
            x.Groups[CleanupGroupName].Value.ToFirstCharacterUpperCase() :
            string.Empty); //strip out any invalid characters, and replace any following one by its uppercase version

        return result;
    }

    /// <summary>
    /// Returns the last portion of a namespaced segment string
    /// </summary>
    /// <param name="segmentString"></param>
    /// <returns></returns>
    public static string GetPartFunctionNameFromNameSpacedSegmentString(this string segmentString)
        {
            var nameOptions = segmentString.Split(
                '('  //remove function brackets
            )[0].Split(
                ".", //split by namespace
                StringSplitOptions.RemoveEmptyEntries
            );
            var splitOptionsCount = nameOptions.Length;
            // retain only last part of the namespace and Capitalize first Character
            var functionName = nameOptions[splitOptionsCount-1].ToFirstCharacterUpperCase();
            return functionName;
        }
}
