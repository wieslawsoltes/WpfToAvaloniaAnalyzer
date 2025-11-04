# Agent Configuration Reference

This document provides comprehensive configuration formats for integrating the WpfToAvalonia MCP Server with various AI agents and development environments.

## Overview

The WpfToAvalonia MCP Server can be integrated with multiple AI agents and tools. This reference covers:

- Configuration file formats for different platforms
- Environment variable configurations
- Server command-line arguments
- Common configuration patterns
- Platform-specific requirements

## Configuration File Locations

### Claude Desktop

**macOS:**
```
~/Library/Application Support/Claude/claude_desktop_config.json
```

**Windows:**
```
%APPDATA%\Claude\claude_desktop_config.json
```

**Linux:**
```
~/.config/Claude/claude_desktop_config.json
```

### GitHub Copilot

**VS Code Workspace:**
```
.vscode/settings.json
```

**VS Code User:**
```
~/.config/Code/User/settings.json  (Linux/macOS)
%APPDATA%\Code\User\settings.json  (Windows)
```

**Global MCP Config (if supported):**
```
~/.config/github-copilot/mcp-servers.json  (Linux/macOS)
%APPDATA%\GitHub Copilot\mcp-servers.json  (Windows)
```

### VS Code MCP Extension

**Workspace:**
```
.vscode/settings.json
```

**User:**
```
~/.config/Code/User/settings.json  (Linux/macOS)
%APPDATA%\Code\User\settings.json  (Windows)
```

### Generic MCP Client

Most MCP clients look for configuration in:
```
~/.config/mcp/servers.json  (Linux/macOS)
%APPDATA%\MCP\servers.json  (Windows)
```

Or accept configuration via environment variables or command-line arguments.

## Configuration Formats

### Claude Desktop Format

**Basic Configuration:**
```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [],
      "env": {}
    }
  }
}
```

**Full Path Configuration (Recommended):**
```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "/Users/username/.dotnet/tools/wpf-to-avalonia-mcp",
      "args": [],
      "env": {}
    }
  }
}
```

**Windows Full Path:**
```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "C:\\Users\\username\\.dotnet\\tools\\wpf-to-avalonia-mcp.exe",
      "args": [],
      "env": {}
    }
  }
}
```

**With Custom Configuration File:**
```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [
        "--config",
        "/path/to/custom-config.json"
      ],
      "env": {}
    }
  }
}
```

**With Environment Variables:**
```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [],
      "env": {
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES": "20",
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__IDLE_TIMEOUT_MINUTES": "60",
        "WPF_TO_AVALONIA_MCP_TIMEOUTS__ANALYSIS_TIMEOUT_SECONDS": "300",
        "WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL": "Information"
      }
    }
  }
}
```

### VS Code MCP Extension Format

**Basic Configuration:**
```json
{
  "mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [],
      "env": {},
      "description": "WPF to Avalonia migration analyzer",
      "autoStart": true
    }
  }
}
```

**With Workspace Variables:**
```json
{
  "mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [
        "--config",
        "${workspaceFolder}/.wpf-to-avalonia-config.json"
      ],
      "env": {
        "WPF_TO_AVALONIA_MCP_LOGGING__FILE_PATH": "${workspaceFolder}/.logs/mcp.log"
      },
      "autoStart": true
    }
  }
}
```

Available VS Code variables:
- `${workspaceFolder}` - Root workspace folder
- `${workspaceFolderBasename}` - Workspace folder name
- `${file}` - Current file path
- `${relativeFile}` - File relative to workspace
- `${fileBasename}` - Current file name
- `${fileDirname}` - Current file's directory
- `${userHome}` - User's home directory

### GitHub Copilot Format

**VS Code Settings:**
```json
{
  "github.copilot.mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [],
      "env": {}
    }
  }
}
```

**With Workspace Context:**
```json
{
  "github.copilot.mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": ["--config", "${workspaceFolder}/.wpf-to-avalonia-config.json"],
      "env": {
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES": "10"
      }
    }
  }
}
```

### Generic MCP Client Format

Standard MCP server configuration:

```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [],
      "env": {},
      "metadata": {
        "name": "WpfToAvalonia MCP Server",
        "version": "1.0.0",
        "description": "WPF to Avalonia migration analyzer and code fix provider"
      }
    }
  }
}
```

## Command-Line Arguments

The MCP server supports the following command-line arguments:

### Configuration File
```bash
wpf-to-avalonia-mcp --config /path/to/config.json
```

Specifies a custom configuration file instead of default locations.

### Verbose Logging
```bash
wpf-to-avalonia-mcp --verbose
```

Enables detailed debug logging to console.

