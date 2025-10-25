using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class CommonDependencyPropertyAttributeCodeFixTests
{
    [Fact]
    public async Task RemovesCommonDependencyPropertyAttribute()
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

        var fixedCode = @"
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
        public static readonly int SomeField = 0;
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA011_RemoveCommonDependencyPropertyAttribute)
            .WithLocation(0)
            .WithArguments("CommonDependencyProperty");

        await CodeFixTestHelper.VerifyCodeFixAsync<CommonDependencyPropertyAttributeAnalyzer, CommonDependencyPropertyAttributeCodeFixProvider>(
            testCode,
            expected,
            fixedCode);
    }

    [Fact]
    public async Task RemovesCommonDependencyPropertyAttributeFromAttributeList()
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
        [Obsolete, {|#0:CommonDependencyProperty|}]
        public static readonly int SomeField = 0;
    }
}";

        var fixedCode = @"
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
        [Obsolete]
        public static readonly int SomeField = 0;
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA011_RemoveCommonDependencyPropertyAttribute)
            .WithLocation(0)
            .WithArguments("CommonDependencyProperty");

        await CodeFixTestHelper.VerifyCodeFixAsync<CommonDependencyPropertyAttributeAnalyzer, CommonDependencyPropertyAttributeCodeFixProvider>(
            testCode,
            expected,
            fixedCode);
    }
}
