# GitHub Copilot Integration Guide

This guide explains how to integrate the WpfToAvalonia MCP Server with GitHub Copilot in VS Code and other supported environments.

## Overview

GitHub Copilot is adding support for the Model Context Protocol (MCP), enabling Copilot to interact with external tools and services. This integration allows Copilot to:

- Analyze WPF projects during migration discussions
- Suggest migration strategies based on actual diagnostic data
- Apply code fixes with your approval
- Provide context-aware migration guidance

> **Note:** MCP support in GitHub Copilot is currently in preview. Check the [GitHub Copilot documentation](https://docs.github.com/en/copilot) for the latest availability and setup instructions.

## Prerequisites

- **VS Code**: Version 1.85 or later
- **GitHub Copilot Extension**: Latest version with MCP support
- **.NET 9.0 SDK**: Required to run the MCP server
- **WpfToAvalonia MCP Server**: Installed as a .NET tool
- **GitHub Copilot Subscription**: Active Copilot subscription

## Installation

### Step 1: Install the MCP Server

Install the WpfToAvalonia MCP server as a global .NET tool:

```bash
dotnet tool install --global WpfToAvaloniaAnalyzers.Mcp
```

Verify the installation:

```bash
dotnet tool list --global | grep WpfToAvalonia
```

### Step 2: Configure GitHub Copilot for MCP

GitHub Copilot's MCP configuration varies by environment. Currently, MCP support is being rolled out in phases.

#### Option A: VS Code Settings (Recommended)

Open VS Code settings (JSON format) and add the MCP server configuration:

**File:** `.vscode/settings.json` (workspace) or `settings.json` (user)

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

For workspace-specific configuration, create or edit `.vscode/settings.json` in your project root.

#### Option B: Global Copilot Configuration

Some versions of Copilot use a global configuration file:

**macOS/Linux:**
```
~/.config/github-copilot/mcp-servers.json
```

**Windows:**
```
%APPDATA%\GitHub Copilot\mcp-servers.json
```

Content:
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

### Step 3: Restart VS Code

After configuring the MCP server, restart VS Code to apply the changes.

## Verification

To verify the integration:

1. Open a WPF project in VS Code
2. Open the Copilot chat panel (Ctrl+Shift+I / Cmd+Shift+I)
3. Type: `@copilot What WPF to Avalonia tools are available?`
4. Copilot should acknowledge the MCP tools or use them to respond

## Usage Patterns

### Pattern 1: Contextual Analysis

While editing a WPF file, ask Copilot for migration help:

```
You: [In MainWindow.xaml.cs]
     @copilot This file uses DependencyProperty. How should I migrate this to Avalonia?

Copilot: [Analyzes the file using the MCP tools]
         I found 3 DependencyProperty declarations in this file. In Avalonia,
         you should use StyledProperty or DirectProperty instead.

         Would you like me to show you the conversion for the first one?
```

### Pattern 2: Project-Wide Guidance

Ask for high-level migration strategy:

```
You: @copilot I need to migrate this WPF project to Avalonia. Where should I start?

Copilot: [Uses analyze_project tool]
         I've analyzed your project and found:
         - 45 issues across 12 files
         - Main categories: DependencyProperty (20), Events (15), XAML (10)

         Recommended migration order:
         1. Start with ViewModels (no UI dependencies)
         2. Convert Models and services
         3. Migrate Controls and Windows
         4. Update XAML files
         5. Fix event handlers

         Shall we start with the ViewModels?
```

### Pattern 3: Interactive Fix Application

Apply fixes with preview and approval:

```
You: @copilot Apply the fix for the DependencyProperty at line 25

Copilot: [Uses preview_fixes tool]
         Here's what will change:

         ```diff
         - public static readonly DependencyProperty NameProperty = ...
         + public static readonly StyledProperty<string> NameProperty = ...
         ```

         Should I apply this change?

You: Yes

Copilot: [Uses apply_fix tool]
         Applied the fix. The file has been updated.
```

### Pattern 4: Batch Operations

Process multiple files at once:

```
You: @copilot Convert all WPFAV001 issues in the Views folder

Copilot: [Uses batch_convert tool with target_files filter]
         I'll batch convert all DependencyProperty issues in the Views folder.

         Processed 8 files:
         - MainWindow.xaml.cs: 3 fixes
         - SettingsView.xaml.cs: 2 fixes
         - UserProfileView.xaml.cs: 1 fix
         ... (5 more files)

         All conversions complete. Would you like me to analyze for remaining issues?
```

## Advanced Configuration

### Workspace-Specific Settings

Configure per-project settings in `.vscode/settings.json`:

```json
{
  "github.copilot.mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": ["--config", "${workspaceFolder}/.wpf-to-avalonia-config.json"],
      "env": {
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES": "5",
        "WPF_TO_AVALONIA_MCP_TIMEOUTS__ANALYSIS_TIMEOUT_SECONDS": "180"
      }
    }
  }
}
```

Variables available:
- `${workspaceFolder}` - Root folder of the workspace
- `${workspaceFolderBasename}` - Name of the workspace folder
- `${userHome}` - User's home directory

### Performance Tuning

For large projects, adjust timeout and cache settings:

```json
{
  "github.copilot.mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": [],
      "env": {
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES": "20",
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__IDLE_TIMEOUT_MINUTES": "60",
        "WPF_TO_AVALONIA_MCP_TIMEOUTS__ANALYSIS_TIMEOUT_SECONDS": "300",
        "WPF_TO_AVALONIA_MCP_PARALLELISM__MAX_DEGREE_OF_PARALLELISM": "8"
      }
    }
  }
}
```

### Debug Configuration

Enable detailed logging for troubleshooting:

```json
{
  "github.copilot.mcp.servers": {
    "wpf-to-avalonia": {
      "command": "wpf-to-avalonia-mcp",
      "args": ["--verbose"],
      "env": {
        "WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL": "Debug",
        "WPF_TO_AVALONIA_MCP_LOGGING__FILE_ENABLED": "true",
        "WPF_TO_AVALONIA_MCP_LOGGING__FILE_PATH": "${workspaceFolder}/.logs/mcp-server.log"
      }
    }
  }
}
```

## Effective Prompts for Copilot

### Discovery and Planning
```
@copilot Analyze my WPF project and create a migration checklist
@copilot What are the most common issues in this project?
@copilot Estimate the effort required to migrate this project to Avalonia
@copilot Which files should I migrate first?
```

### Learning and Understanding
```
@copilot Explain how DependencyProperty works in WPF vs Avalonia
@copilot What's the Avalonia equivalent of this WPF pattern?
@copilot Show me examples of migrating attached properties
@copilot What are the differences between WPF and Avalonia event handling?
```

### Incremental Migration
```
@copilot Migrate just this one property to Avalonia
@copilot Convert this file but show me the changes first
@copilot Apply the fix at line 42, column 17
@copilot Undo the last migration change
```

### Batch Operations
```
@copilot Convert all DependencyProperties in this file
@copilot Migrate all files in the ViewModels folder
@copilot Fix all WPFAV001 warnings in the entire project
@copilot Batch convert but use dry-run mode first
```

### Validation and Testing
```
@copilot Did I break anything with that last change?
@copilot Analyze the project again to see remaining issues
@copilot Validate that my project is ready for migration
@copilot Check if there are any new issues after my manual edits
```

## Integration with VS Code Features

### Task Integration

Create a VS Code task to run analysis:

**File:** `.vscode/tasks.json`

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "Analyze WPF Project",
      "type": "shell",
      "command": "dotnet",
      "args": [
        "wpf-to-avalonia-cli",
        "analyze",
        "--project",
        "${workspaceFolder}/MyProject.csproj",
        "--format",
        "console"
      ],
      "problemMatcher": [],
      "group": {
        "kind": "build",
        "isDefault": false
      }
    }
  ]
}
```

### Snippet Integration

Create snippets for common Avalonia patterns:

**File:** `.vscode/avalonia.code-snippets`

```json
{
  "Avalonia StyledProperty": {
    "prefix": "astyled",
    "body": [
      "public static readonly StyledProperty<${1:string}> ${2:Property}Property =",
      "    AvaloniaProperty.Register<${3:ClassName}, ${1:string}>(",
      "        nameof(${2:Property}),",
      "        defaultValue: ${4:default});",
      "",
      "public ${1:string} ${2:Property}",
      "{",
      "    get => GetValue(${2:Property}Property);",
      "    set => SetValue(${2:Property}Property, value);",
      "}"
    ],
    "description": "Create an Avalonia StyledProperty"
  }
}
```

### Keybindings

Add keyboard shortcuts for common operations:

**File:** `.vscode/keybindings.json`

```json
[
  {
    "key": "ctrl+shift+m",
    "command": "workbench.action.terminal.sendSequence",
    "args": {
      "text": "Ask Copilot: @copilot Analyze this file for Avalonia migration issues\n"
    },
    "when": "editorTextFocus && editorLangId == 'csharp'"
  }
]
```

## Workflow Examples

### Workflow 1: Daily Development

1. **Morning**: Open your WPF project in VS Code
2. **Review**: Ask Copilot for a status update
   ```
   @copilot What's the current migration status of this project?
   ```
3. **Plan**: Get today's migration tasks
   ```
   @copilot What should I work on today? Prioritize by easiest first.
   ```
4. **Work**: Migrate files with Copilot's help
   ```
   @copilot Let's migrate MainWindow.xaml.cs step by step
   ```
5. **Verify**: Check your work
   ```
   @copilot Re-analyze the files I changed today
   ```

### Workflow 2: Pair Programming with Copilot

1. **Copilot suggests** changes based on analysis
2. **You review** the suggested changes in preview
3. **You approve** or modify the suggestions
4. **Copilot applies** the approved changes
5. **You test** the functionality manually
6. **Copilot re-analyzes** to verify success

### Workflow 3: Learning Mode

Use Copilot as a teaching tool:

```
You: @copilot I want to learn Avalonia patterns. For each issue you find,
     explain the WPF pattern, the Avalonia equivalent, and why the change
     is necessary.

