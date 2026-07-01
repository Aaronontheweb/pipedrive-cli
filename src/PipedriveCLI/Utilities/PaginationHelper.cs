using PipedriveCLI.Models;
using PipedriveCLI.Services;

namespace PipedriveCLI.Utilities;

/// <summary>
/// Helper for paginating through Pipedrive API results.
/// Used when date filters are active to fetch all matching deals across pages
/// with server-side pre-filtering via updated_since/updated_until.
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Fetches all deals across all pages, with optional server-side pre-filtering.
    /// Paginates through results until no more items are available.
    /// </summary>
    /// <param name="apiClient">The Pipedrive API client</param>
    /// <param name="status">Optional status filter (open, won, lost, etc.)</param>
    /// <param name="pipelineId">Optional pipeline ID filter</param>
    /// <param name="updatedSince">Optional server-side pre-filter: only deals updated since this RFC 3339 datetime</param>
    /// <param name="updatedUntil">Optional server-side pre-filter: only deals updated until this RFC 3339 datetime</param>
    public static async Task<List<Deal>> FetchAllDealsAsync(
        PipedriveApiClient apiClient,
        string? status = null,
        int? pipelineId = null,
        string? updatedSince = null,
        string? updatedUntil = null)
    {
        var allDeals = new List<Deal>();
        const int pageSize = 500; // Maximum page size for efficiency
        int start = 0;

        while (true)
        {
            var response = await apiClient.GetDealsAsync(
                limit: pageSize,
                start: start,
                status: status,
                pipelineId: pipelineId,
                updatedSince: updatedSince,
                updatedUntil: updatedUntil);

            if (response?.Success != true)
                break;

            var deals = response.Data ?? new List<Deal>();
            allDeals.AddRange(deals);

            // Check if there are more pages
            var pagination = response.AdditionalData?.Pagination;
            if (pagination == null || !pagination.MoreItemsInCollection)
                break;

            start = pagination.NextStart ?? (start + pageSize);
        }

        return allDeals;
    }

    /// <summary>
    /// Fetches all deals for a specific organization across all pages.
    /// </summary>
    /// <param name="apiClient">The Pipedrive API client</param>
    /// <param name="orgId">The organization ID</param>
    /// <param name="status">Optional status filter</param>
    public static async Task<List<Deal>> FetchAllOrganizationDealsAsync(
        PipedriveApiClient apiClient,
        int orgId,
        string? status = null)
    {
        var allDeals = new List<Deal>();
        const int pageSize = 500;
        int start = 0;

        while (true)
        {
            var response = await apiClient.GetOrganizationDealsAsync(
                orgId,
                limit: pageSize,
                start: start,
                status: status);

            if (response?.Success != true)
                break;

            var deals = response.Data ?? new List<Deal>();
            allDeals.AddRange(deals);

            var pagination = response.AdditionalData?.Pagination;
            if (pagination == null || !pagination.MoreItemsInCollection)
                break;

            start = pagination.NextStart ?? (start + pageSize);
        }

        return allDeals;
    }
}