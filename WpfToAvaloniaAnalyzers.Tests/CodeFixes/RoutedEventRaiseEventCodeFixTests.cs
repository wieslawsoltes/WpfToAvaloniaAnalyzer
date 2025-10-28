using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class RoutedEventRaiseEventCodeFixTests
{
    [Fact]
    public async Task ConvertsRaiseEventArgument()
    {
        var source = @"
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

        var expected = @"
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
            RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(ClickEvent));
        }
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA019_ConvertRaiseEvent)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventRaiseEventAnalyzer, RoutedEventRaiseEventCodeFixProvider>(
            source,
            diagnostic,
            expected,
            compilerDiagnostics: CompilerDiagnostics.None,
            relaxDiagnostics: true,
            numberOfIterations: 1);
    }

    [Fact]
    public async Task ConvertsRaiseEventArgumentWithoutParams()
    {
        var source = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public void Trigger()
        {
            RaiseEvent({|#0:new RoutedEventArgs()|});
        }
    }
}";

        var expected = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public void Trigger()
        {
            RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs());
        }
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA019_ConvertRaiseEvent)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventRaiseEventAnalyzer, RoutedEventRaiseEventCodeFixProvider>(
            source,
            diagnostic,
            expected,
            compilerDiagnostics: CompilerDiagnostics.None,
            relaxDiagnostics: true,
            numberOfIterations: 1);
    }

    [Fact]
    public async Task ConvertsRaiseEventCastTarget()
    {
        var source = @"
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

        private static void Trigger(DependencyObject sender)
        {
            (sender as FrameworkElement)?.RaiseEvent({|#0:new RoutedEventArgs(ClickEvent)|});
        }
    }
}";

        var expected = @"
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

        private static void Trigger(DependencyObject sender)
        {
            (sender as global::Avalonia.Interactivity.Interactive)?.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(ClickEvent));
        }
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA019_ConvertRaiseEvent)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventRaiseEventAnalyzer, RoutedEventRaiseEventCodeFixProvider>(
            source,
            diagnostic,
            expected,
            compilerDiagnostics: CompilerDiagnostics.None,
            relaxDiagnostics: true,
            numberOfIterations: 1);
    }
}
