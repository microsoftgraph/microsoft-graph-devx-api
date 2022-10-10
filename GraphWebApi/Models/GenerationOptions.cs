using System;
using System.Collections.Generic;

namespace GraphWebApi.Models;

public class GenerationOptions
{
    public string ClientClassName { get; set; } = "GraphServiceClient";
    public string ClientNamespaceName { get; set; } = "Microsoft.Graph.Client";
    public HashSet<string> IncludePatterns { get; set; } = new();
    public HashSet<string> ExcludePatterns { get; set; } = new();
    public GenerationLanguage GenerationLanguage
    {
        get; set;
    }
    private string _version = "v1.0";
    public string Version
    {
        get
        {
            return _version;
        }
        set
        {
            _version = value.ToLowerInvariant() switch
            {
                "beta" => value,
                "v1.0" => value,
                _ => throw new ArgumentException("Invalid version", nameof(value)),
            };
        }
    }
}

public enum GenerationLanguage
{
    CSharp,
    Java,
    TypeScript,
    PHP,
    Python,
    Go,
    Swift,
    Ruby,
    Shell
}
