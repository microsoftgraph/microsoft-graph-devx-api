// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using System.Collections.Concurrent;
using System.IO;

namespace OpenAPIService.Test
{
    /// <summary>
    /// Defines a Mock class that creates an OpenAPI document from a CSDL file.
    /// </summary>
    public static class OpenAPIDocumentCreatorMock
    {
        private static readonly ConcurrentDictionary<string, OpenApiDocument> _OpenApiDocuments = new ConcurrentDictionary<string, OpenApiDocument>();

        /// <summary>
        /// Gets an OpenAPI document version of Microsoft Graph based on CSDL document
        /// from a dictionary cache or gets a new instance.
        /// </summary>
        /// <param name="graphDocPath">The file path of the Microsoft Graph metadata doc.</param>
        /// <param name="forceRefresh">Don't read from in-memory cache.</param>
        /// <returns>Instance of an OpenApiDocument</returns>
        public static OpenApiDocument GetGraphOpenApiDocument(string graphDocPath, bool forceRefresh)
        {
            if (!forceRefresh && _OpenApiDocuments.TryGetValue(graphDocPath, out OpenApiDocument doc))
            {
                return doc;
            }

            OpenApiDocument source = CreateOpenApiDocument(graphDocPath);
            _OpenApiDocuments[graphDocPath] = source;
            return source;
        }

        /// <summary>
        /// Creates an OpenAPI document from a CSDL document.
        /// </summary>
        /// <param name="graphDocPath">The file path of the CSDL document location.</param>
        /// <param name="styleOptions">Optional parameter that defines the style
        /// options to be used in formatting the OpenAPI document.</param>
        /// <returns>Instance of an OpenApiDocument</returns>
        private static OpenApiDocument CreateOpenApiDocument(string graphDocPath)
        {
            if (string.IsNullOrEmpty(graphDocPath))
            {
                return null;
            }

            using StreamReader streamReader = new StreamReader(graphDocPath);
            Stream csdl = streamReader.BaseStream;

            OpenApiDocument document = OpenApiService.ConvertCsdlToOpenApiAsync(csdl).GetAwaiter().GetResult();

            return document;
        }
    }
}
