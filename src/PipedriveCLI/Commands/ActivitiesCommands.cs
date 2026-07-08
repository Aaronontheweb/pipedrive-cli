using System.CommandLine;
using System.Globalization;
using PipedriveCLI.Models;
using PipedriveCLI.Services;
using PipedriveCLI.Utilities;
using Spectre.Console;

namespace PipedriveCLI.Commands;

/// <summary>
/// Commands for managing Pipedrive activities
/// </summary>
public static class ActivitiesCommands
{
    /// <summary>
    /// Parses a participants string into a list of ActivityParticipant objects.
    /// Format: "123,456,789" or "123:primary,456,789" where :primary marks the primary participant
    /// </summary>
    private static List<ActivityParticipant>? ParseParticipants(string? participantsInput)
    {
        if (string.IsNullOrWhiteSpace(participantsInput))
            return null;

        var participants = new List<ActivityParticipant>();
        var parts = participantsInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var isPrimary = part.EndsWith(":primary", StringComparison.OrdinalIgnoreCase);
            var personIdStr = isPrimary ? part[..^8] : part; // Remove ":primary" suffix if present

            if (int.TryParse(personIdStr, out var personId))
            {
                participants.Add(new ActivityParticipant
                {
                    PersonId = personId,
                    PrimaryFlag = isPrimary
                });
            }
            else
            {
                throw new ArgumentException($"Invalid person ID: '{personIdStr}'. Expected an integer.");
            }
        }

