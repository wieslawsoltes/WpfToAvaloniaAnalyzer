# WpfToAvaloniaAnalyzers

A Roslyn analyzer to help migrate WPF applications to Avalonia UI.

## Overview

This analyzer helps identify WPF-specific code patterns and suggests Avalonia equivalents to streamline the migration process from WPF to Avalonia UI.

## Features

- **Code Analysis**: Detects WPF control usage and suggests Avalonia alternatives
- **Code Fixes**: Provides automated code fixes where possible
- **Migration Guidance**: Helps identify areas that need attention during migration

## Installation

Install the NuGet package in your WPF project:

```bash
dotnet add package WpfToAvaloniaAnalyzers
```

## Project Structure

- **WpfToAvaloniaAnalyzers**: Main analyzer project containing diagnostic analyzers
- **WpfToAvaloniaAnalyzers.CodeFixes**: Code fix providers for automated fixes
- **WpfToAvaloniaAnalyzers.Tests**: Unit tests for analyzers and code fixes
- **WpfToAvaloniaAnalyzers.Package**: NuGet package project

## Building

```bash
dotnet build WpfToAvaloniaAnalyzers.sln
```

## Testing

```bash
dotnet test WpfToAvaloniaAnalyzers.sln
```

## Creating a NuGet Package

```bash
dotnet pack WpfToAvaloniaAnalyzers.Package/WpfToAvaloniaAnalyzers.Package.csproj
```

## Diagnostics

### WPF001: Use Avalonia control instead of WPF control

Identifies usage of WPF controls and suggests using Avalonia equivalents.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

MIT License - see LICENSE file for details.
