# microsoft-graph-explorer-api

## Snippet Generator
This API provides a means of getting a code snippet on how to perform a certain action on Microsoft Graph.

The API takes in the HTTP request message to be sent to Microsoft Graph and the programming language to return a response with the code snippet string.

The API currently supports the generation of snippets to the Microsoft Graph API in the following languages

- C#
- Javascript
- Objective C
- Java

### Example Snippets

- [C#](c-sharp-examples.md)
- [Javascript](javascript-examples.md)
- [Objective C](objective-c-examples.md)
- [Java](java-examples.md)


## Samples
This API serves up the list of sample queries. 
Workload teams are able to add, update or delete the sample queries with the right permissions.
The sample queries are also queryable by their *category name*, *human name* or *tip*.

## Permissions
This API serves up a list of scopes and their information, ordered from the least to most permissive, when given a request url. 


## Samples Admin
This is the administrative endpoint of the API that is used by authorized PMs to assign permissions to workload teams to be able to perform write, update and delete operations on the sample queries.

## How to use The endpoints
This API makes use of Swagger UI to expose its endpoints for end user consumption.
Some Protected resources are accessed only after a successful OAuth2 authentication with Azure AD

### Register an application with Azure AD
1. Navigate to the Azure portal [app registrations](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps) page and click on **New registration**. 
1. Provide a suitable name, e.g. *GE-API-SwaggerUI*. Under **Redirect URI**, select *Web* and paste this value: *https://graphexplorerapi.azurewebsites.net/swagger/oauth2-redirect.html*. Select **Register**
1. In the **Overview** blade, copy the Application (client) ID and paste it somewhere you can easily get to. You will use it during authorization on Swagger UI.
1. Navigate to: https://graphexplorerapi.azurewebsites.net/swagger/index.html

## License

Copyright (c) Microsoft Corporation. All Rights Reserved. Licensed under the MIT [license](LICENSE).