### Version Information
```bash
wpf-to-avalonia-mcp --version
```

Displays server version and exits.

### Help
```bash
wpf-to-avalonia-mcp --help
```

Shows usage information and available arguments.

### Combined Example
```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [
        "--config", "/path/to/config.json",
        "--verbose"
      ],
      "env": {}
    }
  }
}
```

## Environment Variables

All configuration settings can be overridden using environment variables with the prefix `WPF_TO_AVALONIA_MCP_`.

### Naming Convention

Configuration hierarchy uses double underscores (`__`) as separators:

**JSON Path → Environment Variable:**
```
WorkspaceCache.MaxWorkspaces
→ WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES

Timeouts.AnalysisTimeoutSeconds
→ WPF_TO_AVALONIA_MCP_TIMEOUTS__ANALYSIS_TIMEOUT_SECONDS
```

### Workspace Cache Settings

```bash
# Enable/disable workspace caching
WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__ENABLED=true

# Maximum number of cached workspaces (default: 10)
WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES=20

# Idle timeout in minutes (default: 30)
WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__IDLE_TIMEOUT_MINUTES=60

# Enable file system watching (default: true)
WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__ENABLE_FILE_WATCHING=true

# File change debounce delay in milliseconds (default: 2000)
WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__FILE_CHANGE_DEBOUNCE_MS=3000
```

### Timeout Settings

```bash
# Workspace load timeout in seconds (default: 120)
WPF_TO_AVALONIA_MCP_TIMEOUTS__WORKSPACE_LOAD_TIMEOUT_SECONDS=180

# Analysis timeout in seconds (default: 120)
WPF_TO_AVALONIA_MCP_TIMEOUTS__ANALYSIS_TIMEOUT_SECONDS=240

# Code fix timeout in seconds (default: 60)
WPF_TO_AVALONIA_MCP_TIMEOUTS__CODE_FIX_TIMEOUT_SECONDS=120

# Request timeout in seconds (default: 300)
WPF_TO_AVALONIA_MCP_TIMEOUTS__REQUEST_TIMEOUT_SECONDS=600
```

### Parallelism Settings

```bash
# Maximum degree of parallelism (default: CPU cores)
WPF_TO_AVALONIA_MCP_PARALLELISM__MAX_DEGREE_OF_PARALLELISM=8

# Maximum concurrent operations (default: 4)
WPF_TO_AVALONIA_MCP_PARALLELISM__MAX_CONCURRENT_OPERATIONS=8
```

### Logging Settings

```bash
# Minimum log level: Trace, Debug, Information, Warning, Error, Critical
WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL=Information

# Enable console logging (default: true)
WPF_TO_AVALONIA_MCP_LOGGING__CONSOLE_ENABLED=true

# Enable file logging (default: false)
WPF_TO_AVALONIA_MCP_LOGGING__FILE_ENABLED=true

# Log file path
WPF_TO_AVALONIA_MCP_LOGGING__FILE_PATH=/tmp/wpf-to-avalonia-mcp.log

# Enable structured logging (default: false)
WPF_TO_AVALONIA_MCP_LOGGING__STRUCTURED=true
```

### Security Settings

```bash
# Maximum project size in MB (default: 500)
WPF_TO_AVALONIA_MCP_SECURITY__MAX_PROJECT_SIZE_MB=1000

# Maximum files per project (default: 10000)
WPF_TO_AVALONIA_MCP_SECURITY__MAX_FILES_PER_PROJECT=20000

# Enable path validation (default: true)
WPF_TO_AVALONIA_MCP_SECURITY__VALIDATE_PATHS=true
```

## Configuration File Schema

The server accepts a JSON configuration file with the following structure:

```json
{
  "WorkspaceCache": {
    "Enabled": true,
    "MaxWorkspaces": 10,
    "IdleTimeoutMinutes": 30,
    "EnableFileWatching": true,
    "FileChangeDebounceMs": 2000
  },
  "Timeouts": {
    "WorkspaceLoadTimeoutSeconds": 120,
    "AnalysisTimeoutSeconds": 120,
    "CodeFixTimeoutSeconds": 60,
    "RequestTimeoutSeconds": 300
  },
  "Parallelism": {
    "MaxDegreeOfParallelism": 0,
    "MaxConcurrentOperations": 4
  },
  "Logging": {
    "MinimumLevel": "Information",
    "ConsoleEnabled": true,
    "FileEnabled": false,
    "FilePath": "",
    "Structured": false
  },
  "Security": {
    "MaxProjectSizeMB": 500,
    "MaxFilesPerProject": 10000,
    "ValidatePaths": true
  }
}
```

### Field Descriptions

