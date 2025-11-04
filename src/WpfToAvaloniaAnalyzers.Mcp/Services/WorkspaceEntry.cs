using Microsoft.CodeAnalysis;

namespace WpfToAvaloniaAnalyzers.Mcp.Services;

/// <summary>
/// Represents a cached workspace entry with metadata.
/// </summary>
public class WorkspaceEntry : IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceEntry"/> class.
    /// </summary>
    /// <param name="workspace">The MSBuild workspace.</param>
    /// <param name="path">The path to the solution or project file.</param>
    /// <param name="isSolution">Whether this is a solution or project workspace.</param>
    public WorkspaceEntry(Workspace workspace, string path, bool isSolution)
    {
        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        IsSolution = isSolution;
        LoadedAt = DateTime.UtcNow;
        LastAccessedAt = DateTime.UtcNow;
        State = WorkspaceState.Ready;
    }

    /// <summary>
    /// Gets the MSBuild workspace.
    /// </summary>
    public Workspace Workspace { get; }

    /// <summary>
    /// Gets the path to the solution or project file.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets a value indicating whether this is a solution workspace.
    /// </summary>
    public bool IsSolution { get; }

    /// <summary>
    /// Gets when the workspace was loaded.
    /// </summary>
    public DateTime LoadedAt { get; }

    /// <summary>
    /// Gets or sets when the workspace was last accessed.
    /// </summary>
    public DateTime LastAccessedAt { get; set; }

    /// <summary>
    /// Gets or sets the current state of the workspace.
    /// </summary>
    public WorkspaceState State { get; set; }

    /// <summary>
    /// Gets the idle time since last access.
    /// </summary>
    public TimeSpan IdleTime => DateTime.UtcNow - LastAccessedAt;

    /// <summary>
    /// Gets the lock for concurrent access control.
    /// </summary>
    public SemaphoreSlim Lock => _lock;

    /// <summary>
    /// Marks the workspace as accessed.
    /// </summary>
    public void MarkAccessed()
    {
        LastAccessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disposes the workspace entry.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        State = WorkspaceState.Disposing;

        try
        {
            Workspace.Dispose();
            _lock.Dispose();
        }
        finally
        {
            State = WorkspaceState.Disposed;
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
