// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OpenAPIService.Test
{
    /// <summary>
    /// Mock class that creates a sample OpenAPI document.
    /// </summary>
    public static class OpenAPIDocumentCreatorMock
    {
        private static readonly ConcurrentDictionary<string, OpenApiDocument> _OpenApiDocuments = new ConcurrentDictionary<string, OpenApiDocument>();

        /// <summary>
        /// Gets an OpenAPI document of Microsoft Graph
        /// from a dictionary cache or gets a new instance.
        /// </summary>
        /// <param name="key">The key for the OpenAPI document dictionary.</param>
        /// <param name="forceRefresh">Whether to reload the OpenAPI document from source.</param>
        /// <returns>Instance of an OpenApiDocument</returns>
        public static OpenApiDocument GetGraphOpenApiDocument(string key, bool forceRefresh)
        {
            if (!forceRefresh && _OpenApiDocuments.TryGetValue(key, out OpenApiDocument doc))
            {
                return doc;
            }

            OpenApiDocument source = CreateOpenApiDocument();
            _OpenApiDocuments[key] = source;
            return source;
        }

        /// <summary>
        /// Creates an OpenAPI document.
        /// </summary>
        /// <returns>Instance of an OpenApi document</returns>
        private static OpenApiDocument CreateOpenApiDocument()
        {
            string applicationJsonMediaType = "application/json";

            OpenApiDocument document = new OpenApiDocument()
            {
                Paths = new OpenApiPaths()
                {
                    ["/"] = new OpenApiPathItem() // root path
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            {
                                OperationType.Get, new OpenApiOperation
                                {
                                    OperationId = "graphService.GetGraphService",
                                    Responses = new OpenApiResponses()
                                    {
                                        {
                                            "200",new OpenApiResponse()
                                            {
                                                Description = "OK"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    ["/reports/microsoft.graph.getTeamsUserActivityCounts(period={period})"] = new OpenApiPathItem()
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            {
                                OperationType.Get, new OpenApiOperation
                                {
                                    Tags = new List<OpenApiTag>
                                    {
                                        {
                                            new OpenApiTag()
                                            {
                                                Name = "reports.Functions"
                                            }
                                        }
                                    },
                                    OperationId = "reports.getTeamsUserActivityCounts",
                                    Summary = "Invoke function getTeamsUserActivityUserCounts",
                                    Parameters = new List<OpenApiParameter>
                                    {
                                        {
                                            new OpenApiParameter()
                                            {
                                                Name = "period",
                                                In = ParameterLocation.Path,
                                                Required = true,
                                                Schema = new OpenApiSchema()
                                                {
                                                    Type = "string"
                                                }
                                            }
                                        }
                                    },
                                    Responses = new OpenApiResponses()
                                    {
                                        {
                                            "200", new OpenApiResponse()
                                            {
                                                Description = "Success",
                                                Content = new Dictionary<string, OpenApiMediaType>
                                                {
                                                    {
                                                        applicationJsonMediaType,
                                                        new OpenApiMediaType
                                                        {
                                                            Schema = new OpenApiSchema
                                                            {
                                                                Type = "array"
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    ["/reports/microsoft.graph.getTeamsUserActivityUserDetail(date={date})"] = new OpenApiPathItem()
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            {
                                OperationType.Get, new OpenApiOperation
                                {
                                    Tags = new List<OpenApiTag>
                                    {
                                        {
                                            new OpenApiTag()
                                            {
                                                Name = "reports.Functions"
                                            }
                                        }
                                    },
                                    OperationId = "reports.getTeamsUserActivityUserDetail-a3f1",
                                    Summary = "Invoke function getTeamsUserActivityUserDetail",
                                    Parameters = new List<OpenApiParameter>
                                    {
                                        {
                                            new OpenApiParameter()
                                            {
                                                Name = "period",
                                                In = ParameterLocation.Path,
                                                Required = true,
                                                Schema = new OpenApiSchema()
                                                {
                                                    Type = "string"
                                                }
                                            }
                                        }
                                    },
                                    Responses = new OpenApiResponses()
                                    {
                                        {
                                            "200", new OpenApiResponse()
                                            {
                                                Description = "Success",
                                                Content = new Dictionary<string, OpenApiMediaType>
                                                {
                                                    {
                                                        applicationJsonMediaType,
                                                        new OpenApiMediaType
                                                        {
                                                            Schema = new OpenApiSchema
                                                            {
                                                                Type = "array"
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    ["/users"] = new OpenApiPathItem()
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            {
                                OperationType.Get, new OpenApiOperation
                                {
                                    Tags = new List<OpenApiTag>
                                    {
                                        {
                                            new OpenApiTag()
                                            {
                                                Name = "users.user"
                                            }
                                        }
                                    },
                                    OperationId = "users.user.ListUser",
                                    Summary = "Get entities from users",
                                    Responses = new OpenApiResponses()
                                    {
                                        {
                                            "200", new OpenApiResponse()
                                            {
                                                Description = "Retrieved entities",
                                                Content = new Dictionary<string, OpenApiMediaType>
                                                {
                                                    {
                                                        applicationJsonMediaType,
                                                        new OpenApiMediaType
                                                        {
                                                            Schema = new OpenApiSchema
                                                            {
                                                                Title = "Collection of user",
                                                                Type = "object",
                                                                Properties = new Dictionary<string, OpenApiSchema>
                                                                {
                                                                    {
                                                                        "value",
                                                                        new OpenApiSchema
                                                                        {
                                                                            Type = "array",
                                                                            Items = new OpenApiSchema
                                                                            {
                                                                                Reference = new OpenApiReference
                                                                                {
                                                                                    Type = ReferenceType.Schema,
                                                                                    Id = "microsoft.graph.user"
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    ["/users/{user-id}"] = new OpenApiPathItem()
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            {
                                OperationType.Get, new OpenApiOperation
                                {
                                    Tags = new List<OpenApiTag>
                                    {
                                        {
                                            new OpenApiTag()
                                            {
                                                Name = "users.user"
                                            }
                                        }
                                    },
                                    OperationId = "users.user.GetUser",
                                    Summary = "Get entity from users by key",
                                    Responses = new OpenApiResponses()
                                    {
                                        {
                                            "200", new OpenApiResponse()
                                            {
                                                Description = "Retrieved entity",
                                                Content = new Dictionary<string, OpenApiMediaType>
                                                {
                                                    {
                                                        applicationJsonMediaType,
                                                        new OpenApiMediaType
                                                        {
                                                            Schema = new OpenApiSchema
                                                            {
                                                                Reference = new OpenApiReference
                                                                {
                                                                    Type = ReferenceType.Schema,
                                                                    Id = "microsoft.graph.user"
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            {
                                OperationType.Patch, new OpenApiOperation
                                {
                                    Tags = new List<OpenApiTag>
                                    {
                                        {
                                            new OpenApiTag()
                                            {
                                                Name = "users.user"
                                            }
                                        }
                                    },
                                    OperationId = "users.user.UpdateUser",
                                    Summary = "Update entity in users",
                                    Responses = new OpenApiResponses()
                                    {
                                        {
                                            "204", new OpenApiResponse()
                                            {
                                                Description = "Success"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    ["/administrativeUnits/{administrativeUnit-id}/microsoft.graph.restore"] = new OpenApiPathItem()
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            {
                                OperationType.Post, new OpenApiOperation
                                {
                                    Tags = new List<OpenApiTag>
                                    {
                                        {
                                            new OpenApiTag()
                                            {
                                                Name = "administrativeUnits.Actions"
                                            }
                                        }
                                    },
                                    OperationId = "administrativeUnits.restore",
                                    Summary = "Invoke action restore",
                                    Parameters = new List<OpenApiParameter>
                                    {
                                        {
                                            new OpenApiParameter()
                                            {
                                                Name = "administrativeUnit-id",
                                                In = ParameterLocation.Path,
                                                Required = true,
                                                Description = "key: id of administrativeUnit",
                                                Schema = new OpenApiSchema()
                                                {
                                                    Type = "string"
                                                }
                                            }
                                        }
                                    },
                                    Responses = new OpenApiResponses()
                                    {
                                        {
                                            "200", new OpenApiResponse()
                                            {
                                                Description = "Success",
                                                Content = new Dictionary<string, OpenApiMediaType>
                                                {
                                                    {
                                                        applicationJsonMediaType,
                                                        new OpenApiMediaType
                                                        {
                                                            Schema = new OpenApiSchema
                                                            {
                                                                AnyOf = new List<OpenApiSchema>
                                                                {
                                                                    new OpenApiSchema
                                                                    {
                                                                        Type = "string"
                                                                    }
                                                                },
                                                                Nullable = true
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    ["/applications/{application-id}/logo"] = new OpenApiPathItem()
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            {
                                OperationType.Put, new OpenApiOperation
                                {
                                    Tags = new List<OpenApiTag>
                                    {
                                        {
                                            new OpenApiTag()
                                            {
                                                Name = "applications.application"
                                            }
                                        }
                                    },
                                    OperationId = "applications.application.UpdateLogo",
                                    Summary = "Update media content for application in applications",
                                    Responses = new OpenApiResponses()
                                    {
                                        {
                                            "204", new OpenApiResponse()
                                            {
                                                Description = "Success"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return document;
        }
    }
}
