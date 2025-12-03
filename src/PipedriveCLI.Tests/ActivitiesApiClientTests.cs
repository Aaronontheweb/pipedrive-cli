using System.Text.Json;
using PipedriveCLI.Models;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Tests for Activities API client methods and JSON serialization
/// </summary>
public class ActivitiesApiClientTests
{
    /// <summary>
    /// Test that Activity deserialization handles the full Pipedrive API response
    /// </summary>
    [Fact]
    public void Activity_Deserialization_HandlesFullApiResponse()
    {
        // Arrange - Full API response with all fields
        var json = """
        {
            "success": true,
            "data": {
                "id": 123,
                "subject": "Follow-up call",
                "type": "call",
                "due_date": "2024-12-25",
                "due_time": "14:30",
                "done": false,
                "deal_id": 456,
                "person_id": 789,
                "org_id": null,
                "note": "Discuss pricing and contract terms",
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z",
                "marked_as_done_time": null,
                "owner_id": null,
                "creator_user_id": 888
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseActivity);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(123, response.Data.Id);
        Assert.Equal("Follow-up call", response.Data.Subject);
        Assert.Equal("call", response.Data.Type);
        Assert.Equal("2024-12-25", response.Data.DueDate);
        Assert.Equal("14:30", response.Data.DueTime);
        Assert.False(response.Data.Done);
        Assert.Equal(456, response.Data.DealId);
        Assert.Equal(789, response.Data.PersonId);
        Assert.Null(response.Data.OrgId);
        Assert.Equal("Discuss pricing and contract terms", response.Data.Note);
        Assert.Equal("2024-01-15T10:30:00Z", response.Data.AddTime);
        Assert.Equal("2024-01-16T14:20:00Z", response.Data.UpdateTime);
    }

    /// <summary>
    /// Test deserializing a list of activities
    /// </summary>
    [Fact]
    public void ActivitiesList_Deserialization_WithPagination()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [
                {
                    "id": 100,
                    "subject": "Meeting with client",
                    "type": "meeting",
                    "due_date": "2024-01-20",
                    "due_time": "10:00",
                    "done": false,
                    "deal_id": 200,
                    "person_id": 300,
                    "org_id": null,
                    "note": "Present quarterly report",
                    "add_time": "2024-01-01T10:00:00Z",
                    "update_time": "2024-01-01T10:00:00Z"
                },
                {
                    "id": 101,
                    "subject": "Send proposal",
                    "type": "email",
                    "due_date": "2024-01-25",
                    "due_time": null,
                    "done": true,
                    "deal_id": null,
                    "person_id": null,
                    "org_id": 400,
                    "note": null,
                    "add_time": "2024-01-02T11:00:00Z",
                    "update_time": "2024-01-15T09:30:00Z"
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
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListActivity);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Count);

        // First activity
        var activity1 = response.Data[0];
        Assert.Equal(100, activity1.Id);
        Assert.Equal("Meeting with client", activity1.Subject);
        Assert.Equal("meeting", activity1.Type);
        Assert.Equal("2024-01-20", activity1.DueDate);
        Assert.Equal("10:00", activity1.DueTime);
        Assert.False(activity1.Done);
        Assert.Equal(200, activity1.DealId);
        Assert.Equal(300, activity1.PersonId);
        Assert.Null(activity1.OrgId);

        // Second activity
        var activity2 = response.Data[1];
        Assert.Equal(101, activity2.Id);
        Assert.Equal("Send proposal", activity2.Subject);
        Assert.Equal("email", activity2.Type);
        Assert.Null(activity2.DueTime);
        Assert.True(activity2.Done);
        Assert.Null(activity2.DealId);
        Assert.Null(activity2.PersonId);
        Assert.Equal(400, activity2.OrgId);

        // Pagination
        Assert.NotNull(response.AdditionalData?.Pagination);
        Assert.Equal(0, response.AdditionalData.Pagination.Start);
        Assert.Equal(100, response.AdditionalData.Pagination.Limit);
        Assert.False(response.AdditionalData.Pagination.MoreItemsInCollection);
    }

    /// <summary>
    /// Test that Activity serialization works for create/update operations
    /// </summary>
    [Fact]
    public void Activity_Serialization_ForCreateUpdate()
    {
        // Arrange
        var activity = new Activity
        {
            Subject = "New Meeting",
            Type = "meeting",
            DueDate = "2024-12-31",
            DueTime = "15:00",
            Done = false,
            DealId = 123,
            PersonId = 456,
            OrgId = null,
            Note = "Discuss Q4 results"
        };

        // Act
        var json = JsonSerializer.Serialize(activity, ApiJsonContext.Default.Activity);
        var deserialized = JsonSerializer.Deserialize(json, ApiJsonContext.Default.Activity);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("New Meeting", deserialized.Subject);
        Assert.Equal("meeting", deserialized.Type);
        Assert.Equal("2024-12-31", deserialized.DueDate);
        Assert.Equal("15:00", deserialized.DueTime);
        Assert.False(deserialized.Done);
        Assert.Equal(123, deserialized.DealId);
        Assert.Equal(456, deserialized.PersonId);
        Assert.Null(deserialized.OrgId);
        Assert.Equal("Discuss Q4 results", deserialized.Note);
    }

    /// <summary>
    /// Test handling API error responses
    /// </summary>
    [Fact]
    public void Activity_Deserialization_HandlesErrorResponse()
    {
        // Arrange
        var json = """
        {
            "success": false,
            "error": "Activity not found",
            "error_info": "No activity found with ID: 999999",
            "data": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseActivity);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Activity not found", response.Error);
        Assert.Equal("No activity found with ID: 999999", response.ErrorInfo);
        Assert.Null(response.Data);
    }

