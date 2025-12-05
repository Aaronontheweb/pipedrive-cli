using System.CommandLine;
using PipedriveCLI.Services;
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

        listCommand.SetHandler(async () =>
        {
            try
            {
                await apiClient.InitializeAsync();

                await AnsiConsole.Status()
                    .StartAsync("Fetching pipelines...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        var response = await apiClient.GetPipelinesAsync();

                        if (response?.Success == true && response.Data != null)
                        {
                            ctx.Status("Formatting results...");

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
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch pipelines: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
    /// Creates the 'pipelines get' command
    /// </summary>
    private static Command CreateGetCommand(PipedriveApiClient apiClient)
    {
        var getCommand = new Command("get", "Get a specific pipeline by ID");

        var idArgument = new Argument<int>("id", "Pipeline ID");
        getCommand.AddArgument(idArgument);

        getCommand.SetHandler(async (id) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Fetching pipeline {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.GetPipelineByIdAsync(id);
                    });

                if (response?.Success == true && response.Data != null)
                {
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
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch pipeline: {Markup.Escape(response?.Error ?? "Unknown error")}");
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
    /// Creates the 'pipelines stages' command
    /// </summary>
    private static Command CreateStagesCommand(PipedriveApiClient apiClient)
    {
        var stagesCommand = new Command("stages", "List stages for a pipeline");

        var pipelineIdOption = new Option<int?>(
            aliases: new[] { "--pipeline-id", "-p" },
            description: "Filter by pipeline ID (shows all stages if omitted)");

        stagesCommand.AddOption(pipelineIdOption);

        stagesCommand.SetHandler(async (pipelineId) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                await AnsiConsole.Status()
                    .StartAsync("Fetching stages...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        var response = await apiClient.GetStagesAsync(pipelineId);

                        if (response?.Success == true && response.Data != null)
                        {
                            ctx.Status("Formatting results...");

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
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch stages: {Markup.Escape(response?.Error ?? "Unknown error")}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, pipelineIdOption);

        return stagesCommand;
    }
}
