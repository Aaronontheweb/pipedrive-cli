using System.CommandLine;
using PipedriveCLI.Models;
using PipedriveCLI.Services;
using Spectre.Console;

namespace PipedriveCLI.Commands;

/// <summary>
/// Commands for managing Pipedrive leads
/// </summary>
public static class LeadsCommands
{
    /// <summary>
    /// Creates the root 'leads' command with all subcommands
    /// </summary>
    public static Command CreateLeadsCommand(PipedriveApiClient apiClient)
    {
        var leadsCommand = new Command("leads", "Manage Pipedrive leads");

        // Add subcommands
        leadsCommand.AddCommand(CreateListCommand(apiClient));
        leadsCommand.AddCommand(CreateGetCommand(apiClient));
        leadsCommand.AddCommand(CreateCreateCommand(apiClient));
        leadsCommand.AddCommand(CreateUpdateCommand(apiClient));
        leadsCommand.AddCommand(CreateDeleteCommand(apiClient));
        leadsCommand.AddCommand(CreateSearchCommand(apiClient));

        return leadsCommand;
    }

    /// <summary>
    /// Creates the 'leads list' command
    /// </summary>
    private static Command CreateListCommand(PipedriveApiClient apiClient)
    {
        var listCommand = new Command("list", "List all leads");

        var limitOption = new Option<int?>(
            aliases: new[] { "--limit", "-l" },
            description: "Number of leads to return (default: 100)");

        var startOption = new Option<int?>(
            aliases: new[] { "--start", "-s" },
            description: "Pagination start (default: 0)");

        var statusOption = new Option<string>(
            aliases: new[] { "--status" },
            description: "Filter by status: active, archived, or all (default: active)",
            getDefaultValue: () => "active")
            .FromAmong("active", "archived", "all");

        listCommand.AddOption(limitOption);
        listCommand.AddOption(startOption);
        listCommand.AddOption(statusOption);

        listCommand.SetHandler(async (limit, start, status) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                // Convert user-friendly status to API parameter
                var archivedStatus = status switch
                {
                    "active" => "not_archived",
                    "archived" => "archived",
                    "all" => "all",
                    _ => "not_archived"
                };

                var statusLabel = status == "active" ? "active " : (status == "archived" ? "archived " : "");

                await AnsiConsole.Status()
                    .StartAsync($"Fetching {statusLabel}leads...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        var response = await apiClient.GetLeadsAsync(limit, start, archivedStatus);

                        if (response?.Success == true && response.Data != null)
                        {
                            ctx.Status("Formatting results...");

                            var table = new Table();
                            table.Border(TableBorder.Rounded);
                            table.AddColumn("ID");
                            table.AddColumn("Title");
                            table.AddColumn("Value");
                            table.AddColumn("Person/Org ID");
                            table.AddColumn("Owner ID");
                            table.AddColumn("Added");

                            foreach (var lead in response.Data)
                            {
                                var valueDisplay = lead.Value != null
                                    ? $"{lead.Value.Currency} {lead.Value.Amount:N2}"
                                    : "-";

                                var entityId = lead.PersonId?.ToString()
                                    ?? lead.OrganizationId?.ToString()
                                    ?? "-";

                                table.AddRow(
                                    Markup.Escape(lead.Id ?? "-"),
                                    Markup.Escape(lead.Title ?? "-"),
                                    valueDisplay,
                                    entityId,
                                    lead.OwnerId?.ToString() ?? "-",
                                    lead.AddTime ?? "-"
                                );
                            }

                            AnsiConsole.Write(table);

                            if (response.AdditionalData?.Pagination != null)
                            {
                                var pagination = response.AdditionalData.Pagination;
                                AnsiConsole.MarkupLine($"\n[dim]Showing {pagination.Start + 1}-{pagination.Start + response.Data.Count} " +
                                    $"| More available: {pagination.MoreItemsInCollection}[/]");
                            }

                            AnsiConsole.MarkupLine($"\n[green]✓[/] Found {response.Data.Count} lead(s)");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch leads: {Markup.Escape(response?.Error ?? "Unknown error")}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, limitOption, startOption, statusOption);

        return listCommand;
    }

    /// <summary>
    /// Creates the 'leads get' command
    /// </summary>
    private static Command CreateGetCommand(PipedriveApiClient apiClient)
    {
        var getCommand = new Command("get", "Get a specific lead by ID");

        var idArgument = new Argument<string>("id", "Lead ID");
        getCommand.AddArgument(idArgument);

        getCommand.SetHandler(async (id) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Fetching lead {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.GetLeadByIdAsync(id);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    var lead = response.Data;

                    var panel = new Panel(new Markup(
                        $"[bold]Title:[/] {Markup.Escape(lead.Title ?? "")}\n" +
                        $"[bold]ID:[/] {Markup.Escape(lead.Id ?? "")}\n" +
                        $"[bold]Person ID:[/] {lead.PersonId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Organization ID:[/] {lead.OrganizationId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Owner ID:[/] {lead.OwnerId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Value:[/] {(lead.Value != null ? $"{lead.Value.Currency} {lead.Value.Amount:N2}" : "N/A")}\n" +
                        $"[bold]Expected Close Date:[/] {lead.ExpectedCloseDate ?? "N/A"}\n" +
                        $"[bold]CC Email:[/] {Markup.Escape(lead.CcEmail ?? "N/A")}\n" +
                        $"[bold]Was Seen:[/] {lead.WasSeen}\n" +
                        $"[bold]Added:[/] {lead.AddTime}\n" +
                        $"[bold]Updated:[/] {lead.UpdateTime}"))
                    {
                        Header = new PanelHeader($"[green]Lead: {Markup.Escape(lead.Title ?? "")}[/]"),
                        Border = BoxBorder.Rounded
                    };

                    AnsiConsole.Write(panel);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch lead: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument);

        return getCommand;
    }

    /// <summary>
    /// Creates the 'leads create' command
    /// </summary>
    private static Command CreateCreateCommand(PipedriveApiClient apiClient)
    {
        var createCommand = new Command("create", "Create a new lead");

        var titleOption = new Option<string>(
            aliases: new[] { "--title", "-t" },
            description: "Lead title (required)")
        { IsRequired = true };

        var personIdOption = new Option<int?>(
            aliases: new[] { "--person-id", "-p" },
            description: "Person ID");

        var orgIdOption = new Option<int?>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID");

        var valueOption = new Option<decimal?>(
            aliases: new[] { "--value", "-v" },
            description: "Lead value amount");

        var currencyOption = new Option<string?>(
            aliases: new[] { "--currency", "-c" },
            description: "Currency code (e.g., USD, EUR)");

        var expectedCloseDateOption = new Option<string?>(
            aliases: new[] { "--expected-close-date", "-d" },
            description: "Expected close date (YYYY-MM-DD)");

        createCommand.AddOption(titleOption);
        createCommand.AddOption(personIdOption);
        createCommand.AddOption(orgIdOption);
        createCommand.AddOption(valueOption);
        createCommand.AddOption(currencyOption);
        createCommand.AddOption(expectedCloseDateOption);

        createCommand.SetHandler(async (title, personId, orgId, value, currency, expectedCloseDate) =>
        {
            try
            {
                if (!personId.HasValue && !orgId.HasValue)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] Either --person-id or --org-id must be specified");
                    return;
                }

                await apiClient.InitializeAsync();

                var lead = new Lead
                {
                    Title = title,
                    PersonId = personId,
                    OrganizationId = orgId,
                    ExpectedCloseDate = expectedCloseDate
                };

                if (value.HasValue && !string.IsNullOrWhiteSpace(currency))
                {
                    lead.Value = new LeadValue
                    {
                        Amount = value.Value,
                        Currency = currency
                    };
                }

                var response = await AnsiConsole.Status()
                    .StartAsync("Creating lead...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.CreateLeadAsync(lead);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Lead created successfully");
                    AnsiConsole.MarkupLine($"[dim]ID:[/] {Markup.Escape(response.Data.Id ?? "")}");
                    AnsiConsole.MarkupLine($"[dim]Title:[/] {Markup.Escape(response.Data.Title ?? "")}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to create lead: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, titleOption, personIdOption, orgIdOption, valueOption, currencyOption, expectedCloseDateOption);

        return createCommand;
    }

    /// <summary>
    /// Creates the 'leads update' command
    /// </summary>
    private static Command CreateUpdateCommand(PipedriveApiClient apiClient)
    {
        var updateCommand = new Command("update", "Update an existing lead");

        var idArgument = new Argument<string>("id", "Lead ID");
        updateCommand.AddArgument(idArgument);

        var titleOption = new Option<string?>(
            aliases: new[] { "--title", "-t" },
            description: "New lead title");

        var valueOption = new Option<decimal?>(
            aliases: new[] { "--value", "-v" },
            description: "New lead value amount");

        var currencyOption = new Option<string?>(
            aliases: new[] { "--currency", "-c" },
            description: "New currency code");

        var expectedCloseDateOption = new Option<string?>(
            aliases: new[] { "--expected-close-date", "-d" },
            description: "New expected close date (YYYY-MM-DD)");

        updateCommand.AddOption(titleOption);
        updateCommand.AddOption(valueOption);
        updateCommand.AddOption(currencyOption);
        updateCommand.AddOption(expectedCloseDateOption);

        updateCommand.SetHandler(async (id, title, value, currency, expectedCloseDate) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title) && !value.HasValue &&
                    string.IsNullOrWhiteSpace(currency) && string.IsNullOrWhiteSpace(expectedCloseDate))
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] At least one field must be specified to update");
                    return;
                }

                // Validate that value and currency are provided together
                if (value.HasValue && string.IsNullOrWhiteSpace(currency))
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] Currency (--currency) is required when specifying a value");
                    return;
                }

                if (!value.HasValue && !string.IsNullOrWhiteSpace(currency))
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] Value amount (--value) is required when specifying a currency");
                    return;
                }

                await apiClient.InitializeAsync();

                var lead = new Lead();

                if (!string.IsNullOrWhiteSpace(title)) lead.Title = title;
                if (!string.IsNullOrWhiteSpace(expectedCloseDate)) lead.ExpectedCloseDate = expectedCloseDate;

                // Only set Value if both value and currency are provided
                if (value.HasValue && !string.IsNullOrWhiteSpace(currency))
                {
                    lead.Value = new LeadValue
                    {
                        Amount = value.Value,
                        Currency = currency
                    };
                }

                var response = await AnsiConsole.Status()
                    .StartAsync($"Updating lead {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.UpdateLeadAsync(id, lead);
                    });

                if (response?.Success == true)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Lead updated successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to update lead: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument, titleOption, valueOption, currencyOption, expectedCloseDateOption);

        return updateCommand;
    }

    /// <summary>
    /// Creates the 'leads delete' command
    /// </summary>
    private static Command CreateDeleteCommand(PipedriveApiClient apiClient)
    {
        var deleteCommand = new Command("delete", "Delete a lead");

        var idArgument = new Argument<string>("id", "Lead ID");
        deleteCommand.AddArgument(idArgument);

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Skip confirmation prompt");

        deleteCommand.AddOption(forceOption);

        deleteCommand.SetHandler(async (id, force) =>
        {
            try
            {
                if (!force)
                {
                    var confirm = AnsiConsole.Confirm($"Are you sure you want to delete lead {id}?");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                        return;
                    }
                }

                await apiClient.InitializeAsync();

                var success = await AnsiConsole.Status()
                    .StartAsync($"Deleting lead {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.DeleteLeadAsync(id);
                    });

                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Lead {id} deleted successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete lead {id}");
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
    /// Creates the 'leads search' command
    /// </summary>
    private static Command CreateSearchCommand(PipedriveApiClient apiClient)
    {
        var searchCommand = new Command("search", "Search for leads");

        var termArgument = new Argument<string>("term", "Search term");
        searchCommand.AddArgument(termArgument);

        var limitOption = new Option<int?>(
            aliases: new[] { "--limit", "-l" },
            description: "Maximum number of results (default: 100)");

        searchCommand.AddOption(limitOption);

        searchCommand.SetHandler(async (term, limit) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Searching for '{term}'...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.SearchLeadsAsync(term, limit);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    if (response.Data.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[yellow]No leads found matching '{term}'[/]");
                        return;
                    }

                    var table = new Table();
                    table.Border(TableBorder.Rounded);
                    table.AddColumn("ID");
                    table.AddColumn("Title");
                    table.AddColumn("Value");
                    table.AddColumn("Owner ID");

                    foreach (var lead in response.Data)
                    {
                        var valueDisplay = lead.Value != null
                            ? $"{lead.Value.Currency} {lead.Value.Amount:N2}"
                            : "-";

                        table.AddRow(
                            Markup.Escape(lead.Id ?? "-"),
                            Markup.Escape(lead.Title ?? "-"),
                            valueDisplay,
                            lead.OwnerId?.ToString() ?? "-"
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"\n[green]✓[/] Found {response.Data.Count} lead(s) matching '{term}'");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Search failed: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, termArgument, limitOption);

        return searchCommand;
    }
}
