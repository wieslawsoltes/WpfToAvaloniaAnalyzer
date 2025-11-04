# VS Code MCP Extension Integration Guide

This guide explains how to integrate the WpfToAvalonia MCP Server with VS Code using the official Model Context Protocol extension.

## Overview

The Model Context Protocol (MCP) extension for VS Code enables direct integration of MCP servers into your development environment. This provides:

- Native tool integration in the VS Code UI
- Direct access to WpfToAvalonia analyzer tools
- Seamless workflow integration
- Context-aware assistance
- No dependency on external AI services

## Prerequisites

- **VS Code**: Version 1.85 or later
- **MCP Extension for VS Code**: Install from the marketplace
- **.NET 9.0 SDK**: Required to run the MCP server
- **WpfToAvalonia MCP Server**: Installed as a .NET tool

## Installation

### Step 1: Install VS Code MCP Extension

Install the MCP extension from the VS Code marketplace:

1. Open VS Code
2. Press `Ctrl+Shift+X` (or `Cmd+Shift+X` on macOS)
3. Search for "Model Context Protocol"
4. Click "Install"

Alternatively, install via command line:
```bash
code --install-extension modelcontextprotocol.mcp
```

### Step 2: Install the WpfToAvalonia MCP Server

Install the WpfToAvalonia MCP server as a global .NET tool:

```bash
dotnet tool install --global WpfToAvaloniaAnalyzers.Mcp
```

Verify installation:
```bash
dotnet tool list --global | grep WpfToAvalonia
```

### Step 3: Configure the MCP Server

The MCP extension looks for server configuration in your VS Code settings.

#### Workspace Configuration (Recommended)

Create or edit `.vscode/settings.json` in your project root:

```json
{
  "mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [],
      "env": {},
      "description": "WPF to Avalonia migration analyzer and code fix provider",
      "autoStart": true
    }
  }
}
```

#### User Configuration (Global)

For global configuration across all projects:

1. Press `Ctrl+,` (or `Cmd+,` on macOS)
2. Click the "Open Settings (JSON)" icon in the top-right
3. Add the MCP server configuration:

```json
{
  "mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [],
      "env": {},
      "description": "WPF to Avalonia migration analyzer",
      "autoStart": false
    }
  }
}
```

### Step 4: Start the MCP Server

The server can start automatically or manually:

**Automatic Start (if `autoStart: true`):**
- Server starts when VS Code opens a workspace with the configuration

**Manual Start:**
1. Open the Command Palette (`Ctrl+Shift+P` or `Cmd+Shift+P`)
2. Type "MCP: Start Server"
3. Select "wpf-to-avalonia"

## Verification

To verify the integration is working:

1. Open the Command Palette (`Ctrl+Shift+P`)
2. Type "MCP: Show Servers"
3. You should see "wpf-to-avalonia" listed with status "Running"
4. Type "MCP: List Tools"
5. You should see all WpfToAvalonia tools listed

## Using the MCP Tools

### Via Command Palette

Access tools directly from the Command Palette:

1. Press `Ctrl+Shift+P` (or `Cmd+Shift+P`)
2. Type "MCP: Run Tool"
3. Select "wpf-to-avalonia"
4. Choose a tool (e.g., "analyze_project")
5. Fill in the parameters in the input panel
6. View results in the MCP Output panel

### Via MCP Panel

The MCP extension provides a dedicated panel:

1. Click the MCP icon in the Activity Bar (left sidebar)
2. Expand "wpf-to-avalonia" server
3. Browse available tools
4. Click a tool to open the parameter form
5. Fill in parameters and click "Execute"

### Via Keyboard Shortcuts

Add custom keyboard shortcuts in `.vscode/keybindings.json`:

```json
[
  {
    "key": "ctrl+shift+a",
    "command": "mcp.runTool",
    "args": {
      "server": "wpf-to-avalonia",
      "tool": "analyze_project",
      "params": {
        "projectPath": "${workspaceFolder}/${relativeFile}"
      }
    },
    "when": "resourceExtname == .csproj"
  },
  {
    "key": "ctrl+shift+f",
    "command": "mcp.runTool",
    "args": {
      "server": "wpf-to-avalonia",
      "tool": "list_analyzers"
    }
  }
]
```

### Via Tasks

