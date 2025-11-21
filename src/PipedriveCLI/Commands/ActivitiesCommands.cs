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

        listCommand.AddOption(limitOption);
        listCommand.AddOption(startOption);
        listCommand.AddOption(doneOption);

        listCommand.SetHandler(async (limit, start, done) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                await AnsiConsole.Status()
                    .StartAsync("Fetching activities...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        var response = await apiClient.GetActivitiesAsync(limit, start, done);

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
        }, limitOption, startOption, doneOption);

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

        var noteOption = new Option<string?>(
            aliases: new[] { "--note", "-n" },
            description: "Activity note");

        var userIdOption = new Option<int?>(
            aliases: new[] { "--user-id", "-u" },
            description: "Assigned user ID");

        createCommand.AddOption(subjectOption);
        createCommand.AddOption(typeOption);
        createCommand.AddOption(dueDateOption);
        createCommand.AddOption(dealIdOption);
        createCommand.AddOption(personIdOption);
        createCommand.AddOption(orgIdOption);
        createCommand.AddOption(noteOption);
        createCommand.AddOption(userIdOption);

        createCommand.SetHandler(async (subject, type, dueDate, dealId, personId, orgId, note, userId) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var activity = new Activity
                {
                    Subject = subject,
                    Type = type,
                    DueDate = dueDate,
                    DealId = dealId,
                    PersonId = personId,
                    OrgId = orgId,
                    Note = note,
                    UserId = userId,
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
        }, subjectOption, typeOption, dueDateOption, dealIdOption, personIdOption, orgIdOption, noteOption, userIdOption);

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

        updateCommand.AddOption(subjectOption);
        updateCommand.AddOption(typeOption);
        updateCommand.AddOption(dueDateOption);
        updateCommand.AddOption(dueTimeOption);
        updateCommand.AddOption(noteOption);

        updateCommand.SetHandler(async (id, subject, type, dueDate, dueTime, note) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(type) &&
                    string.IsNullOrWhiteSpace(dueDate) && string.IsNullOrWhiteSpace(dueTime) &&
                    string.IsNullOrWhiteSpace(note))
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
        }, idArgument, subjectOption, typeOption, dueDateOption, dueTimeOption, noteOption);

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
