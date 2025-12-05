using System.Net;
using System.Text.Json;
using PipedriveCLI.Models;
using PipedriveCLI.Services;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Tests for Leads API client methods and JSON serialization
/// </summary>
public class LeadsApiClientTests
{
    /// <summary>
    /// Test that Lead deserialization handles the full Pipedrive API response
    /// including extra fields we don't map
    /// </summary>
    [Fact]
    public void Lead_Deserialization_HandlesFullApiResponse()
    {
        // Arrange - Full API response with all fields including ones we don't map
        var json = """
        {
            "success": true,
            "data": {
                "id": "a5c5e7b5-28f7-4b57-96fe-3a5e7fb24d5b",
                "title": "Potential deal",
                "owner_id": null,
                "creator_id": 123,
                "label_ids": ["label-uuid-1", "label-uuid-2"],
                "person_id": 456,
                "organization_id": null,
                "source_name": "API",
                "origin": "API",
                "origin_id": "custom-origin-123",
                "channel": 1,
                "channel_id": "channel-123",
                "is_archived": false,
                "was_seen": true,
                "value": {
                    "amount": 5000.00,
                    "currency": "USD"
                },
                "expected_close_date": "2024-12-31",
                "next_activity_id": 789,
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z",
                "visible_to": "3",
                "cc_email": "lead+abc123@company.pipedrive.com"
            }
        }
        """;

        // Act - Deserialize using our API JSON context
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseLead);

        // Assert - Verify core fields are correctly deserialized
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("a5c5e7b5-28f7-4b57-96fe-3a5e7fb24d5b", response.Data.Id);
        Assert.Equal("Potential deal", response.Data.Title);
        Assert.Null(response.Data.OwnerId);
        Assert.Equal(456, response.Data.PersonId);
        Assert.Null(response.Data.OrganizationId);
        Assert.True(response.Data.WasSeen);

        // Verify value object
        Assert.NotNull(response.Data.Value);
        Assert.Equal(5000.00m, response.Data.Value.Amount);
        Assert.Equal("USD", response.Data.Value.Currency);

