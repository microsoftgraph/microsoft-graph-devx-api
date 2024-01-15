
using System.Collections.Generic;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test;

public class GetImportsTests : OpenApiSnippetGeneratorTestBase
{
    private readonly GetImports getImports;

    public GetImportsTests()
    {
        this.getImports = new GetImports();
    }

    [Fact]
    public void GenerateImportStatements_Snippet_ShouldContainExpectedOutputRequestBuilderImports()
    {
        string snippet = @"
            graph_client = GraphServiceClient(credentials, scopes)

            query_params = MessagesRequestBuilder.MessagesRequestBuilderGetQueryParameters(
                filter = ""importance eq 'high'"",
            )

            request_configuration = MessagesRequestBuilder.MessagesRequestBuilderGetRequestConfiguration(
                query_parameters = query_params,
            )

            result = await graph_client.me.messages.get(request_configuration = request_configuration)
        ";
        List<string> expected = new List<string> { "from msgraph import GraphServiceClient", "from msgraph.generated.users.item.messages.messages_request_builder import MessagesRequestBuilder" };

        string result = getImports.GenerateImportStatements(snippet);
        Assert.Contains(string.Join("\n", expected), result);
    }

    [Fact]
    public void GenerateImportStatements_Snippet_ShouldContainModelImports()
    {
        string snippetText = @"
            graph_client = GraphServiceClient(credentials, scopes)

            request_body = Event(
                subject = ""My event"",
                start = DateTimeTimeZone(
                    date_time = ""2023-11-22T11:17:13.547Z"",
                    time_zone = ""UTC"",
                ),
                end = DateTimeTimeZone(
                    date_time = ""2023-11-29T11:17:13.547Z"",
                    time_zone = ""UTC"",
                ),
            )

            result = await graph_client.me.events.post(request_body);
        ";

        
        List<string> expected = new List<string> { "from msgraph import GraphServiceClient", "from msgraph.generated.models.event import Event", "from msgraph.generated.models.date_time_time_zone import DateTimeTimeZone"};

        string result = getImports.GenerateImportStatements(snippetText);
        Assert.Contains(string.Join("\n", expected), result);
    }
}
