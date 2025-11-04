using Microsoft.Extensions.Logging;

namespace WpfToAvaloniaAnalyzers.Mcp.Services;

/// <summary>
/// Watches workspace files for external changes.
/// </summary>
public class WorkspaceFileWatcher : IDisposable
{
    private readonly ILogger<WorkspaceFileWatcher> _logger;
    private readonly Dictionary<string, FileSystemWatcher> _watchers;
    private readonly object _watcherLock = new();
    private bool _disposed;

    /// <summary>
    /// Event raised when a workspace file changes.
    /// </summary>
    public event EventHandler<WorkspaceFileChangedEventArgs>? FileChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceFileWatcher"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public WorkspaceFileWatcher(ILogger<WorkspaceFileWatcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _watchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Starts watching a workspace path for changes.
    /// </summary>
    /// <param name="path">Path to solution or project file.</param>
    public void WatchWorkspace(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}", path);

        path = Path.GetFullPath(path);

        lock (_watcherLock)
        {
            if (_watchers.ContainsKey(path))
            {
                _logger.LogDebug("Already watching workspace: {Path}", path);
                return;
            }

            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                _logger.LogWarning("Could not determine directory for {Path}", path);
                return;
            }

            var watcher = new FileSystemWatcher(directory)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.*", // Watch all files
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            // Subscribe to events
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Deleted += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            watcher.Error += OnWatcherError;

            _watchers[path] = watcher;

            _logger.LogInformation("Started watching workspace: {Path}", path);
        }
    }

    /// <summary>
    /// Stops watching a workspace path.
    /// </summary>
    /// <param name="path">Path to solution or project file.</param>
    public void StopWatching(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        path = Path.GetFullPath(path);

        lock (_watcherLock)
        {
            if (_watchers.TryGetValue(path, out var watcher))
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                _watchers.Remove(path);

                _logger.LogInformation("Stopped watching workspace: {Path}", path);
            }
        }
    }

    /// <summary>
    /// Stops watching all workspaces.
    /// </summary>
    public void StopAll()
    {
        lock (_watcherLock)
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            _watchers.Clear();
            _logger.LogInformation("Stopped watching all workspaces");
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Filter for relevant file types
        if (!IsRelevantFile(e.FullPath))
            return;

        _logger.LogDebug(
            "File {ChangeType}: {Path}",
            e.ChangeType,
            e.FullPath);

        RaiseFileChanged(e.FullPath, e.ChangeType);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (!IsRelevantFile(e.FullPath))
            return;

        _logger.LogDebug(
            "File renamed: {OldPath} -> {NewPath}",
            e.OldFullPath,
            e.FullPath);

        RaiseFileChanged(e.FullPath, WatcherChangeTypes.Renamed);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        var exception = e.GetException();
        _logger.LogError(exception, "File watcher error occurred");
    }

    private bool IsRelevantFile(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        // Watch C# source files, project files, and solution files
        return extension switch
        {
            ".cs" => true,
            ".csproj" => true,
            ".sln" => true,
            ".xaml" => true,
            ".axaml" => true,
            ".json" => path.EndsWith("project.json", StringComparison.OrdinalIgnoreCase) ||
                      path.EndsWith("global.json", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private void RaiseFileChanged(string path, WatcherChangeTypes changeType)
    {
        try
        {
            FileChanged?.Invoke(this, new WorkspaceFileChangedEventArgs(path, changeType));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error raising FileChanged event for {Path}", path);
        }
    }

    /// <summary>
    /// Disposes the file watcher.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        StopAll();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Event args for workspace file changes.
/// </summary>
public class WorkspaceFileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceFileChangedEventArgs"/> class.
    /// </summary>
    /// <param name="filePath">The path to the changed file.</param>
    /// <param name="changeType">The type of change.</param>
    public WorkspaceFileChangedEventArgs(string filePath, WatcherChangeTypes changeType)
    {
        FilePath = filePath;
        ChangeType = changeType;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the path to the changed file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the type of change.
    /// </summary>
    public WatcherChangeTypes ChangeType { get; }

    /// <summary>
    /// Gets the timestamp of the change.
    /// </summary>
    public DateTime Timestamp { get; }
}