Copilot: Great! Let's start with the first issue...

     WPF Pattern: DependencyProperty
     - Used for data binding and property change notification
     - Registered with static DependencyProperty.Register()
     - Stored in a static readonly field

     Avalonia Equivalent: StyledProperty or DirectProperty
     - StyledProperty: For styled properties (like WPF's DP)
     - DirectProperty: For simple properties without styling
     - Registered with AvaloniaProperty.Register()

     Why Change?
     - Different property system architecture
     - Better performance in Avalonia
     - Clearer separation of concerns

     Here's an example...
```

## Troubleshooting

### Issue: Copilot doesn't respond to MCP commands

**Solutions:**
1. Verify MCP support is available in your Copilot version
2. Check VS Code output panel for Copilot logs
3. Restart VS Code
4. Verify the MCP server is installed: `dotnet tool list --global`
5. Try running the server manually: `wpf-to-avalonia-mcp --version`

### Issue: Tools timeout or are slow

**Solutions:**
1. Increase timeout values in environment variables
2. Reduce workspace cache size for memory-constrained systems
3. Close other resource-intensive applications
4. Use `target_files` parameter to limit analysis scope

### Issue: Copilot applies wrong fixes

**Solutions:**
1. Always preview changes before applying
2. Be specific in your prompts: include file names, line numbers
3. Use step-by-step migration instead of batch operations
4. Verify Copilot is using the correct diagnostic ID

### Issue: Configuration not loading

**Solutions:**
1. Check JSON syntax with a validator
2. Verify file paths are correct (use absolute paths)
3. Check VS Code settings precedence (user vs workspace)
4. Reload VS Code window: Ctrl+Shift+P → "Reload Window"

## Best Practices

### 1. Use Workspace Settings
Configure per-project to keep settings with your code:
```bash
.vscode/
├── settings.json       # MCP configuration
├── tasks.json          # Analysis tasks
└── launch.json         # Debug configurations
```

### 2. Commit Configuration
Add MCP configuration to version control:
```gitignore
# .gitignore
.vscode/*
!.vscode/settings.json
!.vscode/tasks.json
!.vscode/extensions.json
```

### 3. Document Custom Prompts
Keep a document with effective prompts for your team:
```markdown
# Team Migration Prompts

## Daily Standup
@copilot Show migration progress for files changed in the last 24 hours

## Code Review
@copilot Analyze changes in the current branch for migration issues
```

### 4. Progressive Enhancement
Don't try to migrate everything at once:
1. Week 1: Models and ViewModels
2. Week 2: Simple controls
3. Week 3: Complex controls
4. Week 4: XAML and resources
5. Week 5: Testing and refinement

### 5. Continuous Verification
After each change:
```
@copilot Build the project and report any errors
@copilot Run the analyzer again
@copilot List any new issues introduced
```

## Comparison with Other Tools

| Feature | GitHub Copilot + MCP | Standalone CLI | Claude Desktop |
|---------|---------------------|----------------|----------------|
| Code awareness | Excellent | None | Good |
| Project context | Full VS Code context | Limited | Limited |
| Interactive editing | Native | Manual | External |
| Learning curve | Low | Medium | Low |
| Batch operations | Yes | Yes | Yes |
| Preview changes | Yes | Yes | Yes |
| IDE integration | Native | External | External |

## Limitations

1. **MCP Preview Status**: GitHub Copilot's MCP support is evolving
2. **Context Limits**: Large projects may exceed context windows
3. **Approval Required**: Copilot can't automatically apply changes without user approval
4. **Network Dependency**: Requires internet connection for Copilot service
5. **Subscription Required**: Needs active GitHub Copilot subscription

## Future Enhancements

Planned improvements for GitHub Copilot integration:

- **Inline suggestions**: Real-time migration suggestions while typing
- **Hover tooltips**: Show migration info on hover over WPF code
- **Code actions**: Right-click menu items for quick migration
- **Git integration**: Automatic commit messages for migration changes
- **Test generation**: Auto-generate tests for migrated code

## Additional Resources

- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)
- [VS Code MCP Extension](https://marketplace.visualstudio.com/items?itemName=modelcontextprotocol.mcp)
- [MCP Server Guide](./MCP_SERVER_GUIDE.md)
- [Claude Desktop Integration](./CLAUDE_DESKTOP_INTEGRATION.md)
- [Avalonia Documentation](https://docs.avaloniaui.net/)

## Support

For issues specific to this integration:

1. Check [GitHub Copilot Status](https://www.githubstatus.com/)
2. Review VS Code output panel: View → Output → "GitHub Copilot"
3. Check MCP server logs (if enabled)
4. Open an issue at [GitHub Repository](https://github.com/YourRepo/WpfToAvaloniaAnalyzer/issues)

## Version History

- **1.0.0** (2024-01) - Initial release with GitHub Copilot preview support
