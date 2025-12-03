using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PipedriveCLI.Commands;
using PipedriveCLI.Services;
using Spectre.Console;

namespace PipedriveCLI;

/// <summary>
/// Main entry point for the Pipedrive CLI application
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Get version for update checking
        var assembly = Assembly.GetExecutingAssembly();
        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var currentVersion = versionAttribute?.InformationalVersion ?? "0.1.0";

        // Start background update check (non-blocking)
        var updateCheckTask = CheckForUpdateInBackground(currentVersion);

        // Set up dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Get required services
        var configService = serviceProvider.GetRequiredService<ConfigurationService>();
        var apiClient = serviceProvider.GetRequiredService<PipedriveApiClient>();
        var fieldCache = serviceProvider.GetRequiredService<CustomFieldCache>();

        // Create root command
        var rootCommand = new RootCommand("Pipedrive CLI - Manage your Pipedrive CRM from the command line");

        // Add config command
        rootCommand.AddCommand(ConfigCommands.CreateConfigCommand(configService, apiClient));

        // Add leads command
        rootCommand.AddCommand(LeadsCommands.CreateLeadsCommand(apiClient));

        // Add deals command
        rootCommand.AddCommand(DealsCommands.CreateDealsCommand(apiClient, fieldCache));

        // Add activities command
        rootCommand.AddCommand(ActivitiesCommands.CreateActivitiesCommand(apiClient));

        // Add notes command
        rootCommand.AddCommand(NotesCommands.CreateNotesCommand(apiClient));

        // Add persons command
        rootCommand.AddCommand(PersonsCommands.CreatePersonsCommand(apiClient));

        // Add organizations command
        rootCommand.AddCommand(OrganizationsCommands.CreateOrganizationsCommand(apiClient));

        // Add pipelines command
        rootCommand.AddCommand(PipelinesCommands.CreatePipelinesCommand(apiClient));

        // Add deal fields command
        rootCommand.AddCommand(DealFieldsCommands.CreateDealFieldsCommand(apiClient));

        // Add email templates command
        rootCommand.AddCommand(TemplatesCommands.CreateTemplatesCommand(apiClient));

        // Add emails command (email history and conversations)
        rootCommand.AddCommand(EmailsCommands.CreateEmailsCommand(apiClient));

        // Add update command
        rootCommand.AddCommand(UpdateCommands.CreateUpdateCommand());

        // TODO: Add more commands (export)

        // Execute command
        var result = await rootCommand.InvokeAsync(args);

        // Wait for update check to complete and display if available
        var updateInfo = await updateCheckTask;
        if (updateInfo != null)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]📦 Update available: v{updateInfo.Version}[/]");
            AnsiConsole.MarkupLine($"   Run [bold]pipedrive update[/] to install the latest version");
        }

        return result;
    }

    /// <summary>
    /// Configures dependency injection services
    /// </summary>
    private static void ConfigureServices(ServiceCollection services)
    {
        // Register HttpClient for API calls
        services.AddHttpClient<PipedriveApiClient>();

        // Register services
        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<PipedriveApiClient>();
        services.AddSingleton<CustomFieldCache>();
    }

    /// <summary>
    /// Check for updates in the background without blocking the CLI
    /// </summary>
    private static async Task<UpdateInfo?> CheckForUpdateInBackground(string currentVersion)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(3); // Quick timeout for background check
            var updateService = new UpdateService(httpClient, currentVersion.Split('+')[0]); // Remove build metadata
            return await updateService.CheckForUpdateAsync();
        }
        catch
        {
            // Silently ignore errors in background update check
            return null;
        }
    }
}
