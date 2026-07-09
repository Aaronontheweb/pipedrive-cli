using System.CommandLine;
using PipedriveCLI.Models;
using PipedriveCLI.Services;
using PipedriveCLI.Utilities;
using Spectre.Console;

namespace PipedriveCLI.Commands;

/// <summary>
/// Commands for managing Pipedrive organizations
/// </summary>
public static class OrganizationsCommands
{
    /// <summary>
    /// Creates the root 'organizations' command with all subcommands
    /// </summary>
    public static Command CreateOrganizationsCommand(PipedriveApiClient apiClient)
    {
        var organizationsCommand = new Command("organizations", "Manage Pipedrive organizations");

        // Add subcommands
        organizationsCommand.AddCommand(CreateListCommand(apiClient));
        organizationsCommand.AddCommand(CreateGetCommand(apiClient));
        organizationsCommand.AddCommand(CreateCreateCommand(apiClient));
        organizationsCommand.AddCommand(CreateUpdateCommand(apiClient));
        organizationsCommand.AddCommand(CreateDeleteCommand(apiClient));
        organizationsCommand.AddCommand(CreateSearchCommand(apiClient));
        organizationsCommand.AddCommand(CreateMergeCommand(apiClient));

        return organizationsCommand;
    }

    /// <summary>
    /// Creates the 'organizations list' command
    /// </summary>
    private static Command CreateListCommand(PipedriveApiClient apiClient)
    {
        var listCommand = new Command("list", "List all organizations");

        var limitOption = new Option<int?>(
            aliases: new[] { "--limit", "-l" },
            description: "Number of organizations to return (default: 100)");

        var startOption = new Option<int?>(
            aliases: new[] { "--start", "-s" },
            description: "Pagination start (default: 0)");

        listCommand.AddOption(limitOption);
        listCommand.AddOption(startOption);
        var jsonOption = JsonOutputHelper.CreateJsonOption();
        listCommand.AddOption(jsonOption);

        listCommand.SetHandler(async (limit, start, json) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await JsonOutputHelper.FetchAsync(
                    json,
                    "Fetching organizations...",
                    () => apiClient.GetOrganizationsAsync(limit, start));

                if (response?.Success == true)
                {
                    response.Data ??= new List<Organization>();

                    if (json)
                    {
                        JsonOutputHelper.Write(response, ApiJsonContext.Default.PipedriveResponseListOrganization);
                        return;
                    }

                    var table = new Table();
                    table.Border(TableBorder.Rounded);
                    table.AddColumn(new TableColumn("ID").NoWrap());
                    table.AddColumn("Name");
                    table.AddColumn("People Count");
                    table.AddColumn("Address");
                    table.AddColumn("Owner ID");
                    table.AddColumn("Added");

                    foreach (var org in response.Data)
                    {
                        table.AddRow(
                            org.Id.ToString(),
                            org.Name ?? "-",
                            org.PeopleCount.ToString(),
                            org.Address ?? "-",
                            org.OwnerId?.Id.ToString() ?? "-",
                            org.AddTime ?? "-"
                        );
                    }

                    AnsiConsole.Write(table);

                    if (response.AdditionalData?.Pagination != null)
                    {
                        var pagination = response.AdditionalData.Pagination;
                        AnsiConsole.MarkupLine($"\n[dim]Showing {pagination.Start + 1}-{pagination.Start + response.Data.Count} " +
                            $"| More available: {pagination.MoreItemsInCollection}[/]");
                    }

                    AnsiConsole.MarkupLine($"\n[green]✓[/] Found {response.Data.Count} organization(s)");
                }
                else if (json)
                {
                    JsonOutputHelper.WriteError(response?.Error);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch organizations: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                JsonOutputHelper.WriteException(json, ex);
            }
        }, limitOption, startOption, jsonOption);

