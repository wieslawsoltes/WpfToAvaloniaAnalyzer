using System.ComponentModel;
using ModelContextProtocol.Server;
using WpfToAvaloniaAnalyzers.Mcp.Services;

namespace WpfToAvaloniaAnalyzers.Mcp.Tools;

/// <summary>
/// MCP tools for applying code fixes and transformations to WPF code.
/// </summary>
[McpServerToolType]
public static class TransformationTools
{
    /// <summary>
    /// Applies a code fix to a specific diagnostic in a file.
    /// </summary>
    [McpServerTool]
    [Description("Apply a code fix to a specific WPF diagnostic. Returns the modified files and diffs showing the changes made.")]
    public static async Task<ApplyFixToolResult> ApplyFix(
        MCPCodeFixService codeFixService,
        [Description("Path to .csproj, .sln, or directory containing project")] string projectPath,
        [Description("Path to the file containing the diagnostic")] string filePath,
        [Description("Diagnostic ID to fix (e.g., 'WPFAV001')")] string diagnosticId,
        [Description("Line number where the diagnostic occurs (1-based)")] int line,
        [Description("Column number where the diagnostic occurs (1-based)")] int column,
        [Description("Optional index of which fix to apply if multiple fixes available")] int? fixIndex = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                return new ApplyFixToolResult
                {
                    Success = false,
                    Error = "Project path is required"
                };
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new ApplyFixToolResult
                {
                    Success = false,
                    Error = "File path is required"
                };
            }

            if (string.IsNullOrWhiteSpace(diagnosticId))
            {
                return new ApplyFixToolResult
                {
                    Success = false,
                    Error = "Diagnostic ID is required"
                };
            }

            if (line < 1 || column < 1)
            {
                return new ApplyFixToolResult
                {
                    Success = false,
                    Error = "Line and column must be >= 1"
                };
            }

            if (!File.Exists(projectPath) && !Directory.Exists(projectPath))
            {
                return new ApplyFixToolResult
                {
                    Success = false,
                    Error = $"Project path not found: {projectPath}"
                };
            }

            if (!File.Exists(filePath))
            {
                return new ApplyFixToolResult
                {
                    Success = false,
                    Error = $"File not found: {filePath}"
                };
            }

            var result = await codeFixService.ApplyFixAsync(
                projectPath,
                filePath,
                diagnosticId,
                line,
                column,
                fixIndex,
                cancellationToken);

