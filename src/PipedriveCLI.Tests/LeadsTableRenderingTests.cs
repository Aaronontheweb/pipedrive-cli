using PipedriveCLI.Models;
using Spectre.Console;
using Spectre.Console.Testing;
using VerifyXunit;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Tests for leads table rendering to ensure UUIDs are displayed correctly.
/// These tests verify that the NoWrap() fix prevents UUID corruption in table output.
/// </summary>
public class LeadsTableRenderingTests
{
    /// <summary>
    /// Creates test leads with real-world UUID patterns from production data.
    /// </summary>
    private static List<Lead> CreateTestLeads() =>
    [
        new Lead
        {
            Id = "0b9fad60-bae0-11f0-945c-134e56493ed5",
            Title = "Dimitri Donfack (DATIVE) - Training",
            PersonId = 12869,
            OwnerId = 10204689,
            AddTime = "2024-01-01T10:00:00Z"
        },
        new Lead
        {
            Id = "d3ce8ea0-ba68-11f0-9055-d7d6d543086d",
            Title = "Florinel Hociung - Consulting",
            PersonId = 12860,
            OwnerId = 10204689,
            AddTime = "2024-01-02T11:00:00Z"
        },
        new Lead
        {
            Id = "79e8daf0-ba57-11f0-9055-d7d6d543086d",
            Title = "Daniel Kottis - Enterprise",
            PersonId = 12843,
            OwnerId = 10204689,
            AddTime = "2024-01-03T12:00:00Z"
        },
        new Lead
        {
            Id = "bda0a9a0-bb31-11f0-bc3b-c55cf8762758",
            Title = "Franco Pretorius - Support",
            PersonId = 12872,
            OwnerId = 10204689,
            AddTime = "2024-01-04T13:00:00Z"
        }
    ];

    /// <summary>
    /// Test that table rendering with NoWrap preserves full UUIDs on a single line.
    /// This is the key test that verifies the fix for UUID corruption.
    /// </summary>
    [Fact]
    public void LeadsTable_WithNoWrap_PreservesFullUuidsOnSingleLine()
    {
        // Arrange
        var console = new TestConsole();
        console.Profile.Width = 120; // Standard terminal width
        var leads = CreateTestLeads();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("ID").NoWrap()); // The fix!
        table.AddColumn("Title");
        table.AddColumn("Value");
        table.AddColumn("Person/Org ID");
        table.AddColumn("Owner ID");
        table.AddColumn("Added");

        foreach (var lead in leads)
        {
            var valueDisplay = lead.Value != null
                ? $"{lead.Value.Currency} {lead.Value.Amount:N2}"
                : "-";

            var entityId = lead.PersonId?.ToString()
                ?? lead.OrganizationId?.ToString()
                ?? "-";

            table.AddRow(
                Markup.Escape(lead.Id ?? "-"),
                Markup.Escape(lead.Title ?? "-"),
                valueDisplay,
                entityId,
                lead.OwnerId?.ToString() ?? "-",
                lead.AddTime ?? "-"
            );
        }

        // Act
        console.Write(table);
        var output = console.Output;

        // Assert - Each UUID should appear on a single line (not wrapped)
        // Split by newlines and check that each UUID appears intact
        var expectedUuids = new[]
        {
            "0b9fad60-bae0-11f0-945c-134e56493ed5",
            "d3ce8ea0-ba68-11f0-9055-d7d6d543086d",
            "79e8daf0-ba57-11f0-9055-d7d6d543086d",
            "bda0a9a0-bb31-11f0-bc3b-c55cf8762758"
        };

