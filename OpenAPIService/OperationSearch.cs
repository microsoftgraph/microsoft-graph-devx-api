using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using System;
using System.Collections.Generic;

namespace OpenAPIService
{
    public class OperationSearch : OpenApiVisitorBase
    {
        private readonly Func<OpenApiOperation, bool> _predicate;

        private List<SearchResult> _searchResults = new List<SearchResult>();

        public IList<SearchResult> SearchResults { get { return _searchResults; } }

        public OperationSearch(Func<OpenApiOperation, bool> predicate)
        {
            this._predicate = predicate;
        }

        public override void Visit(OpenApiOperation operation)
        {
            if (_predicate(operation))
            {
                _searchResults.Add(new SearchResult()
                {
                    Operation = operation,
                    CurrentKeys = CopyCurrentKeys(CurrentKeys)
                });
            }
        }

        /// <summary>
        /// Visits the list of <see cref="OpenApiParameter>"/>
        /// </summary>
        /// <param name="parameters"></param>
        public override void Visit(IList<OpenApiParameter> parameters)
        {
            /* The Parameter.Explode property should be true
             * if Parameter.Style == Form; but OData query params
             * as used in Microsoft Graph implement explode: false
             * ex: $select=id,displayName,givenName
             */
            foreach (var parameter in parameters.Where(x => x.Style == ParameterStyle.Form))
            {
                 parameter.Explode = false;
            }

            base.Visit(parameters);
        }

        private CurrentKeys CopyCurrentKeys(CurrentKeys currentKeys)
        {
            var keys = new CurrentKeys
            {
                Path = currentKeys.Path,
                Operation = currentKeys.Operation
            };

            return keys;
        }
    }

}
