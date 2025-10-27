using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using WpfToAvaloniaAnalyzers.MSBuild.Transforms;

namespace WpfToAvaloniaAnalyzers.MSBuild;

public static class ProjectTransformer
{
    public const string DefaultAvaloniaVersion = "11.3.7";

    private static readonly IReadOnlyList<IProjectTransform> DefaultTransforms = new IProjectTransform[]
    {
        new AvaloniaPackageTransform(),
        new SdkReplacementTransform(),
        new ProjectPropertyAlignmentTransform()
    };

    public static Task<bool> EnsurePackageReferenceAsync(
        string projectPath,
        PackageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path must be provided.", nameof(projectPath));
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var context = new ProjectTransformContext(projectPath, new[] { request });
        return EnsurePackageReferenceCoreAsync(context, cancellationToken);
    }

    public static Task<bool> EnsureAvaloniaPackageReferenceAsync(
        string projectPath,
        string version = DefaultAvaloniaVersion,
        CancellationToken cancellationToken = default) =>
        EnsurePackageReferenceAsync(
            projectPath,
            new PackageRequest("Avalonia", version),
            cancellationToken);

    public static Task<ProjectTransformResult> ApplyTransformsAsync(
        ProjectTransformContext context,
        IReadOnlyList<IProjectTransform>? transforms = null,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        if (!File.Exists(context.ProjectPath))
            throw new FileNotFoundException("Project file not found.", context.ProjectPath);

        var transformSet = transforms ?? DefaultTransforms;
        if (transformSet.Count == 0)
        {
            return Task.FromResult(new ProjectTransformResult(
                false,
                Array.Empty<string>(),
                new[] { "No transforms were provided." }));
        }

        var applied = new List<string>();
        var diagnostics = new List<string>();
        var projectChanged = false;

        using var projectCollection = new ProjectCollection();
        var root = ProjectRootElement.Open(context.ProjectPath, projectCollection, preserveFormatting: true)
                  ?? throw new InvalidOperationException($"Unable to load project '{context.ProjectPath}'.");

        context = ProjectTransformContext.WithDirectoryPackagesProperties(context);

        foreach (var transform in transformSet)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!transform.CanApply(root, context))
            {
                continue;
            }

            var result = transform.Apply(root, context);

            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                diagnostics.Add($"{transform.Name}: {result.Message}");
            }

            if (result.Changed)
            {
                projectChanged = true;
                applied.Add(transform.Name);
            }
        }

        if (projectChanged)
        {
            root.Save();
        }

        return Task.FromResult(new ProjectTransformResult(
            projectChanged,
            applied.ToArray(),
            diagnostics.ToArray()));
    }

    private static async Task<bool> EnsurePackageReferenceCoreAsync(
        ProjectTransformContext context,
        CancellationToken cancellationToken)
    {
        var result = await ApplyTransformsAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
        return result.ProjectChanged;
    }
}
