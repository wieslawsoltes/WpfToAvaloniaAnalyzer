using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class AttachedPropertyCodeFixTests
{
    [Fact]
    public async Task ConvertsAttachedDependencyProperty()
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

        [AttachedPropertyBrowsableForChildren()]
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

        private static void OnBarChanged(AvaloniaObject sender, AvaloniaPropertyChangedEventArgs e)
        {
        }
    }
}";

        var fixedCode = @"
using System;
using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyClass
    {
        public static readonly AttachedProperty<int> BarProperty = AvaloniaProperty.RegisterAttached<MyClass, AvaloniaObject, int>(""Bar"", 0, validate: value => IsValidBar(value));
        public static int GetBar(AvaloniaObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return element.GetValue(BarProperty);
        }

        public static void SetBar(AvaloniaObject element, int value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(BarProperty, value);
        }

        private static bool IsValidBar(object value) => true;

        private static void OnBarChanged(AvaloniaObject sender, AvaloniaPropertyChangedEventArgs e)
        {
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA012_ConvertAttachedDependencyProperty)
            .WithLocation(0)
            .WithArguments("BarProperty");

        await CodeFixTestHelper.VerifyCodeFixAsync<AttachedPropertyAnalyzer, AttachedPropertyCodeFixProvider>(
            testCode,
            expected,
            fixedCode,
            compilerDiagnostics: CompilerDiagnostics.None);
    }
}
