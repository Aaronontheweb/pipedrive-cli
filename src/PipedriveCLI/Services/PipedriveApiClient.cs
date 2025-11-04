using System.Net.Http.Json;
using System.Text.Json;
using PipedriveCLI.Models;

namespace PipedriveCLI.Services;

/// <summary>
/// HTTP client for interacting with the Pipedrive API
/// </summary>
public sealed class PipedriveApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ConfigurationService _configService;
    private bool _isConfigured;

    public PipedriveApiClient(HttpClient httpClient, ConfigurationService configService)
    {
        _httpClient = httpClient;
        _configService = configService;
    }

    /// <summary>
    /// Initializes the client with configuration
    /// </summary>
    public async Task InitializeAsync()
    {
        var profile = await _configService.GetActiveProfileAsync();

        if (!profile.IsValid())
        {
            throw new InvalidOperationException(
                "Pipedrive CLI is not configured. Run 'pipedrive config set --api-key <key> --domain <domain>' to configure.");
        }

        // Set base address using the domain from config
        _httpClient.BaseAddress = new Uri($"https://{profile.Domain}/api/v1/");

        // Store API key for use in requests
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        _isConfigured = true;
    }

    /// <summary>
    /// Makes a GET request to the Pipedrive API and returns raw JSON string
    /// </summary>
    public async Task<string> GetAsync(string endpoint, Dictionary<string, string>? queryParams = null)
    {
        EnsureConfigured();

        var url = await BuildUrlWithApiKeyAsync(endpoint, queryParams);
        var response = await _httpClient.GetAsync(url);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Makes a POST request to the Pipedrive API and returns raw JSON string
    /// </summary>
    public async Task<string> PostAsync(string endpoint, string jsonData, Dictionary<string, string>? queryParams = null)
    {
        EnsureConfigured();

        var url = await BuildUrlWithApiKeyAsync(endpoint, queryParams);
        var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Makes a PUT request to the Pipedrive API and returns raw JSON string
    /// </summary>
    public async Task<string> PutAsync(string endpoint, string jsonData, Dictionary<string, string>? queryParams = null)
    {
        EnsureConfigured();

        var url = await BuildUrlWithApiKeyAsync(endpoint, queryParams);
        var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync(url, content);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Makes a DELETE request to the Pipedrive API
    /// </summary>
    public async Task<bool> DeleteAsync(string endpoint, Dictionary<string, string>? queryParams = null)
    {
        EnsureConfigured();

        var url = await BuildUrlWithApiKeyAsync(endpoint, queryParams);
        var response = await _httpClient.DeleteAsync(url);

        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Tests the API connection by making a simple request
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await InitializeAsync();

            // Try to get user info to test connection
            var jsonResponse = await GetAsync("users/me");
            var response = JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseObject);
            return response?.Success ?? false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> BuildUrlWithApiKeyAsync(string endpoint, Dictionary<string, string>? queryParams)
    {
        var profile = await _configService.GetActiveProfileAsync();

        var uriBuilder = new UriBuilder(_httpClient.BaseAddress!)
        {
            Path = endpoint.TrimStart('/')
        };

        // Build query string with API key
        var query = new List<string> { $"api_token={Uri.EscapeDataString(profile.ApiKey!)}" };

        if (queryParams != null)
        {
            foreach (var (key, value) in queryParams)
            {
                query.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
            }
        }

        uriBuilder.Query = string.Join("&", query);

        return uriBuilder.Uri.PathAndQuery;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
        {
            throw new InvalidOperationException("Client not initialized. Call InitializeAsync() first.");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
