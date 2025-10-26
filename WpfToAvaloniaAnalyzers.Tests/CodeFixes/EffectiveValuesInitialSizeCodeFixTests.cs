using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class EffectiveValuesInitialSizeCodeFixTests
{
    [Fact]
    public async Task RemovesOverrideProperty()
    {
        var testCode = @"
namespace TestNamespace
{
    public class PanelBase
    {
        protected virtual int EffectiveValuesInitialSize => 0;
    }

    public class MyObject : PanelBase
    {
        protected override int {|#0:EffectiveValuesInitialSize|} => 9;
    }
}
";

        var fixedCode = @"
namespace TestNamespace
{
    public class PanelBase
    {
        protected virtual int EffectiveValuesInitialSize => 0;
    }

    public class MyObject : PanelBase
    {
    }
}
";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA014_RemoveEffectiveValuesInitialSize)
            .WithLocation(0)
            .WithArguments("EffectiveValuesInitialSize");

        await CodeFixTestHelper.VerifyCodeFixAsync<EffectiveValuesInitialSizeAnalyzer, EffectiveValuesInitialSizeCodeFixProvider>(
            testCode,
            expected,
            fixedCode);
    }
}
