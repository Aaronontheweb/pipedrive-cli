using System.Text.Json.Serialization;

namespace PipedriveCLI.Models;

/// <summary>
/// Root configuration object stored in ~/.pipedrive/config.json
/// </summary>
public sealed class PipedriveConfig
{
    /// <summary>
    /// Available configuration profiles (e.g., "default", "staging", "production")
    /// </summary>
    [JsonPropertyName("profiles")]
    public Dictionary<string, ProfileConfig> Profiles { get; set; } = new();

    /// <summary>
    /// Currently active profile name
    /// </summary>
    [JsonPropertyName("activeProfile")]
    public string ActiveProfile { get; set; } = "default";
}

/// <summary>
/// Configuration for a single profile
/// </summary>
public sealed class ProfileConfig
{
    /// <summary>
    /// Pipedrive API key/token
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Pipedrive domain (e.g., "company.pipedrive.com")
    /// </summary>
    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    /// <summary>
    /// Optional: Email Gateway URL for email approval workflow
    /// </summary>
    [JsonPropertyName("emailGatewayUrl")]
    public string? EmailGatewayUrl { get; set; }

    /// <summary>
    /// Optional: Email Gateway API key for submitting drafts
    /// </summary>
    [JsonPropertyName("emailGatewayApiKey")]
    public string? EmailGatewayApiKey { get; set; }

    /// <summary>
    /// Validates that required fields are present
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Domain);
    }
}

/// <summary>
/// JSON source generator context for Native AOT compatibility
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PipedriveConfig))]
[JsonSerializable(typeof(ProfileConfig))]
[JsonSerializable(typeof(Dictionary<string, ProfileConfig>))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}
