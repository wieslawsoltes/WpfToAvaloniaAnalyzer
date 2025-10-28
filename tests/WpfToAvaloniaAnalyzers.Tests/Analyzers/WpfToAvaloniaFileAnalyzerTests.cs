using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class WpfToAvaloniaFileAnalyzerTests
{
    [Fact]
    public async Task ReportsDiagnosticWhenWpfPatternsDetected()
    {
        var testCode = @"
using System.Windows;

namespace TestNamespace
{
    public class MyControl : DependencyObject
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(MyControl),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA007_ApplyAllAnalyzers)
            .WithSpan(2, 1, 2, 22);

        await CodeFixTestHelper.VerifyAnalyzerAsync<WpfToAvaloniaFileAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task NoDiagnosticWhenFileHasNoWpfPatterns()
    {
        var testCode = @"
namespace TestNamespace
{
    public class MyControl
    {
        public int Title { get; set; }
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<WpfToAvaloniaFileAnalyzer>(testCode);
    }
}
