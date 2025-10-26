using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class FrameworkPropertyMetadataAnalyzerTests
{
    [Fact]
    public async Task PropertyMetadata_DoesNotReportDiagnostic()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty FooProperty =
            DependencyProperty.Register(
                ""Foo"",
                typeof(int),
                typeof(MyControl),
                new PropertyMetadata(0));
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<FrameworkPropertyMetadataAnalyzer>(testCode);
    }

    [Fact]
    public async Task FrameworkPropertyMetadata_ReportsDiagnostic()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty {|#0:BarProperty|} =
            DependencyProperty.Register(
                ""Bar"",
                typeof(bool),
                typeof(MyControl),
                new {|#1:FrameworkPropertyMetadata|}(true, FrameworkPropertyMetadataOptions.AffectsMeasure));
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA013_TranslateFrameworkPropertyMetadata)
            .WithLocation(1);

        await CodeFixTestHelper.VerifyAnalyzerAsync<FrameworkPropertyMetadataAnalyzer>(testCode, expected);
    }
}
