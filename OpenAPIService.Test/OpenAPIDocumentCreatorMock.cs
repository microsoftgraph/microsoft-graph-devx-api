// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using OpenAPIService.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OpenAPIService.Test
{
    /// <summary>
    /// Mock class that creates a sample OpenAPI document.
    /// </summary>
    public class OpenApiDocumentCreatorMock
    {
        private static readonly ConcurrentDictionary<string, OpenApiDocument> _OpenApiDocuments = new();
        private readonly IOpenApiService _openApiService;
        private const string NullValueError = "Value cannot be null";

        public OpenApiDocumentCreatorMock(IOpenApiService openApiService)
        {
            _openApiService = openApiService
                ?? throw new ArgumentNullException(nameof(openApiService), $"{ NullValueError }: { nameof(openApiService) }");
        }

        /// <summary>
        /// Gets an OpenAPI document of Microsoft Graph
        /// from a dictionary cache or gets a new instance.
        /// </summary>
        /// <param name="key">The key for the OpenAPI document dictionary.</param>
        /// <param name="forceRefresh">Whether to reload the OpenAPI document from source.</param>
        /// <returns>Instance of an OpenApiDocument</returns>
        public OpenApiDocument GetGraphOpenApiDocument(string key, bool forceRefresh)
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
        private OpenApiDocument CreateOpenApiDocument()
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
                                    },
                                    Extensions = new Dictionary<string, IOpenApiExtension>
                                    {
                                        {
                                            "x-ms-docs-operation-type", new OpenApiString("function")
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
                                    },
                                    Extensions = new Dictionary<string, IOpenApiExtension>
                                    {
                                        {
                                            "x-ms-docs-operation-type", new OpenApiString("function")
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
                    ["/users/{user-id}/messages/{message-id}"] = new OpenApiPathItem()
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
                                                Name = "users.message"
                                            }
                                        }
                                    },
                                    OperationId = "users.GetMessages",
                                    Summary = "Get messages from users",
                                    Description = "The messages in a mailbox or folder. Read-only. Nullable.",
                                    Parameters = new List<OpenApiParameter>
                                    {
                                        new OpenApiParameter()
                                        {
                                            Name = "$select",
                                            In = ParameterLocation.Query,
                                            Required = true,
                                            Description = "Select properties to be returned",
                                            Schema = new OpenApiSchema()
                                            {
                                                Type = "array"
                                            }
                                            // missing explode parameter
                                        }
                                    },
                                    Responses = new OpenApiResponses()
                                    {
                                        {
                                            "200", new OpenApiResponse()
                                            {
                                                Description = "Retrieved navigation property",
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
                                                                    Id = "microsoft.graph.message"
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
                                    RequestBody = new OpenApiRequestBody()
                                    {
                                        Description = "Invoke action restore",
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
                    },
                    ["/security/hostSecurityProfiles"] = new OpenApiPathItem()
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
                                                Name = "security.hostSecurityProfile"
                                            }
                                        }
                                    },
                                    OperationId = "security.ListHostSecurityProfiles",
                                    Summary = "Get hostSecurityProfiles from security",
                                    Responses = new OpenApiResponses()
                                    {
                                        {
                                            "200", new OpenApiResponse()
                                            {
                                                Description = "Retrieved navigation property",
                                                Content = new Dictionary<string, OpenApiMediaType>
                                                {
                                                    {
                                                        applicationJsonMediaType,
                                                        new OpenApiMediaType
                                                        {
                                                            Schema = new OpenApiSchema
                                                            {
                                                                Title = "Collection of hostSecurityProfile",
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
                                                                                    Id = "microsoft.graph.networkInterface"
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
                    ["/communications/calls/{call-id}/microsoft.graph.keepAlive"] = new OpenApiPathItem()
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
                                                Name = "communications.Actions"
                                            }
                                        }
                                    },
                                    OperationId = "communications.calls.call.keepAlive",
                                    Summary = "Invoke action keepAlive",
                                    Parameters = new List<OpenApiParameter>
                                    {
                                        new OpenApiParameter()
                                        {
                                            Name = "call-id",
                                            In = ParameterLocation.Path,
                                            Description = "key: id of call",
                                            Required = true,
                                            Schema = new OpenApiSchema()
                                            {
                                                Type = "string"
                                            },
                                            Extensions = new Dictionary<string, IOpenApiExtension>
                                            {
                                                {
                                                    "x-ms-docs-key-type", new OpenApiString("call")
                                                }
                                            }
                                        }
                                    },
                                    Responses = new OpenApiResponses()
                                    {
                                        {
                                            "204", new OpenApiResponse()
                                            {
                                                Description = "Success"
                                            }
                                        }
                                    },
                                    Extensions = new Dictionary<string, IOpenApiExtension>
                                    {
                                        {
                                            "x-ms-docs-operation-type", new OpenApiString("action")
                                        }
                                    }
                                }
                            }
                        }
                    },
                    ["/groups/{group-id}/events/{event-id}/calendar/events/microsoft.graph.delta"] = new OpenApiPathItem()
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            {
                                OperationType.Get, new OpenApiOperation
                                {
                                    Tags = new List<OpenApiTag>
                                    {
                                        new OpenApiTag()
                                        {
                                            Name = "groups.Functions"
                                        }
                                    },
                                    OperationId = "groups.group.events.event.calendar.events.delta",
                                    Summary = "Invoke function delta",
                                    Parameters = new List<OpenApiParameter>
                                    {
                                        new OpenApiParameter()
                                        {
                                            Name = "group-id",
                                            In = ParameterLocation.Path,
                                            Description = "key: id of group",
                                            Required = true,
                                            Schema = new OpenApiSchema()
                                            {
                                                Type = "string"
                                            },
                                            Extensions = new Dictionary<string, IOpenApiExtension>
                                            {
                                                {
                                                    "x-ms-docs-key-type", new OpenApiString("group")
                                                }
                                            }
                                        },
                                        new OpenApiParameter()
                                        {
                                            Name = "event-id",
                                            In = ParameterLocation.Path,
                                            Description = "key: id of event",
                                            Required = true,
                                            Schema = new OpenApiSchema()
                                            {
                                                Type = "string"
                                            },
                                            Extensions = new Dictionary<string, IOpenApiExtension>
                                            {
                                                {
                                                    "x-ms-docs-key-type", new OpenApiString("event")
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
                                                                Type = "array",
                                                                Reference = new OpenApiReference
                                                                {
                                                                    Type = ReferenceType.Schema,
                                                                    Id = "microsoft.graph.event"
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    },
                                    Extensions = new Dictionary<string, IOpenApiExtension>
                                    {
                                        {
                                            "x-ms-docs-operation-type", new OpenApiString("function")
                                        }
                                    }
                                }
                            }
                        }
                    },
                    ["/reports/microsoft.graph.getSharePointSiteUsageDetail(period={period})"] = new OpenApiPathItem()
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            {
                                OperationType.Get, new OpenApiOperation
                                {
                                    Tags = new List<OpenApiTag>
                                    {
                                        new OpenApiTag()
                                        {
                                            Name = "reports.Functions"
                                        }
                                    },
                                    OperationId = "reports.getSharePointSiteUsageDetail-204b",
                                    Summary = "Invoke function getSharePointSiteUsageDetail",
                                    Parameters = new List<OpenApiParameter>
                                    {
                                        new OpenApiParameter()
                                        {
                                            Name = "period",
                                            In = ParameterLocation.Path,
                                            Description = "Usage: period={period}",
                                            Required = true,
                                            Schema = new OpenApiSchema()
                                            {
                                                Type = "string"
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
                                                                Reference = new OpenApiReference
                                                                {
                                                                    Type = ReferenceType.Schema,
                                                                    Id = "microsoft.graph.report"
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    },
                                    Extensions = new Dictionary<string, IOpenApiExtension>
                                    {
                                        {
                                            "x-ms-docs-operation-type", new OpenApiString("function")
                                        }
                                    }
                                }
                            }
                        }
                    },
                    ["/deviceManagement/microsoft.graph.getRoleScopeTagsByIds(ids={ids})"] = new OpenApiPathItem()
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            {
                                OperationType.Get, new OpenApiOperation
                                {
                                    Tags = new List<OpenApiTag>
                                    {
                                        new OpenApiTag()
                                        {
                                            Name = "deviceManagement.Functions"
                                        }
                                    },
                                    OperationId = "deviceManagement.getRoleScopeTagsByIds",
                                    Summary = "Invoke function getRoleScopeTagsByIds",
                                    Parameters = new List<OpenApiParameter>
                                    {
                                        new OpenApiParameter()
                                        {
                                            Name = "ids",
                                            In = ParameterLocation.Query,
                                            Description = "Usage: ids={ids}",
                                            Required = true,
                                            Content = new Dictionary<string, OpenApiMediaType>
                                            {
                                                {
                                                    applicationJsonMediaType,
                                                    new OpenApiMediaType
                                                    {
                                                        Schema = new OpenApiSchema
                                                        {
                                                            Type = "array",
                                                            Items = new OpenApiSchema
                                                            {
                                                                Type = "string"
                                                            }
                                                        }
                                                    }
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
                                                                Title = "Collection of deviceManagement",
                                                                Type = "object",
                                                                Properties = new Dictionary<string, OpenApiSchema>
                                                                {
                                                                    {
                                                                        "value", new OpenApiSchema
                                                                        {
                                                                            Type = "array",
                                                                            Items = new OpenApiSchema
                                                                            {
                                                                                AnyOf = new List<OpenApiSchema>
                                                                                {
                                                                                    new OpenApiSchema
                                                                                    {
                                                                                        Reference = new OpenApiReference
                                                                                        {
                                                                                            Type = ReferenceType.Schema,
                                                                                            Id = "microsoft.graph.roleScopeTag"
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
                                    Extensions = new Dictionary<string, IOpenApiExtension>
                                    {
                                        {
                                            "x-ms-docs-operation-type", new OpenApiString("function")
                                        }
                                    }
                                }
                            }
                        }
                    },
                    ["/applications/{application-id}/createdOnBehalfOf/$ref"] = new OpenApiPathItem()
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            {
                                OperationType.Get, new OpenApiOperation
                                {
                                    Tags = new List<OpenApiTag>
                                    {
                                        new OpenApiTag()
                                        {
                                            Name = "applications.directoryObject"
                                        }
                                    },
                                    OperationId = "applications.GetRefCreatedOnBehalfOf",
                                    Summary = "Get ref of createdOnBehalfOf from applications"
                                }
                            }
                        }
                    }
                },
                Components = new OpenApiComponents
                {
                    Schemas = new Dictionary<string, OpenApiSchema>
                    {
                        {
                            "microsoft.graph.networkInterface", new OpenApiSchema
                            {
                                Title = "networkInterface",
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    {
                                        "description", new OpenApiSchema
                                        {
                                            Type = "string",
                                            Description = "Description of the NIC (e.g. Ethernet adapter, Wireless LAN adapter Local Area Connection <#>, etc.).",
                                            Nullable = true
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return _openApiService.FixReferences(document);
        }
    }
}
