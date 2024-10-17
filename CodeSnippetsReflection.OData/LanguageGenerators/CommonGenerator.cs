using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using CodeSnippetsReflection.StringExtensions;
using System.Text.Json;

namespace CodeSnippetsReflection.OData.LanguageGenerators
{
    /// <summary>
    /// Common Generator constructor
    /// </summary>
    /// <param name="model">Edm model of metadata</param>
    public class CommonGenerator(IEdmModel model)
    {

        /// <summary>
        /// Language agnostic function to generate Query section of code snippet
        /// </summary>
        /// <param name="snippetModel">Model of the snippet</param>
        /// <param name="languageExpressions">Instance of <see cref="LanguageExpressions"/> that holds the expressions for the specific language</param>
        public static string GenerateQuerySection(SnippetModel snippetModel, LanguageExpressions languageExpressions)
        {
            var snippetBuilder = new StringBuilder();

            //Append any headers section
            foreach (var (key, value) in snippetModel.RequestHeaders)
            {
                //no need to generate source for the host header
                if (key.ToLower().Equals("host", StringComparison.Ordinal))
                    continue;
                //append the header to the snippet
                var valueString = value.First()
                    .EscapeQuotesInLiteral(languageExpressions.DoubleQuotesEscapeSequence, languageExpressions.SingleQuotesEscapeSequence);
                snippetBuilder.Append(string.Format(languageExpressions.HeaderExpression, key, valueString));
            }
            //Append any filter queries
            if (snippetModel.FilterFieldList.Count != 0)
            {
                var filterResult = string.Join(languageExpressions.FilterExpressionDelimiter, snippetModel.FilterFieldList)
                    .EscapeQuotesInLiteral(languageExpressions.DoubleQuotesEscapeSequence, languageExpressions.SingleQuotesEscapeSequence);
                //append the filter to the snippet
                snippetBuilder.Append(string.Format(languageExpressions.FilterExpression, filterResult));
            }

            //Append any search queries
            if (!string.IsNullOrEmpty(snippetModel.SearchExpression))
            {
                snippetBuilder.Append(string.Format(languageExpressions.SearchExpression,
                    snippetModel.SearchExpression
                    .EscapeQuotesInLiteral(languageExpressions.DoubleQuotesEscapeSequence, languageExpressions.SingleQuotesEscapeSequence)));
            }

            //Append the expand section
            if (!string.IsNullOrEmpty(snippetModel.ExpandFieldExpression))
            {
                //append the expand result to the snippet
                snippetBuilder.Append(string.Format(languageExpressions.ExpandExpression,
                    snippetModel.ExpandFieldExpression
                    .EscapeQuotesInLiteral(languageExpressions.DoubleQuotesEscapeSequence, languageExpressions.SingleQuotesEscapeSequence)));
            }

            //Append any select queries
            if (snippetModel.SelectFieldList.Count != 0)
            {
                var selectResult = string.Join(languageExpressions.SelectExpressionDelimiter, snippetModel.SelectFieldList)
                    .EscapeQuotesInLiteral(languageExpressions.DoubleQuotesEscapeSequence, languageExpressions.SingleQuotesEscapeSequence);
                //append the select result to the snippet
                snippetBuilder.Append(string.Format(languageExpressions.SelectExpression, selectResult));
            }

            //Append any orderby queries
            if (snippetModel.OrderByFieldList.Count != 0)
            {
                var orderByResult = string.Join(languageExpressions.OrderByExpressionDelimiter, snippetModel.OrderByFieldList)
                    .EscapeQuotesInLiteral(languageExpressions.DoubleQuotesEscapeSequence, languageExpressions.SingleQuotesEscapeSequence);
                //append the orderby result to the snippet
                snippetBuilder.Append(string.Format(languageExpressions.OrderByExpression, orderByResult));
            }

            //Append any skip queries
            if (snippetModel.ODataUri.Skip.HasValue)
            {
                snippetBuilder.Append(string.Format(languageExpressions.SkipExpression, snippetModel.ODataUri.Skip));
            }

            //Append any skip token queries
            if (!string.IsNullOrEmpty(snippetModel.ODataUri.SkipToken))
            {
                snippetBuilder.Append(string.Format(languageExpressions.SkipTokenExpression,
                    snippetModel.ODataUri.SkipToken
                    .EscapeQuotesInLiteral(languageExpressions.DoubleQuotesEscapeSequence, languageExpressions.SingleQuotesEscapeSequence)));
            }

            //Append any top queries
            if (snippetModel.ODataUri.Top.HasValue)
            {
                snippetBuilder.Append(string.Format(languageExpressions.TopExpression, snippetModel.ODataUri.Top));
            }

            return snippetBuilder.ToString();
        }

