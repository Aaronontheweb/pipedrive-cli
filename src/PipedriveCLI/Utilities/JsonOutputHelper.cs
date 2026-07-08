using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using PipedriveCLI.Models;
using Spectre.Console;

namespace PipedriveCLI.Utilities;

/// <summary>
/// Shared support for read commands that can emit machine-readable JSON.
/// </summary>
internal static class JsonOutputHelper
{
    public const string JsonOptionDescription = "Output JSON to stdout for scripting; disables formatted tables and progress output";

    public static Option<bool> CreateJsonOption()
    {
        return new Option<bool>(
            aliases: new[] { "--json" },
            description: JsonOptionDescription);
    }

    public static async Task<T> FetchAsync<T>(bool json, string statusText, Func<Task<T>> fetch)
    {
        if (json)
        {
            return await fetch();
        }

        return await AnsiConsole.Status()
            .StartAsync(statusText, async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
                return await fetch();
            });
    }

    public static void Write<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
    {
        Console.WriteLine(JsonSerializer.Serialize(value, jsonTypeInfo));
    }

    public static void WriteError(string? error)
    {
        Write(new PipedriveErrorResponse
        {
            Success = false,
            Error = string.IsNullOrWhiteSpace(error) ? "Unknown error" : error
        }, ApiJsonContext.Default.PipedriveErrorResponse);
    }

    public static void WriteErrorOrMarkup(bool json, string message)
    {
        if (json)
        {
            WriteError(message);
        }
        else
        {
            AnsiConsole.MarkupLine(message);
        }
    }

    public static void WriteErrorOrMarkup(bool json, string jsonError, string markupMessage)
    {
        if (json)
        {
            WriteError(jsonError);
        }
        else
        {
            AnsiConsole.MarkupLine(markupMessage);
        }
    }

    public static void WriteException(bool json, Exception ex)
    {
        if (json)
        {
            WriteError(ex.Message);
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        }
    }
}
