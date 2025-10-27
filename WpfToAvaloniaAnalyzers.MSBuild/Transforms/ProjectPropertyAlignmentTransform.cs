using System;
using System.Linq;
using Microsoft.Build.Construction;

namespace WpfToAvaloniaAnalyzers.MSBuild.Transforms;

internal sealed class ProjectPropertyAlignmentTransform : IProjectTransform
{
    private const string TargetFrameworkProperty = "TargetFramework";
    private const string NullableProperty = "Nullable";
    private const string PublishTrimmedProperty = "PublishTrimmed";

    public string Name => "ProjectPropertyAlignment";

    public bool CanApply(ProjectRootElement root, ProjectTransformContext context)
    {
        if (root is null)
            throw new ArgumentNullException(nameof(root));

        return root.PropertyGroups
            .SelectMany(group => group.Properties)
            .Any(property =>
                NeedsTargetFrameworkUpdate(property) ||
                NeedsNullableUpdate(property) ||
                NeedsPublishTrimmedUpdate(property))
            || !HasNullableProperty(root);
    }

    public TransformResult Apply(ProjectRootElement root, ProjectTransformContext context)
    {
        var changed = false;

        foreach (var property in root.PropertyGroups.SelectMany(group => group.Properties))
        {
            if (NeedsTargetFrameworkUpdate(property))
            {
                property.Value = NormalizeTargetFramework(property.Value);
                changed = true;
            }
            else if (NeedsNullableUpdate(property))
            {
                property.Value = "enable";
                changed = true;
            }
            else if (NeedsPublishTrimmedUpdate(property))
            {
                property.Value = "false";
                changed = true;
            }
        }

        if (!HasNullableProperty(root))
        {
            var group = root.PropertyGroups.FirstOrDefault() ?? root.AddPropertyGroup();
            group.AddProperty(NullableProperty, "enable");
            changed = true;
        }

        if (!HasPublishTrimmedProperty(root))
        {
            var group = root.PropertyGroups.FirstOrDefault() ?? root.AddPropertyGroup();
            group.AddProperty(PublishTrimmedProperty, "false");
            changed = true;
        }

        return new TransformResult(
            changed,
            changed
                ? "Aligned target framework, nullable, and trimming settings."
                : "Project properties already aligned.");
    }

    private static bool NeedsTargetFrameworkUpdate(ProjectPropertyElement property)
    {
        if (!string.Equals(property.Name, TargetFrameworkProperty, StringComparison.OrdinalIgnoreCase))
            return false;

        return property.Value != null && property.Value.IndexOf("-windows", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool NeedsNullableUpdate(ProjectPropertyElement property)
    {
        return string.Equals(property.Name, NullableProperty, StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(property.Value, "enable", StringComparison.OrdinalIgnoreCase);
    }

    private static bool NeedsPublishTrimmedUpdate(ProjectPropertyElement property)
    {
        return string.Equals(property.Name, PublishTrimmedProperty, StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(property.Value, "false", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasNullableProperty(ProjectRootElement root) =>
        root.PropertyGroups.SelectMany(group => group.Properties)
            .Any(property => string.Equals(property.Name, NullableProperty, StringComparison.OrdinalIgnoreCase));

    private static bool HasPublishTrimmedProperty(ProjectRootElement root) =>
        root.PropertyGroups.SelectMany(group => group.Properties)
            .Any(property => string.Equals(property.Name, PublishTrimmedProperty, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeTargetFramework(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "net8.0";

        var index = value.IndexOf("-windows", StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return value;

        return value.Substring(0, index);
    }
}
