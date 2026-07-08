using System.CommandLine;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using PipedriveCLI.Commands;
using PipedriveCLI.Services;
using PipedriveCLI.Utilities;

namespace PipedriveCLI.Tests;

public class JsonOutputCommandMetadataTests
{
    [Theory]
    [MemberData(nameof(ReadCommandsWithJsonOptionData))]
    public void ReadCommands_ShouldExposeJsonOption(string commandName, string[] expectedJsonSubcommands)
    {
        var apiClient = CreateApiClient();
        var fieldCache = new CustomFieldCache(apiClient);
        var command = CreateCommand(commandName, apiClient, fieldCache);

        var actualJsonSubcommands = command.Subcommands
            .Where(HasJsonOption)
            .Select(c => c.Name)
            .OrderBy(static c => c)
            .ToArray();

        var expected = expectedJsonSubcommands
            .OrderBy(static c => c)
            .ToArray();

        Assert.Equal(expected, actualJsonSubcommands);
    }

    [Theory]
    [MemberData(nameof(JsonValidationErrorData))]
    public async Task ValidationFailures_WithJson_ReturnJsonErrorPayload(string args, string expectedError)
    {
        var apiClient = CreateApiClient();
        var rootCommand = CreateValidationRootCommand(apiClient);

        var (exitCode, output) = await InvokeWithCapturedOutput(rootCommand, args);

        Assert.Equal(0, exitCode);
        AssertJsonErrorPayload(output, expectedError);
    }

    [Fact]
    public async Task ProgramMain_WithJsonParseError_ReturnsJsonErrorPayload()
    {
        var (exitCode, output) = await CaptureOutputAsync(() =>
            PipedriveCLI.Program.Main(["deals", "get", "not-int", "--json"]));

        Assert.Equal(1, exitCode);
        AssertJsonErrorPayloadContaining(output, "not-int");
    }

    [Fact]
    public void WriteError_ShouldSerializeMachineReadableErrorPayload()
    {
        var output = CaptureOutput(() => JsonOutputHelper.WriteError("Something failed"));

        AssertJsonErrorPayload(output, "Something failed");
    }

    [Fact]
    public void WriteException_WithJson_ShouldSerializeMachineReadableErrorPayload()
    {
        var output = CaptureOutput(() => JsonOutputHelper.WriteException(true, new InvalidOperationException("Boom")));

        AssertJsonErrorPayload(output, "Boom");
    }

    [Fact]
    public async Task FetchAsync_WithJson_ShouldSuppressStatusOutput()
    {
        var (result, output) = await CaptureOutputAsync(() =>
            JsonOutputHelper.FetchAsync(true, "Fetching records...", () => Task.FromResult("result")));

        Assert.Equal("result", result);
        Assert.Empty(output);
    }

    private static void AssertJsonErrorPayload(string output, string expectedError)
    {
        Assert.NotEmpty(output);
        Assert.DoesNotContain("[red]", output);
        Assert.DoesNotContain("Error:[/]", output);

        using var document = AssertJsonObject(output);
        var root = document.RootElement;

        Assert.True(TryGetPropertyCaseInsensitive(root, "error", out var errorProperty));
        Assert.Equal(expectedError, errorProperty.GetString());

        if (TryGetPropertyCaseInsensitive(root, "success", out var successProperty))
        {
            Assert.False(successProperty.GetBoolean());
        }
    }

    private static void AssertJsonErrorPayloadContaining(string output, string expectedErrorContent)
    {
        Assert.NotEmpty(output);
        Assert.DoesNotContain("Usage:", output);

        using var document = AssertJsonObject(output);
        var root = document.RootElement;

        Assert.True(TryGetPropertyCaseInsensitive(root, "error", out var errorProperty));
        Assert.Contains(expectedErrorContent, errorProperty.GetString(), StringComparison.Ordinal);
    }

    private static bool HasJsonOption(Command command)
    {
        return command.Options.Any(option =>
            string.Equals(option.Name, "json", StringComparison.OrdinalIgnoreCase) &&
            option.Aliases.Contains("--json", StringComparer.Ordinal));
    }