            return new ApplyFixToolResult
            {
                Success = result.Success,
                Error = result.Error,
                AppliedFixTitle = result.AppliedFixTitle,
                ModifiedFiles = result.ModifiedFiles,
                AvailableFixes = result.AvailableFixes.Select(f => new FixInfo
                {
                    Index = f.Index,
                    Title = f.Title
                }).ToList(),
                Diffs = result.Diffs.Select(d => new DiffInfo
                {
                    FilePath = d.FilePath,
                    UnifiedDiff = d.UnifiedDiff,
                    LinesAdded = d.LinesAdded,
                    LinesRemoved = d.LinesRemoved
                }).ToList()
            };
        }
        catch (TimeoutException ex)
        {
            return new ApplyFixToolResult
            {
                Success = false,
                Error = $"Code fix timeout: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new ApplyFixToolResult
            {
                Success = false,
                Error = $"Code fix failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Applies batch code fixes to multiple diagnostics in a project.
    /// </summary>
    [McpServerTool]
    [Description("Apply code fixes to multiple diagnostics in a project. Can filter by diagnostic IDs and target specific files. Supports dry-run mode to preview changes.")]
    public static async Task<BatchConvertToolResult> BatchConvert(
        MCPCodeFixService codeFixService,
        [Description("Path to .csproj, .sln, or directory containing project")] string projectPath,
        [Description("Optional array of diagnostic IDs to fix (e.g., ['WPFAV001', 'WPFAV002']). If not specified, fixes all diagnostics.")] string[]? diagnosticIds = null,
        [Description("Optional array of file paths to process. If not specified, processes all files.")] string[]? targetFiles = null,
        [Description("If true, shows what would be fixed without applying changes")] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                return new BatchConvertToolResult
                {
                    Success = false,
                    Error = "Project path is required"
                };
            }

            if (!File.Exists(projectPath) && !Directory.Exists(projectPath))
            {
                return new BatchConvertToolResult
                {
                    Success = false,
                    Error = $"Project path not found: {projectPath}"
                };
            }

            // Validate target files if specified
            if (targetFiles != null)
            {
                foreach (var file in targetFiles)
                {
                    if (!File.Exists(file))
                    {
                        return new BatchConvertToolResult
                        {
                            Success = false,
                            Error = $"Target file not found: {file}"
                        };
                    }
                }
            }

            var progress = new Progress<BatchFixProgress>();
            var result = await codeFixService.ApplyBatchFixesAsync(
                projectPath,
                diagnosticIds,
                targetFiles,
                dryRun,
                progress,
                cancellationToken);

            return new BatchConvertToolResult
            {
                Success = result.Success,
                Error = result.Error,
                FixedDiagnostics = result.FixedDiagnostics,
                ModifiedFiles = result.ModifiedFiles,
                FailedFixes = result.FailedFixes.Select(f => new FailedFixDetails
                {
                    DiagnosticId = f.DiagnosticId,
                    FilePath = f.FilePath,
                    Line = f.Line,
                    Error = f.Error
                }).ToList(),
                SummaryByDiagnosticId = result.SummaryByDiagnosticId,
                DryRun = dryRun
            };
        }
        catch (TimeoutException ex)
        {
            return new BatchConvertToolResult
            {
                Success = false,
                Error = $"Batch convert timeout: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new BatchConvertToolResult
            {
                Success = false,
                Error = $"Batch convert failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets available code fixes for a diagnostic without applying them.
    /// </summary>
    [McpServerTool]
    [Description("Preview available code fixes for a diagnostic at a specific location without applying any changes.")]
    public static async Task<PreviewFixesToolResult> PreviewFixes(
        MCPCodeFixService codeFixService,
        [Description("Path to .csproj, .sln, or directory containing project")] string projectPath,
        [Description("Path to the file containing the diagnostic")] string filePath,
        [Description("Diagnostic ID (e.g., 'WPFAV001')")] string diagnosticId,
        [Description("Line number where the diagnostic occurs (1-based)")] int line,
        [Description("Column number where the diagnostic occurs (1-based)")] int column,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                return new PreviewFixesToolResult
                {
                    Success = false,
                    Error = "Project path is required"
                };
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new PreviewFixesToolResult
                {
                    Success = false,
                    Error = "File path is required"
                };
            }

            if (string.IsNullOrWhiteSpace(diagnosticId))
            {
                return new PreviewFixesToolResult
                {
                    Success = false,
                    Error = "Diagnostic ID is required"
                };
            }

            if (!File.Exists(filePath))
            {
                return new PreviewFixesToolResult
                {
                    Success = false,
                    Error = $"File not found: {filePath}"
                };
            }

            var result = await codeFixService.GetAvailableFixesAsync(
                projectPath,
                filePath,
                diagnosticId,
                line,
                column,
                cancellationToken);

            return new PreviewFixesToolResult
            {
                Success = result.Success,
                Error = result.Error,
                AvailableFixes = result.Fixes.Select(f => new FixInfo
                {
                    Index = f.Index,
                    Title = f.Title
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            return new PreviewFixesToolResult
            {
                Success = false,
                Error = $"Preview fixes failed: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Result from the wpf-apply-fix tool.
/// </summary>
public class ApplyFixToolResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string AppliedFixTitle { get; set; } = string.Empty;
    public List<string> ModifiedFiles { get; set; } = new();
    public List<FixInfo> AvailableFixes { get; set; } = new();
    public List<DiffInfo> Diffs { get; set; } = new();
}

/// <summary>
/// Result from the wpf-batch-convert tool.
/// </summary>
public class BatchConvertToolResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int FixedDiagnostics { get; set; }
    public List<string> ModifiedFiles { get; set; } = new();
    public List<FailedFixDetails> FailedFixes { get; set; } = new();
    public Dictionary<string, int> SummaryByDiagnosticId { get; set; } = new();
    public bool DryRun { get; set; }
}

/// <summary>
/// Result from the wpf-preview-fixes tool.
/// </summary>
public class PreviewFixesToolResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<FixInfo> AvailableFixes { get; set; } = new();
}

/// <summary>
/// Information about an available fix.
/// </summary>
public class FixInfo
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Diff information for a file.
/// </summary>
public class DiffInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string UnifiedDiff { get; set; } = string.Empty;
    public int LinesAdded { get; set; }
    public int LinesRemoved { get; set; }
}

/// <summary>
/// Details about a failed fix.
/// </summary>
public class FailedFixDetails
{
    public string DiagnosticId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public string Error { get; set; } = string.Empty;
}
