using System.Runtime.InteropServices;
using System.Text.Json;
using PipedriveCLI.Models;

namespace PipedriveCLI.Services;

/// <summary>
/// Service for managing Pipedrive CLI configuration stored in ~/.pipedrive/config.json
/// Supports multiple profiles and environment variable overrides
/// </summary>
public sealed class ConfigurationService
{
    private const string ConfigDirectoryName = ".pipedrive";
    private const string ConfigFileName = "config.json";

    private readonly string _configDirectory;
    private readonly string _configFilePath;

    public ConfigurationService()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _configDirectory = Path.Combine(homeDirectory, ConfigDirectoryName);
        _configFilePath = Path.Combine(_configDirectory, ConfigFileName);
    }

    /// <summary>
    /// Gets the full path to the configuration file
    /// </summary>
    public string ConfigFilePath => _configFilePath;

    /// <summary>
    /// Loads the configuration from disk, creating default if it doesn't exist
    /// </summary>
    public async Task<PipedriveConfig> LoadConfigAsync()
    {
        if (!File.Exists(_configFilePath))
        {
            var defaultConfig = new PipedriveConfig
            {
                ActiveProfile = "default",
                Profiles = new Dictionary<string, ProfileConfig>
                {
                    ["default"] = new ProfileConfig()
                }
            };

            await SaveConfigAsync(defaultConfig);
            return defaultConfig;
        }

        var json = await File.ReadAllTextAsync(_configFilePath);
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.PipedriveConfig);

        if (config == null)
        {
            throw new InvalidOperationException($"Failed to deserialize configuration from {_configFilePath}");
        }

        return config;
    }

    /// <summary>
    /// Saves the configuration to disk with secure file permissions (Unix only)
    /// </summary>
    public async Task SaveConfigAsync(PipedriveConfig config)
    {
        // Ensure directory exists
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
        }

        // Serialize config
        var json = JsonSerializer.Serialize(config, ConfigJsonContext.Default.PipedriveConfig);

        // Write to file
        await File.WriteAllTextAsync(_configFilePath, json);

        // Set Unix file permissions (600 = read/write for owner only)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            SetUnixFilePermissions(_configFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    /// <summary>
    /// Gets the active profile configuration with environment variable overrides
    /// </summary>
    public async Task<ProfileConfig> GetActiveProfileAsync()
    {
        var config = await LoadConfigAsync();
        var profileName = config.ActiveProfile;

        if (!config.Profiles.TryGetValue(profileName, out var profile))
        {
            throw new InvalidOperationException($"Active profile '{profileName}' not found in configuration");
        }

        // Apply environment variable overrides
        var effectiveProfile = new ProfileConfig
        {
            ApiKey = GetEnvironmentVariableOrValue("PIPEDRIVE_API_KEY", profile.ApiKey),
            Domain = GetEnvironmentVariableOrValue("PIPEDRIVE_DOMAIN", profile.Domain)
        };

        return effectiveProfile;
    }

    /// <summary>
    /// Sets the API key for the active profile
    /// </summary>
    public async Task SetApiKeyAsync(string apiKey)
    {
        var config = await LoadConfigAsync();
        var profileName = config.ActiveProfile;

        if (!config.Profiles.ContainsKey(profileName))
        {
            config.Profiles[profileName] = new ProfileConfig();
        }

        config.Profiles[profileName].ApiKey = apiKey;
        await SaveConfigAsync(config);
    }

    /// <summary>
    /// Sets the domain for the active profile
    /// </summary>
    public async Task SetDomainAsync(string domain)
    {
        var config = await LoadConfigAsync();
        var profileName = config.ActiveProfile;

        if (!config.Profiles.ContainsKey(profileName))
        {
            config.Profiles[profileName] = new ProfileConfig();
        }

        config.Profiles[profileName].Domain = domain;
        await SaveConfigAsync(config);
    }

    /// <summary>
    /// Sets multiple configuration values at once
    /// </summary>
    public async Task SetConfigAsync(string? apiKey = null, string? domain = null)
    {
        var config = await LoadConfigAsync();
        var profileName = config.ActiveProfile;

        if (!config.Profiles.ContainsKey(profileName))
        {
            config.Profiles[profileName] = new ProfileConfig();
        }

        var profile = config.Profiles[profileName];

        if (apiKey != null) profile.ApiKey = apiKey;
        if (domain != null) profile.Domain = domain;

        await SaveConfigAsync(config);
    }

    /// <summary>
    /// Switches to a different profile
    /// </summary>
    public async Task SetActiveProfileAsync(string profileName)
    {
        var config = await LoadConfigAsync();

        if (!config.Profiles.ContainsKey(profileName))
        {
            config.Profiles[profileName] = new ProfileConfig();
        }

        config.ActiveProfile = profileName;
        await SaveConfigAsync(config);
    }

    /// <summary>
    /// Lists all available profiles
    /// </summary>
    public async Task<Dictionary<string, ProfileConfig>> ListProfilesAsync()
    {
        var config = await LoadConfigAsync();
        return config.Profiles;
    }

    /// <summary>
    /// Tests if the configuration is valid and the API key works
    /// </summary>
    public async Task<bool> TestConfigurationAsync()
    {
        try
        {
            var profile = await GetActiveProfileAsync();
            return profile.IsValid();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current active profile name
    /// </summary>
    public async Task<string> GetActiveProfileNameAsync()
    {
        var config = await LoadConfigAsync();
        return config.ActiveProfile;
    }

    private static string? GetEnvironmentVariableOrValue(string envVarName, string? configValue)
    {
        var envValue = Environment.GetEnvironmentVariable(envVarName);
        return !string.IsNullOrWhiteSpace(envValue) ? envValue : configValue;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    private static void SetUnixFilePermissions(string filePath, UnixFileMode mode)
    {
        try
        {
            File.SetUnixFileMode(filePath, mode);
        }
        catch (Exception ex)
        {
            // Log warning but don't fail - permissions are a security enhancement, not critical
            Console.WriteLine($"Warning: Could not set file permissions on {filePath}: {ex.Message}");
        }
    }
}
