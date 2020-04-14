// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using System.Linq;

namespace OpenAPIService
{
    /// <summary>
    /// Provides OpenApi visitor methods that traverse an OpenAPI document and clears the OpenApiResponse and OpenApiRequestBody Content property values.
    /// </summary>
    internal class ContentRemover: OpenApiVisitorBase
    {       
        public override void Visit(OpenApiResponse response)
        {            
            if (response.Content.Any())
            {
                response.Content.Clear();
                base.Visit(response);
            }
        }

        public override void Visit(OpenApiRequestBody requestBody)
        {
            if (requestBody.Content.Any())
            {
               requestBody.Content.Clear();
               base.Visit(requestBody);
            }         
        }
    }
}
