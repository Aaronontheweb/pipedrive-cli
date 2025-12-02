using System.Text.Json;
using PipedriveCLI.Models;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Tests for cc_email (Smart BCC) field deserialization across all entity types
/// </summary>
public class CcEmailDeserializationTests
{
    /// <summary>
    /// Test Lead deserialization with cc_email field
    /// </summary>
    [Fact]
    public void Lead_Deserialization_HandlesCcEmail()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": "abc-123-def",
                "title": "New Lead",
                "person_id": 456,
                "organization_id": 789,
                "owner_id": 1,
                "value": {
                    "amount": 5000.00,
                    "currency": "USD"
                },
                "expected_close_date": "2024-12-31",
                "was_seen": true,
                "cc_email": "petabridgellc-baa75d+lead123@pipedrivemail.com",
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseLead);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("abc-123-def", response.Data.Id);
        Assert.Equal("New Lead", response.Data.Title);
        Assert.Equal("petabridgellc-baa75d+lead123@pipedrivemail.com", response.Data.CcEmail);
    }

    /// <summary>
    /// Test Lead deserialization with null cc_email
    /// </summary>
    [Fact]
    public void Lead_Deserialization_HandlesNullCcEmail()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": "abc-123-def",
                "title": "Lead Without CC",
                "person_id": 456,
                "cc_email": null,
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseLead);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Null(response.Data.CcEmail);
    }

    /// <summary>
    /// Test Person deserialization with cc_email field
    /// </summary>
    [Fact]
    public void Person_Deserialization_HandlesCcEmail()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 123,
                "name": "John Doe",
                "first_name": "John",
                "last_name": "Doe",
                "email": [
                    {
                        "value": "john@example.com",
                        "primary": true,
                        "label": "work"
                    }
                ],
                "phone": [],
                "org_id": null,
                "cc_email": "petabridgellc-baa75d+person123@pipedrivemail.com",
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponsePerson);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(123, response.Data.Id);
        Assert.Equal("John Doe", response.Data.Name);
        Assert.Equal("petabridgellc-baa75d+person123@pipedrivemail.com", response.Data.CcEmail);
    }

    /// <summary>
    /// Test Person deserialization with null cc_email
    /// </summary>
    [Fact]
    public void Person_Deserialization_HandlesNullCcEmail()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 124,
                "name": "Jane Doe",
                "cc_email": null,
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponsePerson);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Null(response.Data.CcEmail);
    }

    /// <summary>
    /// Test Organization deserialization with cc_email field
    /// </summary>
    [Fact]
    public void Organization_Deserialization_HandlesCcEmail()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 789,
                "name": "Acme Corp",
                "people_count": 5,
                "address": "123 Main St, City, State 12345",
                "cc_email": "petabridgellc-baa75d+org789@pipedrivemail.com",
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseOrganization);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(789, response.Data.Id);
        Assert.Equal("Acme Corp", response.Data.Name);
        Assert.Equal("petabridgellc-baa75d+org789@pipedrivemail.com", response.Data.CcEmail);
    }

    /// <summary>
    /// Test Organization deserialization with null cc_email
    /// </summary>
    [Fact]
    public void Organization_Deserialization_HandlesNullCcEmail()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 790,
                "name": "Widget Inc",
                "people_count": 10,
                "cc_email": null,
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseOrganization);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Null(response.Data.CcEmail);
    }

    /// <summary>
    /// Test Activity deserialization with cc_email field
    /// </summary>
    [Fact]
    public void Activity_Deserialization_HandlesCcEmail()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 456,
                "subject": "Follow up call",
                "type": "call",
                "due_date": "2024-12-31",
                "due_time": "14:00",
                "done": false,
                "deal_id": 100,
                "person_id": 200,
                "org_id": 300,
                "note": "Discuss contract terms",
                "user_id": 1,
                "cc_email": "petabridgellc-baa75d+activity456@pipedrivemail.com",
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseActivity);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(456, response.Data.Id);
        Assert.Equal("Follow up call", response.Data.Subject);
        Assert.Equal("petabridgellc-baa75d+activity456@pipedrivemail.com", response.Data.CcEmail);
    }

    /// <summary>
    /// Test Activity deserialization with null cc_email
    /// </summary>
    [Fact]
    public void Activity_Deserialization_HandlesNullCcEmail()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 457,
                "subject": "Meeting",
                "type": "meeting",
                "due_date": "2024-12-31",
                "done": false,
                "cc_email": null,
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseActivity);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Null(response.Data.CcEmail);
    }

    /// <summary>
    /// Test that missing cc_email field doesn't cause deserialization errors
    /// </summary>
    [Fact]
    public void AllEntities_Deserialization_HandlesMissingCcEmail()
    {
        // Test Deal without cc_email
        var dealJson = """
        {
            "success": true,
            "data": {
                "id": 1,
                "title": "Test",
                "value": 0,
                "status": "open"
            }
        }
        """;
        var dealResponse = JsonSerializer.Deserialize(dealJson, ApiJsonContext.Default.PipedriveResponseDeal);
        Assert.NotNull(dealResponse?.Data);
        Assert.Null(dealResponse.Data.CcEmail);

        // Test Lead without cc_email
        var leadJson = """
        {
            "success": true,
            "data": {
                "id": "abc",
                "title": "Test Lead"
            }
        }
        """;
        var leadResponse = JsonSerializer.Deserialize(leadJson, ApiJsonContext.Default.PipedriveResponseLead);
        Assert.NotNull(leadResponse?.Data);
        Assert.Null(leadResponse.Data.CcEmail);

        // Test Person without cc_email
        var personJson = """
        {
            "success": true,
            "data": {
                "id": 1,
                "name": "Test Person"
            }
        }
        """;
        var personResponse = JsonSerializer.Deserialize(personJson, ApiJsonContext.Default.PipedriveResponsePerson);
        Assert.NotNull(personResponse?.Data);
        Assert.Null(personResponse.Data.CcEmail);

        // Test Organization without cc_email
        var orgJson = """
        {
            "success": true,
            "data": {
                "id": 1,
                "name": "Test Org",
                "people_count": 0
            }
        }
        """;
        var orgResponse = JsonSerializer.Deserialize(orgJson, ApiJsonContext.Default.PipedriveResponseOrganization);
        Assert.NotNull(orgResponse?.Data);
        Assert.Null(orgResponse.Data.CcEmail);

        // Test Activity without cc_email
        var activityJson = """
        {
            "success": true,
            "data": {
                "id": 1,
                "subject": "Test Activity",
                "done": false
            }
        }
        """;
        var activityResponse = JsonSerializer.Deserialize(activityJson, ApiJsonContext.Default.PipedriveResponseActivity);
        Assert.NotNull(activityResponse?.Data);
        Assert.Null(activityResponse.Data.CcEmail);
    }
}
