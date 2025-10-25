using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class BaseClassAnalyzerTests
{
    [Fact]
    public async Task EmptyCode_NoDiagnostics()
    {
        var test = @"";

        await CodeFixTestHelper.VerifyAnalyzerAsync<BaseClassAnalyzer>(test);
    }

    [Fact]
    public async Task WpfControlBaseClass_ReportsDiagnostic()
    {
        var test = @"
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : {|#0:Control|}
    {
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA003_ConvertWpfBaseClass)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyAnalyzerAsync<BaseClassAnalyzer>(test, expected);
    }

    [Fact]
    public async Task WpfControlWithFullyQualifiedName_ReportsDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public class MyControl : {|#0:System.Windows.Controls.Control|}
    {
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA003_ConvertWpfBaseClass)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyAnalyzerAsync<BaseClassAnalyzer>(test, expected);
    }

    [Fact]
    public async Task ClassWithoutBaseClass_NoDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<BaseClassAnalyzer>(test);
    }

    [Fact]
    public async Task NonWpfBaseClass_NoDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public class MyControl : object
    {
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<BaseClassAnalyzer>(test);
    }

    [Fact]
    public async Task AvaloniaControlBaseClass_NoDiagnostic()
    {
        var test = @"
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<BaseClassAnalyzer>(test);
    }

    [Fact]
    public async Task MultipleClassesWithWpfControl_ReportsMultipleDiagnostics()
    {
        var test = @"
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl1 : {|#0:Control|}
    {
    }

    public class MyControl2 : {|#1:Control|}
    {
    }
}";

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.WA003_ConvertWpfBaseClass)
                .WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.WA003_ConvertWpfBaseClass)
                .WithLocation(1)
        };

        await CodeFixTestHelper.VerifyAnalyzerAsync<BaseClassAnalyzer>(test, expected);
    }

    [Fact]
    public async Task ClassWithInterfaceAndWpfControl_ReportsDiagnostic()
    {
        var test = @"
using System;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : {|#0:Control|}, IDisposable
    {
        public void Dispose() { }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA003_ConvertWpfBaseClass)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyAnalyzerAsync<BaseClassAnalyzer>(test, expected);
    }

    [Fact]
    public async Task CustomControlClass_NoDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public class Control
    {
    }

    public class MyControl : Control
    {
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<BaseClassAnalyzer>(test);
    }
}
