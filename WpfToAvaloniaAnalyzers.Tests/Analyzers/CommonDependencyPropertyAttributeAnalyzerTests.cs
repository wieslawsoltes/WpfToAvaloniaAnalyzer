using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class CommonDependencyPropertyAttributeAnalyzerTests
{
    [Fact]
    public async Task NoAttribute_NoDiagnostic()
    {
        var testCode = @"
using System;

namespace TestNamespace
{
    public class MyClass
    {
        public static readonly int SomeField = 0;
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<CommonDependencyPropertyAttributeAnalyzer>(testCode);
    }

    [Fact]
    public async Task CommonDependencyPropertyAttribute_ReportsDiagnostic()
    {
        var testCode = @"
using System;
using MS.Internal.PresentationFramework;

namespace MS.Internal.PresentationFramework
{
    internal sealed class CommonDependencyPropertyAttribute : Attribute
    {
    }
}

namespace TestNamespace
{
    public class MyClass
    {
        [{|#0:CommonDependencyProperty|}]
        public static readonly int SomeField = 0;
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA011_RemoveCommonDependencyPropertyAttribute)
            .WithLocation(0)
            .WithArguments("CommonDependencyProperty");

        await CodeFixTestHelper.VerifyAnalyzerAsync<CommonDependencyPropertyAttributeAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task AliasCommonDependencyPropertyAttribute_ReportsDiagnostic()
    {
        var testCode = @"
using System;
using CommonDependencyProperty = MS.Internal.PresentationFramework.CommonDependencyPropertyAttribute;

namespace MS.Internal.PresentationFramework
{
    internal sealed class CommonDependencyPropertyAttribute : Attribute
    {
    }
}

namespace TestNamespace
{
    public class MyClass
    {
        [{|#0:CommonDependencyProperty|}]
        public static readonly int SomeField = 0;
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA011_RemoveCommonDependencyPropertyAttribute)
            .WithLocation(0)
            .WithArguments("CommonDependencyProperty");

        await CodeFixTestHelper.VerifyAnalyzerAsync<CommonDependencyPropertyAttributeAnalyzer>(testCode, expected);
    }
}
