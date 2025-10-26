using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class FrameworkPropertyMetadataCodeFixTests
{
    [Fact]
    public async Task ConvertsFrameworkPropertyMetadataWithOptions()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty FlagProperty =
            DependencyProperty.Register(
                ""Flag"",
                typeof(bool),
                typeof(MyControl),
                new {|#0:FrameworkPropertyMetadata|}(true, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool Flag
        {
            get => (bool)GetValue(FlagProperty);
            set => SetValue(FlagProperty, value);
        }
    }
}
";

        var fixedCode = @"
using System.Windows;
using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyControl : Avalonia.Controls.Control
    {
        static MyControl()
        {
            AffectsMeasure<MyControl>(FlagProperty);
        }

        public static readonly StyledProperty<bool> FlagProperty = AvaloniaProperty.Register<MyControl, bool>(""Flag"", true);

        public bool Flag
        {
            get => (bool)GetValue(FlagProperty);
            set => SetValue(FlagProperty, value);
        }
    }
}
";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA013_TranslateFrameworkPropertyMetadata)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<FrameworkPropertyMetadataAnalyzer, PropertyMetadataCodeFixProvider>(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task ConvertsFrameworkPropertyMetadataWithCallbackInheritsAndValidate()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty LevelProperty =
            DependencyProperty.Register(
                ""Level"",
                typeof(int),
                typeof(MyControl),
                new {|#0:FrameworkPropertyMetadata|}(
                    10,
                    FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.Inherits,
                    OnLevelChanged),
                IsValidLevel);

        public int Level
        {
            get => (int)GetValue(LevelProperty);
            set => SetValue(LevelProperty, value);
        }

        private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        private static bool IsValidLevel(object value) => true;
    }
}
";

        var fixedCode = @"
using System.Windows;
using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyControl : Avalonia.Controls.Control
    {
        static MyControl()
        {
            AffectsArrange<MyControl>(LevelProperty);
            LevelProperty.Changed.AddClassHandler<MyControl>((sender, args) => OnLevelChanged(sender, args));
        }

        public static readonly StyledProperty<int> LevelProperty = AvaloniaProperty.Register<MyControl, int>(""Level"", 10, inherits: true, validate: value => IsValidLevel(value));

        public int Level
        {
            get => (int)GetValue(LevelProperty);
            set => SetValue(LevelProperty, value);
        }

        private static void OnLevelChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
        }

        private static bool IsValidLevel(object value) => true;
    }
}
";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA013_TranslateFrameworkPropertyMetadata)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<FrameworkPropertyMetadataAnalyzer, PropertyMetadataCodeFixProvider>(testCode, expected, fixedCode);
    }
}
