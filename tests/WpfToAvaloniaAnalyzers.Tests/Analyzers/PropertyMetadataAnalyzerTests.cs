using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class PropertyMetadataAnalyzerTests
{
    [Fact]
    public async Task EmptyCode_NoDiagnostics()
    {
        var test = @"";

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyMetadataAnalyzer>(test);
    }

    [Fact]
    public async Task PropertyMetadataWithCallback_ReportsDiagnostic()
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
                typeof(MyControl),
                {|#0:new PropertyMetadata(0, OnCountChanged)|});

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Handle property changed
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA005_ConvertPropertyMetadata)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyMetadataAnalyzer>(test, expected);
    }

    [Fact]
    public async Task PropertyMetadataWithLambdaCallback_ReportsDiagnostic()
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
                typeof(MyControl),
                {|#0:new PropertyMetadata(0, (d, e) => { })|});

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA005_ConvertPropertyMetadata)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyMetadataAnalyzer>(test, expected);
    }

    [Fact]
    public async Task PropertyMetadataWithoutCallback_NoDiagnostic()
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
                typeof(MyControl),
                new PropertyMetadata(0));

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyMetadataAnalyzer>(test);
    }

    [Fact]
    public async Task PropertyMetadataOnlyDefaultValue_NoDiagnostic()
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
                typeof(MyControl),
                new PropertyMetadata(default(int)));

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyMetadataAnalyzer>(test);
    }

    [Fact]
    public async Task DependencyPropertyWithoutPropertyMetadata_NoDiagnostic()
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
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyMetadataAnalyzer>(test);
    }
}
