using System;
using System.Net.Http;
using System.Xml;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using CodeSnippetsReflection.LanguageGenerators;
using System.Collections.Generic;

namespace CodeSnippetsReflection
{
    /// <summary>
    /// Snippets Generator Class with all the logic for code generation
    /// </summary>
    public class SnippetsGenerator : ISnippetsGenerator
    {
        public static HashSet<string> SupportedLanguages = new HashSet<string>
        {
            "c#",
            "javascript",
            "objective-c",
            "java"
        };

        private Lazy<IEdmModel> IedmModelV1 { get; set; }
        private Lazy<IEdmModel> IedmModelBeta { get; set; }
        private Uri ServiceRootV1 { get; set; }
        private Uri ServiceRootBeta { get; set; }
        private JavascriptExpressions JavascriptExpressions { get; }
        private CSharpExpressions CSharpExpressions { get; }
        private ObjectiveCExpressions ObjectiveCExpressions { get; }
        private JavaExpressions JavaExpressions { get; }

        /// <summary>
        /// Class holding the Edm model and request processing for snippet generations
        /// </summary>
        public SnippetsGenerator()
        {
            LoadGraphMetadata();
            JavascriptExpressions = new JavascriptExpressions();
            CSharpExpressions = new CSharpExpressions();
            ObjectiveCExpressions = new ObjectiveCExpressions();
            JavaExpressions = new JavaExpressions();
        }

        /// <summary>
        /// Load the IEdmModel for both V1 and Beta
        /// </summary>
        private void LoadGraphMetadata()
        {
            ServiceRootV1 = new Uri("https://graph.microsoft.com/v1.0");
            ServiceRootBeta = new Uri("https://graph.microsoft.com/beta");

            // use clean metadata
            IedmModelV1 = new Lazy<IEdmModel>(() => CsdlReader.Parse(XmlReader.Create("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/clean_v10_metadata/cleanMetadataWithDescriptionsv1.0.xml")));
            IedmModelBeta = new Lazy<IEdmModel>(() => CsdlReader.Parse(XmlReader.Create("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/clean_beta_metadata/cleanMetadataWithDescriptionsbeta.xml")));
        }

        /// <summary>
        /// Entry point to generate snippets from the payload 
        /// </summary>
        /// <param name="language"></param>
        /// <param name="httpRequestMessage"></param>
        /// <returns>String of snippet generated</returns>
        public string ProcessPayloadRequest(HttpRequestMessage httpRequestMessage, string language)
        {
            var (edmModel, serviceRootUri) = GetModelAndServiceUriTuple(httpRequestMessage.RequestUri);
            var snippetModel = new SnippetModel(httpRequestMessage, serviceRootUri.AbsoluteUri, edmModel);

            switch (language.ToLower())
            {
                case "c#":
                    var csharpGenerator = new CSharpGenerator(edmModel);
                    return csharpGenerator.GenerateCodeSnippet(snippetModel, CSharpExpressions);

                case "javascript":
                    return JavaScriptGenerator.GenerateCodeSnippet(snippetModel, JavascriptExpressions);

                case "objective-c":
                    var objectiveCGenerator = new ObjectiveCGenerator(edmModel);
                    return objectiveCGenerator.GenerateCodeSnippet(snippetModel, ObjectiveCExpressions);

                case "java":
                    var javaGenerator = new JavaGenerator(edmModel);
                    return javaGenerator.GenerateCodeSnippet(snippetModel, JavaExpressions);

                default:
                    throw new Exception("Invalid Language selected");

            }
        }

        /// <summary>
        /// Helper function to select the appropriate EDM model and service root url depending on the request made
        /// </summary>
        /// <param name="requestUri">The URI of the service requested</param>
        /// <returns>Tuple of the Edm model and the URI of the service root</returns>
        private (IEdmModel, Uri) GetModelAndServiceUriTuple(Uri requestUri)
        {
            switch (requestUri.Segments[1])
            {
                case "v1.0/":
                    return (IedmModelV1.Value, ServiceRootV1);

                case "beta/":
                    return (IedmModelBeta.Value, ServiceRootBeta);

                default:
                    throw new Exception("Unsupported Graph version in url");
            }

        }
    }
}