        /// <summary>
        /// Function to find the <see cref="EdmType"/> being used by identifier that is the last item in the path collection. This function gets
        /// the right <see cref="IEdmType"/> that is is to be used for the search based on the segment type>
        /// </summary>
        /// <param name="oDataPathSegment">Odata path segment that is the root of the search </param>
        /// <param name="path">List of string that show the depth of the search into the definition from the odataPath type definition</param>
        public IEdmType GetEdmTypeFromIdentifier(ODataPathSegment oDataPathSegment, ICollection<string> path)
        {
            var (edmType, _) = GetEdmTypeFromIdentifierAndNavigationProperty(oDataPathSegment, path);
            return edmType;
        }

        /// <summary>
        /// This function is meant to simulate the function located at the link below to provide consistent generation with the SDK
        /// https://github.com/microsoftgraph/MSGraph-SDK-Code-Generator/blob/aa09c93658b984377a646ba046c9f63fd8a6a6e6/src/GraphODataTemplateWriter/Extensions/OdcmModelExtensions.cs#L227
        /// It returns true if any of the conditions below are met
        /// 1. If a navigation property specifies "ContainsTarget='true'", it is self-contained. 
        ///    Generate a direct path to the item (ie "parent/child").
        /// 2. If a navigation property does not specify ContainsTarget but there is a defined EntitySet 
        ///    of the given type, it is a reference relationship. Generate a reference path to the item (ie "item/$ref").
        /// 3. If a navigation property does not have a defined EntitySet but there is a Singleton which has 
        ///    a self-contained reference to the given type, we can make a relationship to the implied EntitySet of 
        ///    the singleton(i.e. the entity collection contained by the singleton). Generate a reference path to the item (ie "singleton/item/$ref").
        /// 4. Otherwise return false so that the generator can find another way to generate snippets   
        /// </summary>
        /// <param name="navigationPropertyLinkSegment">The navigation property link to test</param>
        /// <returns></returns>
        public bool CanGetServiceCollectionNavigationPropertyForProperty(NavigationPropertyLinkSegment navigationPropertyLinkSegment)
        {
            ArgumentNullException.ThrowIfNull(navigationPropertyLinkSegment);

            if (navigationPropertyLinkSegment.NavigationProperty.ContainsTarget)
                return true;

            // Check if its defined directly in an the entitySet
            var isDirectlyInEntitySet = model.EntityContainer.EntitySets()
                .Any(entitySet => entitySet.EntityType.FullName().Equals(navigationPropertyLinkSegment.NavigationProperty.ToEntityType().FullName(), StringComparison.OrdinalIgnoreCase));

            if (isDirectlyInEntitySet)
                return true;

            // check the navBindings/nav Properties on singletons
            var isImplicitFromSingleton = model.EntityContainer.Singletons()
                            .SelectMany(singleton => singleton.NavigationPropertyBindings.Select(navPropertyBindings => navPropertyBindings.NavigationProperty)// get the nav propertyBinding from the singleton
                                                             .Concat(singleton.EntityType.NavigationProperties()))    // Append the nav properties from the singleton type
                            .Any(property => property.ContainsTarget && property.ToEntityType().FullName().Equals(navigationPropertyLinkSegment.NavigationProperty.ToEntityType().FullName(), StringComparison.OrdinalIgnoreCase));

            return isImplicitFromSingleton;
        }

