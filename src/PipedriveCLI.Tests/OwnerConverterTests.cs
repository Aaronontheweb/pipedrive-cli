using System.Text.Json;
using PipedriveCLI.Models;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Regression tests for GitHub issue #179: Pipedrive returns <c>owner_id</c> in two different
/// shapes depending on the endpoint.
///
/// - <c>GET /persons/{id}</c> and <c>GET /organizations/{id}</c> return <c>owner_id</c> as an
///   OBJECT: <c>{"id": .., "name": .., "email": ..}</c>
/// - <c>PUT /persons/{id}/merge</c> and <c>PUT /organizations/{id}/merge</c> return
///   <c>owner_id</c> as a SCALAR INT
///
/// <see cref="OwnerConverter"/> must accept both shapes so that a successful merge is correctly
/// reported as successful instead of throwing a JSON deserialization exception.
/// </summary>
public class OwnerConverterTests
{
    /// <summary>
    /// The scalar-int shape returned by merge endpoints must deserialize to an Owner with
    /// only Id populated.
    /// </summary>
    [Fact]
    public void OwnerConverter_ScalarInt_DeserializesToOwnerWithIdOnly()
    {
        // Arrange
        var json = """{ "owner_id": 10204689 }""";

        // Act
        var person = JsonSerializer.Deserialize(WrapAsPerson(json), ApiJsonContext.Default.Person);

        // Assert
        Assert.NotNull(person);
        Assert.NotNull(person.OwnerId);
        Assert.Equal(10204689, person.OwnerId.Id);
        Assert.Null(person.OwnerId.Name);
        Assert.Null(person.OwnerId.Email);
        Assert.Equal(0, person.OwnerId.Value);
    }

    /// <summary>
    /// The object shape returned by GET endpoints must continue to deserialize with all fields
    /// populated (no regression from adding scalar support).
    /// </summary>
    [Fact]
    public void OwnerConverter_Object_DeserializesAllFields()
    {
        // Arrange
        var json = """
        {
            "owner_id": {
                "id": 10204689,
                "name": "Aaron Stannard",
                "email": "aaron@petabridge.com",
                "value": 10204689
            }
        }
        """;

        // Act
        var person = JsonSerializer.Deserialize(WrapAsPerson(json), ApiJsonContext.Default.Person);

        // Assert
        Assert.NotNull(person);
        Assert.NotNull(person.OwnerId);
        Assert.Equal(10204689, person.OwnerId.Id);
        Assert.Equal("Aaron Stannard", person.OwnerId.Name);
        Assert.Equal("aaron@petabridge.com", person.OwnerId.Email);
        Assert.Equal(10204689, person.OwnerId.Value);
    }

    [Fact]
    public void OwnerConverter_Null_DeserializesToNull()
    {
        // Arrange
        var json = """{ "owner_id": null }""";

        // Act
        var person = JsonSerializer.Deserialize(WrapAsPerson(json), ApiJsonContext.Default.Person);

        // Assert
        Assert.NotNull(person);
        Assert.Null(person.OwnerId);
    }

    /// <summary>
    /// Round-trip the scalar shape: deserialize then re-serialize. The converter always writes
    /// the object shape (Pipedrive's write side accepts it), so this documents that behavior
    /// while confirming no data is lost for the Id.
    /// </summary>
    [Fact]
    public void OwnerConverter_ScalarInt_RoundTripsThroughSerialization()
    {
        // Arrange
        var json = """{ "owner_id": 555 }""";
        var person = JsonSerializer.Deserialize(WrapAsPerson(json), ApiJsonContext.Default.Person);
        Assert.NotNull(person);

        // Act
        var reserialized = JsonSerializer.Serialize(person, ApiJsonContext.Default.Person);
        var roundTripped = JsonSerializer.Deserialize(reserialized, ApiJsonContext.Default.Person);

        // Assert
        Assert.NotNull(roundTripped);
        Assert.NotNull(roundTripped.OwnerId);
        Assert.Equal(555, roundTripped.OwnerId.Id);
    }

    /// <summary>
    /// Round-trip the object shape: deserialize then re-serialize, verifying every field survives.
    /// </summary>
    [Fact]
    public void OwnerConverter_Object_RoundTripsThroughSerialization()
    {
        // Arrange
        var json = """
        {
            "owner_id": {
                "id": 42,
                "name": "Jane Doe",
                "email": "jane@example.com",
                "value": 42
            }
        }
        """;
        var person = JsonSerializer.Deserialize(WrapAsPerson(json), ApiJsonContext.Default.Person);
        Assert.NotNull(person);

        // Act
        var reserialized = JsonSerializer.Serialize(person, ApiJsonContext.Default.Person);
        var roundTripped = JsonSerializer.Deserialize(reserialized, ApiJsonContext.Default.Person);

        // Assert
        Assert.NotNull(roundTripped);
        Assert.NotNull(roundTripped.OwnerId);
        Assert.Equal(42, roundTripped.OwnerId.Id);
        Assert.Equal("Jane Doe", roundTripped.OwnerId.Name);
        Assert.Equal("jane@example.com", roundTripped.OwnerId.Email);
        Assert.Equal(42, roundTripped.OwnerId.Value);
    }

    /// <summary>
    /// Organization.OwnerId must accept the same scalar shape - the merge endpoint for
    /// organizations exhibits the same inconsistency as persons.
    /// </summary>
    [Fact]
    public void OwnerConverter_ScalarInt_AppliesToOrganizationOwnerIdToo()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 999,
                "name": "Acme Corporation (Merged)",
                "people_count": 50,
                "owner_id": 10
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseOrganization);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.OwnerId);
        Assert.Equal(10, response.Data.OwnerId.Id);
        Assert.Null(response.Data.OwnerId.Name);
    }

    private static string WrapAsPerson(string ownerIdJson)
    {
        // ownerIdJson is a JSON object literal like { "owner_id": ... }; splice its contents
        // into a minimal Person payload.
        var inner = ownerIdJson.Trim();
        inner = inner.Substring(1, inner.Length - 2); // strip outer braces
        return $$"""
        {
            "id": 1,
            "name": "Test Person",
            {{inner}}
        }
        """;
    }
}
