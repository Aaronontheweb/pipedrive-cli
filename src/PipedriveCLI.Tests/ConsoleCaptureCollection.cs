using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Groups test classes that redirect the process-wide <see cref="System.Console.Out"/> so they
/// run serially with respect to one another. Without this, xUnit runs test classes in parallel
/// and concurrent <c>Console.SetOut</c> redirections cross-contaminate captured output.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ConsoleCaptureCollection
{
    public const string Name = "ConsoleCapture";
}
