using System.CommandLine;
using PipedriveCLI.Models;
using PipedriveCLI.Services;
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

        listCommand.AddOption(limitOption);
        listCommand.AddOption(startOption);
        listCommand.AddOption(doneOption);
        listCommand.AddOption(dealIdOption);
        listCommand.AddOption(personIdOption);
        listCommand.AddOption(orgIdOption);

        listCommand.SetHandler(async context =>
        {
            var limit = context.ParseResult.GetValueForOption(limitOption);
            var start = context.ParseResult.GetValueForOption(startOption);
            var done = context.ParseResult.GetValueForOption(doneOption);
            var dealId = context.ParseResult.GetValueForOption(dealIdOption);
            var personId = context.ParseResult.GetValueForOption(personIdOption);
            var orgId = context.ParseResult.GetValueForOption(orgIdOption);

            // Validate that only one entity filter is used at a time
            var filterCount = (dealId.HasValue ? 1 : 0) + (personId.HasValue ? 1 : 0) + (orgId.HasValue ? 1 : 0);
            if (filterCount > 1)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Only one of --deal-id, --person-id, or --org-id can be specified at a time");
                return;
            }

            try
            {
                await apiClient.InitializeAsync();

                var statusMessage = dealId.HasValue ? $"Fetching activities for deal {dealId}..."
                    : personId.HasValue ? $"Fetching activities for person {personId}..."
                    : orgId.HasValue ? $"Fetching activities for organization {orgId}..."
                    : "Fetching activities...";

                await AnsiConsole.Status()
                    .StartAsync(statusMessage, async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        PipedriveResponse<List<Activity>>? response;

                        if (dealId.HasValue)
                        {
                            response = await apiClient.GetDealActivitiesAsync(dealId.Value, limit, start, done);
                        }
                        else if (personId.HasValue)
                        {
                            response = await apiClient.GetPersonActivitiesAsync(personId.Value, limit, start, done);
                        }
                        else if (orgId.HasValue)
                        {
                            response = await apiClient.GetOrganizationActivitiesAsync(orgId.Value, limit, start, done);
                        }
                        else
                        {
                            response = await apiClient.GetActivitiesAsync(limit, start, done);
                        }

                        if (response?.Success == true && response.Data != null)
                        {
                            ctx.Status("Formatting results...");

                            var table = new Table();
                            table.Border(TableBorder.Rounded);
                            table.AddColumn("ID");
                            table.AddColumn("Subject");
                            table.AddColumn("Type");
                            table.AddColumn("Due Date");
                            table.AddColumn("Done");
                            table.AddColumn("Deal/Person/Org");
                            table.AddColumn("Added");

                            foreach (var activity in response.Data)
                            {
                                var dueDateTime = !string.IsNullOrWhiteSpace(activity.DueTime)
                                    ? $"{activity.DueDate} {activity.DueTime}"
                                    : activity.DueDate ?? "-";

                                var entityInfo = activity.DealId?.ToString()
                                    ?? activity.PersonId?.ToString()
                                    ?? activity.OrgId?.ToString()
                                    ?? "-";

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
                                AnsiConsole.MarkupLine($"\n[dim]Showing {pagination.Start + 1}-{pagination.Start + response.Data.Count} " +
                                    $"| More available: {pagination.MoreItemsInCollection}[/]");
                            }

                            AnsiConsole.MarkupLine($"\n[green]✓[/] Found {response.Data.Count} activit{(response.Data.Count == 1 ? "y" : "ies")}");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch activities: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
    /// Creates the 'activities get' command
    /// </summary>
    private static Command CreateGetCommand(PipedriveApiClient apiClient)
    {
        var getCommand = new Command("get", "Get a specific activity by ID");

        var idArgument = new Argument<int>("id", "Activity ID");
        getCommand.AddArgument(idArgument);

        getCommand.SetHandler(async (id) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Fetching activity {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.GetActivityByIdAsync(id);
                    });

                if (response?.Success == true && response.Data != null)
                {
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
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch activity: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
            aliases: new[] { "--force", "-f" },
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
