# MCP Server Configuration Guide

This document describes the configuration options for the WpfToAvalonia MCP Server.

## Configuration File

The server looks for a configuration file named `mcpconfig.json` in the following locations (in order):

1. Current working directory: `./mcpconfig.json`
2. User profile directory: `~/.wpf-to-avalonia/mcpconfig.json`
3. Application directory: `<app-directory>/mcpconfig.json`

You can also specify a custom configuration file path using the `--config` command-line argument:

```bash
wpf-to-avalonia-mcp --config /path/to/custom-config.json
```

## Configuration Schema

The configuration file uses JSON format. See `mcpconfig.schema.json` for the complete JSON schema.

### Example Configuration

```json
{
  "workspaceCache": {
    "enabled": true,
    "maxCachedWorkspaces": 5,
    "idleTimeoutMinutes": 30,
    "maxMemoryMB": 2048
  },
  "timeouts": {
    "workspaceLoadTimeoutSeconds": 120,
    "analysisTimeoutSeconds": 300,
    "codeFixTimeoutSeconds": 600,
    "requestTimeoutSeconds": 900
  },
  "parallelism": {
    "enabled": true,
    "maxDegreeOfParallelism": -1,
    "maxConcurrentWorkspaceOperations": 3
  },
  "logging": {
    "minimumLevel": "Information",
    "writeToFile": true,
    "logFilePath": null,
    "includeDetailedDiagnostics": false
  },
  "security": {
    "maxProjectSizeMB": 1024,
    "maxFileCount": 10000,
    "allowedRootPaths": [],
    "validateFilePaths": true
  }
}
```

## Configuration Options

### Workspace Cache

Controls how the server caches loaded MSBuild workspaces to improve performance.

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `enabled` | boolean | `true` | Whether workspace caching is enabled |
| `maxCachedWorkspaces` | integer | `5` | Maximum number of workspaces to keep in cache |
| `idleTimeoutMinutes` | integer | `30` | Minutes of inactivity before evicting a workspace from cache |
| `maxMemoryMB` | integer | `2048` | Maximum memory usage (MB) before forcing cache eviction |

**Environment Variable Overrides:**
- `WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE_ENABLED=true|false`
- `WPF_TO_AVALONIA_MCP_WORKSPACE_MAX_CACHED=5`
- `WPF_TO_AVALONIA_MCP_WORKSPACE_IDLE_TIMEOUT_MINUTES=30`
- `WPF_TO_AVALONIA_MCP_WORKSPACE_MAX_MEMORY_MB=2048`

### Timeouts

Configures timeout limits for various operations to prevent hung requests.

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `workspaceLoadTimeoutSeconds` | integer | `120` | Timeout for loading a solution/project workspace |
| `analysisTimeoutSeconds` | integer | `300` | Timeout for running analyzers on a project |
| `codeFixTimeoutSeconds` | integer | `600` | Timeout for applying code fixes |
| `requestTimeoutSeconds` | integer | `900` | Overall timeout for an MCP request |

**Environment Variable Overrides:**
- `WPF_TO_AVALONIA_MCP_WORKSPACE_LOAD_TIMEOUT_SECONDS=120`
- `WPF_TO_AVALONIA_MCP_ANALYSIS_TIMEOUT_SECONDS=300`
- `WPF_TO_AVALONIA_MCP_CODEFIX_TIMEOUT_SECONDS=600`
- `WPF_TO_AVALONIA_MCP_REQUEST_TIMEOUT_SECONDS=900`

### Parallelism

Controls concurrent processing behavior.

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `enabled` | boolean | `true` | Whether parallel processing is enabled |
| `maxDegreeOfParallelism` | integer | `-1` | Maximum threads for parallel operations (`-1` = auto-detect CPU cores) |
| `maxConcurrentWorkspaceOperations` | integer | `3` | Maximum number of workspace operations running simultaneously |

**Environment Variable Overrides:**
- `WPF_TO_AVALONIA_MCP_PARALLELISM_ENABLED=true|false`
- `WPF_TO_AVALONIA_MCP_MAX_DEGREE_OF_PARALLELISM=-1`
- `WPF_TO_AVALONIA_MCP_MAX_CONCURRENT_WORKSPACE_OPS=3`

### Logging

