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
    /// Makes a PATCH request to the Pipedrive API and returns raw JSON string
    /// </summary>
    public async Task<string> PatchAsync(string endpoint, string jsonData, Dictionary<string, string>? queryParams = null)
    {
        EnsureConfigured();

        var url = await BuildUrlWithApiKeyAsync(endpoint, queryParams);
        var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
        {
            Content = content
        };
        var response = await _httpClient.SendAsync(request);

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

    #region Leads Operations

    /// <summary>
    /// Gets all leads
    /// </summary>
    public async Task<PipedriveResponse<List<Lead>>?> GetLeadsAsync(int? limit = null, int? start = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();

        var jsonResponse = await GetAsync("leads", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListLead);
    }

    /// <summary>
    /// Gets a specific lead by ID
    /// </summary>
    public async Task<PipedriveResponse<Lead>?> GetLeadByIdAsync(string id)
    {
        var jsonResponse = await GetAsync($"leads/{id}");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseLead);
    }

    /// <summary>
    /// Creates a new lead
    /// </summary>
    public async Task<PipedriveResponse<Lead>?> CreateLeadAsync(Lead lead)
    {
        var jsonData = JsonSerializer.Serialize(lead, ApiJsonContext.Default.Lead);
        var jsonResponse = await PostAsync("leads", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseLead);
    }

    /// <summary>
    /// Updates an existing lead
    /// </summary>
    public async Task<PipedriveResponse<Lead>?> UpdateLeadAsync(string id, Lead lead)
    {
        var jsonData = JsonSerializer.Serialize(lead, ApiJsonContext.Default.Lead);
        var jsonResponse = await PatchAsync($"leads/{id}", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseLead);
    }

    /// <summary>
    /// Deletes a lead
    /// </summary>
    public async Task<bool> DeleteLeadAsync(string id)
    {
        return await DeleteAsync($"leads/{id}");
    }

    /// <summary>
    /// Searches for leads (uses API v2)
    /// </summary>
    public async Task<PipedriveResponse<List<Lead>>?> SearchLeadsAsync(string term, int? limit = null)
    {
        // Update base URL temporarily for v2 API
        var originalBaseAddress = _httpClient.BaseAddress;
        _httpClient.BaseAddress = new Uri($"https://{(await _configService.GetActiveProfileAsync()).Domain}/api/v2/");

        try
        {
            var queryParams = new Dictionary<string, string> { ["term"] = term };
            if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();

            var jsonResponse = await GetAsync("leads/search", queryParams);
            return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListLead);
        }
        finally
        {
            _httpClient.BaseAddress = originalBaseAddress;
        }
    }

    #endregion

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
