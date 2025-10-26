namespace MS.Internal.PresentationFramework
{
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal sealed class CommonDependencyPropertyAttribute : System.Attribute
    {
    }
}

namespace MS.Internal.Telemetry.PresentationFramework
{
    public static class ControlsTraceLogger
    {
        public static void AddControl(object control)
        {
        }
    }

    public static class TelemetryControls
    {
        public static readonly object SampleControl = new();
    }
}
