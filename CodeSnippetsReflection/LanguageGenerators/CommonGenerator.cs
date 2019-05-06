using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeSnippetsReflection.LanguageGenerators
{
    public static class CommonGenerator
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
                if (key.ToLower().Equals("host"))
                    continue;
                //append the header to the snippet
                snippetBuilder.Append(string.Format(languageExpressions.HeaderExpression, key, value.First()));
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
        public static IEdmType GetEdmTypeFromIdentifier(ODataPathSegment oDataPathSegment, ICollection<string> path )
        {

            IEdmType returnValue;

            switch (oDataPathSegment)
            {
                case KeySegment _:
                case EntitySetSegment _:
                case NavigationPropertySegment _:
                case NavigationPropertyLinkSegment _:
                case SingletonSegment _:
                case PropertySegment _:
                case ValueSegment _:

                    returnValue = SearchForEdmType(oDataPathSegment.EdmType, path);

                    if (null != returnValue)
                        return returnValue;

                    break;
                    
                case OperationSegment operationSegment:
                    foreach (var parameters in operationSegment.Operations.First().Parameters)
                    {
                        if (!parameters.Name.Equals(path.FirstOrDefault(),StringComparison.OrdinalIgnoreCase))
                            continue;

                        returnValue = SearchForEdmType(parameters.Type.Definition, path);

                        if (null != returnValue)
                            return returnValue;
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
        private static IEdmType SearchForEdmType(IEdmType definition, ICollection<string> searchPath)
        {
            //if the type is a collection, use the type of the element of the collection
            var elementDefinition = GetEdmElementType(definition);

            //we are at the root of the search so just return the definition of the root item
            if (searchPath.Count <= 1)
            {
                return elementDefinition;
            }

            //the second element in the path is the property we are searching for
            var searchIdentifier = searchPath.ElementAt(1);
            
            //Loop through the properties of the entity if is structured
            if (elementDefinition is IEdmStructuredType structuredType)
            {
                foreach (var property in structuredType.DeclaredProperties)
                {
                    if (property.Name.Equals(searchIdentifier))
                    {
                        elementDefinition = GetEdmElementType(property.Type.Definition);
                        
                        //check if we need to search deeper to search the properties of a nested item
                        if (searchPath.Count > 2)
                        {
                            //get rid of the root item and do a deeper search
                            var subList = searchPath.Where(x => !x.Equals(searchPath.First())).ToList();
                            return SearchForEdmType(property.Type.Definition, subList);
                        }
                        else
                        {
                            return elementDefinition;
                        }
                    }
                }

                //check properties of the base type as it may be inherited
                return SearchForEdmType(structuredType.BaseType, searchPath);
            }

            //search failed so return empty string
            return null;
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
        public static string EnsureVariableNameIsNotReserved(string variableName , LanguageExpressions languageExpressions)
        {
            if (languageExpressions.ReservedNames.Contains(variableName))
            {
                return "_" + variableName;//append an underscore
            }

            return variableName;
        }
    }
}
