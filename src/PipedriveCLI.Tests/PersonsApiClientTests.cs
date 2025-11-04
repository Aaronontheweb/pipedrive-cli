using System.Text.Json;
using PipedriveCLI.Models;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Tests for Persons API client methods and JSON serialization
/// </summary>
public class PersonsApiClientTests
{
    /// <summary>
    /// Test that Person deserialization handles the full Pipedrive API response
    /// </summary>
    [Fact]
    public void Person_Deserialization_HandlesFullApiResponse()
    {
        // Arrange - Full API response with all fields
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
                    },
                    {
                        "value": "john.personal@example.com",
                        "primary": false,
                        "label": "home"
                    }
                ],
                "phone": [
                    {
                        "value": "+1-555-1234",
                        "primary": true,
                        "label": "work"
                    }
                ],
                "org_id": 456,
                "owner_id": 789,
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
        Assert.Equal("John", response.Data.FirstName);
        Assert.Equal("Doe", response.Data.LastName);
        Assert.Equal(456, response.Data.OrgId);
        Assert.Equal(789, response.Data.OwnerId);
        Assert.Equal("2024-01-15T10:30:00Z", response.Data.AddTime);
        Assert.Equal("2024-01-16T14:20:00Z", response.Data.UpdateTime);

        // Verify emails
        Assert.NotNull(response.Data.Email);
        Assert.Equal(2, response.Data.Email.Count);
        Assert.Equal("john@example.com", response.Data.Email[0].Value);
        Assert.True(response.Data.Email[0].Primary);
        Assert.Equal("work", response.Data.Email[0].Label);

        // Verify phones
        Assert.NotNull(response.Data.Phone);
        Assert.Single(response.Data.Phone);
        Assert.Equal("+1-555-1234", response.Data.Phone[0].Value);
        Assert.True(response.Data.Phone[0].Primary);
    }

    /// <summary>
    /// Test deserializing a list of persons
    /// </summary>
    [Fact]
    public void PersonsList_Deserialization_WithPagination()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [
                {
                    "id": 100,
                    "name": "Alice Smith",
                    "first_name": "Alice",
                    "last_name": "Smith",
                    "email": [
                        {
                            "value": "alice@company.com",
                            "primary": true,
                            "label": "work"
                        }
                    ],
                    "phone": null,
                    "org_id": 200,
                    "owner_id": 300,
                    "add_time": "2024-01-01T10:00:00Z",
                    "update_time": "2024-01-01T10:00:00Z"
                },
                {
                    "id": 101,
                    "name": "Bob Johnson",
                    "first_name": "Bob",
                    "last_name": "Johnson",
                    "email": null,
                    "phone": [
                        {
                            "value": "+1-555-9999",
                            "primary": true,
                            "label": "mobile"
                        }
                    ],
                    "org_id": null,
                    "owner_id": 300,
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
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListPerson);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Count);

        // First person
        var person1 = response.Data[0];
        Assert.Equal(100, person1.Id);
        Assert.Equal("Alice Smith", person1.Name);
        Assert.NotNull(person1.Email);
        Assert.Single(person1.Email);
        Assert.Equal("alice@company.com", person1.Email[0].Value);
        Assert.Null(person1.Phone);
        Assert.Equal(200, person1.OrgId);

        // Second person
        var person2 = response.Data[1];
        Assert.Equal(101, person2.Id);
        Assert.Equal("Bob Johnson", person2.Name);
        Assert.Null(person2.Email);
        Assert.NotNull(person2.Phone);
        Assert.Single(person2.Phone);
        Assert.Null(person2.OrgId);

        // Pagination
        Assert.NotNull(response.AdditionalData?.Pagination);
        Assert.Equal(0, response.AdditionalData.Pagination.Start);
        Assert.Equal(100, response.AdditionalData.Pagination.Limit);
        Assert.False(response.AdditionalData.Pagination.MoreItemsInCollection);
    }

    /// <summary>
    /// Test that Person serialization works for create/update operations
    /// </summary>
    [Fact]
    public void Person_Serialization_ForCreateUpdate()
    {
        // Arrange
        var person = new Person
        {
            Name = "Jane Doe",
            FirstName = "Jane",
            LastName = "Doe",
            Email = new List<Email>
            {
                new Email { Value = "jane@example.com", Primary = true, Label = "work" }
            },
            Phone = new List<Phone>
            {
                new Phone { Value = "+1-555-5678", Primary = true, Label = "mobile" }
            },
            OrgId = 123
        };

        // Act
        var json = JsonSerializer.Serialize(person, ApiJsonContext.Default.Person);
        var deserialized = JsonSerializer.Deserialize(json, ApiJsonContext.Default.Person);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("Jane Doe", deserialized.Name);
        Assert.Equal("Jane", deserialized.FirstName);
        Assert.Equal("Doe", deserialized.LastName);
        Assert.NotNull(deserialized.Email);
        Assert.Single(deserialized.Email);
        Assert.Equal("jane@example.com", deserialized.Email[0].Value);
        Assert.NotNull(deserialized.Phone);
        Assert.Single(deserialized.Phone);
        Assert.Equal("+1-555-5678", deserialized.Phone[0].Value);
        Assert.Equal(123, deserialized.OrgId);
    }

    /// <summary>
    /// Test handling API error responses
    /// </summary>
    [Fact]
    public void Person_Deserialization_HandlesErrorResponse()
    {
        // Arrange
        var json = """
        {
            "success": false,
            "error": "Person not found",
            "error_info": "No person found with ID: 999999",
            "data": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponsePerson);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Person not found", response.Error);
        Assert.Equal("No person found with ID: 999999", response.ErrorInfo);
        Assert.Null(response.Data);
    }

    /// <summary>
    /// Test person with minimal required fields
    /// </summary>
    [Fact]
    public void Person_Deserialization_MinimalFields()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 500,
                "name": "Minimal Person",
                "first_name": null,
                "last_name": null,
                "email": null,
                "phone": null,
                "org_id": null,
                "owner_id": null,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponsePerson);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(500, response.Data.Id);
        Assert.Equal("Minimal Person", response.Data.Name);
        Assert.Null(response.Data.FirstName);
        Assert.Null(response.Data.LastName);
        Assert.Null(response.Data.Email);
        Assert.Null(response.Data.Phone);
        Assert.Null(response.Data.OrgId);
    }

    /// <summary>
    /// Test person with multiple emails and phones
    /// </summary>
    [Fact]
    public void Person_Deserialization_MultipleContactMethods()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 600,
                "name": "Contact Person",
                "email": [
                    {
                        "value": "primary@example.com",
                        "primary": true,
                        "label": "work"
                    },
                    {
                        "value": "secondary@example.com",
                        "primary": false,
                        "label": "personal"
                    },
                    {
                        "value": "other@example.com",
                        "primary": false,
                        "label": "other"
                    }
                ],
                "phone": [
                    {
                        "value": "+1-555-0001",
                        "primary": true,
                        "label": "work"
                    },
                    {
                        "value": "+1-555-0002",
                        "primary": false,
                        "label": "home"
                    }
                ],
                "org_id": null,
                "owner_id": null,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponsePerson);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Email);
        Assert.Equal(3, response.Data.Email.Count);
        Assert.NotNull(response.Data.Phone);
        Assert.Equal(2, response.Data.Phone.Count);

        // Verify primary email
        var primaryEmail = response.Data.Email.FirstOrDefault(e => e.Primary);
        Assert.NotNull(primaryEmail);
        Assert.Equal("primary@example.com", primaryEmail.Value);

        // Verify primary phone
        var primaryPhone = response.Data.Phone.FirstOrDefault(p => p.Primary);
        Assert.NotNull(primaryPhone);
        Assert.Equal("+1-555-0001", primaryPhone.Value);
    }

    /// <summary>
    /// Test that empty list response deserializes correctly
    /// </summary>
    [Fact]
    public void PersonsList_Deserialization_EmptyList()
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
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListPerson);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
    }
}
