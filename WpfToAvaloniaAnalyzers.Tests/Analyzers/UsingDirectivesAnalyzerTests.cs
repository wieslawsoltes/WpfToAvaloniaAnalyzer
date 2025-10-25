using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class UsingDirectivesAnalyzerTests
{
    [Fact]
    public async Task EmptyCode_NoDiagnostics()
    {
        var test = @"";

        await CodeFixTestHelper.VerifyAnalyzerAsync<UsingDirectivesAnalyzer>(test);
    }

    [Fact]
    public async Task SystemWindowsUsing_ReportsDiagnostic()
    {
        var test = @"
{|#0:using System.Windows;|}

namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA002_RemoveWpfUsings)
            .WithLocation(0)
            .WithArguments("System.Windows");

        await CodeFixTestHelper.VerifyAnalyzerAsync<UsingDirectivesAnalyzer>(test, expected);
    }

    [Fact]
    public async Task SystemWindowsControlsUsing_ReportsDiagnostic()
    {
        var test = @"
{|#0:using System.Windows.Controls;|}

namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA002_RemoveWpfUsings)
            .WithLocation(0)
            .WithArguments("System.Windows.Controls");

        await CodeFixTestHelper.VerifyAnalyzerAsync<UsingDirectivesAnalyzer>(test, expected);
    }

    [Fact]
    public async Task MultipleWpfUsings_ReportsMultipleDiagnostics()
    {
        var test = @"
{|#0:using System.Windows;|}
{|#1:using System.Windows.Controls;|}

namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.WA002_RemoveWpfUsings)
                .WithLocation(0)
                .WithArguments("System.Windows"),
            new DiagnosticResult(DiagnosticDescriptors.WA002_RemoveWpfUsings)
                .WithLocation(1)
                .WithArguments("System.Windows.Controls")
        };

        await CodeFixTestHelper.VerifyAnalyzerAsync<UsingDirectivesAnalyzer>(test, expected);
    }

    [Fact]
    public async Task NonWpfUsings_NoDiagnostic()
    {
        var test = @"
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<UsingDirectivesAnalyzer>(test);
    }

    [Fact]
    public async Task AvaloniaUsings_NoDiagnostic()
    {
        var test = @"
using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<UsingDirectivesAnalyzer>(test);
    }

    [Fact]
    public async Task MixedUsings_OnlyReportsWpfUsings()
    {
        var test = @"
using System;
{|#0:using System.Windows;|}
using System.Collections.Generic;
{|#1:using System.Windows.Controls;|}
using Avalonia;

namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.WA002_RemoveWpfUsings)
                .WithLocation(0)
                .WithArguments("System.Windows"),
            new DiagnosticResult(DiagnosticDescriptors.WA002_RemoveWpfUsings)
                .WithLocation(1)
                .WithArguments("System.Windows.Controls")
        };

        await CodeFixTestHelper.VerifyAnalyzerAsync<UsingDirectivesAnalyzer>(test, expected);
    }
}
