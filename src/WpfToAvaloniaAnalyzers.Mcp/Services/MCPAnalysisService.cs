using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using WpfToAvaloniaAnalyzers.Mcp.Configuration;

namespace WpfToAvaloniaAnalyzers.Mcp.Services;

/// <summary>
/// Service for running WPF to Avalonia analyzers on projects and files.
/// </summary>
public class MCPAnalysisService
{
    private readonly MCPWorkspaceManager _workspaceManager;
    private readonly McpServerConfiguration _config;
    private readonly ILogger<MCPAnalysisService> _logger;
    private readonly Lazy<ImmutableArray<DiagnosticAnalyzer>> _analyzers;

    /// <summary>
    /// Initializes a new instance of the <see cref="MCPAnalysisService"/> class.
    /// </summary>
    public MCPAnalysisService(
        MCPWorkspaceManager workspaceManager,
        McpServerConfiguration config,
        ILogger<MCPAnalysisService> logger)
    {
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _analyzers = new Lazy<ImmutableArray<DiagnosticAnalyzer>>(LoadAnalyzers);
    }

    /// <summary>
    /// Analyzes a project or solution for WPF to Avalonia conversion opportunities.
    /// </summary>
    public async Task<AnalysisResult> AnalyzeProjectAsync(
        string projectPath,
        string[]? diagnosticIds = null,
        DiagnosticSeverity? minimumSeverity = null,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting analysis of {Path}", projectPath);

        var timeout = TimeSpan.FromSeconds(_config.Timeouts.AnalysisTimeoutSeconds);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            // Get or load workspace
            var workspaceEntry = await _workspaceManager.GetOrOpenWorkspaceAsync(projectPath, cts.Token);

            await workspaceEntry.Lock.WaitAsync(cts.Token);
            try
            {
                workspaceEntry.State = WorkspaceState.Analyzing;

                var solution = workspaceEntry.Workspace.CurrentSolution;
                var projects = solution.Projects.ToList();

                _logger.LogDebug("Analyzing {Count} project(s)", projects.Count);

                var allDiagnostics = new List<DiagnosticInfo>();
                var analyzedProjects = 0;

                foreach (var project in projects)
                {
                    var compilation = await project.GetCompilationAsync(cts.Token);
                    if (compilation == null)
                    {
                        _logger.LogWarning("Could not get compilation for project {Name}", project.Name);
                        continue;
                    }

                    // Filter analyzers if diagnostic IDs specified
                    var analyzersToRun = diagnosticIds != null && diagnosticIds.Length > 0
                        ? FilterAnalyzersByDiagnosticIds(diagnosticIds)
                        : _analyzers.Value;

                    var compilationWithAnalyzers = compilation.WithAnalyzers(
                        analyzersToRun,
                        options: null);

                    var diagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync(cts.Token);

                    // Filter by severity if specified
                    var filteredDiagnostics = minimumSeverity.HasValue
                        ? diagnostics.Where(d => d.Severity >= minimumSeverity.Value)
                        : diagnostics;

                    allDiagnostics.AddRange(filteredDiagnostics.Select(d => DiagnosticInfo.FromDiagnostic(d, project.Name)));

                    analyzedProjects++;
                    progress?.Report(new AnalysisProgress
                    {
                        ProjectsAnalyzed = analyzedProjects,
                        TotalProjects = projects.Count,
                        CurrentProject = project.Name,
                        DiagnosticsFound = allDiagnostics.Count
                    });
                }

                workspaceEntry.State = WorkspaceState.Ready;

                return new AnalysisResult
                {
                    Success = true,
                    Diagnostics = allDiagnostics,
                    Summary = CreateSummary(allDiagnostics),
                    ProjectInfo = CreateProjectInfo(solution)
                };
            }
            finally
            {
                workspaceEntry.Lock.Release();
            }
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Analysis timeout after {Timeout}s for {Path}", timeout.TotalSeconds, projectPath);
            throw new TimeoutException($"Analysis timed out after {timeout.TotalSeconds}s");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing project {Path}", projectPath);
            throw;
        }
    }

    /// <summary>
    /// Gets metadata about all available analyzers.
    /// </summary>
    public IEnumerable<AnalyzerMetadata> GetAnalyzerMetadata()
    {
        return _analyzers.Value.SelectMany(analyzer =>
            analyzer.SupportedDiagnostics.Select(descriptor =>
                new AnalyzerMetadata
                {
                    Id = descriptor.Id,
                    Title = descriptor.Title.ToString(),
                    Category = descriptor.Category,
                    DefaultSeverity = descriptor.DefaultSeverity.ToString(),
                    Description = descriptor.Description.ToString(),
                    HelpLinkUri = descriptor.HelpLinkUri
                }));
    }

    private static ImmutableArray<DiagnosticAnalyzer> LoadAnalyzers()
    {
        var assembly = typeof(BaseClassAnalyzer).Assembly;
        var analyzers = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
            .ToImmutableArray();

        return analyzers;
    }

    private ImmutableArray<DiagnosticAnalyzer> FilterAnalyzersByDiagnosticIds(string[] diagnosticIds)
    {
        var idSet = new HashSet<string>(diagnosticIds, StringComparer.OrdinalIgnoreCase);
        return _analyzers.Value
            .Where(a => a.SupportedDiagnostics.Any(d => idSet.Contains(d.Id)))
            .ToImmutableArray();
    }

    private static AnalysisSummary CreateSummary(List<DiagnosticInfo> diagnostics)
    {
        return new AnalysisSummary
        {
            TotalDiagnostics = diagnostics.Count,
            BySeverity = diagnostics
                .GroupBy(d => d.Severity)
                .ToDictionary(g => g.Key, g => g.Count()),
            ByDiagnosticId = diagnostics
                .GroupBy(d => d.Id)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    private static ProjectInfo CreateProjectInfo(Solution solution)
    {
        var projects = solution.Projects.ToList();
        return new ProjectInfo
        {
            Name = Path.GetFileNameWithoutExtension(solution.FilePath) ?? "Unknown",
            ProjectCount = projects.Count,
            Projects = projects.Select(p => new ProjectDetails
            {
                Name = p.Name,
                Language = p.Language,
                DocumentCount = p.Documents.Count()
            }).ToList()
        };
    }
}

/// <summary>
/// Result of project analysis.
/// </summary>
public class AnalysisResult
{
    public bool Success { get; set; }
    public List<DiagnosticInfo> Diagnostics { get; set; } = new();
    public AnalysisSummary Summary { get; set; } = new();
    public ProjectInfo ProjectInfo { get; set; } = new();
}

/// <summary>
/// Information about a diagnostic.
/// </summary>
public class DiagnosticInfo
{
    public string Id { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public int Column { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    public static DiagnosticInfo FromDiagnostic(Diagnostic diagnostic, string projectName)
    {
        var lineSpan = diagnostic.Location.GetLineSpan();
        return new DiagnosticInfo
        {
            Id = diagnostic.Id,
            Severity = diagnostic.Severity.ToString(),
            Message = diagnostic.GetMessage(),
            FilePath = lineSpan.Path ?? string.Empty,
            LineNumber = lineSpan.StartLinePosition.Line + 1,
            Column = lineSpan.StartLinePosition.Character + 1,
            ProjectName = projectName
        };
    }
}

/// <summary>
/// Summary of analysis results.
/// </summary>
public class AnalysisSummary
{
    public int TotalDiagnostics { get; set; }
    public Dictionary<string, int> BySeverity { get; set; } = new();
    public Dictionary<string, int> ByDiagnosticId { get; set; } = new();
}

/// <summary>
/// Information about the analyzed project/solution.
/// </summary>
public class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public int ProjectCount { get; set; }
    public List<ProjectDetails> Projects { get; set; } = new();
}

/// <summary>
/// Details about a single project.
/// </summary>
public class ProjectDetails
{
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
}

/// <summary>
/// Progress information for analysis.
/// </summary>
public class AnalysisProgress
{
    public int ProjectsAnalyzed { get; set; }
    public int TotalProjects { get; set; }
    public string CurrentProject { get; set; } = string.Empty;
    public int DiagnosticsFound { get; set; }
}

/// <summary>
/// Metadata about an analyzer.
/// </summary>
public class AnalyzerMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DefaultSeverity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HelpLinkUri { get; set; }
}
