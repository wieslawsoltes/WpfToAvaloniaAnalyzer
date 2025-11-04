using System.ComponentModel;
using Microsoft.CodeAnalysis;
using ModelContextProtocol.Server;
using WpfToAvaloniaAnalyzers.Mcp.Services;

namespace WpfToAvaloniaAnalyzers.Mcp.Tools;

/// <summary>
/// MCP tools for analyzing WPF code for Avalonia conversion opportunities.
/// </summary>
[McpServerToolType]
public static class AnalysisTools
{
    /// <summary>
    /// Analyzes a project or solution for WPF to Avalonia conversion opportunities.
    /// </summary>
    [McpServerTool]
    [Description("Analyze a WPF project or solution for Avalonia conversion opportunities. Returns diagnostics with line numbers, severity, and suggested fixes.")]
    public static async Task<AnalysisToolResult> AnalyzeProject(
        MCPAnalysisService analysisService,
        [Description("Path to .csproj, .sln, or directory containing project")] string projectPath,
        [Description("Optional array of specific diagnostic IDs to check (e.g., ['WPFAV001', 'WPFAV002'])")] string[]? diagnosticIds = null,
        [Description("Optional minimum severity filter: 'Hidden', 'Info', 'Warning', or 'Error'")] string? minimumSeverity = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate project path
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                return new AnalysisToolResult
                {
                    Success = false,
                    Error = "Project path is required"
                };
            }

            if (!File.Exists(projectPath) && !Directory.Exists(projectPath))
            {
                return new AnalysisToolResult
                {
                    Success = false,
                    Error = $"Project path not found: {projectPath}"
                };
            }

            // Parse minimum severity if provided
            DiagnosticSeverity? minSeverity = null;
            if (!string.IsNullOrWhiteSpace(minimumSeverity))
            {
                if (Enum.TryParse<DiagnosticSeverity>(minimumSeverity, ignoreCase: true, out var severity))
                {
                    minSeverity = severity;
                }
                else
                {
                    return new AnalysisToolResult
                    {
                        Success = false,
                        Error = $"Invalid severity level: {minimumSeverity}. Valid values: Hidden, Info, Warning, Error"
                    };
                }
            }

            // Run analysis with progress reporting
            var progress = new Progress<AnalysisProgress>(p =>
            {
                // Progress is logged internally by the service
            });

            var result = await analysisService.AnalyzeProjectAsync(
                projectPath,
                diagnosticIds,
                minSeverity,
                progress,
                cancellationToken);

