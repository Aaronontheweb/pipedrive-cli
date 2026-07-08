using System.CommandLine;
using PipedriveCLI.Models;
using PipedriveCLI.Services;
using PipedriveCLI.Utilities;
using Spectre.Console;

namespace PipedriveCLI.Commands;

/// <summary>
/// Commands for managing Pipedrive deal field definitions
/// </summary>
public static class DealFieldsCommands
{
    /// <summary>
    /// Creates the root 'dealFields' command with all subcommands
    /// </summary>
    public static Command CreateDealFieldsCommand(PipedriveApiClient apiClient)
    {
        var dealFieldsCommand = new Command("dealFields", "Manage Pipedrive deal field definitions (discover custom field keys)");

        // Add subcommands
        dealFieldsCommand.AddCommand(CreateListCommand(apiClient));

        return dealFieldsCommand;
    }

    /// <summary>
    /// Creates the 'dealFields list' command
    /// </summary>
    private static Command CreateListCommand(PipedriveApiClient apiClient)
    {
        var listCommand = new Command("list", "List all deal fields (including custom fields with their keys)");

        var searchOption = new Option<string?>(
            aliases: new[] { "--search", "-s" },
            description: "Filter fields by name (case-insensitive)");

        var customOnlyOption = new Option<bool>(
            aliases: new[] { "--custom-only", "-c" },
            description: "Only show custom fields (excludes built-in fields)");

        listCommand.AddOption(searchOption);
        listCommand.AddOption(customOnlyOption);
        var jsonOption = JsonOutputHelper.CreateJsonOption();
        listCommand.AddOption(jsonOption);

        listCommand.SetHandler(async (search, customOnly, json) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await JsonOutputHelper.FetchAsync(
                    json,
                    "Fetching deal fields...",
                    () => apiClient.GetDealFieldsAsync());

                if (response?.Success == true)
                {
                    var fields = (response.Data ?? new List<DealField>()).AsEnumerable();

                    if (customOnly)
                    {
                        fields = fields.Where(f => f.EditFlag == true);
                    }

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        fields = fields.Where(f =>
                            f.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
                    }

                    var fieldList = fields.ToList();
                    response.Data = fieldList;

                    if (json)
                    {
                        JsonOutputHelper.Write(response, ApiJsonContext.Default.PipedriveResponseListDealField);
                        return;
                    }

                    if (fieldList.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]No matching fields found[/]");
                    }
                    else
                    {
                        var table = new Table();
                        table.Border(TableBorder.Rounded);
                        table.AddColumn(new TableColumn("ID").NoWrap());
                        table.AddColumn("Key");
                        table.AddColumn("Name");
                        table.AddColumn("Type");
                        table.AddColumn("Editable");
                        table.AddColumn("Required");

                        foreach (var field in fieldList)
                        {
                            table.AddRow(
                                field.Id?.ToString() ?? "-",
                                Markup.Escape(field.Key ?? "-"),
                                Markup.Escape(field.Name ?? "-"),
                                Markup.Escape(field.FieldType ?? "-"),
                                field.EditFlag == true ? "[green]Yes[/]" : "[dim]No[/]",
                                field.MandatoryFlag == true ? "[yellow]Yes[/]" : "[dim]No[/]"
                            );
                        }

                        AnsiConsole.Write(table);

                        AnsiConsole.MarkupLine($"\n[green]✓[/] Found {fieldList.Count} field(s)");
                        AnsiConsole.MarkupLine("[dim]Use the 'Key' value with --custom-fields when updating deals, e.g.:[/]");
                        AnsiConsole.MarkupLine("[dim]  pipedrive deals update <id> --custom-fields \"<key>=<value>\"[/]");
                    }
                }
                else if (json)
                {
                    JsonOutputHelper.WriteError(response?.Error);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch deal fields: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                JsonOutputHelper.WriteException(json, ex);
            }
        }, searchOption, customOnlyOption, jsonOption);

        return listCommand;
    }
}
