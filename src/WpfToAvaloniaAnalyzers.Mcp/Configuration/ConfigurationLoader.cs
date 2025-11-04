using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WpfToAvaloniaAnalyzers.Mcp.Configuration;

/// <summary>
/// Loads and validates MCP server configuration from various sources.
/// </summary>
public class ConfigurationLoader
{
    private readonly ILogger<ConfigurationLoader>? _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationLoader"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public ConfigurationLoader(ILogger<ConfigurationLoader>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads configuration from the specified file path with environment variable overrides.
    /// </summary>
    /// <param name="configFilePath">Path to the configuration JSON file. If null, uses default location.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loaded and validated configuration.</returns>
    public async Task<McpServerConfiguration> LoadConfigurationAsync(
        string? configFilePath = null,
        CancellationToken cancellationToken = default)
    {
        McpServerConfiguration config;

        // Try to load from file if specified
        if (!string.IsNullOrWhiteSpace(configFilePath))
        {
            if (!File.Exists(configFilePath))
            {
                _logger?.LogWarning("Configuration file not found at {Path}, using defaults", configFilePath);
                config = new McpServerConfiguration();
            }
            else
            {
                try
                {
                    _logger?.LogInformation("Loading configuration from {Path}", configFilePath);
                    var json = await File.ReadAllTextAsync(configFilePath, cancellationToken);
                    config = JsonSerializer.Deserialize<McpServerConfiguration>(json, JsonOptions)
                        ?? new McpServerConfiguration();
                    _logger?.LogInformation("Configuration loaded successfully");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to load configuration from {Path}, using defaults", configFilePath);
                    config = new McpServerConfiguration();
                }
            }
        }
        else
        {
            // Try default locations
            config = await TryLoadFromDefaultLocationsAsync(cancellationToken);
        }

        // Apply environment variable overrides
        ApplyEnvironmentVariableOverrides(config);

        // Validate configuration
        ValidateConfiguration(config);

        return config;
    }

    /// <summary>
    /// Tries to load configuration from default locations.
    /// </summary>
    private async Task<McpServerConfiguration> TryLoadFromDefaultLocationsAsync(CancellationToken cancellationToken)
    {
        var defaultLocations = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "mcpconfig.json"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wpf-to-avalonia", "mcpconfig.json"),
            Path.Combine(AppContext.BaseDirectory, "mcpconfig.json")
        };

        foreach (var location in defaultLocations)
        {
            if (File.Exists(location))
            {
                try
                {
                    _logger?.LogInformation("Found configuration at default location: {Path}", location);
                    var json = await File.ReadAllTextAsync(location, cancellationToken);
                    return JsonSerializer.Deserialize<McpServerConfiguration>(json, JsonOptions)
                        ?? new McpServerConfiguration();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to load configuration from {Path}", location);
                }
            }
        }

