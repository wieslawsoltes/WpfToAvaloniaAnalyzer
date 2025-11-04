using System.ComponentModel;
using ModelContextProtocol.Server;
using WpfToAvaloniaAnalyzers.Mcp.Services;

namespace WpfToAvaloniaAnalyzers.Mcp.Tools;

/// <summary>
/// MCP utility tools for WPF to Avalonia conversion.
/// </summary>
[McpServerToolType]
public static class UtilityTools
{
    /// <summary>
    /// Gets detailed information about a specific diagnostic.
    /// </summary>
    [McpServerTool]
    [Description("Get detailed information about a WPF to Avalonia diagnostic, including description, category, severity, and help links.")]
    public static DiagnosticInfoResult GetDiagnosticInfo(
        MCPAnalysisService analysisService,
        [Description("The diagnostic ID to get information about (e.g., 'WPFAV001')")] string diagnosticId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(diagnosticId))
            {
                return new DiagnosticInfoResult
                {
                    Success = false,
                    Error = "Diagnostic ID is required"
                };
            }

            var allAnalyzers = analysisService.GetAnalyzerMetadata().ToList();
            var analyzer = allAnalyzers.FirstOrDefault(a =>
                string.Equals(a.Id, diagnosticId, StringComparison.OrdinalIgnoreCase));

            if (analyzer == null)
            {
                return new DiagnosticInfoResult
                {
                    Success = false,
                    Error = $"Diagnostic '{diagnosticId}' not found. Use wpf-list-analyzers to see all available diagnostics."
                };
            }

