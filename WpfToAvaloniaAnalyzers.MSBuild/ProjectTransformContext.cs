using System;
using System.Collections.Generic;
using System.IO;

namespace WpfToAvaloniaAnalyzers.MSBuild;

/// <summary>
/// Carries shared state used by MSBuild project transforms.
/// </summary>
public sealed class ProjectTransformContext
{
    public ProjectTransformContext(
        string projectPath,
        IReadOnlyList<PackageRequest>? packageRequests = null,
        DirectoryPackagesProps? directoryPackagesProps = null)
    {
        ProjectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
        PackageRequests = packageRequests ?? Array.Empty<PackageRequest>();
        DirectoryPackagesProps = directoryPackagesProps;
    }

    /// <summary>
    /// Absolute path to the MSBuild project.
    /// </summary>
    public string ProjectPath { get; }

    /// <summary>
    /// Packages that should be ensured within the project.
    /// </summary>
    public IReadOnlyList<PackageRequest> PackageRequests { get; }

    /// <summary>
    /// Optional representation of Directory.Packages.props settings.
    /// </summary>
    public DirectoryPackagesProps? DirectoryPackagesProps { get; }

    public static ProjectTransformContext WithDirectoryPackagesProperties(ProjectTransformContext context)
    {
        if (context.DirectoryPackagesProps is not null)
        {
            return context;
        }

        var directory = Path.GetDirectoryName(context.ProjectPath);
        while (!string.IsNullOrEmpty(directory))
        {
            var propsPath = Path.Combine(directory, "Directory.Packages.props");
            if (File.Exists(propsPath))
            {
                return new ProjectTransformContext(
                    context.ProjectPath,
                    context.PackageRequests,
                    DirectoryPackagesProps.Load(propsPath));
            }

            var parent = Directory.GetParent(directory);
            if (parent is null)
            {
                break;
            }

            directory = parent.FullName;
        }

        return context;
    }
}
