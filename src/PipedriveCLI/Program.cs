using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using PipedriveCLI.Commands;
using PipedriveCLI.Services;

namespace PipedriveCLI;

/// <summary>
/// Main entry point for the Pipedrive CLI application
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Set up dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Get required services
        var configService = serviceProvider.GetRequiredService<ConfigurationService>();
        var apiClient = serviceProvider.GetRequiredService<PipedriveApiClient>();

        // Create root command
        var rootCommand = new RootCommand("Pipedrive CLI - Manage your Pipedrive CRM from the command line");

        // Add config command
        rootCommand.AddCommand(ConfigCommands.CreateConfigCommand(configService, apiClient));

        // Add leads command
        rootCommand.AddCommand(LeadsCommands.CreateLeadsCommand(apiClient));

        // Add deals command
        rootCommand.AddCommand(DealsCommands.CreateDealsCommand(apiClient));

        // Add activities command
        rootCommand.AddCommand(ActivitiesCommands.CreateActivitiesCommand(apiClient));

        // TODO: Add more commands (persons, organizations, export)

        // Execute command
        return await rootCommand.InvokeAsync(args);
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
    }
}