#### WorkspaceCache
- **Enabled**: Enable workspace caching (improves performance)
- **MaxWorkspaces**: Maximum number of workspaces to keep in cache
- **IdleTimeoutMinutes**: Close workspaces after this many minutes of inactivity
- **EnableFileWatching**: Watch files for changes and auto-reload
- **FileChangeDebounceMs**: Delay before reloading after file changes

#### Timeouts
- **WorkspaceLoadTimeoutSeconds**: Timeout for loading MSBuild workspaces
- **AnalysisTimeoutSeconds**: Timeout for running analysis
- **CodeFixTimeoutSeconds**: Timeout for applying code fixes
- **RequestTimeoutSeconds**: Overall request timeout

#### Parallelism
- **MaxDegreeOfParallelism**: Max parallel tasks (0 = CPU core count)
- **MaxConcurrentOperations**: Max concurrent workspace operations

#### Logging
- **MinimumLevel**: Minimum log level to capture
- **ConsoleEnabled**: Write logs to console
- **FileEnabled**: Write logs to file
- **FilePath**: Path to log file
- **Structured**: Use structured JSON logging

#### Security
- **MaxProjectSizeMB**: Reject projects larger than this
- **MaxFilesPerProject**: Reject projects with more files than this
- **ValidatePaths**: Validate all paths are within expected bounds

## Common Configuration Patterns

### Development Configuration

Optimized for development with detailed logging:

```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": ["--verbose"],
      "env": {
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES": "5",
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__IDLE_TIMEOUT_MINUTES": "15",
        "WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL": "Debug",
        "WPF_TO_AVALONIA_MCP_LOGGING__FILE_ENABLED": "true",
        "WPF_TO_AVALONIA_MCP_LOGGING__FILE_PATH": "${workspaceFolder}/.logs/mcp-debug.log"
      }
    }
  }
}
```

### Production Configuration

Optimized for performance and reliability:

```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [],
      "env": {
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES": "25",
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__IDLE_TIMEOUT_MINUTES": "120",
        "WPF_TO_AVALONIA_MCP_TIMEOUTS__ANALYSIS_TIMEOUT_SECONDS": "600",
        "WPF_TO_AVALONIA_MCP_PARALLELISM__MAX_DEGREE_OF_PARALLELISM": "16",
        "WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL": "Warning",
        "WPF_TO_AVALONIA_MCP_LOGGING__FILE_ENABLED": "true",
        "WPF_TO_AVALONIA_MCP_LOGGING__FILE_PATH": "/var/log/wpf-to-avalonia-mcp.log"
      }
    }
  }
}
```

### Resource-Constrained Configuration

Optimized for low-resource environments:

```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [],
      "env": {
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES": "3",
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__IDLE_TIMEOUT_MINUTES": "10",
        "WPF_TO_AVALONIA_MCP_PARALLELISM__MAX_DEGREE_OF_PARALLELISM": "2",
        "WPF_TO_AVALONIA_MCP_PARALLELISM__MAX_CONCURRENT_OPERATIONS": "1",
        "WPF_TO_AVALONIA_MCP_SECURITY__MAX_PROJECT_SIZE_MB": "200",
        "WPF_TO_AVALONIA_MCP_LOGGING__CONSOLE_ENABLED": "false"
      }
    }
  }
}
```

### CI/CD Configuration

Optimized for automated pipelines:

```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [],
      "env": {
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__ENABLED": "false",
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__ENABLE_FILE_WATCHING": "false",
        "WPF_TO_AVALONIA_MCP_TIMEOUTS__ANALYSIS_TIMEOUT_SECONDS": "900",
        "WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL": "Information",
        "WPF_TO_AVALONIA_MCP_LOGGING__STRUCTURED": "true",
        "WPF_TO_AVALONIA_MCP_LOGGING__FILE_PATH": "/tmp/mcp-ci.log"
      }
    }
  }
}
```

## Platform-Specific Considerations

### Windows

**Path Separators:**
Use double backslashes in JSON strings:
```json
{
  "command": "C:\\Users\\username\\.dotnet\\tools\\wpf-to-avalonia-mcp.exe"
}
```

Or use forward slashes (supported by Windows):
```json
{
  "command": "C:/Users/username/.dotnet/tools/wpf-to-avalonia-mcp.exe"
}
```

**Environment Variables:**
Set using PowerShell:
```powershell
$env:WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL="Debug"
```

Or Command Prompt:
```cmd
set WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL=Debug
```

### macOS/Linux

**Executable Permissions:**
Ensure the tool is executable:
```bash
chmod +x ~/.dotnet/tools/wpf-to-avalonia-mcp
```

