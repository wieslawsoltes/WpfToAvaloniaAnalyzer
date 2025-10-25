using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class PropertyChangedCallbackAnalyzerTests
{
    [Fact]
    public async Task EmptyCode_NoDiagnostics()
    {
        var test = @"";

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyChangedCallbackAnalyzer>(test);
    }

    [Fact]
    public async Task WpfPropertyChangedCallback_ReportsDiagnostic()
    {
        var test = @"
using System.Windows;

namespace TestNamespace
{
    public class MyControl
    {
        private static void {|#0:OnPropertyChanged|}(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Handle property changed
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA006_ConvertPropertyChangedCallback)
            .WithLocation(0)
            .WithArguments("OnPropertyChanged");

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyChangedCallbackAnalyzer>(test, expected);
    }

    [Fact]
    public async Task WpfPropertyChangedCallbackWithCast_ReportsDiagnostic()
    {
        var test = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        private static void {|#0:OnCountChanged|}(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MyControl control)
            {
                var newValue = (int)e.NewValue;
                var oldValue = (int)e.OldValue;
            }
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA006_ConvertPropertyChangedCallback)
            .WithLocation(0)
            .WithArguments("OnCountChanged");

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyChangedCallbackAnalyzer>(test, expected);
    }

    [Fact]
    public async Task MethodWithDifferentSignature_NoDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public class MyControl
    {
        private static void OnSomethingChanged(string value)
        {
            // Different signature
        }
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyChangedCallbackAnalyzer>(test);
    }

    [Fact]
    public async Task MethodWithThreeParameters_NoDiagnostic()
    {
        var test = @"
using System.Windows;

namespace TestNamespace
{
    public class MyControl
    {
        private static void OnSomethingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e, int extra)
        {
            // Different signature
        }
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyChangedCallbackAnalyzer>(test);
    }

    [Fact]
    public async Task AvaloniaPropertyChangedCallback_NoDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public class AvaloniaObject { }
    public class AvaloniaPropertyChangedEventArgs<T> { }

    public class MyControl
    {
        private static void OnPropertyChanged(AvaloniaObject sender, AvaloniaPropertyChangedEventArgs<int> e)
        {
            // Avalonia signature
        }
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyChangedCallbackAnalyzer>(test);
    }

    [Fact]
    public async Task MultipleWpfCallbacks_ReportMultipleDiagnostics()
    {
        var test = @"
using System.Windows;

namespace TestNamespace
{
    public class MyControl
    {
        private static void {|#0:OnFirstChanged|}(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        private static void {|#1:OnSecondChanged|}(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
    }
}";

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.WA006_ConvertPropertyChangedCallback)
                .WithLocation(0)
                .WithArguments("OnFirstChanged"),
            new DiagnosticResult(DiagnosticDescriptors.WA006_ConvertPropertyChangedCallback)
                .WithLocation(1)
                .WithArguments("OnSecondChanged")
        };

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyChangedCallbackAnalyzer>(test, expected);
    }
}
