using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class WpfToAvaloniaFileCodeFixTests
{
    [Fact]
    public async Task AppliesAllFixesInFile()
    {
        var testCode = @"using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                ""Title"",
                typeof(string),
                typeof(MyControl),
                new PropertyMetadata(""Default"", OnTitlePropertyChanged));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private static void OnTitlePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
        }
    }
}
";

        var fixedCode = @"using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyControl : Avalonia.Controls.Control
    {
        static MyControl()
        {
            TitleProperty.Changed.AddClassHandler<MyControl>((sender, args) => OnTitlePropertyChanged(sender, args));
        }

        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<MyControl, string>(""Title"", ""Default"");

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private static void OnTitlePropertyChanged(AvaloniaObject sender, AvaloniaPropertyChangedEventArgs e)
        {
        }
    }
}
";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA007_ApplyAllAnalyzers)
            .WithSpan(1, 1, 1, 22);

        await CodeFixTestHelper.VerifyCodeFixAsync<WpfToAvaloniaFileAnalyzer, WpfToAvaloniaFileCodeFixProvider>(
            testCode,
            expected,
            fixedCode,
            compilerDiagnostics: CompilerDiagnostics.Errors);
    }
}
