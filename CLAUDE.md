# CLAUDE.md - Project Guidelines for pipedrive-cli

## Testing Guidelines

### Unit Test Pattern
This project uses **JSON serialization/deserialization tests** for API models rather than testing command behavior directly. This approach is:

1. **Feasible** - No need to mock HttpClient or Spectre.Console output
2. **Valuable** - Catches API response format issues early (e.g., polymorphic fields like `mandatory_flag` that can be boolean OR object)
3. **Fast** - Pure in-memory tests that run in milliseconds

### When to Add Tests
When adding new model properties, especially those with custom JSON converters, add corresponding tests in the appropriate `*ApiClientTests.cs` file:

- Test deserialization from JSON (simulating API response)
- Test serialization to JSON (for create/update operations)
- Test edge cases (null values, missing fields, unexpected formats)

### Test File Locations
- `src/PipedriveCLI.Tests/LeadsCommandTests.cs` - Lead model tests
- `src/PipedriveCLI.Tests/DealsApiClientTests.cs` - Deal model tests
- `src/PipedriveCLI.Tests/ActivitiesApiClientTests.cs` - Activity model tests
- `src/PipedriveCLI.Tests/PersonsApiClientTests.cs` - Person model tests
- `src/PipedriveCLI.Tests/OrganizationsApiClientTests.cs` - Organization model tests

### Example Test Pattern
```csharp
[Fact]
public void Model_Deserialization_HandlesNewField()
{
    var json = """
    {
        "success": true,
        "data": {
            "id": "test-id",
            "new_field": true
        }
    }
    """;

    var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseModel);

    Assert.NotNull(response);
    Assert.True(response.Success);
    Assert.True(response.Data.NewField);
}
```

## Command Handler Pattern

For commands with 5+ options, use context-based parsing instead of lambda binding:

```csharp
command.SetHandler(async context =>
{
    var option1 = context.ParseResult.GetValueForOption(option1Option);
    var option2 = context.ParseResult.GetValueForOption(option2Option);
    // ... handle the command
});
```

## API Response Handling

The Pipedrive API returns `"data": null` (not empty array) when no results match a filter. Handle this gracefully:

```csharp
if (response?.Success == true)
{
    var items = response.Data ?? new List<Model>();
    if (items.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No items found[/]");
    }
    // ... display items
}
```
