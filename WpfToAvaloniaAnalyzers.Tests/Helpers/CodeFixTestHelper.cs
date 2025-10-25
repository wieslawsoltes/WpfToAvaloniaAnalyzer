using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using WpfToAvaloniaAnalyzers.CodeFixes;

namespace WpfToAvaloniaAnalyzers.Tests.Helpers;

public static class CodeFixTestHelper
{
    public static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource, CompilerDiagnostics compilerDiagnostics = CompilerDiagnostics.Errors)
    {
        await VerifyCodeFixAsync<DependencyPropertyAnalyzer, DependencyPropertyCodeFixProvider>(source, expected, fixedSource, compilerDiagnostics);
    }

    public static async Task VerifyCodeFixAsync<TAnalyzer, TCodeFix>(string source, DiagnosticResult expected, string fixedSource, CompilerDiagnostics compilerDiagnostics = CompilerDiagnostics.Errors)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, new()
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck
        };

        // Add WPF assemblies from NuGet package
        var nugetPackagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var wpfRefPath = Path.Combine(nugetPackagesPath, "microsoft.windowsdesktop.app.ref", "8.0.0", "ref", "net8.0");
        var avaloniaRefPath = GetAvaloniaReferencePath(nugetPackagesPath);

        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "WindowsBase.dll")));
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "PresentationCore.dll")));
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "PresentationFramework.dll")));

        test.FixedState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "WindowsBase.dll")));
        test.FixedState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "PresentationCore.dll")));
        test.FixedState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "PresentationFramework.dll")));

        if (NeedsAvaloniaReferences(source))
        {
            AddAvaloniaReferences(test.TestState.AdditionalReferences, avaloniaRefPath);
        }
        AddAvaloniaReferences(test.FixedState.AdditionalReferences, avaloniaRefPath);

        test.CompilerDiagnostics = compilerDiagnostics;

        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync();
    }

    public static Task VerifyCodeFixAsync(string source, string fixedSource, params DiagnosticResult[] expected) =>
        VerifyCodeFixAsync(source, fixedSource, CompilerDiagnostics.Errors, expected);

    public static async Task VerifyCodeFixAsync(string source, string fixedSource, CompilerDiagnostics compilerDiagnostics, params DiagnosticResult[] expected)
    {
        var test = new CSharpCodeFixTest<
            DependencyPropertyAnalyzer,
            DependencyPropertyCodeFixProvider,
            DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck
        };

        // Add WPF assemblies from NuGet package
        var nugetPackagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var wpfRefPath = Path.Combine(nugetPackagesPath, "microsoft.windowsdesktop.app.ref", "8.0.0", "ref", "net8.0");
        var avaloniaRefPath = GetAvaloniaReferencePath(nugetPackagesPath);

        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "WindowsBase.dll")));
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "PresentationCore.dll")));
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "PresentationFramework.dll")));

        test.FixedState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "WindowsBase.dll")));
        test.FixedState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "PresentationCore.dll")));
        test.FixedState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "PresentationFramework.dll")));

        if (NeedsAvaloniaReferences(source))
        {
            AddAvaloniaReferences(test.TestState.AdditionalReferences, avaloniaRefPath);
        }
        AddAvaloniaReferences(test.FixedState.AdditionalReferences, avaloniaRefPath);

        test.CompilerDiagnostics = compilerDiagnostics;

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }

    public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        await VerifyAnalyzerAsync<DependencyPropertyAnalyzer>(source, expected);
    }

    public static async Task VerifyAnalyzerAsync<TAnalyzer>(string source, params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        // STEP 1: Compile WPF test code to ensure it's valid before running analyzer
        if (!string.IsNullOrWhiteSpace(source))
        {
            await CompileWpfCodeAsync(source);
        }

        // STEP 2: Run analyzer test
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck
        };

        // Add WPF assemblies from NuGet package
        var nugetPackagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var wpfRefPath = Path.Combine(nugetPackagesPath, "microsoft.windowsdesktop.app.ref", "8.0.0", "ref", "net8.0");

        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "WindowsBase.dll")));
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "PresentationCore.dll")));
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "PresentationFramework.dll")));

        // Suppress compiler errors in tests
        test.CompilerDiagnostics = CompilerDiagnostics.None;

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }

    private static async Task CompileWpfCodeAsync(string source)
    {
        // Remove test markup like {|#0:identifier|}
        var cleanedSource = System.Text.RegularExpressions.Regex.Replace(source, @"\{\|#\d+:(.*?)\|\}", "$1");

        var nugetPackagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var wpfRefPath = Path.Combine(nugetPackagesPath, "microsoft.windowsdesktop.app.ref", "8.0.0", "ref", "net8.0");

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("WpfTestProject", LanguageNames.CSharp);

        // Add .NET 8.0 references
        foreach (var reference in ReferenceAssemblies.Net.Net80.ResolveAsync(LanguageNames.CSharp, CancellationToken.None).Result)
        {
            project = project.AddMetadataReference(reference);
        }

        // Add WPF references
        project = project.AddMetadataReference(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "WindowsBase.dll")));
        project = project.AddMetadataReference(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "PresentationCore.dll")));
        project = project.AddMetadataReference(MetadataReference.CreateFromFile(Path.Combine(wpfRefPath, "PresentationFramework.dll")));

        project = project.WithCompilationOptions(new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

        var document = project.AddDocument("TestWpf.cs", cleanedSource);
        var compilation = await document.Project.GetCompilationAsync();

        // Check for compilation errors, but ignore errors related to Avalonia types (which are expected in some tests)
        bool hasAvaloniaUsing = cleanedSource.Contains("using Avalonia");
        var diagnostics = compilation!.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error &&
                       !d.GetMessage().Contains("Avalonia") && // Skip Avalonia-related errors since we're testing WPF code
                       !(hasAvaloniaUsing && d.GetMessage().Contains("'Control'"))); // Skip Control errors if Avalonia using is present

        if (diagnostics.Any())
        {
            var errorMessages = string.Join(Environment.NewLine, diagnostics.Select(d => d.ToString()));
            throw new InvalidOperationException($"WPF test code has compilation errors:{Environment.NewLine}{errorMessages}");
        }
    }

    private static string? GetAvaloniaReferencePath(string nugetPackagesPath)
    {
        var avaloniaPackagePath = Path.Combine(nugetPackagesPath, "avalonia");
        if (!Directory.Exists(avaloniaPackagePath))
            return null;

        var latestVersionPath = Directory.GetDirectories(avaloniaPackagePath)
            .Select(dir => new { Path = dir, Version = ParseVersion(Path.GetFileName(dir)) })
            .Where(x => x.Version != null)
            .OrderByDescending(x => x.Version)
            .Select(x => x.Path)
            .FirstOrDefault();

        if (latestVersionPath == null)
            return null;

        var refPath = Path.Combine(latestVersionPath, "ref", "net8.0");
        return Directory.Exists(refPath) ? refPath : null;

        static Version? ParseVersion(string? value) =>
            Version.TryParse(value, out var version) ? version : null;
    }

    private static void AddAvaloniaReferences(ICollection<MetadataReference> references, string? avaloniaRefPath)
    {
        if (avaloniaRefPath == null)
            return;

        var assemblies = new[]
        {
            "Avalonia.dll",
            "Avalonia.Base.dll",
            "Avalonia.Controls.dll",
            "Avalonia.Styling.dll"
        };

        foreach (var assembly in assemblies)
        {
            var assemblyPath = Path.Combine(avaloniaRefPath, assembly);
            if (File.Exists(assemblyPath))
            {
                references.Add(MetadataReference.CreateFromFile(assemblyPath));
            }
        }
    }

    private static bool NeedsAvaloniaReferences(string source) =>
        source.Contains("Avalonia", StringComparison.Ordinal);
}
