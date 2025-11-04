namespace WpfToAvaloniaAnalyzers.Mcp.Services;

/// <summary>
/// Represents the current state of a workspace.
/// </summary>
public enum WorkspaceState
{
    /// <summary>
    /// Workspace is being loaded.
    /// </summary>
    Loading,

    /// <summary>
    /// Workspace is loaded and ready for operations.
    /// </summary>
    Ready,

    /// <summary>
    /// Workspace is currently being analyzed.
    /// </summary>
    Analyzing,

    /// <summary>
    /// Workspace is being modified (code fixes being applied).
    /// </summary>
    Modifying,

    /// <summary>
    /// Workspace has encountered an error.
    /// </summary>
    Error,

    /// <summary>
    /// Workspace is being disposed.
    /// </summary>
    Disposing,

    /// <summary>
    /// Workspace has been disposed.
    /// </summary>
    Disposed
}