        return listCommand;
    }

    /// <summary>
    /// Creates the 'organizations get' command
    /// </summary>
    private static Command CreateGetCommand(PipedriveApiClient apiClient)
    {
        var getCommand = new Command("get", "Get a specific organization by ID");

        var idArgument = new Argument<int>("id", "Organization ID");
        getCommand.AddArgument(idArgument);

        var jsonOption = JsonOutputHelper.CreateJsonOption();
        getCommand.AddOption(jsonOption);

        getCommand.SetHandler(async (id, json) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await JsonOutputHelper.FetchAsync(
                    json,
                    $"Fetching organization {id}...",
                    () => apiClient.GetOrganizationByIdAsync(id));

                if (response?.Success == true && response.Data != null)
                {
                    if (json)
                    {
                        JsonOutputHelper.Write(response.Data, ApiJsonContext.Default.Organization);
                    }
                    else
                    {
                        // Output formatted display
                        var org = response.Data;
                        var ownerInfo = org.OwnerId != null
                            ? $"{org.OwnerId.Id} ({org.OwnerId.Name})"
                            : "N/A";

                        var customFieldsDisplay = CustomFieldHelper.FormatCustomFields(org.CustomFields);

                        var panel = new Panel(new Markup(
                            $"[bold]Name:[/] {Markup.Escape(org.Name ?? "")}\n" +
                            $"[bold]ID:[/] {org.Id}\n" +
                            $"[bold]People Count:[/] {org.PeopleCount}\n" +
                            $"[bold]Address:[/] {Markup.Escape(org.Address ?? "N/A")}\n" +
                            $"[bold]Owner:[/] {Markup.Escape(ownerInfo)}\n" +
                            $"[bold]CC Email:[/] {Markup.Escape(org.CcEmail ?? "N/A")}\n" +
                            $"[bold]Added:[/] {Markup.Escape(org.AddTime ?? "N/A")}\n" +
                            $"[bold]Updated:[/] {Markup.Escape(org.UpdateTime ?? "N/A")}" +
                            customFieldsDisplay))
                        {
                            Header = new PanelHeader($"[green]Organization: {Markup.Escape(org.Name ?? "")}[/]"),
                            Border = BoxBorder.Rounded
                        };

                        AnsiConsole.Write(panel);
                    }
                }
                else
                {
                    if (json)
                    {
                        JsonOutputHelper.WriteError(response?.Error);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch organization: {Markup.Escape(response?.Error ?? "Unknown error")}");
                    }
                }
            }
            catch (Exception ex)
            {
                JsonOutputHelper.WriteException(json, ex);
            }
        }, idArgument, jsonOption);

        return getCommand;
    }

    /// <summary>
    /// Creates the 'organizations create' command
    /// </summary>
    private static Command CreateCreateCommand(PipedriveApiClient apiClient)
    {
        var createCommand = new Command("create", "Create a new organization");

        var nameOption = new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Organization name (required)")
        { IsRequired = true };

        var addressOption = new Option<string?>(
            aliases: new[] { "--address", "-a" },
            description: "Organization address");

        createCommand.AddOption(nameOption);
        createCommand.AddOption(addressOption);

        createCommand.SetHandler(async (name, address) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var organization = new Organization
                {
                    Name = name,
                    Address = address
                };

                var response = await AnsiConsole.Status()
                    .StartAsync("Creating organization...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.CreateOrganizationAsync(organization);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Organization created successfully");
                    AnsiConsole.MarkupLine($"[dim]ID:[/] {Markup.Escape(response.Data.Id.ToString())}");
                    AnsiConsole.MarkupLine($"[dim]Name:[/] {Markup.Escape(response.Data.Name ?? "")}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to create organization: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, nameOption, addressOption);

        return createCommand;
    }

    /// <summary>
    /// Creates the 'organizations update' command
    /// </summary>
    private static Command CreateUpdateCommand(PipedriveApiClient apiClient)
    {
        var updateCommand = new Command("update", "Update an existing organization");

        var idArgument = new Argument<int>("id", "Organization ID");
        updateCommand.AddArgument(idArgument);

        var nameOption = new Option<string?>(
            aliases: new[] { "--name", "-n" },
            description: "New organization name");

        var addressOption = new Option<string?>(
            aliases: new[] { "--address", "-a" },
            description: "New organization address");

        var customFieldsOption = new Option<string?>(
            aliases: new[] { "--custom-fields", "-cf" },
            description: "Custom fields to update in format: hash1=value1,hash2=value2");

        updateCommand.AddOption(nameOption);
        updateCommand.AddOption(addressOption);
        updateCommand.AddOption(customFieldsOption);

        updateCommand.SetHandler(async (id, name, address, customFields) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(address) &&
                    string.IsNullOrWhiteSpace(customFields))
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] At least one field must be specified to update");
                    return;
                }

                await apiClient.InitializeAsync();

                var organization = new Organization();

                if (!string.IsNullOrWhiteSpace(name)) organization.Name = name;
                if (!string.IsNullOrWhiteSpace(address)) organization.Address = address;

                // Parse and set custom fields
                if (!string.IsNullOrWhiteSpace(customFields))
                {
                    organization.CustomFields = CustomFieldHelper.ParseCustomFields(customFields);
                }

                var response = await AnsiConsole.Status()
                    .StartAsync($"Updating organization {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.UpdateOrganizationAsync(id, organization);
                    });

                if (response?.Success == true)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Organization updated successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to update organization: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument, nameOption, addressOption, customFieldsOption);

        return updateCommand;
    }

    /// <summary>
    /// Creates the 'organizations delete' command
    /// </summary>
    private static Command CreateDeleteCommand(PipedriveApiClient apiClient)
    {
        var deleteCommand = new Command("delete", "Delete an organization");

        var idArgument = new Argument<int>("id", "Organization ID");
        deleteCommand.AddArgument(idArgument);

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f", "-y" },
            description: "Skip confirmation prompt");

        deleteCommand.AddOption(forceOption);

        deleteCommand.SetHandler(async (id, force) =>
        {
            try
            {
                if (!force)
                {
                    var confirm = AnsiConsole.Confirm($"Are you sure you want to delete organization {id}?");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                        return;
                    }
                }

                await apiClient.InitializeAsync();

                var success = await AnsiConsole.Status()
                    .StartAsync($"Deleting organization {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.DeleteOrganizationAsync(id);
                    });

                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Organization {id} deleted successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete organization {id}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument, forceOption);

        return deleteCommand;
    }

    /// <summary>
    /// Creates the 'organizations search' command
    /// </summary>
    private static Command CreateSearchCommand(PipedriveApiClient apiClient)
    {
        var searchCommand = new Command("search", "Search for organizations");

        var termArgument = new Argument<string>("term", "Search term");
        searchCommand.AddArgument(termArgument);

        var limitOption = new Option<int?>(
            aliases: new[] { "--limit", "-l" },
            description: "Maximum number of results (default: 100)");

        searchCommand.AddOption(limitOption);
        var jsonOption = JsonOutputHelper.CreateJsonOption();
        searchCommand.AddOption(jsonOption);

        searchCommand.SetHandler(async (term, limit, json) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await JsonOutputHelper.FetchAsync(
                    json,
                    $"Searching for '{term}'...",
                    () => apiClient.SearchOrganizationsAsync(term, limit));

                if (response?.Success == true)
                {
                    response.Data ??= new List<Organization>();

                    if (json)
                    {
                        JsonOutputHelper.Write(response, ApiJsonContext.Default.PipedriveResponseListOrganization);
                        return;
                    }

                    if (response.Data.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[yellow]No organizations found matching '{term}'[/]");
                        return;
                    }

                    var table = new Table();
                    table.Border(TableBorder.Rounded);
                    table.AddColumn(new TableColumn("ID").NoWrap());
                    table.AddColumn("Name");
                    table.AddColumn("People Count");
                    table.AddColumn("Address");
                    table.AddColumn("Owner ID");

                    foreach (var org in response.Data)
                    {
                        table.AddRow(
                            org.Id.ToString(),
                            org.Name ?? "-",
                            org.PeopleCount.ToString(),
                            org.Address ?? "-",
                            org.OwnerId?.Id.ToString() ?? "-"
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"\n[green]✓[/] Found {response.Data.Count} organization(s) matching '{term}'");
                }
                else if (json)
                {
                    JsonOutputHelper.WriteError(response?.Error);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Search failed: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                JsonOutputHelper.WriteException(json, ex);
            }
        }, termArgument, limitOption, jsonOption);

        return searchCommand;
    }

    /// <summary>
    /// Creates the 'organizations merge' command
    /// </summary>
    private static Command CreateMergeCommand(PipedriveApiClient apiClient)
    {
        var mergeCommand = new Command("merge", "Merge two organizations");

        var idArgument = new Argument<int>("id", "ID of the organization to be merged (will be deleted)");
        mergeCommand.AddArgument(idArgument);

        var mergeWithIdArgument = new Argument<int>("merge-with-id", "ID of the organization to merge with (takes priority in conflicts)");
        mergeCommand.AddArgument(mergeWithIdArgument);

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f", "-y" },
            description: "Skip confirmation prompt");

        mergeCommand.AddOption(forceOption);

        mergeCommand.SetHandler(async (id, mergeWithId, force) =>
        {
            try
            {
                if (!force)
                {
                    var confirm = AnsiConsole.Confirm($"Are you sure you want to merge organization {id} into organization {mergeWithId}? Organization {id} will be deleted.");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                        return;
                    }
                }

                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Merging organization {id} into {mergeWithId}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.MergeOrganizationAsync(id, mergeWithId);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Organizations merged successfully");
                    AnsiConsole.MarkupLine($"[dim]Merged organization ID:[/] {response.Data.Id}");
                    AnsiConsole.MarkupLine($"[dim]Name:[/] {Markup.Escape(response.Data.Name ?? "")}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to merge organizations: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument, mergeWithIdArgument, forceOption);

        return mergeCommand;
    }
}
