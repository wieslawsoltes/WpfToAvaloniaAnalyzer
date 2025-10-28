using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class RoutedEventFieldAnalyzerTests
{
    [Fact]
    public async Task RoutedEventField_ReportsDiagnostic()
    {
        var test = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly RoutedEvent {|#0:ClickEvent|} = EventManager.RegisterRoutedEvent(
            ""Click"",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(MyControl));
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA015_ConvertRoutedEventField)
            .WithLocation(0)
            .WithArguments("ClickEvent");

        await CodeFixTestHelper.VerifyAnalyzerAsync<RoutedEventFieldAnalyzer>(test, expected);
    }

    [Fact]
    public async Task NonStaticField_NoDiagnostic()
    {
        var test = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
            ""Click"",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(MyControl));
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<RoutedEventFieldAnalyzer>(test);
    }
}