            return new AnalysisToolResult
            {
                Success = result.Success,
                ProjectName = result.ProjectInfo.Name,
                ProjectCount = result.ProjectInfo.ProjectCount,
                Projects = result.ProjectInfo.Projects.Select(p => new ProjectSummary
                {
                    Name = p.Name,
                    Language = p.Language,
                    DocumentCount = p.DocumentCount
                }).ToList(),
                TotalDiagnostics = result.Summary.TotalDiagnostics,
                DiagnosticsBySeverity = result.Summary.BySeverity,
                DiagnosticsById = result.Summary.ByDiagnosticId,
                Diagnostics = result.Diagnostics.Select(d => new DiagnosticResult
                {
                    Id = d.Id,
                    Severity = d.Severity,
                    Message = d.Message,
                    FilePath = d.FilePath,
                    LineNumber = d.LineNumber,
                    Column = d.Column,
                    ProjectName = d.ProjectName
                }).ToList()
            };
        }
        catch (TimeoutException ex)
        {
            return new AnalysisToolResult
            {
                Success = false,
                Error = $"Analysis timeout: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new AnalysisToolResult
            {
                Success = false,
                Error = $"Analysis failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Analyzes a single C# file for WPF to Avalonia conversion opportunities.
    /// </summary>
    [McpServerTool]
    [Description("Analyze a single C# file for WPF to Avalonia conversion opportunities. Can analyze files with or without project context.")]
    public static async Task<FileAnalysisResult> AnalyzeFile(
        MCPAnalysisService analysisService,
        [Description("Path to the C# file to analyze")] string filePath,
        [Description("Optional path to .csproj or .sln for full semantic analysis")] string? projectPath = null,
        [Description("Optional array of specific diagnostic IDs to check")] string[]? diagnosticIds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file path
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new FileAnalysisResult
                {
                    Success = false,
                    Error = "File path is required"
                };
            }

            if (!File.Exists(filePath))
            {
                return new FileAnalysisResult
                {
                    Success = false,
                    Error = $"File not found: {filePath}"
                };
            }

            if (!filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return new FileAnalysisResult
                {
                    Success = false,
                    Error = "Only C# (.cs) files are supported"
                };
            }

            // If project context provided, analyze within project
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                var projectResult = await analysisService.AnalyzeProjectAsync(
                    projectPath,
                    diagnosticIds,
                    null,
                    null,
                    cancellationToken);

                // Filter diagnostics to only this file
                var fileDiagnostics = projectResult.Diagnostics
                    .Where(d => Path.GetFullPath(d.FilePath) == Path.GetFullPath(filePath))
                    .ToList();

                return new FileAnalysisResult
                {
                    Success = true,
                    FilePath = filePath,
                    HasProjectContext = true,
                    ProjectPath = projectPath,
                    TotalDiagnostics = fileDiagnostics.Count,
                    DiagnosticsById = fileDiagnostics
                        .GroupBy(d => d.Id)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    Diagnostics = fileDiagnostics.Select(d => new DiagnosticResult
                    {
                        Id = d.Id,
                        Severity = d.Severity,
                        Message = d.Message,
                        FilePath = d.FilePath,
                        LineNumber = d.LineNumber,
                        Column = d.Column,
                        ProjectName = d.ProjectName
                    }).ToList()
                };
            }

            // Standalone file analysis without project context
            // This provides limited analysis based on syntax only
            var fileContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(fileContent, path: filePath);

            return new FileAnalysisResult
            {
                Success = true,
                FilePath = filePath,
                HasProjectContext = false,
                TotalDiagnostics = 0,
                Diagnostics = new List<DiagnosticResult>(),
                Message = "Standalone file analysis (syntax-only) is limited. Provide projectPath for full semantic analysis."
            };
        }
        catch (Exception ex)
        {
            return new FileAnalysisResult
            {
                Success = false,
                Error = $"File analysis failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Lists all available WPF to Avalonia analyzers with their metadata.
    /// </summary>
    [McpServerTool]
    [Description("Get information about all available WPF to Avalonia analyzers, including diagnostic IDs, descriptions, and categories.")]
    public static AnalyzersListResult ListAnalyzers(MCPAnalysisService analysisService)
    {
        try
        {
            var metadata = analysisService.GetAnalyzerMetadata().ToList();

            return new AnalyzersListResult
            {
                Success = true,
                TotalAnalyzers = metadata.Count,
                Analyzers = metadata.Select(m => new AnalyzerInfo
                {
                    Id = m.Id,
                    Title = m.Title,
                    Category = m.Category,
                    DefaultSeverity = m.DefaultSeverity,
                    Description = m.Description,
                    HelpLinkUri = m.HelpLinkUri
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            return new AnalyzersListResult
            {
                Success = false,
                Error = $"Failed to list analyzers: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Result from the wpf-analyze-project tool.
/// </summary>
public class AnalysisToolResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int ProjectCount { get; set; }
    public List<ProjectSummary> Projects { get; set; } = new();
    public int TotalDiagnostics { get; set; }
    public Dictionary<string, int> DiagnosticsBySeverity { get; set; } = new();
    public Dictionary<string, int> DiagnosticsById { get; set; } = new();
    public List<DiagnosticResult> Diagnostics { get; set; } = new();
}

/// <summary>
/// Summary of a project in the analysis result.
/// </summary>
public class ProjectSummary
{
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
}

/// <summary>
/// A single diagnostic result from analysis.
/// </summary>
public class DiagnosticResult
{
    public string Id { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public int Column { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}

/// <summary>
/// Result from the wpf-analyze-file tool.
/// </summary>
public class FileAnalysisResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public bool HasProjectContext { get; set; }
    public string? ProjectPath { get; set; }
    public int TotalDiagnostics { get; set; }
    public Dictionary<string, int> DiagnosticsById { get; set; } = new();
    public List<DiagnosticResult> Diagnostics { get; set; } = new();
}

/// <summary>
/// Result from the wpf-list-analyzers tool.
/// </summary>
public class AnalyzersListResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int TotalAnalyzers { get; set; }
    public List<AnalyzerInfo> Analyzers { get; set; } = new();
}

/// <summary>
/// Information about a single analyzer.
/// </summary>
public class AnalyzerInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DefaultSeverity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HelpLinkUri { get; set; }
}
