using System.Text.Json;

namespace PipedriveCLI.Utilities;

/// <summary>
/// Helper methods for working with custom fields
/// </summary>
public static class CustomFieldHelper
{
    /// <summary>
    /// Formats custom fields into readable key-value pairs
    /// Returns formatted string for display, or empty string if no custom fields
    /// </summary>
    /// <param name="customFields">The custom fields dictionary</param>
    /// <param name="fieldNames">Optional mapping of hash keys to friendly names</param>
    /// <param name="useRawKeys">If true, display hash keys instead of friendly names</param>
    public static string FormatCustomFields(
        Dictionary<string, JsonElement>? customFields,
        Dictionary<string, string>? fieldNames = null,
        bool useRawKeys = false)
    {
        if (customFields == null || customFields.Count == 0)
        {
            return string.Empty;
        }

        var lines = new List<string>();
        foreach (var (key, value) in customFields)
        {
            // Skip standard Pipedrive fields (they don't have hash-like keys)
            if (key.Length < 30) // Hash keys are 40+ characters
                continue;

            var formattedValue = FormatJsonElement(value);

            // Use friendly name if available and not using raw keys
            var displayKey = key;
            if (!useRawKeys && fieldNames != null && fieldNames.TryGetValue(key, out var friendlyName))
            {
                displayKey = friendlyName;
            }

            lines.Add($"  [dim]{displayKey}:[/] {formattedValue}");
        }

        return lines.Count > 0 ? "\n[bold]Custom Fields:[/]\n" + string.Join("\n", lines) : string.Empty;
    }

    /// <summary>
    /// Formats a JsonElement into a readable string
    /// </summary>
    private static string FormatJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "-",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "Yes",
            JsonValueKind.False => "No",
            JsonValueKind.Null => "-",
            JsonValueKind.Array => $"[{element.GetArrayLength()} items]",
            JsonValueKind.Object => "[Object]",
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Parses custom field key-value pairs from command line arguments
    /// Format: key1=value1,key2=value2
    /// </summary>
    public static Dictionary<string, JsonElement>? ParseCustomFields(string? customFieldsString)
    {
        if (string.IsNullOrWhiteSpace(customFieldsString))
            return null;

        var customFields = new Dictionary<string, JsonElement>();
        var pairs = customFieldsString.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            // Try to parse as number, boolean, or string
            // Use JsonDocument.Parse to create JsonElement in an AOT-compatible way
            JsonElement element;
            if (int.TryParse(value, out var intValue))
            {
                using var doc = JsonDocument.Parse(intValue.ToString());
                element = doc.RootElement.Clone();
            }
            else if (bool.TryParse(value, out var boolValue))
            {
                using var doc = JsonDocument.Parse(boolValue.ToString().ToLower());
                element = doc.RootElement.Clone();
            }
            else
            {
                // For strings, we need to escape and quote them properly
                // Manually escape the string for JSON
                var escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
                var jsonString = $"\"{escaped}\"";
                using var doc = JsonDocument.Parse(jsonString);
                element = doc.RootElement.Clone();
            }

            customFields[key] = element;
        }

        return customFields.Count > 0 ? customFields : null;
    }
}
