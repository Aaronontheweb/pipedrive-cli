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
                "Pipedrive CLI is not configured. Run 'pipedrive config set --api-key YOUR_API_KEY --domain company.pipedrive.com' to configure.");
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
        response.EnsureSuccessStatusCode();

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
        var activity = new Activity { Done = true };
        var jsonData = JsonSerializer.Serialize(activity, ApiJsonContext.Default.Activity);
        var jsonResponse = await PutAsync($"activities/{id}", jsonData);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseActivity);
    }

    #endregion

    #region Notes Operations

    /// <summary>
    /// Retrieves all notes with pagination support
    /// </summary>
    public async Task<PipedriveResponse<List<Note>>?> GetNotesAsync(int? limit = null, int? start = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();

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
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListPerson);
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
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListOrganization);
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
    /// Gets mail messages for a deal
    /// </summary>
    public async Task<PipedriveResponse<List<MailMessage>>?> GetMailMessagesForDealAsync(int dealId, int? limit = null, int? start = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();

        var jsonResponse = await GetAsync($"deals/{dealId}/mailMessages", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListMailMessage);
    }

    /// <summary>
    /// Gets mail messages for a person
    /// </summary>
    public async Task<PipedriveResponse<List<MailMessage>>?> GetMailMessagesForPersonAsync(int personId, int? limit = null, int? start = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();

        var jsonResponse = await GetAsync($"persons/{personId}/mailMessages", queryParams);
        return JsonSerializer.Deserialize(jsonResponse, ApiJsonContext.Default.PipedriveResponseListMailMessage);
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
    /// Gets mail threads with optional folder filter
    /// </summary>
    /// <param name="folder">Filter by folder: inbox, drafts, sent, archive</param>
    /// <param name="limit">Number of threads to return</param>
    /// <param name="start">Pagination start</param>
    public async Task<PipedriveResponse<List<MailThread>>?> GetMailThreadsAsync(string? folder = null, int? limit = null, int? start = null)
    {
        var queryParams = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(folder)) queryParams["folder"] = folder;
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (start.HasValue) queryParams["start"] = start.Value.ToString();

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
