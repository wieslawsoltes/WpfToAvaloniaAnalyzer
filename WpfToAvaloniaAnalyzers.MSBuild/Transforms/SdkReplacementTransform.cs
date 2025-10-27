using System;
using System.Linq;
using Microsoft.Build.Construction;

namespace WpfToAvaloniaAnalyzers.MSBuild.Transforms;

internal sealed class SdkReplacementTransform : IProjectTransform
{
    private const string WpfSdk = "Microsoft.NET.Sdk.WindowsDesktop";
    private const string NetSdk = "Microsoft.NET.Sdk";

    public string Name => "SdkReplacement";

    public bool CanApply(ProjectRootElement root, ProjectTransformContext context)
    {
        if (root is null)
            throw new ArgumentNullException(nameof(root));

        return string.Equals(root.Sdk, WpfSdk, StringComparison.OrdinalIgnoreCase) ||
               root.PropertyGroups
                   .SelectMany(group => group.Properties)
                   .Any(property => string.Equals(property.Name, "UseWPF", StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(property.Value, "true", StringComparison.OrdinalIgnoreCase));
    }

    public TransformResult Apply(ProjectRootElement root, ProjectTransformContext context)
    {
        var changed = false;

        if (string.Equals(root.Sdk, WpfSdk, StringComparison.OrdinalIgnoreCase))
        {
            root.Sdk = NetSdk;
            changed = true;
        }

        foreach (var property in root.PropertyGroups
                     .SelectMany(group => group.Properties)
                     .Where(property => string.Equals(property.Name, "UseWPF", StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            property.Parent?.RemoveChild(property);
            changed = true;
        }

        return new TransformResult(
            changed,
            changed
                ? "Updated SDK to Microsoft.NET.Sdk and removed UseWPF flag."
                : "Project SDK already compatible with Avalonia.");
    }
}
