using CodeSnippetsReflection.LanguageGenerators;
using Microsoft.OData.Edm;
using System;
using System.Linq;
using System.Text;

namespace CodeSnippetsReflection.TypeProperties
{
    public class CSharpTypeProperties
    {
        /// <summary>
        /// Default namespace string for the graph
        /// </summary>
        private const string DefaultNamespace = "microsoft.graph";

        /// <summary>
        /// returns C# class name from graph type: microsoft.graph.data => Data
        /// </summary>
        public string ClassName {
            get {
                var className = CommonGenerator.UppercaseFirstLetter(EdmType.ToString().Split(".").Last());
                if (className == "Json")
                {
                    className = "JToken"; // graph.Json is JToken in SDK
                }
                else if (className.EndsWith("Request"))
                {
                    className += "Object"; // disambiguate class names that end with Request
                }

                string namespaceString = (string)EdmType.GetType().GetProperty("Namespace")?.GetValue(EdmType, null) ?? DefaultNamespace;
                // the classname is okay if we are in the default(or Edm) namespace.
                if (namespaceString.Equals(DefaultNamespace) || namespaceString == "Edm") 
                    return className;

                // Otherwise use fully qualified name
                return GetFullyQualifiedName($"{namespaceString}.{className}");
            }
        }

        /// <summary>
        /// Whether the type is open, i.e. supports arbitrary properties (which goes in AdditionalData dictionary in SDK)
        /// </summary>
        public bool IsOpenType => EdmType.IsOpen();

        /// <summary>
        /// Edm type
        /// </summary>
        public readonly IEdmType EdmType;

        /// <summary>
        /// Whether found property is a navigation property or not
        /// Used in decision for ICollectionPage casting
        /// </summary>
        public readonly bool IsNavigationProperty;

        /// <summary>
        /// initializes a properties object
        /// </summary>
        /// <param name="edmType">edm type</param>
        /// <param name="isNavigationProperty">Whether found property is a navigation property or not</param>
        public CSharpTypeProperties(IEdmType edmType, bool isNavigationProperty)
        {
            EdmType = edmType;
            IsNavigationProperty = isNavigationProperty;
        }

        /// <summary>
        /// converts fully qualified name from OData metadata to .NET SDK fully qualified name
        /// </summary>
        /// <param name="metadataTypeName">metadata fully qualified type name, e.g.:  microsoft.graph.ediscovery.legalHold</param>
        /// <returns>.NET SDK fully qualified name, e.g. Microsoft.Graph.Ediscovery.LegalHold</returns>
        public static string GetFullyQualifiedName(string metadataTypeName)
        {
            return metadataTypeName switch
            {
                null => throw new ArgumentNullException(nameof(metadataTypeName)),
                _ => string.Join(".", metadataTypeName.Split(".").Select(CommonGenerator.UppercaseFirstLetter))
            };
        }
    }
}
