using PipedriveCLI.Models;
using PipedriveCLI.Utilities;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Unit tests for date filter helpers used in deals list filtering
/// (issues #146 and #164).
/// </summary>
public class DateFilterTests
{
    #region TryParseDateFilter

    [Fact]
    public void TryParseDateFilter_ValidDate_ReturnsTrue()
    {
        // Act
        var result = DateFilterHelper.TryParseDateFilter("2024-05-15", out var parsed);

        // Assert
        Assert.True(result);
        Assert.NotNull(parsed);
        Assert.Equal(2024, parsed.Value.Year);
        Assert.Equal(5, parsed.Value.Month);
        Assert.Equal(15, parsed.Value.Day);
    }

    [Fact]
    public void TryParseDateFilter_InvalidFormat_ReturnsFalse()
    {
        // Act
        var result = DateFilterHelper.TryParseDateFilter("05-15-2024", out var parsed);

        // Assert
        Assert.False(result);
        Assert.Null(parsed);
    }

    [Fact]
    public void TryParseDateFilter_NullInput_ReturnsTrueWithNull()
    {
        // Act
        var result = DateFilterHelper.TryParseDateFilter(null, out var parsed);

        // Assert
        Assert.True(result);
        Assert.Null(parsed);
    }

    [Fact]
    public void TryParseDateFilter_EmptyInput_ReturnsTrueWithNull()
    {
        // Act
        var result = DateFilterHelper.TryParseDateFilter("", out var parsed);

        // Assert
        Assert.True(result);
        Assert.Null(parsed);
    }

    [Fact]
    public void TryParseDateFilter_WhitespaceInput_ReturnsTrueWithNull()
    {
        // Act
        var result = DateFilterHelper.TryParseDateFilter("  ", out var parsed);

        // Assert
        Assert.True(result);
        Assert.Null(parsed);
    }

    #endregion

    #region DeriveUpdateBounds

    [Fact]
    public void DeriveUpdateBounds_OnlyAfter_ProducesSince()
    {
        // Arrange
        var after = new DateTime(2026, 4, 1);

        // Act
        var (since, until) = DateFilterHelper.DeriveUpdateBounds(after, null);

        // Assert
        Assert.NotNull(since);
        Assert.Equal("2026-03-29T00:00:00Z", since);
        Assert.Null(until);
    }

    [Fact]
    public void DeriveUpdateBounds_OnlyBefore_ProducesUntil()
    {
        // Arrange
        var before = new DateTime(2026, 6, 30);

        // Act
        var (since, until) = DateFilterHelper.DeriveUpdateBounds(null, before);

        // Assert — only before means only until, since is null
        Assert.NotNull(until);
        Assert.Equal("2026-07-03T00:00:00Z", until);
        Assert.Null(since);
    }

    [Fact]
    public void DeriveUpdateBounds_BothBounds_ProducesSymmetricBuffer()
    {
        // Arrange
        var after = new DateTime(2026, 4, 1);
        var before = new DateTime(2026, 6, 30);

        // Act
        var (since, until) = DateFilterHelper.DeriveUpdateBounds(after, before);

        // Assert
        Assert.NotNull(since);
        Assert.Equal("2026-03-29T00:00:00Z", since); // 3 days before April 1
        Assert.NotNull(until);
        Assert.Equal("2026-07-03T00:00:00Z", until); // 3 days after June 30
    }

    [Fact]
    public void DeriveUpdateBounds_NullInputs_ProducesNullBounds()
    {
        // Act
        var (since, until) = DateFilterHelper.DeriveUpdateBounds(null, null);

        // Assert
        Assert.Null(since);
        Assert.Null(until);
    }

    #endregion

    #region FilterByExpectedCloseDate

