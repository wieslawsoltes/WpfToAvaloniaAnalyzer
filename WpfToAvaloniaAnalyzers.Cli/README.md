# WpfToAvaloniaAnalyzers CLI

Command‑line runner for the WPF → Avalonia Roslyn analyzers and code fixes.

## Prerequisites

- .NET SDK 9.0 or newer (repo ships a `global.json` that pins `9.0.100`).
- The solution/projects you want to analyze must restore/build with MSBuild.

## Building

```bash
dotnet build ../WpfToAvaloniaAnalyzers.sln
```

The build produces `WpfToAvaloniaAnalyzers.Cli.dll` under `bin/Debug/net9.0/`.

## Usage

```
dotnet run --project WpfToAvaloniaAnalyzers.Cli -- --path <solution|project> [options]
```

### Options

| Option | Description |
| --- | --- |
| `--path`, `-p` | Path to a `.sln` or `.csproj` file (required). |
| `--scope`, `-s` | Scope for fixes: `document`, `project`, or `solution`. Defaults based on other options (document if `--document`, project if a single project is open/selected, otherwise solution). |
| `--project` | Project name when running against a solution (needed if ambiguous). |
| `--document`, `-d` | Full path to a document for document scope. |
| `--diagnostic`, `--diagnostics`, `-id` | Diagnostic IDs to fix (comma separated or repeated). Omitting fixes everything the bundled providers support. |
| `--code-action`, `-a` | Filter code actions by title (exact or contains match). |
| `--mode` | Execution strategy: `sequential` (default), `parallel`, or `fixall`. |

### Examples

Fix every supported diagnostic across the solution:

```bash
dotnet run --project WpfToAvaloniaAnalyzers.Cli -- \
  --path ../WpfToAvaloniaAnalyzers.sln \
  --scope solution
```

Fix specific diagnostics in a single project:

```bash
dotnet run --project WpfToAvaloniaAnalyzers.Cli -- \
  --path ../WpfToAvaloniaAnalyzers.sln \
  --scope project \
  --project WpfToAvaloniaAnalyzers.Sample.Wpf \
  --diagnostics WA002,WA004
```

Fix a document using a particular code action:

```bash
dotnet run --project WpfToAvaloniaAnalyzers.Cli -- \
  --path ../WpfToAvaloniaAnalyzers.Sample.Wpf/WpfToAvaloniaAnalyzers.Sample.Wpf.csproj \
  --scope document \
  --document ../WpfToAvaloniaAnalyzers.Sample.Wpf/SampleControl.cs \
  --code-action "Remove WPF usings"
```

### Exit Codes

- `0` – Fixes applied successfully or no matching diagnostics found.
- `1` – Invalid arguments, failed to load the workspace, or other unrecoverable errors.

### Getting Help

```
dotnet run --project WpfToAvaloniaAnalyzers.Cli -- --help
```
