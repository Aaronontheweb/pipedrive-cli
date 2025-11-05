using System.Text.Json;
using PipedriveCLI.Models;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Tests for Notes API client methods and JSON serialization
/// </summary>
public class NotesApiClientTests
{
    /// <summary>
    /// Test that Note deserialization handles the full Pipedrive API response
    /// </summary>
    [Fact]
    public void Note_Deserialization_HandlesFullApiResponse()
    {
        // Arrange - Full API response with all fields
        var json = """
        {
            "success": true,
            "data": {
                "id": 123,
                "content": "This is a test note with <strong>HTML</strong> content",
                "active_flag": true,
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z",
                "user_id": 456,
                "deal_id": 789,
                "person_id": 111,
                "org_id": 222,
                "lead_id": "dd6b3950-b9e0-11f0-9c54-c1f964796ce6",
                "project_id": 333,
                "pinned_to_deal_flag": true,
                "pinned_to_person_flag": false,
                "pinned_to_organization_flag": false,
                "pinned_to_lead_flag": true,
                "pinned_to_project_flag": false
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseNote);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(123, response.Data.Id);
        Assert.Equal("This is a test note with <strong>HTML</strong> content", response.Data.Content);
        Assert.True(response.Data.ActiveFlag);
        Assert.Equal("2024-01-15T10:30:00Z", response.Data.AddTime);
        Assert.Equal("2024-01-16T14:20:00Z", response.Data.UpdateTime);
        Assert.Equal(456, response.Data.UserId);
        Assert.Equal(789, response.Data.DealId);
        Assert.Equal(111, response.Data.PersonId);
        Assert.Equal(222, response.Data.OrgId);
        Assert.Equal("dd6b3950-b9e0-11f0-9c54-c1f964796ce6", response.Data.LeadId);
        Assert.Equal(333, response.Data.ProjectId);
        Assert.True(response.Data.PinnedToDealFlag);
        Assert.False(response.Data.PinnedToPersonFlag);
        Assert.False(response.Data.PinnedToOrganizationFlag);
        Assert.True(response.Data.PinnedToLeadFlag);
        Assert.False(response.Data.PinnedToProjectFlag);
    }

    /// <summary>
    /// Test deserializing a list of notes
    /// </summary>
    [Fact]
    public void NotesList_Deserialization_WithPagination()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [
                {
                    "id": 100,
                    "content": "First note about the deal",
                    "active_flag": true,
                    "add_time": "2024-01-01T10:00:00Z",
                    "update_time": "2024-01-01T10:00:00Z",
                    "user_id": 10,
                    "deal_id": 50,
                    "person_id": null,
                    "org_id": null,
                    "lead_id": null,
                    "project_id": null,
                    "pinned_to_deal_flag": true,
                    "pinned_to_person_flag": false,
                    "pinned_to_organization_flag": false,
                    "pinned_to_lead_flag": false,
                    "pinned_to_project_flag": false
                },
                {
                    "id": 101,
                    "content": "Second note about the person",
                    "active_flag": true,
                    "add_time": "2024-01-02T11:00:00Z",
                    "update_time": "2024-01-02T11:00:00Z",
                    "user_id": 11,
                    "deal_id": null,
                    "person_id": 60,
                    "org_id": null,
                    "lead_id": null,
                    "project_id": null,
                    "pinned_to_deal_flag": false,
                    "pinned_to_person_flag": true,
                    "pinned_to_organization_flag": false,
                    "pinned_to_lead_flag": false,
                    "pinned_to_project_flag": false
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
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListNote);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Count);

        // First note
        var note1 = response.Data[0];
        Assert.Equal(100, note1.Id);
        Assert.Equal("First note about the deal", note1.Content);
        Assert.True(note1.ActiveFlag);
        Assert.Equal(10, note1.UserId);
        Assert.Equal(50, note1.DealId);
        Assert.Null(note1.PersonId);
        Assert.True(note1.PinnedToDealFlag);

        // Second note
        var note2 = response.Data[1];
        Assert.Equal(101, note2.Id);
        Assert.Equal("Second note about the person", note2.Content);
        Assert.Equal(11, note2.UserId);
        Assert.Null(note2.DealId);
        Assert.Equal(60, note2.PersonId);
        Assert.True(note2.PinnedToPersonFlag);

        // Pagination
        Assert.NotNull(response.AdditionalData?.Pagination);
        Assert.Equal(0, response.AdditionalData.Pagination.Start);
        Assert.Equal(100, response.AdditionalData.Pagination.Limit);
        Assert.False(response.AdditionalData.Pagination.MoreItemsInCollection);
    }

    /// <summary>
    /// Test that Note serialization works for create/update operations
    /// </summary>
    [Fact]
    public void Note_Serialization_ForCreateUpdate()
    {
        // Arrange
        var note = new Note
        {
            Content = "New note content",
            DealId = 123,
            PersonId = 456,
            PinnedToDealFlag = true
        };

        // Act
        var json = JsonSerializer.Serialize(note, ApiJsonContext.Default.Note);
        var deserialized = JsonSerializer.Deserialize(json, ApiJsonContext.Default.Note);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("New note content", deserialized.Content);
        Assert.Equal(123, deserialized.DealId);
        Assert.Equal(456, deserialized.PersonId);
        Assert.True(deserialized.PinnedToDealFlag);
    }

    /// <summary>
    /// Test handling API error responses
    /// </summary>
    [Fact]
    public void Note_Deserialization_HandlesErrorResponse()
    {
        // Arrange
        var json = """
        {
            "success": false,
            "error": "Note not found",
            "error_info": "No note found with ID: 999999",
            "data": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseNote);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Note not found", response.Error);
        Assert.Equal("No note found with ID: 999999", response.ErrorInfo);
        Assert.Null(response.Data);
    }

