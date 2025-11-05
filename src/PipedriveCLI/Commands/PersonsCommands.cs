using System.CommandLine;
using PipedriveCLI.Models;
using PipedriveCLI.Services;
using Spectre.Console;

namespace PipedriveCLI.Commands;

/// <summary>
/// Commands for managing Pipedrive persons (contacts)
/// </summary>
public static class PersonsCommands
{
    /// <summary>
    /// Creates the root 'persons' command with all subcommands
    /// </summary>
    public static Command CreatePersonsCommand(PipedriveApiClient apiClient)
    {
        var personsCommand = new Command("persons", "Manage Pipedrive persons (contacts)");

        // Add subcommands
        personsCommand.AddCommand(CreateListCommand(apiClient));
        personsCommand.AddCommand(CreateGetCommand(apiClient));
        personsCommand.AddCommand(CreateCreateCommand(apiClient));
        personsCommand.AddCommand(CreateUpdateCommand(apiClient));
        personsCommand.AddCommand(CreateDeleteCommand(apiClient));
        personsCommand.AddCommand(CreateSearchCommand(apiClient));
        personsCommand.AddCommand(CreateMergeCommand(apiClient));

        return personsCommand;
    }

    /// <summary>
    /// Creates the 'persons list' command
    /// </summary>
    private static Command CreateListCommand(PipedriveApiClient apiClient)
    {
        var listCommand = new Command("list", "List all persons");

        var limitOption = new Option<int?>(
            aliases: new[] { "--limit", "-l" },
            description: "Number of persons to return (default: 100)");

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
                    .StartAsync("Fetching persons...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        var response = await apiClient.GetPersonsAsync(limit, start);

                        if (response?.Success == true && response.Data != null)
                        {
                            ctx.Status("Formatting results...");

                            var table = new Table();
                            table.Border(TableBorder.Rounded);
                            table.AddColumn("ID");
                            table.AddColumn("Name");
                            table.AddColumn("Email");
                            table.AddColumn("Phone");
                            table.AddColumn("Org ID");
                            table.AddColumn("Owner ID");
                            table.AddColumn("Added");

                            foreach (var person in response.Data)
                            {
                                var primaryEmail = person.Email?.FirstOrDefault(e => e.Primary)?.Value
                                    ?? person.Email?.FirstOrDefault()?.Value
                                    ?? "-";

                                var primaryPhone = person.Phone?.FirstOrDefault(p => p.Primary)?.Value
                                    ?? person.Phone?.FirstOrDefault()?.Value
                                    ?? "-";

                                table.AddRow(
                                    person.Id.ToString(),
                                    person.Name ?? "-",
                                    primaryEmail,
                                    primaryPhone,
                                    person.OrgId?.ToString() ?? "-",
                                    person.OwnerId?.ToString() ?? "-",
                                    person.AddTime ?? "-"
                                );
                            }

                            AnsiConsole.Write(table);

                            if (response.AdditionalData?.Pagination != null)
                            {
                                var pagination = response.AdditionalData.Pagination;
                                AnsiConsole.MarkupLine($"\n[dim]Showing {pagination.Start + 1}-{pagination.Start + response.Data.Count} " +
                                    $"| More available: {pagination.MoreItemsInCollection}[/]");
                            }

                            AnsiConsole.MarkupLine($"\n[green]✓[/] Found {response.Data.Count} person(s)");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch persons: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
    /// Creates the 'persons get' command
    /// </summary>
    private static Command CreateGetCommand(PipedriveApiClient apiClient)
    {
        var getCommand = new Command("get", "Get a specific person by ID");

        var idArgument = new Argument<int>("id", "Person ID");
        getCommand.AddArgument(idArgument);

        getCommand.SetHandler(async (id) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Fetching person {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.GetPersonByIdAsync(id);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    var person = response.Data;

                    var emails = person.Email != null && person.Email.Count > 0
                        ? string.Join(", ", person.Email.Select(e => $"{e.Value}{(e.Primary ? " (primary)" : "")}"))
                        : "N/A";

                    var phones = person.Phone != null && person.Phone.Count > 0
                        ? string.Join(", ", person.Phone.Select(p => $"{p.Value}{(p.Primary ? " (primary)" : "")}"))
                        : "N/A";

                    var ownerInfo = person.OwnerId != null
                        ? $"{person.OwnerId.Id} ({person.OwnerId.Name})"
                        : "N/A";

                    var panel = new Panel(new Markup(
                        $"[bold]Name:[/] {Markup.Escape(person.Name ?? "")}\n" +
                        $"[bold]ID:[/] {person.Id}\n" +
                        $"[bold]First Name:[/] {Markup.Escape(person.FirstName ?? "N/A")}\n" +
                        $"[bold]Last Name:[/] {Markup.Escape(person.LastName ?? "N/A")}\n" +
                        $"[bold]Email(s):[/] {Markup.Escape(emails)}\n" +
                        $"[bold]Phone(s):[/] {Markup.Escape(phones)}\n" +
                        $"[bold]Organization ID:[/] {person.OrgId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Owner:[/] {Markup.Escape(ownerInfo)}\n" +
                        $"[bold]Added:[/] {Markup.Escape(person.AddTime ?? "N/A")}\n" +
                        $"[bold]Updated:[/] {Markup.Escape(person.UpdateTime ?? "N/A")}"))
                    {
                        Header = new PanelHeader($"[green]Person: {Markup.Escape(person.Name ?? "")}[/]"),
                        Border = BoxBorder.Rounded
                    };

                    AnsiConsole.Write(panel);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch person: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
    /// Creates the 'persons create' command
    /// </summary>
    private static Command CreateCreateCommand(PipedriveApiClient apiClient)
    {
        var createCommand = new Command("create", "Create a new person");

        var nameOption = new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Person name (required)")
        { IsRequired = true };

        var emailOption = new Option<string?>(
            aliases: new[] { "--email", "-e" },
            description: "Primary email address");

        var phoneOption = new Option<string?>(
            aliases: new[] { "--phone", "-p" },
            description: "Primary phone number");

        var orgIdOption = new Option<int?>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID");

        createCommand.AddOption(nameOption);
        createCommand.AddOption(emailOption);
        createCommand.AddOption(phoneOption);
        createCommand.AddOption(orgIdOption);

        createCommand.SetHandler(async (name, email, phone, orgId) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var person = new Person
                {
                    Name = name,
                    OrgId = orgId
                };

                // Add email if provided
                if (!string.IsNullOrWhiteSpace(email))
                {
                    person.Email = new List<Email>
                    {
                        new Email { Value = email, Primary = true }
                    };
                }

                // Add phone if provided
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    person.Phone = new List<Phone>
                    {
                        new Phone { Value = phone, Primary = true }
                    };
                }

                var response = await AnsiConsole.Status()
                    .StartAsync("Creating person...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.CreatePersonAsync(person);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Person created successfully");
                    AnsiConsole.MarkupLine($"[dim]ID:[/] {Markup.Escape(response.Data.Id.ToString())}");
                    AnsiConsole.MarkupLine($"[dim]Name:[/] {Markup.Escape(response.Data.Name ?? "")}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to create person: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, nameOption, emailOption, phoneOption, orgIdOption);

        return createCommand;
    }

    /// <summary>
    /// Creates the 'persons update' command
    /// </summary>
    private static Command CreateUpdateCommand(PipedriveApiClient apiClient)
    {
        var updateCommand = new Command("update", "Update an existing person");

        var idArgument = new Argument<int>("id", "Person ID");
        updateCommand.AddArgument(idArgument);

        var nameOption = new Option<string?>(
            aliases: new[] { "--name", "-n" },
            description: "New person name");

        var emailOption = new Option<string?>(
            aliases: new[] { "--email", "-e" },
            description: "New primary email address");

        var phoneOption = new Option<string?>(
            aliases: new[] { "--phone", "-p" },
            description: "New primary phone number");

        updateCommand.AddOption(nameOption);
        updateCommand.AddOption(emailOption);
        updateCommand.AddOption(phoneOption);

        updateCommand.SetHandler(async (id, name, email, phone) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] At least one field must be specified to update");
                    return;
                }

                await apiClient.InitializeAsync();

                var person = new Person();

                if (!string.IsNullOrWhiteSpace(name)) person.Name = name;

                if (!string.IsNullOrWhiteSpace(email))
                {
                    person.Email = new List<Email>
                    {
                        new Email { Value = email, Primary = true }
                    };
                }

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    person.Phone = new List<Phone>
                    {
                        new Phone { Value = phone, Primary = true }
                    };
                }

                var response = await AnsiConsole.Status()
                    .StartAsync($"Updating person {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.UpdatePersonAsync(id, person);
                    });

                if (response?.Success == true)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Person updated successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to update person: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument, nameOption, emailOption, phoneOption);

        return updateCommand;
    }

    /// <summary>
    /// Creates the 'persons delete' command
    /// </summary>
    private static Command CreateDeleteCommand(PipedriveApiClient apiClient)
    {
        var deleteCommand = new Command("delete", "Delete a person");

        var idArgument = new Argument<int>("id", "Person ID");
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
                    var confirm = AnsiConsole.Confirm($"Are you sure you want to delete person {id}?");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                        return;
                    }
                }

                await apiClient.InitializeAsync();

                var success = await AnsiConsole.Status()
                    .StartAsync($"Deleting person {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.DeletePersonAsync(id);
                    });

                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Person {id} deleted successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete person {id}");
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
    /// Creates the 'persons search' command
    /// </summary>
    private static Command CreateSearchCommand(PipedriveApiClient apiClient)
    {
        var searchCommand = new Command("search", "Search for persons");

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
                        return await apiClient.SearchPersonsAsync(term, limit);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    if (response.Data.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[yellow]No persons found matching '{term}'[/]");
                        return;
                    }

                    var table = new Table();
                    table.Border(TableBorder.Rounded);
                    table.AddColumn("ID");
                    table.AddColumn("Name");
                    table.AddColumn("Email");
                    table.AddColumn("Phone");
                    table.AddColumn("Org ID");

                    foreach (var person in response.Data)
                    {
                        var primaryEmail = person.Email?.FirstOrDefault(e => e.Primary)?.Value
                            ?? person.Email?.FirstOrDefault()?.Value
                            ?? "-";

                        var primaryPhone = person.Phone?.FirstOrDefault(p => p.Primary)?.Value
                            ?? person.Phone?.FirstOrDefault()?.Value
                            ?? "-";

                        table.AddRow(
                            person.Id.ToString(),
                            person.Name ?? "-",
                            primaryEmail,
                            primaryPhone,
                            person.OrgId?.ToString() ?? "-"
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"\n[green]✓[/] Found {response.Data.Count} person(s) matching '{term}'");
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

    /// <summary>
    /// Creates the 'persons merge' command
    /// </summary>
    private static Command CreateMergeCommand(PipedriveApiClient apiClient)
    {
        var mergeCommand = new Command("merge", "Merge two persons");

        var idArgument = new Argument<int>("id", "ID of the person to be merged (will be deleted)");
        mergeCommand.AddArgument(idArgument);

        var mergeWithIdArgument = new Argument<int>("merge-with-id", "ID of the person to merge with (takes priority in conflicts)");
        mergeCommand.AddArgument(mergeWithIdArgument);

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Skip confirmation prompt");

        mergeCommand.AddOption(forceOption);

        mergeCommand.SetHandler(async (id, mergeWithId, force) =>
        {
            try
            {
                if (!force)
                {
                    var confirm = AnsiConsole.Confirm($"Are you sure you want to merge person {id} into person {mergeWithId}? Person {id} will be deleted.");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                        return;
                    }
                }

                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Merging person {id} into {mergeWithId}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.MergePersonAsync(id, mergeWithId);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Persons merged successfully");
                    AnsiConsole.MarkupLine($"[dim]Merged person ID:[/] {response.Data.Id}");
                    AnsiConsole.MarkupLine($"[dim]Name:[/] {Markup.Escape(response.Data.Name ?? "")}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to merge persons: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
