using System;
using System.Collections.Generic;
using System.Text;

namespace CodeSnippetsReflection.LanguageGenerators
{
    public interface ILanguageGenerator
    {
        string GenerateCodeSnippet(SnippetModel snippetModel);
    }
}
