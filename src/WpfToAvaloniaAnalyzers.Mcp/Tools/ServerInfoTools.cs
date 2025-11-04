using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol.Server;
using WpfToAvaloniaAnalyzers.Mcp.Configuration;

namespace WpfToAvaloniaAnalyzers.Mcp.Tools;

/// <summary>
/// Server information and health check tools.
/// </summary>
[McpServerToolType]
public static class ServerInfoTools
{
    /// <summary>
    /// Gets information about the MCP server, including version and capabilities.
    /// </summary>
    /// <param name="config">Server configuration.</param>
    /// <returns>Server information including version, configuration, and status.</returns>
    [McpServerTool]
    [Description("Get information about the WpfToAvalonia MCP Server, including version, configuration, and capabilities.")]
    public static ServerInfo GetServerInfo(McpServerConfiguration config)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "0.1.0";
        var assemblyInfo = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var informationalVersion = assemblyInfo?.InformationalVersion ?? version;

        return new ServerInfo
        {
            Name = "WpfToAvalonia MCP Server",
            Version = informationalVersion,
            Description = "Model Context Protocol server for automated WPF-to-Avalonia code conversion",
            Capabilities = new ServerCapabilities
            {
                SupportsAnalysis = true,
                SupportsCodeFixes = true,
                SupportsBatchConversion = true,
                SupportsWorkspaceCaching = config.WorkspaceCache.Enabled,
                MaxCachedWorkspaces = config.WorkspaceCache.MaxCachedWorkspaces,
                ParallelismEnabled = config.Parallelism.Enabled
            },
            Configuration = new ConfigurationSummary
            {
                MaxProjectSizeMB = config.Security.MaxProjectSizeMB,
                MaxFileCount = config.Security.MaxFileCount,
                AnalysisTimeoutSeconds = config.Timeouts.AnalysisTimeoutSeconds,
                CodeFixTimeoutSeconds = config.Timeouts.CodeFixTimeoutSeconds
            }
        };
    }

    /// <summary>
    /// Performs a health check on the server.
    /// </summary>
    /// <returns>Health check status.</returns>
    [McpServerTool]
    [Description("Perform a health check to verify the server is running and responsive.")]
    public static HealthCheckResult HealthCheck()
    {
        return new HealthCheckResult
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
            MemoryUsageMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0)
        };
    }
}

/// <summary>
/// Server information response.
/// </summary>
public class ServerInfo
{
    /// <summary>
    /// Gets or sets the server name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server capabilities.
    /// </summary>
    public ServerCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration summary.
    /// </summary>
    public ConfigurationSummary Configuration { get; set; } = new();
}

/// <summary>
/// Server capabilities information.
/// </summary>
public class ServerCapabilities
{
    /// <summary>
    /// Gets or sets whether the server supports project analysis.
    /// </summary>
    public bool SupportsAnalysis { get; set; }

    /// <summary>
    /// Gets or sets whether the server supports code fixes.
    /// </summary>
    public bool SupportsCodeFixes { get; set; }

    /// <summary>
    /// Gets or sets whether the server supports batch conversion.
    /// </summary>
    public bool SupportsBatchConversion { get; set; }

    /// <summary>
    /// Gets or sets whether workspace caching is enabled.
    /// </summary>
    public bool SupportsWorkspaceCaching { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of cached workspaces.
    /// </summary>
    public int MaxCachedWorkspaces { get; set; }

    /// <summary>
    /// Gets or sets whether parallel processing is enabled.
    /// </summary>
    public bool ParallelismEnabled { get; set; }
}

/// <summary>
/// Configuration summary.
/// </summary>
public class ConfigurationSummary
{
    /// <summary>
    /// Gets or sets the maximum project size in MB.
    /// </summary>
    public int MaxProjectSizeMB { get; set; }

    /// <summary>
    /// Gets or sets the maximum file count.
    /// </summary>
    public int MaxFileCount { get; set; }

    /// <summary>
    /// Gets or sets the analysis timeout in seconds.
    /// </summary>
    public int AnalysisTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the code fix timeout in seconds.
    /// </summary>
    public int CodeFixTimeoutSeconds { get; set; }
}

/// <summary>
/// Health check result.
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the health check.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the server uptime.
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Gets or sets the current memory usage in MB.
    /// </summary>
    public double MemoryUsageMB { get; set; }
}
