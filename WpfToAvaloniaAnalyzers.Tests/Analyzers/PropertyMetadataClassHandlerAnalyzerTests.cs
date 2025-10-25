using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class PropertyMetadataClassHandlerAnalyzerTests
{
    [Fact]
    public async Task ReportsDiagnosticForPropertyMetadataWithCallback()
    {
        var testCode = @"using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(
                ""Count"",
                typeof(int),
                typeof(MyControl),
                {|#0:new PropertyMetadata(0, OnCountChanged)|});

        public int Count { get; set; }

        private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
    }
}
";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA008_ConvertPropertyMetadataCallbackToClassHandler)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyMetadataClassHandlerAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task NoDiagnosticWhenNoCallback()
    {
        var testCode = @"using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(
                ""Count"",
                typeof(int),
                typeof(MyControl),
                new PropertyMetadata(0));

        public int Count { get; set; }
    }
}
";

        await CodeFixTestHelper.VerifyAnalyzerAsync<PropertyMetadataClassHandlerAnalyzer>(testCode);
    }
}
