// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.OData;
using Microsoft.OpenApi.Services;
using OpenAPIService.Common;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace OpenAPIService.Interfaces
{
    public interface IOpenApiService
    {
        OpenApiDocument CreateFilteredDocument(OpenApiDocument source, string title, string graphVersion, Func<OpenApiOperation, bool> predicate);

        Func<OpenApiOperation, bool> CreatePredicate(string operationIds, string tags, string url,
            OpenApiDocument source, string graphVersion = "v1.0", bool forceRefresh = false);

        MemoryStream SerializeOpenApiDocument(OpenApiDocument subset, OpenApiStyleOptions styleOptions);

        Task<OpenApiDocument> GetGraphOpenApiDocumentAsync(string graphUri, OpenApiStyle openApiStyle, bool forceRefresh);

        OpenApiUrlTreeNode CreateOpenApiUrlTreeNode(ConcurrentDictionary<string, OpenApiDocument> sources);

        void ConvertOpenApiUrlTreeNodeToJson(OpenApiUrlTreeNode rootNode, Stream stream);

        OpenApiDocument ApplyStyle(OpenApiStyle style, OpenApiDocument subsetOpenApiDocument, bool includeRequestBody = false);

        OpenApiConvertSettings GetOpenApiConvertSettings(OpenApiStyle style = OpenApiStyle.Plain);
    }
}
