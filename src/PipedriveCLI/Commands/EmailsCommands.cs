using System.CommandLine;
using System.Text.RegularExpressions;
using PipedriveCLI.Models;
using PipedriveCLI.Services;
using Spectre.Console;

namespace PipedriveCLI.Commands;

/// <summary>
/// Commands for viewing email history in Pipedrive
/// </summary>
public static class EmailsCommands
{
    /// <summary>
    /// Creates the root 'emails' command with all subcommands
    /// </summary>
    public static Command CreateEmailsCommand(PipedriveApiClient apiClient)
    {
        var emailsCommand = new Command("emails", "View email history and conversations");

        // Add subcommands
        emailsCommand.AddCommand(CreateListForDealCommand(apiClient));
        emailsCommand.AddCommand(CreateListForPersonCommand(apiClient));
        emailsCommand.AddCommand(CreateGetCommand(apiClient));
        emailsCommand.AddCommand(CreateThreadsCommand(apiClient));
        emailsCommand.AddCommand(CreateThreadCommand(apiClient));
        emailsCommand.AddCommand(CreateThreadMessagesCommand(apiClient));

        return emailsCommand;
    }

    /// <summary>
    /// Creates the 'emails list-for-deal' command
    /// </summary>
    private static Command CreateListForDealCommand(PipedriveApiClient apiClient)
    {
        var listCommand = new Command("list-for-deal", "List emails associated with a deal");

        var dealIdArgument = new Argument<int>("deal-id", "Deal ID");
        listCommand.AddArgument(dealIdArgument);

        var limitOption = new Option<int?>(
            aliases: ["--limit", "-l"],
            description: "Number of emails to return (default: 50)");

        var startOption = new Option<int?>(
            aliases: ["--start", "-s"],
            description: "Pagination start (default: 0)");

        listCommand.AddOption(limitOption);
        listCommand.AddOption(startOption);

        listCommand.SetHandler(async (dealId, limit, start) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                await AnsiConsole.Status()
                    .StartAsync($"Fetching emails for deal {dealId}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        var response = await apiClient.GetMailMessagesForDealAsync(dealId, limit ?? 50, start);

                        if (response?.Success == true && response.Data != null)
                        {
                            ctx.Status("Formatting results...");
                            // Extract inner MailMessage objects from the wrapper structure
                            var messages = response.Data
                                .Where(w => w.Data != null)
                                .Select(w => w.Data!)
                                .ToList();
                            DisplayMailMessageList(messages, response.AdditionalData?.Pagination);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch emails: {Markup.Escape(response?.Error ?? "Unknown error")}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, dealIdArgument, limitOption, startOption);

        return listCommand;
    }

    /// <summary>
    /// Creates the 'emails list-for-person' command
    /// </summary>
    private static Command CreateListForPersonCommand(PipedriveApiClient apiClient)
    {
        var listCommand = new Command("list-for-person", "List emails associated with a person");

        var personIdArgument = new Argument<int>("person-id", "Person ID");
        listCommand.AddArgument(personIdArgument);

        var limitOption = new Option<int?>(
            aliases: ["--limit", "-l"],
            description: "Number of emails to return (default: 50)");

        var startOption = new Option<int?>(
            aliases: ["--start", "-s"],
            description: "Pagination start (default: 0)");

        listCommand.AddOption(limitOption);
        listCommand.AddOption(startOption);

        listCommand.SetHandler(async (personId, limit, start) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                await AnsiConsole.Status()
                    .StartAsync($"Fetching emails for person {personId}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        var response = await apiClient.GetMailMessagesForPersonAsync(personId, limit ?? 50, start);

                        if (response?.Success == true && response.Data != null)
                        {
                            ctx.Status("Formatting results...");
                            // Extract inner MailMessage objects from the wrapper structure
                            var messages = response.Data
                                .Where(w => w.Data != null)
                                .Select(w => w.Data!)
                                .ToList();
                            DisplayMailMessageList(messages, response.AdditionalData?.Pagination);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch emails: {Markup.Escape(response?.Error ?? "Unknown error")}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, personIdArgument, limitOption, startOption);

        return listCommand;
    }

    /// <summary>
    /// Creates the 'emails get' command
    /// </summary>
    private static Command CreateGetCommand(PipedriveApiClient apiClient)
    {
        var getCommand = new Command("get", "Get a specific email message by ID");

        var idArgument = new Argument<int>("message-id", "Email message ID");
        getCommand.AddArgument(idArgument);

        var includeBodyOption = new Option<bool>(
            aliases: ["--include-body", "-b"],
            description: "Include the full email body content");

        getCommand.AddOption(includeBodyOption);

        getCommand.SetHandler(async (id, includeBody) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Fetching email {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.GetMailMessageByIdAsync(id, includeBody);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    DisplayMailMessageDetail(response.Data, includeBody);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch email: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument, includeBodyOption);

        return getCommand;
    }

    /// <summary>
    /// Creates the 'emails threads' command
    /// </summary>
    private static Command CreateThreadsCommand(PipedriveApiClient apiClient)
    {
        var threadsCommand = new Command("threads", "List mail threads by folder");

        var folderOption = new Option<string?>(
            aliases: ["--folder", "-f"],
            description: "Filter by folder: inbox, sent, drafts, archive");

        var limitOption = new Option<int?>(
            aliases: ["--limit", "-l"],
            description: "Number of threads to return (default: 50)");

        var startOption = new Option<int?>(
            aliases: ["--start", "-s"],
            description: "Pagination start (default: 0)");

        threadsCommand.AddOption(folderOption);
        threadsCommand.AddOption(limitOption);
        threadsCommand.AddOption(startOption);

        threadsCommand.SetHandler(async (folder, limit, start) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var statusText = string.IsNullOrWhiteSpace(folder)
                    ? "Fetching mail threads..."
                    : $"Fetching {folder} mail threads...";

                await AnsiConsole.Status()
                    .StartAsync(statusText, async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        var response = await apiClient.GetMailThreadsAsync(folder, limit ?? 50, start);

                        if (response?.Success == true && response.Data != null)
                        {
                            ctx.Status("Formatting results...");
                            DisplayMailThreadList(response.Data, response.AdditionalData?.Pagination);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch mail threads: {Markup.Escape(response?.Error ?? "Unknown error")}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, folderOption, limitOption, startOption);

        return threadsCommand;
    }

    /// <summary>
    /// Creates the 'emails thread' command
    /// </summary>
    private static Command CreateThreadCommand(PipedriveApiClient apiClient)
    {
        var threadCommand = new Command("thread", "Get a specific mail thread by ID");

        var idArgument = new Argument<int>("thread-id", "Thread ID");
        threadCommand.AddArgument(idArgument);

        threadCommand.SetHandler(async (id) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Fetching thread {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.GetMailThreadByIdAsync(id);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    DisplayMailThreadDetail(response.Data);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch thread: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument);

        return threadCommand;
    }

    /// <summary>
    /// Creates the 'emails thread-messages' command
    /// </summary>
    private static Command CreateThreadMessagesCommand(PipedriveApiClient apiClient)
    {
        var threadMessagesCommand = new Command("thread-messages", "Get all messages in a thread");

        var idArgument = new Argument<int>("thread-id", "Thread ID");
        threadMessagesCommand.AddArgument(idArgument);

        var includeBodyOption = new Option<bool>(
            aliases: ["--include-body", "-b"],
            description: "Include the full email body content for each message");

        threadMessagesCommand.AddOption(includeBodyOption);

        threadMessagesCommand.SetHandler(async (id, includeBody) =>
        {
            try
            {
                await apiClient.InitializeAsync();

                var response = await AnsiConsole.Status()
                    .StartAsync($"Fetching messages in thread {id}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        return await apiClient.GetMailThreadMessagesAsync(id);
                    });

                if (response?.Success == true && response.Data != null)
                {
                    if (includeBody)
                    {
                        // Display each message with full body
                        foreach (var message in response.Data)
                        {
                            DisplayMailMessageDetail(message, includeBody);
                            AnsiConsole.WriteLine();
                        }
                        AnsiConsole.MarkupLine($"\n[green]✓[/] Found {response.Data.Count} message(s) in thread");
                    }
                    else
                    {
                        DisplayMailMessageList(response.Data, null);
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to fetch thread messages: {Markup.Escape(response?.Error ?? "Unknown error")}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }, idArgument, includeBodyOption);

        return threadMessagesCommand;
    }

    #region Display Helpers

    /// <summary>
    /// Displays a list of mail messages in a table format
    /// </summary>
    private static void DisplayMailMessageList(List<MailMessage> messages, Pagination? pagination)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("ID").NoWrap());
        table.AddColumn("From");
        table.AddColumn("To");
        table.AddColumn("Subject");
        table.AddColumn("Date");
        table.AddColumn("Snippet");

        foreach (var message in messages)
        {
            var from = FormatParty(message.From?.FirstOrDefault());
            var to = FormatParty(message.To?.FirstOrDefault());
            var subject = TruncateAndEscape(message.Subject, 40);
            var snippet = TruncateAndEscape(message.Snippet, 50);
            var date = FormatDateTime(message.MessageTime);

            table.AddRow(
                message.Id.ToString(),
                from,
                to,
                subject,
                date,
                snippet
            );
        }

        AnsiConsole.Write(table);

        if (pagination != null)
        {
            AnsiConsole.MarkupLine($"\n[dim]Showing {pagination.Start + 1}-{pagination.Start + messages.Count} " +
                $"| More available: {pagination.MoreItemsInCollection}[/]");
        }

        AnsiConsole.MarkupLine($"\n[green]✓[/] Found {messages.Count} email(s)");
    }

    /// <summary>
    /// Displays detailed information about a single mail message
    /// </summary>
    private static void DisplayMailMessageDetail(MailMessage message, bool includeBody)
    {
        var from = FormatParties(message.From);
        var to = FormatParties(message.To);
        var cc = FormatParties(message.Cc);
        var bcc = FormatParties(message.Bcc);

        var content = new System.Text.StringBuilder();
        content.AppendLine($"[bold]ID:[/] {message.Id}");
        content.AppendLine($"[bold]Thread ID:[/] {message.MailThreadId?.ToString() ?? "N/A"}");
        content.AppendLine($"[bold]Subject:[/] {Markup.Escape(message.Subject ?? "")}");
        content.AppendLine($"[bold]From:[/] {from}");
        content.AppendLine($"[bold]To:[/] {to}");

        if (!string.IsNullOrWhiteSpace(cc))
        {
            content.AppendLine($"[bold]CC:[/] {cc}");
        }

        if (!string.IsNullOrWhiteSpace(bcc))
        {
            content.AppendLine($"[bold]BCC:[/] {bcc}");
        }

        content.AppendLine($"[bold]Date:[/] {FormatDateTime(message.MessageTime)}");
        content.AppendLine($"[bold]Read:[/] {(message.ReadFlag == 1 ? "Yes" : "No")}");
        content.AppendLine($"[bold]Sent:[/] {(message.SentFlag == 1 ? "Yes" : "No")}");
        content.AppendLine($"[bold]Has Attachments:[/] {(message.HasAttachmentsFlag == 1 ? "Yes" : "No")}");

        if (message.DealId.HasValue)
        {
            content.AppendLine($"[bold]Deal ID:[/] {message.DealId}");
        }

        if (!string.IsNullOrWhiteSpace(message.LeadId))
        {
            content.AppendLine($"[bold]Lead ID:[/] {message.LeadId}");
        }

        if (includeBody && !string.IsNullOrWhiteSpace(message.Body))
        {
            content.AppendLine();
            content.AppendLine("[bold]Body:[/]");
            // Strip HTML tags for terminal display
            var plainBody = StripHtml(message.Body);
            content.AppendLine(Markup.Escape(plainBody));
        }
        else if (!string.IsNullOrWhiteSpace(message.Snippet))
        {
            content.AppendLine();
            content.AppendLine($"[bold]Snippet:[/] {Markup.Escape(message.Snippet)}");
        }

        var panel = new Panel(new Markup(content.ToString()))
        {
            Header = new PanelHeader($"[green]Email {message.Id}[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Displays a list of mail threads in a table format
    /// </summary>
    private static void DisplayMailThreadList(List<MailThread> threads, Pagination? pagination)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("ID").NoWrap());
        table.AddColumn("From");
        table.AddColumn("To");
        table.AddColumn("Subject");
        table.AddColumn("Messages");
        table.AddColumn("Last Message");
        table.AddColumn("Folders");

        foreach (var thread in threads)
        {
            var from = FormatParty(thread.Parties?.From?.FirstOrDefault());
            var to = FormatParty(thread.Parties?.To?.FirstOrDefault());
            var subject = TruncateAndEscape(thread.Subject, 40);
            var lastMessage = FormatDateTime(thread.LastMessageTimestamp);
            var folders = thread.Folders != null ? string.Join(", ", thread.Folders) : "-";

            table.AddRow(
                thread.Id.ToString(),
                from,
                to,
                subject,
                thread.MessageCount?.ToString() ?? "-",
                lastMessage,
                Markup.Escape(folders)
            );
        }

        AnsiConsole.Write(table);

        if (pagination != null)
        {
            AnsiConsole.MarkupLine($"\n[dim]Showing {pagination.Start + 1}-{pagination.Start + threads.Count} " +
                $"| More available: {pagination.MoreItemsInCollection}[/]");
        }

        AnsiConsole.MarkupLine($"\n[green]✓[/] Found {threads.Count} thread(s)");
    }

    /// <summary>
    /// Displays detailed information about a single mail thread
    /// </summary>
    private static void DisplayMailThreadDetail(MailThread thread)
    {
        var from = FormatParties(thread.Parties?.From);
        var to = FormatParties(thread.Parties?.To);

        var content = new System.Text.StringBuilder();
        content.AppendLine($"[bold]ID:[/] {thread.Id}");
        content.AppendLine($"[bold]Subject:[/] {Markup.Escape(thread.Subject ?? "")}");
        content.AppendLine($"[bold]From:[/] {from}");
        content.AppendLine($"[bold]To:[/] {to}");
        content.AppendLine($"[bold]Message Count:[/] {thread.MessageCount?.ToString() ?? "0"}");
        content.AppendLine($"[bold]Folders:[/] {(thread.Folders != null ? Markup.Escape(string.Join(", ", thread.Folders)) : "N/A")}");
        content.AppendLine($"[bold]Read:[/] {(thread.ReadFlag == 1 ? "Yes" : "No")}");
        content.AppendLine($"[bold]Archived:[/] {(thread.ArchivedFlag == 1 ? "Yes" : "No")}");
        content.AppendLine($"[bold]Has Attachments:[/] {(thread.HasAttachmentsFlag == 1 ? "Yes" : "No")}");
        content.AppendLine($"[bold]Has Draft:[/] {(thread.HasDraftFlag == 1 ? "Yes" : "No")}");
        content.AppendLine($"[bold]First Message:[/] {FormatDateTime(thread.FirstMessageTimestamp)}");
        content.AppendLine($"[bold]Last Message:[/] {FormatDateTime(thread.LastMessageTimestamp)}");

        if (thread.DealId.HasValue)
        {
            content.AppendLine($"[bold]Deal ID:[/] {thread.DealId}");
        }

        if (!string.IsNullOrWhiteSpace(thread.LeadId))
        {
            content.AppendLine($"[bold]Lead ID:[/] {thread.LeadId}");
        }

        if (!string.IsNullOrWhiteSpace(thread.Snippet))
        {
            content.AppendLine();
            content.AppendLine($"[bold]Latest Snippet:[/] {Markup.Escape(thread.Snippet)}");
        }

        var panel = new Panel(new Markup(content.ToString()))
        {
            Header = new PanelHeader($"[green]Thread {thread.Id}: {Markup.Escape(TruncateString(thread.Subject, 40))}[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);

        AnsiConsole.MarkupLine($"\n[dim]Use 'pipedrive emails thread-messages {thread.Id}' to view all messages in this thread[/]");
    }

    /// <summary>
    /// Formats a single mail party for display
    /// </summary>
    private static string FormatParty(MailParty? party)
    {
        if (party == null) return "-";

        var name = party.Name ?? party.LinkedPersonName;
        var email = party.EmailAddress;

        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(email))
        {
            return Markup.Escape($"{name} <{email}>");
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            return Markup.Escape(email);
        }
        else if (!string.IsNullOrWhiteSpace(name))
        {
            return Markup.Escape(name);
        }

        return "-";
    }

    /// <summary>
    /// Formats a list of mail parties for display
    /// </summary>
    private static string FormatParties(List<MailParty>? parties)
    {
        if (parties == null || parties.Count == 0) return "-";

        var formatted = parties.Select(p =>
        {
            var name = p.Name ?? p.LinkedPersonName;
            var email = p.EmailAddress;

            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(email))
            {
                return $"{name} <{email}>";
            }
            else if (!string.IsNullOrWhiteSpace(email))
            {
                return email;
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
            return null;
        }).Where(s => s != null);

        return Markup.Escape(string.Join("; ", formatted));
    }

    /// <summary>
    /// Truncates a string and escapes it for Spectre.Console markup
    /// </summary>
    private static string TruncateAndEscape(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return "-";

        var sanitized = text.Replace("\n", " ").Replace("\r", "");
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized.Substring(0, maxLength) + "...";
        }

        return Markup.Escape(sanitized);
    }

    /// <summary>
    /// Truncates a string without escaping
    /// </summary>
    private static string TruncateString(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        if (text.Length > maxLength)
        {
            return text.Substring(0, maxLength) + "...";
        }

        return text;
    }

    /// <summary>
    /// Formats a datetime string for display
    /// </summary>
    private static string FormatDateTime(string? dateTime)
    {
        if (string.IsNullOrWhiteSpace(dateTime)) return "-";

        if (DateTime.TryParse(dateTime, out var dt))
        {
            return dt.ToString("yyyy-MM-dd HH:mm");
        }

        return Markup.Escape(dateTime);
    }

    /// <summary>
    /// Strips HTML tags from a string for terminal display
    /// </summary>
    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";

        // Remove script and style elements completely
        html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);

        // Replace common block elements with newlines
        html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</p>", "\n\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</div>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</li>", "\n", RegexOptions.IgnoreCase);

        // Remove remaining HTML tags
        html = Regex.Replace(html, @"<[^>]+>", "");

        // Decode HTML entities
        html = System.Net.WebUtility.HtmlDecode(html);

        // Normalize whitespace
        html = Regex.Replace(html, @"[ \t]+", " ");
        html = Regex.Replace(html, @"\n{3,}", "\n\n");

        return html.Trim();
    }

    #endregion
}
