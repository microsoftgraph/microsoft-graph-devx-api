using CodeSnippetsReflection.LanguageGenerators;
using Microsoft.OData.Edm;
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

                // Otherwise modify the classname by concatenating with the Namespace prefix.
                // Join any parts by uppercase first letter and dots.
                var segments = namespaceString.Split(".");
                var stringBuilder = new StringBuilder();
                foreach (var segment in segments)
                {
                    // prepend the uppercase of each segment and form the classname
                    var uppercaseSegment = CommonGenerator.UppercaseFirstLetter(segment);
                    stringBuilder.Append("." + uppercaseSegment);
                }
                // append the classname
                stringBuilder.Append("." + className);
                // remove starting dot
                return stringBuilder.ToString().Substring(1);
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
    }
}
