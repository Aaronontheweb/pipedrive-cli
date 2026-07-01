using PipedriveCLI.Models;

namespace PipedriveCLI.Utilities;

/// <summary>
/// Helper for parsing and applying date filters to Pipedrive deals.
/// Used for --closing-after/--closing-before (expected_close_date) and
/// --won-after/--won-before (won_time) filtering on deals list.
/// </summary>
public static class DateFilterHelper
{
    /// <summary>
    /// Parses a date string in YYYY-MM-DD format.
    /// Returns true if the input is valid, false otherwise.
    /// Null or empty input returns true with a null result (no filter applied).
    /// </summary>
    public static bool TryParseDateFilter(string? input, out DateTime? result)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            result = null;
            return true;
        }

        if (DateTime.TryParseExact(input, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed))
        {
            result = parsed;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Parses an ISO 8601 date-time string (e.g., "2024-05-15T12:00:00Z").
    /// Returns true if the input is valid, false otherwise.
    /// Null or empty input returns true with a null result (no filter applied).
    /// </summary>
    public static bool TryParseDateTimeFilter(string? input, out DateTimeOffset? result)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            result = null;
            return true;
        }

        if (DateTimeOffset.TryParse(input, null, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
        {
            result = parsed;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Filters deals by expected_close_date (YYYY-MM-DD format from the API).
    /// Only includes deals that have a non-null expected_close_date within the specified range.
    /// </summary>
    /// <param name="deals">The deals to filter</param>
    /// <param name="after">Include deals with expected_close_date >= this date (inclusive). Null means no lower bound.</param>
    /// <param name="before">Include deals with expected_close_date <= this date (inclusive, entire day). Null means no upper bound.</param>
    public static IEnumerable<Deal> FilterByExpectedCloseDate(IEnumerable<Deal> deals, DateTime? after, DateTime? before)
    {
        var upperBound = before?.Date.AddDays(1).AddTicks(-1); // Include the entire "before" day

        return deals.Where(d =>
        {
            if (string.IsNullOrEmpty(d.ExpectedCloseDate))
                return false;

            if (!DateTime.TryParseExact(d.ExpectedCloseDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var closeDate))
                return false;

            if (after.HasValue && closeDate < after.Value.Date)
                return false;

            if (before.HasValue && closeDate > upperBound!)
                return false;

            return true;
        });
    }

    /// <summary>
    /// Filters deals by won_time (ISO 8601 format from the API).
    /// Only includes deals that have a non-null won_time within the specified range.
    /// </summary>
    /// <param name="deals">The deals to filter</param>
    /// <param name="after">Include deals with won_time >= this datetime (inclusive). Null means no lower bound.</param>
    /// <param name="before">Include deals with won_time <= this datetime (inclusive, entire day). Null means no upper bound.</param>
    public static IEnumerable<Deal> FilterByWonTime(IEnumerable<Deal> deals, DateTimeOffset? after, DateTimeOffset? before)
    {
        var upperBound = before?.Date.AddDays(1).AddTicks(-1); // Include the entire "before" day

        return deals.Where(d =>
        {
            if (string.IsNullOrEmpty(d.WonTime))
                return false;

            if (!DateTimeOffset.TryParse(d.WonTime, out var wonTime))
                return false;

            if (after.HasValue && wonTime < after.Value)
                return false;

            if (before.HasValue && wonTime > upperBound!)
                return false;

            return true;
        });
    }

    /// <summary>
    /// Derives server-side updated_since/updated_until bounds from client-side date filters.
    /// Adds a buffer (default 3 days) on each side to account for deals whose update_time
    /// may slightly precede their won_time or expected_close_date.
    ///
    /// Returns RFC 3339 formatted strings suitable for the Pipedrive API.
    /// </summary>
    /// <param name="after">Lower bound date filter (if any)</param>
    /// <param name="before">Upper bound date filter (if any)</param>
    /// <param name="bufferDays">Buffer days to add/subtract (default 3)</param>
    /// <returns>Tuple of (since, until) as RFC 3339 strings, or null if no corresponding bound</returns>
    public static (string? since, string? until) DeriveUpdateBounds(DateTime? after, DateTime? before, int bufferDays = 3)
    {
        string? since = null;
        string? until = null;

        // Derive updated_since from the lower bound (--closing-after / --won-after)
        if (after.HasValue)
        {
            since = after.Value.Date.AddDays(-bufferDays).ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        // Derive updated_until from the upper bound (--closing-before / --won-before)
        if (before.HasValue)
        {
            until = before.Value.Date.AddDays(bufferDays).ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        return (since, until);
    }
}