    /// <summary>
    /// Test activity with minimal required fields
    /// </summary>
    [Fact]
    public void Activity_Deserialization_MinimalFields()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 500,
                "subject": "Minimal Activity",
                "type": "task",
                "due_date": "2024-02-01",
                "due_time": null,
                "done": false,
                "deal_id": null,
                "person_id": null,
                "org_id": null,
                "note": null,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseActivity);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(500, response.Data.Id);
        Assert.Equal("Minimal Activity", response.Data.Subject);
        Assert.Equal("task", response.Data.Type);
        Assert.Equal("2024-02-01", response.Data.DueDate);
        Assert.Null(response.Data.DueTime);
        Assert.False(response.Data.Done);
        Assert.Null(response.Data.DealId);
        Assert.Null(response.Data.PersonId);
        Assert.Null(response.Data.OrgId);
        Assert.Null(response.Data.Note);
    }

    /// <summary>
    /// Test activity marked as done
    /// </summary>
    [Fact]
    public void Activity_Deserialization_DoneActivity()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 600,
                "subject": "Completed Task",
                "type": "call",
                "due_date": "2024-01-10",
                "due_time": "09:00",
                "done": true,
                "deal_id": 111,
                "person_id": 222,
                "org_id": null,
                "note": "Call completed successfully",
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-10T10:30:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseActivity);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.True(response.Data.Done);
        Assert.Equal("Completed Task", response.Data.Subject);
    }

    /// <summary>
    /// Test that empty list response deserializes correctly
    /// </summary>
    [Fact]
    public void ActivitiesList_Deserialization_EmptyList()
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
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListActivity);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
    }

    /// <summary>
    /// Test activity without due time (all-day activity)
    /// </summary>
    [Fact]
    public void Activity_Deserialization_AllDayActivity()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 700,
                "subject": "All-day Conference",
                "type": "meeting",
                "due_date": "2024-03-15",
                "due_time": null,
                "done": false,
                "deal_id": null,
                "person_id": 333,
                "org_id": 444,
                "note": "Annual company conference",
                "add_time": "2024-02-01T00:00:00Z",
                "update_time": "2024-02-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseActivity);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("2024-03-15", response.Data.DueDate);
        Assert.Null(response.Data.DueTime);
        Assert.Equal("All-day Conference", response.Data.Subject);
    }

    /// <summary>
    /// Test that Activity deserialization handles reference fields as objects (from actual API GET responses)
    /// The PipedriveReferenceConverter should extract the "value" property from nested objects
    /// </summary>
    [Fact]
    public void Activity_Deserialization_HandlesReferenceFieldsAsObjects()
    {
        // Arrange - API response with reference fields as complex objects (actual API format)
        var json = """
        {
            "success": true,
            "data": {
                "id": 999,
                "subject": "Test Activity with Complex References",
                "type": "meeting",
                "due_date": "2024-12-01",
                "due_time": "15:00",
                "done": false,
                "deal_id": {
                    "active_flag": true,
                    "title": "Big Enterprise Deal",
                    "value": 12345,
                    "currency": "USD",
                    "stage_id": 1,
                    "pipeline_id": 1
                },
                "person_id": {
                    "active_flag": true,
                    "name": "John Doe",
                    "email": [
                        {
                            "value": "john@example.com",
                            "primary": true,
                            "label": "work"
                        }
                    ],
                    "phone": [
                        {
                            "value": "+1-555-1234",
                            "primary": true
                        }
                    ],
                    "owner_id": 23920819,
                    "company_id": 999,
                    "value": 67890
                },
                "org_id": {
                    "name": "Acme Corporation",
                    "people_count": 25,
                    "owner_id": 23920819,
                    "address": "123 Main St",
                    "active_flag": true,
                    "cc_email": "acme@pipedrive.com",
                    "value": 54321
                },
                "note": "Important meeting with nested reference objects",
                "add_time": "2024-11-01T10:00:00Z",
                "update_time": "2024-11-01T10:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseActivity);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(999, response.Data.Id);
        Assert.Equal("Test Activity with Complex References", response.Data.Subject);
        Assert.Equal("meeting", response.Data.Type);
        Assert.Equal("2024-12-01", response.Data.DueDate);
        Assert.Equal("15:00", response.Data.DueTime);
        Assert.False(response.Data.Done);

        // Verify that the PipedriveReferenceConverter correctly extracted the "value" property from each object
        Assert.Equal(12345, response.Data.DealId);
        Assert.Equal(67890, response.Data.PersonId);
        Assert.Equal(54321, response.Data.OrgId);

        Assert.Equal("Important meeting with nested reference objects", response.Data.Note);
        Assert.Equal("2024-11-01T10:00:00Z", response.Data.AddTime);
        Assert.Equal("2024-11-01T10:00:00Z", response.Data.UpdateTime);
    }

    /// <summary>
    /// Test that Activity deserialization handles lead_id field
    /// </summary>
    [Fact]
    public void Activity_Deserialization_HandlesLeadId()
    {
        // Arrange - Activity with lead_id (leads use UUID strings, not integers)
        var json = """
        {
            "success": true,
            "data": {
                "id": 800,
                "subject": "Lead Follow-up",
                "type": "call",
                "due_date": "2024-12-15",
                "due_time": "11:00",
                "done": false,
                "deal_id": null,
                "person_id": null,
                "org_id": null,
                "lead_id": "adf21080-0e10-11eb-879b-05d71fb426ec",
                "note": "Follow up on new lead",
                "add_time": "2024-12-01T00:00:00Z",
                "update_time": "2024-12-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseActivity);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(800, response.Data.Id);
        Assert.Equal("Lead Follow-up", response.Data.Subject);
        Assert.Equal("adf21080-0e10-11eb-879b-05d71fb426ec", response.Data.LeadId);
        Assert.Null(response.Data.DealId);
        Assert.Null(response.Data.PersonId);
        Assert.Null(response.Data.OrgId);
    }

    /// <summary>
    /// Test activity serialization includes lead_id
    /// </summary>
    [Fact]
    public void Activity_Serialization_IncludesLeadId()
    {
        // Arrange
        var activity = new Activity
        {
            Subject = "Lead Call",
            Type = "call",
            DueDate = "2024-12-20",
            LeadId = "test-lead-uuid-1234"
        };

        // Act
        var json = JsonSerializer.Serialize(activity, ApiJsonContext.Default.Activity);
        var deserialized = JsonSerializer.Deserialize(json, ApiJsonContext.Default.Activity);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("Lead Call", deserialized.Subject);
        Assert.Equal("test-lead-uuid-1234", deserialized.LeadId);
        Assert.Contains("lead_id", json);
    }

    /// <summary>
    /// Test that activities with all null associations are handled (orphaned activities)
    /// </summary>
    [Fact]
    public void Activity_Deserialization_HandlesOrphanedActivity()
    {
        // Arrange - Activity with no associations (orphaned)
        var json = """
        {
            "success": true,
            "data": {
                "id": 900,
                "subject": "Orphaned Activity",
                "type": "task",
                "due_date": "2024-12-20",
                "due_time": null,
                "done": false,
                "deal_id": null,
                "person_id": null,
                "org_id": null,
                "lead_id": null,
                "note": "This activity has no associations",
                "add_time": "2024-12-01T00:00:00Z",
                "update_time": "2024-12-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseActivity);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(900, response.Data.Id);
        Assert.Equal("Orphaned Activity", response.Data.Subject);
        Assert.Null(response.Data.DealId);
        Assert.Null(response.Data.PersonId);
        Assert.Null(response.Data.OrgId);
        Assert.Null(response.Data.LeadId);
    }
}
