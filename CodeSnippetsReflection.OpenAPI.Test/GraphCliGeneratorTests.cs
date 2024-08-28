using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.Test;
using Microsoft.OpenApi.Services;
using Microsoft.OpenApi.Readers;
using Xunit;
using System.Net.Http.Headers;
using System;
using Microsoft.OpenApi.Models;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators;

public class GraphCliGeneratorTests : OpenApiSnippetGeneratorTestBase
{
    private readonly GraphCliGenerator _generator = new();
    
    public static IEnumerable<object[]> GetSnippetData()
    {
        return new[] {
            new object[] {HttpMethod.Delete, $"{ServiceRootUrl}/users/100/settings", "mgc users settings delete --user-id {user-id}"},
            new object[] {HttpMethod.Get, $"{ServiceRootUrl}/users", "mgc users list"},
            new object[] {HttpMethod.Get, $"{ServiceRootUrl}/users/100", "mgc users get --user-id {user-id}"},
            new object[] {HttpMethod.Patch, $"{ServiceRootUrl}/users/100/licenseDetails/123", "mgc users license-details patch --user-id {user-id} --license-details-id {licenseDetails-id}"},
            new object[] {HttpMethod.Post, $"{ServiceRootUrl}/users/100/extensions", "mgc users extensions create --user-id {user-id}"},
            new object[] {HttpMethod.Put, $"{ServiceRootUrl}/users/100/manager/$ref", "mgc users manager ref put --user-id {user-id}"},
        };
    }

    [Theory]
    [MemberData(nameof(GetSnippetData))]
    public async Task GeneratesSnippetsForCommandsAsync(HttpMethod method, string url, string expectedCommand)
    {
        // Given a url and method
        using var requestPayload = new HttpRequestMessage(method, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + expectedCommand, result);
    }

    [Fact]
    public async Task GeneratesSnippetsForDeepSubCommandAsync()
    {
        // GET /users/{user-id}/todo/lists/{todoTaskList-id}/tasks/{todoTask-id}/attachments/{attachmentBase-id}
        // Given
        string url = $"{ServiceRootUrl}/users/100/todo/lists/123/tasks/1234/attachments/12345";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc users todo lists tasks attachments get --user-id {user-id} --todo-task-list-id {todoTaskList-id} --todo-task-id {todoTask-id} --attachment-base-id {attachmentBase-id}", result);
    }

