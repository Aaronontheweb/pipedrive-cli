using System.Text.Json;
using PipedriveCLI.Models;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Tests for Organizations API client methods and JSON serialization
/// </summary>
public class OrganizationsApiClientTests
{
    /// <summary>
    /// Test that Organization deserialization handles the full Pipedrive API response
    /// </summary>
    [Fact]
    public void Organization_Deserialization_HandlesFullApiResponse()
    {
        // Arrange - Full API response with all fields
        var json = """
        {
            "success": true,
            "data": {
                "id": 123,
                "name": "Acme Corporation",
                "people_count": 50,
                "owner_id": 456,
                "address": "123 Main St, San Francisco, CA 94105",
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
        Assert.Equal(123, response.Data.Id);
        Assert.Equal("Acme Corporation", response.Data.Name);
        Assert.Equal(50, response.Data.PeopleCount);
        Assert.Equal(456, response.Data.OwnerId);
        Assert.Equal("123 Main St, San Francisco, CA 94105", response.Data.Address);
        Assert.Equal("2024-01-15T10:30:00Z", response.Data.AddTime);
        Assert.Equal("2024-01-16T14:20:00Z", response.Data.UpdateTime);
    }

    /// <summary>
    /// Test deserializing a list of organizations
    /// </summary>
    [Fact]
    public void OrganizationsList_Deserialization_WithPagination()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [
                {
                    "id": 100,
                    "name": "Tech Startup Inc",
                    "people_count": 25,
                    "owner_id": 200,
                    "address": "456 Innovation Way, Austin, TX 78701",
                    "add_time": "2024-01-01T10:00:00Z",
                    "update_time": "2024-01-01T10:00:00Z"
                },
                {
                    "id": 101,
                    "name": "Global Enterprises",
                    "people_count": 500,
                    "owner_id": null,
                    "address": null,
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
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListOrganization);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Count);

        // First organization
        var org1 = response.Data[0];
        Assert.Equal(100, org1.Id);
        Assert.Equal("Tech Startup Inc", org1.Name);
        Assert.Equal(25, org1.PeopleCount);
        Assert.Equal(200, org1.OwnerId);
        Assert.Equal("456 Innovation Way, Austin, TX 78701", org1.Address);

        // Second organization
        var org2 = response.Data[1];
        Assert.Equal(101, org2.Id);
        Assert.Equal("Global Enterprises", org2.Name);
        Assert.Equal(500, org2.PeopleCount);
        Assert.Null(org2.OwnerId);
        Assert.Null(org2.Address);

        // Pagination
        Assert.NotNull(response.AdditionalData?.Pagination);
        Assert.Equal(0, response.AdditionalData.Pagination.Start);
        Assert.Equal(100, response.AdditionalData.Pagination.Limit);
        Assert.False(response.AdditionalData.Pagination.MoreItemsInCollection);
    }

    /// <summary>
    /// Test that Organization serialization works for create/update operations
    /// </summary>
    [Fact]
    public void Organization_Serialization_ForCreateUpdate()
    {
        // Arrange
        var organization = new Organization
        {
            Name = "New Company",
            Address = "789 Business Blvd, New York, NY 10001"
        };

        // Act
        var json = JsonSerializer.Serialize(organization, ApiJsonContext.Default.Organization);
        var deserialized = JsonSerializer.Deserialize(json, ApiJsonContext.Default.Organization);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("New Company", deserialized.Name);
        Assert.Equal("789 Business Blvd, New York, NY 10001", deserialized.Address);
    }

    /// <summary>
    /// Test handling API error responses
    /// </summary>
    [Fact]
    public void Organization_Deserialization_HandlesErrorResponse()
    {
        // Arrange
        var json = """
        {
            "success": false,
            "error": "Organization not found",
            "error_info": "No organization found with ID: 999999",
            "data": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseOrganization);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Organization not found", response.Error);
        Assert.Equal("No organization found with ID: 999999", response.ErrorInfo);
        Assert.Null(response.Data);
    }

    /// <summary>
    /// Test organization with minimal required fields
    /// </summary>
    [Fact]
    public void Organization_Deserialization_MinimalFields()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 500,
                "name": "Minimal Org",
                "people_count": 0,
                "owner_id": null,
                "address": null,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseOrganization);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(500, response.Data.Id);
        Assert.Equal("Minimal Org", response.Data.Name);
        Assert.Equal(0, response.Data.PeopleCount);
        Assert.Null(response.Data.OwnerId);
        Assert.Null(response.Data.Address);
    }

    /// <summary>
    /// Test organization with many people
    /// </summary>
    [Fact]
    public void Organization_Deserialization_LargePeopleCount()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 600,
                "name": "Large Corporation",
                "people_count": 10000,
                "owner_id": 999,
                "address": "1 Corporate Plaza, Chicago, IL 60601",
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseOrganization);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(10000, response.Data.PeopleCount);
        Assert.Equal("Large Corporation", response.Data.Name);
    }

    /// <summary>
    /// Test that empty list response deserializes correctly
    /// </summary>
    [Fact]
    public void OrganizationsList_Deserialization_EmptyList()
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
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListOrganization);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
    }

    /// <summary>
    /// Test organization with null address (common case)
    /// </summary>
    [Fact]
    public void Organization_Deserialization_NullAddress()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 700,
                "name": "Remote Company",
                "people_count": 15,
                "owner_id": 123,
                "address": null,
                "add_time": "2024-03-01T00:00:00Z",
                "update_time": "2024-03-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseOrganization);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("Remote Company", response.Data.Name);
        Assert.Null(response.Data.Address);
        Assert.Equal(15, response.Data.PeopleCount);
    }
}