        return participants.Count > 0 ? participants : null;
    }

    /// <summary>
    /// Creates the root 'activities' command with all subcommands
    /// </summary>
    public static Command CreateActivitiesCommand(PipedriveApiClient apiClient)
    {
        var activitiesCommand = new Command("activities", "Manage Pipedrive activities");

        // Add subcommands
        activitiesCommand.AddCommand(CreateListCommand(apiClient));
        activitiesCommand.AddCommand(CreateGetCommand(apiClient));
        activitiesCommand.AddCommand(CreateCreateCommand(apiClient));
        activitiesCommand.AddCommand(CreateUpdateCommand(apiClient));
        activitiesCommand.AddCommand(CreateDeleteCommand(apiClient));
        activitiesCommand.AddCommand(CreateMarkDoneCommand(apiClient));

        return activitiesCommand;
    }

    /// <summary>
    /// Creates the 'activities list' command
    /// </summary>
    private static Command CreateListCommand(PipedriveApiClient apiClient)
    {
        var listCommand = new Command("list", "List all activities");

        var limitOption = new Option<int?>(
            aliases: new[] { "--limit", "-l" },
            description: "Number of activities to return (default: 100)");

        var startOption = new Option<int?>(
            aliases: new[] { "--start", "-s" },
            description: "Pagination start (default: 0)");

        var cursorOption = new Option<string?>(
            aliases: new[] { "--cursor" },
            description: "Cursor for the next page when using update filters or sorting");

        var doneOption = new Option<bool?>(
            aliases: new[] { "--done", "-d" },
            description: "Filter by done status (true/false, default: false)",
            getDefaultValue: () => false);

        var dealIdOption = new Option<int?>(
            aliases: new[] { "--deal-id" },
            description: "Filter activities by deal ID");

        var personIdOption = new Option<int?>(
            aliases: new[] { "--person-id", "-p" },
            description: "Filter activities by person ID");

        var orgIdOption = new Option<int?>(
            aliases: new[] { "--org-id", "-o" },
            description: "Filter activities by organization ID");

        var overdueOption = new Option<bool>(
            aliases: new[] { "--overdue" },
            description: "Filter activities that are overdue (due date before today)");

        var dueBeforeOption = new Option<string?>(
            aliases: new[] { "--due-before" },
            description: "Filter activities due before the specified date (YYYY-MM-DD)");

        var dueAfterOption = new Option<string?>(
            aliases: new[] { "--due-after" },
            description: "Filter activities due after the specified date (YYYY-MM-DD)");

        var includeArchivedLeadsOption = new Option<bool>(
            aliases: new[] { "--include-archived-leads" },
            description: "Include activities associated with archived leads (excluded by default)",
            getDefaultValue: () => false);

        var updatedSinceOption = new Option<string?>(
            aliases: new[] { "--updated-since" },
            description: "Filter activities updated at or after this RFC3339 timestamp (e.g. 2026-06-24T00:00:00Z)");

        var updatedUntilOption = new Option<string?>(
            aliases: new[] { "--updated-until" },
            description: "Filter activities updated before this RFC3339 timestamp (e.g. 2026-06-24T00:00:00Z)");

        var sortByOption = new Option<string?>(
            aliases: new[] { "--sort-by" },
            description: "Sort activities by id, update_time, add_time, or due_date")
            .FromAmong("id", "update_time", "add_time", "due_date");

        var sortDirOption = new Option<string?>(
            aliases: new[] { "--sort-dir" },
            description: "Sort direction: asc or desc")
            .FromAmong("asc", "desc");

        listCommand.AddOption(limitOption);
        listCommand.AddOption(startOption);
        listCommand.AddOption(cursorOption);
        listCommand.AddOption(doneOption);
        listCommand.AddOption(dealIdOption);
        listCommand.AddOption(personIdOption);
        listCommand.AddOption(orgIdOption);
        listCommand.AddOption(overdueOption);
        listCommand.AddOption(dueBeforeOption);
        listCommand.AddOption(dueAfterOption);
        listCommand.AddOption(includeArchivedLeadsOption);
        listCommand.AddOption(updatedSinceOption);
        listCommand.AddOption(updatedUntilOption);
        listCommand.AddOption(sortByOption);
        listCommand.AddOption(sortDirOption);
        var jsonOption = JsonOutputHelper.CreateJsonOption();
        listCommand.AddOption(jsonOption);

        listCommand.SetHandler(async context =>
        {
            var limit = context.ParseResult.GetValueForOption(limitOption);
            var start = context.ParseResult.GetValueForOption(startOption);
            var cursor = context.ParseResult.GetValueForOption(cursorOption);
            var done = context.ParseResult.GetValueForOption(doneOption);
            var dealId = context.ParseResult.GetValueForOption(dealIdOption);
            var personId = context.ParseResult.GetValueForOption(personIdOption);
            var orgId = context.ParseResult.GetValueForOption(orgIdOption);
            var overdue = context.ParseResult.GetValueForOption(overdueOption);
            var dueBefore = context.ParseResult.GetValueForOption(dueBeforeOption);
            var dueAfter = context.ParseResult.GetValueForOption(dueAfterOption);
            var includeArchivedLeads = context.ParseResult.GetValueForOption(includeArchivedLeadsOption);
            var updatedSince = context.ParseResult.GetValueForOption(updatedSinceOption);
            var updatedUntil = context.ParseResult.GetValueForOption(updatedUntilOption);
            var sortBy = context.ParseResult.GetValueForOption(sortByOption);
            var sortDir = context.ParseResult.GetValueForOption(sortDirOption);
            var json = context.ParseResult.GetValueForOption(jsonOption);

            // Validate that only one entity filter is used at a time
            var filterCount = (dealId.HasValue ? 1 : 0) + (personId.HasValue ? 1 : 0) + (orgId.HasValue ? 1 : 0);
            if (filterCount > 1)
            {
                JsonOutputHelper.WriteErrorOrMarkup(json,
                    "Only one of --deal-id, --person-id, or --org-id can be specified at a time",
                    "[red]Error:[/] Only one of --deal-id, --person-id, or --org-id can be specified at a time");
                return;
            }

            // Parse and validate date filters
            DateOnly? dueBeforeDate = null;
            DateOnly? dueAfterDate = null;

            if (overdue)
            {
                dueBeforeDate = DateOnly.FromDateTime(DateTime.Today);
            }

            if (!string.IsNullOrWhiteSpace(dueBefore))
            {
                if (!DateOnly.TryParseExact(dueBefore, "yyyy-MM-dd", out var parsedDate))
                {
                    JsonOutputHelper.WriteErrorOrMarkup(json,
                        "Invalid date format for --due-before. Expected YYYY-MM-DD",
                        "[red]Error:[/] Invalid date format for --due-before. Expected YYYY-MM-DD");
                    return;
                }
                dueBeforeDate = parsedDate;
            }

            if (!string.IsNullOrWhiteSpace(dueAfter))
            {
                if (!DateOnly.TryParseExact(dueAfter, "yyyy-MM-dd", out var parsedDate))
                {
                    JsonOutputHelper.WriteErrorOrMarkup(json,
                        "Invalid date format for --due-after. Expected YYYY-MM-DD",
                        "[red]Error:[/] Invalid date format for --due-after. Expected YYYY-MM-DD");
                    return;
                }
                dueAfterDate = parsedDate;
            }

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

            try
            {
                await apiClient.InitializeAsync();

                var statusMessage = dealId.HasValue ? $"Fetching activities for deal {dealId}..."
                    : personId.HasValue ? $"Fetching activities for person {personId}..."
                    : orgId.HasValue ? $"Fetching activities for organization {orgId}..."
                    : "Fetching activities...";

                var response = await JsonOutputHelper.FetchAsync(json, statusMessage, async () =>
                {
                    if (dealId.HasValue)
                    {
                        return await apiClient.GetDealActivitiesAsync(dealId.Value, limit, start, done, updatedSince, updatedUntil, sortBy, sortDir, cursor);
                    }

                    if (personId.HasValue)
                    {
                        return await apiClient.GetPersonActivitiesAsync(personId.Value, limit, start, done, updatedSince, updatedUntil, sortBy, sortDir, cursor);
                    }

                    if (orgId.HasValue)
                    {
                        return await apiClient.GetOrganizationActivitiesAsync(orgId.Value, limit, start, done, updatedSince, updatedUntil, sortBy, sortDir, cursor);
                    }

                    return await apiClient.GetActivitiesAsync(limit, start, done, updatedSince, updatedUntil, sortBy, sortDir, cursor);
                });

                if (response?.Success == true)
                {
                    var activities = response.Data ?? new List<Activity>();

                    if (dueBeforeDate.HasValue || dueAfterDate.HasValue)
                    {
                        activities = activities.Where(activity =>
                        {
                            if (string.IsNullOrWhiteSpace(activity.DueDate))
                                return false;

                            if (!DateOnly.TryParseExact(activity.DueDate, "yyyy-MM-dd", out var activityDueDate))
                                return false;

                            if (dueBeforeDate.HasValue && activityDueDate >= dueBeforeDate.Value)
                                return false;

                            if (dueAfterDate.HasValue && activityDueDate <= dueAfterDate.Value)
                                return false;

                            return true;
                        }).ToList();
                    }

                    if (!includeArchivedLeads)
                    {
                        var activitiesWithLeads = activities.Where(a => !string.IsNullOrWhiteSpace(a.LeadId)).ToList();
                        if (activitiesWithLeads.Count > 0)
                        {
                            var archivedLeadIds = new HashSet<string>();

                            var uniqueLeadIds = activitiesWithLeads.Select(a => a.LeadId!).Distinct().ToList();
                            foreach (var leadId in uniqueLeadIds)
                            {
                                try
                                {
                                    var lead = await apiClient.GetLeadByIdAsync(leadId);
                                    if (lead?.Success == true && lead.Data?.IsArchived == true)
                                    {
                                        archivedLeadIds.Add(leadId);
                                    }
                                }
                                catch
                                {
                                    // If we can't fetch the lead, don't filter it out.
                                }
                            }

                            if (archivedLeadIds.Count > 0)
                            {
                                activities = activities.Where(a =>
                                    string.IsNullOrWhiteSpace(a.LeadId) || !archivedLeadIds.Contains(a.LeadId)
                                ).ToList();
                            }
                        }
                    }

                    response.Data = activities;

                    if (json)
                    {
                        JsonOutputHelper.Write(response, ApiJsonContext.Default.PipedriveResponseListActivity);
                        return;
                    }

                    var table = new Table();
                    table.Border(TableBorder.Rounded);
                    table.AddColumn(new TableColumn("ID").NoWrap());
                    table.AddColumn("Subject");
                    table.AddColumn("Type");
                    table.AddColumn("Due Date");
                    table.AddColumn("Done");
                    table.AddColumn(new TableColumn("Association").NoWrap());
                    table.AddColumn("Added");

                    foreach (var activity in activities)
                    {
                        var dueDateTime = !string.IsNullOrWhiteSpace(activity.DueTime)
                            ? $"{activity.DueDate} {activity.DueTime}"
                            : activity.DueDate ?? "-";

                        string entityInfo;
                        if (activity.DealId.HasValue)
                            entityInfo = $"Deal: {activity.DealId}";
                        else if (!string.IsNullOrWhiteSpace(activity.LeadId))
                            entityInfo = $"Lead: {activity.LeadId}";
                        else if (activity.PersonId.HasValue)
                            entityInfo = $"Person: {activity.PersonId}";
                        else if (activity.OrgId.HasValue)
                            entityInfo = $"Org: {activity.OrgId}";
                        else
                            entityInfo = "[dim](orphaned)[/]";

                        var doneStatus = activity.Done ? "[green]✓[/]" : "[red]✗[/]";

                        table.AddRow(
                            activity.Id.ToString(),
                            activity.Subject ?? "-",
                            activity.Type ?? "-",
                            dueDateTime,
                            doneStatus,
                            entityInfo,
                            activity.AddTime ?? "-"
                        );
                    }

                    AnsiConsole.Write(table);

                    if (response.AdditionalData?.Pagination != null)
                    {
                        var pagination = response.AdditionalData.Pagination;
                        AnsiConsole.MarkupLine($"\n[dim]Showing {pagination.Start + 1}-{pagination.Start + (response.Data?.Count ?? 0)} (filtered to {activities.Count}) " +
                            $"| More available: {pagination.MoreItemsInCollection}[/]");
                    }

                    if (!string.IsNullOrWhiteSpace(response.AdditionalData?.NextCursor))
                    {
                        AnsiConsole.MarkupLine($"[dim]Next cursor: {Markup.Escape(response.AdditionalData.NextCursor)}[/]");
                    }

                    AnsiConsole.MarkupLine($"\n[green]✓[/] Found {activities.Count} activit{(activities.Count == 1 ? "y" : "ies")}");
                }
                else if (json)
                {
                    JsonOutputHelper.WriteError(response?.Error);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch activities: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                JsonOutputHelper.WriteException(json, ex);
            }
        });

        return listCommand;
    }

    /// <summary>
    /// Creates the 'activities get' command
    /// </summary>
    private static Command CreateGetCommand(PipedriveApiClient apiClient)
    {
        var getCommand = new Command("get", "Get a specific activity by ID");

        var idArgument = new Argument<int>("id", "Activity ID");
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
                    $"Fetching activity {id}...",
                    () => apiClient.GetActivityByIdAsync(id));

                if (response?.Success == true && response.Data != null)
                {
                    if (json)
                    {
                        JsonOutputHelper.Write(response.Data, ApiJsonContext.Default.Activity);
                        return;
                    }

                    var activity = response.Data;

                    var dueDateTime = !string.IsNullOrWhiteSpace(activity.DueTime)
                        ? $"{activity.DueDate} {activity.DueTime}"
                        : activity.DueDate ?? "N/A";

                    var panel = new Panel(new Markup(
                        $"[bold]Subject:[/] {activity.Subject}\n" +
                        $"[bold]ID:[/] {activity.Id}\n" +
                        $"[bold]Type:[/] {activity.Type ?? "N/A"}\n" +
                        $"[bold]Due:[/] {dueDateTime}\n" +
                        $"[bold]Done:[/] {(activity.Done ? "[green]Yes[/]" : "[red]No[/]")}\n" +
                        $"[bold]Deal ID:[/] {activity.DealId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Person ID:[/] {activity.PersonId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Organization ID:[/] {activity.OrgId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Note:[/] {activity.Note ?? "N/A"}\n" +
                        $"[bold]CC Email:[/] {Markup.Escape(activity.CcEmail ?? "N/A")}\n" +
                        $"[bold]Added:[/] {activity.AddTime}\n" +
                        $"[bold]Updated:[/] {activity.UpdateTime}"))
                    {
                        Header = new PanelHeader($"[green]Activity: {activity.Subject}[/]"),
                        Border = BoxBorder.Rounded
                    };

                    AnsiConsole.Write(panel);
                }
                else if (json)
                {
                    JsonOutputHelper.WriteError(response?.Error);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch activity: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
    /// Creates the 'activities create' command
    /// </summary>
    private static Command CreateCreateCommand(PipedriveApiClient apiClient)
    {
        var createCommand = new Command("create", "Create a new activity");

        var subjectOption = new Option<string>(
            aliases: new[] { "--subject", "-s" },
            description: "Activity subject (required)")
        { IsRequired = true };

        var typeOption = new Option<string>(
            aliases: new[] { "--type", "-t" },
            description: "Activity type (e.g., call, meeting, task, deadline, email, lunch)",
            getDefaultValue: () => "task");

        var dueDateOption = new Option<string>(
            aliases: new[] { "--due-date", "-d" },
            description: "Due date (YYYY-MM-DD) (required)")
        { IsRequired = true };

        var dealIdOption = new Option<int?>(
            aliases: new[] { "--deal-id" },
            description: "Associated deal ID");

        var personIdOption = new Option<int?>(
            aliases: new[] { "--person-id", "-p" },
            description: "Associated person ID");

        var orgIdOption = new Option<int?>(
            aliases: new[] { "--org-id", "-o" },
            description: "Associated organization ID");

        var leadIdOption = new Option<string?>(
            aliases: new[] { "--lead-id", "-l" },
            description: "Associated lead ID (UUID format)");

        var noteOption = new Option<string?>(
            aliases: new[] { "--note", "-n" },
            description: "Activity note");

        var userIdOption = new Option<int?>(
            aliases: new[] { "--user-id", "-u" },
            description: "Assigned user ID");

        var participantsOption = new Option<string?>(
            aliases: new[] { "--participants" },
            description: "Activity participants as comma-separated person IDs (e.g., '123,456,789' or '123:primary,456,789')");

        createCommand.AddOption(subjectOption);
        createCommand.AddOption(typeOption);
        createCommand.AddOption(dueDateOption);
        createCommand.AddOption(dealIdOption);
        createCommand.AddOption(personIdOption);
        createCommand.AddOption(orgIdOption);
        createCommand.AddOption(leadIdOption);
        createCommand.AddOption(noteOption);
        createCommand.AddOption(userIdOption);
        createCommand.AddOption(participantsOption);

        createCommand.SetHandler(async context =>
        {
            var subject = context.ParseResult.GetValueForOption(subjectOption)!;
            var type = context.ParseResult.GetValueForOption(typeOption)!;
            var dueDate = context.ParseResult.GetValueForOption(dueDateOption)!;
            var dealId = context.ParseResult.GetValueForOption(dealIdOption);
            var personId = context.ParseResult.GetValueForOption(personIdOption);
            var orgId = context.ParseResult.GetValueForOption(orgIdOption);
            var leadId = context.ParseResult.GetValueForOption(leadIdOption);
            var note = context.ParseResult.GetValueForOption(noteOption);
            var userId = context.ParseResult.GetValueForOption(userIdOption);
            var participantsInput = context.ParseResult.GetValueForOption(participantsOption);

            try
            {
                await apiClient.InitializeAsync();

                List<ActivityParticipant>? participants = null;
                try
                {
                    participants = ParseParticipants(participantsInput);
                }
                catch (ArgumentException ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
                    return;
                }

                var activity = new Activity
                {
                    Subject = subject,
                    Type = type,
                    DueDate = dueDate,
                    DealId = dealId,
                    PersonId = personId,
                    OrgId = orgId,
                    LeadId = leadId,
                    Note = note,
                    UserId = userId,
                    Participants = participants,
                    Done = false
                };

                var response = await AnsiConsole.Status()
                    .StartAsync("Creating activity...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.CreateActivityAsync(activity);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Activity created successfully");
                    AnsiConsole.MarkupLine($"[dim]ID:[/] {Markup.Escape(response.Data.Id.ToString())}");
                    AnsiConsole.MarkupLine($"[dim]Subject:[/] {Markup.Escape(response.Data.Subject ?? "")}");
                    AnsiConsole.MarkupLine($"[dim]Due:[/] {Markup.Escape(response.Data.DueDate ?? "")} {Markup.Escape(response.Data.DueTime ?? "")}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to create activity: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        });

        return createCommand;
    }

    /// <summary>
    /// Creates the 'activities update' command
    /// </summary>
    private static Command CreateUpdateCommand(PipedriveApiClient apiClient)
    {
        var updateCommand = new Command("update", "Update an existing activity");

        var idArgument = new Argument<int>("id", "Activity ID");
        updateCommand.AddArgument(idArgument);

        var subjectOption = new Option<string?>(
            aliases: new[] { "--subject", "-s" },
            description: "New activity subject");

        var typeOption = new Option<string?>(
            aliases: new[] { "--type", "-t" },
            description: "New activity type");

        var dueDateOption = new Option<string?>(
            aliases: new[] { "--due-date", "-d" },
            description: "New due date (YYYY-MM-DD)");

        var dueTimeOption = new Option<string?>(
            aliases: new[] { "--due-time" },
            description: "New due time (HH:MM)");

        var noteOption = new Option<string?>(
            aliases: new[] { "--note", "-n" },
            description: "New activity note");

        var participantsOption = new Option<string?>(
            aliases: new[] { "--participants" },
            description: "Activity participants as comma-separated person IDs (e.g., '123,456,789' or '123:primary,456,789')");

        var doneOption = new Option<bool?>(
            aliases: new[] { "--done" },
            description: "Mark activity as done (true) or not done (false)");

        updateCommand.AddOption(subjectOption);
        updateCommand.AddOption(typeOption);
        updateCommand.AddOption(dueDateOption);
        updateCommand.AddOption(dueTimeOption);
        updateCommand.AddOption(noteOption);
        updateCommand.AddOption(participantsOption);
        updateCommand.AddOption(doneOption);

        updateCommand.SetHandler(async context =>
        {
            var id = context.ParseResult.GetValueForArgument(idArgument);
            var subject = context.ParseResult.GetValueForOption(subjectOption);
            var type = context.ParseResult.GetValueForOption(typeOption);
            var dueDate = context.ParseResult.GetValueForOption(dueDateOption);
            var dueTime = context.ParseResult.GetValueForOption(dueTimeOption);
            var note = context.ParseResult.GetValueForOption(noteOption);
            var participantsInput = context.ParseResult.GetValueForOption(participantsOption);
            var done = context.ParseResult.GetValueForOption(doneOption);

            try
            {
                List<ActivityParticipant>? participants = null;
                try
                {
                    participants = ParseParticipants(participantsInput);
                }
                catch (ArgumentException ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(type) &&
                    string.IsNullOrWhiteSpace(dueDate) && string.IsNullOrWhiteSpace(dueTime) &&
                    string.IsNullOrWhiteSpace(note) && participants == null && !done.HasValue)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] At least one field must be specified to update");
                    return;
                }

                await apiClient.InitializeAsync();

                var activity = new Activity();

                if (!string.IsNullOrWhiteSpace(subject)) activity.Subject = subject;
                if (!string.IsNullOrWhiteSpace(type)) activity.Type = type;
                if (!string.IsNullOrWhiteSpace(dueDate)) activity.DueDate = dueDate;
                if (!string.IsNullOrWhiteSpace(dueTime)) activity.DueTime = dueTime;
                if (!string.IsNullOrWhiteSpace(note)) activity.Note = note;
                if (participants != null) activity.Participants = participants;
                if (done.HasValue) activity.Done = done.Value;

                var response = await AnsiConsole.Status()
                    .StartAsync($"Updating activity {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.UpdateActivityAsync(id, activity);
                    });

                if (response?.Success == true)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Activity updated successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to update activity: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
    /// Creates the 'activities delete' command
    /// </summary>
    private static Command CreateDeleteCommand(PipedriveApiClient apiClient)
    {
        var deleteCommand = new Command("delete", "Delete an activity");

        var idArgument = new Argument<int>("id", "Activity ID");
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
                    var confirm = AnsiConsole.Confirm($"Are you sure you want to delete activity {id}?");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                        return;
                    }
                }

                await apiClient.InitializeAsync();

                var success = await AnsiConsole.Status()
                    .StartAsync($"Deleting activity {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.DeleteActivityAsync(id);
                    });

                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Activity {id} deleted successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete activity {id}");
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
    /// Creates the 'activities mark-done' command
    /// </summary>
    private static Command CreateMarkDoneCommand(PipedriveApiClient apiClient)
    {
        var markDoneCommand = new Command("mark-done", "Mark an activity as done");

        var idArgument = new Argument<int>("id", "Activity ID");
        markDoneCommand.AddArgument(idArgument);

        markDoneCommand.SetHandler(async (id) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Marking activity {id} as done...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.MarkActivityDoneAsync(id);
                    });

                if (response?.Success == true)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Activity {id} marked as done");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to mark activity as done: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument);

        return markDoneCommand;
    }
}
