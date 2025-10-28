using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.Analyzers;

public class TelemetryInstrumentationAnalyzerTests
{
    [Fact]
    public async Task NoTelemetry_NoDiagnostic()
    {
        var testCode = @"
namespace TestNamespace
{
    public class MyClass
    {
        public void DoNothing()
        {
        }
    }
}";

        await CodeFixTestHelper.VerifyAnalyzerAsync<TelemetryInstrumentationAnalyzer>(testCode);
    }

    [Fact]
    public async Task TelemetryUsing_ReportsDiagnostic()
    {
        var testCode = @"
{|#0:using MS.Internal.PresentationFramework;|}

namespace MS.Internal.PresentationFramework
{
    public static class Dummy
    {
    }
}

namespace TestNamespace
{
    public class MyClass
    {
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA010_RemoveTelemetryInstrumentation)
            .WithLocation(0)
            .WithArguments("MS.Internal.PresentationFramework");

        await CodeFixTestHelper.VerifyAnalyzerAsync<TelemetryInstrumentationAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task ControlsTraceLoggerInvocation_ReportsDiagnostic()
    {
        var testCode = @"
namespace TestNamespace
{
    public class MyClass
    {
        static MyClass()
        {
            {|#0:MS.Internal.Telemetry.PresentationFramework.ControlsTraceLogger.AddControl(MS.Internal.Telemetry.PresentationFramework.TelemetryControls.DockPanel);|}
        }
    }
}

namespace MS.Internal.Telemetry.PresentationFramework
{
    public static class ControlsTraceLogger
    {
        public static void AddControl(object value)
        {
        }
    }

    public static class TelemetryControls
    {
        public static readonly object DockPanel = new object();
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA010_RemoveTelemetryInstrumentation)
            .WithSpan(8, 13, 8, 160)
            .WithArguments("ControlsTraceLogger.AddControl");

        await CodeFixTestHelper.VerifyAnalyzerAsync<TelemetryInstrumentationAnalyzer>(testCode, expected);
    }
}
