# MSBuild Transform Guide

The `WpfToAvaloniaAnalyzers.MSBuild` library hosts project-level transformations that prepare WPF projects for Avalonia conversion prior to Roslyn analysis. This guide summarizes the currently available transforms and how to consume them.

## Current Transforms

| Transform | Description |
| --- | --- |
| `AvaloniaPackageTransform` | Ensures `<PackageReference Include="Avalonia" Version="11.3.7" />` (or caller-specified version) is present in the project. Updates the version metadata when the package already exists. |
| `SdkReplacementTransform` | Converts `Microsoft.NET.Sdk.WindowsDesktop` projects to `Microsoft.NET.Sdk` and removes the `<UseWPF>` flag, preparing the project for Avalonia-specific build settings. |
| `ProjectPropertyAlignmentTransform` | Normalizes `<TargetFramework>` (drops `-windows` suffix), ensures `<Nullable>enable</Nullable>`, and sets `<PublishTrimmed>false</PublishTrimmed>`. |

Additional transforms (SDK replacement, property alignment, etc.) will be layered atop the same infrastructure over time.

## Public API

The entry point lives in `ProjectTransformer`:

```csharp
using WpfToAvaloniaAnalyzers.MSBuild;

// Ensure Avalonia package in a specific project.
var changed = await ProjectTransformer.EnsureAvaloniaPackageReferenceAsync(projectPath);

// Ensure an arbitrary package reference.
var changed = await ProjectTransformer.EnsurePackageReferenceAsync(
    projectPath,
    new PackageRequest("Avalonia", "11.3.7"));

// Run all registered transforms explicitly.
var context = new ProjectTransformContext(projectPath, new[] { new PackageRequest("Avalonia", "11.3.7") });
var result = await ProjectTransformer.ApplyTransformsAsync(context);
```

`ProjectTransformResult` reports whether the project was modified, which transforms ran, and any informational diagnostics emitted during the pass.

> **Central package management:** when `Directory.Packages.props` is present, package versions declared via `<PackageVersion Include="..." Version="..." />` take precedence over explicit versions passed to `ProjectTransformer`. This keeps transforms aligned with central NuGet versioning.

## CLI Integration

The CLI runs the transform pipeline automatically before opening the Roslyn workspace:

- When targeting a single `.csproj`, the project file is updated in-place if Avalonia is missing.
- When targeting a solution, every `.csproj` under the solution directory is processed.
- Projects changed by the transform pass are saved before Roslyn re-analysis.

## Custom Usage

External tooling or IDE integrations can consume the same library by referencing `WpfToAvaloniaAnalyzers.MSBuild` and invoking the `ProjectTransformer` methods directly. Transforms operate on top of the MSBuild object model (`Microsoft.Build.Construction`), so any custom transform must update the `ProjectRootElement` safely (preserving formatting and respecting shared `Directory.Packages.props` when possible).

As new transforms land, this guide will be updated with configuration knobs, versioning rules, and extensibility points.