        // Verify dates
        Assert.Equal("2024-12-31", response.Data.ExpectedCloseDate);
        Assert.Equal("2024-01-15T10:30:00Z", response.Data.AddTime);
        Assert.Equal("2024-01-16T14:20:00Z", response.Data.UpdateTime);
    }

    /// <summary>
    /// Test deserializing a list of leads
    /// </summary>
    [Fact]
    public void LeadsList_Deserialization_WithPagination()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [
                {
                    "id": "lead-1",
                    "title": "Lead One",
                    "owner_id": null,
                    "person_id": 200,
                    "organization_id": null,
                    "value": {
                        "amount": 1000.00,
                        "currency": "USD"
                    },
                    "was_seen": false,
                    "add_time": "2024-01-01T10:00:00Z",
                    "update_time": "2024-01-01T10:00:00Z"
                },
                {
                    "id": "lead-2",
                    "title": "Lead Two",
                    "owner_id": null,
                    "person_id": null,
                    "organization_id": 300,
                    "value": {
                        "amount": 2500.50,
                        "currency": "EUR"
                    },
                    "was_seen": true,
                    "expected_close_date": "2024-06-30",
                    "add_time": "2024-01-02T11:00:00Z",
                    "update_time": "2024-01-02T11:00:00Z"
                }
            ],
            "additional_data": {
                "pagination": {
                    "start": 0,
                    "limit": 100,
                    "more_items_in_collection": false,
                    "next_start": null
                }
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListLead);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Count);

        // First lead
        var lead1 = response.Data[0];
        Assert.Equal("lead-1", lead1.Id);
        Assert.Equal("Lead One", lead1.Title);
        Assert.Equal(200, lead1.PersonId);
        Assert.Null(lead1.OrganizationId);
        Assert.False(lead1.WasSeen);
        Assert.Equal(1000.00m, lead1.Value?.Amount);
        Assert.Equal("USD", lead1.Value?.Currency);

        // Second lead
        var lead2 = response.Data[1];
        Assert.Equal("lead-2", lead2.Id);
        Assert.Equal("Lead Two", lead2.Title);
        Assert.Null(lead2.PersonId);
        Assert.Equal(300, lead2.OrganizationId);
        Assert.True(lead2.WasSeen);
        Assert.Equal("2024-06-30", lead2.ExpectedCloseDate);
        Assert.Equal(2500.50m, lead2.Value?.Amount);
        Assert.Equal("EUR", lead2.Value?.Currency);

        // Pagination
        Assert.NotNull(response.AdditionalData?.Pagination);
        Assert.Equal(0, response.AdditionalData.Pagination.Start);
        Assert.Equal(100, response.AdditionalData.Pagination.Limit);
        Assert.False(response.AdditionalData.Pagination.MoreItemsInCollection);
    }

    /// <summary>
    /// Test that Lead serialization works for create/update operations
    /// </summary>
    [Fact]
    public void Lead_Serialization_ForCreateUpdate()
    {
        // Arrange
        var lead = new Lead
        {
            Title = "New Lead",
            PersonId = 123,
            OrganizationId = null,
            Value = new LeadValue
            {
                Amount = 10000m,
                Currency = "USD"
            },
            ExpectedCloseDate = "2024-12-31"
        };

        // Act
        var json = JsonSerializer.Serialize(lead, ApiJsonContext.Default.Lead);
        var deserialized = JsonSerializer.Deserialize(json, ApiJsonContext.Default.Lead);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("New Lead", deserialized.Title);
        Assert.Equal(123, deserialized.PersonId);
        Assert.Null(deserialized.OrganizationId);
        Assert.Equal(10000m, deserialized.Value?.Amount);
        Assert.Equal("USD", deserialized.Value?.Currency);
        Assert.Equal("2024-12-31", deserialized.ExpectedCloseDate);
    }

    /// <summary>
    /// Test handling API error responses
    /// </summary>
    [Fact]
    public void Lead_Deserialization_HandlesErrorResponse()
    {
        // Arrange
        var json = """
        {
            "success": false,
            "error": "Lead not found",
            "error_info": "No lead found with ID: invalid-id",
            "data": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseLead);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Lead not found", response.Error);
        Assert.Equal("No lead found with ID: invalid-id", response.ErrorInfo);
        Assert.Null(response.Data);
    }

    /// <summary>
    /// Test lead with minimal required fields (person_id OR organization_id)
    /// </summary>
    [Fact]
    public void Lead_Deserialization_MinimalFields()
    {
        // Arrange - Minimal valid lead response
        var json = """
        {
            "success": true,
            "data": {
                "id": "minimal-lead",
                "title": "Minimal Lead",
                "owner_id": null,
                "person_id": 111,
                "organization_id": null,
                "value": null,
                "expected_close_date": null,
                "was_seen": false,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseLead);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("minimal-lead", response.Data.Id);
        Assert.Equal("Minimal Lead", response.Data.Title);
        Assert.Equal(111, response.Data.PersonId);
        Assert.Null(response.Data.OrganizationId);
        Assert.Null(response.Data.Value);
        Assert.Null(response.Data.ExpectedCloseDate);
    }

    /// <summary>
    /// Test lead with organization but no person
    /// </summary>
    [Fact]
    public void Lead_Deserialization_OrganizationOnly()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": "org-lead",
                "title": "Corporate Lead",
                "owner_id": null,
                "person_id": null,
                "organization_id": 555,
                "was_seen": true,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseLead);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Null(response.Data.PersonId);
        Assert.Equal(555, response.Data.OrganizationId);
    }

    /// <summary>
    /// Test lead with is_archived field (for archive/unarchive functionality)
    /// </summary>
    [Fact]
    public void Lead_Deserialization_HandlesIsArchivedField()
    {
        // Arrange - Lead with is_archived field
        var json = """
        {
            "success": true,
            "data": {
                "id": "archived-lead",
                "title": "Archived Lead",
                "owner_id": null,
                "person_id": 123,
                "organization_id": null,
                "is_archived": true,
                "was_seen": true,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseLead);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("archived-lead", response.Data.Id);
        Assert.True(response.Data.IsArchived);
    }

    /// <summary>
    /// Test lead serialization includes is_archived for update operations
    /// </summary>
    [Fact]
    public void Lead_Serialization_IncludesIsArchived()
    {
        // Arrange
        var lead = new Lead
        {
            IsArchived = true
        };

        // Act
        var json = JsonSerializer.Serialize(lead, ApiJsonContext.Default.Lead);

        // Assert - Check for the key and value (allowing for whitespace in pretty-printed JSON)
        Assert.Contains("is_archived", json);
        Assert.Contains("true", json);
    }

    /// <summary>
    /// Test lead with is_archived = false
    /// </summary>
    [Fact]
    public void Lead_Deserialization_HandlesIsArchivedFalse()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": "active-lead",
                "title": "Active Lead",
                "owner_id": null,
                "person_id": 456,
                "organization_id": null,
                "is_archived": false,
                "was_seen": false,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseLead);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.False(response.Data.IsArchived);
    }

    /// <summary>
    /// Test that empty list response deserializes correctly
    /// </summary>
    [Fact]
    public void LeadsList_Deserialization_EmptyList()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [],
            "additional_data": {
                "pagination": {
                    "start": 0,
                    "limit": 100,
                    "more_items_in_collection": false
                }
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListLead);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
    }

    /// <summary>
    /// Test serialization with null value (value is optional)
    /// </summary>
    [Fact]
    public void Lead_Serialization_NullValue()
    {
        // Arrange
        var lead = new Lead
        {
            Title = "Lead Without Value",
            PersonId = 123,
            Value = null,
            ExpectedCloseDate = null
        };

        // Act
        var json = JsonSerializer.Serialize(lead, ApiJsonContext.Default.Lead);

        // Assert - Should not include null fields (per JsonIgnoreCondition.WhenWritingNull)
        Assert.DoesNotContain("\"value\":", json);
        Assert.DoesNotContain("\"expected_close_date\":", json);
        Assert.Contains("\"title\":", json);
        Assert.Contains("\"person_id\":", json);
    }

    /// <summary>
    /// Test that Lead IDs with full UUID format are properly preserved through serialization.
    /// This documents the expected format and ensures UUIDs are not truncated or corrupted.
    /// Related to GitHub issue where table rendering could corrupt displayed UUIDs.
    /// </summary>
    [Theory]
    [InlineData("0b9fad60-bae0-11f0-945c-134e56493ed5")]
    [InlineData("d3ce8ea0-ba68-11f0-9055-d7d6d543086d")]
    [InlineData("79e8daf0-ba57-11f0-9055-d7d6d543086d")]
    [InlineData("bda0a9a0-bb31-11f0-bc3b-c55cf8762758")]
    public void Lead_Deserialization_PreservesFullUuidFormat(string expectedUuid)
    {
        // Arrange - Lead with full 36-character UUID
        var json = $$"""
        {
            "success": true,
            "data": {
                "id": "{{expectedUuid}}",
                "title": "Test Lead",
                "owner_id": null,
                "person_id": 12345,
                "organization_id": null,
                "was_seen": false,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseLead);

        // Assert - UUID must be preserved exactly as received from API
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        var leadId = response.Data.Id;
        Assert.NotNull(leadId);
        Assert.Equal(expectedUuid, leadId);
        Assert.Equal(36, leadId.Length); // UUIDs are always 36 chars (32 hex + 4 dashes)
        Assert.True(Guid.TryParse(leadId, out _), $"Lead ID should be valid UUID format: {leadId}");
    }

    /// <summary>
    /// Test that a list of leads preserves all UUIDs correctly.
    /// This helps catch any issues with UUID handling in list responses.
    /// </summary>
    [Fact]
    public void LeadsList_Deserialization_PreservesAllUuids()
    {
        // Arrange - Multiple leads with full UUIDs (from real production data patterns)
        var json = """
        {
            "success": true,
            "data": [
                {
                    "id": "0b9fad60-bae0-11f0-945c-134e56493ed5",
                    "title": "Lead 1",
                    "owner_id": null,
                    "person_id": 12869,
                    "organization_id": null,
                    "was_seen": false,
                    "add_time": "2024-01-01T00:00:00Z",
                    "update_time": "2024-01-01T00:00:00Z"
                },
                {
                    "id": "d3ce8ea0-ba68-11f0-9055-d7d6d543086d",
                    "title": "Lead 2",
                    "owner_id": null,
                    "person_id": 12860,
                    "organization_id": null,
                    "was_seen": false,
                    "add_time": "2024-01-01T00:00:00Z",
                    "update_time": "2024-01-01T00:00:00Z"
                },
                {
                    "id": "79e8daf0-ba57-11f0-9055-d7d6d543086d",
                    "title": "Lead 3",
                    "owner_id": null,
                    "person_id": 12843,
                    "organization_id": null,
                    "was_seen": false,
                    "add_time": "2024-01-01T00:00:00Z",
                    "update_time": "2024-01-01T00:00:00Z"
                },
                {
                    "id": "bda0a9a0-bb31-11f0-bc3b-c55cf8762758",
                    "title": "Lead 4",
                    "owner_id": null,
                    "person_id": 12872,
                    "organization_id": null,
                    "was_seen": false,
                    "add_time": "2024-01-01T00:00:00Z",
                    "update_time": "2024-01-01T00:00:00Z"
                }
            ],
            "additional_data": {
                "pagination": {
                    "start": 0,
                    "limit": 100,
                    "more_items_in_collection": false
                }
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListLead);

        // Assert - All UUIDs must be preserved exactly
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(4, response.Data.Count);

        // Verify each UUID is valid and preserved
        var expectedUuids = new[]
        {
            "0b9fad60-bae0-11f0-945c-134e56493ed5",
            "d3ce8ea0-ba68-11f0-9055-d7d6d543086d",
            "79e8daf0-ba57-11f0-9055-d7d6d543086d",
            "bda0a9a0-bb31-11f0-bc3b-c55cf8762758"
        };

        for (int i = 0; i < response.Data.Count; i++)
        {
            var lead = response.Data[i];
            var leadId = lead.Id;
            Assert.NotNull(leadId);
            Assert.Equal(expectedUuids[i], leadId);
            Assert.Equal(36, leadId.Length);
            Assert.True(Guid.TryParse(leadId, out _), $"Lead {i + 1} ID should be valid UUID: {leadId}");
        }
    }
}
