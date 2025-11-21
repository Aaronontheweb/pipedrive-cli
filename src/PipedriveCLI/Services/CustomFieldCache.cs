using System.Text.Json;
using PipedriveCLI.Models;

namespace PipedriveCLI.Services;

/// <summary>
/// Service for caching custom field definitions to resolve hash keys to friendly names
/// </summary>
public class CustomFieldCache
{
    private readonly PipedriveApiClient _apiClient;
    private Dictionary<string, string>? _dealFieldNames;
    private Dictionary<string, string>? _personFieldNames;
    private Dictionary<string, string>? _organizationFieldNames;
    private bool _dealFieldsInitialized;
    private bool _personFieldsInitialized;
    private bool _organizationFieldsInitialized;

    public CustomFieldCache(PipedriveApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Get deal custom field names mapped from hash keys
    /// </summary>
    public async Task<Dictionary<string, string>> GetDealFieldNamesAsync()
    {
        if (!_dealFieldsInitialized)
        {
            try
            {
                _dealFieldNames = await GetFieldNamesAsync("dealFields");
            }
            catch
            {
                _dealFieldNames = new Dictionary<string, string>();
            }

            _dealFieldsInitialized = true;
        }

        return _dealFieldNames ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Generic method to extract field names from API response
    /// </summary>
    private async Task<Dictionary<string, string>> GetFieldNamesAsync(string endpoint)
    {
        var result = new Dictionary<string, string>();
        var jsonResponse = await _apiClient.GetAsync(endpoint);

        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement;

        if (root.TryGetProperty("success", out var success) && success.GetBoolean() &&
            root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var field in data.EnumerateArray())
            {
                // Only extract key and name properties
                if (field.TryGetProperty("key", out var keyProp) && keyProp.ValueKind == JsonValueKind.String)
                {
                    var key = keyProp.GetString();
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        var name = key; // Default to key if name is not available
                        if (field.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                        {
                            name = nameProp.GetString() ?? key;
                        }
                        result[key] = name;
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Get person custom field names mapped from hash keys
    /// </summary>
    public async Task<Dictionary<string, string>> GetPersonFieldNamesAsync()
    {
        if (!_personFieldsInitialized)
        {
            try
            {
                _personFieldNames = await GetFieldNamesAsync("personFields");
            }
            catch
            {
                _personFieldNames = new Dictionary<string, string>();
            }

            _personFieldsInitialized = true;
        }

        return _personFieldNames ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Get organization custom field names mapped from hash keys
    /// </summary>
    public async Task<Dictionary<string, string>> GetOrganizationFieldNamesAsync()
    {
        if (!_organizationFieldsInitialized)
        {
            try
            {
                _organizationFieldNames = await GetFieldNamesAsync("organizationFields");
            }
            catch
            {
                _organizationFieldNames = new Dictionary<string, string>();
            }

            _organizationFieldsInitialized = true;
        }

        return _organizationFieldNames ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Resolve a custom field name to its hash key for deals
    /// </summary>
    public async Task<string?> ResolveDealFieldNameToKeyAsync(string fieldName)
    {
        var fieldNames = await GetDealFieldNamesAsync();
        return fieldNames.FirstOrDefault(kvp =>
            kvp.Value.Equals(fieldName, StringComparison.OrdinalIgnoreCase)).Key;
    }

    /// <summary>
    /// Resolve a custom field name to its hash key for persons
    /// </summary>
    public async Task<string?> ResolvePersonFieldNameToKeyAsync(string fieldName)
    {
        var fieldNames = await GetPersonFieldNamesAsync();
        return fieldNames.FirstOrDefault(kvp =>
            kvp.Value.Equals(fieldName, StringComparison.OrdinalIgnoreCase)).Key;
    }

    /// <summary>
    /// Resolve a custom field name to its hash key for organizations
    /// </summary>
    public async Task<string?> ResolveOrganizationFieldNameToKeyAsync(string fieldName)
    {
        var fieldNames = await GetOrganizationFieldNamesAsync();
        return fieldNames.FirstOrDefault(kvp =>
            kvp.Value.Equals(fieldName, StringComparison.OrdinalIgnoreCase)).Key;
    }
}
