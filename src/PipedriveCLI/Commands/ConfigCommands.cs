using System.CommandLine;
using PipedriveCLI.Services;
using Spectre.Console;

namespace PipedriveCLI.Commands;

/// <summary>
/// Configuration commands for managing CLI settings and profiles
/// </summary>
public static class ConfigCommands
{
    /// <summary>
    /// Creates the root 'config' command with all subcommands
    /// </summary>
    public static Command CreateConfigCommand(ConfigurationService configService, PipedriveApiClient apiClient)
    {
        var configCommand = new Command("config", "Manage Pipedrive CLI configuration");

        // Add subcommands
        configCommand.AddCommand(CreateSetCommand(configService));
        configCommand.AddCommand(CreateGetCommand(configService));
        configCommand.AddCommand(CreateTestCommand(configService, apiClient));
        configCommand.AddCommand(CreateProfileCommand(configService));

        return configCommand;
    }

    /// <summary>
    /// Creates the 'config set' command for setting configuration values
    /// </summary>
    private static Command CreateSetCommand(ConfigurationService configService)
    {
        var setCommand = new Command("set", "Set configuration values (both API key and domain are required for a working configuration)");

        var apiKeyOption = new Option<string?>(
            aliases: new[] { "--api-key", "-k" },
            description: "Pipedrive API key (required for API access)");

        var domainOption = new Option<string?>(
            aliases: new[] { "--domain", "-d" },
            description: "Pipedrive domain - your company name, will auto-append .pipedrive.com if not present (e.g., 'company' or 'company.pipedrive.com')");

        var profileOption = new Option<string?>(
            aliases: new[] { "--profile", "-p" },
            description: "Profile name to configure (creates if it doesn't exist). If not specified, uses the active profile.");

        setCommand.AddOption(apiKeyOption);
        setCommand.AddOption(domainOption);
        setCommand.AddOption(profileOption);

        setCommand.SetHandler(async (apiKey, domain, profile) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey) && string.IsNullOrWhiteSpace(domain))
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] At least one configuration value must be specified.");
                    AnsiConsole.MarkupLine("Note: Both [cyan]--api-key[/] and [cyan]--domain[/] are required for a working configuration.");
                    AnsiConsole.MarkupLine("Example: [dim]pipedrive config set --api-key YOUR_KEY --domain company[/]");
                    return;
                }

                await configService.SetConfigAsync(apiKey, domain, profile);

                var targetProfile = profile ?? await configService.GetActiveProfileNameAsync();
                AnsiConsole.MarkupLine($"[green]✓[/] Configuration updated successfully for profile: [cyan]{targetProfile}[/]");

                // Show what was set
                var config = await configService.LoadConfigAsync();
                var profileConfig = config.Profiles[targetProfile];

                var table = new Table();
                table.Border(TableBorder.Rounded);
                table.AddColumn("Setting");
                table.AddColumn("Value");

                table.AddRow("Profile", $"[cyan]{targetProfile}[/]");
                if (!string.IsNullOrWhiteSpace(apiKey))
                    table.AddRow("API Key", $"[dim]{MaskApiKey(profileConfig.ApiKey)}[/]");
                if (!string.IsNullOrWhiteSpace(domain))
                    table.AddRow("Domain", $"[cyan]{profileConfig.Domain}[/]");

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }, apiKeyOption, domainOption, profileOption);

        return setCommand;
    }

    /// <summary>
    /// Creates the 'config get' command for displaying current configuration
    /// </summary>
    private static Command CreateGetCommand(ConfigurationService configService)
    {
        var getCommand = new Command("get", "Display current configuration");

        getCommand.SetHandler(async () =>
        {
            try
            {
                var profile = await configService.GetActiveProfileAsync();
                var profileName = await configService.GetActiveProfileNameAsync();

                var table = new Table();
                table.Border(TableBorder.Rounded);
                table.AddColumn("Setting");
                table.AddColumn("Value");

                table.AddRow("Profile", $"[cyan]{profileName}[/]");
                table.AddRow("API Key", profile.ApiKey != null ? $"[dim]{MaskApiKey(profile.ApiKey)}[/]" : "[dim]Not set[/]");
                table.AddRow("Domain", !string.IsNullOrWhiteSpace(profile.Domain) ? $"[cyan]{profile.Domain}[/]" : "[dim]Not set[/]");
                table.AddRow("Config File", $"[dim]{configService.ConfigFilePath}[/]");

                AnsiConsole.Write(table);

                if (!profile.IsValid())
                {
                    AnsiConsole.MarkupLine("\n[yellow]⚠[/]  Configuration is incomplete. Run [cyan]pipedrive config set[/] to configure.");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        });

        return getCommand;
    }

    /// <summary>
    /// Creates the 'config test' command for testing API connectivity
    /// </summary>
    private static Command CreateTestCommand(ConfigurationService configService, PipedriveApiClient apiClient)
    {
        var testCommand = new Command("test", "Test API connection");

        testCommand.SetHandler(async () =>
        {
            try
            {
                AnsiConsole.Status()
                    .Start("Testing connection...", ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                    });

                var isConnected = await apiClient.TestConnectionAsync();

                if (isConnected)
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Connection successful! API key is valid.");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Connection failed. Please check your API key and domain.");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Connection failed: {ex.Message}");
            }
        });

        return testCommand;
    }

    /// <summary>
    /// Creates the 'config profile' command for managing profiles
    /// </summary>
    private static Command CreateProfileCommand(ConfigurationService configService)
    {
        var profileCommand = new Command("profile", "Manage configuration profiles");

        // List profiles subcommand
        var listCommand = new Command("list", "List all profiles");
        listCommand.SetHandler(async () =>
        {
            try
            {
                var profiles = await configService.ListProfilesAsync();
                var activeProfile = await configService.GetActiveProfileNameAsync();

                var table = new Table();
                table.Border(TableBorder.Rounded);
                table.AddColumn("Profile");
                table.AddColumn("Domain");
                table.AddColumn("Status");

                foreach (var (name, profile) in profiles.OrderBy(p => p.Key))
                {
                    var isActive = name == activeProfile;
                    var statusIcon = isActive ? "[green]✓ Active[/]" : "";
                    var profileDisplay = isActive ? $"[cyan bold]{name}[/]" : name;
                    var domainDisplay = !string.IsNullOrWhiteSpace(profile.Domain)
                        ? profile.Domain
                        : "[dim]Not configured[/]";

                    table.AddRow(profileDisplay, domainDisplay, statusIcon);
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        });

        // Switch profile subcommand
        var switchCommand = new Command("switch", "Switch to a different profile");
        var profileNameArg = new Argument<string>("name", "Profile name");
        switchCommand.AddArgument(profileNameArg);

        switchCommand.SetHandler(async (profileName) =>
        {
            try
            {
                await configService.SetActiveProfileAsync(profileName);
                AnsiConsole.MarkupLine($"[green]✓[/] Switched to profile: [cyan]{profileName}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }, profileNameArg);

        profileCommand.AddCommand(listCommand);
        profileCommand.AddCommand(switchCommand);

        return profileCommand;
    }

    /// <summary>
    /// Masks an API key for display (shows first 4 and last 4 characters)
    /// </summary>
    private static string MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return "Not set";

        if (apiKey.Length <= 8)
            return new string('*', apiKey.Length);

        return $"{apiKey[..4]}{'*'.ToString().PadRight(apiKey.Length - 8, '*')}{apiKey[^4..]}";
    }
}
