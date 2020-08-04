using Microsoft.OData.Edm.Csdl;
using Microsoft.OpenApi.Models;
// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.OData;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;
using OpenAPIService.Common;
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
        /// <summary>
        /// Creates an OpenAPI document from a CSDL document.
        /// </summary>
        /// <param name="uri">The file path of the CSDL document location.</param>
        /// <param name="styleOptions">Optional parameter that defines the style
        /// options to be used in formatting the OpenAPI document.</param>
        /// <returns></returns>
        public static OpenApiDocument CreateOpenApiDocument(string uri, OpenApiStyleOptions styleOptions = null)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return null;
            }

            using StreamReader streamReader = new StreamReader(uri);
            Stream csdl = streamReader.BaseStream;

            var edmModel = CsdlReader.Parse(XElement.Load(csdl).CreateReader());

            var settings = new OpenApiConvertSettings()
            {
                EnableKeyAsSegment = true,
                EnableOperationId = true,
                PrefixEntityTypeNameBeforeKey = true,
                TagDepth = 2,
                EnablePagination = styleOptions != null && styleOptions.EnablePagination,
                EnableDiscriminatorValue = styleOptions != null && styleOptions.EnableDiscriminatorValue,
                EnableDerivedTypesReferencesForRequestBody = styleOptions != null && styleOptions.EnableDerivedTypesReferencesForRequestBody,
                EnableDerivedTypesReferencesForResponses = styleOptions != null && styleOptions.EnableDerivedTypesReferencesForResponses,
                ShowRootPath = styleOptions != null && styleOptions.ShowRootPath
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
