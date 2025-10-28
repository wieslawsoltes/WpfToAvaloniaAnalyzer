using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;
using WpfToAvaloniaAnalyzers.CodeFixes;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class BaseClassCodeFixTests
{
    [Fact]
    public async Task ConvertWpfControlToAvaloniaControl()
    {
        var testCode = @"
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : {|#0:Control|}
    {
    }
}";

        var fixedCode = @"
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Avalonia.Controls.Control
    {
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA003_ConvertWpfBaseClass)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<BaseClassAnalyzer, BaseClassCodeFixProvider>(
            testCode, expected, fixedCode);
    }

    [Fact]
    public async Task ConvertWpfControlWithMultipleUsings()
    {
        var testCode = @"
using System;
using System.Windows.Controls;
using System.Collections.Generic;

namespace TestNamespace
{
    public class MyControl : {|#0:Control|}
    {
    }
}";

        var fixedCode = @"
using System;
using System.Windows.Controls;
using System.Collections.Generic;

namespace TestNamespace
{
    public class MyControl : Avalonia.Controls.Control
    {
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA003_ConvertWpfBaseClass)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<BaseClassAnalyzer, BaseClassCodeFixProvider>(
            testCode, expected, fixedCode);
    }

    [Fact]
    public async Task ConvertWpfControlWithInterface()
    {
        var testCode = @"
using System;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : {|#0:Control|}, IDisposable
    {
        public void Dispose() { }
    }
}";

        var fixedCode = @"
using System;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Avalonia.Controls.Control, IDisposable
    {
        public void Dispose() { }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA003_ConvertWpfBaseClass)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<BaseClassAnalyzer, BaseClassCodeFixProvider>(
            testCode, expected, fixedCode);
    }

    [Fact]
    public async Task ConvertFullyQualifiedWpfControl()
    {
        var testCode = @"
namespace TestNamespace
{
    public class MyControl : {|#0:System.Windows.Controls.Control|}
    {
    }
}";

        var fixedCode = @"
namespace TestNamespace
{
    public class MyControl : Avalonia.Controls.Control
    {
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA003_ConvertWpfBaseClass)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<BaseClassAnalyzer, BaseClassCodeFixProvider>(
            testCode, expected, fixedCode);
    }
}
