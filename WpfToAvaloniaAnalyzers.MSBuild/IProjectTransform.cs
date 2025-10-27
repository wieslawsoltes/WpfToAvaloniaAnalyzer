using Microsoft.Build.Construction;

namespace WpfToAvaloniaAnalyzers.MSBuild;

public interface IProjectTransform
{
    string Name { get; }

    bool CanApply(ProjectRootElement root, ProjectTransformContext context);

    TransformResult Apply(ProjectRootElement root, ProjectTransformContext context);
}