    private static Command CreateCommand(string commandName, PipedriveApiClient apiClient, CustomFieldCache fieldCache)
    {
        return commandName switch
        {
            "activities" => ActivitiesCommands.CreateActivitiesCommand(apiClient),
            "deals" => DealsCommands.CreateDealsCommand(apiClient, fieldCache),
            "emails" => EmailsCommands.CreateEmailsCommand(apiClient),
            "leads" => LeadsCommands.CreateLeadsCommand(apiClient),
            "notes" => NotesCommands.CreateNotesCommand(apiClient),
            "persons" => PersonsCommands.CreatePersonsCommand(apiClient),
            "pipelines" => PipelinesCommands.CreatePipelinesCommand(apiClient),
            "dealFields" => DealFieldsCommands.CreateDealFieldsCommand(apiClient),
            "organizationFields" => OrganizationFieldsCommands.CreateOrganizationFieldsCommand(apiClient),
            "templates" => TemplatesCommands.CreateTemplatesCommand(apiClient),
            "organizations" => OrganizationsCommands.CreateOrganizationsCommand(apiClient),
            _ => throw new ArgumentOutOfRangeException(nameof(commandName), commandName)
        };
    }

    private static RootCommand CreateValidationRootCommand(PipedriveApiClient apiClient)
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(ActivitiesCommands.CreateActivitiesCommand(apiClient));
        rootCommand.AddCommand(DealsCommands.CreateDealsCommand(apiClient, new CustomFieldCache(apiClient)));
        return rootCommand;
    }

    private static PipedriveApiClient CreateApiClient()
    {
        var httpClient = new HttpClient(new DummyHttpMessageHandler());
        return new PipedriveApiClient(httpClient, new ConfigurationService());
    }

    private static async Task<(int ExitCode, string Output)> InvokeWithCapturedOutput(RootCommand rootCommand, string args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var outputWriter = new StringWriter();

        Console.SetOut(outputWriter);
        Console.SetError(outputWriter);

        try
        {
            var exitCode = await rootCommand.InvokeAsync(args);
            return (exitCode, outputWriter.ToString().Trim());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static string CaptureOutput(Action action)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var outputWriter = new StringWriter();

        Console.SetOut(outputWriter);
        Console.SetError(outputWriter);

        try
        {
            action();
            return outputWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static async Task<(T Result, string Output)> CaptureOutputAsync<T>(Func<Task<T>> action)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var outputWriter = new StringWriter();

        Console.SetOut(outputWriter);
        Console.SetError(outputWriter);

        try
        {
            var result = await action();
            return (result, outputWriter.ToString().Trim());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static JsonDocument AssertJsonObject(string output)
    {
        var trimmedOutput = output.Trim();
        if (!trimmedOutput.StartsWith('{') || !trimmedOutput.EndsWith('}'))
        {
            throw new Xunit.Sdk.XunitException($"Expected JSON object output. Received: {output}");
        }

        return JsonDocument.Parse(trimmedOutput);
    }

    public static IEnumerable<object[]> ReadCommandsWithJsonOptionData()
    {
        yield return ["activities", new[] { "get", "list" }];
        yield return ["deals", new[] { "list", "get", "participants", "products" }];
        yield return ["emails", new[] { "list-for-deal", "list-for-person", "get", "threads", "thread", "thread-messages" }];
        yield return ["leads", new[] { "list", "get", "search" }];
        yield return ["notes", new[] { "list", "get" }];
        yield return ["persons", new[] { "list", "get", "search" }];
        yield return ["pipelines", new[] { "list", "get", "stages" }];
        yield return ["dealFields", new[] { "list" }];
        yield return ["organizationFields", new[] { "list" }];
        yield return ["templates", new[] { "list", "get" }];
        yield return ["organizations", new[] { "list", "get", "search" }];
    }

    public static IEnumerable<object[]> JsonValidationErrorData()
    {
        yield return
        [
            "deals list --json --updated-since not-a-date",
            "Invalid timestamp format for --updated-since. Expected RFC3339, e.g. 2026-06-24T00:00:00Z"
        ];
        yield return
        [
            "deals list --json --closing-after not-a-date",
            "Invalid date format for --closing-after. Use YYYY-MM-DD (e.g., 2024-01-15)."
        ];
        yield return
        [
            "deals list --json --closing-after 2024-12-31 --closing-before 2024-01-01",
            "--closing-after must be before --closing-before."
        ];
        yield return
        [
            "deals list --json --closing-after 2024-01-01 --won-after 2024-01-01",
            "Cannot use --closing-after/--closing-before together with --won-after/--won-before."
        ];
        yield return
        [
            "activities list --json --deal-id 1 --person-id 2",
            "Only one of --deal-id, --person-id, or --org-id can be specified at a time"
        ];
        yield return
        [
            "activities list --json --due-before not-a-date",
            "Invalid date format for --due-before. Expected YYYY-MM-DD"
        ];
        yield return
        [
            "activities list --json --updated-until not-a-date",
            "Invalid timestamp format for --updated-until. Expected RFC3339, e.g. 2026-06-24T00:00:00Z"
        ];
    }

    private sealed class DummyHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };

            return Task.FromResult(response);
        }
    }
}
