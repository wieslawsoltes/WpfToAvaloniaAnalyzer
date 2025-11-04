using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.Extensions.Logging;
using WpfToAvaloniaAnalyzers.Mcp.Configuration;

namespace WpfToAvaloniaAnalyzers.Mcp.Services;

/// <summary>
/// Service for applying code fixes to WPF code for Avalonia conversion.
/// </summary>
public class MCPCodeFixService
{
    private readonly MCPWorkspaceManager _workspaceManager;
    private readonly McpServerConfiguration _config;
    private readonly ILogger<MCPCodeFixService> _logger;
    private readonly Lazy<ImmutableArray<CodeFixProvider>> _codeFixProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="MCPCodeFixService"/> class.
    /// </summary>
    public MCPCodeFixService(
        MCPWorkspaceManager workspaceManager,
        McpServerConfiguration config,
        ILogger<MCPCodeFixService> logger)
    {
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _codeFixProviders = new Lazy<ImmutableArray<CodeFixProvider>>(LoadCodeFixProviders);
    }

    /// <summary>
    /// Applies a code fix to a specific diagnostic in a file.
    /// </summary>
    public async Task<CodeFixResult> ApplyFixAsync(
        string projectPath,
        string filePath,
        string diagnosticId,
        int line,
        int column,
        int? fixIndex = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Applying fix {DiagnosticId} to {FilePath}:{Line}:{Column}",
            diagnosticId, filePath, line, column);

        var timeout = TimeSpan.FromSeconds(_config.Timeouts.CodeFixTimeoutSeconds);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            // Get or load workspace
            var workspaceEntry = await _workspaceManager.GetOrOpenWorkspaceAsync(projectPath, cts.Token);

