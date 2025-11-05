using System.Text.Json;
using PipedriveCLI.Models;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Tests for Merge API client methods and JSON serialization
/// </summary>
public class MergeApiClientTests
{
    /// <summary>
    /// Test MergeRequest serialization
    /// </summary>
    [Fact]
    public void MergeRequest_Serialization()
    {
        // Arrange
        var mergeRequest = new MergeRequest { MergeWithId = 123 };

        // Act
        var json = JsonSerializer.Serialize(mergeRequest, ApiJsonContext.Default.MergeRequest);
        var deserialized = JsonSerializer.Deserialize(json, ApiJsonContext.Default.MergeRequest);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(123, deserialized.MergeWithId);
        Assert.Contains("\"merge_with_id\"", json);
        Assert.Contains("123", json);
    }

    /// <summary>
    /// Test Person merge response deserialization
    /// </summary>
    [Fact]
    public void PersonMerge_Deserialization_Success()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 456,
                "name": "John Smith (Merged)",
                "first_name": "John",
                "last_name": "Smith",
                "email": [
                    {
                        "value": "john@example.com",
                        "primary": true,
                        "label": "work"
                    },
                    {
                        "value": "jsmith@company.com",
                        "primary": false,
                        "label": "work"
                    }
                ],
                "phone": [
                    {
                        "value": "+1234567890",
                        "primary": true,
                        "label": "work"
                    }
                ],
                "org_id": 789,
                "add_time": "2024-01-01T10:00:00Z",
                "update_time": "2024-11-05T15:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponsePerson);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(456, response.Data.Id);
        Assert.Equal("John Smith (Merged)", response.Data.Name);
        Assert.Equal(2, response.Data.Email?.Count);
        Assert.Equal("john@example.com", response.Data.Email?[0].Value);
        Assert.Equal("jsmith@company.com", response.Data.Email?[1].Value);
    }

    /// <summary>
    /// Test Deal merge response deserialization
    /// </summary>
    [Fact]
    public void DealMerge_Deserialization_Success()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 789,
                "title": "Enterprise Deal (Merged)",
                "value": 150000.00,
                "currency": "USD",
                "person_id": 123,
                "org_id": 456,
                "stage_id": 5,
                "status": "open",
                "probability": 75,
                "add_time": "2024-01-01T10:00:00Z",
                "update_time": "2024-11-05T15:00:00Z",
                "expected_close_date": "2024-12-31"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseDeal);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(789, response.Data.Id);
        Assert.Equal("Enterprise Deal (Merged)", response.Data.Title);
        Assert.Equal(150000.00m, response.Data.Value);
        Assert.Equal("USD", response.Data.Currency);
        Assert.Equal(123, response.Data.PersonId);
        Assert.Equal(456, response.Data.OrgId);
    }

    /// <summary>
    /// Test Organization merge response deserialization
    /// </summary>
    [Fact]
    public void OrganizationMerge_Deserialization_Success()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 999,
                "name": "Acme Corporation (Merged)",
                "people_count": 50,
                "owner_id": {
                    "id": 10,
                    "name": "Sales Manager",
                    "email": "manager@company.com",
                    "value": 10
                },
                "address": "123 Main St, New York, NY 10001",
                "add_time": "2024-01-01T10:00:00Z",
                "update_time": "2024-11-05T15:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseOrganization);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(999, response.Data.Id);
        Assert.Equal("Acme Corporation (Merged)", response.Data.Name);
        Assert.Equal(50, response.Data.PeopleCount);
        Assert.Equal("123 Main St, New York, NY 10001", response.Data.Address);
        Assert.NotNull(response.Data.OwnerId);
        Assert.Equal(10, response.Data.OwnerId.Id);
    }

    /// <summary>
    /// Test merge error response
    /// </summary>
    [Fact]
    public void Merge_Deserialization_ErrorResponse()
    {
        // Arrange
        var json = """
        {
            "success": false,
            "error": "Cannot merge: Entity not found",
            "error_info": "Person with ID 99999 does not exist",
            "data": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponsePerson);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Cannot merge: Entity not found", response.Error);
        Assert.Equal("Person with ID 99999 does not exist", response.ErrorInfo);
        Assert.Null(response.Data);
    }

    /// <summary>
    /// Test merge with reference fields as objects (actual API format)
    /// </summary>
    [Fact]
    public void PersonMerge_Deserialization_WithReferenceFields()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 555,
                "name": "Jane Doe (Merged)",
                "first_name": "Jane",
                "last_name": "Doe",
                "email": [
                    {
                        "value": "jane@example.com",
                        "primary": true,
                        "label": "work"
                    }
                ],
                "phone": [],
                "org_id": {
                    "name": "Tech Solutions Inc",
                    "people_count": 25,
                    "owner_id": 10,
                    "address": "456 Tech Blvd",
                    "active_flag": true,
                    "value": 888
                },
                "add_time": "2024-01-01T10:00:00Z",
                "update_time": "2024-11-05T15:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponsePerson);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(555, response.Data.Id);
        Assert.Equal("Jane Doe (Merged)", response.Data.Name);
        // PipedriveReferenceConverter should extract the value property
        Assert.Equal(888, response.Data.OrgId);
    }

    /// <summary>
    /// Test deal merge with nested reference fields
    /// </summary>
    [Fact]
    public void DealMerge_Deserialization_WithReferenceFields()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 1000,
                "title": "Major Contract (Merged)",
                "value": 250000.00,
                "currency": "USD",
                "person_id": {
                    "active_flag": true,
                    "name": "Alice Johnson",
                    "email": [
                        {
                            "value": "alice@company.com",
                            "primary": true,
                            "label": "work"
                        }
                    ],
                    "owner_id": 10,
                    "value": 333
                },
                "org_id": {
                    "name": "Enterprise Corp",
                    "people_count": 100,
                    "owner_id": 10,
                    "address": "789 Enterprise Way",
                    "active_flag": true,
                    "value": 444
                },
                "stage_id": 3,
                "status": "open",
                "add_time": "2024-01-01T10:00:00Z",
                "update_time": "2024-11-05T15:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseDeal);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(1000, response.Data.Id);
        Assert.Equal("Major Contract (Merged)", response.Data.Title);
        // PipedriveReferenceConverter should extract the value properties
        Assert.Equal(333, response.Data.PersonId);
        Assert.Equal(444, response.Data.OrgId);
    }

    /// <summary>
    /// Test merge with conflicting data - person with multiple emails and phones
    /// </summary>
    [Fact]
    public void PersonMerge_Deserialization_ConflictingData()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 777,
                "name": "Bob Williams (Merged)",
                "first_name": "Bob",
                "last_name": "Williams",
                "email": [
                    {
                        "value": "bob@company1.com",
                        "primary": true,
                        "label": "work"
                    },
                    {
                        "value": "bob@company2.com",
                        "primary": false,
                        "label": "work"
                    },
                    {
                        "value": "bwilliams@personal.com",
                        "primary": false,
                        "label": "home"
                    }
                ],
                "phone": [
                    {
                        "value": "+1111111111",
                        "primary": true,
                        "label": "work"
                    },
                    {
                        "value": "+2222222222",
                        "primary": false,
                        "label": "mobile"
                    }
                ],
                "org_id": 999,
                "add_time": "2024-01-01T10:00:00Z",
                "update_time": "2024-11-05T15:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponsePerson);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(777, response.Data.Id);
        Assert.Equal("Bob Williams (Merged)", response.Data.Name);
        Assert.Equal(3, response.Data.Email?.Count);
        Assert.Equal(2, response.Data.Phone?.Count);
        // Verify primary email
        Assert.True(response.Data.Email?[0].Primary);
        Assert.Equal("bob@company1.com", response.Data.Email?[0].Value);
        // Verify primary phone
        Assert.True(response.Data.Phone?[0].Primary);
        Assert.Equal("+1111111111", response.Data.Phone?[0].Value);
    }
}
