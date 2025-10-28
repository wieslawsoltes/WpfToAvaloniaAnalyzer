using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class RoutedEventRaiseEventAnalyzerTests
{
    [Fact]
    public async Task RaiseEvent_ReportsDiagnostic()
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

        public void Trigger()
        {
            RaiseEvent({|#0:new RoutedEventArgs(ClickEvent)|});
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA019_ConvertRaiseEvent)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyAnalyzerAsync<RoutedEventRaiseEventAnalyzer>(test, expected);
    }
}
