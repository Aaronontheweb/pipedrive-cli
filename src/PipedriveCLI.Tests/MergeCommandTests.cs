using System.CommandLine;
using System.Net;
using PipedriveCLI.Commands;
using PipedriveCLI.Services;
using Spectre.Console;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// End-to-end regression tests for GitHub issue #179: <c>persons merge</c> and
/// <c>organizations merge</c> must correctly report success when the API returns the
/// merge-endpoint's scalar-int <c>owner_id</c> shape, instead of throwing a JSON
/// deserialization exception that gets reported as an ambiguous failure.
///
/// These exercise the real command invocation path (parse -> handler -> HTTP -> JSON
/// deserialization -> console output), not just model serialization, so a regression back to
/// the object-only <c>Owner</c> type would fail here exactly as it did for the real user.
/// </summary>
[Collection(ConsoleCaptureCollection.Name)]
public class MergeCommandTests
{
    [Fact]
    public async Task PersonsMerge_WithScalarOwnerIdResponse_ReportsSuccess()
    {
        // Arrange - actual PUT /persons/{id}/merge response shape (owner_id as scalar int)
        const string apiBody = """
        {
            "success": true,
            "data": {
                "id": 456,
                "name": "John Smith (Merged)",
                "owner_id": 10,
                "org_id": 789,
                "add_time": "2024-01-01T10:00:00Z",
                "update_time": "2024-11-05T15:00:00Z"
            }
        }
        """;

        var output = await InvokeAsync(
            apiBody,
            PersonsCommands.CreatePersonsCommand,
            "merge 456 999 --force");

        // Assert - the merge must be reported as successful, not as a JSON deserialization error
        Assert.Contains("merged successfully", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("could not be converted", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Failed to merge", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OrganizationsMerge_WithScalarOwnerIdResponse_ReportsSuccess()
    {
        // Arrange - actual PUT /organizations/{id}/merge response shape (owner_id as scalar int)
        const string apiBody = """
        {
            "success": true,
            "data": {
                "id": 999,
                "name": "Acme Corporation (Merged)",
                "people_count": 50,
                "owner_id": 10,
                "address": "123 Main St, New York, NY 10001",
                "add_time": "2024-01-01T10:00:00Z",
                "update_time": "2024-11-05T15:00:00Z"
            }
        }
        """;

        var output = await InvokeAsync(
            apiBody,
            OrganizationsCommands.CreateOrganizationsCommand,
            "merge 999 111 --force");

        // Assert - the merge must be reported as successful, not as a JSON deserialization error
        Assert.Contains("merged successfully", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("could not be converted", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Failed to merge", output, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> InvokeAsync(
        string apiResponseBody,
        Func<PipedriveApiClient, Command> commandFactory,
        string args)
    {
        var originalApiKey = Environment.GetEnvironmentVariable("PIPEDRIVE_API_KEY");
        var originalDomain = Environment.GetEnvironmentVariable("PIPEDRIVE_DOMAIN");
        var originalOut = Console.Out;
        var originalAnsiConsole = AnsiConsole.Console;

        Environment.SetEnvironmentVariable("PIPEDRIVE_API_KEY", "test-token");
        Environment.SetEnvironmentVariable("PIPEDRIVE_DOMAIN", "test.pipedrive.com");

        using var outputWriter = new StringWriter();
        Console.SetOut(outputWriter);

        // The command handlers write via the static AnsiConsole, which captures its output
        // writer once at first use rather than re-reading Console.Out on every call. Rebind it
        // explicitly to this test's StringWriter so output is captured regardless of test order.
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Interactive = InteractionSupport.No,
            Out = new AnsiConsoleOutput(outputWriter)
        });

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
            AnsiConsole.Console = originalAnsiConsole;
            Console.SetOut(originalOut);
            Environment.SetEnvironmentVariable("PIPEDRIVE_API_KEY", originalApiKey);
            Environment.SetEnvironmentVariable("PIPEDRIVE_DOMAIN", originalDomain);
        }
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
