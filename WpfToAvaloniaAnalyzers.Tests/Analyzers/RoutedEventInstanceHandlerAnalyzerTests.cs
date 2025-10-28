using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class RoutedEventInstanceHandlerAnalyzerTests
{
    [Fact]
    public async Task AddHandler_ReportsDiagnostic()
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

        public MyControl()
        {
            AddHandler({|#0:ClickEvent|}, new RoutedEventHandler(OnClick));
        }

        private void OnClick(object sender, RoutedEventArgs e) { }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA018_ConvertAddRemoveHandler)
            .WithLocation(0)
            .WithArguments("ClickEvent");

        await CodeFixTestHelper.VerifyAnalyzerAsync<RoutedEventInstanceHandlerAnalyzer>(test, expected);
    }

    [Fact]
    public async Task AddHandlerWithoutDelegateCreation_NoDiagnostic()
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

        public MyControl()
        {
            AddHandler(ClickEvent, OnClick);
        }

        private void OnClick(object sender, RoutedEventArgs e) { }
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<RoutedEventInstanceHandlerAnalyzer>(test);
    }
}
