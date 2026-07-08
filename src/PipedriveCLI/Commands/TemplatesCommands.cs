using System.CommandLine;
using PipedriveCLI.Models;
using PipedriveCLI.Services;
using PipedriveCLI.Utilities;
using Spectre.Console;

namespace PipedriveCLI.Commands;

/// <summary>
/// Commands for managing Pipedrive email templates
/// </summary>
public static class TemplatesCommands
{
    /// <summary>
    /// Creates the root 'templates' command with all subcommands
    /// </summary>
    public static Command CreateTemplatesCommand(PipedriveApiClient apiClient)
    {
        var templatesCommand = new Command("templates", "Manage Pipedrive email templates");

        // Add subcommands
        templatesCommand.AddCommand(CreateListCommand(apiClient));
        templatesCommand.AddCommand(CreateGetCommand(apiClient));

        return templatesCommand;
    }

    /// <summary>
    /// Creates the 'templates list' command
    /// </summary>
    private static Command CreateListCommand(PipedriveApiClient apiClient)
    {
        var listCommand = new Command("list", "List all email templates");
        var jsonOption = JsonOutputHelper.CreateJsonOption();
        listCommand.AddOption(jsonOption);

        listCommand.SetHandler(async (json) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await JsonOutputHelper.FetchAsync(
                    json,
                    "Fetching email templates...",
                    () => apiClient.GetEmailTemplatesAsync());

                if (response?.Success == true)
                {
                    response.Data ??= new List<EmailTemplate>();

                    if (json)
                    {
                        JsonOutputHelper.Write(response, ApiJsonContext.Default.PipedriveResponseListEmailTemplate);
                        return;
                    }

                    var table = new Table();
                    table.Border(TableBorder.Rounded);
                    table.AddColumn(new TableColumn("ID").NoWrap());
                    table.AddColumn("Name");
                    table.AddColumn("Shared");
                    table.AddColumn("Updated");

                    foreach (var template in response.Data)
                    {
                        var sharedDisplay = template.SharedFlag == 1 ? "Yes" : "No";

                        table.AddRow(
                            template.Id.ToString(),
                            Markup.Escape(template.Name ?? "-"),
                            sharedDisplay,
                            template.UpdateTime ?? "-"
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"\n[green]✓[/] Found {response.Data.Count} template(s)");
                }
                else if (json)
                {
                    JsonOutputHelper.WriteError(response?.Error);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch templates: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                JsonOutputHelper.WriteException(json, ex);
            }
        }, jsonOption);

        return listCommand;
    }

    /// <summary>
    /// Creates the 'templates get' command
    /// </summary>
    private static Command CreateGetCommand(PipedriveApiClient apiClient)
    {
        var getCommand = new Command("get", "Get a specific email template by ID");

        var idArgument = new Argument<int>("id", "Template ID");
        getCommand.AddArgument(idArgument);

        var showContentOption = new Option<bool>(
            aliases: new[] { "--content", "-c" },
            description: "Show the full HTML content of the template");

        getCommand.AddOption(showContentOption);
        var jsonOption = JsonOutputHelper.CreateJsonOption();
        getCommand.AddOption(jsonOption);

        getCommand.SetHandler(async (id, showContent, json) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await JsonOutputHelper.FetchAsync(
                    json,
                    $"Fetching template {id}...",
                    () => apiClient.GetEmailTemplateByIdAsync(id));

                if (response?.Success == true && response.Data != null)
                {
                    if (json)
                    {
                        JsonOutputHelper.Write(response.Data, ApiJsonContext.Default.EmailTemplate);
                        return;
                    }

                    var template = response.Data;

                    var panel = new Panel(new Markup(
                        $"[bold]Name:[/] {Markup.Escape(template.Name ?? "")}\n" +
                        $"[bold]ID:[/] {template.Id}\n" +
                        $"[bold]Subject:[/] {Markup.Escape(template.Subject ?? "N/A")}\n" +
                        $"[bold]Shared:[/] {(template.SharedFlag == 1 ? "Yes" : "No")}\n" +
                        $"[bold]User ID:[/] {template.UserId?.ToString() ?? "N/A"}\n" +
                        $"[bold]Added:[/] {template.AddTime ?? "N/A"}\n" +
                        $"[bold]Updated:[/] {template.UpdateTime ?? "N/A"}"))
                    {
                        Header = new PanelHeader($"[green]Template: {Markup.Escape(template.Name ?? "")}[/]"),
                        Border = BoxBorder.Rounded
                    };

                    AnsiConsole.Write(panel);

                    if (showContent && !string.IsNullOrWhiteSpace(template.Content))
                    {
                        AnsiConsole.WriteLine();
                        AnsiConsole.Write(new Rule("[yellow]Content (HTML)[/]"));
                        AnsiConsole.WriteLine();

                        // Strip HTML tags for display, or show raw HTML
                        var content = template.Content;
                        // Simple HTML tag stripping for readable display
                        var plainText = System.Text.RegularExpressions.Regex.Replace(content, "<[^>]+>", "");
                        plainText = System.Net.WebUtility.HtmlDecode(plainText);
                        plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ").Trim();

                        AnsiConsole.Write(new Panel(Markup.Escape(plainText))
                        {
                            Header = new PanelHeader("[dim]Plain text preview[/]"),
                            Border = BoxBorder.Rounded
                        });

                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine("[dim]Use --content to see content. Raw HTML is available in the API response.[/]");
                    }
                    else if (!showContent)
                    {
                        AnsiConsole.MarkupLine("\n[dim]Tip: Use --content or -c to see the template content[/]");
                    }
                }
                else if (json)
                {
                    JsonOutputHelper.WriteError(response?.Error);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch template: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                JsonOutputHelper.WriteException(json, ex);
            }
        }, idArgument, showContentOption, jsonOption);

        return getCommand;
    }
}
