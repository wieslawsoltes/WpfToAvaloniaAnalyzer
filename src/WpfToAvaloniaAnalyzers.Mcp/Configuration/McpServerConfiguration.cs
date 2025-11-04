namespace WpfToAvaloniaAnalyzers.Mcp.Configuration;

/// <summary>
/// Configuration settings for the WpfToAvalonia MCP Server.
/// </summary>
public class McpServerConfiguration
{
    /// <summary>
    /// Gets or sets the workspace cache configuration.
    /// </summary>
    public WorkspaceCacheSettings WorkspaceCache { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout configuration.
    /// </summary>
    public TimeoutSettings Timeouts { get; set; } = new();

    /// <summary>
    /// Gets or sets the parallelism configuration.
    /// </summary>
    public ParallelismSettings Parallelism { get; set; } = new();

    /// <summary>
    /// Gets or sets the logging configuration.
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// Gets or sets the security configuration.
    /// </summary>
    public SecuritySettings Security { get; set; } = new();
}

/// <summary>
/// Workspace cache configuration.
/// </summary>
public class WorkspaceCacheSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether workspace caching is enabled.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of workspaces to cache.
    /// Default: 5.
    /// </summary>
    public int MaxCachedWorkspaces { get; set; } = 5;

    /// <summary>
    /// Gets or sets the workspace idle timeout in minutes before eviction.
    /// Default: 30 minutes.
    /// </summary>
    public int IdleTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum memory usage in MB before cache eviction.
    /// Default: 2048 MB (2 GB).
    /// </summary>
    public int MaxMemoryMB { get; set; } = 2048;
}

/// <summary>
/// Timeout configuration for various operations.
/// </summary>
public class TimeoutSettings
{
    /// <summary>
    /// Gets or sets the timeout for workspace loading in seconds.
    /// Default: 120 seconds (2 minutes).
    /// </summary>
    public int WorkspaceLoadTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets the timeout for analysis operations in seconds.
    /// Default: 300 seconds (5 minutes).
    /// </summary>
    public int AnalysisTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the timeout for code fix operations in seconds.
    /// Default: 600 seconds (10 minutes).
    /// </summary>
    public int CodeFixTimeoutSeconds { get; set; } = 600;

    /// <summary>
    /// Gets or sets the timeout for individual MCP requests in seconds.
    /// Default: 900 seconds (15 minutes).
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 900;
}

/// <summary>
/// Parallelism configuration for concurrent operations.
/// </summary>
public class ParallelismSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether parallel processing is enabled.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for analysis operations.
    /// Default: -1 (use system default based on CPU cores).
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = -1;

    /// <summary>
    /// Gets or sets the maximum number of concurrent workspace operations.
    /// Default: 3.
    /// </summary>
    public int MaxConcurrentWorkspaceOperations { get; set; } = 3;
}

/// <summary>
/// Logging configuration.
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Gets or sets the minimum log level.
    /// Values: Trace, Debug, Information, Warning, Error, Critical, None.
    /// Default: Information.
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets a value indicating whether to write logs to a file.
    /// Default: true.
    /// </summary>
    public bool WriteToFile { get; set; } = true;

    /// <summary>
    /// Gets or sets the log file path. If null, uses default location.
    /// Default: null (uses ./logs/mcp-server.log).
    /// </summary>
    public string? LogFilePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include detailed diagnostics.
    /// Default: false.
    /// </summary>
    public bool IncludeDetailedDiagnostics { get; set; } = false;
}

/// <summary>
/// Security configuration.
/// </summary>
public class SecuritySettings
{
    /// <summary>
    /// Gets or sets the maximum project size in MB to process.
    /// Default: 1024 MB (1 GB).
    /// </summary>
    public int MaxProjectSizeMB { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the maximum number of files in a project to process.
    /// Default: 10000.
    /// </summary>
    public int MaxFileCount { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the allowed root paths for workspace access.
    /// Empty list means all paths are allowed.
    /// Default: empty (no restrictions).
    /// </summary>
    public List<string> AllowedRootPaths { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to validate file paths for security.
    /// Default: true.
    /// </summary>
    public bool ValidateFilePaths { get; set; } = true;
}
