using System;
using System.Linq;
using Microsoft.Build.Construction;

namespace WpfToAvaloniaAnalyzers.MSBuild.Transforms;

internal sealed class AvaloniaPackageTransform : IProjectTransform
{
    public string Name => "AvaloniaPackageReference";

    public bool CanApply(ProjectRootElement root, ProjectTransformContext context)
    {
        if (root is null)
            throw new ArgumentNullException(nameof(root));
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        return context.PackageRequests.Any(request =>
            string.Equals(request.Id, "Avalonia", StringComparison.OrdinalIgnoreCase));
    }

    public TransformResult Apply(ProjectRootElement root, ProjectTransformContext context)
    {
        var request = context.PackageRequests.FirstOrDefault(r =>
            string.Equals(r.Id, "Avalonia", StringComparison.OrdinalIgnoreCase));

        if (request is null)
        {
            return new TransformResult(false, "No Avalonia package request found.");
        }

        var resolvedVersion = ResolveVersion(request, context.DirectoryPackagesProps);

        var existing = root.Items
            .Where(item => string.Equals(item.ItemType, "PackageReference", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(item => string.Equals(item.Include, request.Id, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return UpdateExistingReferenceIfNeeded(existing, resolvedVersion);
        }

        var targetGroup = root.Items
            .Where(item => string.Equals(item.ItemType, "PackageReference", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Parent as ProjectItemGroupElement)
            .FirstOrDefault(group => group != null);

        targetGroup ??= root.AddItemGroup();

        var newItem = targetGroup.AddItem("PackageReference", request.Id);
        if (!string.IsNullOrWhiteSpace(resolvedVersion))
        {
            newItem.AddMetadata("Version", resolvedVersion, expressAsAttribute: true);
        }

        return new TransformResult(true, $"Added Avalonia package reference ({resolvedVersion ?? "no version specified"}).");
    }

    private static TransformResult UpdateExistingReferenceIfNeeded(ProjectItemElement existing, string? resolvedVersion)
    {
        if (string.IsNullOrWhiteSpace(resolvedVersion))
        {
            return new TransformResult(false, "Avalonia package already referenced.");
        }

        var versionMetadata = existing.Metadata.FirstOrDefault(metadata =>
            string.Equals(metadata.Name, "Version", StringComparison.OrdinalIgnoreCase));

        if (versionMetadata is null)
        {
            existing.AddMetadata("Version", resolvedVersion, expressAsAttribute: true);
            return new TransformResult(true, $"Added version metadata for Avalonia ({resolvedVersion}).");
        }

        if (!string.Equals(versionMetadata.Value, resolvedVersion, StringComparison.OrdinalIgnoreCase))
        {
            versionMetadata.Value = resolvedVersion;
            return new TransformResult(true, $"Updated Avalonia package version to {resolvedVersion}.");
        }

        return new TransformResult(false, "Avalonia package already referenced with correct version.");
    }

    private static string? ResolveVersion(PackageRequest request, DirectoryPackagesProps? directoryPackages)
    {
        if (directoryPackages is not null && directoryPackages.TryGetVersion(request.Id, out var version))
        {
            return version;
        }

        if (!string.IsNullOrWhiteSpace(request.Version))
        {
            return request.Version;
        }

        return null;
    }
}
