using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class DependencyPropertyAnalyzerTests
{
    [Fact]
    public async Task EmptyCode_NoDiagnostics()
    {
        var test = @"";

        await CodeFixTestHelper.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DependencyPropertyDeclaration_ReportsDiagnostic()
    {
        var test = @"
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

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty)
            .WithLocation(0)
            .WithArguments("MyPropertyProperty");

        await CodeFixTestHelper.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task MultipleDependencyProperties_ReportMultipleDiagnostics()
    {
        var test = @"
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

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty {|#1:WidthProperty|} =
            DependencyProperty.Register(
                nameof(Width),
                typeof(double),
                typeof(MyControl));

        public double Width
        {
            get => (double)GetValue(WidthProperty);
            set => SetValue(WidthProperty, value);
        }
    }
}";

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty)
                .WithLocation(0)
                .WithArguments("TitleProperty"),
            new DiagnosticResult(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty)
                .WithLocation(1)
                .WithArguments("WidthProperty")
        };

        await CodeFixTestHelper.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task NonDependencyProperty_NoDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public class MyControl
    {
        public static readonly string MyProperty = ""test"";
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task InstanceDependencyProperty_NoDiagnostic()
    {
        var test = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        // Non-static fields should not trigger diagnostic
        public readonly DependencyProperty MyPropertyProperty =
            DependencyProperty.Register(
                nameof(MyProperty),
                typeof(string),
                typeof(MyControl));

        public string MyProperty { get; set; }
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync(test);
    }
}
