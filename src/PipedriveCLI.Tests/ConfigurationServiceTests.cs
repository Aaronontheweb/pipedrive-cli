using PipedriveCLI.Services;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Tests for ConfigurationService, particularly domain normalization
/// </summary>
public class ConfigurationServiceTests
{
    [Theory]
    [InlineData("company", "company.pipedrive.com")] // Simple company name
    [InlineData("Company", "company.pipedrive.com")] // Case normalization
    [InlineData("company.pipedrive.com", "company.pipedrive.com")] // Already qualified
    [InlineData("https://company.pipedrive.com/", "company.pipedrive.com")] // Protocol + trailing slash
    [InlineData("  company  ", "company.pipedrive.com")] // Whitespace trimming
    public void NormalizeDomain_ShouldHandleVariousFormats(string input, string expected)
    {
        // Act
        var result = ConfigurationService.NormalizeDomain(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeDomain_ShouldHandleNull()
    {
        // Act
        var result = ConfigurationService.NormalizeDomain(null!);

        // Assert
        Assert.Null(result);
    }
}
