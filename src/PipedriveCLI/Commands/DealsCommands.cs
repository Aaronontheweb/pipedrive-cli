using System.CommandLine;
using System.Text.Json;
using PipedriveCLI.Models;
using PipedriveCLI.Services;
using PipedriveCLI.Utilities;
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
    public static Command CreateDealsCommand(PipedriveApiClient apiClient, CustomFieldCache? fieldCache = null)
    {
        var dealsCommand = new Command("deals", "Manage Pipedrive deals");

        // Add subcommands
        dealsCommand.AddCommand(CreateListCommand(apiClient));
        dealsCommand.AddCommand(CreateGetCommand(apiClient, fieldCache));
        dealsCommand.AddCommand(CreateCreateCommand(apiClient));
        dealsCommand.AddCommand(CreateUpdateCommand(apiClient));
        dealsCommand.AddCommand(CreateDeleteCommand(apiClient));
        dealsCommand.AddCommand(CreateMergeCommand(apiClient));
        dealsCommand.AddCommand(CreateParticipantsCommand(apiClient));
        dealsCommand.AddCommand(CreateAddParticipantCommand(apiClient));
        dealsCommand.AddCommand(CreateRemoveParticipantCommand(apiClient));

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
            description: "Filter by status (open, won, lost, deleted, all_not_deleted)",
            getDefaultValue: () => "open");

        var pipelineIdOption = new Option<int?>(
            aliases: new[] { "--pipeline-id", "-p" },
            description: "Filter by pipeline ID");

        var orgIdOption = new Option<int?>(
            aliases: new[] { "--org-id", "-o" },
            description: "Filter deals by organization ID");

        listCommand.AddOption(limitOption);
        listCommand.AddOption(startOption);
        listCommand.AddOption(statusOption);
        listCommand.AddOption(pipelineIdOption);
        listCommand.AddOption(orgIdOption);

        listCommand.SetHandler(async (context) =>
        {
            var limit = context.ParseResult.GetValueForOption(limitOption);
            var start = context.ParseResult.GetValueForOption(startOption);
            var status = context.ParseResult.GetValueForOption(statusOption);
            var pipelineId = context.ParseResult.GetValueForOption(pipelineIdOption);
            var orgId = context.ParseResult.GetValueForOption(orgIdOption);

            try
            {
                await apiClient.InitializeAsync();

                await AnsiConsole.Status()
                    .StartAsync("Fetching deals...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        PipedriveResponse<List<Deal>>? response;

                        if (orgId.HasValue)
                        {
                            // Use organization-specific endpoint when filtering by org
                            response = await apiClient.GetOrganizationDealsAsync(orgId.Value, limit, start, status);
                        }
                        else
                        {
                            response = await apiClient.GetDealsAsync(limit, start, status, pipelineId);
                        }

                        if (response?.Success == true)
                        {
                            ctx.Status("Formatting results...");

                            var deals = response.Data ?? new List<Deal>();

                            if (deals.Count == 0)
                            {
                                AnsiConsole.MarkupLine("[yellow]No deals found[/]");
                            }
                            else
                            {
                                var table = new Table();
                                table.Border(TableBorder.Rounded);
                                table.AddColumn(new TableColumn("ID").NoWrap());
                                table.AddColumn("Title");
                                table.AddColumn("Value");
                                table.AddColumn("Status");
                                table.AddColumn("Stage ID");
                                table.AddColumn("Person/Org ID");
                                table.AddColumn("Expected Close");

                                foreach (var deal in deals)
                                {
                                    var valueDisplay = $"{deal.Currency} {deal.Value:N2}";

                                    var entityId = deal.PersonId?.ToString()
                                        ?? deal.OrgId?.ToString()
                                        ?? "-";

                                    table.AddRow(
                                        deal.Id.ToString(),
                                        Markup.Escape(deal.Title ?? "-"),
                                        valueDisplay,
                                        Markup.Escape(deal.Status ?? "-"),
                                        deal.StageId?.ToString() ?? "-",
                                        entityId,
                                        Markup.Escape(deal.ExpectedCloseDate ?? "-")
                                    );
                                }

                                AnsiConsole.Write(table);

                                if (response.AdditionalData?.Pagination != null)
                                {
                                    var pagination = response.AdditionalData.Pagination;
                                    AnsiConsole.MarkupLine($"\n[dim]Showing {pagination.Start + 1}-{pagination.Start + deals.Count} " +
                                        $"| More available: {pagination.MoreItemsInCollection}[/]");
                                }
                            }

                            AnsiConsole.MarkupLine($"\n[green]✓[/] Found {deals.Count} deal(s)");
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
        });

        return listCommand;
    }

    /// <summary>
    /// Creates the 'deals get' command
    /// </summary>
    private static Command CreateGetCommand(PipedriveApiClient apiClient, CustomFieldCache? fieldCache = null)
    {
        var getCommand = new Command("get", "Get a specific deal by ID");

        var idArgument = new Argument<int>("id", "Deal ID");
        getCommand.AddArgument(idArgument);

        var jsonOption = new Option<bool>(
            aliases: new[] { "--json" },
            description: "Output raw JSON instead of formatted display");
        getCommand.AddOption(jsonOption);

        var rawKeysOption = new Option<bool>(
            aliases: new[] { "--raw-keys" },
            description: "Display custom field hash keys instead of friendly names");
        getCommand.AddOption(rawKeysOption);

        getCommand.SetHandler(async (id, json, rawKeys) =>
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
                    if (json)
                    {
                        // Output raw JSON
                        var jsonOutput = JsonSerializer.Serialize(response.Data, ApiJsonContext.Default.Deal);
                        Console.WriteLine(jsonOutput);
                    }
                    else
                    {
                        // Output formatted display
                        var deal = response.Data;

                        // Get field names from cache if available and not using raw keys
                        Dictionary<string, string>? fieldNames = null;
                        if (fieldCache != null && !rawKeys)
                        {
                            try
                            {
                                fieldNames = await fieldCache.GetDealFieldNamesAsync();
                            }
                            catch
                            {
                                // If field cache fails, fall back to raw keys
                                fieldNames = null;
                            }
                        }

                        var customFieldsDisplay = CustomFieldHelper.FormatCustomFields(
                            deal.CustomFields,
                            fieldNames,
                            rawKeys);

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
                            $"[bold]CC Email:[/] {Markup.Escape(deal.CcEmail ?? "N/A")}\n" +
                            $"[bold]Added:[/] {Markup.Escape(deal.AddTime ?? "N/A")}\n" +
                            $"[bold]Updated:[/] {Markup.Escape(deal.UpdateTime ?? "N/A")}" +
                            customFieldsDisplay))
                        {
                            Header = new PanelHeader($"[green]Deal: {Markup.Escape(deal.Title ?? "")}[/]"),
                            Border = BoxBorder.Rounded
                        };

                        AnsiConsole.Write(panel);
                    }
                }
                else
                {
                    if (json)
                    {
                        Console.WriteLine($"{{\"success\":false,\"error\":\"{response?.Error ?? "Unknown error"}\"}}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch deal: {Markup.Escape(response?.Error ?? "Unknown error")}");
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument, jsonOption, rawKeysOption);

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
            description: "New expected close date (YYYY-MM-DD), or 'clear' to remove the date");

        var customFieldsOption = new Option<string?>(
            aliases: new[] { "--custom-fields", "-cf" },
            description: "Custom fields to update in format: hash1=value1,hash2=value2");

        var wonTimeOption = new Option<string?>(
            aliases: new[] { "--won-time" },
            description: "Date/time when the deal was won (YYYY-MM-DD or YYYY-MM-DD HH:mm:ss). Use with --status won or on already won deals.");

        var lostTimeOption = new Option<string?>(
            aliases: new[] { "--lost-time" },
            description: "Date/time when the deal was lost (YYYY-MM-DD or YYYY-MM-DD HH:mm:ss). Use with --status lost or on already lost deals.");

        var personIdOption = new Option<int?>(
            aliases: new[] { "--person-id", "-p" },
            description: "Change associated person ID");

        var orgIdOption = new Option<int?>(
            aliases: new[] { "--org-id", "-o" },
            description: "Change associated organization ID");

        var probabilityOption = new Option<int?>(
            aliases: new[] { "--probability" },
            description: "Deal success probability (0-100)");

        updateCommand.AddOption(titleOption);
        updateCommand.AddOption(valueOption);
        updateCommand.AddOption(currencyOption);
        updateCommand.AddOption(stageIdOption);
        updateCommand.AddOption(statusOption);
        updateCommand.AddOption(expectedCloseDateOption);
        updateCommand.AddOption(customFieldsOption);
        updateCommand.AddOption(wonTimeOption);
        updateCommand.AddOption(lostTimeOption);
        updateCommand.AddOption(personIdOption);
        updateCommand.AddOption(orgIdOption);
        updateCommand.AddOption(probabilityOption);

        updateCommand.SetHandler(async context =>
        {
            var id = context.ParseResult.GetValueForArgument(idArgument);
            var title = context.ParseResult.GetValueForOption(titleOption);
            var value = context.ParseResult.GetValueForOption(valueOption);
            var currency = context.ParseResult.GetValueForOption(currencyOption);
            var stageId = context.ParseResult.GetValueForOption(stageIdOption);
            var status = context.ParseResult.GetValueForOption(statusOption);
            var expectedCloseDate = context.ParseResult.GetValueForOption(expectedCloseDateOption);
            var customFields = context.ParseResult.GetValueForOption(customFieldsOption);
            var wonTime = context.ParseResult.GetValueForOption(wonTimeOption);
            var lostTime = context.ParseResult.GetValueForOption(lostTimeOption);
            var personId = context.ParseResult.GetValueForOption(personIdOption);
            var orgId = context.ParseResult.GetValueForOption(orgIdOption);
            var probability = context.ParseResult.GetValueForOption(probabilityOption);

            try
            {
                // Check if user wants to clear the date field
                var clearExpectedCloseDate = IsClearValue(expectedCloseDate);

                if (string.IsNullOrWhiteSpace(title) && !value.HasValue &&
                    string.IsNullOrWhiteSpace(currency) && !stageId.HasValue &&
                    string.IsNullOrWhiteSpace(status) && !clearExpectedCloseDate && string.IsNullOrWhiteSpace(expectedCloseDate) &&
                    string.IsNullOrWhiteSpace(customFields) && string.IsNullOrWhiteSpace(wonTime) &&
                    string.IsNullOrWhiteSpace(lostTime) && !personId.HasValue &&
                    !orgId.HasValue && !probability.HasValue)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] At least one field must be specified to update");
                    return;
                }

                // Validate won-time and lost-time usage
                if (!string.IsNullOrWhiteSpace(wonTime) && status == "lost")
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] Cannot use --won-time with --status lost");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(lostTime) && status == "won")
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] Cannot use --lost-time with --status won");
                    return;
                }

                await apiClient.InitializeAsync();

                var deal = new Deal();
                var fieldsToClears = new List<string>();

                if (!string.IsNullOrWhiteSpace(title)) deal.Title = title;
                if (value.HasValue) deal.Value = value.Value;
                if (!string.IsNullOrWhiteSpace(currency)) deal.Currency = currency;
                if (stageId.HasValue) deal.StageId = stageId;
                if (!string.IsNullOrWhiteSpace(status)) deal.Status = status;
                if (clearExpectedCloseDate)
                    fieldsToClears.Add("expected_close_date");
                else if (!string.IsNullOrWhiteSpace(expectedCloseDate))
                    deal.ExpectedCloseDate = expectedCloseDate;
                if (!string.IsNullOrWhiteSpace(wonTime)) deal.WonTime = NormalizeDateTimeFormat(wonTime);
                if (!string.IsNullOrWhiteSpace(lostTime)) deal.LostTime = NormalizeDateTimeFormat(lostTime);
                if (personId.HasValue) deal.PersonId = personId;
                if (orgId.HasValue) deal.OrgId = orgId;
                if (probability.HasValue) deal.Probability = probability;

                // Parse and set custom fields
                if (!string.IsNullOrWhiteSpace(customFields))
                {
                    deal.CustomFields = CustomFieldHelper.ParseCustomFields(customFields);
                }

                var response = await AnsiConsole.Status()
                    .StartAsync($"Updating deal {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return fieldsToClears.Count > 0
                            ? await apiClient.UpdateDealAsync(id, deal, fieldsToClears)
                            : await apiClient.UpdateDealAsync(id, deal);
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
        });

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
            aliases: new[] { "--force", "-f", "-y" },
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

    /// <summary>
    /// Creates the 'deals merge' command
    /// </summary>
    private static Command CreateMergeCommand(PipedriveApiClient apiClient)
    {
        var mergeCommand = new Command("merge", "Merge two deals");

        var idArgument = new Argument<int>("id", "ID of the deal to be merged (will be deleted)");
        mergeCommand.AddArgument(idArgument);

        var mergeWithIdArgument = new Argument<int>("merge-with-id", "ID of the deal to merge with (takes priority in conflicts)");
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
                    var confirm = AnsiConsole.Confirm($"Are you sure you want to merge deal {id} into deal {mergeWithId}? Deal {id} will be deleted.");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                        return;
                    }
                }

                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Merging deal {id} into {mergeWithId}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.MergeDealAsync(id, mergeWithId);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Deals merged successfully");
                    AnsiConsole.MarkupLine($"[dim]Merged deal ID:[/] {response.Data.Id}");
                    AnsiConsole.MarkupLine($"[dim]Title:[/] {Markup.Escape(response.Data.Title ?? "")}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to merge deals: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument, mergeWithIdArgument, forceOption);

        return mergeCommand;
    }

    /// <summary>
    /// Normalizes date/time input to the format expected by Pipedrive API.
    /// Accepts YYYY-MM-DD or YYYY-MM-DD HH:mm:ss formats.
    /// If only date is provided, appends 12:00:00 as the time.
    /// </summary>
    private static string NormalizeDateTimeFormat(string input)
    {
        // If it's just a date (YYYY-MM-DD), append a default time
        if (input.Length == 10 && !input.Contains(' '))
        {
            return $"{input} 12:00:00";
        }
        return input;
    }

    /// <summary>
    /// Checks if a string value indicates the user wants to clear/null the field.
    /// Accepts "clear", "null", or empty string.
    /// </summary>
    private static bool IsClearValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return value.Equals("clear", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("null", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates the 'deals participants' command to list deal participants
    /// </summary>
    private static Command CreateParticipantsCommand(PipedriveApiClient apiClient)
    {
        var participantsCommand = new Command("participants", "List participants of a deal");

        var dealIdArgument = new Argument<int>("deal-id", "Deal ID");
        participantsCommand.AddArgument(dealIdArgument);

        var limitOption = new Option<int?>(
            aliases: new[] { "--limit", "-l" },
            description: "Number of participants to return (default: 100)");

        participantsCommand.AddOption(limitOption);

        participantsCommand.SetHandler(async (dealId, limit) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Fetching participants for deal {dealId}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.GetDealParticipantsAsync(dealId, limit);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    if (response.Data.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]No participants found for this deal[/]");
                        return;
                    }

                    var table = new Table();
                    table.AddColumn(new TableColumn("ID").NoWrap());
                    table.AddColumn("Person ID");
                    table.AddColumn("Name");
                    table.AddColumn("Email");
                    table.AddColumn("Added");

                    foreach (var participant in response.Data)
                    {
                        var email = participant.Person?.Email?.FirstOrDefault()?.Value ?? "";
                        table.AddRow(
                            participant.Id.ToString(),
                            participant.PersonId?.ToString() ?? "",
                            Markup.Escape(participant.Person?.Name ?? ""),
                            Markup.Escape(email),
                            Markup.Escape(participant.AddTime ?? ""));
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"[dim]Total: {response.Data.Count} participant(s)[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to get participants: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, dealIdArgument, limitOption);

        return participantsCommand;
    }

    /// <summary>
    /// Creates the 'deals add-participant' command
    /// </summary>
    private static Command CreateAddParticipantCommand(PipedriveApiClient apiClient)
    {
        var addParticipantCommand = new Command("add-participant", "Add a participant to a deal");

        var dealIdArgument = new Argument<int>("deal-id", "Deal ID");
        addParticipantCommand.AddArgument(dealIdArgument);

        var personIdOption = new Option<int>(
            aliases: new[] { "--person-id", "-p" },
            description: "Person ID to add as participant")
        { IsRequired = true };

        addParticipantCommand.AddOption(personIdOption);

        addParticipantCommand.SetHandler(async (dealId, personId) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Adding person {personId} to deal {dealId}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.AddDealParticipantAsync(dealId, personId);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Participant added successfully");
                    AnsiConsole.MarkupLine($"[dim]Participant ID:[/] {response.Data.Id}");
                    if (response.Data.Person != null)
                    {
                        AnsiConsole.MarkupLine($"[dim]Person:[/] {Markup.Escape(response.Data.Person.Name ?? "")}");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to add participant: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, dealIdArgument, personIdOption);

        return addParticipantCommand;
    }

    /// <summary>
    /// Creates the 'deals remove-participant' command
    /// </summary>
    private static Command CreateRemoveParticipantCommand(PipedriveApiClient apiClient)
    {
        var removeParticipantCommand = new Command("remove-participant", "Remove a participant from a deal");

        var dealIdArgument = new Argument<int>("deal-id", "Deal ID");
        removeParticipantCommand.AddArgument(dealIdArgument);

        var participantIdOption = new Option<int>(
            aliases: new[] { "--participant-id", "-p" },
            description: "Participant ID to remove (use 'deals participants' to find IDs)")
        { IsRequired = true };

        removeParticipantCommand.AddOption(participantIdOption);

        removeParticipantCommand.SetHandler(async (dealId, participantId) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var success = await AnsiConsole.Status()
                    .StartAsync($"Removing participant {participantId} from deal {dealId}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.RemoveDealParticipantAsync(dealId, participantId);
                    });

                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Participant removed successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to remove participant");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, dealIdArgument, participantIdOption);

        return removeParticipantCommand;
    }
}
