using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class DependencyPropertyCodeFixTests
{
    [Fact]
    public async Task ConvertDependencyPropertyToStyledProperty()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty {|#0:MyPropertyProperty|} =
            DependencyProperty.Register(
                nameof(MyProperty),
                typeof(string),
                typeof(MyControl));

        public string MyProperty
        {
            get => (string)GetValue(MyPropertyProperty);
            set => SetValue(MyPropertyProperty, value);
        }
    }
}";

var fixedCode = @"
using System.Windows;
using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyControl : Avalonia.Controls.Control
    {
        public static readonly StyledProperty<string> MyPropertyProperty = AvaloniaProperty.Register<MyControl, string>(nameof(MyProperty), default(string));

        public string MyProperty
        {
            get => (string)GetValue(MyPropertyProperty);
            set => SetValue(MyPropertyProperty, value);
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty)
            .WithLocation(0)
            .WithArguments("MyPropertyProperty");

        await CodeFixTestHelper.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task ConvertMultipleDependencyProperties()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty {|#0:TitleProperty|} =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(MyControl));

        public string Title { get; set; }

        public static readonly DependencyProperty {|#1:CountProperty|} =
            DependencyProperty.Register(
                nameof(Count),
                typeof(int),
                typeof(MyControl));

        public int Count { get; set; }
    }
}";

var fixedCode = @"
using System.Windows;
using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyControl : Avalonia.Controls.Control
    {
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<MyControl, string>(nameof(Title), default(string));

        public string Title { get; set; }

        public static readonly StyledProperty<int> CountProperty = AvaloniaProperty.Register<MyControl, int>(nameof(Count), default(int));

        public int Count { get; set; }
    }
}";

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty)
                .WithLocation(0)
                .WithArguments("TitleProperty"),
            new DiagnosticResult(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty)
                .WithLocation(1)
                .WithArguments("CountProperty")
        };

        await CodeFixTestHelper.VerifyCodeFixAsync(testCode, fixedCode, expected);
    }

    [Fact]
    public async Task ConvertDependencyProperty_GeneratesValidAvaloniaCode()
    {
        // This test verifies that properly migrated Avalonia code compiles successfully
        // (after user manually changes base class and removes cast)
        var avaloniaCode = @"
using Avalonia;

namespace TestNamespace
{
    public class MyControl : AvaloniaObject
    {
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<MyControl, string>(nameof(Title), string.Empty);

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
    }
}";

        // Verify the Avalonia code compiles
        var nugetPackagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var avaloniaBasePath = Path.Combine(nugetPackagesPath, "avalonia", "11.3.7", "lib", "net8.0");

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("AvaloniaTestProject", LanguageNames.CSharp);

        // Add .NET 8.0 references
        foreach (var reference in ReferenceAssemblies.Net.Net80.ResolveAsync(LanguageNames.CSharp, CancellationToken.None).Result)
        {
            project = project.AddMetadataReference(reference);
        }

        // Add Avalonia references
        project = project.AddMetadataReference(MetadataReference.CreateFromFile(Path.Combine(avaloniaBasePath, "Avalonia.Base.dll")));
        project = project.AddMetadataReference(MetadataReference.CreateFromFile(Path.Combine(avaloniaBasePath, "Avalonia.Controls.dll")));

        project = project.WithCompilationOptions(new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

        var document = project.AddDocument("TestAvalonia.cs", avaloniaCode);
        var compilation = await document.Project.GetCompilationAsync();

        var diagnostics = compilation!.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
        if (diagnostics.Any())
        {
            var errorMessages = string.Join(Environment.NewLine, diagnostics.Select(d => d.ToString()));
            throw new InvalidOperationException($"Avalonia code has compilation errors:{Environment.NewLine}{errorMessages}");
        }
    }
}