        _logger?.LogInformation("No configuration file found, using defaults");
        return new McpServerConfiguration();
    }

    /// <summary>
    /// Applies environment variable overrides to the configuration.
    /// </summary>
    /// <param name="config">Configuration to override.</param>
    private void ApplyEnvironmentVariableOverrides(McpServerConfiguration config)
    {
        _logger?.LogDebug("Applying environment variable overrides");

        // Workspace Cache
        if (TryGetEnvBool("WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE_ENABLED", out var cacheEnabled))
            config.WorkspaceCache.Enabled = cacheEnabled;

        if (TryGetEnvInt("WPF_TO_AVALONIA_MCP_WORKSPACE_MAX_CACHED", out var maxCached))
            config.WorkspaceCache.MaxCachedWorkspaces = maxCached;

        if (TryGetEnvInt("WPF_TO_AVALONIA_MCP_WORKSPACE_IDLE_TIMEOUT_MINUTES", out var idleTimeout))
            config.WorkspaceCache.IdleTimeoutMinutes = idleTimeout;

        if (TryGetEnvInt("WPF_TO_AVALONIA_MCP_WORKSPACE_MAX_MEMORY_MB", out var maxMemory))
            config.WorkspaceCache.MaxMemoryMB = maxMemory;

        // Timeouts
        if (TryGetEnvInt("WPF_TO_AVALONIA_MCP_WORKSPACE_LOAD_TIMEOUT_SECONDS", out var loadTimeout))
            config.Timeouts.WorkspaceLoadTimeoutSeconds = loadTimeout;

        if (TryGetEnvInt("WPF_TO_AVALONIA_MCP_ANALYSIS_TIMEOUT_SECONDS", out var analysisTimeout))
            config.Timeouts.AnalysisTimeoutSeconds = analysisTimeout;

        if (TryGetEnvInt("WPF_TO_AVALONIA_MCP_CODEFIX_TIMEOUT_SECONDS", out var codeFixTimeout))
            config.Timeouts.CodeFixTimeoutSeconds = codeFixTimeout;

        if (TryGetEnvInt("WPF_TO_AVALONIA_MCP_REQUEST_TIMEOUT_SECONDS", out var requestTimeout))
            config.Timeouts.RequestTimeoutSeconds = requestTimeout;

        // Parallelism
        if (TryGetEnvBool("WPF_TO_AVALONIA_MCP_PARALLELISM_ENABLED", out var parallelismEnabled))
            config.Parallelism.Enabled = parallelismEnabled;

        if (TryGetEnvInt("WPF_TO_AVALONIA_MCP_MAX_DEGREE_OF_PARALLELISM", out var maxDegree))
            config.Parallelism.MaxDegreeOfParallelism = maxDegree;

        if (TryGetEnvInt("WPF_TO_AVALONIA_MCP_MAX_CONCURRENT_WORKSPACE_OPS", out var maxConcurrent))
            config.Parallelism.MaxConcurrentWorkspaceOperations = maxConcurrent;

        // Logging
        if (TryGetEnvString("WPF_TO_AVALONIA_MCP_LOG_LEVEL", out var logLevel))
            config.Logging.MinimumLevel = logLevel;

        if (TryGetEnvBool("WPF_TO_AVALONIA_MCP_LOG_TO_FILE", out var logToFile))
            config.Logging.WriteToFile = logToFile;

        if (TryGetEnvString("WPF_TO_AVALONIA_MCP_LOG_FILE_PATH", out var logFilePath))
            config.Logging.LogFilePath = logFilePath;

        if (TryGetEnvBool("WPF_TO_AVALONIA_MCP_LOG_DETAILED_DIAGNOSTICS", out var detailedDiagnostics))
            config.Logging.IncludeDetailedDiagnostics = detailedDiagnostics;

        // Security
        if (TryGetEnvInt("WPF_TO_AVALONIA_MCP_MAX_PROJECT_SIZE_MB", out var maxProjectSize))
            config.Security.MaxProjectSizeMB = maxProjectSize;

        if (TryGetEnvInt("WPF_TO_AVALONIA_MCP_MAX_FILE_COUNT", out var maxFileCount))
            config.Security.MaxFileCount = maxFileCount;

        if (TryGetEnvBool("WPF_TO_AVALONIA_MCP_VALIDATE_FILE_PATHS", out var validatePaths))
            config.Security.ValidateFilePaths = validatePaths;

        if (TryGetEnvString("WPF_TO_AVALONIA_MCP_ALLOWED_ROOT_PATHS", out var allowedPaths))
        {
            // Parse comma-separated paths
            config.Security.AllowedRootPaths = allowedPaths
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
    }

    /// <summary>
    /// Validates the configuration and applies constraints.
    /// </summary>
    /// <param name="config">Configuration to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    private void ValidateConfiguration(McpServerConfiguration config)
    {
        _logger?.LogDebug("Validating configuration");

        var errors = new List<string>();

        // Workspace Cache validation
        if (config.WorkspaceCache.MaxCachedWorkspaces < 1)
            errors.Add("WorkspaceCache.MaxCachedWorkspaces must be at least 1");

        if (config.WorkspaceCache.IdleTimeoutMinutes < 1)
            errors.Add("WorkspaceCache.IdleTimeoutMinutes must be at least 1");

        if (config.WorkspaceCache.MaxMemoryMB < 256)
            errors.Add("WorkspaceCache.MaxMemoryMB must be at least 256");

        // Timeout validation
        if (config.Timeouts.WorkspaceLoadTimeoutSeconds < 10)
            errors.Add("Timeouts.WorkspaceLoadTimeoutSeconds must be at least 10");

        if (config.Timeouts.AnalysisTimeoutSeconds < 10)
            errors.Add("Timeouts.AnalysisTimeoutSeconds must be at least 10");

        if (config.Timeouts.CodeFixTimeoutSeconds < 10)
            errors.Add("Timeouts.CodeFixTimeoutSeconds must be at least 10");

        if (config.Timeouts.RequestTimeoutSeconds < 10)
            errors.Add("Timeouts.RequestTimeoutSeconds must be at least 10");

        // Parallelism validation
        if (config.Parallelism.MaxDegreeOfParallelism < -1)
            errors.Add("Parallelism.MaxDegreeOfParallelism must be -1 or greater");

        if (config.Parallelism.MaxConcurrentWorkspaceOperations < 1)
            errors.Add("Parallelism.MaxConcurrentWorkspaceOperations must be at least 1");

        // Logging validation
        var validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "None" };
        if (!validLogLevels.Contains(config.Logging.MinimumLevel, StringComparer.OrdinalIgnoreCase))
            errors.Add($"Logging.MinimumLevel must be one of: {string.Join(", ", validLogLevels)}");

        // Security validation
        if (config.Security.MaxProjectSizeMB < 1)
            errors.Add("Security.MaxProjectSizeMB must be at least 1");

        if (config.Security.MaxFileCount < 1)
            errors.Add("Security.MaxFileCount must be at least 1");

        // Validate allowed root paths exist
        foreach (var path in config.Security.AllowedRootPaths)
        {
            if (!Path.IsPathRooted(path))
                errors.Add($"Allowed root path must be absolute: {path}");
        }

        if (errors.Any())
        {
            var errorMessage = "Configuration validation failed:\n" + string.Join("\n", errors);
            _logger?.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        _logger?.LogInformation("Configuration validated successfully");
    }

    /// <summary>
    /// Saves the configuration to a file.
    /// </summary>
    /// <param name="config">Configuration to save.</param>
    /// <param name="filePath">File path to save to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SaveConfigurationAsync(
        McpServerConfiguration config,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
            _logger?.LogInformation("Configuration saved to {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save configuration to {Path}", filePath);
            throw;
        }
    }

    // Helper methods for environment variable parsing

    private static bool TryGetEnvString(string key, out string value)
    {
        value = Environment.GetEnvironmentVariable(key) ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetEnvInt(string key, out int value)
    {
        var strValue = Environment.GetEnvironmentVariable(key);
        return int.TryParse(strValue, out value);
    }

    private static bool TryGetEnvBool(string key, out bool value)
    {
        var strValue = Environment.GetEnvironmentVariable(key);
        return bool.TryParse(strValue, out value);
    }
}
