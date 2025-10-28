using MS.Internal.Telemetry.PresentationFramework;
using System.Windows.Controls;

namespace WpfToAvaloniaAnalyzers.Sample.Wpf.Samples;

/// <summary>
/// Provides WA010 coverage by exercising MS.Internal telemetry hooks.
/// </summary>
public class TelemetryInstrumentationSampleControl : Control
{
    static TelemetryInstrumentationSampleControl()
    {
        ControlsTraceLogger.AddControl(TelemetryControls.SampleControl);
    }
}