**Environment Variables:**
Set in shell profile (`~/.bashrc`, `~/.zshrc`, etc.):
```bash
export WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL="Debug"
```

Or set temporarily:
```bash
WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL=Debug wpf-to-avalonia-mcp
```

### Docker/Containers

**Dockerfile Example:**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0

# Install the MCP server
RUN dotnet tool install --global WpfToAvaloniaAnalyzers.Mcp

# Add .NET tools to PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# Set configuration via environment
ENV WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES=20
ENV WPF_TO_AVALONIA_MCP_LOGGING__FILE_PATH=/logs/mcp.log

# Create log directory
RUN mkdir -p /logs

# Expose stdio for MCP protocol
CMD ["wpf-to-avalonia-mcp"]
```

**Docker Compose:**
```yaml
version: '3.8'
services:
  wpf-to-avalonia-mcp:
    image: mcr.microsoft.com/dotnet/sdk:9.0
    environment:
      - WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES=20
      - WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL=Information
    volumes:
      - ./projects:/projects:ro
      - ./logs:/logs
    command: dotnet tool run wpf-to-avalonia-mcp
```

## Validation and Testing

### Test Configuration Syntax

Validate JSON syntax:
```bash
# Using jq
jq . < config.json

# Using python
python -m json.tool config.json
```

### Test Server Startup

Test the server starts correctly:
```bash
# With verbose logging
wpf-to-avalonia-mcp --verbose

# With custom config
wpf-to-avalonia-mcp --config /path/to/config.json --verbose
```

Expected output:
```
WpfToAvalonia MCP Server v1.0.0
Initializing server...
Loading configuration...
Starting MCP protocol handler...
Server ready. Listening on stdio.
```

### Verify Environment Variables

Check which settings are active:
```bash
# Set verbose mode
export WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL=Debug

# Run with verbose output
wpf-to-avalonia-mcp --verbose
```

Look for log lines like:
```
[Debug] Configuration loaded:
  WorkspaceCache.MaxWorkspaces: 10
  Timeouts.AnalysisTimeoutSeconds: 120
  ...
```

### Test Tool Discovery

Verify tools are registered correctly using any MCP client:
```json
{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "id": 1
}
```

Expected response includes all 10 tools.

## Troubleshooting Configuration

### Issue: Configuration Not Loading

**Check:**
1. File exists and is readable
2. JSON syntax is valid
3. Path is absolute (not relative)
4. File permissions allow reading

**Debug:**
```bash
wpf-to-avalonia-mcp --config /path/to/config.json --verbose 2>&1 | grep -i config
```

### Issue: Environment Variables Not Working

**Check:**
1. Variable name exactly matches expected format
2. Double underscores (`__`) used for nesting
3. Prefix `WPF_TO_AVALONIA_MCP_` is correct
4. Variable is exported (Unix) or set in correct scope (Windows)

**Debug:**
```bash
# Unix/macOS/Linux
env | grep WPF_TO_AVALONIA_MCP

# Windows PowerShell
Get-ChildItem Env: | Where-Object Name -like "WPF_TO_AVALONIA_MCP*"
```

### Issue: Server Fails to Start

**Check:**
1. .NET 9.0 SDK is installed
2. Tool is installed globally
3. Tool is in PATH
4. Configuration values are valid types (numbers, booleans, strings)

**Debug:**
```bash
# Check .NET version
dotnet --version

# Check tool installation
dotnet tool list --global | grep -i wpf

# Test tool directly
wpf-to-avalonia-mcp --version
```

## Best Practices

1. **Use Workspace Configuration**: Keep settings with your project
2. **Version Control**: Commit configuration files (sanitize sensitive data)
3. **Environment Variables for Secrets**: Don't store sensitive data in config files
4. **Document Custom Settings**: Add comments (in a separate .md file)
5. **Test Configuration Changes**: Restart server and verify settings applied
6. **Monitor Resource Usage**: Adjust cache and parallelism based on actual usage
7. **Use Appropriate Log Levels**: Debug for development, Warning for production
8. **Regular Updates**: Keep the MCP server updated to latest version

## References

- [MCP Server Guide](./MCP_SERVER_GUIDE.md) - Complete server documentation
- [Claude Desktop Integration](./CLAUDE_DESKTOP_INTEGRATION.md) - Claude-specific setup
- [GitHub Copilot Integration](./GITHUB_COPILOT_INTEGRATION.md) - Copilot-specific setup
- [VS Code MCP Integration](./VSCODE_MCP_INTEGRATION.md) - VS Code-specific setup
- [Model Context Protocol Specification](https://modelcontextprotocol.io/) - MCP protocol docs
