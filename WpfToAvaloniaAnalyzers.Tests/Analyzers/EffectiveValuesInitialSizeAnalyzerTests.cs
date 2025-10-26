using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class EffectiveValuesInitialSizeAnalyzerTests
{
    [Fact]
    public async Task NoOverride_NoDiagnostic()
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
        public int EffectiveValuesInitialSize => 4;
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<EffectiveValuesInitialSizeAnalyzer>(testCode);
    }

    [Fact]
    public async Task Override_ReportsDiagnostic()
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
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA014_RemoveEffectiveValuesInitialSize)
            .WithLocation(0)
            .WithArguments("EffectiveValuesInitialSize");

        await CodeFixTestHelper.VerifyAnalyzerAsync<EffectiveValuesInitialSizeAnalyzer>(testCode, expected);
    }
}