        /// <summary>
        /// Function to find the <see cref="EdmType"/> being used by identifier that is the last item in the path collection. This function gets
        /// the right <see cref="IEdmType"/> that is is to be used for the search based on the segment type>
        /// </summary>
        /// <param name="oDataPathSegment">Odata path segment that is the root of the search </param>
        /// <param name="path">List of string that show the depth of the search into the definition from the odataPath type definition</param>
        /// <returns>pair of Edm type name and whether type is found as navigation property</returns>
        public (IEdmType type, bool isNavigationProperty) GetEdmTypeFromIdentifierAndNavigationProperty(ODataPathSegment oDataPathSegment, ICollection<string> path)
        {

            IEdmType type;
            bool isNavigationProperty;

            switch (oDataPathSegment)
            {
                case KeySegment _:
                case EntitySetSegment _:
                case NavigationPropertySegment _:
                case NavigationPropertyLinkSegment _:
                case SingletonSegment _:
                case PropertySegment _:
                case ValueSegment _:

                    (type, isNavigationProperty) = SearchForEdmType(oDataPathSegment.EdmType, path);

                    if (null != type)
                        return (type, isNavigationProperty);

                    break;

                case OperationSegment operationSegment:
                    foreach (var parameters in operationSegment.Operations.First().Parameters)
                    {
                        if (!parameters.Name.Equals(path.FirstOrDefault(), StringComparison.OrdinalIgnoreCase))
                            continue;

                        (type, isNavigationProperty) = SearchForEdmType(parameters.Type.Definition, path);

                        if (null != type)
                            return (type, isNavigationProperty);
                    }
                    break;
            }

            throw new Exception($"No Class Name Found for Identifier {path.Last()}");
        }

        /// <summary>
        /// Function to find the name of the type/class being used by identifier that is the last item in the path collection.
        /// This function can make recursive calls
        /// </summary>
        /// <param name="definition"><see cref="IEdmType"/> that has properties to be searched into</param>
        /// <param name="searchPath">List of string that shows the depth of the search into the definition from the odataPath type definition</param>
        /// <param name="searchingParent">Denotes whether we are searching for the property in parent classes</param>
        /// <returns>pair of Edm type name and whether type is found as navigation property</returns>
        public (IEdmType type, bool isNavigationProperty) SearchForEdmType(IEdmType definition, ICollection<string> searchPath, bool searchingParent = false)
        {
            // if the type is a collection, use the type of the element of the collection
            var elementDefinition = GetEdmElementType(definition);

            // we are at the root of the search so just return the definition of the root item
            if (searchPath.Count <= 1)
            {
                return (elementDefinition, false);
            }

            // Find all derived types. This is required in cases where the path refers to a parent class,
            // but the searched propery belongs to the child class.
            // Example: "post/attachments" path refers to attachments as "Attachment", but one can have a "FileAttachment"
            // in post. "ContentBytes", which exists only on the derived class, doesn't show up if looked in "Attachment".
            // We are also checking if we are searching parent classes at the moment for inherited properties. If that is the case,
            // we don't go to derived types, because that would cause searching for properties in sibling classes.
            // It can cause infinite loop if the property is not found, because we have a way of both going up and down in the
            // class hierarchy.
            IEnumerable<IEdmStructuredType> derivedTypes = null;
            if (!searchingParent && elementDefinition is IEdmStructuredType structuredTypeDefinition)
            {
                derivedTypes = model?.FindAllDerivedTypes(structuredTypeDefinition);
            }

            // the second element in the path is the property we are searching for
            var searchIdentifier = searchPath.ElementAt(1);

            // Loop through the properties of the entity if is structured
            if (elementDefinition is IEdmStructuredType structuredType)
            {
                foreach (var property in structuredType.DeclaredProperties)
                {
                    if (property.Name.Equals(searchIdentifier, StringComparison.OrdinalIgnoreCase))
                    {
                        elementDefinition = GetEdmElementType(property.Type.Definition);

                        // check if we need to search deeper to search the properties of a nested item
                        if (searchPath.Count > 2)
                        {
                            // get rid of the root item and do a deeper search
                            var subList = searchPath.Where(x => !x.Equals(searchPath.First(), StringComparison.OrdinalIgnoreCase)).ToList();
                            return SearchForEdmType(property.Type.Definition, subList);
                        }
                        else
                        {
                            return (elementDefinition, property.PropertyKind == EdmPropertyKind.Navigation);
                        }
                    }
                }

                // search in derived types
                if (derivedTypes is object)
                {
                    foreach (var derivedType in derivedTypes)
                    {
                        var (result, isNavigationProperty) = SearchForEdmType(derivedType, searchPath);
                        if (result != null)
                        {
                            return (result, isNavigationProperty);
                        }
                    }
                }

                // check properties of the base type as it may be inherited
                return SearchForEdmType(structuredType.BaseType, searchPath, searchingParent: true);
            }

            // search failed so return null type
            return (null, false);
        }

