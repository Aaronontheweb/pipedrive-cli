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

    #region Deals Operations

    /// <summary>
    /// Gets all deals
    /// </summary>
    public async Task<PipedriveResponse<List<Deal>>?> GetDealsAsync(int? limit = null, int? start = null, string? status = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();
        if (!string.IsNullOrWhiteSpace(status)) queryParams["status"] = status;

        var jsonResponse = await GetAsync("deals", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListDeal);
    }

    /// <summary>
    /// Gets a specific deal by ID
    /// </summary>
    public async Task<PipedriveResponse<Deal>?> GetDealByIdAsync(int id)
    {
        var jsonResponse = await GetAsync($"deals/{id}");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseDeal);
    }

    /// <summary>
    /// Creates a new deal
    /// </summary>
    public async Task<PipedriveResponse<Deal>?> CreateDealAsync(Deal deal)
    {
        var jsonData = JsonSerializer.Serialize(deal, ApiJsonContext.Default.Deal);
        var jsonResponse = await PostAsync("deals", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseDeal);
    }

    /// <summary>
    /// Updates an existing deal
    /// </summary>
    public async Task<PipedriveResponse<Deal>?> UpdateDealAsync(int id, Deal deal)
    {
        var jsonData = JsonSerializer.Serialize(deal, ApiJsonContext.Default.Deal);
        var jsonResponse = await PutAsync($"deals/{id}", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseDeal);
    }

    /// <summary>
    /// Deletes a deal
    /// </summary>
    public async Task<bool> DeleteDealAsync(int id)
    {
        return await DeleteAsync($"deals/{id}");
    }

    #endregion

    #region Activities Operations

    /// <summary>
    /// Gets all activities
    /// </summary>
    public async Task<PipedriveResponse<List<Activity>>?> GetActivitiesAsync(int? limit = null, int? start = null, bool? done = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();
        if (done.HasValue) queryParams["done"] = done.Value ? "1" : "0";

        var jsonResponse = await GetAsync("activities", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListActivity);
    }

    /// <summary>
    /// Gets a specific activity by ID
    /// </summary>
    public async Task<PipedriveResponse<Activity>?> GetActivityByIdAsync(int id)
    {
        var jsonResponse = await GetAsync($"activities/{id}");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseActivity);
    }

    /// <summary>
    /// Creates a new activity
    /// </summary>
    public async Task<PipedriveResponse<Activity>?> CreateActivityAsync(Activity activity)
    {
        var jsonData = JsonSerializer.Serialize(activity, ApiJsonContext.Default.Activity);
        var jsonResponse = await PostAsync("activities", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseActivity);
    }

    /// <summary>
    /// Updates an existing activity
    /// </summary>
    public async Task<PipedriveResponse<Activity>?> UpdateActivityAsync(int id, Activity activity)
    {
        var jsonData = JsonSerializer.Serialize(activity, ApiJsonContext.Default.Activity);
        var jsonResponse = await PutAsync($"activities/{id}", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseActivity);
    }

    /// <summary>
    /// Deletes an activity
    /// </summary>
    public async Task<bool> DeleteActivityAsync(int id)
    {
        return await DeleteAsync($"activities/{id}");
    }

    /// <summary>
    /// Marks an activity as done
    /// </summary>
    public async Task<PipedriveResponse<Activity>?> MarkActivityDoneAsync(int id)
    {
        var jsonData = JsonSerializer.Serialize(new { done = true }, ApiJsonContext.Default.Object);
        var jsonResponse = await PutAsync($"activities/{id}", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseActivity);
    }

    #endregion

    #region Persons Operations

    /// <summary>
    /// Gets all persons (contacts)
    /// </summary>
    public async Task<PipedriveResponse<List<Person>>?> GetPersonsAsync(int? limit = null, int? start = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();

        var jsonResponse = await GetAsync("persons", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListPerson);
    }

    /// <summary>
    /// Gets a specific person by ID
    /// </summary>
    public async Task<PipedriveResponse<Person>?> GetPersonByIdAsync(int id)
    {
        var jsonResponse = await GetAsync($"persons/{id}");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponsePerson);
    }

    /// <summary>
    /// Creates a new person
    /// </summary>
    public async Task<PipedriveResponse<Person>?> CreatePersonAsync(Person person)
    {
        var jsonData = JsonSerializer.Serialize(person, ApiJsonContext.Default.Person);
        var jsonResponse = await PostAsync("persons", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponsePerson);
    }

    /// <summary>
    /// Updates an existing person
    /// </summary>
    public async Task<PipedriveResponse<Person>?> UpdatePersonAsync(int id, Person person)
    {
        var jsonData = JsonSerializer.Serialize(person, ApiJsonContext.Default.Person);
        var jsonResponse = await PutAsync($"persons/{id}", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponsePerson);
    }

    /// <summary>
    /// Deletes a person
    /// </summary>
    public async Task<bool> DeletePersonAsync(int id)
    {
        return await DeleteAsync($"persons/{id}");
    }

    /// <summary>
    /// Searches for persons
    /// </summary>
    public async Task<PipedriveResponse<List<Person>>?> SearchPersonsAsync(string term, int? limit = null)
    {
        var queryParams = new Dictionary<string, string> { ["term"] = term };
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();

        var jsonResponse = await GetAsync("persons/search", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListPerson);
    }

    #endregion

    #region Organizations Operations

    /// <summary>
    /// Gets all organizations
    /// </summary>
    public async Task<PipedriveResponse<List<Organization>>?> GetOrganizationsAsync(int? limit = null, int? start = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();

        var jsonResponse = await GetAsync("organizations", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListOrganization);
    }

    /// <summary>
    /// Gets a specific organization by ID
    /// </summary>
    public async Task<PipedriveResponse<Organization>?> GetOrganizationByIdAsync(int id)
    {
        var jsonResponse = await GetAsync($"organizations/{id}");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseOrganization);
    }

    /// <summary>
    /// Creates a new organization
    /// </summary>
    public async Task<PipedriveResponse<Organization>?> CreateOrganizationAsync(Organization organization)
    {
        var jsonData = JsonSerializer.Serialize(organization, ApiJsonContext.Default.Organization);
        var jsonResponse = await PostAsync("organizations", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseOrganization);
    }

    /// <summary>
    /// Updates an existing organization
    /// </summary>
    public async Task<PipedriveResponse<Organization>?> UpdateOrganizationAsync(int id, Organization organization)
    {
        var jsonData = JsonSerializer.Serialize(organization, ApiJsonContext.Default.Organization);
        var jsonResponse = await PutAsync($"organizations/{id}", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseOrganization);
    }

    /// <summary>
    /// Deletes an organization
    /// </summary>
    public async Task<bool> DeleteOrganizationAsync(int id)
    {
        return await DeleteAsync($"organizations/{id}");
    }

    /// <summary>
    /// Searches for organizations
    /// </summary>
    public async Task<PipedriveResponse<List<Organization>>?> SearchOrganizationsAsync(string term, int? limit = null)
    {
        var queryParams = new Dictionary<string, string> { ["term"] = term };
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();

        var jsonResponse = await GetAsync("organizations/search", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListOrganization);
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

        // Build query string with API key
        var query = new List<string> { $"api_token={Uri.EscapeDataString(profile.ApiKey!)}" };

        if (queryParams != null)
        {
            foreach (var (key, value) in queryParams)
            {
                query.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
            }
        }

        var queryString = string.Join("&", query);
        var trimmedEndpoint = endpoint.TrimStart('/');

        return $"{trimmedEndpoint}?{queryString}";
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
