using System.Collections.Generic;

namespace WpfToAvaloniaAnalyzers.MSBuild;

public sealed class ProjectTransformResult
{
    public ProjectTransformResult(
        bool projectChanged,
        IReadOnlyList<string> appliedTransforms,
        IReadOnlyList<string> diagnostics)
    {
        ProjectChanged = projectChanged;
        AppliedTransforms = appliedTransforms;
        Diagnostics = diagnostics;
    }

    public bool ProjectChanged { get; }

    public IReadOnlyList<string> AppliedTransforms { get; }

    public IReadOnlyList<string> Diagnostics { get; }
}
