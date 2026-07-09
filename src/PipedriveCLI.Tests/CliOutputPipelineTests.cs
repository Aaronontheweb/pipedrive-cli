using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using PipedriveCLI;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Regression tests for the CLI output pipeline.
///
/// Guards against the 0.11.0 silent-output regression, where the parser was built via
/// <c>rootCommand.Parse(args)</c> (a bare <c>new Parser(command)</c> with no middleware),
/// causing <c>--version</c>, <c>--help</c>, and parse-error reporting to emit nothing and
/// exit 0. The fix routes parser construction through <see cref="Program.BuildParser"/>,
/// which applies <c>UseDefaults()</c>. These tests exercise the actual invocation/output
/// path rather than pure JSON model serialization.
/// </summary>
public class CliOutputPipelineTests
{
    private static RootCommand CreateRootCommand()
    {
        var root = new RootCommand("Pipedrive CLI test root");
        // Include at least one subcommand so --help renders a command list.
        root.AddCommand(new Command("config", "Manage configuration"));
        return root;
    }

    [Fact]
    public async Task VersionFlag_ProducesNonEmptyOutput()
    {
        var parser = Program.BuildParser(CreateRootCommand());
        var console = new TestConsole();

        var exitCode = await parser.InvokeAsync("--version", console);

        Assert.Equal(0, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(console.Out.ToString()),
            "--version must print the version to stdout (regression: 0.11.0 emitted nothing).");
    }

    [Fact]
    public async Task HelpFlag_ProducesNonEmptyOutput()
    {
        var parser = Program.BuildParser(CreateRootCommand());
        var console = new TestConsole();

        var exitCode = await parser.InvokeAsync("--help", console);

        Assert.Equal(0, exitCode);
        var output = console.Out.ToString();
        Assert.False(string.IsNullOrWhiteSpace(output),
            "--help must print usage to stdout (regression: 0.11.0 emitted nothing).");
        Assert.Contains("Usage", output);
    }

    [Fact]
    public async Task UnrecognizedCommand_ReportsErrorAndNonZeroExit()
    {
        var parser = Program.BuildParser(CreateRootCommand());
        var console = new TestConsole();

        var exitCode = await parser.InvokeAsync("this-is-not-a-command", console);

        Assert.NotEqual(0, exitCode);
        // Parse-error reporting middleware writes diagnostics to stderr.
        Assert.False(string.IsNullOrWhiteSpace(console.Error.ToString()),
            "Unrecognized commands must report an error (regression: 0.11.0 was silent and exited 0).");
    }
}
