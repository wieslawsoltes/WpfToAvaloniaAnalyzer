using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;
using WpfToAvaloniaAnalyzers.CodeFixes;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class UsingDirectivesCodeFixTests
{
    [Fact]
    public async Task RemoveSystemWindowsUsing()
    {
        var testCode = @"
{|#0:using System.Windows;|}

namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        var fixedCode = @"using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA002_RemoveWpfUsings)
            .WithLocation(0)
            .WithArguments("System.Windows");

        await CodeFixTestHelper.VerifyCodeFixAsync<UsingDirectivesAnalyzer, UsingDirectivesCodeFixProvider>(
            testCode, expected, fixedCode);
    }

    [Fact]
    public async Task RemoveSystemWindowsControlsUsing()
    {
        var testCode = @"
{|#0:using System.Windows.Controls;|}

namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        var fixedCode = @"using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA002_RemoveWpfUsings)
            .WithLocation(0)
            .WithArguments("System.Windows.Controls");

        await CodeFixTestHelper.VerifyCodeFixAsync<UsingDirectivesAnalyzer, UsingDirectivesCodeFixProvider>(
            testCode, expected, fixedCode);
    }

    [Fact]
    public async Task RemoveMultipleWpfUsings()
    {
        var testCode = @"
using System;
{|#0:using System.Windows;|}
using System.Collections.Generic;

namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        var fixedCode = @"
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA002_RemoveWpfUsings)
            .WithLocation(0)
            .WithArguments("System.Windows");

        await CodeFixTestHelper.VerifyCodeFixAsync<UsingDirectivesAnalyzer, UsingDirectivesCodeFixProvider>(
            testCode, expected, fixedCode);
    }
}
