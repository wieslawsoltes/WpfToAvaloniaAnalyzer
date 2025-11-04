using System.Collections.Concurrent;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using WpfToAvaloniaAnalyzers.Mcp.Configuration;

namespace WpfToAvaloniaAnalyzers.Mcp.Services;

/// <summary>
/// Manages MSBuild workspaces with caching and lifecycle management.
/// </summary>
public class MCPWorkspaceManager : IDisposable
{
    private readonly McpServerConfiguration _config;
    private readonly ILogger<MCPWorkspaceManager> _logger;
    private readonly ConcurrentDictionary<string, WorkspaceEntry> _workspaceCache;
    private readonly SemaphoreSlim _cacheAccessLock = new(1, 1);
    private readonly Timer _cleanupTimer;
    private readonly WorkspaceFileWatcher? _fileWatcher;
    private readonly Dictionary<string, DateTime> _pendingReloads;
    private readonly Timer _reloadDebounceTimer;
    private static bool _msbuildRegistered;
    private static readonly object _msbuildLock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MCPWorkspaceManager"/> class.
    /// </summary>
    /// <param name="config">Server configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public MCPWorkspaceManager(
        McpServerConfiguration config,
        ILogger<MCPWorkspaceManager> logger,
        ILogger<WorkspaceFileWatcher>? fileWatcherLogger = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workspaceCache = new ConcurrentDictionary<string, WorkspaceEntry>(StringComparer.OrdinalIgnoreCase);
        _pendingReloads = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        // Ensure MSBuild is registered
        EnsureMSBuildRegistered();

        // Set up file watcher for workspace changes (optional)
        if (fileWatcherLogger != null)
        {
            _fileWatcher = new WorkspaceFileWatcher(fileWatcherLogger);
            _fileWatcher.FileChanged += OnWorkspaceFileChanged;
            _logger.LogInformation("Workspace file watching enabled");
        }

        // Set up reload debounce timer (processes pending reloads every 2 seconds)
        _reloadDebounceTimer = new Timer(
            ProcessPendingReloads,
            null,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(2));

