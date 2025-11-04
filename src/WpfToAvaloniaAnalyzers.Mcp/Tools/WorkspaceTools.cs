using System.ComponentModel;
using ModelContextProtocol.Server;
using WpfToAvaloniaAnalyzers.Mcp.Services;

namespace WpfToAvaloniaAnalyzers.Mcp.Tools;

/// <summary>
/// Tools for workspace management and diagnostics.
/// </summary>
[McpServerToolType]
public static class WorkspaceTools
{
    /// <summary>
    /// Gets statistics about the workspace cache.
    /// </summary>
    /// <param name="workspaceManager">Workspace manager instance.</param>
    /// <returns>Cache statistics.</returns>
    [McpServerTool]
    [Description("Get statistics about the workspace cache, including memory usage and workspace count.")]
    public static WorkspaceCacheStats GetWorkspaceCacheStats(MCPWorkspaceManager workspaceManager)
    {
        return workspaceManager.GetCacheStats();
    }

    /// <summary>
    /// Clears all cached workspaces to free up memory.
    /// </summary>
    /// <param name="workspaceManager">Workspace manager instance.</param>
    /// <returns>Result of the cache clear operation.</returns>
    [McpServerTool]
    [Description("Clear all cached workspaces to free up memory. Use this if experiencing memory pressure or before analyzing a large project.")]
    public static async Task<CacheClearResult> ClearWorkspaceCacheAsync(MCPWorkspaceManager workspaceManager)
    {
        var statsBefore = workspaceManager.GetCacheStats();
        await workspaceManager.ClearCacheAsync();
        var statsAfter = workspaceManager.GetCacheStats();

        return new CacheClearResult
        {
            Success = true,
            WorkspacesCleared = statsBefore.TotalWorkspaces,
            MemoryFreedMB = statsBefore.TotalMemoryMB - statsAfter.TotalMemoryMB
        };
    }
}

/// <summary>
/// Result of clearing the workspace cache.
/// </summary>
public class CacheClearResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of workspaces cleared.
    /// </summary>
    public int WorkspacesCleared { get; set; }

    /// <summary>
    /// Gets or sets the approximate memory freed in MB.
    /// </summary>
    public double MemoryFreedMB { get; set; }
}
