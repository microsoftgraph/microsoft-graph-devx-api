using CodeSnippetsReflection.LanguageGenerators;
using Microsoft.OData.Edm;
using System.Linq;

namespace CodeSnippetsReflection.TypeProperties
{
    public class CSharpTypeProperties
    {
        /// <summary>
        /// returns C# class name from graph type: microsoft.graph.data => Data
        /// </summary>
        public string ClassName {
            get {
                var className = CommonGenerator.UppercaseFirstLetter(EdmType.ToString().Split(".").Last());
                return className.EndsWith("Request") ? className + "Object" : className; // disambiguate class names that end with Request
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