        // Set up cleanup timer if caching is enabled
        if (_config.WorkspaceCache.Enabled)
        {
            var cleanupInterval = TimeSpan.FromMinutes(Math.Max(1, _config.WorkspaceCache.IdleTimeoutMinutes / 2));
            _cleanupTimer = new Timer(
                CleanupIdleWorkspaces,
                null,
                cleanupInterval,
                cleanupInterval);

            _logger.LogInformation(
                "Workspace cache enabled: max {MaxWorkspaces} workspaces, {IdleTimeout}min idle timeout",
                _config.WorkspaceCache.MaxCachedWorkspaces,
                _config.WorkspaceCache.IdleTimeoutMinutes);
        }
        else
        {
            _cleanupTimer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
            _logger.LogInformation("Workspace caching disabled");
        }
    }

    /// <summary>
    /// Gets or opens a workspace for the specified solution or project file.
    /// </summary>
    /// <param name="path">Path to .sln or .csproj file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Workspace entry.</returns>
    public async Task<WorkspaceEntry> GetOrOpenWorkspaceAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}", path);

        // Normalize path
        path = Path.GetFullPath(path);

        // Check cache first
        if (_config.WorkspaceCache.Enabled && _workspaceCache.TryGetValue(path, out var cachedEntry))
        {
            _logger.LogDebug("Workspace cache hit for {Path}", path);
            cachedEntry.MarkAccessed();
            return cachedEntry;
        }

        _logger.LogInformation("Loading workspace for {Path}", path);

        // Load workspace
        var entry = await LoadWorkspaceAsync(path, cancellationToken);

        // Cache if enabled
        if (_config.WorkspaceCache.Enabled)
        {
            await _cacheAccessLock.WaitAsync(cancellationToken);
            try
            {
                // Check cache size and evict if necessary
                await EnsureCacheCapacityAsync();

                _workspaceCache[path] = entry;
                _logger.LogDebug("Cached workspace for {Path} ({CacheSize}/{MaxSize})",
                    path, _workspaceCache.Count, _config.WorkspaceCache.MaxCachedWorkspaces);

                // Start watching this workspace for changes
                _fileWatcher?.WatchWorkspace(path);
            }
            finally
            {
                _cacheAccessLock.Release();
            }
        }

        return entry;
    }

    /// <summary>
    /// Removes a workspace from the cache and disposes it.
    /// </summary>
    /// <param name="path">Path to the workspace.</param>
    public async Task RemoveWorkspaceAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        path = Path.GetFullPath(path);

        if (_workspaceCache.TryRemove(path, out var entry))
        {
            await _cacheAccessLock.WaitAsync();
            try
            {
                _logger.LogInformation("Removing workspace from cache: {Path}", path);
                entry.Dispose();
            }
            finally
            {
                _cacheAccessLock.Release();
            }
        }
    }

    /// <summary>
    /// Clears all cached workspaces.
    /// </summary>
    public async Task ClearCacheAsync()
    {
        await _cacheAccessLock.WaitAsync();
        try
        {
            _logger.LogInformation("Clearing workspace cache ({Count} workspaces)", _workspaceCache.Count);

            foreach (var entry in _workspaceCache.Values)
            {
                entry.Dispose();
            }

            _workspaceCache.Clear();
        }
        finally
        {
            _cacheAccessLock.Release();
        }
    }

    /// <summary>
    /// Gets statistics about the workspace cache.
    /// </summary>
    public WorkspaceCacheStats GetCacheStats()
    {
        var entries = _workspaceCache.Values.ToList();

        return new WorkspaceCacheStats
        {
            TotalWorkspaces = entries.Count,
            MaxWorkspaces = _config.WorkspaceCache.MaxCachedWorkspaces,
            CacheEnabled = _config.WorkspaceCache.Enabled,
            WorkspacesByState = entries.GroupBy(e => e.State)
                .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            TotalMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            MaxMemoryMB = _config.WorkspaceCache.MaxMemoryMB
        };
    }

    private async Task<WorkspaceEntry> LoadWorkspaceAsync(string path, CancellationToken cancellationToken)
    {
        var isSolution = path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase);
        var timeout = TimeSpan.FromSeconds(_config.Timeouts.WorkspaceLoadTimeoutSeconds);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            // Create workspace with custom properties for better compatibility
            var properties = new Dictionary<string, string>
            {
                // Disable design-time builds which can be slow
                ["DesignTimeBuild"] = "true",
                // Skip analyzers during initial load for performance
                ["RunAnalyzers"] = "false",
                ["RunAnalyzersDuringBuild"] = "false",
                // Enable faster restore
                ["RestorePackagesWithLockFile"] = "false"
            };

            var workspace = MSBuildWorkspace.Create(properties);

            // Subscribe to workspace events for better diagnostics
            workspace.WorkspaceFailed += (sender, args) =>
            {
                _logger.LogWarning(
                    "Workspace failure: {Diagnostic} ({Kind})",
                    args.Diagnostic.Message,
                    args.Diagnostic.Kind);
            };

            if (isSolution)
            {
                _logger.LogDebug("Opening solution: {Path}", path);
                await workspace.OpenSolutionAsync(path, cancellationToken: cts.Token);
            }
            else
            {
                _logger.LogDebug("Opening project: {Path}", path);
                await workspace.OpenProjectAsync(path, cancellationToken: cts.Token);
            }

            // Log any workspace diagnostics
            if (workspace.Diagnostics.Any())
            {
                _logger.LogWarning("Workspace loaded with {Count} diagnostics", workspace.Diagnostics.Count());
                foreach (var diagnostic in workspace.Diagnostics.Take(5))
                {
                    _logger.LogDebug("Workspace diagnostic: {Message}", diagnostic.Message);
                }
            }

            // Log project information
            LogWorkspaceInfo(workspace);

            return new WorkspaceEntry(workspace, path, isSolution);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Workspace load timeout after {Timeout}s for {Path}", timeout.TotalSeconds, path);
            throw new TimeoutException($"Workspace load timed out after {timeout.TotalSeconds}s");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workspace for {Path}", path);
            throw;
        }
    }

    private void LogWorkspaceInfo(Workspace workspace)
    {
        var solution = workspace.CurrentSolution;
        var projectCount = solution.ProjectIds.Count;

        _logger.LogInformation(
            "Workspace loaded successfully: {ProjectCount} project(s)",
            projectCount);

        foreach (var project in solution.Projects.Take(10))
        {
            var targetFrameworks = project.CompilationOptions?.Platform.ToString() ?? "Unknown";
            var hasMultipleTargets = project.OutputFilePath?.Contains(';') ?? false;

            _logger.LogDebug(
                "  Project: {Name} (Framework: {Framework}, MultiTarget: {MultiTarget})",
                project.Name,
                targetFrameworks,
                hasMultipleTargets);
        }

        if (projectCount > 10)
        {
            _logger.LogDebug("  ... and {More} more projects", projectCount - 10);
        }
    }

    private async Task EnsureCacheCapacityAsync()
    {
        while (_workspaceCache.Count >= _config.WorkspaceCache.MaxCachedWorkspaces)
        {
            // Find the least recently used workspace
            var lruEntry = _workspaceCache.Values
                .OrderBy(e => e.LastAccessedAt)
                .FirstOrDefault();

            if (lruEntry != null)
            {
                _logger.LogInformation(
                    "Cache capacity reached, evicting LRU workspace: {Path} (idle: {Idle})",
                    lruEntry.Path,
                    lruEntry.IdleTime);

                await RemoveWorkspaceAsync(lruEntry.Path);
            }
            else
            {
                break;
            }
        }
    }

    private void CleanupIdleWorkspaces(object? state)
    {
        if (_disposed)
            return;

        try
        {
            var idleTimeout = TimeSpan.FromMinutes(_config.WorkspaceCache.IdleTimeoutMinutes);
            var idleWorkspaces = _workspaceCache.Values
                .Where(e => e.IdleTime > idleTimeout)
                .ToList();

            if (idleWorkspaces.Any())
            {
                _logger.LogInformation(
                    "Cleaning up {Count} idle workspaces (idle timeout: {Timeout}min)",
                    idleWorkspaces.Count,
                    _config.WorkspaceCache.IdleTimeoutMinutes);

                foreach (var workspace in idleWorkspaces)
                {
                    _ = RemoveWorkspaceAsync(workspace.Path);
                }
            }

            // Check memory pressure
            var currentMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
            if (currentMemoryMB > _config.WorkspaceCache.MaxMemoryMB)
            {
                _logger.LogWarning(
                    "Memory usage ({Current:F0}MB) exceeds limit ({Max}MB), forcing cache cleanup",
                    currentMemoryMB,
                    _config.WorkspaceCache.MaxMemoryMB);

                // Evict half the cache, starting with oldest
                var toEvict = _workspaceCache.Values
                    .OrderBy(e => e.LastAccessedAt)
                    .Take(_workspaceCache.Count / 2)
                    .ToList();

                foreach (var workspace in toEvict)
                {
                    _ = RemoveWorkspaceAsync(workspace.Path);
                }

                // Force GC collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during workspace cleanup");
        }
    }

    private void OnWorkspaceFileChanged(object? sender, WorkspaceFileChangedEventArgs e)
    {
        // Find which workspace(s) contain this file
        var affectedWorkspaces = _workspaceCache.Keys
            .Where(wsPath =>
            {
                var wsDir = Path.GetDirectoryName(wsPath);
                return !string.IsNullOrEmpty(wsDir) &&
                       e.FilePath.StartsWith(wsDir, StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        if (!affectedWorkspaces.Any())
            return;

        _logger.LogDebug(
            "File change detected: {Path} ({ChangeType}), affects {Count} workspace(s)",
            e.FilePath,
            e.ChangeType,
            affectedWorkspaces.Count);

        // Debounce: add to pending reloads instead of immediately reloading
        lock (_pendingReloads)
        {
            foreach (var wsPath in affectedWorkspaces)
            {
                _pendingReloads[wsPath] = DateTime.UtcNow;
            }
        }
    }

    private void ProcessPendingReloads(object? state)
    {
        if (_disposed)
            return;

        List<string> toReload;
        lock (_pendingReloads)
        {
            // Reload workspaces that haven't had changes in the last 2 seconds (debounce)
            var cutoff = DateTime.UtcNow.AddSeconds(-2);
            toReload = _pendingReloads
                .Where(kvp => kvp.Value < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var path in toReload)
            {
                _pendingReloads.Remove(path);
            }
        }

        foreach (var path in toReload)
        {
            _ = ReloadWorkspaceAsync(path);
        }
    }

    private async Task ReloadWorkspaceAsync(string path)
    {
        try
        {
            _logger.LogInformation("Reloading workspace due to file changes: {Path}", path);

            // Remove old workspace
            await RemoveWorkspaceAsync(path);

            // It will be reloaded on next access
            _logger.LogDebug("Workspace {Path} removed from cache, will reload on next access", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading workspace {Path}", path);
        }
    }

    private void EnsureMSBuildRegistered()
    {
        if (_msbuildRegistered)
            return;

        lock (_msbuildLock)
        {
            if (_msbuildRegistered)
                return;

            try
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
                if (instances.Any())
                {
                    var instance = instances.OrderByDescending(i => i.Version).First();
                    MSBuildLocator.RegisterInstance(instance);
                    _logger.LogInformation(
                        "Registered MSBuild instance: {Name} {Version} at {Path}",
                        instance.Name,
                        instance.Version,
                        instance.MSBuildPath);
                }
                else
                {
                    MSBuildLocator.RegisterDefaults();
                    _logger.LogInformation("Registered default MSBuild instance");
                }

                _msbuildRegistered = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register MSBuild instance");
                throw;
            }
        }
    }

    /// <summary>
    /// Disposes the workspace manager and all cached workspaces.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing workspace manager");

        _disposed = true;
        _cleanupTimer?.Dispose();
        _reloadDebounceTimer?.Dispose();
        _fileWatcher?.Dispose();

        var clearTask = ClearCacheAsync();
        clearTask.Wait(TimeSpan.FromSeconds(10));

        _cacheAccessLock.Dispose();

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Statistics about the workspace cache.
/// </summary>
public class WorkspaceCacheStats
{
    /// <summary>
    /// Gets or sets the total number of cached workspaces.
    /// </summary>
    public int TotalWorkspaces { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed workspaces.
    /// </summary>
    public int MaxWorkspaces { get; set; }

    /// <summary>
    /// Gets or sets whether caching is enabled.
    /// </summary>
    public bool CacheEnabled { get; set; }

    /// <summary>
    /// Gets or sets workspaces grouped by state.
    /// </summary>
    public Dictionary<string, int> WorkspacesByState { get; set; } = new();

    /// <summary>
    /// Gets or sets the current memory usage in MB.
    /// </summary>
    public double TotalMemoryMB { get; set; }

    /// <summary>
    /// Gets or sets the maximum memory limit in MB.
    /// </summary>
    public double MaxMemoryMB { get; set; }
}
