using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using WpfToAvaloniaAnalyzers;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.MSBuild;

namespace WpfToAvaloniaAnalyzers.Cli;

internal static class Program
{
    private static Task<int> Main(string[] args)
    {
        var rootCommand = CreateRootCommand();
        return InvokeAsync(rootCommand, args);
    }

    private static async Task<int> InvokeAsync(RootCommand command, string[] args)
    {
        var exitCode = await command.InvokeAsync(args).ConfigureAwait(false);
        return Environment.ExitCode != 0 ? Environment.ExitCode : exitCode;
    }

    private static RootCommand CreateRootCommand()
    {
        var pathOption = new Option<string>(new[] { "--path", "-p" }, "Path to a .sln or .csproj file.")
        {
            IsRequired = true
        };

        var scopeOption = new Option<string?>(new[] { "--scope", "-s" }, "Scope to apply fixes: document, project, or solution.");
        scopeOption.AddValidator(result =>
        {
            if (result.GetValueOrDefault<string?>() is { Length: > 0 } value &&
                !Enum.GetNames(typeof(FixScope)).Any(name => string.Equals(name, value, StringComparison.OrdinalIgnoreCase)))
            {
                result.ErrorMessage = "Scope must be one of: document, project, solution.";
            }
        });

        var projectOption = new Option<string?>(new[] { "--project" }, "Project name to target when running on a solution.");
        var documentOption = new Option<string?>(new[] { "--document", "-d" }, "Document path for document scope.");
        var diagnosticsOption = new Option<string[]>(new[] { "--diagnostic", "--diagnostics", "-id" }, "Diagnostic IDs to apply fixes for.")
        {
            Arity = ArgumentArity.ZeroOrMore
        };
        var codeActionOption = new Option<string?>(new[] { "--code-action", "-a" }, "Code action title to apply when multiple fixes are available.");
        var modeOption = new Option<string?>(new[] { "--mode" }, () => "sequential", "Execution mode: sequential, parallel, or fixall.");
        var msbuildFixOption = new Option<string?>(new[] { "--msbuild-fix" }, () => "default", "MSBuild transform preset: default, none, or avalonia.");

        var rootCommand = new RootCommand("Apply WpfToAvalonia code fixes using the Roslyn workspace API.")
        {
            pathOption,
            scopeOption,
            projectOption,
            documentOption,
            diagnosticsOption,
            codeActionOption,
            modeOption,
            msbuildFixOption
        };

        rootCommand.SetHandler(async context =>
        {
            var workspacePath = context.ParseResult.GetValueForOption(pathOption);
            if (workspacePath is null)
            {
                Console.Error.WriteLine("The --path option is required.");
                Environment.ExitCode = 1;
                return;
            }

            var scopeValue = context.ParseResult.GetValueForOption(scopeOption);
            var projectName = context.ParseResult.GetValueForOption(projectOption);
            var documentPath = context.ParseResult.GetValueForOption(documentOption);
            var diagnosticValues = context.ParseResult.GetValueForOption(diagnosticsOption) ?? Array.Empty<string>();
            var codeActionTitle = context.ParseResult.GetValueForOption(codeActionOption);
            var modeValue = context.ParseResult.GetValueForOption(modeOption);
            var msbuildFixValue = context.ParseResult.GetValueForOption(msbuildFixOption);

            if (!ToolOptions.TryCreate(workspacePath, scopeValue, projectName, documentPath, diagnosticValues, codeActionTitle, modeValue, msbuildFixValue, out var options, out var error))
            {
                Console.Error.WriteLine(error);
                Environment.ExitCode = 1;
                return;
            }

            var exitCode = await RunAsync(options, CancellationToken.None).ConfigureAwait(false);
            Environment.ExitCode = exitCode;
        });

        return rootCommand;
    }

