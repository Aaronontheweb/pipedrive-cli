using System.CommandLine;
using PipedriveCLI.Models;
using PipedriveCLI.Services;
using Spectre.Console;

namespace PipedriveCLI.Commands;

/// <summary>
/// Commands for managing Pipedrive deals
/// </summary>
public static class DealsCommands
{
    /// <summary>
    /// Creates the root 'deals' command with all subcommands
    /// </summary>
    public static Command CreateDealsCommand(PipedriveApiClient apiClient)
    {
        var dealsCommand = new Command("deals", "Manage Pipedrive deals");

        // Add subcommands
        dealsCommand.AddCommand(CreateListCommand(apiClient));
        dealsCommand.AddCommand(CreateGetCommand(apiClient));
        dealsCommand.AddCommand(CreateCreateCommand(apiClient));
        dealsCommand.AddCommand(CreateUpdateCommand(apiClient));
        dealsCommand.AddCommand(CreateDeleteCommand(apiClient));

        return dealsCommand;
    }

    /// <summary>
    /// Creates the 'deals list' command
    /// </summary>
    private static Command CreateListCommand(PipedriveApiClient apiClient)
    {
        var listCommand = new Command("list", "List all deals");

        var limitOption = new Option<int?>(
            aliases: new[] { "--limit", "-l" },
            description: "Number of deals to return (default: 100)");

        var startOption = new Option<int?>(
            aliases: new[] { "--start", "-s" },
            description: "Pagination start (default: 0)");

        var statusOption = new Option<string?>(
            aliases: new[] { "--status" },
            description: "Filter by status (open, won, lost, deleted, all_not_deleted)");

        listCommand.AddOption(limitOption);
        listCommand.AddOption(startOption);
        listCommand.AddOption(statusOption);

        listCommand.SetHandler(async (limit, start, status) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                await AnsiConsole.Status()
                    .StartAsync("Fetching deals...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        var response = await apiClient.GetDealsAsync(limit, start, status);

                        if (response?.Success == true && response.Data != null)
                        {
                            ctx.Status("Formatting results...");

                            var table = new Table();
                            table.Border(TableBorder.Rounded);
                            table.AddColumn("ID");
                            table.AddColumn("Title");
                            table.AddColumn("Value");
                            table.AddColumn("Status");
                            table.AddColumn("Stage ID");
                            table.AddColumn("Person/Org ID");
                            table.AddColumn("Expected Close");

                            foreach (var deal in response.Data)
                            {
                                var valueDisplay = $"{deal.Currency} {deal.Value:N2}";

                                var entityId = deal.PersonId?.ToString()
                                    ?? deal.OrgId?.ToString()
                                    ?? "-";

                                table.AddRow(
                                    deal.Id.ToString(),
                                    deal.Title ?? "-",
                                    valueDisplay,
                                    deal.Status ?? "-",
                                    deal.StageId?.ToString() ?? "-",
                                    entityId,
                                    deal.ExpectedCloseDate ?? "-"
                                );
                            }

                            AnsiConsole.Write(table);

                            if (response.AdditionalData?.Pagination != null)
                            {
                                var pagination = response.AdditionalData.Pagination;
                                AnsiConsole.MarkupLine($"\n[dim]Showing {pagination.Start + 1}-{pagination.Start + response.Data.Count} " +
                                    $"| More available: {pagination.MoreItemsInCollection}[/]");
                            }

                            AnsiConsole.MarkupLine($"\n[green]✓[/] Found {response.Data.Count} deal(s)");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch deals: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
    /// Creates the 'deals get' command
    /// </summary>
    private static Command CreateGetCommand(PipedriveApiClient apiClient)
    {
        var getCommand = new Command("get", "Get a specific deal by ID");

        var idArgument = new Argument<int>("id", "Deal ID");
        getCommand.AddArgument(idArgument);

        getCommand.SetHandler(async (id) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Fetching deal {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.GetDealByIdAsync(id);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    var deal = response.Data;

                    var panel = new Panel(new Markup(
                        $"[bold]Title:[/] {Markup.Escape(deal.Title ?? "")}\n" +
                        $"[bold]ID:[/] {deal.Id}\n" +
                        $"[bold]Value:[/] {Markup.Escape(deal.Currency ?? "")} {deal.Value:N2}\n" +
                        $"[bold]Status:[/] {Markup.Escape(deal.Status ?? "N/A")}\n" +
                        $"[bold]Stage ID:[/] {deal.StageId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Person ID:[/] {deal.PersonId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Organization ID:[/] {deal.OrgId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Probability:[/] {(deal.Probability.HasValue ? $"{deal.Probability.Value}%" : "N/A")}\n" +
                        $"[bold]Expected Close Date:[/] {Markup.Escape(deal.ExpectedCloseDate ?? "N/A")}\n" +
                        $"[bold]Added:[/] {Markup.Escape(deal.AddTime ?? "N/A")}\n" +
                        $"[bold]Updated:[/] {Markup.Escape(deal.UpdateTime ?? "N/A")}"))
                    {
                        Header = new PanelHeader($"[green]Deal: {Markup.Escape(deal.Title ?? "")}[/]"),
                        Border = BoxBorder.Rounded
                    };

                    AnsiConsole.Write(panel);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch deal: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
    /// Creates the 'deals create' command
    /// </summary>
    private static Command CreateCreateCommand(PipedriveApiClient apiClient)
    {
        var createCommand = new Command("create", "Create a new deal");

        var titleOption = new Option<string>(
            aliases: new[] { "--title", "-t" },
            description: "Deal title (required)")
        { IsRequired = true };

        var valueOption = new Option<decimal>(
            aliases: new[] { "--value", "-v" },
            description: "Deal value amount (required)")
        { IsRequired = true };

        var currencyOption = new Option<string>(
            aliases: new[] { "--currency", "-c" },
            description: "Currency code (e.g., USD, EUR)",
            getDefaultValue: () => "USD");

        var personIdOption = new Option<int?>(
            aliases: new[] { "--person-id", "-p" },
            description: "Person ID");

        var orgIdOption = new Option<int?>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID");

        var stageIdOption = new Option<int?>(
            aliases: new[] { "--stage-id" },
            description: "Pipeline stage ID");

        var expectedCloseDateOption = new Option<string?>(
            aliases: new[] { "--expected-close-date", "-d" },
            description: "Expected close date (YYYY-MM-DD)");

        createCommand.AddOption(titleOption);
        createCommand.AddOption(valueOption);
        createCommand.AddOption(currencyOption);
        createCommand.AddOption(personIdOption);
        createCommand.AddOption(orgIdOption);
        createCommand.AddOption(stageIdOption);
        createCommand.AddOption(expectedCloseDateOption);

        createCommand.SetHandler(async (title, value, currency, personId, orgId, stageId, expectedCloseDate) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var deal = new Deal
                {
                    Title = title,
                    Value = value,
                    Currency = currency,
                    PersonId = personId,
                    OrgId = orgId,
                    StageId = stageId,
                    ExpectedCloseDate = expectedCloseDate
                };

                var response = await AnsiConsole.Status()
                    .StartAsync("Creating deal...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.CreateDealAsync(deal);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Deal created successfully");
                    AnsiConsole.MarkupLine($"[dim]ID:[/] {response.Data.Id}");
                    AnsiConsole.MarkupLine($"[dim]Title:[/] {Markup.Escape(response.Data.Title ?? "")}");
                    AnsiConsole.MarkupLine($"[dim]Value:[/] {Markup.Escape(response.Data.Currency ?? "")} {response.Data.Value:N2}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to create deal: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, titleOption, valueOption, currencyOption, personIdOption, orgIdOption, stageIdOption, expectedCloseDateOption);

        return createCommand;
    }

    /// <summary>
    /// Creates the 'deals update' command
    /// </summary>
    private static Command CreateUpdateCommand(PipedriveApiClient apiClient)
    {
        var updateCommand = new Command("update", "Update an existing deal");

        var idArgument = new Argument<int>("id", "Deal ID");
        updateCommand.AddArgument(idArgument);

        var titleOption = new Option<string?>(
            aliases: new[] { "--title", "-t" },
            description: "New deal title");

        var valueOption = new Option<decimal?>(
            aliases: new[] { "--value", "-v" },
            description: "New deal value amount");

        var currencyOption = new Option<string?>(
            aliases: new[] { "--currency", "-c" },
            description: "New currency code");

        var stageIdOption = new Option<int?>(
            aliases: new[] { "--stage-id" },
            description: "New pipeline stage ID");

        var statusOption = new Option<string?>(
            aliases: new[] { "--status" },
            description: "New status (open, won, lost, deleted)");

        var expectedCloseDateOption = new Option<string?>(
            aliases: new[] { "--expected-close-date", "-d" },
            description: "New expected close date (YYYY-MM-DD)");

        updateCommand.AddOption(titleOption);
        updateCommand.AddOption(valueOption);
        updateCommand.AddOption(currencyOption);
        updateCommand.AddOption(stageIdOption);
        updateCommand.AddOption(statusOption);
        updateCommand.AddOption(expectedCloseDateOption);

        updateCommand.SetHandler(async (id, title, value, currency, stageId, status, expectedCloseDate) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title) && !value.HasValue &&
                    string.IsNullOrWhiteSpace(currency) && !stageId.HasValue &&
                    string.IsNullOrWhiteSpace(status) && string.IsNullOrWhiteSpace(expectedCloseDate))
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] At least one field must be specified to update");
                    return;
                }

                await apiClient.InitializeAsync();

                var deal = new Deal();

                if (!string.IsNullOrWhiteSpace(title)) deal.Title = title;
                if (value.HasValue) deal.Value = value.Value;
                if (!string.IsNullOrWhiteSpace(currency)) deal.Currency = currency;
                if (stageId.HasValue) deal.StageId = stageId;
                if (!string.IsNullOrWhiteSpace(status)) deal.Status = status;
                if (!string.IsNullOrWhiteSpace(expectedCloseDate)) deal.ExpectedCloseDate = expectedCloseDate;

                var response = await AnsiConsole.Status()
                    .StartAsync($"Updating deal {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.UpdateDealAsync(id, deal);
                    });

                if (response?.Success == true)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Deal updated successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to update deal: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument, titleOption, valueOption, currencyOption, stageIdOption, statusOption, expectedCloseDateOption);

        return updateCommand;
    }

    /// <summary>
    /// Creates the 'deals delete' command
    /// </summary>
    private static Command CreateDeleteCommand(PipedriveApiClient apiClient)
    {
        var deleteCommand = new Command("delete", "Delete a deal");

        var idArgument = new Argument<int>("id", "Deal ID");
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
                    var confirm = AnsiConsole.Confirm($"Are you sure you want to delete deal {id}?");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                        return;
                    }
                }

                await apiClient.InitializeAsync();

                var success = await AnsiConsole.Status()
                    .StartAsync($"Deleting deal {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.DeleteDealAsync(id);
                    });

                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Deal {id} deleted successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete deal {id}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument, forceOption);

        return deleteCommand;
    }
}
