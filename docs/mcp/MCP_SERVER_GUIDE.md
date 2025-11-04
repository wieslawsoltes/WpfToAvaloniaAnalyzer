# WpfToAvalonia MCP Server Guide

Complete guide for using the WpfToAvalonia Model Context Protocol (MCP) server with AI coding agents.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Configuration](#configuration)
4. [Tool Catalog](#tool-catalog)
5. [Usage Examples](#usage-examples)
6. [Troubleshooting](#troubleshooting)
7. [Advanced Topics](#advanced-topics)

---

## Overview

The WpfToAvalonia MCP Server enables AI coding agents (like Claude, GitHub Copilot, etc.) to orchestrate WPF-to-Avalonia migrations through a standardized protocol. The server provides 10 tools covering analysis, transformation, and validation.

### Features

- **19 Roslyn Analyzers**: Detect WPF patterns that need conversion
- **19 Code Fix Providers**: Automatically apply Avalonia equivalents
- **Workspace Management**: Efficient caching and reuse of loaded projects
- **Progress Reporting**: Real-time feedback on long-running operations
- **Batch Processing**: Convert entire projects or solutions at once
- **Validation**: Pre-flight checks for migration readiness

### Architecture

```
AI Agent (Claude, Copilot, etc.)
    ↓
MCP Protocol (JSON-RPC over stdio)
    ↓
WpfToAvalonia MCP Server
    ↓
Roslyn APIs (Analyzers, CodeFixes, MSBuildWorkspace)
    ↓
Your WPF Project
```

---

## Installation

### Prerequisites

- .NET 9.0 SDK or later
- MSBuild (usually included with Visual Studio or .NET SDK)
- Git (for cloning source)

### Option 1: Install from Source

```bash
# Clone the repository
git clone https://github.com/wieslawsoltes/WpfToAvaloniaAnalyzer.git
cd WpfToAvaloniaAnalyzer

# Build and pack
dotnet pack src/WpfToAvaloniaAnalyzers.Mcp/WpfToAvaloniaAnalyzers.Mcp.csproj

# Install as global tool
dotnet tool install --global --add-source src/WpfToAvaloniaAnalyzers.Mcp/bin/Debug WpfToAvaloniaAnalyzers.Mcp
```

### Option 2: Install from NuGet (when published)

```bash
dotnet tool install --global WpfToAvaloniaAnalyzers.Mcp
```

### Verify Installation

```bash
wpf-to-avalonia-mcp --help
```

---

## Configuration

### Configuration File

Create `mcpconfig.json` in one of these locations:
- Current directory: `./mcpconfig.json`
- User profile: `~/.wpf-to-avalonia/mcpconfig.json`
- Application directory: `<app-dir>/mcpconfig.json`

### Default Configuration

```json
{
  "workspaceCache": {
    "enabled": true,
    "maxWorkspaces": 10,
    "idleTimeoutMinutes": 30,
    "enableFileWatching": true,
    "debounceDelaySeconds": 2
  },
  "timeouts": {
    "workspaceLoadTimeoutSeconds": 300,
    "analysisTimeoutSeconds": 120,
    "codeFixTimeoutSeconds": 60,
    "requestTimeoutSeconds": 600
  },
  "parallelism": {
    "enabled": true,
    "maxDegreeOfParallelism": 4
  },
  "logging": {
    "minimumLevel": "Information",
    "writeToFile": false,
    "logFilePath": null
  },
  "security": {
    "maxProjectSizeMB": 1000,
    "allowedPaths": []
  }
}
```

### Environment Variables

Override configuration using environment variables with `WPF_TO_AVALONIA_MCP_` prefix:

```bash
# Example: Set log level
export WPF_TO_AVALONIA_MCP_LOGGING__MINIMUMLEVEL=Debug

# Example: Disable workspace cache
export WPF_TO_AVALONIA_MCP_WORKSPACECACHE__ENABLED=false

# Example: Set analysis timeout
export WPF_TO_AVALONIA_MCP_TIMEOUTS__ANALYSISTIMEOUTSECONDS=180
```

### AI Agent Configuration

#### Claude Desktop

Add to `~/.claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": ["--config", "/path/to/mcpconfig.json"]
    }
  }
}
```

#### Generic MCP Client

```json
{
  "servers": [
    {
      "name": "wpf-to-avalonia",
      "command": "wpf-to-avalonia-mcp",
      "args": ["--config", "mcpconfig.json"],
      "cwd": "/path/to/your/project"
    }
  ]
}
```

---

## Tool Catalog

### Analysis Tools

#### 1. `wpf-analyze-project`

Analyzes a WPF project or solution for Avalonia conversion opportunities.

**Input:**
```json
{
  "projectPath": "/path/to/project.csproj",
  "diagnosticIds": ["WPFAV001", "WPFAV002"],
  "minimumSeverity": "Warning"
}
```

**Output:**
```json
{
  "success": true,
  "projectName": "MyWpfApp",
  "projectCount": 3,
  "totalDiagnostics": 47,
  "diagnosticsBySeverity": {
    "Warning": 42,
    "Error": 5
  },
  "diagnosticsById": {
    "WPFAV001": 32,
    "WPFAV002": 15
  },
  "diagnostics": [
    {
      "id": "WPFAV001",
      "severity": "Warning",
      "message": "DependencyProperty should be converted to StyledProperty",
      "filePath": "/path/to/File.cs",
      "lineNumber": 42,
      "column": 15,
      "projectName": "MyWpfApp"
    }
  ]
}
```

**Use Cases:**
- Initial project assessment
- Identify conversion scope
- Track migration progress

---

#### 2. `wpf-list-analyzers`

Lists all available WPF-to-Avalonia analyzers.

**Input:** None

**Output:**
```json
{
  "success": true,
  "totalAnalyzers": 19,
  "analyzers": [
    {
      "id": "WPFAV001",
      "title": "DependencyProperty Analyzer",
      "category": "DependencyProperty",
      "defaultSeverity": "Warning",
      "description": "Detects WPF DependencyProperty usage...",
      "helpLinkUri": "https://..."
    }
  ]
}
```

**Use Cases:**
- Discover available analyzers
- Understand diagnostic categories
- Filter analysis by specific patterns

---

### Transformation Tools

#### 3. `wpf-apply-fix`

Applies a code fix to a specific diagnostic.

**Input:**
```json
{
  "projectPath": "/path/to/project.csproj",
  "filePath": "/path/to/File.cs",
  "diagnosticId": "WPFAV001",
  "line": 42,
  "column": 15,
  "fixIndex": 0
}
```

**Output:**
```json
{
  "success": true,
  "appliedFixTitle": "Convert to Avalonia StyledProperty",
  "modifiedFiles": ["/path/to/File.cs"],
  "availableFixes": [
    {"index": 0, "title": "Convert to Avalonia StyledProperty"}
  ],
  "diffs": [
    {
      "filePath": "/path/to/File.cs",
      "unifiedDiff": "--- a/File.cs\n+++ b/File.cs\n...",
      "linesAdded": 5,
      "linesRemoved": 3
    }
  ]
}
```

**Use Cases:**
- Apply targeted fixes
- Review changes before committing
- Fix specific issues identified by analysis

---

#### 4. `wpf-batch-convert`

Applies code fixes to multiple diagnostics in a project.

**Input:**
```json
{
  "projectPath": "/path/to/solution.sln",
  "diagnosticIds": ["WPFAV001", "WPFAV002"],
  "targetFiles": ["/path/to/File1.cs"],
  "dryRun": false
}
```

**Output:**
```json
{
  "success": true,
  "fixedDiagnostics": 47,
  "modifiedFiles": ["/path/to/File1.cs", "/path/to/File2.cs"],
  "failedFixes": [],
  "summaryByDiagnosticId": {
    "WPFAV001": 32,
    "WPFAV002": 15
  },
  "dryRun": false
}
```

**Use Cases:**
- Convert entire projects at once
- Preview changes with dry-run
- Automate large-scale migrations

---

#### 5. `wpf-preview-fixes`

Gets available code fixes without applying them.

**Input:**
```json
{
  "projectPath": "/path/to/project.csproj",
  "filePath": "/path/to/File.cs",
  "diagnosticId": "WPFAV001",
  "line": 42,
  "column": 15
}
```

**Output:**
```json
{
  "success": true,
  "availableFixes": [
    {"index": 0, "title": "Convert to Avalonia StyledProperty"},
    {"index": 1, "title": "Convert to Avalonia DirectProperty"}
  ]
}
```

**Use Cases:**
- Explore fix options
- Choose between multiple fixes
- Understand what changes will be made

---

### Utility Tools

#### 6. `wpf-get-diagnostic-info`

Gets detailed information about a diagnostic.

**Input:**
```json
{
  "diagnosticId": "WPFAV001"
}
```

**Output:**
```json
{
  "success": true,
  "id": "WPFAV001",
  "title": "Convert DependencyProperty to StyledProperty",
  "category": "DependencyProperty",
  "defaultSeverity": "Warning",
  "description": "WPF DependencyProperty should be converted...",
  "examples": "WPF Code:\npublic static readonly DependencyProperty...",
  "migrationGuide": "Migration Guide for DependencyProperty:\n1. Change DependencyProperty.Register..."
}
```

**Use Cases:**
- Learn about specific diagnostics
- Get migration guidance
- See before/after examples

---

#### 7. `wpf-validate-project`

Validates a project for migration readiness.

**Input:**
```json
{
  "projectPath": "/path/to/project.csproj"
}
```

**Output:**
```json
{
  "success": true,
  "projectPath": "/path/to/project.csproj",
  "pathExists": true,
  "canLoadWorkspace": true,
  "projectCount": 3,
  "hasWpfProjects": true,
  "hasAvaloniaProjects": false,
  "hasCompilationErrors": false,
  "totalCompilationErrors": 0,
  "projects": [
    {
      "name": "MyApp",
      "language": "C#",
      "documentCount": 45,
      "canCompile": true,
      "hasWpfReferences": true,
      "wpfReferences": ["PresentationFramework.dll"]
    }
  ],
  "recommendations": [
    "Project is ready for WPF to Avalonia migration."
  ]
}
```

**Use Cases:**
- Pre-flight checks
- Verify project can be loaded
- Identify blockers before starting

---

### Infrastructure Tools

#### 8. `get-server-info`

Gets server metadata and capabilities.

**Input:** None

**Output:**
```json
{
  "name": "WpfToAvalonia MCP Server",
  "version": "0.1.0-beta.1",
  "description": "MCP server for WPF to Avalonia migration",
  "capabilities": [
    "Analysis",
    "Transformation",
    "Validation"
  ],
  "configuration": {
    "workspaceCache": {"enabled": true}
  }
}
```

---

#### 9. `get-workspace-cache-stats`

Gets workspace cache statistics.

**Input:** None

**Output:**
```json
{
  "cacheEnabled": true,
  "cachedWorkspaceCount": 3,
  "maxWorkspaces": 10
}
```

---

#### 10. `clear-workspace-cache`

Clears the workspace cache.

**Input:** None

**Output:**
```json
{
  "success": true,
  "clearedCount": 3
}
```

---

## Usage Examples

### Example 1: Basic Migration Workflow

```typescript
// 1. Validate the project
const validation = await mcp.call("wpf-validate-project", {
  projectPath: "/path/to/MyWpfApp.csproj"
});

if (!validation.success) {
  console.error("Validation failed:", validation.error);
  return;
}

// 2. Analyze the project
const analysis = await mcp.call("wpf-analyze-project", {
  projectPath": "/path/to/MyWpfApp.csproj",
  minimumSeverity: "Warning"
});

console.log(`Found ${analysis.totalDiagnostics} issues to fix`);

// 3. Get info about a specific diagnostic
const info = await mcp.call("wpf-get-diagnostic-info", {
  diagnosticId: "WPFAV001"
});

console.log("Migration guide:", info.migrationGuide);

// 4. Apply fixes in dry-run mode
const dryRun = await mcp.call("wpf-batch-convert", {
  projectPath: "/path/to/MyWpfApp.csproj",
  diagnosticIds: ["WPFAV001"],
  dryRun: true
});

console.log(`Would fix ${dryRun.fixedDiagnostics} issues`);

// 5. Apply fixes for real
const result = await mcp.call("wpf-batch-convert", {
  projectPath: "/path/to/MyWpfApp.csproj",
  diagnosticIds: ["WPFAV001"],
  dryRun: false
});

console.log(`Fixed ${result.fixedDiagnostics} issues in ${result.modifiedFiles.length} files`);

// 6. Re-analyze to verify
const reanalysis = await mcp.call("wpf-analyze-project", {
  projectPath: "/path/to/MyWpfApp.csproj",
  diagnosticIds: ["WPFAV001"]
});

console.log(`Remaining issues: ${reanalysis.totalDiagnostics}`);
```

### Example 2: Targeted Fix Application

```typescript
// Find a specific issue
const analysis = await mcp.call("wpf-analyze-project", {
  projectPath: "/path/to/project.csproj"
});

const diagnostic = analysis.diagnostics[0];

// Preview available fixes
const preview = await mcp.call("wpf-preview-fixes", {
  projectPath: "/path/to/project.csproj",
  filePath: diagnostic.filePath,
  diagnosticId: diagnostic.id,
  line: diagnostic.lineNumber,
  column: diagnostic.column
});

console.log("Available fixes:", preview.availableFixes);

// Apply the first fix
const fix = await mcp.call("wpf-apply-fix", {
  projectPath: "/path/to/project.csproj",
  filePath: diagnostic.filePath,
  diagnosticId: diagnostic.id,
  line: diagnostic.lineNumber,
  column: diagnostic.column,
  fixIndex: 0
});

console.log("Applied:", fix.appliedFixTitle);
console.log("Diff:", fix.diffs[0].unifiedDiff);
```

### Example 3: Progressive Migration

```typescript
// Get all analyzers
const analyzers = await mcp.call("wpf-list-analyzers", {});

// Migrate one category at a time
for (const analyzer of analyzers.analyzers) {
  if (analyzer.category === "DependencyProperty") {
    console.log(`Migrating: ${analyzer.title}`);

    const result = await mcp.call("wpf-batch-convert", {
      projectPath: "/path/to/project.csproj",
      diagnosticIds: [analyzer.id],
      dryRun: false
    });

    console.log(`  Fixed: ${result.fixedDiagnostics}`);
    console.log(`  Failed: ${result.failedFixes.length}`);
  }
}
```

---

## Troubleshooting

### Common Issues

#### 1. Workspace Load Failures

**Symptom:** `wpf-validate-project` returns `canLoadWorkspace: false`

**Solutions:**
- Ensure MSBuild is installed and in PATH
- Check that .csproj/.sln file is valid
- Verify all NuGet packages are restored
- Increase `workspaceLoadTimeoutSeconds` in config

```bash
# Restore packages first
dotnet restore YourProject.sln

# Check MSBuild
dotnet msbuild --version
```

#### 2. Analysis Timeouts

**Symptom:** `wpf-analyze-project` throws timeout error

**Solutions:**
- Increase `analysisTimeoutSeconds` in configuration
- Analyze smaller scopes (single project instead of solution)
- Filter by specific diagnostic IDs

```json
{
  "timeouts": {
    "analysisTimeoutSeconds": 300
  }
}
```

#### 3. Code Fix Application Fails

**Symptom:** `wpf-apply-fix` returns `success: false`

**Solutions:**
- Ensure file hasn't changed since analysis
- Check that diagnostic still exists at location
- Verify workspace is not locked by another process
- Try clearing workspace cache

```typescript
// Clear cache and retry
await mcp.call("clear-workspace-cache", {});
await mcp.call("wpf-apply-fix", { /* ... */ });
```

#### 4. Compilation Errors After Fixes

**Symptom:** Project doesn't compile after applying fixes

**Solutions:**
- Add Avalonia NuGet packages
- Update using directives
- Review failed fixes in batch convert result
- Apply fixes incrementally and test

```bash
# Add Avalonia packages
dotnet add package Avalonia
dotnet add package Avalonia.Desktop
```

#### 5. Memory Issues with Large Projects

**Symptom:** Server crashes or becomes unresponsive

**Solutions:**
- Reduce `maxWorkspaces` in configuration
- Process projects one at a time
- Disable file watching for large solutions
- Increase available memory

```json
{
  "workspaceCache": {
    "maxWorkspaces": 3,
    "enableFileWatching": false
  }
}
```

### Logging

Enable detailed logging for troubleshooting:

```json
{
  "logging": {
    "minimumLevel": "Debug",
    "writeToFile": true,
    "logFilePath": "/path/to/logs/mcp-server.log"
  }
}
```

Or via environment variable:

```bash
export WPF_TO_AVALONIA_MCP_LOGGING__MINIMUMLEVEL=Debug
export WPF_TO_AVALONIA_MCP_LOGGING__WRITETOFILE=true
export WPF_TO_AVALONIA_MCP_LOGGING__LOGFILEPATH=/tmp/mcp.log
```

### Getting Help

- **GitHub Issues**: https://github.com/wieslawsoltes/WpfToAvaloniaAnalyzer/issues
- **Documentation**: https://github.com/wieslawsoltes/WpfToAvaloniaAnalyzer/tree/main/docs
- **Avalonia Docs**: https://docs.avaloniaui.net/

---

## Advanced Topics

### Performance Optimization

**Workspace Caching:**
- Keep `enabled: true` for repeated analysis
- Adjust `maxWorkspaces` based on available memory
- Use `idleTimeoutMinutes` to free resources

**Parallelism:**
- Enable for batch operations
- Adjust `maxDegreeOfParallelism` based on CPU cores
- Disable for debugging

**File Watching:**
- Disable for CI/CD environments
- Use `debounceDelaySeconds` to avoid reload thrashing
- Essential for interactive development

### Security Considerations

**Allowed Paths:**
```json
{
  "security": {
    "allowedPaths": [
      "/home/user/projects",
      "/workspace"
    ]
  }
}
```

**Project Size Limits:**
```json
{
  "security": {
    "maxProjectSizeMB": 500,
    "maxFilesPerProject": 10000
  }
}
```

### Custom Workflows

**Pre-commit Hook:**
```bash
#!/bin/bash
# Validate before commit
wpf-to-avalonia-mcp validate-project MyProject.csproj
if [ $? -ne 0 ]; then
  echo "Project validation failed"
  exit 1
fi
```

**CI/CD Integration:**
```yaml
# GitHub Actions example
- name: Analyze WPF Issues
  run: |
    dotnet tool install -g WpfToAvaloniaAnalyzers.Mcp
    wpf-to-avalonia-mcp analyze-project MyProject.csproj --output report.json

- name: Upload Report
  uses: actions/upload-artifact@v2
  with:
    name: wpf-analysis
    path: report.json
```

### Extending the Server

The MCP server can be extended with custom analyzers and code fixes:

1. Create analyzer in `WpfToAvaloniaAnalyzers` project
2. Create code fix in `WpfToAvaloniaAnalyzers.CodeFixes` project
3. Rebuild and reinstall the MCP server
4. New diagnostics automatically available

---

## Appendix

### Diagnostic ID Reference

| ID | Category | Description |
|----|----------|-------------|
| WPFAV001 | DependencyProperty | Convert DependencyProperty to StyledProperty |
| WPFAV002 | AttachedProperty | Convert attached property registration |
| WPFAV003 | RoutedEvent | Convert RoutedEvent registration |
| WPFAV004 | BaseClass | Update WPF base class to Avalonia |
| WPFAV005 | UsingDirectives | Update using directives |
| ... | ... | (See `wpf-list-analyzers` for complete list) |

### Configuration Schema

See `mcpconfig.schema.json` for complete JSON schema with IntelliSense support.

### Supported WPF Patterns

- DependencyProperty → StyledProperty
- Attached Properties
- RoutedEvents
- Base Classes (Window, UserControl, etc.)
- Property Metadata
- PropertyChanged Callbacks
- Coerce Callbacks
- Validation Callbacks
- Framework Property Metadata
- Class Handlers
- Event Handlers

### Version History

- **0.1.0-beta.1** (2025-01): Initial release
  - 19 analyzers
  - 19 code fixes
  - 10 MCP tools
  - Workspace management
  - Progress reporting

---

*Last updated: 2025-01*
