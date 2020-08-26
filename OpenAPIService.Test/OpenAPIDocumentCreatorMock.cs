// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OData.Edm.Csdl;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.OData;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace OpenAPIService.Test
{
    /// <summary>
    /// Defines a Mock class that simulates creating an OpenAPI document.
    /// </summary>
    public static class OpenAPIDocumentCreatorMock
    {
        private static readonly ConcurrentDictionary<string, OpenApiDocument> _OpenApiDocuments = new ConcurrentDictionary<string, OpenApiDocument>();

        /// <summary>
        /// Get OpenApiDocument version of Microsoft Graph based on CSDL document
        /// </summary>
        /// <param name="graphDocPath">The uri of the Microsoft Graph metadata doc.</param>
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
        /// <returns></returns>
        private static OpenApiDocument CreateOpenApiDocument(string graphDocPath)
        {
            if (string.IsNullOrEmpty(graphDocPath))
            {
                return null;
            }

            using StreamReader streamReader = new StreamReader(graphDocPath);
            Stream csdl = streamReader.BaseStream;

            var edmModel = CsdlReader.Parse(XElement.Load(csdl).CreateReader());

            var settings = new OpenApiConvertSettings()
            {
                EnableKeyAsSegment = true,
                EnableOperationId = true,
                PrefixEntityTypeNameBeforeKey = true,
                TagDepth = 2,
                EnablePagination = true,
                EnableDiscriminatorValue = false,
                EnableDerivedTypesReferencesForRequestBody = false,
                EnableDerivedTypesReferencesForResponses = false,
                ShowRootPath = true,
                ShowLinks = true
            };
            OpenApiDocument document = edmModel.ConvertToOpenApi(settings);

            document = FixReferences(document);

            return document;
        }

        /// <summary>
        /// This method is only needed because the output of ConvertToOpenApi isn't quite a valid OpenApiDocument instance.
        /// So we write it out, and read it back in again to fix it up
        /// </summary>
        /// <param name="document">The OpenAPI document to be actioned.</param>
        /// <returns></returns>
        private static OpenApiDocument FixReferences(OpenApiDocument document)
        {
            var sb = new StringBuilder();
            document.SerializeAsV3(new OpenApiYamlWriter(new StringWriter(sb)));
            var doc = new OpenApiStringReader().Read(sb.ToString(), out _);

            return doc;
        }
    }
}
