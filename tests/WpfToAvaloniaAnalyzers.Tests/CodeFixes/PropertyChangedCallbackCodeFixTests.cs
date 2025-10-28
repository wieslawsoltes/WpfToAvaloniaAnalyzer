using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class PropertyChangedCallbackCodeFixTests
{
    [Fact]
    public async Task ConvertWpfCallbackSignatureWithoutTypeHint_DefaultsToObject()
    {
        var test = @"
using System.Windows;

namespace TestNamespace
{
    public class MyControl
    {
        private static void {|#0:OnPropertyChanged|}(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Handle property changed - no type inference available
        }
    }
}";

var fixedCode = @"
using System.Windows;
using Avalonia;

namespace TestNamespace
{
    public class MyControl
    {
        private static void OnPropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            // Handle property changed - no type inference available
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA006_ConvertPropertyChangedCallback)
            .WithLocation(0)
            .WithArguments("OnPropertyChanged");

        await CodeFixTestHelper.VerifyCodeFixAsync<PropertyChangedCallbackAnalyzer, PropertyChangedCallbackCodeFixProvider>(
            test, expected, fixedCode, compilerDiagnostics: CompilerDiagnostics.None);
    }

    [Fact]
    public async Task ConvertWpfCallbackWithIntCast_InfersPropertyType()
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

var fixedCode = @"
using System.Windows;
using System.Windows.Controls;
using Avalonia;

namespace TestNamespace
{
    public class MyControl : Control
    {
        private static void OnCountChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
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

        await CodeFixTestHelper.VerifyCodeFixAsync<PropertyChangedCallbackAnalyzer, PropertyChangedCallbackCodeFixProvider>(
            test, expected, fixedCode, compilerDiagnostics: CompilerDiagnostics.None);
    }

    [Fact]
    public async Task ConvertWpfCallbackWithStringCast_InfersPropertyType()
    {
        var test = @"
using System.Windows;

namespace TestNamespace
{
    public class MyControl
    {
        private static void {|#0:OnNameChanged|}(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var name = (string)e.NewValue;
        }
    }
}";

var fixedCode = @"
using System.Windows;
using Avalonia;

namespace TestNamespace
{
    public class MyControl
    {
        private static void OnNameChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            var name = (string)e.NewValue;
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA006_ConvertPropertyChangedCallback)
            .WithLocation(0)
            .WithArguments("OnNameChanged");

        await CodeFixTestHelper.VerifyCodeFixAsync<PropertyChangedCallbackAnalyzer, PropertyChangedCallbackCodeFixProvider>(
            test, expected, fixedCode, compilerDiagnostics: CompilerDiagnostics.None);
    }

    [Fact]
    public async Task ConvertWpfCallbackWithDoubleCast_InfersPropertyType()
    {
        var test = @"
using System.Windows;

namespace TestNamespace
{
    public class MyControl
    {
        private static void {|#0:OnScoreChanged|}(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var score = (double)e.OldValue;
        }
    }
}";

var fixedCode = @"
using System.Windows;
using Avalonia;

namespace TestNamespace
{
    public class MyControl
    {
        private static void OnScoreChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            var score = (double)e.OldValue;
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA006_ConvertPropertyChangedCallback)
            .WithLocation(0)
            .WithArguments("OnScoreChanged");

        await CodeFixTestHelper.VerifyCodeFixAsync<PropertyChangedCallbackAnalyzer, PropertyChangedCallbackCodeFixProvider>(
            test, expected, fixedCode, compilerDiagnostics: CompilerDiagnostics.None);
    }
}
