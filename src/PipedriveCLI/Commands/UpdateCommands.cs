using System.CommandLine;
using System.Reflection;
using PipedriveCLI.Services;
using Spectre.Console;

namespace PipedriveCLI.Commands;

public static class UpdateCommands
{
    public static Command CreateUpdateCommand()
    {
        var updateCommand = new Command("update", "Check for and install updates");

        var checkOption = new Option<bool>(
            new[] { "--check", "-c" },
            "Check for updates without installing"
        );

        var forceOption = new Option<bool>(
            new[] { "--force", "-f" },
            "Skip confirmation prompt"
        );

        var betaOption = new Option<bool>(
            new[] { "--beta", "-b" },
            "Include beta/pre-release versions"
        );

        updateCommand.AddOption(checkOption);
        updateCommand.AddOption(forceOption);
        updateCommand.AddOption(betaOption);

        updateCommand.SetHandler(async (checkOnly, force, beta) =>
        {
            await HandleUpdateCommand(checkOnly, force, beta);
        }, checkOption, forceOption, betaOption);

        return updateCommand;
    }

    private static async Task<int> HandleUpdateCommand(bool checkOnly, bool force, bool includeBeta)
    {
        // Get version information from assembly
        var assembly = Assembly.GetExecutingAssembly();
        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var currentVersion = versionAttribute?.InformationalVersion ?? "0.1.0";

        try
        {
            using var httpClient = new HttpClient();
            var updateService = new UpdateService(httpClient, currentVersion);

            if (includeBeta)
            {
                AnsiConsole.MarkupLine("[bold]Checking for updates (including beta/pre-releases)...[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[bold]Checking for updates...[/]");
            }

            var update = await updateService.CheckForUpdateAsync(includeBeta);

            if (update == null)
            {
                var versionType = includeBeta ? "version (including pre-releases)" : "stable version";
                AnsiConsole.MarkupLine($"[green]✓[/] You're running the latest {versionType} (v{currentVersion})");
                return 0;
            }

            var versionLabel = update.IsPrerelease ? "[yellow]New pre-release available:[/]" : "[yellow]New version available:[/]";
            var panel = new Panel(new Markup($"{versionLabel} [bold]v{update.Version}[/]\n[dim]Current version: v{currentVersion}[/]"))
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Yellow)
            };
            AnsiConsole.Write(panel);

            if (!string.IsNullOrEmpty(update.ReleaseNotes))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Release notes:[/]");
                AnsiConsole.WriteLine(new string('─', 40));

                // Truncate long release notes
                var notes = update.ReleaseNotes;
                if (notes.Length > 500)
                {
                    notes = notes.Substring(0, 497) + "...";
                }
                AnsiConsole.WriteLine(notes);
            }

            if (checkOnly)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Run[/] [yellow]pipedrive update[/] [dim]to install the latest version[/]");
                return 0;
            }

            if (!force)
            {
                AnsiConsole.WriteLine();
                if (!AnsiConsole.Confirm("Do you want to update now?", false))
                {
                    AnsiConsole.MarkupLine("[dim]Update cancelled[/]");
                    return 0;
                }
            }

            AnsiConsole.WriteLine();
            var success = await updateService.PerformUpdateAsync(update);

            if (success)
            {
                // This line won't be reached as the process exits during update
                AnsiConsole.MarkupLine("[green]✓[/] Update completed successfully!");
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Update failed.[/] Please try again or download manually from:");
                AnsiConsole.MarkupLine($"  [link]https://github.com/Aaronontheweb/pipedrive-cli/releases/latest[/]");
                return 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}
