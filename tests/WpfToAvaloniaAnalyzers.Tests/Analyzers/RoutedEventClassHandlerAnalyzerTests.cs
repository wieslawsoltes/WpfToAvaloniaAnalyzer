using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class RoutedEventClassHandlerAnalyzerTests
{
    [Fact]
    public async Task RegisterClassHandler_ReportsDiagnostic()
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

        static MyControl()
        {
            EventManager.RegisterClassHandler(typeof(MyControl), {|#0:ClickEvent|}, new RoutedEventHandler(OnClick));
        }

        private static void OnClick(object sender, RoutedEventArgs e) { }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA017_ConvertRegisterClassHandler)
            .WithLocation(0)
            .WithArguments("ClickEvent");

        await CodeFixTestHelper.VerifyAnalyzerAsync<RoutedEventClassHandlerAnalyzer>(test, expected);
    }
}
