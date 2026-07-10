using System.CommandLine;
using PipedriveCLI.Models;
using PipedriveCLI.Services;
using PipedriveCLI.Utilities;
using Spectre.Console;

namespace PipedriveCLI.Commands;

/// <summary>
/// Commands for managing Pipedrive pipelines
/// </summary>
public static class PipelinesCommands
{
    /// <summary>
    /// Creates the root 'pipelines' command with all subcommands
    /// </summary>
    public static Command CreatePipelinesCommand(PipedriveApiClient apiClient)
    {
        var pipelinesCommand = new Command("pipelines", "Manage Pipedrive pipelines");

        // Add subcommands
        pipelinesCommand.AddCommand(CreateListCommand(apiClient));
        pipelinesCommand.AddCommand(CreateGetCommand(apiClient));
        pipelinesCommand.AddCommand(CreateStagesCommand(apiClient));

        return pipelinesCommand;
    }

    /// <summary>
    /// Creates the 'pipelines list' command
    /// </summary>
    private static Command CreateListCommand(PipedriveApiClient apiClient)
    {
        var listCommand = new Command("list", "List all pipelines");
        var jsonOption = JsonOutputHelper.CreateJsonOption();
        listCommand.AddOption(jsonOption);

        listCommand.SetHandler(async (json) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await JsonOutputHelper.FetchAsync(
                    json,
                    "Fetching pipelines...",
                    () => apiClient.GetPipelinesAsync());

                if (response?.Success == true)
                {
                    response.Data ??= new List<Pipeline>();

                    if (json)
                    {
                        JsonOutputHelper.Write(response, ApiJsonContext.Default.PipedriveResponseListPipeline);
                        return;
                    }

                    var table = new Table();
                    table.Border(TableBorder.Rounded);
                    table.AddColumn(new TableColumn("ID").NoWrap());
                    table.AddColumn("Name");
                    table.AddColumn("Active");
                    table.AddColumn("Order");

                    foreach (var pipeline in response.Data)
                    {
                        table.AddRow(
                            pipeline.Id.ToString(),
                            Markup.Escape(pipeline.Name ?? "-"),
                            pipeline.Active ? "[green]Yes[/]" : "[dim]No[/]",
                            pipeline.OrderNr.ToString()
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"\n[green]✓[/] Found {response.Data.Count} pipeline(s)");
                }
                else if (json)
                {
                    JsonOutputHelper.WriteError(response?.Error);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch pipelines: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
    /// Creates the 'pipelines get' command
    /// </summary>
    private static Command CreateGetCommand(PipedriveApiClient apiClient)
    {
        var getCommand = new Command("get", "Get a specific pipeline by ID");

        var idArgument = new Argument<int>("id", "Pipeline ID");
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
                    $"Fetching pipeline {id}...",
                    () => apiClient.GetPipelineByIdAsync(id));

                if (response?.Success == true && response.Data != null)
                {
                    if (json)
                    {
                        JsonOutputHelper.Write(response, ApiJsonContext.Default.PipedriveResponsePipeline);
                        return;
                    }

                    var pipeline = response.Data;

                    var panel = new Panel(new Markup(
                        $"[bold]Name:[/] {Markup.Escape(pipeline.Name ?? "")}\n" +
                        $"[bold]ID:[/] {pipeline.Id}\n" +
                        $"[bold]Active:[/] {(pipeline.Active ? "Yes" : "No")}\n" +
                        $"[bold]Order:[/] {pipeline.OrderNr}\n" +
                        $"[bold]Deal Probability:[/] {(pipeline.DealProbability ? "Yes" : "No")}\n" +
                        $"[bold]Added:[/] {Markup.Escape(pipeline.AddTime ?? "N/A")}\n" +
                        $"[bold]Updated:[/] {Markup.Escape(pipeline.UpdateTime ?? "N/A")}"))
                    {
                        Header = new PanelHeader($"[green]Pipeline: {Markup.Escape(pipeline.Name ?? "")}[/]"),
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
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch pipeline: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
    /// Creates the 'pipelines stages' command
    /// </summary>
    private static Command CreateStagesCommand(PipedriveApiClient apiClient)
    {
        var stagesCommand = new Command("stages", "List stages for a pipeline");

        var pipelineIdOption = new Option<int?>(
            aliases: new[] { "--pipeline-id", "-p" },
            description: "Filter by pipeline ID (shows all stages if omitted)");

        stagesCommand.AddOption(pipelineIdOption);
        var jsonOption = JsonOutputHelper.CreateJsonOption();
        stagesCommand.AddOption(jsonOption);

        stagesCommand.SetHandler(async (pipelineId, json) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await JsonOutputHelper.FetchAsync(
                    json,
                    "Fetching stages...",
                    () => apiClient.GetStagesAsync(pipelineId));

                if (response?.Success == true)
                {
                    response.Data ??= new List<Stage>();

                    if (json)
                    {
                        JsonOutputHelper.Write(response, ApiJsonContext.Default.PipedriveResponseListStage);
                        return;
                    }

                    var table = new Table();
                    table.Border(TableBorder.Rounded);
                    table.AddColumn(new TableColumn("ID").NoWrap());
                    table.AddColumn("Name");
                    table.AddColumn("Pipeline ID");
                    table.AddColumn("Active");
                    table.AddColumn("Order");
                    table.AddColumn("Deal Prob %");

                    foreach (var stage in response.Data)
                    {
                        table.AddRow(
                            stage.Id.ToString(),
                            Markup.Escape(stage.Name ?? "-"),
                            stage.PipelineId.ToString(),
                            stage.ActiveFlag ? "[green]Yes[/]" : "[dim]No[/]",
                            stage.OrderNr.ToString(),
                            stage.DealProbability?.ToString() ?? "-"
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"\n[green]✓[/] Found {response.Data.Count} stage(s)");
                }
                else if (json)
                {
                    JsonOutputHelper.WriteError(response?.Error);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch stages: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                JsonOutputHelper.WriteException(json, ex);
            }
        }, pipelineIdOption, jsonOption);

        return stagesCommand;
    }
}