        /// <summary>
        /// Helper function to check if <see cref="IEdmType"/> is a collection and return the element type>
        /// </summary>
        /// <param name="edmType">Type to be checked</param>
        private static IEdmType GetEdmElementType(IEdmType edmType)
        {
            if (edmType is IEdmCollectionType innerCollection)
            {
                return innerCollection.ElementType.Definition;
            }

            //just return the same value
            return edmType;
        }

        /// <summary>
        /// Helper function to make check and ensure that a variable name is not a reserved keyword for the language in use.
        /// If it is reserved, return an appropiate transformation of the variale name
        /// </summary>
        /// <param name="variableName">variable name to check for uniqueness</param>
        /// <param name="languageExpressions">Language expressions that holds list of reserved words for filtering</param>
        /// <returns>Modified variable name that is not a keyword</returns>
        public static string EnsureVariableNameIsNotReserved(string variableName, LanguageExpressions languageExpressions)
        {
            if (languageExpressions.ReservedNames.Contains(variableName))
            {
                return languageExpressions.ReservedNameEscapeSequence + variableName;//append the language specific escape sequence
            }

            return variableName;
        }

        /// <summary>
        /// Helper function to make the first character of a string to be capitalized
        /// </summary>
        /// <param name="s">Input string to modified</param>
        /// <returns>Modified string</returns>
        public static string UppercaseFirstLetter(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            var a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        /// <summary>
        /// Helper function to make the first character of a string to be small letter
        /// </summary>
        /// <param name="s">Input string to modified</param>
        /// <returns>Modified string</returns>
        public static string LowerCaseFirstLetter(string s)
        {
            return s.ToFirstCharacterLowerCase();
        }
        /// <summary>
        /// This is a language agnostic function that looks at a operationSegment and returns a list of parameters with their names and values needed by the operation.
        /// If the method is a post, the parameters are sought for in the request body. Otherwise they are sort for in the request url
        /// </summary>
        /// <param name="operationSegment">OData OperationSegment representing a Function or action</param>
        /// <param name="snippetModel">Snippet Model to obtain useful data from</param>
        /// <param name="collectionSuffix">Suffix to be added to elements that are proved to be members collections</param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<string, string>> GetParameterListFromOperationSegmentWithNames(OperationSegment operationSegment, SnippetModel snippetModel, bool returnEnumTypeIfEnum, string collectionSuffix = "")
        {
            var parametersProvided = new List<string>();
            if (!string.IsNullOrEmpty(snippetModel.RequestBody))
            {
                var jsonObject = JsonSerializer.Deserialize<JsonElement>(snippetModel.RequestBody, JsonHelper.JsonSerializerOptions);
                if (jsonObject.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in jsonObject.EnumerateObject())
                    {
                        parametersProvided.Add(property.Name);
                    }
                }
            }

            if (snippetModel.Method == HttpMethod.Post)
            {
                //use the order from the metadata
                var parameters = operationSegment
                    .Operations
                    .First()
                    .Parameters
                    .Skip(1) // the first parameter is always the binding one
                    .ToList();
                return AddValidParameterItemsFromIEdmOperationParameterList(new List<string>(), parameters, parametersProvided, collectionSuffix)
                .Select((x, idx) => new KeyValuePair<string, string>(parameters[idx].Name, x));
            }
            else
                return operationSegment.Parameters.Select(x => new KeyValuePair<string, string>(x.Name, GetParameterValueFromOperationUrlSegement(x, returnEnumTypeIfEnum)));

        }
        /// <summary>
        /// This is a language agnostic function that looks at a operationSegment and returns a list of parameters needed by the operation.
        /// If the method is a post, the parameters are sought for in the request body. Otherwise they are sort for in the request url
        /// </summary>
        /// <param name="operationSegment">OData OperationSegment representing a Function or action</param>
        /// <param name="snippetModel">Snippet Model to obtain useful data from</param>
        /// <param name="collectionSuffix">Suffix to be added to elements that are proved to be members collections</param>
        /// <param name="isOrderedByOptionalParameters">Flag to show whether the parameters are ordered by the the metadata or optionality of params</param>
        /// <returns></returns>
        public static IEnumerable<string> GetParameterListFromOperationSegment(
            OperationSegment operationSegment, SnippetModel snippetModel, string collectionSuffix = "",
            bool isOrderedByOptionalParameters = true, bool returnEnumTypeIfEnum = false)
        {
            var paramList = new List<string>();

            if (snippetModel.Method == HttpMethod.Post)
            {
                //read parameters from request body since this is an odata action
                var parametersProvided = new List<string>();
                if (!string.IsNullOrEmpty(snippetModel.RequestBody))
                {
                    var jsonObject = JsonSerializer.Deserialize<JsonElement>(snippetModel.RequestBody, JsonHelper.JsonSerializerOptions);
                    if (jsonObject.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in jsonObject.EnumerateObject())
                        {
                            parametersProvided.Add(property.Name);
                        }
                    }
                }

                if (isOrderedByOptionalParameters)
                {
                    //first populate the required parameters
                    var requiredParameters = operationSegment.Operations.First().Parameters.Where(param => !param.Type.IsNullable);
                    paramList = AddValidParameterItemsFromIEdmOperationParameterList(paramList, requiredParameters, parametersProvided, collectionSuffix);
                    //populate the parameters the optional parameters we have from the request
                    var optionalParameters = operationSegment.Operations.First().Parameters.Where(param => param.Type.IsNullable);
                    paramList = AddValidParameterItemsFromIEdmOperationParameterList(paramList, optionalParameters, parametersProvided, collectionSuffix);
                }
                else
                {
                    //use the order from the metadata
                    var parameters = operationSegment.Operations.First().Parameters;
                    paramList = AddValidParameterItemsFromIEdmOperationParameterList(paramList, parameters, parametersProvided, collectionSuffix);
                }
            }
            else
            {
                //read parameters from url since this is an odata function
                foreach (var parameter in operationSegment.Parameters)
                {
                    var value = GetParameterValueFromOperationUrlSegement(parameter, returnEnumTypeIfEnum);
                    if (!string.IsNullOrEmpty(value))
                        paramList.Add(value);
                }
            }

