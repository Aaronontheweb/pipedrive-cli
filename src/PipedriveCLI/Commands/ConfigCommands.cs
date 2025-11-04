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
        var setCommand = new Command("set", "Set configuration values");

        var apiKeyOption = new Option<string?>(
            aliases: new[] { "--api-key", "-k" },
            description: "Pipedrive API key");

        var domainOption = new Option<string?>(
            aliases: new[] { "--domain", "-d" },
            description: "Pipedrive domain (e.g., company.pipedrive.com)");

        var emailGatewayUrlOption = new Option<string?>(
            aliases: new[] { "--email-gateway-url", "-e" },
            description: "Email Gateway URL for approval workflow");

        var emailGatewayApiKeyOption = new Option<string?>(
            aliases: new[] { "--email-gateway-api-key", "-g" },
            description: "Email Gateway API key");

        setCommand.AddOption(apiKeyOption);
        setCommand.AddOption(domainOption);
        setCommand.AddOption(emailGatewayUrlOption);
        setCommand.AddOption(emailGatewayApiKeyOption);

        setCommand.SetHandler(async (apiKey, domain, emailGatewayUrl, emailGatewayApiKey) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey) &&
                    string.IsNullOrWhiteSpace(domain) &&
                    string.IsNullOrWhiteSpace(emailGatewayUrl) &&
                    string.IsNullOrWhiteSpace(emailGatewayApiKey))
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] At least one configuration value must be specified.");
                    AnsiConsole.MarkupLine("Use [cyan]--api-key[/], [cyan]--domain[/], [cyan]--email-gateway-url[/], or [cyan]--email-gateway-api-key[/]");
                    return;
                }

                await configService.SetConfigAsync(apiKey, domain, emailGatewayUrl, emailGatewayApiKey);

                AnsiConsole.MarkupLine("[green]✓[/] Configuration updated successfully");

                // Show what was set
                var profile = await configService.GetActiveProfileAsync();
                var profileName = await configService.GetActiveProfileNameAsync();

                var table = new Table();
                table.Border(TableBorder.Rounded);
                table.AddColumn("Setting");
                table.AddColumn("Value");

                table.AddRow("Profile", $"[cyan]{profileName}[/]");
                if (!string.IsNullOrWhiteSpace(apiKey))
                    table.AddRow("API Key", $"[dim]{MaskApiKey(profile.ApiKey)}[/]");
                if (!string.IsNullOrWhiteSpace(domain))
                    table.AddRow("Domain", $"[cyan]{profile.Domain}[/]");
                if (!string.IsNullOrWhiteSpace(emailGatewayUrl))
                    table.AddRow("Email Gateway URL", $"[cyan]{profile.EmailGatewayUrl ?? "Not set"}[/]");
                if (!string.IsNullOrWhiteSpace(emailGatewayApiKey))
                    table.AddRow("Email Gateway API Key", $"[dim]{MaskApiKey(profile.EmailGatewayApiKey)}[/]");

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }, apiKeyOption, domainOption, emailGatewayUrlOption, emailGatewayApiKeyOption);

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
                table.AddRow("Email Gateway URL", !string.IsNullOrWhiteSpace(profile.EmailGatewayUrl) ? $"[cyan]{profile.EmailGatewayUrl}[/]" : "[dim]Not set[/]");
                table.AddRow("Email Gateway API Key", profile.EmailGatewayApiKey != null ? $"[dim]{MaskApiKey(profile.EmailGatewayApiKey)}[/]" : "[dim]Not set[/]");
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