Configures server logging behavior.

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `minimumLevel` | string | `"Information"` | Minimum log level: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None` |
| `writeToFile` | boolean | `true` | Whether to write logs to a file |
| `logFilePath` | string? | `null` | Custom log file path (null = `./logs/mcp-server.log`) |
| `includeDetailedDiagnostics` | boolean | `false` | Include detailed diagnostic information (useful for debugging) |

**Environment Variable Overrides:**
- `WPF_TO_AVALONIA_MCP_LOG_LEVEL=Information`
- `WPF_TO_AVALONIA_MCP_LOG_TO_FILE=true|false`
- `WPF_TO_AVALONIA_MCP_LOG_FILE_PATH=/path/to/log.log`
- `WPF_TO_AVALONIA_MCP_LOG_DETAILED_DIAGNOSTICS=true|false`

### Security

Security restrictions to prevent abuse and protect the system.

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `maxProjectSizeMB` | integer | `1024` | Maximum project size (MB) to process |
| `maxFileCount` | integer | `10000` | Maximum number of files in a project to process |
| `allowedRootPaths` | string[] | `[]` | List of allowed root paths for workspace access (empty = all allowed) |
| `validateFilePaths` | boolean | `true` | Validate file paths to prevent path traversal attacks |

**Environment Variable Overrides:**
- `WPF_TO_AVALONIA_MCP_MAX_PROJECT_SIZE_MB=1024`
- `WPF_TO_AVALONIA_MCP_MAX_FILE_COUNT=10000`
- `WPF_TO_AVALONIA_MCP_ALLOWED_ROOT_PATHS=/path1,/path2`
- `WPF_TO_AVALONIA_MCP_VALIDATE_FILE_PATHS=true|false`

## Environment Variable Precedence

Environment variables always override configuration file values. This allows for deployment-specific overrides without modifying the configuration file.

### Example: Override log level via environment variable

```bash
export WPF_TO_AVALONIA_MCP_LOG_LEVEL=Debug
wpf-to-avalonia-mcp
```

## Security Recommendations

### Production Environments

For production or shared environments, consider these security settings:

```json
{
  "security": {
    "maxProjectSizeMB": 512,
    "maxFileCount": 5000,
    "allowedRootPaths": [
      "/home/user/projects",
      "/opt/repositories"
    ],
    "validateFilePaths": true
  },
  "workspaceCache": {
    "maxMemoryMB": 1024,
    "maxCachedWorkspaces": 3
  }
}
```

### Development Environments

For local development, you can use more permissive settings:

```json
{
  "security": {
    "maxProjectSizeMB": 2048,
    "maxFileCount": 20000,
    "allowedRootPaths": [],
    "validateFilePaths": true
  },
  "logging": {
    "minimumLevel": "Debug",
    "includeDetailedDiagnostics": true
  }
}
```

## Performance Tuning

### Large Projects

For very large projects (1000+ files):

```json
{
  "timeouts": {
    "workspaceLoadTimeoutSeconds": 300,
    "analysisTimeoutSeconds": 600,
    "codeFixTimeoutSeconds": 1200
  },
  "workspaceCache": {
    "maxMemoryMB": 4096,
    "idleTimeoutMinutes": 60
  }
}
```

### Limited Resources

For systems with limited CPU/memory:

```json
{
  "parallelism": {
    "enabled": true,
    "maxDegreeOfParallelism": 2,
    "maxConcurrentWorkspaceOperations": 1
  },
  "workspaceCache": {
    "maxMemoryMB": 512,
    "maxCachedWorkspaces": 2
  }
}
```

## Troubleshooting

### Server Times Out During Analysis

Increase timeout values:

```json
{
  "timeouts": {
    "analysisTimeoutSeconds": 600,
    "requestTimeoutSeconds": 1200
  }
}
```

### High Memory Usage

Reduce cache settings:

```json
{
  "workspaceCache": {
    "maxCachedWorkspaces": 2,
    "maxMemoryMB": 1024,
    "idleTimeoutMinutes": 15
  }
}
```

### Slow Performance

Enable parallelism and increase concurrency:

```json
{
  "parallelism": {
    "enabled": true,
    "maxDegreeOfParallelism": -1,
    "maxConcurrentWorkspaceOperations": 5
  }
}
```

### Debugging Issues

Enable detailed logging:

```json
{
  "logging": {
    "minimumLevel": "Debug",
    "includeDetailedDiagnostics": true,
    "writeToFile": true
  }
}
```

Then check the log file at `./logs/mcp-server.log` for detailed diagnostic information.

## Configuration Validation

The server validates all configuration values on startup. If validation fails, the server will:

1. Log detailed error messages explaining what's wrong
2. Exit with a non-zero status code
3. Display validation errors on stderr

Example validation error:

```
Configuration validation failed:
- WorkspaceCache.MaxCachedWorkspaces must be at least 1
- Timeouts.AnalysisTimeoutSeconds must be at least 10
- Logging.MinimumLevel must be one of: Trace, Debug, Information, Warning, Error, Critical, None
```

## Default Configuration

If no configuration file is found, the server uses built-in defaults optimized for typical development scenarios:

- 5 cached workspaces
- 2GB memory limit
- 5-15 minute timeouts
- Parallel processing enabled
- Information-level logging
- No security restrictions

To generate a default configuration file:

```bash
wpf-to-avalonia-mcp --generate-config ./mcpconfig.json
```

---

**Last Updated:** 2025-11-04
**Configuration Version:** 1.0
