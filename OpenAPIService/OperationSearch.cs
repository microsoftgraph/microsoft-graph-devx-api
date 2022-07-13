// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenAPIService
{
    public class OperationSearch : OpenApiVisitorBase
    {
        private readonly Func<OpenApiOperation, bool> _predicate;
        private readonly List<SearchResult> _searchResults = new();

        public IList<SearchResult> SearchResults => _searchResults;

        public OperationSearch(Func<OpenApiOperation, bool> predicate)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public override void Visit(OpenApiPathItem pathItem)
        {
            foreach (var item in pathItem.Operations)
            {
                var operation = item.Value;
                if (_predicate(operation))
                {
                    // Remove the operation description.
                    // This is temporary until some of the invalid/incorrect texts coming from the CSDL are fixed.
                    operation.Description = null;
                    _searchResults.Add(new SearchResult()
                    {
                        Operation = operation,
                        Parameters = pathItem.Parameters,
                        CurrentKeys = CopyCurrentKeys(CurrentKeys),
                    });
                }
            }
        }

        /// <summary>
        /// Visits <see cref="OpenApiOperation"/>.
        /// </summary>
        /// <param name="operation">The target <see cref="OpenApiOperation"/>.</param>
        //public override void Visit(OpenApiOperation operation)
        //{
        //    if (_predicate(operation))
        //    {
        //        // Remove the operation description.
        //        // This is temporary until some of the invalid/incorrect texts coming from the CSDL are fixed.
        //        operation.Description = null;
        //        _searchResults.Add(new SearchResult()
        //        {
        //            Operation = operation,
        //            CurrentKeys = CopyCurrentKeys(CurrentKeys)
        //        });
        //    }
        //}

        /// <summary>
        /// Visits list of <see cref="OpenApiParameter"/>.
        /// </summary>
        /// <param name="parameters">The target list of <see cref="OpenApiParameter"/>.</param>
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
