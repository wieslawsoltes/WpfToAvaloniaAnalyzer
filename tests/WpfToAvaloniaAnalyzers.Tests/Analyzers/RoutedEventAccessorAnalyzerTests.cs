using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class RoutedEventAccessorAnalyzerTests
{
    [Fact]
    public async Task RoutedEventAccessor_ReportsDiagnostic()
    {
        var test = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
            ""Click"",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(MyControl));

        public event RoutedEventHandler {|#0:Click|}
        {
            add { AddHandler(ClickEvent, value); }
            remove { RemoveHandler(ClickEvent, value); }
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA016_ConvertRoutedEventAccessors)
            .WithLocation(0)
            .WithArguments("Click");

        await CodeFixTestHelper.VerifyAnalyzerAsync<RoutedEventAccessorAnalyzer>(test, expected);
    }

    [Fact]
    public async Task NonRoutedEventAccessor_NoDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public class MyControl
    {
        public event System.EventHandler? Click;
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<RoutedEventAccessorAnalyzer>(test);
    }
}
