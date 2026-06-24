using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PipedriveCLI.Models;

namespace PipedriveCLI.Services;

/// <summary>
/// Exception thrown when the Pipedrive API returns an error response
/// </summary>
public class PipedriveApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ApiError { get; }
    public string? ApiErrorInfo { get; }

    public PipedriveApiException(HttpStatusCode statusCode, string? apiError, string? apiErrorInfo)
        : base(FormatMessage(statusCode, apiError, apiErrorInfo))
    {
        StatusCode = statusCode;
        ApiError = apiError;
        ApiErrorInfo = apiErrorInfo;
    }

    private static string FormatMessage(HttpStatusCode statusCode, string? apiError, string? apiErrorInfo)
    {
        if (!string.IsNullOrWhiteSpace(apiError))
        {
            return !string.IsNullOrWhiteSpace(apiErrorInfo)
                ? $"{apiError} - {apiErrorInfo}"
                : apiError;
        }
        return $"HTTP {(int)statusCode} ({statusCode})";
    }
}

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
                "Pipedrive CLI is not configured. Run 'pipedrive config set --api-key YOUR_API_KEY --domain company.pipedrive.com' to configure.");
        }

        // Set base address using the domain from config
        _httpClient.BaseAddress = new Uri($"https://{profile.Domain}/api/v1/");

        // Store API key for use in requests
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        _isConfigured = true;
    }

    /// <summary>
    /// Ensures the response is successful, or throws a PipedriveApiException with the actual error message
    /// </summary>
    private static async Task EnsureSuccessOrThrowApiErrorAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        // Try to read the response body to get the actual error message
        string? apiError = null;
        string? apiErrorInfo = null;

        try
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                // Try to parse as a Pipedrive error response
                var errorResponse = JsonSerializer.Deserialize(responseBody, ApiJsonContext.Default.PipedriveErrorResponse);
                if (errorResponse != null)
                {
                    apiError = errorResponse.Error;
                    apiErrorInfo = errorResponse.ErrorInfo;
                }
            }
        }
        catch
        {
            // Ignore JSON parsing errors - we'll fall back to generic message
        }

        throw new PipedriveApiException(response.StatusCode, apiError, apiErrorInfo);
    }

    /// <summary>
    /// Makes a GET request to the Pipedrive API and returns raw JSON string
    /// </summary>
    public async Task<string> GetAsync(string endpoint, Dictionary<string, string>? queryParams = null)
    {
        EnsureConfigured();

        var url = await BuildUrlWithApiKeyAsync(endpoint, queryParams);
        var response = await _httpClient.GetAsync(url);

        await EnsureSuccessOrThrowApiErrorAsync(response);

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

        await EnsureSuccessOrThrowApiErrorAsync(response);

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

        await EnsureSuccessOrThrowApiErrorAsync(response);

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

        await EnsureSuccessOrThrowApiErrorAsync(response);

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
    /// <param name="limit">Number of leads to return</param>
    /// <param name="start">Pagination start</param>
    /// <param name="archivedStatus">Filter by archived status: "not_archived" (active only), "archived", or "all"</param>
    public async Task<PipedriveResponse<List<Lead>>?> GetLeadsAsync(int? limit = null, int? start = null, string? archivedStatus = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();
        if (!string.IsNullOrWhiteSpace(archivedStatus)) queryParams["archived_status"] = archivedStatus;

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
    /// Converts a lead to a deal
    /// </summary>
    /// <param name="id">Lead ID</param>
    /// <param name="request">Conversion parameters (stage_id, pipeline_id)</param>
    public async Task<PipedriveResponse<LeadConvertResult>?> ConvertLeadToDealAsync(string id, LeadConvertRequest request)
    {
        var jsonData = JsonSerializer.Serialize(request, ApiJsonContext.Default.LeadConvertRequest);
        var jsonResponse = await PostAsync($"leads/{id}/convert/deal", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseLeadConvertResult);
    }

    /// <summary>
    /// Searches for leads (uses API v2)
    /// </summary>
    public async Task<PipedriveResponse<List<Lead>>?> SearchLeadsAsync(string term, int? limit = null)
    {
        EnsureConfigured();

        var profile = await _configService.GetActiveProfileAsync();

        // Build query parameters with API key
        var queryParams = new Dictionary<string, string>
        {
            ["term"] = term,
            ["api_token"] = profile.ApiKey!
        };
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();

        // Build full v2 URL with query string
        var queryString = string.Join("&", queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        var absoluteUrl = $"https://{profile.Domain}/api/v2/leads/search?{queryString}";

        // Use absolute URI to avoid modifying BaseAddress
        var response = await _httpClient.GetAsync(new Uri(absoluteUrl, UriKind.Absolute));
        await EnsureSuccessOrThrowApiErrorAsync(response);

        var jsonResponse = await response.Content.ReadAsStringAsync();

        // Deserialize v2 search response format
        var searchResponse = JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseLeadSearchData);

        // Transform v2 search response to standard list format
        if (searchResponse?.Success == true && searchResponse.Data?.Items != null)
        {
            var leads = searchResponse.Data.Items
                .Where(item => item.Item != null)
                .Select(item => item.Item!.ToLead())
                .ToList();

            return new PipedriveResponse<List<Lead>>
            {
                Success = true,
                Data = leads,
                AdditionalData = searchResponse.AdditionalData
            };
        }

        return new PipedriveResponse<List<Lead>>
        {
            Success = searchResponse?.Success ?? false,
            Error = searchResponse?.Error,
            ErrorInfo = searchResponse?.ErrorInfo
        };
    }

    #endregion

    #region Deals Operations

    /// <summary>
    /// Gets all deals
    /// </summary>
    public async Task<PipedriveResponse<List<Deal>>?> GetDealsAsync(int? limit = null, int? start = null, string? status = null, int? pipelineId = null, string? updatedSince = null, string? updatedUntil = null, string? cursor = null)
    {
        var useV2 = !string.IsNullOrWhiteSpace(updatedSince)
            || !string.IsNullOrWhiteSpace(updatedUntil)
            || !string.IsNullOrWhiteSpace(cursor);

        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        AddStartQueryParam(queryParams, start, useV2);
        AddDealStatusQueryParam(queryParams, status, useV2, mapAllToAllNotDeleted: false);
        if (pipelineId.HasValue) queryParams["pipeline_id"] = pipelineId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(updatedSince)) queryParams["updated_since"] = updatedSince;
        if (!string.IsNullOrWhiteSpace(updatedUntil)) queryParams["updated_until"] = updatedUntil;
        if (!string.IsNullOrWhiteSpace(cursor)) queryParams["cursor"] = cursor;

        var jsonResponse = await GetAsync(useV2 ? "v2/deals" : "deals", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListDeal);
    }

    /// <summary>
    /// Gets deals associated with a specific organization
    /// </summary>
    public async Task<PipedriveResponse<List<Deal>>?> GetOrganizationDealsAsync(int orgId, int? limit = null, int? start = null, string? status = null, string? updatedSince = null, string? updatedUntil = null, string? cursor = null)
    {
        var useV2 = !string.IsNullOrWhiteSpace(updatedSince)
            || !string.IsNullOrWhiteSpace(updatedUntil)
            || !string.IsNullOrWhiteSpace(cursor);

        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        AddStartQueryParam(queryParams, start, useV2);
        if (useV2) queryParams["org_id"] = orgId.ToString();
        if (!string.IsNullOrWhiteSpace(updatedSince)) queryParams["updated_since"] = updatedSince;
        if (!string.IsNullOrWhiteSpace(updatedUntil)) queryParams["updated_until"] = updatedUntil;
        if (!string.IsNullOrWhiteSpace(cursor)) queryParams["cursor"] = cursor;

        AddDealStatusQueryParam(queryParams, status, useV2, mapAllToAllNotDeleted: true);

        var jsonResponse = await GetAsync(useV2 ? "v2/deals" : $"organizations/{orgId}/deals", queryParams);
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
    /// Updates an existing deal with support for clearing fields by setting them to null.
    /// Use this overload when you need to clear date fields or other nullable fields.
    /// </summary>
    /// <param name="id">The deal ID</param>
    /// <param name="deal">The deal object with fields to update</param>
    /// <param name="clearFields">List of field names to explicitly set to null (e.g., "expected_close_date")</param>
    public async Task<PipedriveResponse<Deal>?> UpdateDealAsync(int id, Deal deal, IEnumerable<string> clearFields)
    {
        // Build JSON manually to include explicit nulls for cleared fields
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();

        // First, serialize the non-null deal properties
        var dealJson = JsonSerializer.Serialize(deal, ApiJsonContext.Default.Deal);
        using var dealDoc = JsonDocument.Parse(dealJson);
        foreach (var property in dealDoc.RootElement.EnumerateObject())
        {
            property.WriteTo(writer);
        }

        // Then, add explicit nulls for fields to clear (if not already in deal)
        foreach (var field in clearFields)
        {
            if (!dealDoc.RootElement.TryGetProperty(field, out _))
            {
                writer.WriteNull(field);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        var jsonData = System.Text.Encoding.UTF8.GetString(stream.ToArray());
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

    /// <summary>
    /// Merges two deals
    /// </summary>
    /// <param name="id">ID of the deal to be merged (will be deleted)</param>
    /// <param name="mergeWithId">ID of the deal to merge with (takes priority in conflicts)</param>
    public async Task<PipedriveResponse<Deal>?> MergeDealAsync(int id, int mergeWithId)
    {
        var mergeRequest = new MergeRequest { MergeWithId = mergeWithId };
        var jsonData = JsonSerializer.Serialize(mergeRequest, ApiJsonContext.Default.MergeRequest);
        var jsonResponse = await PutAsync($"deals/{id}/merge", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseDeal);
    }

    /// <summary>
    /// Gets all participants of a deal
    /// </summary>
    public async Task<PipedriveResponse<List<DealParticipant>>?> GetDealParticipantsAsync(int dealId, int? limit = null, int? start = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();

        var jsonResponse = await GetAsync($"deals/{dealId}/participants", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListDealParticipant);
    }

    /// <summary>
    /// Adds a participant to a deal
    /// </summary>
    public async Task<PipedriveResponse<DealParticipant>?> AddDealParticipantAsync(int dealId, int personId)
    {
        var request = new AddDealParticipantRequest { PersonId = personId };
        var jsonData = JsonSerializer.Serialize(request, ApiJsonContext.Default.AddDealParticipantRequest);
        var jsonResponse = await PostAsync($"deals/{dealId}/participants", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseDealParticipant);
    }

    /// <summary>
    /// Removes a participant from a deal
    /// </summary>
    public async Task<bool> RemoveDealParticipantAsync(int dealId, int participantId)
    {
        return await DeleteAsync($"deals/{dealId}/participants/{participantId}");
    }

    /// <summary>
    /// Gets all products attached to a deal
    /// </summary>
    public async Task<PipedriveResponse<List<DealProduct>>?> GetDealProductsAsync(int dealId, int? limit = null, int? start = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();

        var jsonResponse = await GetAsync($"deals/{dealId}/products", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListDealProduct);
    }

    /// <summary>
    /// Adds a product to a deal
    /// </summary>
    public async Task<PipedriveResponse<DealProduct>?> AddDealProductAsync(int dealId, AddDealProductRequest request)
    {
        var jsonData = JsonSerializer.Serialize(request, ApiJsonContext.Default.AddDealProductRequest);
        var jsonResponse = await PostAsync($"deals/{dealId}/products", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseDealProduct);
    }

    /// <summary>
    /// Removes a product from a deal
    /// </summary>
    /// <param name="dealId">The deal ID</param>
    /// <param name="dealProductId">The deal-product attachment ID (not the product ID)</param>
    public async Task<bool> RemoveDealProductAsync(int dealId, int dealProductId)
    {
        return await DeleteAsync($"deals/{dealId}/products/{dealProductId}");
    }

    #endregion

    #region Activities Operations

    /// <summary>
    /// Gets all activities
    /// </summary>
    public async Task<PipedriveResponse<List<Activity>>?> GetActivitiesAsync(int? limit = null, int? start = null, bool? done = null, string? updatedSince = null, string? updatedUntil = null, string? sortBy = null, string? sortDirection = null, string? cursor = null)
    {
        var useV2 = !string.IsNullOrWhiteSpace(updatedSince)
            || !string.IsNullOrWhiteSpace(updatedUntil)
            || !string.IsNullOrWhiteSpace(sortBy)
            || !string.IsNullOrWhiteSpace(sortDirection)
            || !string.IsNullOrWhiteSpace(cursor);

        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        AddStartQueryParam(queryParams, start, useV2);
        if (done.HasValue) queryParams["done"] = useV2 ? done.Value.ToString().ToLowerInvariant() : done.Value ? "1" : "0";
        if (!string.IsNullOrWhiteSpace(updatedSince)) queryParams["updated_since"] = updatedSince;
        if (!string.IsNullOrWhiteSpace(updatedUntil)) queryParams["updated_until"] = updatedUntil;
        if (!string.IsNullOrWhiteSpace(sortBy)) queryParams["sort_by"] = sortBy;
        if (!string.IsNullOrWhiteSpace(sortDirection)) queryParams["sort_direction"] = sortDirection;
        if (!string.IsNullOrWhiteSpace(cursor)) queryParams["cursor"] = cursor;

        var jsonResponse = await GetAsync(useV2 ? "v2/activities" : "activities", queryParams);
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
        var activity = new Activity { Done = true };
        var jsonData = JsonSerializer.Serialize(activity, ApiJsonContext.Default.Activity);
        var jsonResponse = await PutAsync($"activities/{id}", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseActivity);
    }

    /// <summary>
    /// Gets activities associated with a specific deal
    /// </summary>
    public async Task<PipedriveResponse<List<Activity>>?> GetDealActivitiesAsync(int dealId, int? limit = null, int? start = null, bool? done = null, string? updatedSince = null, string? updatedUntil = null, string? sortBy = null, string? sortDirection = null, string? cursor = null)
    {
        var useV2 = !string.IsNullOrWhiteSpace(updatedSince)
            || !string.IsNullOrWhiteSpace(updatedUntil)
            || !string.IsNullOrWhiteSpace(sortBy)
            || !string.IsNullOrWhiteSpace(sortDirection)
            || !string.IsNullOrWhiteSpace(cursor);

        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        AddStartQueryParam(queryParams, start, useV2);
        if (done.HasValue) queryParams["done"] = useV2 ? done.Value.ToString().ToLowerInvariant() : done.Value ? "1" : "0";
        if (useV2) queryParams["deal_id"] = dealId.ToString();
        if (!string.IsNullOrWhiteSpace(updatedSince)) queryParams["updated_since"] = updatedSince;
        if (!string.IsNullOrWhiteSpace(updatedUntil)) queryParams["updated_until"] = updatedUntil;
        if (!string.IsNullOrWhiteSpace(sortBy)) queryParams["sort_by"] = sortBy;
        if (!string.IsNullOrWhiteSpace(sortDirection)) queryParams["sort_direction"] = sortDirection;
        if (!string.IsNullOrWhiteSpace(cursor)) queryParams["cursor"] = cursor;

        var jsonResponse = await GetAsync(useV2 ? "v2/activities" : $"deals/{dealId}/activities", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListActivity);
    }

    /// <summary>
    /// Gets activities associated with a specific person
    /// </summary>
    public async Task<PipedriveResponse<List<Activity>>?> GetPersonActivitiesAsync(int personId, int? limit = null, int? start = null, bool? done = null, string? updatedSince = null, string? updatedUntil = null, string? sortBy = null, string? sortDirection = null, string? cursor = null)
    {
        var useV2 = !string.IsNullOrWhiteSpace(updatedSince)
            || !string.IsNullOrWhiteSpace(updatedUntil)
            || !string.IsNullOrWhiteSpace(sortBy)
            || !string.IsNullOrWhiteSpace(sortDirection)
            || !string.IsNullOrWhiteSpace(cursor);

        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        AddStartQueryParam(queryParams, start, useV2);
        if (done.HasValue) queryParams["done"] = useV2 ? done.Value.ToString().ToLowerInvariant() : done.Value ? "1" : "0";
        if (useV2) queryParams["person_id"] = personId.ToString();
        if (!string.IsNullOrWhiteSpace(updatedSince)) queryParams["updated_since"] = updatedSince;
        if (!string.IsNullOrWhiteSpace(updatedUntil)) queryParams["updated_until"] = updatedUntil;
        if (!string.IsNullOrWhiteSpace(sortBy)) queryParams["sort_by"] = sortBy;
        if (!string.IsNullOrWhiteSpace(sortDirection)) queryParams["sort_direction"] = sortDirection;
        if (!string.IsNullOrWhiteSpace(cursor)) queryParams["cursor"] = cursor;

        var jsonResponse = await GetAsync(useV2 ? "v2/activities" : $"persons/{personId}/activities", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListActivity);
    }

    /// <summary>
    /// Gets activities associated with a specific organization
    /// </summary>
    public async Task<PipedriveResponse<List<Activity>>?> GetOrganizationActivitiesAsync(int orgId, int? limit = null, int? start = null, bool? done = null, string? updatedSince = null, string? updatedUntil = null, string? sortBy = null, string? sortDirection = null, string? cursor = null)
    {
        var useV2 = !string.IsNullOrWhiteSpace(updatedSince)
            || !string.IsNullOrWhiteSpace(updatedUntil)
            || !string.IsNullOrWhiteSpace(sortBy)
            || !string.IsNullOrWhiteSpace(sortDirection)
            || !string.IsNullOrWhiteSpace(cursor);

        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        AddStartQueryParam(queryParams, start, useV2);
        if (done.HasValue) queryParams["done"] = useV2 ? done.Value.ToString().ToLowerInvariant() : done.Value ? "1" : "0";
        if (useV2) queryParams["org_id"] = orgId.ToString();
        if (!string.IsNullOrWhiteSpace(updatedSince)) queryParams["updated_since"] = updatedSince;
        if (!string.IsNullOrWhiteSpace(updatedUntil)) queryParams["updated_until"] = updatedUntil;
        if (!string.IsNullOrWhiteSpace(sortBy)) queryParams["sort_by"] = sortBy;
        if (!string.IsNullOrWhiteSpace(sortDirection)) queryParams["sort_direction"] = sortDirection;
        if (!string.IsNullOrWhiteSpace(cursor)) queryParams["cursor"] = cursor;

        var jsonResponse = await GetAsync(useV2 ? "v2/activities" : $"organizations/{orgId}/activities", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListActivity);
    }

    #endregion

    #region Notes Operations

    /// <summary>
    /// Retrieves all notes with pagination support
    /// </summary>
    public async Task<PipedriveResponse<List<Note>>?> GetNotesAsync(int? limit = null, int? start = null, int? dealId = null, int? personId = null, int? orgId = null, string? leadId = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();
        if (dealId.HasValue) queryParams["deal_id"] = dealId.Value.ToString();
        if (personId.HasValue) queryParams["person_id"] = personId.Value.ToString();
        if (orgId.HasValue) queryParams["org_id"] = orgId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(leadId)) queryParams["lead_id"] = leadId;

        var jsonResponse = await GetAsync("notes", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListNote);
    }

    /// <summary>
    /// Retrieves a specific note by ID
    /// </summary>
    public async Task<PipedriveResponse<Note>?> GetNoteByIdAsync(int id)
    {
        var jsonResponse = await GetAsync($"notes/{id}");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseNote);
    }

    /// <summary>
    /// Creates a new note
    /// </summary>
    public async Task<PipedriveResponse<Note>?> CreateNoteAsync(Note note)
    {
        var jsonData = JsonSerializer.Serialize(note, ApiJsonContext.Default.Note);
        var jsonResponse = await PostAsync("notes", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseNote);
    }

    /// <summary>
    /// Updates an existing note
    /// </summary>
    public async Task<PipedriveResponse<Note>?> UpdateNoteAsync(int id, Note note)
    {
        var jsonData = JsonSerializer.Serialize(note, ApiJsonContext.Default.Note);
        var jsonResponse = await PutAsync($"notes/{id}", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseNote);
    }

    /// <summary>
    /// Deletes a note
    /// </summary>
    public async Task<bool> DeleteNoteAsync(int id)
    {
        return await DeleteAsync($"notes/{id}");
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

        // Deserialize search response format (data.items containing result_score and item)
        var searchResponse = JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponsePersonSearchData);

        // Transform search response to standard list format
        if (searchResponse?.Success == true && searchResponse.Data?.Items != null)
        {
            var persons = searchResponse.Data.Items
                .Where(item => item.Item != null)
                .Select(item => item.Item!.ToPerson())
                .ToList();

            return new PipedriveResponse<List<Person>>
            {
                Success = true,
                Data = persons,
                AdditionalData = searchResponse.AdditionalData
            };
        }

        return new PipedriveResponse<List<Person>>
        {
            Success = searchResponse?.Success ?? false,
            Error = searchResponse?.Error,
            ErrorInfo = searchResponse?.ErrorInfo
        };
    }

    /// <summary>
    /// Merges two persons
    /// </summary>
    /// <param name="id">ID of the person to be merged (will be deleted)</param>
    /// <param name="mergeWithId">ID of the person to merge with (takes priority in conflicts)</param>
    public async Task<PipedriveResponse<Person>?> MergePersonAsync(int id, int mergeWithId)
    {
        var mergeRequest = new MergeRequest { MergeWithId = mergeWithId };
        var jsonData = JsonSerializer.Serialize(mergeRequest, ApiJsonContext.Default.MergeRequest);
        var jsonResponse = await PutAsync($"persons/{id}/merge", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponsePerson);
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

        // Deserialize search response format (data.items containing result_score and item)
        var searchResponse = JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseOrganizationSearchData);

        // Transform search response to standard list format
        if (searchResponse?.Success == true && searchResponse.Data?.Items != null)
        {
            var organizations = searchResponse.Data.Items
                .Where(item => item.Item != null)
                .Select(item => item.Item!.ToOrganization())
                .ToList();

            return new PipedriveResponse<List<Organization>>
            {
                Success = true,
                Data = organizations,
                AdditionalData = searchResponse.AdditionalData
            };
        }

        return new PipedriveResponse<List<Organization>>
        {
            Success = searchResponse?.Success ?? false,
            Error = searchResponse?.Error,
            ErrorInfo = searchResponse?.ErrorInfo
        };
    }

    /// <summary>
    /// Merges two organizations
    /// </summary>
    /// <param name="id">ID of the organization to be merged (will be deleted)</param>
    /// <param name="mergeWithId">ID of the organization to merge with (takes priority in conflicts)</param>
    public async Task<PipedriveResponse<Organization>?> MergeOrganizationAsync(int id, int mergeWithId)
    {
        var mergeRequest = new MergeRequest { MergeWithId = mergeWithId };
        var jsonData = JsonSerializer.Serialize(mergeRequest, ApiJsonContext.Default.MergeRequest);
        var jsonResponse = await PutAsync($"organizations/{id}/merge", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseOrganization);
    }

    #endregion

    #region Custom Fields

    /// <summary>
    /// Gets all deal field definitions
    /// </summary>
    public async Task<PipedriveResponse<List<DealField>>?> GetDealFieldsAsync()
    {
        var jsonResponse = await GetAsync("dealFields");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListDealField);
    }

    /// <summary>
    /// Gets all person field definitions
    /// </summary>
    public async Task<PipedriveResponse<List<PersonField>>?> GetPersonFieldsAsync()
    {
        var jsonResponse = await GetAsync("personFields");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListPersonField);
    }

    /// <summary>
    /// Gets all organization field definitions
    /// </summary>
    public async Task<PipedriveResponse<List<OrganizationField>>?> GetOrganizationFieldsAsync()
    {
        var jsonResponse = await GetAsync("organizationFields");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListOrganizationField);
    }

    #endregion

    #region Pipelines Operations

    /// <summary>
    /// Gets all pipelines
    /// </summary>
    public async Task<PipedriveResponse<List<Pipeline>>?> GetPipelinesAsync()
    {
        var jsonResponse = await GetAsync("pipelines");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListPipeline);
    }

    /// <summary>
    /// Gets a specific pipeline by ID
    /// </summary>
    public async Task<PipedriveResponse<Pipeline>?> GetPipelineByIdAsync(int id)
    {
        var jsonResponse = await GetAsync($"pipelines/{id}");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponsePipeline);
    }

    /// <summary>
    /// Gets all stages for a specific pipeline
    /// </summary>
    public async Task<PipedriveResponse<List<Stage>>?> GetStagesAsync(int? pipelineId = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (pipelineId.HasValue) queryParams["pipeline_id"] = pipelineId.Value.ToString();

        var jsonResponse = await GetAsync("stages", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListStage);
    }

    /// <summary>
    /// Gets a specific stage by ID
    /// </summary>
    public async Task<PipedriveResponse<Stage>?> GetStageByIdAsync(int id)
    {
        var jsonResponse = await GetAsync($"stages/{id}");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseStage);
    }

    #endregion

    #region Email Templates Operations

    /// <summary>
    /// Gets all email templates
    /// </summary>
    public async Task<PipedriveResponse<List<EmailTemplate>>?> GetEmailTemplatesAsync()
    {
        var jsonResponse = await GetAsync("mailbox/mailTemplates");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListEmailTemplate);
    }

    /// <summary>
    /// Gets a specific email template by ID
    /// </summary>
    public async Task<PipedriveResponse<EmailTemplate>?> GetEmailTemplateByIdAsync(int id)
    {
        var jsonResponse = await GetAsync($"mailbox/mailTemplates/{id}");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseEmailTemplate);
    }

    #endregion

    #region Mail Messages Operations

    /// <summary>
    /// Gets mail messages for a deal.
    /// Note: The deals/{id}/mailMessages endpoint returns a nested structure with wrapper objects.
    /// </summary>
    public async Task<PipedriveResponse<List<MailMessageWrapper>>?> GetMailMessagesForDealAsync(int dealId, int? limit = null, int? start = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();

        var jsonResponse = await GetAsync($"deals/{dealId}/mailMessages", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListMailMessageWrapper);
    }

    /// <summary>
    /// Gets mail messages for a person.
    /// Note: The persons/{id}/mailMessages endpoint returns a nested structure with wrapper objects.
    /// </summary>
    public async Task<PipedriveResponse<List<MailMessageWrapper>>?> GetMailMessagesForPersonAsync(int personId, int? limit = null, int? start = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();

        var jsonResponse = await GetAsync($"persons/{personId}/mailMessages", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListMailMessageWrapper);
    }

    /// <summary>
    /// Gets a specific mail message by ID
    /// </summary>
    /// <param name="id">The mail message ID</param>
    /// <param name="includeBody">Whether to include the full email body</param>
    public async Task<PipedriveResponse<MailMessage>?> GetMailMessageByIdAsync(int id, bool includeBody = false)
    {
        var queryParams = new Dictionary<string, string>();
        if (includeBody) queryParams["include_body"] = "1";

        var jsonResponse = await GetAsync($"mailbox/mailMessages/{id}", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseMailMessage);
    }

    #endregion

    #region Mail Threads Operations

    /// <summary>
    /// Gets mail threads with optional filters
    /// </summary>
    /// <param name="folder">Filter by folder: inbox, drafts, sent, archive</param>
    /// <param name="limit">Number of threads to return</param>
    /// <param name="start">Pagination start</param>
    /// <param name="personId">Filter by person ID</param>
    /// <param name="dealId">Filter by deal ID</param>
    public async Task<PipedriveResponse<List<MailThread>>?> GetMailThreadsAsync(string? folder = null, int? limit = null, int? start = null, int? personId = null, int? dealId = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(folder)) queryParams["folder"] = folder;
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();
        if (personId.HasValue) queryParams["person_id"] = personId.Value.ToString();
        if (dealId.HasValue) queryParams["deal_id"] = dealId.Value.ToString();

        var jsonResponse = await GetAsync("mailbox/mailThreads", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListMailThread);
    }

    /// <summary>
    /// Gets a specific mail thread by ID
    /// </summary>
    public async Task<PipedriveResponse<MailThread>?> GetMailThreadByIdAsync(int id)
    {
        var jsonResponse = await GetAsync($"mailbox/mailThreads/{id}");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseMailThread);
    }

    /// <summary>
    /// Gets all mail messages in a thread
    /// </summary>
    public async Task<PipedriveResponse<List<MailMessage>>?> GetMailThreadMessagesAsync(int threadId)
    {
        var jsonResponse = await GetAsync($"mailbox/mailThreads/{threadId}/mailMessages");
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListMailMessage);
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

        if (trimmedEndpoint.StartsWith("v2/", StringComparison.OrdinalIgnoreCase))
        {
            return $"https://{profile.Domain}/api/{trimmedEndpoint}?{queryString}";
        }

        if (trimmedEndpoint.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
        {
            return $"https://{profile.Domain}/{trimmedEndpoint}?{queryString}";
        }

        return $"{trimmedEndpoint}?{queryString}";
    }

    private static void AddStartQueryParam(Dictionary<string, string> queryParams, int? start, bool useV2)
    {
        if (!start.HasValue)
            return;

        if (useV2)
        {
            if (start.Value != 0)
            {
                throw new InvalidOperationException("--start is not supported with filters that use Pipedrive API v2 cursor pagination. Use --limit without --start.");
            }

            return;
        }

        queryParams["start"] = start.Value.ToString();
    }

    private static void AddDealStatusQueryParam(Dictionary<string, string> queryParams, string? status, bool useV2, bool mapAllToAllNotDeleted)
    {
        if (string.IsNullOrWhiteSpace(status))
            return;

        if (useV2 && (status.Equals("all", StringComparison.OrdinalIgnoreCase) || status.Equals("all_not_deleted", StringComparison.OrdinalIgnoreCase)))
            return;

        queryParams["status"] = !useV2 && mapAllToAllNotDeleted && status.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? "all_not_deleted"
            : status;
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
