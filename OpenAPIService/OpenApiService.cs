using Microsoft.OData.Edm.Csdl;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;
using Microsoft.OpenApi.Writers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Threading.Tasks;
using Microsoft.OpenApi.OData;
using System.Collections.Concurrent;

namespace OpenAPIService
{
    public enum OpenApiStyle
    {
        PowerShell,
        PowerPlatform,
        Plain
    }

    public class OpenApiService
    {
        static ConcurrentDictionary<Uri, OpenApiDocument> _OpenApiDocuments = new ConcurrentDictionary<Uri, OpenApiDocument>();

        /// <summary>
        /// Create partial document based on provided predicate
        /// </summary>
        public static OpenApiDocument CreateFilteredDocument(OpenApiDocument source, string title, string graphVersion, Func<OpenApiOperation, bool> predicate)
        {

            var subset = new OpenApiDocument
            {
                Info = new OpenApiInfo()
                {
                    Title = title,
                    Version = graphVersion
                },

                Components = new OpenApiComponents()
            };
            var aadv2Scheme = new OpenApiSecurityScheme()
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows()
                {
                    AuthorizationCode = new OpenApiOAuthFlow()
                    {
                        AuthorizationUrl = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/authorize"),
                        TokenUrl = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/token")
                    }
                },
                Reference = new OpenApiReference() { Id = "azureaadv2", Type = ReferenceType.SecurityScheme },
                UnresolvedReference = false
            };
            subset.Components.SecuritySchemes.Add("azureaadv2", aadv2Scheme);

            subset.SecurityRequirements.Add(new OpenApiSecurityRequirement() { { aadv2Scheme, new string[] { } } });
            
            subset.Servers.Add(new OpenApiServer() { Description = "Core", Url = $"https://graph.microsoft.com/{graphVersion}/" });

            var operationObjects = new List<OpenApiOperation>();
            var results = FindOperations(source, predicate);
            foreach (var result in results)
            {
                OpenApiPathItem pathItem = null;
                if (subset.Paths == null)
                {
                    subset.Paths = new OpenApiPaths();
                    pathItem = new OpenApiPathItem();
                    subset.Paths.Add(result.CurrentKeys.Path, pathItem);
                }
                else
                {
                    if (!subset.Paths.TryGetValue(result.CurrentKeys.Path, out pathItem))
                    {
                        pathItem = new OpenApiPathItem();
                        subset.Paths.Add(result.CurrentKeys.Path, pathItem);
                    }
                }

                pathItem.Operations.Add((OperationType)result.CurrentKeys.Operation, result.Operation);
            }

            OpenApiService.CopyReferences(subset);

            return subset;
        }

        /// <summary>
        /// Create predicate function based on passed query parameters
        /// </summary>
        /// <param name="operationIds">comma delimited list of operationIds or * for all operations</param>
        /// <param name="tags">comma delimited list of tags or a single regex</param>
        /// <returns></returns>
        public static Func<OpenApiOperation, bool> CreatePredicate(string operationIds, string tags)
        {
            if (operationIds != null && tags != null)
            {                
                return null; // Cannot filter by OperationIds and Tags at the same time
            }

            Func<OpenApiOperation, bool> predicate;
            if (operationIds != null)
            {
                if (operationIds == "*")
                {
                    predicate = (o) => true;  // All operations
                }
                else
                {
                    var operationIdsArray = operationIds.Split(',');
                    predicate = (o) => operationIdsArray.Contains(o.OperationId);
                }
            }
            else if (tags != null)
            {
                var tagsArray = tags.Split(',');
                if (tagsArray.Length == 1)
                {
                    var regex = new Regex(tagsArray[0]);
                    
                    predicate = (o) => o.Tags.Any(t => regex.IsMatch(t.Name));
                } else
                {
                    predicate = (o) => o.Tags.Any(t => tagsArray.Contains(t.Name));
                }
            }
            else
            {
                predicate = null;
            }

            return predicate;
        }

