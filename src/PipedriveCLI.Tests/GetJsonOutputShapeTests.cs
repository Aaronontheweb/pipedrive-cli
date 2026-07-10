using System.CommandLine;
using System.Net;
using System.Text.Json;
using PipedriveCLI.Commands;
using PipedriveCLI.Services;

namespace PipedriveCLI.Tests;

/// <summary>
/// Regression tests for GitHub issue #175: single-object <c>get</c> commands must emit the
/// SAME <c>{ "success": ..., "data": { ... } }</c> wrapper under <c>--json</c> as
/// <c>list</c>/<c>search</c>, instead of the bare entity at the JSON root.
///
/// These exercise the real command invocation path (parse -> handler -> JSON serialization),
/// not just model serialization, so a regression back to root-level objects would fail here.
/// </summary>
[Collection(ConsoleCaptureCollection.Name)]
public class GetJsonOutputShapeTests
{
    [Fact]
    public async Task DealsGet_WithJson_WrapsEntityInSuccessDataEnvelope()
    {
        const string apiBody = """
        {
            "success": true,
            "data": {
                "id": 123,
                "title": "Enterprise Deal",
                "value": 50000.00,
                "currency": "USD",
                "status": "open"
            }
        }
        """;

        var output = await InvokeGetAsync(
            apiBody,
            apiClient => DealsCommands.CreateDealsCommand(apiClient, new CustomFieldCache(apiClient)),
            "get 123 --json");

        using var document = ParseJsonObject(output);
        var root = document.RootElement;

        // The entity must NOT be at the root anymore.
        Assert.False(root.TryGetProperty("title", out _),
            "Issue #175 regression: entity fields must be nested under .data, not at the JSON root.");

        Assert.True(root.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());

        Assert.True(root.TryGetProperty("data", out var data));
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal(123, data.GetProperty("id").GetInt32());
        Assert.Equal("Enterprise Deal", data.GetProperty("title").GetString());
    }

    [Fact]
    public async Task OrganizationsGet_WithJson_WrapsEntityInSuccessDataEnvelope()
    {
        const string apiBody = """
        {
            "success": true,
            "data": {
                "id": 456,
                "name": "Acme Corporation"
            }
        }
        """;

        var output = await InvokeGetAsync(
            apiBody,
            OrganizationsCommands.CreateOrganizationsCommand,
            "get 456 --json");

        using var document = ParseJsonObject(output);
        var root = document.RootElement;

        Assert.False(root.TryGetProperty("name", out _),
            "Issue #175 regression: entity fields must be nested under .data, not at the JSON root.");

        Assert.True(root.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());

        Assert.True(root.TryGetProperty("data", out var data));
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal(456, data.GetProperty("id").GetInt32());
        Assert.Equal("Acme Corporation", data.GetProperty("name").GetString());
    }

    private static async Task<string> InvokeGetAsync(
        string apiResponseBody,
        Func<PipedriveApiClient, Command> commandFactory,
        string args)
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
            var httpClient = new HttpClient(new StubHttpMessageHandler(apiResponseBody));
            var apiClient = new PipedriveApiClient(httpClient, new ConfigurationService());
            var command = commandFactory(apiClient);

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