            await workspaceEntry.Lock.WaitAsync(cts.Token);
            try
            {
                workspaceEntry.State = WorkspaceState.Modifying;

                var solution = workspaceEntry.Workspace.CurrentSolution;
                var document = solution.GetDocumentByFilePath(filePath);

                if (document == null)
                {
                    return new CodeFixResult
                    {
                        Success = false,
                        Error = $"Document not found in workspace: {filePath}"
                    };
                }

                // Get the diagnostic at the specified location
                var semanticModel = await document.GetSemanticModelAsync(cts.Token);
                var syntaxTree = await document.GetSyntaxTreeAsync(cts.Token);
                if (semanticModel == null || syntaxTree == null)
                {
                    return new CodeFixResult
                    {
                        Success = false,
                        Error = "Could not get semantic model or syntax tree"
                    };
                }

                // Convert line/column to position
                var linePosition = new Microsoft.CodeAnalysis.Text.LinePosition(line - 1, column - 1);
                var position = syntaxTree.GetText(cts.Token).Lines.GetPosition(linePosition);
                var span = new Microsoft.CodeAnalysis.Text.TextSpan(position, 0);

                // Find applicable code fix providers
                var providers = _codeFixProviders.Value
                    .Where(p => p.FixableDiagnosticIds.Contains(diagnosticId))
                    .ToList();

                if (providers.Count == 0)
                {
                    return new CodeFixResult
                    {
                        Success = false,
                        Error = $"No code fix provider found for diagnostic {diagnosticId}"
                    };
                }

                // Get diagnostics at location
                var compilation = await document.Project.GetCompilationAsync(cts.Token);
                if (compilation == null)
                {
                    return new CodeFixResult
                    {
                        Success = false,
                        Error = "Could not get compilation"
                    };
                }

                var diagnostics = semanticModel.GetDiagnostics(span, cts.Token)
                    .Where(d => d.Id == diagnosticId)
                    .ToList();

                if (diagnostics.Count == 0)
                {
                    return new CodeFixResult
                    {
                        Success = false,
                        Error = $"No diagnostic {diagnosticId} found at {line}:{column}"
                    };
                }

                var diagnostic = diagnostics.First();

                // Get available fixes
                var codeActions = new List<CodeAction>();
                foreach (var provider in providers)
                {
                    var context = new CodeFixContext(
                        document,
                        diagnostic,
                        (action, _) => codeActions.Add(action),
                        cts.Token);

                    await provider.RegisterCodeFixesAsync(context);
                }

                if (codeActions.Count == 0)
                {
                    return new CodeFixResult
                    {
                        Success = false,
                        Error = "No code fixes available for this diagnostic"
                    };
                }

                // Select the fix to apply
                var selectedAction = fixIndex.HasValue && fixIndex.Value < codeActions.Count
                    ? codeActions[fixIndex.Value]
                    : codeActions.First();

                // Apply the fix
                var operations = await selectedAction.GetOperationsAsync(cts.Token);
                var changedSolution = solution;
                var modifiedFiles = new List<string>();

                foreach (var operation in operations)
                {
                    if (operation is ApplyChangesOperation applyChangesOp)
                    {
                        changedSolution = applyChangesOp.ChangedSolution;
                    }
                }

                // Collect modified documents
                var changes = changedSolution.GetChanges(solution);
                foreach (var projectChanges in changes.GetProjectChanges())
                {
                    foreach (var changedDocId in projectChanges.GetChangedDocuments())
                    {
                        var changedDoc = changedSolution.GetDocument(changedDocId);
                        if (changedDoc?.FilePath != null)
                        {
                            modifiedFiles.Add(changedDoc.FilePath);
                        }
                    }

                    foreach (var addedDocId in projectChanges.GetAddedDocuments())
                    {
                        var addedDoc = changedSolution.GetDocument(addedDocId);
                        if (addedDoc?.FilePath != null)
                        {
                            modifiedFiles.Add(addedDoc.FilePath);
                        }
                    }
                }

                // Apply changes to workspace
                if (!workspaceEntry.Workspace.TryApplyChanges(changedSolution))
                {
                    return new CodeFixResult
                    {
                        Success = false,
                        Error = "Failed to apply changes to workspace"
                    };
                }

                // Generate diffs
                var diffs = new List<FileDiff>();
                foreach (var modifiedFilePath in modifiedFiles.Distinct())
                {
                    var oldDoc = solution.GetDocumentByFilePath(modifiedFilePath);
                    var newDoc = changedSolution.GetDocumentByFilePath(modifiedFilePath);

                    if (oldDoc != null && newDoc != null)
                    {
                        var oldText = await oldDoc.GetTextAsync(cts.Token);
                        var newText = await newDoc.GetTextAsync(cts.Token);
                        var diff = GenerateUnifiedDiff(modifiedFilePath, oldText.ToString(), newText.ToString());
                        diffs.Add(diff);
                    }
                }

                workspaceEntry.State = WorkspaceState.Ready;

                return new CodeFixResult
                {
                    Success = true,
                    AppliedFixTitle = selectedAction.Title,
                    ModifiedFiles = modifiedFiles.Distinct().ToList(),
                    AvailableFixes = codeActions.Select((a, i) => new AvailableFixInfo
                    {
                        Index = i,
                        Title = a.Title
                    }).ToList(),
                    Diffs = diffs
                };
            }
            finally
            {
                workspaceEntry.Lock.Release();
            }
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Code fix timeout after {Timeout}s", timeout.TotalSeconds);
            throw new TimeoutException($"Code fix timed out after {timeout.TotalSeconds}s");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying code fix");
            throw;
        }
    }

    /// <summary>
    /// Applies batch fixes to multiple diagnostics in a project.
    /// </summary>
    public async Task<BatchFixResult> ApplyBatchFixesAsync(
        string projectPath,
        string[]? diagnosticIds = null,
        string[]? targetFiles = null,
        bool dryRun = false,
        IProgress<BatchFixProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting batch fix for {ProjectPath}", projectPath);

        var timeout = TimeSpan.FromSeconds(_config.Timeouts.CodeFixTimeoutSeconds * 10); // 10x for batch
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            var workspaceEntry = await _workspaceManager.GetOrOpenWorkspaceAsync(projectPath, cts.Token);

            await workspaceEntry.Lock.WaitAsync(cts.Token);
            try
            {
                workspaceEntry.State = WorkspaceState.Modifying;

                var solution = workspaceEntry.Workspace.CurrentSolution;
                var projects = solution.Projects.ToList();

                var totalFixed = 0;
                var modifiedFiles = new List<string>();
                var failedFixes = new List<FailedFixInfo>();
                var summaryByDiagnosticId = new Dictionary<string, int>();

                foreach (var project in projects)
                {
                    var compilation = await project.GetCompilationAsync(cts.Token);
                    if (compilation == null) continue;

                    var documents = project.Documents;

                    // Filter by target files if specified
                    if (targetFiles != null && targetFiles.Length > 0)
                    {
                        var targetFileSet = new HashSet<string>(targetFiles, StringComparer.OrdinalIgnoreCase);
                        documents = documents.Where(d => d.FilePath != null && targetFileSet.Contains(d.FilePath));
                    }

                    foreach (var document in documents)
                    {
                        var semanticModel = await document.GetSemanticModelAsync(cts.Token);
                        if (semanticModel == null) continue;

                        var diagnostics = semanticModel.GetDiagnostics(cancellationToken: cts.Token)
                            .Where(d => diagnosticIds == null || diagnosticIds.Length == 0 || diagnosticIds.Contains(d.Id))
                            .ToList();

                        foreach (var diagnostic in diagnostics)
                        {
                            try
                            {
                                // Find applicable providers
                                var providers = _codeFixProviders.Value
                                    .Where(p => p.FixableDiagnosticIds.Contains(diagnostic.Id))
                                    .ToList();

                                if (providers.Count == 0) continue;

                                // Get available fixes
                                var codeActions = new List<CodeAction>();
                                foreach (var provider in providers)
                                {
                                    var context = new CodeFixContext(
                                        document,
                                        diagnostic,
                                        (action, _) => codeActions.Add(action),
                                        cts.Token);

                                    await provider.RegisterCodeFixesAsync(context);
                                }

                                if (codeActions.Count == 0) continue;

                                if (!dryRun)
                                {
                                    // Apply first fix
                                    var action = codeActions.First();
                                    var operations = await action.GetOperationsAsync(cts.Token);

                                    foreach (var operation in operations)
                                    {
                                        if (operation is ApplyChangesOperation applyChangesOp)
                                        {
                                            solution = applyChangesOp.ChangedSolution;
                                        }
                                    }
                                }

                                totalFixed++;
                                summaryByDiagnosticId[diagnostic.Id] =
                                    summaryByDiagnosticId.GetValueOrDefault(diagnostic.Id, 0) + 1;

                                if (document.FilePath != null && !modifiedFiles.Contains(document.FilePath))
                                {
                                    modifiedFiles.Add(document.FilePath);
                                }
                            }
                            catch (Exception ex)
                            {
                                failedFixes.Add(new FailedFixInfo
                                {
                                    DiagnosticId = diagnostic.Id,
                                    FilePath = document.FilePath ?? "unknown",
                                    Line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                                    Error = ex.Message
                                });
                            }
                        }

                        progress?.Report(new BatchFixProgress
                        {
                            TotalFixed = totalFixed,
                            CurrentFile = document.FilePath ?? "unknown"
                        });
                    }
                }

                // Apply changes if not dry run
                if (!dryRun && !workspaceEntry.Workspace.TryApplyChanges(solution))
                {
                    return new BatchFixResult
                    {
                        Success = false,
                        Error = "Failed to apply batch changes to workspace"
                    };
                }

                workspaceEntry.State = WorkspaceState.Ready;

                return new BatchFixResult
                {
                    Success = true,
                    FixedDiagnostics = totalFixed,
                    ModifiedFiles = modifiedFiles.Distinct().ToList(),
                    FailedFixes = failedFixes,
                    SummaryByDiagnosticId = summaryByDiagnosticId
                };
            }
            finally
            {
                workspaceEntry.Lock.Release();
            }
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Batch fix timeout after {Timeout}s", timeout.TotalSeconds);
            throw new TimeoutException($"Batch fix timed out after {timeout.TotalSeconds}s");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch fix");
            throw;
        }
    }

    /// <summary>
    /// Gets available code fixes for a diagnostic at a specific location.
    /// </summary>
    public async Task<AvailableFixesResult> GetAvailableFixesAsync(
        string projectPath,
        string filePath,
        string diagnosticId,
        int line,
        int column,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workspaceEntry = await _workspaceManager.GetOrOpenWorkspaceAsync(projectPath, cancellationToken);

            var solution = workspaceEntry.Workspace.CurrentSolution;
            var document = solution.GetDocumentByFilePath(filePath);

            if (document == null)
            {
                return new AvailableFixesResult
                {
                    Success = false,
                    Error = $"Document not found: {filePath}"
                };
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
            if (semanticModel == null || syntaxTree == null)
            {
                return new AvailableFixesResult { Success = false, Error = "Could not get semantic model" };
            }

            var linePosition = new Microsoft.CodeAnalysis.Text.LinePosition(line - 1, column - 1);
            var position = syntaxTree.GetText(cancellationToken).Lines.GetPosition(linePosition);
            var span = new Microsoft.CodeAnalysis.Text.TextSpan(position, 0);

            var diagnostics = semanticModel.GetDiagnostics(span, cancellationToken)
                .Where(d => d.Id == diagnosticId)
                .ToList();

            if (diagnostics.Count == 0)
            {
                return new AvailableFixesResult
                {
                    Success = false,
                    Error = $"No diagnostic {diagnosticId} found at location"
                };
            }

            var diagnostic = diagnostics.First();
            var providers = _codeFixProviders.Value
                .Where(p => p.FixableDiagnosticIds.Contains(diagnosticId))
                .ToList();

            var codeActions = new List<CodeAction>();
            foreach (var provider in providers)
            {
                var context = new CodeFixContext(
                    document,
                    diagnostic,
                    (action, _) => codeActions.Add(action),
                    cancellationToken);

                await provider.RegisterCodeFixesAsync(context);
            }

            return new AvailableFixesResult
            {
                Success = true,
                Fixes = codeActions.Select((a, i) => new AvailableFixInfo
                {
                    Index = i,
                    Title = a.Title
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available fixes");
            return new AvailableFixesResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private static ImmutableArray<CodeFixProvider> LoadCodeFixProviders()
    {
        var assembly = typeof(WpfToAvaloniaAnalyzers.CodeFixes.DependencyPropertyCodeFixProvider).Assembly;
        var providers = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(CodeFixProvider).IsAssignableFrom(t))
            .Select(t => (CodeFixProvider)Activator.CreateInstance(t)!)
            .ToImmutableArray();

        return providers;
    }

    private static FileDiff GenerateUnifiedDiff(string filePath, string oldText, string newText)
    {
        // Simple line-based diff
        var oldLines = oldText.Split('\n');
        var newLines = newText.Split('\n');

        var diffLines = new List<string>();
        diffLines.Add($"--- a/{filePath}");
        diffLines.Add($"+++ b/{filePath}");

        // Very simple diff - just show changed sections
        for (int i = 0; i < Math.Max(oldLines.Length, newLines.Length); i++)
        {
            var oldLine = i < oldLines.Length ? oldLines[i] : "";
            var newLine = i < newLines.Length ? newLines[i] : "";

            if (oldLine != newLine)
            {
                if (!string.IsNullOrEmpty(oldLine))
                    diffLines.Add($"-{oldLine}");
                if (!string.IsNullOrEmpty(newLine))
                    diffLines.Add($"+{newLine}");
            }
        }

        return new FileDiff
        {
            FilePath = filePath,
            UnifiedDiff = string.Join("\n", diffLines),
            LinesAdded = newLines.Length - oldLines.Length,
            LinesRemoved = oldLines.Length > newLines.Length ? oldLines.Length - newLines.Length : 0
        };
    }
}

/// <summary>
/// Result of applying a code fix.
/// </summary>
public class CodeFixResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string AppliedFixTitle { get; set; } = string.Empty;
    public List<string> ModifiedFiles { get; set; } = new();
    public List<AvailableFixInfo> AvailableFixes { get; set; } = new();
    public List<FileDiff> Diffs { get; set; } = new();
}

/// <summary>
/// Result of batch fix operation.
/// </summary>
public class BatchFixResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int FixedDiagnostics { get; set; }
    public List<string> ModifiedFiles { get; set; } = new();
    public List<FailedFixInfo> FailedFixes { get; set; } = new();
    public Dictionary<string, int> SummaryByDiagnosticId { get; set; } = new();
}

/// <summary>
/// Information about an available fix.
/// </summary>
public class AvailableFixInfo
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Result of getting available fixes.
/// </summary>
public class AvailableFixesResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<AvailableFixInfo> Fixes { get; set; } = new();
}

/// <summary>
/// Information about a failed fix.
/// </summary>
public class FailedFixInfo
{
    public string DiagnosticId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// File diff information.
/// </summary>
public class FileDiff
{
    public string FilePath { get; set; } = string.Empty;
    public string UnifiedDiff { get; set; } = string.Empty;
    public int LinesAdded { get; set; }
    public int LinesRemoved { get; set; }
}

/// <summary>
/// Progress information for batch fixes.
/// </summary>
public class BatchFixProgress
{
    public int TotalFixed { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
}

/// <summary>
/// Extension methods for Solution.
/// </summary>
internal static class SolutionExtensions
{
    public static Document? GetDocumentByFilePath(this Solution solution, string filePath)
    {
        return solution.Projects
            .SelectMany(p => p.Documents)
            .FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
    }
}
