using System.CommandLine;
using System.Globalization;
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
        dealsCommand.AddCommand(CreateProductsCommand(apiClient));
        dealsCommand.AddCommand(CreateAddProductCommand(apiClient));
        dealsCommand.AddCommand(CreateRemoveProductCommand(apiClient));
        dealsCommand.AddCommand(CreateClearProductsCommand(apiClient));

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

        var cursorOption = new Option<string?>(
            aliases: new[] { "--cursor" },
            description: "Cursor for the next page when using update filters");

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

        var updatedSinceOption = new Option<string?>(
            aliases: new[] { "--updated-since" },
            description: "Filter deals updated at or after this RFC3339 timestamp (e.g. 2026-06-24T00:00:00Z)");

        var updatedUntilOption = new Option<string?>(
            aliases: new[] { "--updated-until" },
            description: "Filter deals updated before this RFC3339 timestamp (e.g. 2026-06-24T00:00:00Z)");

        // Date range filters for expected_close_date (#146)
        var closingAfterOption = new Option<string?>(
            aliases: new[] { "--closing-after" },
            description: "Filter deals with expected_close_date >= this date (YYYY-MM-DD)");

        var closingBeforeOption = new Option<string?>(
            aliases: new[] { "--closing-before" },
            description: "Filter deals with expected_close_date <= this date (YYYY-MM-DD)");

        // Date range filters for won_time (#164)
        var wonAfterOption = new Option<string?>(
            aliases: new[] { "--won-after" },
            description: "Filter won deals with won_time >= this date (YYYY-MM-DD)");

        var wonBeforeOption = new Option<string?>(
            aliases: new[] { "--won-before" },
            description: "Filter won deals with won_time <= this date (YYYY-MM-DD)");

        listCommand.AddOption(limitOption);
        listCommand.AddOption(startOption);
        listCommand.AddOption(cursorOption);
        listCommand.AddOption(statusOption);
        listCommand.AddOption(pipelineIdOption);
        listCommand.AddOption(orgIdOption);
        listCommand.AddOption(updatedSinceOption);
        listCommand.AddOption(updatedUntilOption);
        listCommand.AddOption(closingAfterOption);
        listCommand.AddOption(closingBeforeOption);
        listCommand.AddOption(wonAfterOption);
        listCommand.AddOption(wonBeforeOption);
        var jsonOption = JsonOutputHelper.CreateJsonOption();
        listCommand.AddOption(jsonOption);

        listCommand.SetHandler(async (context) =>
        {
            var limit = context.ParseResult.GetValueForOption(limitOption);
            var start = context.ParseResult.GetValueForOption(startOption);
            var cursor = context.ParseResult.GetValueForOption(cursorOption);
            var status = context.ParseResult.GetValueForOption(statusOption);
            var pipelineId = context.ParseResult.GetValueForOption(pipelineIdOption);
            var orgId = context.ParseResult.GetValueForOption(orgIdOption);
            var statusWasSpecified = context.ParseResult.FindResultFor(statusOption) != null;

            var updatedSince = context.ParseResult.GetValueForOption(updatedSinceOption);
            var updatedUntil = context.ParseResult.GetValueForOption(updatedUntilOption);
            var closingAfterInput = context.ParseResult.GetValueForOption(closingAfterOption);
            var closingBeforeInput = context.ParseResult.GetValueForOption(closingBeforeOption);
            var wonAfterInput = context.ParseResult.GetValueForOption(wonAfterOption);
            var wonBeforeInput = context.ParseResult.GetValueForOption(wonBeforeOption);
            var json = context.ParseResult.GetValueForOption(jsonOption);

            // Validate existing timestamp formats
            if (!string.IsNullOrWhiteSpace(updatedSince) &&
                !DateTimeOffset.TryParse(updatedSince, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            {
                JsonOutputHelper.WriteErrorOrMarkup(json,
                    "Invalid timestamp format for --updated-since. Expected RFC3339, e.g. 2026-06-24T00:00:00Z",
                    "[red]Error:[/] Invalid timestamp format for --updated-since. Expected RFC3339, e.g. 2026-06-24T00:00:00Z");
                return;
            }

            if (!string.IsNullOrWhiteSpace(updatedUntil) &&
                !DateTimeOffset.TryParse(updatedUntil, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            {
                JsonOutputHelper.WriteErrorOrMarkup(json,
                    "Invalid timestamp format for --updated-until. Expected RFC3339, e.g. 2026-06-24T00:00:00Z",
                    "[red]Error:[/] Invalid timestamp format for --updated-until. Expected RFC3339, e.g. 2026-06-24T00:00:00Z");
                return;
            }

            // Validate date filters
            if (!DateFilterHelper.TryParseDateFilter(closingAfterInput, out var closingAfter))
            {
                JsonOutputHelper.WriteErrorOrMarkup(json,
                    "Invalid date format for --closing-after. Use YYYY-MM-DD (e.g., 2024-01-15).",
                    "[red]Error:[/] Invalid date format for --closing-after. Use YYYY-MM-DD (e.g., 2024-01-15).");
                return;
            }

            if (!DateFilterHelper.TryParseDateFilter(closingBeforeInput, out var closingBefore))
            {
                JsonOutputHelper.WriteErrorOrMarkup(json,
                    "Invalid date format for --closing-before. Use YYYY-MM-DD (e.g., 2024-12-31).",
                    "[red]Error:[/] Invalid date format for --closing-before. Use YYYY-MM-DD (e.g., 2024-12-31).");
                return;
            }

            if (!DateFilterHelper.TryParseDateFilter(wonAfterInput, out var wonAfterDate))
            {
                JsonOutputHelper.WriteErrorOrMarkup(json,
                    "Invalid date format for --won-after. Use YYYY-MM-DD (e.g., 2024-01-15).",
                    "[red]Error:[/] Invalid date format for --won-after. Use YYYY-MM-DD (e.g., 2024-01-15).");
                return;
            }

            if (!DateFilterHelper.TryParseDateFilter(wonBeforeInput, out var wonBeforeDate))
            {
                JsonOutputHelper.WriteErrorOrMarkup(json,
                    "Invalid date format for --won-before. Use YYYY-MM-DD (e.g., 2024-12-31).",
                    "[red]Error:[/] Invalid date format for --won-before. Use YYYY-MM-DD (e.g., 2024-12-31).");
                return;
            }

            // Validate: after must be before before
            if (closingAfter.HasValue && closingBefore.HasValue && closingAfter.Value > closingBefore.Value)
            {
                JsonOutputHelper.WriteErrorOrMarkup(json,
                    "--closing-after must be before --closing-before.",
                    "[red]Error:[/] --closing-after must be before --closing-before.");
                return;
            }

            if (wonAfterDate.HasValue && wonBeforeDate.HasValue && wonAfterDate.Value > wonBeforeDate.Value)
            {
                JsonOutputHelper.WriteErrorOrMarkup(json,
                    "--won-after must be before --won-before.",
                    "[red]Error:[/] --won-after must be before --won-before.");
                return;
            }

            // Convert won date filters to DateTimeOffset (UTC)
            var wonAfter = wonAfterDate.HasValue ? new DateTimeOffset(wonAfterDate.Value.Date, TimeSpan.Zero) : (DateTimeOffset?)null;
            var wonBefore = wonBeforeDate.HasValue ? new DateTimeOffset(wonBeforeDate.Value.Date, TimeSpan.Zero) : (DateTimeOffset?)null;

            var hasClosingFilter = closingAfter.HasValue || closingBefore.HasValue;
            var hasWonFilter = wonAfter.HasValue || wonBefore.HasValue;

            if (hasClosingFilter && hasWonFilter)
            {
                JsonOutputHelper.WriteErrorOrMarkup(json,
                    "Cannot use --closing-after/--closing-before together with --won-after/--won-before.",
                    "[red]Error:[/] Cannot use --closing-after/--closing-before together with --won-after/--won-before.");
                return;
            }

            if (!json && hasWonFilter && status?.Equals("won", StringComparison.OrdinalIgnoreCase) != true)
            {
                AnsiConsole.MarkupLine("[yellow]Warning:[/] Using --won-after/--won-before without --status won. Consider adding --status won for accurate results.[/]");
            }

            // Derive server-side pre-filter bounds from date filters (with 3-day buffer)
            string? dateFilterSince = null, dateFilterUntil = null;
            if (hasClosingFilter || hasWonFilter)
            {
                var boundAfter = hasClosingFilter ? closingAfter : wonAfterDate;
                var boundBefore = hasClosingFilter ? closingBefore : wonBeforeDate;
                var bounds = DateFilterHelper.DeriveUpdateBounds(boundAfter, boundBefore);
                dateFilterSince = bounds.since;
                dateFilterUntil = bounds.until;
            }

            var effectiveStatus = status;
            if (!statusWasSpecified && (!string.IsNullOrWhiteSpace(updatedSince) || !string.IsNullOrWhiteSpace(updatedUntil) || !string.IsNullOrWhiteSpace(dateFilterSince) || !string.IsNullOrWhiteSpace(dateFilterUntil)))
            {
                effectiveStatus = null;
            }

            try
            {
                await apiClient.InitializeAsync();

                bool hasDateFilter = hasClosingFilter || hasWonFilter;

                async Task<(PipedriveResponse<List<Deal>>? Response, List<Deal>? Deals)> FetchDealsAsync(StatusContext? ctx = null)
                {
                    List<Deal> deals;

                    if (hasDateFilter)
                    {
                        // Date filter active: use hybrid filtering with pagination
                        ctx?.Status("Fetching deals with date filter (this may take a moment)...");

                        if (orgId.HasValue)
                        {
                            // Organization-specific: no updated_since/until support on that endpoint.
                            // Fall back to fetch-all + client-side filter.
                            deals = await PaginationHelper.FetchAllOrganizationDealsAsync(apiClient, orgId.Value, effectiveStatus);
                        }
                        else
                        {
                            // Full hybrid filtering: server-side pre-filter + pagination + client-side filter.
                            deals = await PaginationHelper.FetchAllDealsAsync(
                                apiClient,
                                status: effectiveStatus,
                                pipelineId: pipelineId,
                                updatedSince: dateFilterSince,
                                updatedUntil: dateFilterUntil);
                        }

                        if (hasClosingFilter)
                        {
                            deals = DateFilterHelper.FilterByExpectedCloseDate(deals, closingAfter, closingBefore).ToList();
                        }

                        if (hasWonFilter)
                        {
                            deals = DateFilterHelper.FilterByWonTime(deals, wonAfter, wonBefore).ToList();
                        }

                        if (limit.HasValue)
                        {
                            var startIndex = start ?? 0;
                            deals = startIndex >= deals.Count
                                ? new List<Deal>()
                                : deals.Skip(startIndex).Take(limit.Value).ToList();
                        }

                        return (new PipedriveResponse<List<Deal>> { Success = true, Data = deals }, deals);
                    }

                    PipedriveResponse<List<Deal>>? response;

                    if (orgId.HasValue)
                    {
                        response = await apiClient.GetOrganizationDealsAsync(orgId.Value, limit, start, effectiveStatus, updatedSince, updatedUntil, cursor);
                    }
                    else
                    {
                        response = await apiClient.GetDealsAsync(limit, start, effectiveStatus, pipelineId, updatedSince, updatedUntil, cursor);
                    }

                    if (response?.Success != true)
                    {
                        return (response, null);
                    }

                    deals = response.Data ?? new List<Deal>();
                    response.Data = deals;
                    ctx?.Status("Formatting results...");
                    return (response, deals);
                }

                var (response, deals) = json
                    ? await FetchDealsAsync()
                    : await AnsiConsole.Status()
                        .StartAsync("Fetching deals...", async ctx =>
                        {
                            ctx.Spinner(Spinner.Known.Dots);
                            ctx.SpinnerStyle(Style.Parse("green"));
                            return await FetchDealsAsync(ctx);
                        });

                if (response?.Success != true || deals == null)
                {
                    if (json)
                    {
                        JsonOutputHelper.WriteError(response?.Error);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch deals: {Markup.Escape(response?.Error ?? "Unknown error")}");
                    }

                    return;
                }

                if (json)
                {
                    JsonOutputHelper.Write(response, ApiJsonContext.Default.PipedriveResponseListDeal);
                    return;
                }

                if (response.AdditionalData?.Pagination != null)
                {
                    var pagination = response.AdditionalData.Pagination;
                    AnsiConsole.MarkupLine($"\n[dim]Showing {pagination.Start + 1}-{pagination.Start + deals.Count} " +
                        $"| More available: {pagination.MoreItemsInCollection}[/]");
                }

                if (!string.IsNullOrWhiteSpace(response.AdditionalData?.NextCursor))
                {
                    AnsiConsole.MarkupLine($"[dim]Next cursor: {Markup.Escape(response.AdditionalData.NextCursor)}[/]");
                }

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
                }

                AnsiConsole.MarkupLine($"\n[green]✓[/] Found {deals.Count} deal(s)");
            }
            catch (Exception ex)
            {
                JsonOutputHelper.WriteException(json, ex);
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

        var jsonOption = JsonOutputHelper.CreateJsonOption();
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

                var response = await JsonOutputHelper.FetchAsync(
                    json,
                    $"Fetching deal {id}...",
                    () => apiClient.GetDealByIdAsync(id));

                if (response?.Success == true && response.Data != null)
                {
                    if (json)
                    {
                        JsonOutputHelper.Write(response, ApiJsonContext.Default.PipedriveResponseDeal);
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
                            (string.IsNullOrEmpty(deal.WonTime) ? "" : $"[bold]Won Time:[/] {Markup.Escape(deal.WonTime)}\n") +
                            (string.IsNullOrEmpty(deal.LostTime) ? "" : $"[bold]Lost Time:[/] {Markup.Escape(deal.LostTime)}\n") +
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
                        JsonOutputHelper.WriteError(response?.Error);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch deal: {Markup.Escape(response?.Error ?? "Unknown error")}");
                    }
                }
            }
            catch (Exception ex)
            {
                JsonOutputHelper.WriteException(json, ex);
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

        var lostReasonOption = new Option<string?>(
            aliases: new[] { "--lost-reason" },
            description: "Reason why the deal was lost. Use with --status lost or on already lost deals.");

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
        updateCommand.AddOption(lostReasonOption);
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
            var lostReason = context.ParseResult.GetValueForOption(lostReasonOption);
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
                    string.IsNullOrWhiteSpace(lostTime) && string.IsNullOrWhiteSpace(lostReason) &&
                    !personId.HasValue && !orgId.HasValue && !probability.HasValue)
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

                if (!string.IsNullOrWhiteSpace(lostReason) && status == "won")
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] Cannot use --lost-reason with --status won");
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
                if (!string.IsNullOrWhiteSpace(lostReason)) deal.LostReason = lostReason;
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
        var jsonOption = JsonOutputHelper.CreateJsonOption();
        participantsCommand.AddOption(jsonOption);

        participantsCommand.SetHandler(async (dealId, limit, json) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await JsonOutputHelper.FetchAsync(
                    json,
                    $"Fetching participants for deal {dealId}...",
                    () => apiClient.GetDealParticipantsAsync(dealId, limit));

                if (response?.Success == true)
                {
                    response.Data ??= new List<DealParticipant>();

                    if (json)
                    {
                        JsonOutputHelper.Write(response, ApiJsonContext.Default.PipedriveResponseListDealParticipant);
                        return;
                    }

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
                else if (json)
                {
                    JsonOutputHelper.WriteError(response?.Error);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to get participants: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                JsonOutputHelper.WriteException(json, ex);
            }
        }, dealIdArgument, limitOption, jsonOption);

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

    /// <summary>
    /// Creates the 'deals products' command to list products attached to a deal
    /// </summary>
    private static Command CreateProductsCommand(PipedriveApiClient apiClient)
    {
        var productsCommand = new Command("products", "List products attached to a deal");

        var dealIdArgument = new Argument<int>("deal-id", "Deal ID");
        productsCommand.AddArgument(dealIdArgument);

        var limitOption = new Option<int?>(
            aliases: new[] { "--limit", "-l" },
            description: "Number of products to return (default: 100)");

        productsCommand.AddOption(limitOption);
        var jsonOption = JsonOutputHelper.CreateJsonOption();
        productsCommand.AddOption(jsonOption);

        productsCommand.SetHandler(async (dealId, limit, json) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await JsonOutputHelper.FetchAsync(
                    json,
                    $"Fetching products for deal {dealId}...",
                    () => apiClient.GetDealProductsAsync(dealId, limit));

                if (response?.Success == true)
                {
                    var products = response.Data ?? new List<DealProduct>();
                    response.Data = products;

                    if (json)
                    {
                        JsonOutputHelper.Write(response, ApiJsonContext.Default.PipedriveResponseListDealProduct);
                        return;
                    }

                    if (products.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]No products attached to this deal[/]");
                        return;
                    }

                    var table = new Table();
                    table.Border(TableBorder.Rounded);
                    table.AddColumn(new TableColumn("ID").NoWrap());
                    table.AddColumn("Product ID");
                    table.AddColumn("Name");
                    table.AddColumn("Quantity");
                    table.AddColumn("Item Price");
                    table.AddColumn("Sum");
                    table.AddColumn("Discount");

                    foreach (var product in products)
                    {
                        var discountDisplay = product.Discount > 0
                            ? $"{product.Discount}{(product.DiscountType == "percentage" ? "%" : "")}"
                            : "-";

                        table.AddRow(
                            product.Id.ToString(),
                            product.ProductId.ToString(),
                            Markup.Escape(product.Name ?? "-"),
                            product.Quantity.ToString(),
                            $"{product.Currency} {product.ItemPrice:N2}",
                            $"{product.Currency} {product.Sum:N2}",
                            discountDisplay);
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"\n[dim]Total: {products.Count} product(s)[/]");
                }
                else if (json)
                {
                    JsonOutputHelper.WriteError(response?.Error);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to get products: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                JsonOutputHelper.WriteException(json, ex);
            }
        }, dealIdArgument, limitOption, jsonOption);

        return productsCommand;
    }

    /// <summary>
    /// Creates the 'deals add-product' command
    /// </summary>
    private static Command CreateAddProductCommand(PipedriveApiClient apiClient)
    {
        var addProductCommand = new Command("add-product", "Add a product to a deal");

        var dealIdArgument = new Argument<int>("deal-id", "Deal ID");
        addProductCommand.AddArgument(dealIdArgument);

        var productIdOption = new Option<int>(
            aliases: new[] { "--product-id", "-p" },
            description: "Product ID to add")
        { IsRequired = true };

        var quantityOption = new Option<int>(
            aliases: new[] { "--quantity", "-q" },
            description: "Quantity of the product",
            getDefaultValue: () => 1);

        var priceOption = new Option<decimal>(
            aliases: new[] { "--price" },
            description: "Price per item (required)")
        { IsRequired = true };

        var discountOption = new Option<decimal?>(
            aliases: new[] { "--discount" },
            description: "Discount amount");

        var discountTypeOption = new Option<string?>(
            aliases: new[] { "--discount-type" },
            description: "Discount type (percentage or amount)",
            getDefaultValue: () => "percentage");

        var commentsOption = new Option<string?>(
            aliases: new[] { "--comments" },
            description: "Comments about the product");

        addProductCommand.AddOption(productIdOption);
        addProductCommand.AddOption(quantityOption);
        addProductCommand.AddOption(priceOption);
        addProductCommand.AddOption(discountOption);
        addProductCommand.AddOption(discountTypeOption);
        addProductCommand.AddOption(commentsOption);

        addProductCommand.SetHandler(async context =>
        {
            var dealId = context.ParseResult.GetValueForArgument(dealIdArgument);
            var productId = context.ParseResult.GetValueForOption(productIdOption);
            var quantity = context.ParseResult.GetValueForOption(quantityOption);
            var price = context.ParseResult.GetValueForOption(priceOption);
            var discount = context.ParseResult.GetValueForOption(discountOption);
            var discountType = context.ParseResult.GetValueForOption(discountTypeOption);
            var comments = context.ParseResult.GetValueForOption(commentsOption);

            try
            {
                await apiClient.InitializeAsync();

                var request = new AddDealProductRequest
                {
                    ProductId = productId,
                    Quantity = quantity,
                    ItemPrice = price,
                    Discount = discount,
                    DiscountType = discountType,
                    Comments = comments
                };

                var response = await AnsiConsole.Status()
                    .StartAsync($"Adding product {productId} to deal {dealId}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.AddDealProductAsync(dealId, request);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Product added successfully");
                    AnsiConsole.MarkupLine($"[dim]Deal-Product ID:[/] {response.Data.Id}");
                    AnsiConsole.MarkupLine($"[dim]Product:[/] {Markup.Escape(response.Data.Name ?? "")}");
                    AnsiConsole.MarkupLine($"[dim]Quantity:[/] {response.Data.Quantity}");
                    AnsiConsole.MarkupLine($"[dim]Sum:[/] {response.Data.Currency} {response.Data.Sum:N2}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to add product: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        });

        return addProductCommand;
    }

    /// <summary>
    /// Creates the 'deals remove-product' command
    /// </summary>
    private static Command CreateRemoveProductCommand(PipedriveApiClient apiClient)
    {
        var removeProductCommand = new Command("remove-product", "Remove a product from a deal");

        var dealIdArgument = new Argument<int>("deal-id", "Deal ID");
        removeProductCommand.AddArgument(dealIdArgument);

        var productAttachmentIdOption = new Option<int>(
            aliases: new[] { "--id" },
            description: "Deal-product attachment ID (use 'deals products' to find IDs)")
        { IsRequired = true };

        removeProductCommand.AddOption(productAttachmentIdOption);

        removeProductCommand.SetHandler(async (dealId, productAttachmentId) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var success = await AnsiConsole.Status()
                    .StartAsync($"Removing product attachment {productAttachmentId} from deal {dealId}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.RemoveDealProductAsync(dealId, productAttachmentId);
                    });

                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Product removed successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to remove product");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, dealIdArgument, productAttachmentIdOption);

        return removeProductCommand;
    }

    /// <summary>
    /// Creates the 'deals clear-products' command to remove all products from a deal
    /// </summary>
    private static Command CreateClearProductsCommand(PipedriveApiClient apiClient)
    {
        var clearProductsCommand = new Command("clear-products", "Remove all products from a deal");

        var dealIdArgument = new Argument<int>("deal-id", "Deal ID");
        clearProductsCommand.AddArgument(dealIdArgument);

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f", "-y" },
            description: "Skip confirmation prompt");

        clearProductsCommand.AddOption(forceOption);

        clearProductsCommand.SetHandler(async (dealId, force) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                // First, get the list of products
                var productsResponse = await AnsiConsole.Status()
                    .StartAsync($"Fetching products for deal {dealId}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.GetDealProductsAsync(dealId);
                    });

                if (productsResponse?.Success != true)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch products: {Markup.Escape(productsResponse?.Error ?? "Unknown error")}");
                    return;
                }

                var products = productsResponse.Data ?? new List<DealProduct>();

                if (products.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No products attached to this deal[/]");
                    return;
                }

                if (!force)
                {
                    var confirm = AnsiConsole.Confirm($"Are you sure you want to remove all {products.Count} product(s) from deal {dealId}?");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                        return;
                    }
                }

                // Delete each product
                var successCount = 0;
                var failCount = 0;

                await AnsiConsole.Status()
                    .StartAsync($"Removing {products.Count} product(s)...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        foreach (var product in products)
                        {
                            ctx.Status($"Removing product {product.Id} ({product.Name})...");
                            var success = await apiClient.RemoveDealProductAsync(dealId, product.Id);
                            if (success)
                                successCount++;
                            else
                                failCount++;
                        }
                    });

                if (failCount == 0)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] All {successCount} product(s) removed successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Removed {successCount} product(s), {failCount} failed");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, dealIdArgument, forceOption);

        return clearProductsCommand;
    }
}