Integrate MCP tools into VS Code tasks (`.vscode/tasks.json`):

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "Analyze Current Project",
      "type": "mcp",
      "server": "wpf-to-avalonia",
      "tool": "analyze_project",
      "params": {
        "projectPath": "${workspaceFolder}/${relativeFileDirname}/${fileBasenameNoExtension}.csproj"
      },
      "problemMatcher": [],
      "group": {
        "kind": "build",
        "isDefault": false
      }
    },
    {
      "label": "Validate Project",
      "type": "mcp",
      "server": "wpf-to-avalonia",
      "tool": "validate_project",
      "params": {
        "projectPath": "${workspaceFolder}/${input:projectFile}"
      },
      "problemMatcher": []
    }
  ],
  "inputs": [
    {
      "id": "projectFile",
      "type": "pickString",
      "description": "Select project file",
      "options": [
        "MyApp.csproj",
        "MyApp.Core.csproj",
        "MyApp.Tests.csproj"
      ]
    }
  ]
}
```

Run tasks via:
- Terminal → Run Task → Select your task
- `Ctrl+Shift+B` for default build task

## Advanced Configuration

### Environment Variables

Configure server behavior with environment variables:

```json
{
  "mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [],
      "env": {
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES": "15",
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__IDLE_TIMEOUT_MINUTES": "45",
        "WPF_TO_AVALONIA_MCP_TIMEOUTS__ANALYSIS_TIMEOUT_SECONDS": "240",
        "WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL": "Information"
      },
      "autoStart": true
    }
  }
}
```

### Custom Configuration File

Use a project-specific configuration file:

```json
{
  "mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [
        "--config",
        "${workspaceFolder}/.wpf-to-avalonia-config.json"
      ],
      "env": {},
      "autoStart": true
    }
  }
}
```

**Configuration file:** `.wpf-to-avalonia-config.json`
```json
{
  "WorkspaceCache": {
    "Enabled": true,
    "MaxWorkspaces": 20,
    "IdleTimeoutMinutes": 60
  },
  "Timeouts": {
    "AnalysisTimeoutSeconds": 300,
    "CodeFixTimeoutSeconds": 180
  },
  "Parallelism": {
    "MaxDegreeOfParallelism": 8
  }
}
```

### Logging Configuration

Enable detailed logging for debugging:

```json
{
  "mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": ["--verbose"],
      "env": {
        "WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL": "Debug",
        "WPF_TO_AVALONIA_MCP_LOGGING__FILE_ENABLED": "true",
        "WPF_TO_AVALONIA_MCP_LOGGING__FILE_PATH": "${workspaceFolder}/.logs/mcp-server.log"
      },
      "autoStart": true
    }
  },
  "mcp.logging.level": "debug"
}
```

View logs:
1. Open Output panel (`Ctrl+Shift+U`)
2. Select "MCP" or "MCP: wpf-to-avalonia" from dropdown

## Workflow Patterns

### Pattern 1: Continuous Analysis

Set up automatic analysis when opening project files:

**.vscode/settings.json:**
```json
{
  "mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "autoStart": true,
      "triggerOnOpen": [
        "**/*.csproj",
        "**/*.sln"
      ]
    }
  },
  "mcp.autoRun": {
    "wpf-to-avalonia.analyze_project": {
      "trigger": "onProjectOpen",
      "params": {
        "projectPath": "${workspaceFolder}/${file}"
      }
    }
  }
}
```

### Pattern 2: Quick Fix Integration

Add code actions for quick fixes:

1. The MCP extension can register code actions
2. When you see a squiggle, press `Ctrl+.` (Quick Fix)
3. Select "MCP: Apply WPF to Avalonia fix"
4. Choose the fix from available options

### Pattern 3: Status Bar Integration

Show migration status in the status bar:

The MCP extension can display custom status:
- **Green**: No issues found
- **Yellow**: Issues found, some fixable
- **Red**: Critical issues or errors

Click the status bar item to:
- View issue summary
- Run analysis
- Apply batch fixes

### Pattern 4: Problem Matcher Integration

Integrate analysis results with VS Code's Problems panel:

**.vscode/tasks.json:**
```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "Analyze for Migration Issues",
      "type": "mcp",
      "server": "wpf-to-avalonia",
      "tool": "analyze_project",
      "params": {
        "projectPath": "${workspaceFolder}/MyProject.csproj"
      },
      "problemMatcher": {
        "owner": "wpf-to-avalonia",
        "fileLocation": "absolute",
        "pattern": {
          "regexp": "^(.+)\\((\\d+),(\\d+)\\):\\s+(warning|error)\\s+(WPFAV\\d+):\\s+(.+)$",
          "file": 1,
          "line": 2,
          "column": 3,
          "severity": 4,
          "code": 5,
          "message": 6
        }
      }
    }
  ]
}
```

Now diagnostics appear in the Problems panel (`Ctrl+Shift+M`).

## Extension Commands

The MCP extension provides several commands:

| Command | Description | Shortcut |
|---------|-------------|----------|
| `MCP: Start Server` | Start a configured MCP server | - |
| `MCP: Stop Server` | Stop a running server | - |
| `MCP: Restart Server` | Restart a server | - |
| `MCP: Show Servers` | List all configured servers | - |
| `MCP: List Tools` | Show available tools for a server | - |
| `MCP: Run Tool` | Execute a tool with parameters | - |
| `MCP: Clear Cache` | Clear the tool results cache | - |
| `MCP: View Logs` | Open server logs | - |

Add keyboard shortcuts in `.vscode/keybindings.json`:

```json
[
  {
    "key": "ctrl+alt+m s",
    "command": "mcp.showServers"
  },
  {
    "key": "ctrl+alt+m t",
    "command": "mcp.listTools"
  },
  {
    "key": "ctrl+alt+m r",
    "command": "mcp.runTool"
  },
  {
    "key": "ctrl+alt+m c",
    "command": "mcp.clearCache"
  }
]
```

## UI Integration Points

### Activity Bar

The MCP extension adds an icon to the Activity Bar (left sidebar):

```
┌─────────────────┐
│  MCP Servers    │
├─────────────────┤
│  wpf-to-avalonia│ ● (running)
│    Tools (10)   │
│    ├─ analyze_project
│    ├─ list_analyzers
│    ├─ apply_fix
│    ├─ batch_convert
│    ├─ preview_fixes
│    ├─ get_diagnostic_info
│    ├─ validate_project
│    ├─ get_workspace_cache_stats
│    ├─ clear_workspace_cache
│    └─ get_server_info
└─────────────────┘
```

### Context Menus

Right-click menus for files and folders:

**File context menu (.cs files):**
- "Analyze for Migration Issues"
- "Apply WPF to Avalonia Fixes..."
- "Preview Migration Changes"

**Folder context menu:**
- "Batch Convert to Avalonia..."
- "Analyze Folder for Issues"

### Editor Decorations

The extension can highlight code with migration issues:

- **Green underline**: Auto-fixable issues
- **Yellow underline**: Manual review needed
- **Red underline**: Breaking changes required

Hover over underlined code to see:
- Diagnostic ID and message
- Available fixes
- Migration guidance
- "Apply Fix" button

## Integration with Other Extensions

### C# Extension

Works seamlessly with the C# Dev Kit:

```json
{
  "omnisharp.enableRoslynAnalyzers": true,
  "omnisharp.enableEditorConfigSupport": true,
  "mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "autoStart": true,
      "integrations": {
        "csharp": {
          "provideCodeActions": true,
          "provideDiagnostics": true
        }
      }
    }
  }
}
```

### GitLens

Integrate with GitLens for historical analysis:

```json
{
  "gitlens.advanced.messages": {
    "suppressWpfToAvaloniaHints": false
  },
  "mcp.integrations": {
    "gitlens": {
      "annotateWithMigrationStatus": true
    }
  }
}
```

### TODO Tree

Track migration tasks:

```json
{
  "todo-tree.general.tags": [
    "TODO",
    "FIXME",
    "MIGRATE",
    "WPFAV001",
    "WPFAV002"
  ],
  "todo-tree.highlights.customHighlight": {
    "MIGRATE": {
      "icon": "arrow-right",
      "iconColour": "#FFA500"
    }
  }
}
```

## Workflow Examples

### Daily Development Workflow

1. **Morning Setup**
   - Open VS Code
   - MCP server auto-starts
   - Status bar shows current migration status

2. **Active Development**
   - Edit WPF code
   - See inline migration hints
   - Quick fix with `Ctrl+.`
   - Preview changes before applying

3. **Batch Operations**
   - Select folder in explorer
   - Right-click → "Batch Convert to Avalonia"
   - Review changes in diff view
   - Accept or reject changes

4. **End of Day**
   - Run full project analysis
   - Review Problems panel
   - Commit migrated code
   - Server auto-stops on VS Code close

### Code Review Workflow

1. **Open Pull Request**
   - Checkout PR branch
   - Run analysis on changed files

2. **Review Changes**
   - See migration status in editor
   - Check Problems panel for issues
   - Verify fixes are correct

3. **Request Changes**
   - Comment on specific issues
   - Suggest fixes using tool output
   - Re-run analysis after updates

4. **Approve**
   - Final full project analysis
   - Verify no new issues
   - Approve and merge

### Team Migration Workflow

1. **Planning Phase**
   - Analyze entire codebase
   - Export diagnostic report
   - Assign files to team members

2. **Individual Work**
   - Each developer migrates assigned files
   - Uses MCP tools for guidance
   - Commits changes incrementally

3. **Integration**
   - Merge branches
   - Run full analysis
   - Fix integration issues
   - Re-run tests

4. **Validation**
   - Final analysis of merged code
   - Verify all issues resolved
   - Document manual changes needed

## Troubleshooting

### Issue: Server fails to start

**Symptoms:** "Failed to start server wpf-to-avalonia" error

**Solutions:**
1. Check server is installed: `dotnet tool list --global`
2. Verify .NET 9.0 SDK: `dotnet --version`
3. Test manually: `wpf-to-avalonia-mcp --version`
4. Check Output panel (MCP) for error details
5. Verify configuration JSON syntax

### Issue: Tools not appearing

**Symptoms:** MCP panel shows server but no tools

**Solutions:**
1. Restart the server: Command Palette → "MCP: Restart Server"
2. Check server logs for initialization errors
3. Verify server is running: Command Palette → "MCP: Show Servers"
4. Clear extension cache: Command Palette → "MCP: Clear Cache"

### Issue: Slow performance

**Symptoms:** Tools take a long time to execute

**Solutions:**
1. Increase timeout values in configuration
2. Reduce workspace cache size
3. Close other applications
4. Use `target_files` parameter to limit scope
5. Enable file watching to avoid repeated workspace loads

### Issue: Changes not persisting

**Symptoms:** Applied fixes disappear after reload

**Solutions:**
1. Check file permissions (read/write access)
2. Verify no file watchers are reverting changes
3. Ensure no format-on-save is undoing changes
4. Check version control status (not locked)

### Issue: Extension conflicts

**Symptoms:** MCP extension interferes with other extensions

**Solutions:**
1. Disable conflicting extensions temporarily
2. Check extension settings for conflicts
3. Report issue to extension maintainers
4. Use workspace-specific extension settings

## Performance Optimization

### For Large Projects

```json
{
  "mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "env": {
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES": "25",
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__IDLE_TIMEOUT_MINUTES": "120",
        "WPF_TO_AVALONIA_MCP_TIMEOUTS__ANALYSIS_TIMEOUT_SECONDS": "600",
        "WPF_TO_AVALONIA_MCP_PARALLELISM__MAX_DEGREE_OF_PARALLELISM": "16",
        "WPF_TO_AVALONIA_MCP_PARALLELISM__MAX_CONCURRENT_OPERATIONS": "8"
      }
    }
  },
  "mcp.cache.enabled": true,
  "mcp.cache.ttl": 3600
}
```

### For Resource-Constrained Systems

```json
{
  "mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "env": {
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES": "3",
        "WPF_TO_AVALONIA_MCP_PARALLELISM__MAX_DEGREE_OF_PARALLELISM": "2"
      },
      "autoStart": false
    }
  }
}
```

## Security Considerations

### File System Access

The MCP server requires read/write access to your project:

```json
{
  "mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "security": {
        "allowFileSystemAccess": true,
        "restrictToWorkspace": true,
        "allowedPaths": [
          "${workspaceFolder}"
        ]
      }
    }
  }
}
```

### Sandboxing

Run the server in a restricted environment:

```json
{
  "mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "sandbox": {
        "enabled": true,
        "allowNetwork": false,
        "allowShellCommands": false
      }
    }
  }
}
```

## Best Practices

1. **Use Workspace Configuration**: Keep settings with your project
2. **Version Control Config**: Commit `.vscode/settings.json`
3. **Document Custom Tasks**: Add comments to task definitions
4. **Enable Auto-Start for Active Projects**: Disable for occasional use
5. **Monitor Resource Usage**: Adjust cache and parallelism settings
6. **Regular Updates**: Keep MCP extension and server updated
7. **Use Tasks for Repetitive Operations**: Automate common workflows
8. **Leverage Keyboard Shortcuts**: Speed up frequent operations

## Additional Resources

- [MCP Extension Documentation](https://marketplace.visualstudio.com/items?itemName=modelcontextprotocol.mcp)
- [VS Code Tasks Documentation](https://code.visualstudio.com/docs/editor/tasks)
- [VS Code Keybindings](https://code.visualstudio.com/docs/getstarted/keybindings)
- [MCP Server Guide](./MCP_SERVER_GUIDE.md)
- [Claude Desktop Integration](./CLAUDE_DESKTOP_INTEGRATION.md)
- [GitHub Copilot Integration](./GITHUB_COPILOT_INTEGRATION.md)

## Version History

- **1.0.0** (2024-01) - Initial release with VS Code MCP extension support