    /// <summary>
    /// Test note with minimal required fields
    /// </summary>
    [Fact]
    public void Note_Deserialization_MinimalFields()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 500,
                "content": "Minimal note",
                "active_flag": true,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z",
                "user_id": null,
                "deal_id": 100,
                "person_id": null,
                "org_id": null,
                "lead_id": null,
                "project_id": null
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseNote);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(500, response.Data.Id);
        Assert.Equal("Minimal note", response.Data.Content);
        Assert.True(response.Data.ActiveFlag);
        Assert.Null(response.Data.UserId);
        Assert.Equal(100, response.Data.DealId);
        Assert.Null(response.Data.PersonId);
        Assert.Null(response.Data.OrgId);
        Assert.Null(response.Data.LeadId);
    }

    /// <summary>
    /// Test note with HTML content
    /// </summary>
    [Fact]
    public void Note_Deserialization_HtmlContent()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 600,
                "content": "<h1>Meeting Notes</h1><p>Discussed <strong>Q4 strategy</strong> and <em>budget allocation</em>.</p><ul><li>Item 1</li><li>Item 2</li></ul>",
                "active_flag": true,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z",
                "user_id": 50,
                "deal_id": 200,
                "person_id": null,
                "org_id": null,
                "lead_id": null,
                "project_id": null,
                "pinned_to_deal_flag": true
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseNote);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(600, response.Data.Id);
        Assert.Contains("<h1>Meeting Notes</h1>", response.Data.Content);
        Assert.Contains("<strong>Q4 strategy</strong>", response.Data.Content);
        Assert.True(response.Data.PinnedToDealFlag);
    }

    /// <summary>
    /// Test that empty list response deserializes correctly
    /// </summary>
    [Fact]
    public void NotesList_Deserialization_EmptyList()
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
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListNote);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
    }

    /// <summary>
    /// Test that Note deserialization handles reference fields as objects (from actual API GET responses)
    /// The PipedriveReferenceConverter should extract the "value" property from nested objects
    /// </summary>
    [Fact]
    public void Note_Deserialization_HandlesReferenceFieldsAsObjects()
    {
        // Arrange - API response with reference fields as complex objects (actual API format)
        var json = """
        {
            "success": true,
            "data": {
                "id": 888,
                "content": "Important note about enterprise deal",
                "active_flag": true,
                "add_time": "2024-10-01T09:00:00Z",
                "update_time": "2024-11-01T15:30:00Z",
                "user_id": 999,
                "deal_id": {
                    "title": "Enterprise Software Deal",
                    "currency": "USD",
                    "status": "open",
                    "value": 11111
                },
                "person_id": {
                    "active_flag": true,
                    "name": "Sarah Johnson",
                    "email": [
                        {
                            "value": "sarah@enterprise.com",
                            "primary": true,
                            "label": "work"
                        }
                    ],
                    "owner_id": 23920819,
                    "value": 22222
                },
                "org_id": {
                    "name": "Enterprise Solutions Inc",
                    "people_count": 150,
                    "owner_id": 23920819,
                    "address": "456 Corporate Drive, Seattle, WA",
                    "active_flag": true,
                    "value": 33333
                },
                "lead_id": null,
                "project_id": null,
                "pinned_to_deal_flag": true,
                "pinned_to_person_flag": true,
                "pinned_to_organization_flag": false
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseNote);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(888, response.Data.Id);
        Assert.Equal("Important note about enterprise deal", response.Data.Content);
        Assert.True(response.Data.ActiveFlag);

        // Verify that the PipedriveReferenceConverter correctly extracted the "value" property from each object
        Assert.Equal(11111, response.Data.DealId);
        Assert.Equal(22222, response.Data.PersonId);
        Assert.Equal(33333, response.Data.OrgId);

        Assert.Equal(999, response.Data.UserId);
        Assert.True(response.Data.PinnedToDealFlag);
        Assert.True(response.Data.PinnedToPersonFlag);
        Assert.False(response.Data.PinnedToOrganizationFlag);
        Assert.Equal("2024-10-01T09:00:00Z", response.Data.AddTime);
        Assert.Equal("2024-11-01T15:30:00Z", response.Data.UpdateTime);
    }

    /// <summary>
    /// Test note with lead association
    /// </summary>
    [Fact]
    public void Note_Deserialization_WithLeadAssociation()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 777,
                "content": "Follow up on enterprise lead",
                "active_flag": true,
                "add_time": "2024-11-04T10:00:00Z",
                "update_time": "2024-11-04T10:00:00Z",
                "user_id": 50,
                "deal_id": null,
                "person_id": null,
                "org_id": null,
                "lead_id": "dd6b3950-b9e0-11f0-9c54-c1f964796ce6",
                "project_id": null,
                "pinned_to_lead_flag": true
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseNote);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(777, response.Data.Id);
        Assert.Equal("Follow up on enterprise lead", response.Data.Content);
        Assert.Equal("dd6b3950-b9e0-11f0-9c54-c1f964796ce6", response.Data.LeadId);
        Assert.Null(response.Data.DealId);
        Assert.Null(response.Data.PersonId);
        Assert.Null(response.Data.OrgId);
        Assert.True(response.Data.PinnedToLeadFlag);
    }

    /// <summary>
    /// Test note with all pinned flags
    /// </summary>
    [Fact]
    public void Note_Deserialization_WithAllPinnedFlags()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 999,
                "content": "Important note pinned everywhere",
                "active_flag": true,
                "add_time": "2024-11-04T10:00:00Z",
                "update_time": "2024-11-04T10:00:00Z",
                "user_id": 100,
                "deal_id": 200,
                "person_id": 300,
                "org_id": 400,
                "lead_id": "lead-123",
                "project_id": 500,
                "pinned_to_deal_flag": true,
                "pinned_to_person_flag": true,
                "pinned_to_organization_flag": true,
                "pinned_to_lead_flag": true,
                "pinned_to_project_flag": true
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseNote);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(999, response.Data.Id);
        Assert.True(response.Data.PinnedToDealFlag);
        Assert.True(response.Data.PinnedToPersonFlag);
        Assert.True(response.Data.PinnedToOrganizationFlag);
        Assert.True(response.Data.PinnedToLeadFlag);
        Assert.True(response.Data.PinnedToProjectFlag);
    }
}
