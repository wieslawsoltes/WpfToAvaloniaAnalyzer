namespace WpfToAvaloniaAnalyzers.Sample.Wpf.Samples;

public class DependencyObjectSimulation
{
    internal virtual int EffectiveValuesInitialSize => 0;
}

/// <summary>
/// Samples WA014 by overriding the EffectiveValuesInitialSize property from a simulated WPF base type.
/// </summary>
public class EffectiveValuesInitialSizeSample : DependencyObjectSimulation
{
    internal override int EffectiveValuesInitialSize => 9;
}
