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

            //Append any expand queries
            if (snippetModel.ExpandFieldList.Any())
            {
                var expandResult = GetListAsStringForSnippet(snippetModel.ExpandFieldList, languageExpressions.ExpandExpressionDelimiter);
                //append the expand result to the snippet
                snippetBuilder.Append(string.Format(languageExpressions.ExpandExpression, expandResult));
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

        public static string GetClassNameFromIdentifier(ODataPathSegment oDataPathSegment, ICollection<string> path )
        {
            switch (oDataPathSegment)
            {
                case NavigationPropertySegment navigationPropertySegment:
                    return GetClassNameFromEdmType(navigationPropertySegment.NavigationProperty.Type.Definition, path);

                case EntitySetSegment entitySetSegment:
                    var definition = entitySetSegment.EdmType as IEdmCollectionType;
                    return GetClassNameFromEdmType(definition, path);

                case OperationSegment operationSegment:
                    foreach (var parameters in operationSegment.Operations.First().Parameters)
                    {
                        if (parameters.Name.ToLower().Equals("bindingparameter") || parameters.Name.ToLower().Equals("bindparameter"))
                            continue;

                        var returnValue = GetClassNameFromEdmType(parameters.Type.Definition, path);

                        if (!string.IsNullOrEmpty(returnValue))
                            return returnValue;
                    }
                    break;
            }

            throw new Exception("No Class Found for Idenfier");
        }

        private static string GetClassNameFromEdmType(IEdmType definition, ICollection<string> searchPath)
        {
            //if the type is a collection, use the type of the element of the collection
            var elementDefinition = GetEdmElementType(definition);

            //we are at the root of the search so just return the definition of the root item
            if (searchPath.Count <= 1)
            {
                return elementDefinition.ToString();
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
                        if (searchPath.Count() > 2)
                        {
                            //get rid of the root item and do a deeper search
                            var subList = searchPath.Where(x => !x.Equals(searchPath.First())).ToList();
                            return GetClassNameFromEdmType(property.Type.Definition, subList);
                        }
                        else
                        {
                            return elementDefinition.ToString();
                        }
                    }
                }
            }
            
            return "";
        }

        private static IEdmType GetEdmElementType(IEdmType edmType)
        {
            if (edmType is IEdmCollectionType innerCollection)
            {
                return innerCollection.ElementType.Definition;
            }
            else
            {
                return edmType;
            }
        }


        /// <summary>
        /// Helper function to join string list into one string delimited with a desired character
        /// </summary>
        /// <param name="fieldList">List of strings that are to be concatenated to a string </param>
        /// <param name="delimiter">Delimiter to be used to join the string elements</param>
        private static string GetListAsStringForSnippet(IEnumerable<string> fieldList, string delimiter)
        {
            var result = new StringBuilder();
            foreach (var queryOption in fieldList)
            {
                result.Append(queryOption + delimiter);
            }
            if (!string.IsNullOrEmpty(delimiter))
            {
                result.Remove(result.Length - delimiter.Length, delimiter.Length);
            }

            return result.ToString();

        }
    }
}