    [Theory]
    [InlineData("/users/100/directReports/graph.orgContact", "mgc users direct-reports graph-org-contact get --user-id {user-id}")]
    [InlineData("/users/100/directReports/123/graph.orgContact", "mgc users direct-reports graph-org-contact-by-id get --user-id {user-id} --directory-object-id {directoryObject-id}")]
    public async Task GeneratesSnippetsForConflictingIndexerNavSubCommandAsync(string url, string expectedCommand)
    {
        // Tests:
        // GET /users/{user-id}/directReports/graph.orgContact
        // GET /users/{user-id}/directReports/{directoryObject-id}/graph.orgContact
        // Given
        string schema = """{"openapi":"3.0.0","info":{"title":"Tests API","version":"1.0.11"},"servers":[{"url":"https://example.com/api/v1.0"}],"paths":{"/users/{user-id}/directReports/graph.orgContact":{"get":{"responses":{"200":{"description":"Successful operation"}}}},"/users/{user-id}/directReports/{directoryObject-id}/graph.orgContact":{"get":{"responses":{"200":{"description":"Successful operation"}}}}}}""";
        var doc = new OpenApiStringReader().Read(schema, out _);
        var rootNode = OpenApiUrlTreeNode.Create(doc, "default");
        var openApiMetadata = new OpenApiSnippetMetadata(rootNode, new Dictionary<string, OpenApiSchema>());
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}{url}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, openApiMetadata);
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + expectedCommand, result);
    }

    // Powershell metadata doesn't have /$count endpoints
    [Fact]
    public async Task GeneratesSnippetsForCountCommandAsync()
    {
        // Given
        string schema = """{"openapi":"3.0.0","info":{"title":"Tests API","version":"1.0.11"},"servers":[{"url":"https://example.com/api/v1.0"}],"paths":{"/tests/$count":{"get":{"operationId":"getTestResults","responses":{"200":{"description":"Successful operation"}}}}}}""";
        string url = $"{ServiceRootUrl}/tests/$count";
        var doc = new OpenApiStringReader().Read(schema, out _);
        var rootNode = OpenApiUrlTreeNode.Create(doc, "default");
        var openApiMetadata = new OpenApiSnippetMetadata(rootNode, new Dictionary<string, OpenApiSchema>());
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, openApiMetadata);
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc tests count get", result);
    }

    [Fact]
    public async Task GeneratesSnippetsForBetaCommandAsync()
    {
        // Given
        string url = $"{ServiceRootBetaUrl}/users";
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestMessage, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestMessage);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        // TODO: Check snippet generation support after beta releases.
        Assert.Equal(Environment.NewLine + "mgc-beta users list", result);
    }

    [Fact]
    public async Task GeneratesSnippetsForRefCommandAsync()
    {
        // GET /users/{user-id}/manager/$ref
        // Given
        string url = $"{ServiceRootUrl}/users/100/manager/$ref";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc users manager ref get --user-id {user-id}", result);
    }

    [Fact]
    public async Task GeneratesSnippetsForContentCommandAsync()
    {
        // GET /users/{user-id}/photo/$value
        // Given
        string url = $"{ServiceRootUrl}/users/100/photo/$value";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc users photo content get --user-id {user-id}", result);
    }

    [Fact]
    public async Task GeneratesSnippetsForGetListCommandAsync()
    {
        // Given
        string url = $"{ServiceRootUrl}/users";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc users list", result);
    }

    [Theory]
    [InlineData("?id=10", "--id {id} --id-query 10")]
    [InlineData("?id=", "--id {id}")] // When the query parameter value is an empty string, the snippet generator ignores it
    [InlineData("?id", "--id {id}")] // When the query parameter value is not provided, the snippet generator ignores it
    [InlineData(null, "--id {id}")]
    [InlineData("? ", "--id {id}")]
    public async Task GeneratesSnippetsForCommandWithConflictingParameterNameAsync(string queryString, string commandSuffix)
    {
        // Given
        string schema = """{"openapi":"3.0.0","info":{"title":"Tests API","version":"1.0.11"},"servers":[{"url":"https://example.com/api/v1.0"}],"paths":{"/tests/{id}/results":{"get":{"operationId":"getTestResults","parameters":[{"name":"id","in":"path","required":true,"schema":{"type":"integer"}},{"name":"id","in":"query","schema":{"type":"integer"}}],"responses":{"200":{"description":"Successful operation"}}}}}}""";
        var doc = new OpenApiStringReader().Read(schema, out _);
        var rootNode = OpenApiUrlTreeNode.Create(doc, "default");
        var openApiMetadata = new OpenApiSnippetMetadata(rootNode, new Dictionary<string, OpenApiSchema>());
        string url = $"{ServiceRootUrl}/tests/1/results{queryString}";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, openApiMetadata);
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        // TODO: What should happen to the query parameter?
        Assert.Equal(Environment.NewLine + $"mgc tests results get {commandSuffix}", result);
    }

    [Fact]
    public async Task GeneratesSnippetsForCommandWithConflictingParameterNameAndNoLocationAsync()
    {
        // Given
        // This open api doc is not valid since there's a parameter that has no location
        // defined. That parameter will be skipped
        string schema = """{"openapi":"3.0.0","info":{"title":"Tests API","version":"1.0.11"},"servers":[{"url":"https://example.com/api/v1.0"}],"paths":{"/tests/{id}/results":{"get":{"operationId":"getTestResults","parameters":[{"name":"id","in":"path","required":true,"schema":{"type":"integer"}},{"name":"id","in":"","schema":{"type":"integer"}}],"responses":{"200":{"description":"Successful operation"}}}}}}""";
        var doc = new OpenApiStringReader().Read(schema, out _);
        var rootNode = OpenApiUrlTreeNode.Create(doc, "default");
        var openApiMetadata = new OpenApiSnippetMetadata(rootNode, new Dictionary<string, OpenApiSchema>());
        string url = $"{ServiceRootUrl}/tests/1/results";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, openApiMetadata);
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        // TODO: What should happen to the query parameter?
        Assert.Equal(Environment.NewLine + "mgc tests results get --id {id}", result);
    }

    [Fact]
    public async Task GeneratesSnippetsForCommandWithConflictingPathAndCookieParameterNameAsync()
    {
        // Given
        // The cookie parameter will be skipped. not supported in the CLI
        string schema = """{"openapi":"3.0.0","info":{"title":"Tests API","version":"1.0.11"},"servers":[{"url":"https://example.com/api/v1.0"}],"paths":{"/tests/{id}/results":{"get":{"operationId":"getTestResults","parameters":[{"name":"id","in":"path","required":true,"schema":{"type":"integer"}},{"name":"id","in":"cookie","schema":{"type":"integer"}}],"responses":{"200":{"description":"Successful operation"}}}}}}""";
        var doc = new OpenApiStringReader().Read(schema, out _);
        var rootNode = OpenApiUrlTreeNode.Create(doc, "default");
        var openApiMetadata = new OpenApiSnippetMetadata(rootNode, new Dictionary<string, OpenApiSchema>());
        string url = $"{ServiceRootUrl}/tests/1/results";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, openApiMetadata);
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc tests results get --id {id}", result);
    }

    [Fact]
    public async Task GeneratesSnippetsForCommandWithConflictingPathAndHeaderParameterNameAsync()
    {
        // Given
        // The cookie parameter will be skipped. not supported in the CLI
        string schema = """{"openapi":"3.0.0","info":{"title":"Tests API","version":"1.0.11"},"servers":[{"url":"https://example.com/api/v1.0"}],"paths":{"/tests/{id}/results":{"get":{"operationId":"getTestResults","parameters":[{"name":"id","in":"path","required":true,"schema":{"type":"integer"}},{"name":"id","in":"header","schema":{"type":"integer"}}],"responses":{"200":{"description":"Successful operation"}}}}}}""";
        var doc = new OpenApiStringReader().Read(schema, out _);
        var rootNode = OpenApiUrlTreeNode.Create(doc, "default");
        var openApiMetadata = new OpenApiSnippetMetadata(rootNode, new Dictionary<string, OpenApiSchema>());
        string url = $"{ServiceRootUrl}/tests/1/results";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        requestPayload.Headers.Add("id", "test-header");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, openApiMetadata);
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc tests results get --id {id} --id-header test-header", result);
    }

    [Theory]
    [InlineData("?$select=name", " --select \"name\"")]
    [InlineData("?$filter=test&$select=name", " --filter \"test\" --select \"name\"")]
    public async Task GeneratesSnippetsForCommandWithODataParametersAsync(string queryString, string commandOptions)
    {
        // Given
        string url = $"{ServiceRootUrl}/users{queryString}";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + $"mgc users list{commandOptions}", result);
    }

    [Fact]
    public async Task GeneratesSnippetsForCommandWithEmptyParameterNamesAsync()
    {
        // Given
        string schema = """{"openapi":"3.0.0","info":{"title":"Tests API","version":"1.0.11"},"servers":[{"url":"https://example.com/api/v1.0"}],"paths":{"/tests":{"get":{"operationId":"getTestResults","parameters":[{"name":"","in":"query","schema":{"type":"integer"}}],"responses":{"200":{"description":"Successful operation"}}}}}}""";
        var doc = new OpenApiStringReader().Read(schema, out _);
        var rootNode = OpenApiUrlTreeNode.Create(doc, "default");
        var openApiMetadata = new OpenApiSnippetMetadata(rootNode, new Dictionary<string, OpenApiSchema>());
        string url = $"{ServiceRootUrl}/tests?";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, openApiMetadata);
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        // TODO: What should happen to the query parameter?
        Assert.Equal(Environment.NewLine + "mgc tests get", result);
    }

    [Fact]
    public async Task GeneratesSnippetsForCommandWithQueryParametersAsync()
    {
        // Given
        string schema = """{"openapi":"3.0.0","info":{"title":"Tests API","version":"1.0.11"},"servers":[{"url":"https://example.com/api/v1.0"}],"paths":{"/tests":{"get":{"operationId":"getTestResults","parameters":[{"name":"id","in":"query","required":false,"schema":{"type":"integer"}}],"responses":{"200":{"description":"Successful operation"}}}}}}""";
        var doc = new OpenApiStringReader().Read(schema, out _);
        var rootNode = OpenApiUrlTreeNode.Create(doc, "default");
        var openApiMetadata = new OpenApiSnippetMetadata(rootNode, new Dictionary<string, OpenApiSchema>());
        string url = $"{ServiceRootUrl}/tests?id=10";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, openApiMetadata);
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        // TODO: What should happen to the query parameter?
        Assert.Equal(Environment.NewLine + "mgc tests get --id \"10\"", result);
    }

    [Theory]
    [InlineData("Test content")]
    [InlineData("Test content", "text/plain;charset=UTF-8")]
    [InlineData("Test content", "application/json;a=b;c=d")]
    [InlineData("Test content", "application/octet-stream;a=b;c=d", "mgc users create --file <file path>")]
    [InlineData("""{"key": 10}""", "application/json;a=b;c=d", """mgc users create --body '{"key": 10}'""")]
    public async Task GeneratesSnippetsForCreateInListCommandWithBodyAsync(string content, string contentType = "text/plain", string expectedCommand = "mgc users create --body 'Test content'")
    {
        // Given
        string url = $"{ServiceRootUrl}/users";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, url);
        requestPayload.Content = new StringContent(content);
        requestPayload.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + expectedCommand, result);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("undefined")]
    [InlineData(" ", "text/plain;charset=UTF-8")]
    [InlineData(null, "application/json;a=b;c=d")]
    [InlineData(" ", "application/octet-stream;a=b;c=d")]
    [InlineData("Test content", null, true)]
    public async Task GeneratesSnippetsForCreateInListCommandWithInvalidContentAsync(string content, string contentType = null, bool removeContentType = false)
    {
        // Given
        string url = $"{ServiceRootUrl}/users";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, url);
        if (content != null)
        {
            requestPayload.Content = new StringContent(content);
            var success = MediaTypeHeaderValue.TryParse(contentType, out var customContentType);
            if (contentType == null && removeContentType)
            {
                requestPayload.Content.Headers.ContentType = null;
            }
            else if (success && requestPayload.Content?.Headers?.ContentType?.MediaType != customContentType?.MediaType
                        && requestPayload.Content?.Headers?.ContentType?.CharSet != customContentType?.CharSet)
            {
                requestPayload.Content.Headers.ContentType = customContentType;
            }
        }

        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc users create", result);
    }

    [Fact]
    public void ReturnsEmptyStringForNullSnippetModel()
    {
        // Given a null snippet model
        // When
        var result = _generator.GenerateCodeSnippet(null);

        // Then
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task ThrowsExceptionOnUnsupportedApiVersionAsync()
    {
        // Given
        string rootUrl = "https://example.com/v303";
        string url = $"{rootUrl}/users";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, rootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        // Then
        Assert.Throws<ArgumentException>(() => _generator.GenerateCodeSnippet(snippetModel));
    }
    
    [Fact]
    public async Task GeneratesEscapedSnippetsForMultilineCommandAsync()
    {
        // Given
        string url = $"{ServiceRootUrl}/users";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, url);
        requestPayload.Content = new StringContent("{\n  \"name\": \"test\"\n}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc users create --body '{\\\n  \"name\": \"test\"\\\n}'", result);
    }

   [Fact]
    public async Task GeneratesEscapedBetaSnippetsForMultilineCommandAsync()
    {
        // Given
        string url = $"{ServiceRootBetaUrl}/users";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, url);
        requestPayload.Content = new StringContent("{\n  \"name\": \"test\"\n}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc-beta users create --body '{\\\n  \"name\": \"test\"\\\n}'", result);
    }
    [Fact]
    public async Task GeneratesSnippetsContainingOverLoadedBoundFunctionsWithSingleParameterAsync()
    {
        // Given
        string url = $"{ServiceRootUrl}/drives/driveid/items/driveitemid/delta(token='token')";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc drives items delta-with-token get --token {token-id} --drive-id {drive-id} --drive-item-id {driveItem-id}", result);
    }

    [Fact]
    public async Task GeneratesSnippetsContainingOverLoadedBoundFunctionsWithMultipleParametersAsync()
    {
        // Given
        string url = $"{ServiceRootUrl}/drives/driveid/items/driveitemid/getActivitiesByInterval(startDateTime='startdatetime',endDateTime='enddatetime',interval='interval')";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc drives items get-activities-by-interval-with-start-date-time-with-end-date-time-with-interval get --start-date-time {start-date-time-id} --end-date-time {end-date-time-id} --interval {interval-id}  --drive-id {drive-id} --drive-item-id {driveItem-id}", result);
    }

    [Fact]
    public async Task GeneratesSnippetsContainingUnBoundedFunctionsAsync()
    {
        // Given
        string url = $"{ServiceRootUrl}/identity/identityProviders/availableProviderTypes()";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc identity identity-providers available-provider-types get", result);
    }

    [Fact]
    public async Task GeneratesSnippetsWithSlashMeEndpointsAsync()
    {
        // Given
        string url = $"{ServiceRootUrl}/me/calendar/events?$filter=startsWith%28subject%2C%27All%27%29";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc users calendar events list --user-id {user-id} --filter \"startsWith(subject,'All')\"", result);
    }
    [Fact]
    public async Task GeneratesSnippetsWithExpandQueryOptionsAsync()
    {
        // Given
        string url = $"{ServiceRootUrl}/me/messages/XXXX?$expand=singleValueExtendedProperties%28$filter%3Did%20eq%20%27XXXX%27%29";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc users messages get --user-id {user-id} --message-id {message-id} --expand \"singleValueExtendedProperties(\\$filter=id eq 'XXXX')\"", result);
    }
    [Fact]
    public async Task GeneratesSnippetsWithFilterQueryOptionsAsync()
    {
        // Given
        string url = $"{ServiceRootUrl}/identityGovernance/accessReviews/definitions?$filter=contains%28scope%2Fmicrosoft.graph.accessReviewQueryScope%2Fquery%2C%20%27.%2Fmembers%27%29";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc identity-governance access-reviews definitions list --filter \"contains(scope/microsoft.graph.accessReviewQueryScope/query, './members')\"", result);
    }

    [Fact]
    public async Task GeneratesSnippetsForHttpSnippetsWithUrlEncodedValuesForSystemQueryOptionParametersAsync()
    {
        // Given
        string url = $"{ServiceRootUrl}/teams/XXXXXXX/members?$filter=%28microsoft.graph.aadUserConversationMember%2FdisplayName%2520eq%2520%27Harry%2520Johnson%27%2520or%2520microsoft.graph.aadUserConversationMember%2Femail%2520eq%2520%27admin%40M365x987948.OnMicrosoft.com%27%29";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal(Environment.NewLine + "mgc teams members list --team-id {team-id} --filter \"(microsoft.graph.aadUserConversationMember/displayName%20eq%20'Harry%20Johnson'%20or%20microsoft.graph.aadUserConversationMember/email%20eq%20'admin@M365x987948.OnMicrosoft.com')\"", result);
    }
}
