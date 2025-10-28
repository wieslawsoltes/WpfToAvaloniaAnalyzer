using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class RoutedEventAddOwnerAnalyzerTests
{
    [Fact]
    public async Task RoutedEventAddOwner_ReportsDiagnostic()
    {
        var test = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly RoutedEvent FooEvent = {|#0:Button.ClickEvent|}.AddOwner(typeof(MyControl));
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA020_ConvertAddOwner)
            .WithSpan(9, 55, 9, 72)
            .WithArguments("ClickEvent");

        await CodeFixTestHelper.VerifyAnalyzerAsync<RoutedEventAddOwnerAnalyzer>(test, expected);
    }
}
