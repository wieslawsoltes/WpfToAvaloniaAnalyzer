using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;
using WpfToAvaloniaAnalyzers.CodeFixes;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class PropertyAccessorCodeFixTests
{
    [Fact]
    public async Task RemoveCastFromStringGetValue()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty MyPropertyProperty =
            DependencyProperty.Register(
                nameof(MyProperty),
                typeof(string),
                typeof(MyControl));

        public string MyProperty
        {
            get => {|#0:(string)GetValue(MyPropertyProperty)|};
            set => SetValue(MyPropertyProperty, value);
        }
    }
}";

        var fixedCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty MyPropertyProperty =
            DependencyProperty.Register(
                nameof(MyProperty),
                typeof(string),
                typeof(MyControl));

        public string MyProperty
        {
            get => GetValue(MyPropertyProperty);
            set => SetValue(MyPropertyProperty, value);
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA004_RemoveCastsFromGetValue)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<PropertyAccessorAnalyzer, PropertyAccessorCodeFixProvider>(
            testCode, expected, fixedCode, compilerDiagnostics: CompilerDiagnostics.None);
    }

    [Fact]
    public async Task RemoveCastFromBoolGetValue()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(MyControl));

        public bool IsActive
        {
            get => {|#0:(bool)GetValue(IsActiveProperty)|};
            set => SetValue(IsActiveProperty, value);
        }
    }
}";

        var fixedCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(MyControl));

        public bool IsActive
        {
            get => GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA004_RemoveCastsFromGetValue)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<PropertyAccessorAnalyzer, PropertyAccessorCodeFixProvider>(
            testCode, expected, fixedCode, compilerDiagnostics: CompilerDiagnostics.None);
    }

    [Fact]
    public async Task RemoveCastFromIntGetValue()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(
                nameof(Count),
                typeof(int),
                typeof(MyControl));

        public int Count
        {
            get => {|#0:(int)GetValue(CountProperty)|};
            set => SetValue(CountProperty, value);
        }
    }
}";

        var fixedCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(
                nameof(Count),
                typeof(int),
                typeof(MyControl));

        public int Count
        {
            get => GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA004_RemoveCastsFromGetValue)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<PropertyAccessorAnalyzer, PropertyAccessorCodeFixProvider>(
            testCode, expected, fixedCode, compilerDiagnostics: CompilerDiagnostics.None);
    }

    [Fact]
    public async Task RemoveCastFromDoubleGetValue()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty WidthValueProperty =
            DependencyProperty.Register(
                nameof(WidthValue),
                typeof(double),
                typeof(MyControl));

        public double WidthValue
        {
            get => {|#0:(double)GetValue(WidthValueProperty)|};
            set => SetValue(WidthValueProperty, value);
        }
    }
}";

        var fixedCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty WidthValueProperty =
            DependencyProperty.Register(
                nameof(WidthValue),
                typeof(double),
                typeof(MyControl));

        public double WidthValue
        {
            get => GetValue(WidthValueProperty);
            set => SetValue(WidthValueProperty, value);
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA004_RemoveCastsFromGetValue)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<PropertyAccessorAnalyzer, PropertyAccessorCodeFixProvider>(
            testCode, expected, fixedCode, compilerDiagnostics: CompilerDiagnostics.None);
    }
}