    private static async Task<int> RunAsync(ToolOptions options, CancellationToken cancellationToken)
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        await ApplyMsbuildTransformsAsync(options, cancellationToken).ConfigureAwait(false);

        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) =>
        {
            Console.Error.WriteLine($"MSBuild workspace: {e.Diagnostic}");
        };

        Solution solution;
        Project? openedProject = null;

        try
        {
            if (options.WorkspacePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                solution = await workspace.OpenSolutionAsync(options.WorkspacePath, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else if (options.WorkspacePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                openedProject = await workspace.OpenProjectAsync(options.WorkspacePath, cancellationToken: cancellationToken).ConfigureAwait(false);
                solution = workspace.CurrentSolution;
            }
            else
            {
                Console.Error.WriteLine("Input path must point to a .sln or .csproj file.");
                return 1;
            }
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Operation cancelled.");
            return 1;
        }
        catch (AggregateException aggregate) when (aggregate.InnerExceptions.Count == 1)
        {
            Console.Error.WriteLine($"Failed to open workspace '{options.WorkspacePath}': {aggregate.InnerException!.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open workspace '{options.WorkspacePath}': {ex.Message}");
            return 1;
        }

        if (!options.TryResolve(solution, openedProject, out var resolved, out var resolveError))
        {
            Console.Error.WriteLine(resolveError);
            return 1;
        }

        var analyzers = AnalyzerLoader.LoadAnalyzers();
        if (analyzers.IsDefaultOrEmpty)
        {
            Console.Error.WriteLine("No analyzers were discovered.");
            return 1;
        }

        var codeFixSet = AnalyzerLoader.LoadCodeFixProviders(resolved.RequestedDiagnosticIds);
        if (codeFixSet.Providers.IsDefaultOrEmpty)
        {
            Console.Error.WriteLine("No code fix providers match the requested diagnostics.");
            return 1;
        }

        var activeDiagnosticIds = resolved.RequestedDiagnosticIds.Count > 0
            ? resolved.RequestedDiagnosticIds.Where(codeFixSet.FixableDiagnosticIds.Contains).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)
            : codeFixSet.FixableDiagnosticIds;

        var missingDiagnostics = resolved.RequestedDiagnosticIds.Where(id => !activeDiagnosticIds.Contains(id)).ToList();
        if (missingDiagnostics.Count > 0)
        {
            Console.Error.WriteLine($"Warning: no code fix providers found for diagnostic(s): {string.Join(", ", missingDiagnostics)}");
        }

        if (activeDiagnosticIds.Count == 0)
        {
            Console.Error.WriteLine("No diagnostics remain after filtering; nothing to do.");
            return 1;
        }

        resolved = resolved with { ActiveDiagnosticIds = activeDiagnosticIds };

        Console.WriteLine($"Workspace: {options.WorkspacePath}");
        Console.WriteLine($"Scope: {resolved.Scope}; Diagnostics: {string.Join(", ", resolved.ActiveDiagnosticIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))}");
        Console.WriteLine($"Mode: {resolved.Mode}");
        if (resolved.DocumentPath != null)
        {
            Console.WriteLine($"Document: {resolved.DocumentPath}");
        }

        var applied = await CodeFixApplier.ApplyAsync(
            workspace,
            analyzers,
            codeFixSet.Providers,
            resolved,
            cancellationToken).ConfigureAwait(false);

        if (!applied)
        {
            Console.WriteLine("No matching diagnostics were found.");
        }

        return 0;
    }

    private static async Task ApplyMsbuildTransformsAsync(ToolOptions options, CancellationToken cancellationToken)
    {
        if (options.MsBuildFixMode == MsBuildFixMode.None)
        {
            return;
        }

        try
        {
            if (options.WorkspacePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyTransformsForProjectAsync(options.WorkspacePath, options.MsBuildFixMode, cancellationToken).ConfigureAwait(false);

                return;
            }

            if (!options.WorkspacePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var solutionDirectory = Path.GetDirectoryName(options.WorkspacePath);
            if (solutionDirectory is null)
            {
                return;
            }

            var anyChanged = false;

            foreach (var project in Directory.EnumerateFiles(solutionDirectory, "*.csproj", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var changed = await ApplyTransformsForProjectAsync(project, options.MsBuildFixMode, cancellationToken).ConfigureAwait(false);
                anyChanged |= changed;
            }

            if (anyChanged)
            {
                Console.WriteLine("MSBuild: Project files updated prior to Roslyn analysis.");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"MSBuild transform failed: {ex.Message}");
        }
    }

    private static async Task<bool> ApplyTransformsForProjectAsync(string projectPath, MsBuildFixMode mode, CancellationToken cancellationToken)
    {
        switch (mode)
        {
            case MsBuildFixMode.Avalonia:
            {
                var changed = await ProjectTransformer.EnsureAvaloniaPackageReferenceAsync(
                    projectPath,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (changed)
                {
                    Console.WriteLine($"MSBuild: Added Avalonia package reference to '{projectPath}'.");
                }

                return changed;
            }

            case MsBuildFixMode.Default:
            default:
            {
                var context = new ProjectTransformContext(
                    projectPath,
                    new[] { new PackageRequest("Avalonia", ProjectTransformer.DefaultAvaloniaVersion) });

                var result = await ProjectTransformer.ApplyTransformsAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);

                foreach (var diagnostic in result.Diagnostics)
                {
                    Console.WriteLine($"MSBuild: {diagnostic}");
                }

                if (result.ProjectChanged)
                {
                    Console.WriteLine($"MSBuild: '{projectPath}' updated via {string.Join(", ", result.AppliedTransforms)}.");
                }

                return result.ProjectChanged;
            }
        }
    }
}

internal static class AnalyzerLoader
{
    public static ImmutableArray<DiagnosticAnalyzer> LoadAnalyzers()
    {
        var assembly = typeof(BaseClassAnalyzer).Assembly;
        var analyzers = assembly
            .GetTypes()
            .Where(static type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
            .Select(static type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
            .ToImmutableArray();

        return analyzers;
    }

    public static CodeFixProviderSet LoadCodeFixProviders(ImmutableHashSet<string> requestedDiagnosticIds)
    {
        var assembly = typeof(BaseClassCodeFixProvider).Assembly;
        var providers = assembly
            .GetTypes()
            .Where(static type => !type.IsAbstract && typeof(CodeFixProvider).IsAssignableFrom(type))
            .Select(static type => (CodeFixProvider)Activator.CreateInstance(type)!)
            .ToImmutableArray();

        if (!requestedDiagnosticIds.IsEmpty)
        {
            providers = providers
                .Where(provider => provider.FixableDiagnosticIds.Any(requestedDiagnosticIds.Contains))
                .ToImmutableArray();
        }

        var fixableIds = providers
            .SelectMany(provider => provider.FixableDiagnosticIds)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        return new CodeFixProviderSet(providers, fixableIds);
    }
}

internal sealed record CodeFixProviderSet(ImmutableArray<CodeFixProvider> Providers, ImmutableHashSet<string> FixableDiagnosticIds);

internal static class CodeFixApplier
{
    public static async Task<bool> ApplyAsync(
        MSBuildWorkspace workspace,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        ImmutableArray<CodeFixProvider> codeFixProviders,
        ResolvedOptions options,
        CancellationToken cancellationToken)
    {
        if (options.ActiveDiagnosticIds.Count == 0)
        {
            return false;
        }

        var workspaceGate = new object();
        var summary = new FixSummary(options.Mode);
        var anyProjectProcessed = false;

        foreach (var projectId in options.ProjectIds)
        {
            var project = workspace.CurrentSolution.GetProject(projectId);
            if (project is null)
            {
                continue;
            }

            var analysis = await AnalyzeProjectAsync(project, analyzers, options, cancellationToken).ConfigureAwait(false);
            if (!analysis.HasDiagnostics)
            {
                continue;
            }

            var fixAllApplied = false;
            if (options.Mode == ExecutionMode.FixAll)
            {
                fixAllApplied = await TryApplyFixAllAsync(workspace, codeFixProviders, options, analysis, workspaceGate, summary, cancellationToken).ConfigureAwait(false);
                if (fixAllApplied)
                {
                    project = workspace.CurrentSolution.GetProject(projectId);
                    if (project is null)
                    {
                        continue;
                    }

                    analysis = await AnalyzeProjectAsync(project, analyzers, options, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!analysis.HasDiagnostics)
            {
                anyProjectProcessed |= fixAllApplied;
                continue;
            }

            var maxDegreeOfParallelism = options.Mode == ExecutionMode.Parallel
                ? Math.Max(1, Environment.ProcessorCount)
                : 1;

            if (maxDegreeOfParallelism > 1)
            {
                summary.MarkParallel();
            }

            await ProcessDocumentsAsync(
                workspace,
                codeFixProviders,
                options,
                analysis,
                maxDegreeOfParallelism,
                workspaceGate,
                summary,
                cancellationToken).ConfigureAwait(false);

            anyProjectProcessed = true;
        }

        if (summary.DiagnosticsFixed > 0)
        {
            var modeLabel = summary.UsedFixAll
                ? "fix-all"
                : summary.UsedParallel ? "parallel" : "sequential";

            Console.WriteLine($"Fixed {summary.DiagnosticsFixed} diagnostics across {summary.DocumentCount} documents via {modeLabel} mode.");
            return true;
        }

        return anyProjectProcessed;
    }

    private static async Task<ProjectAnalysisResult> AnalyzeProjectAsync(
        Project project,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        ResolvedOptions options,
        CancellationToken cancellationToken)
    {
        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null)
        {
            return ProjectAnalysisResult.Empty(project);
        }

        var analysisResult = await compilation
            .WithAnalyzers(analyzers)
            .GetAnalysisResultAsync(cancellationToken)
            .ConfigureAwait(false);

        var diagnostics = analysisResult
            .GetAllDiagnostics()
            .Where(diagnostic => options.ActiveDiagnosticIds.Contains(diagnostic.Id))
            .ToImmutableArray();

        if (diagnostics.IsEmpty)
        {
            return ProjectAnalysisResult.Empty(project);
        }

        var documentDiagnostics = ImmutableDictionary.CreateBuilder<DocumentId, DocumentDiagnostics>();

        foreach (var group in diagnostics
                     .Where(diagnostic => diagnostic.Location.IsInSource && diagnostic.Location.SourceTree is not null)
                     .GroupBy(diagnostic => project.Solution.GetDocument(diagnostic.Location.SourceTree!)))
        {
            var document = group.Key;
            if (document is null || document.Project.Id != project.Id)
            {
                continue;
            }

            if (options.Scope == FixScope.Document && options.DocumentId is not null && document.Id != options.DocumentId)
            {
                continue;
            }

            var orderedDiagnostics = SortDiagnostics(group.ToImmutableArray());
            if (orderedDiagnostics.Length > 0)
            {
                documentDiagnostics[document.Id] = new DocumentDiagnostics(document.Id, document.FilePath, orderedDiagnostics);
            }
        }

        var projectDiagnostics = diagnostics
            .Where(diagnostic => !diagnostic.Location.IsInSource || diagnostic.Location.SourceTree is null)
            .ToImmutableArray();

        return new ProjectAnalysisResult(project, documentDiagnostics.ToImmutable(), projectDiagnostics);
    }

    private static async Task<bool> TryApplyFixAllAsync(
        MSBuildWorkspace workspace,
        ImmutableArray<CodeFixProvider> codeFixProviders,
        ResolvedOptions options,
        ProjectAnalysisResult analysis,
        object workspaceGate,
        FixSummary summary,
        CancellationToken cancellationToken)
    {
        var applied = false;
        var fixAllScope = ToFixAllScope(options.Scope);

        foreach (var provider in codeFixProviders)
        {
            var fixAllProvider = provider.GetFixAllProvider();
            if (fixAllProvider is null)
            {
                continue;
            }

            var supportedDiagnosticIds = provider.FixableDiagnosticIds
                .Where(options.ActiveDiagnosticIds.Contains)
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

            if (supportedDiagnosticIds.Count == 0 || !HasDiagnosticsForIds(analysis, supportedDiagnosticIds))
            {
                continue;
            }

            var diagnosticProvider = new PrecomputedFixAllDiagnosticProvider(analysis, supportedDiagnosticIds);

            FixAllContext context;
            if (fixAllScope == FixAllScope.Document)
            {
                if (options.DocumentId is null)
                {
                    continue;
                }

                var document = workspace.CurrentSolution.GetDocument(options.DocumentId);
                if (document is null)
                {
                    continue;
                }

                context = new FixAllContext(
                    document,
                    provider,
                    fixAllScope,
                    options.CodeActionTitle ?? provider.GetType().Name,
                    supportedDiagnosticIds,
                    diagnosticProvider,
                    cancellationToken);
            }
            else
            {
                context = new FixAllContext(
                    analysis.Project,
                    provider,
                    fixAllScope,
                    options.CodeActionTitle ?? provider.GetType().Name,
                    supportedDiagnosticIds,
                    diagnosticProvider,
                    cancellationToken);
            }

            var fixAllAction = await fixAllProvider.GetFixAsync(context).ConfigureAwait(false);
            if (fixAllAction is null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(options.CodeActionTitle) &&
                fixAllAction.Title.IndexOf(options.CodeActionTitle, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            await ApplyCodeActionAsync(workspace, fixAllAction, workspaceGate, cancellationToken).ConfigureAwait(false);
            summary.RecordFixAll(analysis, supportedDiagnosticIds);
            applied = true;
        }

        return applied;
    }

    private static async Task ProcessDocumentsAsync(
        MSBuildWorkspace workspace,
        ImmutableArray<CodeFixProvider> codeFixProviders,
        ResolvedOptions options,
        ProjectAnalysisResult analysis,
        int maxDegreeOfParallelism,
        object workspaceGate,
        FixSummary summary,
        CancellationToken cancellationToken)
    {
        var workItems = CreateWorkItems(analysis, options);
        if (workItems.IsEmpty)
        {
            return;
        }

        if (maxDegreeOfParallelism <= 1)
        {
            foreach (var workItem in workItems.OrderBy(item => item.FilePath, PathComparer))
            {
                await ProcessDocumentWorkItemAsync(workItem, workspace, codeFixProviders, options, workspaceGate, summary, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        await Parallel.ForEachAsync(
            workItems,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (workItem, ct) =>
            {
                await ProcessDocumentWorkItemAsync(workItem, workspace, codeFixProviders, options, workspaceGate, summary, ct).ConfigureAwait(false);
            });
    }

    private static async ValueTask ProcessDocumentWorkItemAsync(
        DocumentWorkItem workItem,
        MSBuildWorkspace workspace,
        ImmutableArray<CodeFixProvider> codeFixProviders,
        ResolvedOptions options,
        object workspaceGate,
        FixSummary summary,
        CancellationToken cancellationToken)
    {
        Document? currentDocument = null;

        foreach (var diagnostic in workItem.Diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();

            currentDocument ??= workspace.CurrentSolution.GetDocument(workItem.DocumentId);
            if (currentDocument is null)
            {
                return;
            }

            var applied = await TryApplyFixAsync(
                workspace,
                currentDocument,
                diagnostic,
                codeFixProviders,
                options,
                workspaceGate,
                summary,
                cancellationToken).ConfigureAwait(false);

            if (applied)
            {
                currentDocument = null; // force refresh for next diagnostic
            }
        }
    }

    private static bool HasDiagnosticsForIds(ProjectAnalysisResult analysis, ImmutableHashSet<string> diagnosticIds)
    {
        foreach (var document in analysis.DocumentDiagnostics.Values)
        {
            if (document.Diagnostics.Any(diagnostic => diagnosticIds.Contains(diagnostic.Id)))
            {
                return true;
            }
        }

        return analysis.ProjectDiagnostics.Any(diagnostic => diagnosticIds.Contains(diagnostic.Id));
    }

    private static ImmutableArray<DocumentWorkItem> CreateWorkItems(ProjectAnalysisResult analysis, ResolvedOptions options)
    {
        var builder = ImmutableArray.CreateBuilder<DocumentWorkItem>();

        foreach (var document in analysis.DocumentDiagnostics.Values)
        {
            if (document.Diagnostics.IsDefaultOrEmpty || document.Diagnostics.Length == 0)
            {
                continue;
            }

            if (options.Scope == FixScope.Document && options.DocumentId is not null && document.DocumentId != options.DocumentId)
            {
                continue;
            }

            builder.Add(new DocumentWorkItem(document.DocumentId, document.FilePath, document.Diagnostics));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<Diagnostic> SortDiagnostics(ImmutableArray<Diagnostic> diagnostics) =>
        diagnostics
            .OrderByDescending(diagnostic => diagnostic.Location.SourceSpan.Start)
            .ToImmutableArray();

    private static async Task<bool> TryApplyFixAsync(
        MSBuildWorkspace workspace,
        Document document,
        Diagnostic diagnostic,
        ImmutableArray<CodeFixProvider> codeFixProviders,
        ResolvedOptions options,
        object workspaceGate,
        FixSummary summary,
        CancellationToken cancellationToken)
    {
        foreach (var provider in codeFixProviders)
        {
            if (!provider.FixableDiagnosticIds.Contains(diagnostic.Id))
            {
                continue;
            }

            var actions = new List<CodeAction>();
            var context = new CodeFixContext(
                document,
                diagnostic,
                (action, _) =>
                {
                    if (options.CodeActionTitle is null ||
                        action.Title.IndexOf(options.CodeActionTitle, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        actions.Add(action);
                    }
                },
                cancellationToken);

            await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            var action = SelectCodeAction(actions, options.CodeActionTitle);
            if (action is null)
            {
                continue;
            }

            await ApplyCodeActionAsync(workspace, action, workspaceGate, cancellationToken).ConfigureAwait(false);
            summary.RecordDocument(document.Id, 1);
            Console.WriteLine($"{diagnostic.Id} -> {action.Title} ({document.FilePath})");
            return true;
        }

        Console.Error.WriteLine($"No code fix available for {diagnostic.Id} in {document.FilePath}");
        return false;
    }

    private static CodeAction? SelectCodeAction(List<CodeAction> actions, string? filter)
    {
        if (actions.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(filter))
        {
            return actions[0];
        }

        var exact = actions.FirstOrDefault(action => string.Equals(action.Title, filter, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact;
        }

        return actions.FirstOrDefault(action => action.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static async Task ApplyCodeActionAsync(Workspace workspace, CodeAction action, object workspaceGate, CancellationToken cancellationToken)
    {
        var operations = await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);

        lock (workspaceGate)
        {
            foreach (var operation in operations)
            {
                operation.Apply(workspace, cancellationToken);
            }
        }
    }

    private static FixAllScope ToFixAllScope(FixScope scope) =>
        scope switch
        {
            FixScope.Document => FixAllScope.Document,
            FixScope.Project => FixAllScope.Project,
            _ => FixAllScope.Solution
        };

    private static readonly IComparer<string?> PathComparer =
        Comparer<string?>.Create((left, right) =>
        {
            var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            return comparer.Compare(left ?? string.Empty, right ?? string.Empty);
        });

    private sealed record DocumentWorkItem(DocumentId DocumentId, string? FilePath, ImmutableArray<Diagnostic> Diagnostics);

    private sealed class FixSummary
    {
        private readonly object _gate = new();
        private readonly HashSet<DocumentId> _documents = new();
        private int _diagnosticsFixed;
        private bool _usedFixAll;
        private bool _usedParallel;

        public FixSummary(ExecutionMode mode)
        {
            Mode = mode;
        }

        public ExecutionMode Mode { get; }

        public int DiagnosticsFixed
        {
            get
            {
                lock (_gate)
                {
                    return _diagnosticsFixed;
                }
            }
        }

        public int DocumentCount
        {
            get
            {
                lock (_gate)
                {
                    return _documents.Count;
                }
            }
        }

        public bool UsedFixAll
        {
            get
            {
                lock (_gate)
                {
                    return _usedFixAll;
                }
            }
        }

        public bool UsedParallel
        {
            get
            {
                lock (_gate)
                {
                    return _usedParallel;
                }
            }
        }

        public void RecordDocument(DocumentId documentId, int diagnostics)
        {
            lock (_gate)
            {
                _diagnosticsFixed += diagnostics;
                _documents.Add(documentId);
            }
        }

        public void RecordFixAll(ProjectAnalysisResult analysis, ImmutableHashSet<string> diagnosticIds)
        {
            lock (_gate)
            {
                foreach (var document in analysis.DocumentDiagnostics.Values)
                {
                    var count = document.Diagnostics.Count(diagnostic => diagnosticIds.Contains(diagnostic.Id));
                    if (count == 0)
                    {
                        continue;
                    }

                    _diagnosticsFixed += count;
                    _documents.Add(document.DocumentId);
                }

                _diagnosticsFixed += analysis.ProjectDiagnostics.Count(diagnostic => diagnosticIds.Contains(diagnostic.Id));
                _usedFixAll = true;
            }
        }

        public void MarkParallel()
        {
            lock (_gate)
            {
                _usedParallel = true;
            }
        }
    }

    private sealed record DocumentDiagnostics(DocumentId DocumentId, string? FilePath, ImmutableArray<Diagnostic> Diagnostics);

    private sealed record ProjectAnalysisResult(
        Project Project,
        ImmutableDictionary<DocumentId, DocumentDiagnostics> DocumentDiagnostics,
        ImmutableArray<Diagnostic> ProjectDiagnostics)
    {
        public bool HasDiagnostics =>
            DocumentDiagnostics.Values.Any(document => !document.Diagnostics.IsDefaultOrEmpty && document.Diagnostics.Length > 0) ||
            !ProjectDiagnostics.IsEmpty;

        public static ProjectAnalysisResult Empty(Project project) =>
            new(project, ImmutableDictionary<DocumentId, DocumentDiagnostics>.Empty, ImmutableArray<Diagnostic>.Empty);
    }

    private sealed class PrecomputedFixAllDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        private readonly ProjectAnalysisResult _analysis;
        private readonly ImmutableHashSet<string> _diagnosticIds;

        public PrecomputedFixAllDiagnosticProvider(ProjectAnalysisResult analysis, ImmutableHashSet<string> diagnosticIds)
        {
            _analysis = analysis;
            _diagnosticIds = diagnosticIds;
        }

        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            if (_analysis.DocumentDiagnostics.TryGetValue(document.Id, out var documentDiagnostics))
            {
                var result = documentDiagnostics.Diagnostics.Where(diagnostic => _diagnosticIds.Contains(diagnostic.Id));
                return Task.FromResult<IEnumerable<Diagnostic>>(result);
            }

            return Task.FromResult<IEnumerable<Diagnostic>>(Array.Empty<Diagnostic>());
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            var result = _analysis.ProjectDiagnostics.Where(diagnostic => _diagnosticIds.Contains(diagnostic.Id));
            return Task.FromResult<IEnumerable<Diagnostic>>(result);
        }

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<Diagnostic>();

            foreach (var document in _analysis.DocumentDiagnostics.Values)
            {
                builder.AddRange(document.Diagnostics.Where(diagnostic => _diagnosticIds.Contains(diagnostic.Id)));
            }

            builder.AddRange(_analysis.ProjectDiagnostics.Where(diagnostic => _diagnosticIds.Contains(diagnostic.Id)));
            return Task.FromResult<IEnumerable<Diagnostic>>(builder.ToImmutable());
        }
    }
}

internal sealed record ResolvedOptions(
    FixScope Scope,
    ImmutableArray<ProjectId> ProjectIds,
    DocumentId? DocumentId,
    ImmutableHashSet<string> RequestedDiagnosticIds,
    ImmutableHashSet<string> ActiveDiagnosticIds,
    string? CodeActionTitle,
    string? DocumentPath,
    ExecutionMode Mode,
    MsBuildFixMode MsBuildFixMode);

internal enum FixScope
{
    Document,
    Project,
    Solution
}

internal enum ExecutionMode
{
    Sequential,
    Parallel,
    FixAll
}

internal enum MsBuildFixMode
{
    Default,
    None,
    Avalonia
}

internal sealed class ToolOptions
{
    private ToolOptions(
        string workspacePath,
        FixScope? scopeOverride,
        string? projectName,
        string? documentPath,
        ImmutableArray<string> diagnosticIds,
        string? codeActionTitle,
        ExecutionMode mode,
        MsBuildFixMode msBuildFixMode)
    {
        WorkspacePath = workspacePath;
        ScopeOverride = scopeOverride;
        ProjectName = projectName;
        DocumentPath = documentPath;
        DiagnosticIds = diagnosticIds;
        CodeActionTitle = codeActionTitle;
        Mode = mode;
        MsBuildFixMode = msBuildFixMode;
    }

    public string WorkspacePath { get; }

    public FixScope? ScopeOverride { get; }

    public string? ProjectName { get; }

    public string? DocumentPath { get; }

    public ImmutableArray<string> DiagnosticIds { get; }

    public string? CodeActionTitle { get; }

    public ExecutionMode Mode { get; }

    public MsBuildFixMode MsBuildFixMode { get; }

    public static bool TryCreate(
        string workspacePath,
        string? scopeValue,
        string? projectName,
        string? documentPath,
        IEnumerable<string>? diagnosticValues,
        string? codeActionTitle,
        string? modeValue,
        string? msBuildFixValue,
        out ToolOptions options,
        out string? error)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            error = "The --path option is required.";
            options = default!;
            return false;
        }

        FixScope? scopeOverride = null;
        if (!string.IsNullOrWhiteSpace(scopeValue))
        {
            if (!Enum.TryParse(scopeValue, ignoreCase: true, out FixScope parsedScope))
            {
                error = $"Unrecognized scope '{scopeValue}'. Expected document, project, or solution.";
                options = default!;
                return false;
            }

            scopeOverride = parsedScope;
        }

        string? normalizedDocument = null;
        if (!string.IsNullOrWhiteSpace(documentPath))
        {
            normalizedDocument = Path.GetFullPath(documentPath.Trim());
        }

        var mode = ExecutionMode.Sequential;
        if (!string.IsNullOrWhiteSpace(modeValue))
        {
            if (!Enum.TryParse(modeValue, ignoreCase: true, out mode))
            {
                error = $"Unrecognized mode '{modeValue}'. Expected sequential, parallel, or fixall.";
                options = default!;
                return false;
            }
        }

        var diagnosticsBuilder = ImmutableArray.CreateBuilder<string>();
        if (diagnosticValues is not null)
        {
            foreach (var value in diagnosticValues)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                diagnosticsBuilder.AddRange(
                    value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }

        var normalizedWorkspace = Path.GetFullPath(workspacePath.Trim());
        var normalizedProject = string.IsNullOrWhiteSpace(projectName) ? null : projectName.Trim();
        var normalizedCodeAction = string.IsNullOrWhiteSpace(codeActionTitle) ? null : codeActionTitle.Trim();
        var msbuildFixMode = MsBuildFixMode.Default;
        if (!string.IsNullOrWhiteSpace(msBuildFixValue))
        {
            if (!Enum.TryParse(msBuildFixValue, ignoreCase: true, out msbuildFixMode))
            {
                error = $"Unrecognized msbuild-fix '{msBuildFixValue}'. Expected default, none, or avalonia.";
                options = default!;
                return false;
            }
        }

        options = new ToolOptions(
            normalizedWorkspace,
            scopeOverride,
            normalizedProject,
            normalizedDocument,
            diagnosticsBuilder.ToImmutable(),
            normalizedCodeAction,
            mode,
            msbuildFixMode);

        error = null;
        return true;
    }

    public bool TryResolve(
        Solution solution,
        Project? openedProject,
        out ResolvedOptions resolved,
        out string? error)
    {
        var inferredScope = InferScope(openedProject);
        var scope = ScopeOverride ?? inferredScope;

        var projectIds = ImmutableArray.CreateBuilder<ProjectId>();
        DocumentId? documentId = null;
        var documentPath = DocumentPath;

        switch (scope)
        {
            case FixScope.Document:
                if (documentPath is null)
                {
                    error = "Document scope requires --document <path>.";
                    resolved = default!;
                    return false;
                }

                var document = FindDocument(solution, documentPath);
                if (document is null)
                {
                    error = $"Document '{documentPath}' was not found in the workspace.";
                    resolved = default!;
                    return false;
                }

                documentId = document.Id;
                documentPath = NormalizePath(documentPath);
                projectIds.Add(document.Project.Id);
                break;

            case FixScope.Project:
                var project = ResolveProject(solution, openedProject, ProjectName);
                if (project is null)
                {
                    error = ProjectName is null
                        ? "Project scope requires --project <name> when multiple projects are available."
                        : $"Project '{ProjectName}' was not found in the workspace.";
                    resolved = default!;
                    return false;
                }

                projectIds.Add(project.Id);
                break;

            case FixScope.Solution:
                if (ProjectName is not null)
                {
                    var targetProject = solution.Projects.FirstOrDefault(p =>
                        string.Equals(p.Name, ProjectName, StringComparison.OrdinalIgnoreCase));
                    if (targetProject is null)
                    {
                        error = $"Project '{ProjectName}' was not found in the workspace.";
                        resolved = default!;
                        return false;
                    }

                    projectIds.Add(targetProject.Id);
                }
                else if (openedProject is not null)
                {
                    projectIds.Add(openedProject.Id);
                }
                else
                {
                    projectIds.AddRange(solution.ProjectIds);
                }
                break;
        }

        var requestedDiagnostics = DiagnosticIds.IsDefaultOrEmpty
            ? ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase)
            : DiagnosticIds
                .Select(id => id.Trim())
                .Where(id => id.Length > 0)
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        resolved = new ResolvedOptions(
            scope,
            projectIds.ToImmutable(),
            documentId,
            requestedDiagnostics,
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase),
            CodeActionTitle,
            documentPath,
            Mode,
            MsBuildFixMode);

        error = null;
        return true;
    }

    private FixScope InferScope(Project? openedProject)
    {
        if (ScopeOverride.HasValue)
        {
            return ScopeOverride.Value;
        }

        if (DocumentPath is not null)
        {
            return FixScope.Document;
        }

        if (openedProject is not null || ProjectName is not null)
        {
            return FixScope.Project;
        }

        return FixScope.Solution;
    }

    private static Document? FindDocument(Solution solution, string absolutePath)
    {
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.FilePath is null)
                {
                    continue;
                }

                if (PathsEqual(document.FilePath, absolutePath))
                {
                    return document;
                }
            }
        }

        return null;
    }

    private static Project? ResolveProject(Solution solution, Project? openedProject, string? projectName)
    {
        if (projectName is not null)
        {
            return solution.Projects.FirstOrDefault(project =>
                string.Equals(project.Name, projectName, StringComparison.OrdinalIgnoreCase));
        }

        if (openedProject is not null)
        {
            return openedProject;
        }

        return solution.ProjectIds.Count == 1
            ? solution.Projects.FirstOrDefault()
            : null;
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(NormalizePath(left), NormalizePath(right), PathComparison);
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path);
    }

    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
