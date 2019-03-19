using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeSnippetsReflection.LanguageGenerators
{
    public static class CommonGenerator
    {
        public static string GetListAsStringForSnippet(List<string> fieldList, string delimiter)
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

        public static string GenerateQuerySection(SnippetModel snippetModel, LanguageExpressions languageExpressions)
        {
            var snippetBuilder = new StringBuilder();
            //Append any filter queries
            if (snippetModel.FilterFieldList.Any())
            {
                var filterResult = GetListAsStringForSnippet(snippetModel.FilterFieldList, ",");
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
                var expandResult = GetListAsStringForSnippet(snippetModel.ExpandFieldList, ",");
                //append the expand result to the snippet
                snippetBuilder.Append(string.Format(languageExpressions.ExpandExpression, expandResult));
            }

            //Append any select queries
            if (snippetModel.SelectFieldList.Any())
            {
                var selectResult = GetListAsStringForSnippet(snippetModel.SelectFieldList, ",");
                //append the select result to the snippet
                snippetBuilder.Append(string.Format(languageExpressions.SelectExpression, selectResult));
            }

            //Append any orderby queries
            if (snippetModel.OrderByFieldList.Any())
            {
                var orderByResult = GetListAsStringForSnippet(snippetModel.OrderByFieldList, " ");
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
    }
}