    [Fact]
    public void FilterByExpectedCloseDate_DealInRange_IsIncluded()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "In Range", ExpectedCloseDate = "2024-05-15" }
        };
        var after = new DateTime(2024, 4, 1);
        var before = new DateTime(2024, 6, 30);

        // Act
        var result = DateFilterHelper.FilterByExpectedCloseDate(deals, after, before).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void FilterByExpectedCloseDate_DealBeforeRange_IsExcluded()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "Before Range", ExpectedCloseDate = "2024-03-15" }
        };
        var after = new DateTime(2024, 4, 1);
        var before = new DateTime(2024, 6, 30);

        // Act
        var result = DateFilterHelper.FilterByExpectedCloseDate(deals, after, before).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterByExpectedCloseDate_DealAfterRange_IsExcluded()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "After Range", ExpectedCloseDate = "2024-08-15" }
        };
        var after = new DateTime(2024, 4, 1);
        var before = new DateTime(2024, 6, 30);

        // Act
        var result = DateFilterHelper.FilterByExpectedCloseDate(deals, after, before).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterByExpectedCloseDate_NullExpectedCloseDate_IsExcluded()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "No Close Date", ExpectedCloseDate = null }
        };
        var after = new DateTime(2024, 4, 1);
        var before = new DateTime(2024, 6, 30);

        // Act
        var result = DateFilterHelper.FilterByExpectedCloseDate(deals, after, before).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterByExpectedCloseDate_BoundaryExactMatch_IsIncluded()
    {
        // Arrange — deal closes exactly on the lower bound
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "On Boundary", ExpectedCloseDate = "2024-04-01" }
        };
        var after = new DateTime(2024, 4, 1);
        var before = new DateTime(2024, 6, 30);

        // Act
        var result = DateFilterHelper.FilterByExpectedCloseDate(deals, after, before).ToList();

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void FilterByExpectedCloseDate_UpperBoundaryExactMatch_IsIncluded()
    {
        // Arrange — deal closes exactly on the upper bound
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "On Upper Boundary", ExpectedCloseDate = "2024-06-30" }
        };
        var after = new DateTime(2024, 4, 1);
        var before = new DateTime(2024, 6, 30);

        // Act
        var result = DateFilterHelper.FilterByExpectedCloseDate(deals, after, before).ToList();

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void FilterByExpectedCloseDate_OnlyAfterFilter_IncludesAllAfter()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "Before", ExpectedCloseDate = "2024-03-15" },
            new Deal { Id = 2, Title = "On", ExpectedCloseDate = "2024-04-01" },
            new Deal { Id = 3, Title = "After", ExpectedCloseDate = "2024-05-15" }
        };
        var after = new DateTime(2024, 4, 1);

        // Act
        var result = DateFilterHelper.FilterByExpectedCloseDate(deals, after, null).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Id == 2);
        Assert.Contains(result, d => d.Id == 3);
    }

    [Fact]
    public void FilterByExpectedCloseDate_OnlyBeforeFilter_IncludesAllBefore()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "Before", ExpectedCloseDate = "2024-03-15" },
            new Deal { Id = 2, Title = "On", ExpectedCloseDate = "2024-04-01" },
            new Deal { Id = 3, Title = "After", ExpectedCloseDate = "2024-06-15" }
        };
        var before = new DateTime(2024, 4, 1);

        // Act
        var result = DateFilterHelper.FilterByExpectedCloseDate(deals, null, before).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Id == 1);
        Assert.Contains(result, d => d.Id == 2);
    }

    #endregion

    #region FilterByWonTime

    [Fact]
    public void FilterByWonTime_DealInRange_IsIncluded()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "Won In Range", WonTime = "2024-05-15T12:00:00Z" }
        };
        var after = new DateTimeOffset(new DateTime(2024, 4, 1));
        var before = new DateTimeOffset(new DateTime(2024, 6, 30));

        // Act
        var result = DateFilterHelper.FilterByWonTime(deals, after, before).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void FilterByWonTime_DealBeforeRange_IsExcluded()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "Won Before", WonTime = "2024-03-15T10:00:00Z" }
        };
        var after = new DateTimeOffset(new DateTime(2024, 4, 1));
        var before = new DateTimeOffset(new DateTime(2024, 6, 30));

        // Act
        var result = DateFilterHelper.FilterByWonTime(deals, after, before).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterByWonTime_NullWonTime_IsExcluded()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "Not Won", WonTime = null }
        };
        var after = new DateTimeOffset(new DateTime(2024, 4, 1));
        var before = new DateTimeOffset(new DateTime(2024, 6, 30));

        // Act
        var result = DateFilterHelper.FilterByWonTime(deals, after, before).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterByWonTime_TimezoneHandling_UTCWorks()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "Won UTC", WonTime = "2024-05-15T16:30:00Z" }
        };
        var after = new DateTimeOffset(2024, 5, 15, 0, 0, 0, TimeSpan.Zero);
        var before = new DateTimeOffset(2024, 5, 16, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = DateFilterHelper.FilterByWonTime(deals, after, before).ToList();

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void FilterByWonTime_UpperBoundIncludesEntireDay()
    {
        // Arrange — won late on the upper-bound day should still be included
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "Won Late", WonTime = "2024-06-30T23:59:59Z" }
        };
        var before = new DateTimeOffset(new DateTime(2024, 6, 30));

        // Act
        var result = DateFilterHelper.FilterByWonTime(deals, null, before).ToList();

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void FilterByWonTime_ExactLowerBound_IsIncluded()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "Won At Start", WonTime = "2024-04-01T00:00:00Z" }
        };
        var after = new DateTimeOffset(new DateTime(2024, 4, 1));

        // Act
        var result = DateFilterHelper.FilterByWonTime(deals, after, null).ToList();

        // Assert
        Assert.Single(result);
    }

    #endregion

    #region TryParseDateTimeFilter

    [Fact]
    public void TryParseDateTimeFilter_ISOString_ReturnsTrue()
    {
        // Act
        var result = DateFilterHelper.TryParseDateTimeFilter("2024-05-15T12:00:00Z", out var parsed);

        // Assert
        Assert.True(result);
        Assert.NotNull(parsed);
        Assert.Equal(2024, parsed.Value.Year);
        Assert.Equal(5, parsed.Value.Month);
        Assert.Equal(15, parsed.Value.Day);
        Assert.Equal(12, parsed.Value.Hour);
    }

    [Fact]
    public void TryParseDateTimeFilter_Null_ReturnsTrueWithNull()
    {
        // Act
        var result = DateFilterHelper.TryParseDateTimeFilter(null, out var parsed);

        // Assert
        Assert.True(result);
        Assert.Null(parsed);
    }

    [Fact]
    public void TryParseDateTimeFilter_Invalid_ReturnsFalse()
    {
        // Act
        var result = DateFilterHelper.TryParseDateTimeFilter("not-a-date", out var parsed);

        // Assert
        Assert.False(result);
        Assert.Null(parsed);
    }

    #endregion

    #region Combined Filtering

    [Fact]
    public void FilterByExpectedCloseDate_CombinedAfterBefore_ReturnsIntersection()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "Too Early", ExpectedCloseDate = "2024-03-01" },
            new Deal { Id = 2, Title = "In Range", ExpectedCloseDate = "2024-05-15" },
            new Deal { Id = 3, Title = "Too Late", ExpectedCloseDate = "2024-09-01" },
            new Deal { Id = 4, Title = "On Lower Bound", ExpectedCloseDate = "2024-04-01" },
            new Deal { Id = 5, Title = "On Upper Bound", ExpectedCloseDate = "2024-06-30" },
            new Deal { Id = 6, Title = "No Date", ExpectedCloseDate = null }
        };

        var after = new DateTime(2024, 4, 1);
        var before = new DateTime(2024, 6, 30);

        // Act
        var result = DateFilterHelper.FilterByExpectedCloseDate(deals, after, before).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, d => d.Id == 2); // In range
        Assert.Contains(result, d => d.Id == 4); // On lower bound
        Assert.Contains(result, d => d.Id == 5); // On upper bound
    }

    [Fact]
    public void FilterByWonTime_CombinedFilters_ReturnsCorrectResults()
    {
        // Arrange
        var deals = new List<Deal>
        {
            new Deal { Id = 1, Title = "Won Q1", WonTime = "2024-03-15T10:00:00Z" },
            new Deal { Id = 2, Title = "Won Q2", WonTime = "2024-05-15T12:00:00Z" },
            new Deal { Id = 3, Title = "Won Q3", WonTime = "2024-08-20T14:00:00Z" },
            new Deal { Id = 4, Title = "Not Won", WonTime = null }
        };

        var after = new DateTimeOffset(new DateTime(2024, 4, 1));
        var before = new DateTimeOffset(new DateTime(2024, 6, 30));

        // Act
        var result = DateFilterHelper.FilterByWonTime(deals, after, before).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].Id); // Only Q2 deal
    }

    #endregion
}