            return new DiagnosticInfoResult
            {
                Success = true,
                Id = analyzer.Id,
                Title = analyzer.Title,
                Category = analyzer.Category,
                DefaultSeverity = analyzer.DefaultSeverity,
                Description = analyzer.Description,
                HelpLinkUri = analyzer.HelpLinkUri,
                Examples = GetExamplesForDiagnostic(analyzer.Id),
                MigrationGuide = GetMigrationGuideForDiagnostic(analyzer.Id, analyzer.Category)
            };
        }
        catch (Exception ex)
        {
            return new DiagnosticInfoResult
            {
                Success = false,
                Error = $"Failed to get diagnostic info: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Validates a project for WPF to Avalonia conversion readiness.
    /// </summary>
    [McpServerTool]
    [Description("Validate a project for WPF to Avalonia conversion. Checks if workspace can load, MSBuild configuration is valid, and detects WPF dependencies.")]
    public static async Task<ProjectValidationResult> ValidateProject(
        MCPWorkspaceManager workspaceManager,
        [Description("Path to .csproj, .sln, or directory containing project")] string projectPath,
        CancellationToken cancellationToken = default)
    {
        var result = new ProjectValidationResult
        {
            Success = true,
            ProjectPath = projectPath
        };

        try
        {
            // Validate path exists
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                return new ProjectValidationResult
                {
                    Success = false,
                    Error = "Project path is required"
                };
            }

            if (!File.Exists(projectPath) && !Directory.Exists(projectPath))
            {
                return new ProjectValidationResult
                {
                    Success = false,
                    ProjectPath = projectPath,
                    Error = $"Project path not found: {projectPath}"
                };
            }

            result.PathExists = true;

            // Try to load workspace
            WorkspaceEntry? workspaceEntry = null;
            try
            {
                workspaceEntry = await workspaceManager.GetOrOpenWorkspaceAsync(projectPath, cancellationToken);
                result.CanLoadWorkspace = true;
                result.WorkspaceState = workspaceEntry.State.ToString();
            }
            catch (Exception ex)
            {
                result.CanLoadWorkspace = false;
                result.WorkspaceLoadError = ex.Message;
                result.Success = false;
                return result;
            }

            // Get solution info
            var solution = workspaceEntry.Workspace.CurrentSolution;
            var projects = solution.Projects.ToList();
            result.ProjectCount = projects.Count;

            // Check each project
            var projectDetails = new List<ProjectValidationDetails>();
            foreach (var project in projects)
            {
                var details = new ProjectValidationDetails
                {
                    Name = project.Name,
                    Language = project.Language,
                    DocumentCount = project.Documents.Count()
                };

                // Check if it compiles
                try
                {
                    var compilation = await project.GetCompilationAsync(cancellationToken);
                    details.CanCompile = compilation != null;

                    if (compilation != null)
                    {
                        var diagnostics = compilation.GetDiagnostics(cancellationToken);
                        var errors = diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToList();
                        details.CompilationErrorCount = errors.Count;
                        details.HasCompilationErrors = errors.Count > 0;

                        if (details.HasCompilationErrors)
                        {
                            details.CompilationErrors = errors.Take(5).Select(e => new CompilationErrorInfo
                            {
                                Id = e.Id,
                                Message = e.GetMessage(),
                                FilePath = e.Location.SourceTree?.FilePath ?? "unknown",
                                Line = e.Location.GetLineSpan().StartLinePosition.Line + 1
                            }).ToList();
                        }
                    }
                }
                catch (Exception ex)
                {
                    details.CanCompile = false;
                    details.CompilationError = ex.Message;
                }

                // Detect WPF references
                var references = project.MetadataReferences
                    .Select(r => r.Display ?? "")
                    .Where(r => !string.IsNullOrEmpty(r))
                    .ToList();

                details.HasWpfReferences = references.Any(r =>
                    r.Contains("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
                    r.Contains("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
                    r.Contains("WindowsBase", StringComparison.OrdinalIgnoreCase));

                details.WpfReferences = references
                    .Where(r =>
                        r.Contains("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
                        r.Contains("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
                        r.Contains("WindowsBase", StringComparison.OrdinalIgnoreCase))
                    .Select(r => Path.GetFileName(r))
                    .ToList();

                // Detect Avalonia references
                details.HasAvaloniaReferences = references.Any(r =>
                    r.Contains("Avalonia", StringComparison.OrdinalIgnoreCase));

                details.AvaloniaReferences = references
                    .Where(r => r.Contains("Avalonia", StringComparison.OrdinalIgnoreCase))
                    .Select(r => Path.GetFileName(r))
                    .ToList();

                projectDetails.Add(details);
            }

            result.Projects = projectDetails;

            // Overall assessment
            result.HasWpfProjects = projectDetails.Any(p => p.HasWpfReferences);
            result.HasAvaloniaProjects = projectDetails.Any(p => p.HasAvaloniaReferences);
            result.HasCompilationErrors = projectDetails.Any(p => p.HasCompilationErrors);
            result.TotalCompilationErrors = projectDetails.Sum(p => p.CompilationErrorCount);

            // Recommendations
            var recommendations = new List<string>();

            if (!result.HasWpfProjects)
            {
                recommendations.Add("No WPF references detected. This may not be a WPF project.");
            }

            if (result.HasAvaloniaProjects)
            {
                recommendations.Add("Avalonia references already present. Project may be partially migrated.");
            }

            if (result.HasCompilationErrors)
            {
                recommendations.Add($"Project has {result.TotalCompilationErrors} compilation error(s). Fix compilation errors before migration.");
            }

            if (result.HasWpfProjects && !result.HasCompilationErrors)
            {
                recommendations.Add("Project is ready for WPF to Avalonia migration. Use wpf-analyze-project to identify conversion opportunities.");
            }

            result.Recommendations = recommendations;

            return result;
        }
        catch (Exception ex)
        {
            return new ProjectValidationResult
            {
                Success = false,
                ProjectPath = projectPath,
                Error = $"Validation failed: {ex.Message}"
            };
        }
    }

    private static string GetExamplesForDiagnostic(string diagnosticId)
    {
        return diagnosticId switch
        {
            "WPFAV001" => @"WPF Code:
public static readonly DependencyProperty MyProperty =
    DependencyProperty.Register(
        nameof(MyProperty),
        typeof(string),
        typeof(MyClass));

Avalonia Code:
public static readonly StyledProperty<string> MyProperty =
    AvaloniaProperty.Register<MyClass, string>(nameof(MyProperty));",

            "WPFAV002" => @"WPF Code:
public static readonly DependencyProperty AttachedProperty =
    DependencyProperty.RegisterAttached(
        ""Attached"",
        typeof(string),
        typeof(MyClass));

Avalonia Code:
public static readonly AttachedProperty<string> AttachedProperty =
    AvaloniaProperty.RegisterAttached<MyClass, Control, string>(""Attached"");",

            _ => "No examples available for this diagnostic. Check the help link for more information."
        };
    }

    private static string GetMigrationGuideForDiagnostic(string diagnosticId, string category)
    {
        var baseGuide = category switch
        {
            "DependencyProperty" => @"Migration Guide for DependencyProperty:
1. Change DependencyProperty.Register to AvaloniaProperty.Register
2. Update type from DependencyProperty to StyledProperty<T>
3. Update property metadata to Avalonia equivalents
4. Update callbacks to use AvaloniaPropertyChangedEventArgs
5. Update using directives (remove System.Windows, add Avalonia)

Key Differences:
- Avalonia uses strongly-typed properties (StyledProperty<T>)
- Metadata structure is different (no FrameworkPropertyMetadata)
- Callback signatures are different
- Coerce callbacks work differently

Resources:
- https://docs.avaloniaui.net/docs/guides/custom-controls/defining-properties
- https://docs.avaloniaui.net/docs/concepts/property-system",

            "RoutedEvent" => @"Migration Guide for RoutedEvents:
1. Change RoutedEvent to Avalonia's RoutedEvent<T>
2. Update event registration
3. Update event handlers to use RoutedEventArgs<T>
4. Change routing strategy names (Bubble → Bubble, Tunnel → Tunnel, Direct → Direct)

Key Differences:
- Avalonia uses strongly-typed routed events
- Handler signature is slightly different
- Event argument access is type-safe

Resources:
- https://docs.avaloniaui.net/docs/concepts/input/routed-events",

            "BaseClass" => @"Migration Guide for Base Classes:
1. Replace System.Windows base classes with Avalonia equivalents
2. Common mappings:
   - Window → Window
   - UserControl → UserControl
   - Control → Control
   - DependencyObject → AvaloniaObject
   - FrameworkElement → Control

Resources:
- https://docs.avaloniaui.net/docs/guides/migration/migration-from-wpf",

            _ => $@"General Migration Guide for {category}:
1. Identify the WPF pattern being used
2. Find the Avalonia equivalent in the documentation
3. Update the code structure
4. Test thoroughly

Resources:
- https://docs.avaloniaui.net/docs/guides/migration/migration-from-wpf
- https://github.com/AvaloniaUI/Avalonia"
        };

        return baseGuide;
    }
}

/// <summary>
/// Result from the wpf-get-diagnostic-info tool.
/// </summary>
public class DiagnosticInfoResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DefaultSeverity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HelpLinkUri { get; set; }
    public string Examples { get; set; } = string.Empty;
    public string MigrationGuide { get; set; } = string.Empty;
}

/// <summary>
/// Result from the wpf-validate-project tool.
/// </summary>
public class ProjectValidationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string ProjectPath { get; set; } = string.Empty;
    public bool PathExists { get; set; }
    public bool CanLoadWorkspace { get; set; }
    public string? WorkspaceLoadError { get; set; }
    public string WorkspaceState { get; set; } = string.Empty;
    public int ProjectCount { get; set; }
    public bool HasWpfProjects { get; set; }
    public bool HasAvaloniaProjects { get; set; }
    public bool HasCompilationErrors { get; set; }
    public int TotalCompilationErrors { get; set; }
    public List<ProjectValidationDetails> Projects { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Validation details for a single project.
/// </summary>
public class ProjectValidationDetails
{
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public bool CanCompile { get; set; }
    public string? CompilationError { get; set; }
    public bool HasCompilationErrors { get; set; }
    public int CompilationErrorCount { get; set; }
    public List<CompilationErrorInfo> CompilationErrors { get; set; } = new();
    public bool HasWpfReferences { get; set; }
    public List<string> WpfReferences { get; set; } = new();
    public bool HasAvaloniaReferences { get; set; }
    public List<string> AvaloniaReferences { get; set; } = new();
}

/// <summary>
/// Information about a compilation error.
/// </summary>
public class CompilationErrorInfo
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
}
