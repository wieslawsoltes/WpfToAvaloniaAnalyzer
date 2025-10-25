using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace WpfToAvaloniaAnalyzers.Tests;

public class WpfControlAnalyzerTests
{
    [Fact]
    public async Task EmptyCode_NoDiagnostics()
    {
        var test = @"";

        await VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DockPanelCreation_ReportsDiagnostic()
    {
        var test = @"
using System.Windows.Controls;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var dockPanel = new {|#0:DockPanel|}();
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WPF001_UseAvaloniaControl)
            .WithLocation(0)
            .WithArguments("DockPanel");

        await VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task MultipleWpfControls_ReportMultipleDiagnostics()
    {
        var test = @"
using System.Windows.Controls;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var dockPanel = new {|#0:DockPanel|}();
            var button = new {|#1:Button|}();
            var textBox = new {|#2:TextBox|}();
        }
    }
}";

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.WPF001_UseAvaloniaControl)
                .WithLocation(0)
                .WithArguments("DockPanel"),
            new DiagnosticResult(DiagnosticDescriptors.WPF001_UseAvaloniaControl)
                .WithLocation(1)
                .WithArguments("Button"),
            new DiagnosticResult(DiagnosticDescriptors.WPF001_UseAvaloniaControl)
                .WithLocation(2)
                .WithArguments("TextBox")
        };

        await VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task DockPanelSetDock_ReportsDiagnostic()
    {
        var test = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var button = new {|#0:Button|}();
            {|#1:DockPanel|}.SetDock(button, Dock.Top);
        }
    }
}";

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.WPF001_UseAvaloniaControl)
                .WithLocation(0)
                .WithArguments("Button"),
            new DiagnosticResult(DiagnosticDescriptors.WPF001_UseAvaloniaControl)
                .WithLocation(1)
                .WithArguments("DockPanel")
        };

        await VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task NonWpfControl_NoDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public class DockPanel // Custom class, not WPF
    {
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var dockPanel = new DockPanel();
        }
    }
}";

        await VerifyAnalyzerAsync(test);
    }

    private static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<WpfControlAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80Windows,
            TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck
        };

        // Suppress compiler errors in tests
        test.CompilerDiagnostics = CompilerDiagnostics.None;

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }
}