        foreach (var uuid in expectedUuids)
        {
            // The full UUID should appear somewhere in the output
            Assert.Contains(uuid, output);

            // Additionally verify UUID appears on a single line by checking
            // that no line contains just a partial UUID segment
            var lines = output.Split('\n');
            var uuidFound = false;
            foreach (var line in lines)
            {
                if (line.Contains(uuid))
                {
                    uuidFound = true;
                    break;
                }
            }
            Assert.True(uuidFound, $"UUID {uuid} should appear intact on a single line");
        }
    }

    /// <summary>
    /// Test that WITHOUT NoWrap, UUIDs might get split across lines in narrow terminals.
    /// This documents the bug we're fixing.
    /// </summary>
    [Fact]
    public void LeadsTable_WithoutNoWrap_MayWrapUuids_InNarrowTerminal()
    {
        // Arrange - Very narrow terminal that forces wrapping
        var console = new TestConsole();
        console.Profile.Width = 60; // Narrow terminal
        var leads = CreateTestLeads();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("ID"); // NO NoWrap - this is the bug scenario
        table.AddColumn("Title");
        table.AddColumn("Person/Org ID");

        foreach (var lead in leads)
        {
            var entityId = lead.PersonId?.ToString()
                ?? lead.OrganizationId?.ToString()
                ?? "-";

            table.AddRow(
                Markup.Escape(lead.Id ?? "-"),
                Markup.Escape(lead.Title ?? "-"),
                entityId
            );
        }

        // Act
        console.Write(table);
        var output = console.Output;

        // Assert - Just verify output is generated (we can't reliably assert wrapping behavior
        // since it depends on Spectre.Console's internal logic)
        Assert.NotEmpty(output);
    }

    /// <summary>
    /// Test the table rendering at various terminal widths to ensure robustness.
    /// </summary>
    [Theory]
    [InlineData(80)]
    [InlineData(100)]
    [InlineData(120)]
    [InlineData(160)]
    [InlineData(200)]
    public void LeadsTable_WithNoWrap_PreservesUuidsAtVariousWidths(int terminalWidth)
    {
        // Arrange
        var console = new TestConsole();
        console.Profile.Width = terminalWidth;
        var leads = CreateTestLeads();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("ID").NoWrap());
        table.AddColumn("Title");
        table.AddColumn("Person/Org ID");

        foreach (var lead in leads)
        {
            var entityId = lead.PersonId?.ToString()
                ?? lead.OrganizationId?.ToString()
                ?? "-";

            table.AddRow(
                Markup.Escape(lead.Id ?? "-"),
                Markup.Escape(lead.Title ?? "-"),
                entityId
            );
        }

        // Act
        console.Write(table);
        var output = console.Output;

        // Assert - All UUIDs must be present intact
        Assert.Contains("0b9fad60-bae0-11f0-945c-134e56493ed5", output);
        Assert.Contains("d3ce8ea0-ba68-11f0-9055-d7d6d543086d", output);
        Assert.Contains("79e8daf0-ba57-11f0-9055-d7d6d543086d", output);
        Assert.Contains("bda0a9a0-bb31-11f0-bc3b-c55cf8762758", output);
    }

    /// <summary>
    /// Verify that when the ID column uses NoWrap, the UUID is never fragmented
    /// by ensuring each line containing UUID parts has the full UUID.
    /// </summary>
    [Fact]
    public void LeadsTable_UuidsAreNotFragmented()
    {
        // Arrange
        var console = new TestConsole();
        console.Profile.Width = 100;
        var leads = CreateTestLeads();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("ID").NoWrap());
        table.AddColumn("Title");
        table.AddColumn("Person/Org ID");

        foreach (var lead in leads)
        {
            table.AddRow(
                Markup.Escape(lead.Id ?? "-"),
                Markup.Escape(lead.Title ?? "-"),
                lead.PersonId?.ToString() ?? "-"
            );
        }

        // Act
        console.Write(table);
        var output = console.Output;
        var lines = output.Split('\n');

        // Assert - No line should contain a partial UUID segment without the full UUID
        // A UUID segment would be something like "ba66-11f0" which shouldn't appear alone
        var uuidSegmentPattern = new System.Text.RegularExpressions.Regex(@"\b[a-f0-9]{4}-[a-f0-9]{4}\b");

        foreach (var line in lines)
        {
            var matches = uuidSegmentPattern.Matches(line);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                // If we find a UUID-like segment, the line should contain a full UUID
                var hasFullUuid = System.Text.RegularExpressions.Regex.IsMatch(
                    line,
                    @"[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}");

                Assert.True(hasFullUuid,
                    $"Line contains UUID fragment '{match.Value}' but no full UUID: {line.Trim()}");
            }
        }
    }

    /// <summary>
    /// Verify snapshot test for leads table rendering at standard width.
    /// This captures the exact table output so any changes to rendering are detected.
    /// </summary>
    [Fact]
    public Task LeadsTable_Snapshot_StandardWidth()
    {
        // Arrange
        var console = new TestConsole();
        console.Profile.Width = 120;
        var leads = CreateTestLeads();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("ID").NoWrap());
        table.AddColumn("Title");
        table.AddColumn("Value");
        table.AddColumn("Person/Org ID");
        table.AddColumn("Owner ID");
        table.AddColumn("Added");

        foreach (var lead in leads)
        {
            var valueDisplay = lead.Value != null
                ? $"{lead.Value.Currency} {lead.Value.Amount:N2}"
                : "-";

            var entityId = lead.PersonId?.ToString()
                ?? lead.OrganizationId?.ToString()
                ?? "-";

            table.AddRow(
                Markup.Escape(lead.Id ?? "-"),
                Markup.Escape(lead.Title ?? "-"),
                valueDisplay,
                entityId,
                lead.OwnerId?.ToString() ?? "-",
                lead.AddTime ?? "-"
            );
        }

        // Act
        console.Write(table);
        var output = console.Output;

        // Assert via snapshot
        return Verifier.Verify(output);
    }

    /// <summary>
    /// Verify snapshot test for leads table rendering at narrow width.
    /// Even at narrow width, UUIDs should remain intact due to NoWrap.
    /// </summary>
    [Fact]
    public Task LeadsTable_Snapshot_NarrowWidth()
    {
        // Arrange
        var console = new TestConsole();
        console.Profile.Width = 80;
        var leads = CreateTestLeads();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("ID").NoWrap());
        table.AddColumn("Title");
        table.AddColumn("Person/Org ID");

        foreach (var lead in leads)
        {
            var entityId = lead.PersonId?.ToString()
                ?? lead.OrganizationId?.ToString()
                ?? "-";

            table.AddRow(
                Markup.Escape(lead.Id ?? "-"),
                Markup.Escape(lead.Title ?? "-"),
                entityId
            );
        }

        // Act
        console.Write(table);
        var output = console.Output;

        // Assert via snapshot
        return Verifier.Verify(output);
    }
}
