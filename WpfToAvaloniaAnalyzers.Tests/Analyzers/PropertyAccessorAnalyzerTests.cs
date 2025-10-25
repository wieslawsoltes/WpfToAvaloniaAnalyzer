using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class PropertyAccessorAnalyzerTests
{
    [Fact]
    public async Task EmptyCode_NoDiagnostics()
    {
        var test = @"";

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyAccessorAnalyzer>(test);
    }

    [Fact]
    public async Task CastOnGetValue_ReportsDiagnostic()
    {
        var test = @"
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

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA004_RemoveCastsFromGetValue)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyAccessorAnalyzer>(test, expected);
    }

    [Fact]
    public async Task MultipleCastsOnGetValue_ReportsMultipleDiagnostics()
    {
        var test = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(MyControl));

        public string Title
        {
            get => {|#0:(string)GetValue(TitleProperty)|};
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                nameof(Width),
                typeof(double),
                typeof(MyControl));

        public double Width
        {
            get => {|#1:(double)GetValue(WidthProperty)|};
            set => SetValue(WidthProperty, value);
        }
    }
}";

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.WA004_RemoveCastsFromGetValue)
                .WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.WA004_RemoveCastsFromGetValue)
                .WithLocation(1)
        };

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyAccessorAnalyzer>(test, expected);
    }

    [Fact]
    public async Task GetValueWithoutCast_NoDiagnostic()
    {
        var test = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty MyPropertyProperty =
            DependencyProperty.Register(
                nameof(MyProperty),
                typeof(object),
                typeof(MyControl));

        public object MyProperty
        {
            get => GetValue(MyPropertyProperty);
            set => SetValue(MyPropertyProperty, value);
        }
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyAccessorAnalyzer>(test);
    }

    [Fact]
    public async Task CastOnNonGetValueMethod_NoDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public class MyClass
    {
        private object GetSomeValue()
        {
            return new object();
        }

        public string GetStringValue()
        {
            return (string)GetSomeValue();
        }
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyAccessorAnalyzer>(test);
    }

    [Fact]
    public async Task CastOnDifferentExpression_NoDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public class MyClass
    {
        private object _value = ""test"";

        public string GetValue()
        {
            return (string)_value;
        }
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyAccessorAnalyzer>(test);
    }

    [Fact]
    public async Task BooleanCastOnGetValue_ReportsDiagnostic()
    {
        var test = @"
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

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA004_RemoveCastsFromGetValue)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyAccessorAnalyzer>(test, expected);
    }

    [Fact]
    public async Task IntCastOnGetValue_ReportsDiagnostic()
    {
        var test = @"
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

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA004_RemoveCastsFromGetValue)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyAccessorAnalyzer>(test, expected);
    }
}
