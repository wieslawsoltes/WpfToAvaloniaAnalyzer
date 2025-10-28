using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class RoutedEventAddOwnerCodeFixTests
{
    [Fact]
    public async Task ConvertsAddOwnerField()
    {
        var source = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly RoutedEvent FooEvent = {|#0:Button.ClickEvent|}.AddOwner(typeof(MyControl));
    }
}";

        var expected = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly global::Avalonia.Interactivity.RoutedEvent FooEvent = Button.ClickEvent;
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA020_ConvertAddOwner)
            .WithSpan(9, 55, 9, 72)
            .WithArguments("ClickEvent");

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventAddOwnerAnalyzer, RoutedEventAddOwnerCodeFixProvider>(
            source,
            diagnostic,
            expected,
            compilerDiagnostics: CompilerDiagnostics.None);
    }
}
