using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace CodeSnippetsReflection.LanguageGenerators
{
    public class CommonGenerator
    {
        /// <summary>
        /// Edm model of metadata
        /// </summary>
        private readonly IEdmModel Model;

        /// <summary>
        /// Common Generator constructor
        /// </summary>
        /// <param name="model">Edm model of metadata</param>
        public CommonGenerator(IEdmModel model)
        {
            Model = model;
        }

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
                var valueString = value.First().Replace("\"", languageExpressions.DoubleQuotesEscapeSequence);
                snippetBuilder.Append(string.Format(languageExpressions.HeaderExpression, key, valueString));
            }
            //Append any filter queries
            if (snippetModel.FilterFieldList.Any())
            {
                var filterResult = GetListAsStringForSnippet(snippetModel.FilterFieldList, languageExpressions.FilterExpressionDelimiter);
                //append the filter to the snippet
                snippetBuilder.Append(string.Format(languageExpressions.FilterExpression, filterResult));
            }

            //Append any search queries
            if (!string.IsNullOrEmpty(snippetModel.SearchExpression))
            {
                snippetBuilder.Append(string.Format(languageExpressions.SearchExpression, snippetModel.SearchExpression));
            }

            //Append the expand section
            if (!string.IsNullOrEmpty(snippetModel.ExpandFieldExpression))
            {
                //append the expand result to the snippet
                snippetBuilder.Append(string.Format(languageExpressions.ExpandExpression, snippetModel.ExpandFieldExpression));
            }

            //Append any select queries
            if (snippetModel.SelectFieldList.Any())
            {
                var selectResult = GetListAsStringForSnippet(snippetModel.SelectFieldList, languageExpressions.SelectExpressionDelimiter);
                //append the select result to the snippet
                snippetBuilder.Append(string.Format(languageExpressions.SelectExpression, selectResult));
            }

            //Append any orderby queries
            if (snippetModel.OrderByFieldList.Any())
            {
                var orderByResult = GetListAsStringForSnippet(snippetModel.OrderByFieldList, languageExpressions.OrderByExpressionDelimiter);
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
                snippetBuilder.Append(string.Format(languageExpressions.SkipTokenExpression, snippetModel.ODataUri.SkipToken));
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
                derivedTypes = Model?.FindAllDerivedTypes(structuredTypeDefinition);
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
        /// Helper function to join string list into one string delimited with a desired character
        /// </summary>
        /// <param name="fieldList">List of strings that are to be concatenated to a string </param>
        /// <param name="delimiter">Delimiter to be used to join the string elements</param>
        public static string GetListAsStringForSnippet(IEnumerable<string> fieldList, string delimiter)
        {
            var result = new StringBuilder();
            foreach (var queryOption in fieldList)
            {
                result.Append(queryOption + delimiter);
            }
            if (!string.IsNullOrEmpty(delimiter) && !string.IsNullOrEmpty(result.ToString()))
            {
                result.Remove(result.Length - delimiter.Length, delimiter.Length);
            }

            return result.ToString();

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
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            var a = s.ToCharArray();
            a[0] = char.ToLower(a[0]);
            return new string(a);
        }
        /// <summary>
        /// This is a language agnostic function that looks at a operationSegment and returns a list of parameters with their names and values needed by the operation.
        /// If the method is a post, the parameters are sought for in the request body. Otherwise they are sort for in the request url
        /// </summary>
        /// <param name="operationSegment">OData OperationSegment representing a Function or action</param>
        /// <param name="snippetModel">Snippet Model to obtain useful data from</param>
        /// <param name="collectionSuffix">Suffix to be added to elements that are proved to be members collections</param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<string, string>> GetParameterListFromOperationSegmentWithNames(OperationSegment operationSegment, SnippetModel snippetModel, string collectionSuffix = "")
        {
            var parametersProvided = new List<string>();
            if (!string.IsNullOrEmpty(snippetModel.RequestBody)
                && JsonConvert.DeserializeObject(snippetModel.RequestBody) is JObject testObj)
            {
                foreach (var (key, _) in testObj)
                {
                    parametersProvided.Add(key);
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
                return operationSegment.Parameters.Select(x => new KeyValuePair<string, string>(x.Name, GetParameterValueFromOperationUrlSegement(x)));

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
        public static IEnumerable<string> GetParameterListFromOperationSegment(OperationSegment operationSegment, SnippetModel snippetModel, string collectionSuffix = "", bool isOrderedByOptionalParameters = true)
        {
            var paramList = new List<string>();

            if (snippetModel.Method == HttpMethod.Post)
            {
                //read parameters from request body since this is an odata action
                var parametersProvided = new List<string>();
                if (!string.IsNullOrEmpty(snippetModel.RequestBody)
                    && JsonConvert.DeserializeObject(snippetModel.RequestBody) is JObject testObj)
                {
                    foreach (var (key, _) in testObj)
                    {
                        parametersProvided.Add(key);
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
                    var value = GetParameterValueFromOperationUrlSegement(parameter);
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
        private static string GetParameterValueFromOperationUrlSegement(OperationSegmentParameter parameter)
        {
            switch (parameter.Value)
            {
                case ConvertNode convertNode when convertNode.Source is ConstantNode cNode:
                    return $"\"{cNode.Value}\"";
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