        /// <summary>
        /// Create a representation of the OpenApiDocument to return from an API
        /// </summary>
        /// <param name="subset">OpenAPI document</param>
        /// <param name="openApiVersion"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static MemoryStream SerializeOpenApiDocument(OpenApiDocument subset, string openApiVersion, string format)
        {
            var stream = new MemoryStream();
            var sr = new StreamWriter(stream);
            OpenApiWriterBase writer;
            if (format == "yaml")
            {
                writer = new OpenApiYamlWriter(sr);
            }
            else
            {
                writer = new OpenApiJsonWriter(sr);
            }

            if (openApiVersion == "2")
            {
                subset.SerializeAsV2(writer);
            }
            else
            {
                subset.SerializeAsV3(writer);
            }
            sr.Flush();
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Get OpenApiDocument version of Microsoft Graph based on CSDL document 
        /// </summary>
        /// <param name="graphVersion">Version of Microsoft Graph</param>
        /// <param name="forceRefresh">Don't read from in-memory cache</param>
        /// <returns>Instance of an OpenApiDocument</returns>
        public static async Task<OpenApiDocument> GetGraphOpenApiDocument(string graphVersion, bool forceRefresh)
        {
            var csdlHref = new Uri($"https://graph.microsoft.com/{graphVersion}/$metadata");
            if (!forceRefresh && _OpenApiDocuments.TryGetValue(csdlHref, out OpenApiDocument doc))
            {
                return doc;
            }

            OpenApiDocument source = await CreateOpenApiDocument(csdlHref, forceRefresh);
            _OpenApiDocuments[csdlHref] = source;
            return source;
        }

        /// <summary>
        /// Update the OpenAPI document based on the style option
        /// </summary>
        /// <param name="style"></param>
        /// <param name="subsetOpenApiDocument"></param>
        /// <returns></returns>
        public static OpenApiDocument ApplyStyle(OpenApiStyle style, OpenApiDocument subsetOpenApiDocument)
        {
            if (style == OpenApiStyle.Plain)
            {
                return subsetOpenApiDocument;
            }

            /* For Powershell and PowerPlatform Styles */

            // Clone doc before making changes
            subsetOpenApiDocument = Clone(subsetOpenApiDocument);

            var anyOfRemover = new AnyOfRemover();
            var walker = new OpenApiWalker(anyOfRemover);
            walker.Walk(subsetOpenApiDocument);
                        
            if (style == OpenApiStyle.PowerShell)
            {
                // Format the OperationId for Powershell cmdlet names generation 
                var operationIdFormatter = new OperationIdPowershellFormatter();
                walker = new OpenApiWalker(operationIdFormatter);
                walker.Walk(subsetOpenApiDocument);

                var version = subsetOpenApiDocument.Info.Version;
                if (!new Regex("v\\d\\.\\d").Match(version).Success)
                {
                    subsetOpenApiDocument.Info.Version = "v1.0-" + version;
                }
            }
                        
            return subsetOpenApiDocument;
        }

        private static OpenApiDocument Clone(OpenApiDocument subsetOpenApiDocument)
        {
            var stream = new MemoryStream();
            var writer = new OpenApiYamlWriter(new StreamWriter(stream));
            subsetOpenApiDocument.SerializeAsV3(writer);
            writer.Flush();
            stream.Position = 0;
            var reader = new OpenApiStreamReader();
            return reader.Read(stream, out OpenApiDiagnostic diag);
        }

        private static async Task<OpenApiDocument> CreateOpenApiDocument(Uri csdlHref, bool forceRefresh = false)
        {
            var httpClient = CreateHttpClient();

            Stream csdl = await httpClient.GetStreamAsync(csdlHref.OriginalString);
            var edmModel = CsdlReader.Parse(XElement.Load(csdl).CreateReader());

            var settings = new OpenApiConvertSettings() {
                 EnableKeyAsSegment = true,
                 EnableOperationId = true,
                 PrefixEntityTypeNameBeforeKey =true,
                 TagDepth = 2
                  
            };
            OpenApiDocument document = edmModel.ConvertToOpenApi(settings);

            document = FixReferences(document);

            return document;
        }
        
        private static OpenApiDocument FixReferences(OpenApiDocument document)
        {
            // This method is only needed because the output of ConvertToOpenApi isn't quite a valid OpenApiDocument instance.
            // So we write it out, and read it back in again to fix it up.
            var sb = new StringBuilder();
            document.SerializeAsV3(new OpenApiYamlWriter(new StringWriter(sb)));
            var doc = new OpenApiStringReader().Read(sb.ToString(), out var diag);
            return doc;
        }

        private static IList<SearchResult> FindOperations(OpenApiDocument graphOpenApi, Func<OpenApiOperation, bool> predicate)
        {
            var search = new OperationSearch(predicate);
            var walker = new OpenApiWalker(search);
            walker.Walk(graphOpenApi);
            return search.SearchResults;
        }

        private static HttpClient CreateHttpClient()
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var httpClient = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip
            });
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("graphslice", "1.0"));
            return httpClient;
        }

        private static void CopyReferences(OpenApiDocument target)
        {
            bool morestuff = false;
            do
            {
                var copy = new CopyReferences(target);
                var walker = new OpenApiWalker(copy);
                walker.Walk(target);

                morestuff = Add(copy.Components, target.Components);
                
            } while (morestuff);
        }

        private static bool Add(OpenApiComponents newComponents, OpenApiComponents target)
        {
            var moreStuff = false; 
            foreach (var item in newComponents.Schemas)
            {
                if (!target.Schemas.ContainsKey(item.Key))
                {
                    moreStuff = true;
                    target.Schemas.Add(item);

                }
            }

            foreach (var item in newComponents.Parameters)
            {
                if (!target.Parameters.ContainsKey(item.Key))
                {
                    moreStuff = true;
                    target.Parameters.Add(item);
                }
            }

            foreach (var item in newComponents.Responses)
            {
                if (!target.Responses.ContainsKey(item.Key))
                {
                    moreStuff = true;
                    target.Responses.Add(item);
                }
            }
            return moreStuff;
        }
   }
}
