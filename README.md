# WpfToAvaloniaAnalyzers

Roslyn analyzers and code fixes that accelerate migrating existing WPF code to Avalonia UI.

## Overview

The analyzers flag WPF‑specific APIs, `DependencyProperty` patterns, and other migration pain points. Each diagnostic ships with a fixer that rewrites the code to idiomatic Avalonia equivalents—including property registrations, class handlers, and file‑wide conversions.

## Highlights

- **Incremental & batch fixes** – Apply individual fixes or run the `WpfToAvaloniaFile` fixer to transform an entire file in one pass.
- **DependencyProperty conversion** – Automatically converts `DependencyProperty.Register` calls to `AvaloniaProperty.Register`, rewrites accessors, and adds Avalonia class handlers.
- **Callback upgrades** – Updates `DependencyObject/DependencyPropertyChangedEventArgs` callbacks to Avalonia signatures, creating static constructors and handler wiring when required.
- **Guided migration** – Diagnostics explain the WPF concept being replaced so you can review each change with context.

## Getting Started

Install the analyzer package into the project you want to migrate:

```bash
dotnet add package WpfToAvaloniaAnalyzers
```

Build the project (or run the IDE code analysis) to see diagnostics. Use your IDE’s light bulb/code fix menu to apply changes, or let the Fix All provider convert files and projects in bulk.

## Key Diagnostics & Fixers

| Id | Title | Summary |
| --- | --- | --- |
| `WA001`–`WA006` | WPF usage helpers | Remove WPF usings, replace casts, update base classes, and convert property metadata and callbacks. |
| `WA007` | Apply all analyzers in file | Adds a single diagnostic per file so Fix All can apply every available migration in one shot. |
| `WA008` | Convert `PropertyMetadata` callbacks to class handlers | Generates static constructors, adds `Property.Changed.AddClassHandler`, and converts callback signatures to Avalonia patterns. |

See `WpfToAvaloniaAnalyzers/DiagnosticDescriptors.cs` for the complete catalog.

## Project Layout

- `WpfToAvaloniaAnalyzers` – Analyzer implementations and diagnostic descriptors.
- `WpfToAvaloniaAnalyzers.CodeFixes` – All fixers plus reusable services (e.g., `ClassHandlerService`, batch fixer pipeline).
- `WpfToAvaloniaAnalyzers.Tests` – Analyzer/code-fix regression tests. Helpers support optional compiler diagnostics so Avalonia references can be validated.
- `WpfToAvaloniaAnalyzers.Sample.Wpf` – Simple WPF app for manual migration experiments.
- `extern` – Vendored Avalonia sources referenced by the analyzers.

## Build & Test

```bash
dotnet build WpfToAvaloniaAnalyzers.sln
dotnet test  WpfToAvaloniaAnalyzers.sln
```

The test suite uses the shared `CodeFixTestHelper`, which now accepts a `CompilerDiagnostics` flag; enabling `CompilerDiagnostics.Errors` ensures fixed states compile against the provided Avalonia reference assemblies.

## Packaging

Generate an updated NuGet package after validating the tests:

```bash
dotnet pack WpfToAvaloniaAnalyzers.NuGet/WpfToAvaloniaAnalyzers.NuGet.csproj
```

## Contributing

Issues and pull requests are welcome! If you are extending the analyzers:

1. Add or update diagnostics in `DiagnosticDescriptors`.
2. Cover the behavior with analyzer and code-fix tests (enable compiler diagnostics when the fix introduces Avalonia types).
3. Update this README and the sample project if new migration scenarios are supported.

## License

MIT License – see [`LICENSE`](LICENSE) for details.
