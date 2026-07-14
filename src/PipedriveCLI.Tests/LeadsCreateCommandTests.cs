using System.CommandLine;
using System.Net;
using System.Text.Json;
using PipedriveCLI.Commands;
using PipedriveCLI.Services;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Regression tests for GitHub issue #180: <c>leads create</c> rejected the shared
/// <c>--json</c> option that other commands (list/get/search) accept, breaking scripting
/// patterns like <c>LEAD=$(pipedrive leads create ... --json | jq -r .data.id)</c>.
///
/// These exercise the real command invocation path (parse -> handler -> JSON serialization),
/// not just model serialization, so a regression back to "Unrecognized command or argument
/// --json" would fail here exactly as it did for the real user.
/// </summary>
[Collection(ConsoleCaptureCollection.Name)]
public class LeadsCreateCommandTests
{
    [Fact]
    public async Task LeadsCreate_WithJson_EmitsSuccessDataEnvelope()
    {
        const string apiBody = """
        {
            "success": true,
            "data": {
                "id": "a5c5e7b5-28f7-4b57-96fe-3a5e7fb24d5b",
                "title": "New Lead From Script",
                "owner_id": null,
                "person_id": 123,
                "organization_id": null,
                "was_seen": false,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        var output = await InvokeAsync(
            apiBody,
            "create --title \"New Lead From Script\" --person-id 123 --json");

        using var document = ParseJsonObject(output);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());

        Assert.True(root.TryGetProperty("data", out var data));
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("a5c5e7b5-28f7-4b57-96fe-3a5e7fb24d5b", data.GetProperty("id").GetString());
        Assert.Equal("New Lead From Script", data.GetProperty("title").GetString());

        // Regression guard: no formatted-table / markup output must leak into the JSON stream
        Assert.DoesNotContain("[green]", output);
        Assert.DoesNotContain("Lead created successfully", output);
    }

    [Fact]
    public async Task LeadsCreate_WithJson_ValidationFailure_EmitsJsonErrorPayload()
    {
        // Neither --person-id nor --org-id supplied - must fail validation before any HTTP call
        var output = await InvokeAsync(
            apiBody: "{}",
            args: "create --title \"Orphan Lead\" --json");

        using var document = ParseJsonObject(output);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("error", out var error));
        Assert.Equal("Either --person-id or --org-id must be specified", error.GetString());

        if (root.TryGetProperty("success", out var success))
        {
            Assert.False(success.GetBoolean());
        }
    }

    private static async Task<string> InvokeAsync(string apiBody, string args)
    {
        var originalApiKey = Environment.GetEnvironmentVariable("PIPEDRIVE_API_KEY");
        var originalDomain = Environment.GetEnvironmentVariable("PIPEDRIVE_DOMAIN");
        var originalOut = Console.Out;

        Environment.SetEnvironmentVariable("PIPEDRIVE_API_KEY", "test-token");
        Environment.SetEnvironmentVariable("PIPEDRIVE_DOMAIN", "test.pipedrive.com");

        using var outputWriter = new StringWriter();
        Console.SetOut(outputWriter);

        try
        {
            var httpClient = new HttpClient(new StubHttpMessageHandler(apiBody));
            var apiClient = new PipedriveApiClient(httpClient, new ConfigurationService());
            var command = LeadsCommands.CreateLeadsCommand(apiClient);

            await command.InvokeAsync(args);
            return outputWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
            Environment.SetEnvironmentVariable("PIPEDRIVE_API_KEY", originalApiKey);
            Environment.SetEnvironmentVariable("PIPEDRIVE_DOMAIN", originalDomain);
        }
    }

    private static JsonDocument ParseJsonObject(string output)
    {
        var trimmed = output.Trim();
        if (!trimmed.StartsWith('{') || !trimmed.EndsWith('}'))
        {
            throw new Xunit.Sdk.XunitException($"Expected a JSON object. Received: {output}");
        }

        return JsonDocument.Parse(trimmed);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _body;

        public StubHttpMessageHandler(string body) => _body = body;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_body)
            });
        }
    }
}
