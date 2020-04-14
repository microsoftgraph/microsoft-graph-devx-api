// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using System.Linq;

namespace OpenAPIService
{
    internal class ContentRemover: OpenApiVisitorBase
    {       
        public override void Visit(OpenApiResponse response)
        {            
            if (response.Content.Any() == true)
            {
                response.Content.Clear();
                base.Visit(response);
            }
        }

        public override void Visit(OpenApiRequestBody requestBody)
        {
            if (requestBody.Content.Any() == true)
            {
               requestBody.Content.Clear();
               base.Visit(requestBody);
            }         
        }
    }
}
