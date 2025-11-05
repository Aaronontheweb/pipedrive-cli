using System.CommandLine;
using PipedriveCLI.Models;
using PipedriveCLI.Services;
using Spectre.Console;

namespace PipedriveCLI.Commands;

/// <summary>
/// Commands for managing Pipedrive notes
/// </summary>
public static class NotesCommands
{
    /// <summary>
    /// Creates the root 'notes' command with all subcommands
    /// </summary>
    public static Command CreateNotesCommand(PipedriveApiClient apiClient)
    {
        var notesCommand = new Command("notes", "Manage Pipedrive notes");

        // Add subcommands
        notesCommand.AddCommand(CreateListCommand(apiClient));
        notesCommand.AddCommand(CreateGetCommand(apiClient));
        notesCommand.AddCommand(CreateCreateCommand(apiClient));
        notesCommand.AddCommand(CreateUpdateCommand(apiClient));
        notesCommand.AddCommand(CreateDeleteCommand(apiClient));

        return notesCommand;
    }

    /// <summary>
    /// Creates the 'notes list' command
    /// </summary>
    private static Command CreateListCommand(PipedriveApiClient apiClient)
    {
        var listCommand = new Command("list", "List all notes");

        var limitOption = new Option<int?>(
            aliases: new[] { "--limit", "-l" },
            description: "Number of notes to return (default: 100)");

        var startOption = new Option<int?>(
            aliases: new[] { "--start", "-s" },
            description: "Pagination start (default: 0)");

        listCommand.AddOption(limitOption);
        listCommand.AddOption(startOption);

        listCommand.SetHandler(async (limit, start) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                await AnsiConsole.Status()
                    .StartAsync("Fetching notes...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        var response = await apiClient.GetNotesAsync(limit, start);

                        if (response?.Success == true && response.Data != null)
                        {
                            ctx.Status("Formatting results...");

                            var table = new Table();
                            table.Border(TableBorder.Rounded);
                            table.AddColumn("ID");
                            table.AddColumn("Content Preview");
                            table.AddColumn("Deal/Person/Org/Lead");
                            table.AddColumn("User ID");
                            table.AddColumn("Added");

                            foreach (var note in response.Data)
                            {
                                // Truncate and sanitize content for display
                                var contentPreview = note.Content ?? "-";
                                if (contentPreview.Length > 50)
                                {
                                    contentPreview = contentPreview.Substring(0, 50) + "...";
                                }
                                contentPreview = Markup.Escape(contentPreview.Replace("\n", " ").Replace("\r", ""));

                                var entityInfo = note.DealId?.ToString()
                                    ?? note.PersonId?.ToString()
                                    ?? note.OrgId?.ToString()
                                    ?? note.LeadId
                                    ?? note.ProjectId?.ToString()
                                    ?? "-";

                                table.AddRow(
                                    note.Id.ToString(),
                                    contentPreview,
                                    entityInfo,
                                    note.UserId?.ToString() ?? "-",
                                    note.AddTime ?? "-"
                                );
                            }

                            AnsiConsole.Write(table);

                            if (response.AdditionalData?.Pagination != null)
                            {
                                var pagination = response.AdditionalData.Pagination;
                                AnsiConsole.MarkupLine($"\n[dim]Showing {pagination.Start + 1}-{pagination.Start + response.Data.Count} " +
                                    $"| More available: {pagination.MoreItemsInCollection}[/]");
                            }

                            AnsiConsole.MarkupLine($"\n[green]✓[/] Found {response.Data.Count} note(s)");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch notes: {Markup.Escape(response?.Error ?? "Unknown error")}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, limitOption, startOption);

        return listCommand;
    }

    /// <summary>
    /// Creates the 'notes get' command
    /// </summary>
    private static Command CreateGetCommand(PipedriveApiClient apiClient)
    {
        var getCommand = new Command("get", "Get a specific note by ID");

        var idArgument = new Argument<int>("id", "Note ID");
        getCommand.AddArgument(idArgument);

        getCommand.SetHandler(async (id) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Fetching note {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.GetNoteByIdAsync(id);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    var note = response.Data;
                    var content = Markup.Escape(note.Content ?? "");

                    var panel = new Panel(new Markup(
                        $"[bold]ID:[/] {note.Id}\n" +
                        $"[bold]Content:[/]\n{content}\n\n" +
                        $"[bold]Deal ID:[/] {note.DealId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Person ID:[/] {note.PersonId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Organization ID:[/] {note.OrgId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Lead ID:[/] {note.LeadId ?? "N/A"}\n" +
                        $"[bold]Project ID:[/] {note.ProjectId?.ToString() ?? "N/A"}\n" +
                        $"[bold]User ID:[/] {note.UserId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Pinned to Deal:[/] {note.PinnedToDealFlag ?? false}\n" +
                        $"[bold]Pinned to Person:[/] {note.PinnedToPersonFlag ?? false}\n" +
                        $"[bold]Pinned to Org:[/] {note.PinnedToOrganizationFlag ?? false}\n" +
                        $"[bold]Pinned to Lead:[/] {note.PinnedToLeadFlag ?? false}\n" +
                        $"[bold]Added:[/] {note.AddTime}\n" +
                        $"[bold]Updated:[/] {note.UpdateTime}"))
                    {
                        Header = new PanelHeader($"[green]Note {note.Id}[/]"),
                        Border = BoxBorder.Rounded
                    };

                    AnsiConsole.Write(panel);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch note: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
    /// Creates the 'notes create' command
    /// </summary>
    private static Command CreateCreateCommand(PipedriveApiClient apiClient)
    {
        var createCommand = new Command("create", "Create a new note");

        var contentOption = new Option<string>(
            aliases: new[] { "--content", "-c" },
            description: "Note content (required)")
        { IsRequired = true };

        var dealIdOption = new Option<int?>(
            aliases: new[] { "--deal-id", "-d" },
            description: "Deal ID");

        var personIdOption = new Option<int?>(
            aliases: new[] { "--person-id", "-p" },
            description: "Person ID");

        var orgIdOption = new Option<int?>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID");

        var leadIdOption = new Option<string?>(
            aliases: new[] { "--lead-id", "-l" },
            description: "Lead ID");

        var projectIdOption = new Option<int?>(
            aliases: new[] { "--project-id" },
            description: "Project ID");

        createCommand.AddOption(contentOption);
        createCommand.AddOption(dealIdOption);
        createCommand.AddOption(personIdOption);
        createCommand.AddOption(orgIdOption);
        createCommand.AddOption(leadIdOption);
        createCommand.AddOption(projectIdOption);

        createCommand.SetHandler(async (content, dealId, personId, orgId, leadId, projectId) =>
        {
            try
            {
                // Validate at least one association is provided
                if (!dealId.HasValue && !personId.HasValue && !orgId.HasValue &&
                    string.IsNullOrWhiteSpace(leadId) && !projectId.HasValue)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] At least one of --deal-id, --person-id, --org-id, --lead-id, or --project-id must be specified");
                    return;
                }

                await apiClient.InitializeAsync();

                var note = new Note
                {
                    Content = content,
                    DealId = dealId,
                    PersonId = personId,
                    OrgId = orgId,
                    LeadId = leadId,
                    ProjectId = projectId
                };

                var response = await AnsiConsole.Status()
                    .StartAsync("Creating note...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.CreateNoteAsync(note);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Note created successfully");
                    AnsiConsole.MarkupLine($"[dim]ID:[/] {response.Data.Id}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to create note: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, contentOption, dealIdOption, personIdOption, orgIdOption, leadIdOption, projectIdOption);

        return createCommand;
    }

    /// <summary>
    /// Creates the 'notes update' command
    /// </summary>
    private static Command CreateUpdateCommand(PipedriveApiClient apiClient)
    {
        var updateCommand = new Command("update", "Update an existing note");

        var idArgument = new Argument<int>("id", "Note ID");
        updateCommand.AddArgument(idArgument);

        var contentOption = new Option<string?>(
            aliases: new[] { "--content", "-c" },
            description: "New note content");

        updateCommand.AddOption(contentOption);

        updateCommand.SetHandler(async (id, content) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] At least one field must be specified to update");
                    return;
                }

                await apiClient.InitializeAsync();

                var note = new Note { Content = content };

                var response = await AnsiConsole.Status()
                    .StartAsync($"Updating note {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.UpdateNoteAsync(id, note);
                    });

                if (response?.Success == true)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Note updated successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to update note: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument, contentOption);

        return updateCommand;
    }

    /// <summary>
    /// Creates the 'notes delete' command
    /// </summary>
    private static Command CreateDeleteCommand(PipedriveApiClient apiClient)
    {
        var deleteCommand = new Command("delete", "Delete a note");

        var idArgument = new Argument<int>("id", "Note ID");
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
                    var confirm = AnsiConsole.Confirm($"Are you sure you want to delete note {id}?");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                        return;
                    }
                }

                await apiClient.InitializeAsync();

                var success = await AnsiConsole.Status()
                    .StartAsync($"Deleting note {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.DeleteNoteAsync(id);
                    });

                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Note {id} deleted successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete note {id}");
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
