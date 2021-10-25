using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace OpenAPIService
{
    internal class PathSearch : OpenApiVisitorBase
    {
        private readonly Func<OpenApiPaths, bool> _predicate;
        private readonly List<SearchResult> _searchResults = new();

        public IList<SearchResult> SearchResults => _searchResults;

        public PathSearch(Func<OpenApiPaths, bool> predicate)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        /// <summary>
        /// Visits <see cref="OpenApiOperation"/>.
        /// </summary>
        /// <param name="paths">The target <see cref="OpenApiOperation"/>.</param>
        public override void Visit(OpenApiPaths paths)
        {
            if (_predicate(paths))
            {
                _searchResults.Add(new SearchResult()
                {
                    Paths = paths,
                    CurrentKeys = CopyCurrentKeys(CurrentKeys)
                });
            }
        }

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

        private static CurrentKeys CopyCurrentKeys(CurrentKeys currentKeys)
        {
            return new CurrentKeys
            {
                Path = currentKeys.Path,
                Operation = currentKeys.Operation
            };
        }
    }
}
