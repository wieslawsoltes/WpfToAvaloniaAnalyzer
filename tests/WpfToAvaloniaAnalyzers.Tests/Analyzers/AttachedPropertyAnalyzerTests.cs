using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class AttachedPropertyAnalyzerTests
{
    [Fact]
    public async Task NoAttachedProperty_NoDiagnostic()
    {
        var testCode = @"
using System.Windows;

namespace TestNamespace
{
    public class MyClass : DependencyObject
    {
        public static readonly DependencyProperty FooProperty =
            DependencyProperty.Register(""Foo"", typeof(int), typeof(MyClass));
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<AttachedPropertyAnalyzer>(testCode);
    }

    [Fact]
    public async Task RegisterAttached_ReportsDiagnostic()
    {
        var testCode = @"
using System;
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyClass
    {
        public static readonly DependencyProperty {|#0:BarProperty|} =
            DependencyProperty.RegisterAttached(
                ""Bar"",
                typeof(int),
                typeof(MyClass),
                new FrameworkPropertyMetadata(0),
                new ValidateValueCallback(IsValidBar));

        public static int GetBar(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (int)element.GetValue(BarProperty);
        }

        public static void SetBar(UIElement element, int value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(BarProperty, value);
        }

        private static bool IsValidBar(object value) => true;
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA012_ConvertAttachedDependencyProperty)
            .WithLocation(0)
            .WithArguments("BarProperty");

        await CodeFixTestHelper.VerifyAnalyzerAsync<AttachedPropertyAnalyzer>(testCode, expected);
    }
}