            return paramList;
        }
        /// <summary>
        /// Gets the value of an operation from the URL segments when available
        /// </summary>
        /// <param name="parameter">Parameter to look for</param>
        /// <returns>Value provided by the HTTP snippet</returns>
        private static string GetParameterValueFromOperationUrlSegement(OperationSegmentParameter parameter, bool returnEnumTypeIfEnum)
        {
            switch (parameter.Value)
            {
                case ConvertNode convertNode when convertNode.Source is ConstantNode cNode:
                    return $"\"{cNode.Value}\"";
                case ConstantNode constantNode when constantNode.TypeReference.Definition.TypeKind == EdmTypeKind.Enum && returnEnumTypeIfEnum:
                    return $"{constantNode.TypeReference.Definition.FullTypeName()}{constantNode.LiteralText}";
                case ConstantNode constantNode:
                    return constantNode.LiteralText;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Helper function to add parameters from list of operationSegment parameters. This is validated from a list of parameters given
        /// from the request to prevent unnecessary population of optional params.
        /// </summary>
        /// <param name="initialParameterList">Initial list of parameters that we need to add parameters to</param>
        /// <param name="edmOperationParameterList">List of parameters for the operation segment</param>
        /// <param name="parametersProvided">List of parameters that are present in the request</param>
        /// <param name="collectionSuffix">Suffix to add in collection parameters</param>
        /// <returns></returns>
        private static List<string> AddValidParameterItemsFromIEdmOperationParameterList(
            List<string> initialParameterList,
            IEnumerable<IEdmOperationParameter> edmOperationParameterList,
            List<string> parametersProvided,
            string collectionSuffix)
        {
            foreach (var parameter in edmOperationParameterList)
            {
                //check if the parameter is from a function/action that is bound. If it is we don't add
                //the first parameter of a bound function which is essentially the bind parameter
                if (parameter.DeclaringOperation.IsBound &&
                    parameter.DeclaringOperation.Parameters.First().Equals(parameter))
                    continue;

                //if we actually have been given the parameter before we can add it.
                if (parametersProvided.Contains(parameter.Name, StringComparer.OrdinalIgnoreCase))
                {
                    initialParameterList.Add(parameter.Type.Definition is IEdmCollectionType
                        ? $"{LowerCaseFirstLetter(parameter.Name)}{collectionSuffix}"
                        : LowerCaseFirstLetter(parameter.Name));
                }
                else
                {
                    //add null as the parameter is not provided/nullable.
                    initialParameterList.Add("null");
                }
            }
            return initialParameterList;
        }
    }
}
