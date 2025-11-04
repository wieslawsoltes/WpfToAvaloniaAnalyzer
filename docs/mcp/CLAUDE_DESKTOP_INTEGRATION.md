# Claude Desktop Integration Guide

This guide provides step-by-step instructions for integrating the WpfToAvalonia MCP Server with Claude Desktop.

## Overview

Claude Desktop supports the Model Context Protocol (MCP), allowing Claude to directly interact with the WpfToAvalonia analyzer tools. This integration enables Claude to:

- Analyze WPF projects for migration issues
- Apply automated code fixes
- Provide migration guidance
- Preview changes before applying them
- Batch convert multiple files

## Prerequisites

- **Claude Desktop**: Download from [claude.ai](https://claude.ai/download)
- **.NET 9.0 SDK**: Required to run the MCP server
- **WpfToAvalonia MCP Server**: Installed as a .NET tool

## Installation

### Step 1: Install the MCP Server

Install the WpfToAvalonia MCP server as a global .NET tool:

```bash
dotnet tool install --global WpfToAvaloniaAnalyzers.Mcp
```

To verify the installation:

```bash
dotnet tool list --global | grep WpfToAvalonia
```

You should see output similar to:
```
wpftoavaloniaanalyzers.mcp    1.0.0    wpf-to-avalonia-mcp
```

### Step 2: Locate the MCP Server Executable

Find where .NET tools are installed:

**Windows:**
```powershell
$env:USERPROFILE\.dotnet\tools\wpf-to-avalonia-mcp.exe
```

**macOS/Linux:**
```bash
~/.dotnet/tools/wpf-to-avalonia-mcp
```

### Step 3: Configure Claude Desktop

Claude Desktop's MCP configuration is stored in a JSON file. The location varies by operating system:

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

If the file doesn't exist, create it with the following content:

**For macOS/Linux:**
```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "/Users/YOUR_USERNAME/.dotnet/tools/wpf-to-avalonia-mcp",
      "args": [],
      "env": {}
    }
  }
}
```

**For Windows:**
```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "C:\\Users\\YOUR_USERNAME\\.dotnet\\tools\\wpf-to-avalonia-mcp.exe",
      "args": [],
      "env": {}
    }
  }
}
```

**Important:** Replace `YOUR_USERNAME` with your actual username.

### Step 4: Restart Claude Desktop

After saving the configuration file, completely quit and restart Claude Desktop for the changes to take effect.

## Verification

To verify the integration is working:

1. Open Claude Desktop
2. Start a new conversation
3. Type: "What MCP tools are available?"
4. Claude should list the WpfToAvalonia tools, including:
   - `analyze_project`
   - `list_analyzers`
   - `apply_fix`
   - `batch_convert`
   - `preview_fixes`
   - `get_diagnostic_info`
   - `validate_project`
   - And more...

If the tools don't appear, check the troubleshooting section below.

## Example Workflows

### Workflow 1: Analyze a WPF Project

```
You: I have a WPF project at C:\Projects\MyWpfApp\MyWpfApp.csproj.
     Can you analyze it for Avalonia migration issues?

Claude: I'll analyze your WPF project for migration issues.
        [Uses analyze_project tool]

        Found 15 issues:
        - 8 WPFAV001 (DependencyProperty usage)
        - 4 WPFAV002 (Event routing)
        - 3 WPFAV003 (XAML namespace issues)

        Would you like me to show you details about any specific issue?
```

### Workflow 2: Apply Fixes with Preview

```
You: Show me what changes would be made to fix the WPFAV001 issues in
     MainWindow.xaml.cs at line 45, column 9.

Claude: I'll preview the available fixes for that diagnostic.
        [Uses preview_fixes tool]

        Available fixes:
        1. Convert DependencyProperty to StyledProperty
        2. Convert DependencyProperty to DirectProperty

        Here's the diff for option 1:
        [Shows unified diff]

        Would you like me to apply this fix?

You: Yes, apply fix option 1.

Claude: I'll apply the fix now.
        [Uses apply_fix tool]

        Successfully applied fix "Convert DependencyProperty to StyledProperty"
        Modified files:
        - C:\Projects\MyWpfApp\MainWindow.xaml.cs
```

### Workflow 3: Batch Convert Multiple Files

```
You: Can you convert all DependencyProperty issues (WPFAV001) in my project?

Claude: I'll batch convert all WPFAV001 diagnostics in your project.
        [Uses batch_convert tool]

        Successfully converted 8 diagnostics:
        - WPFAV001: 8 fixed

        Modified files:
        - MainWindow.xaml.cs
        - UserControl1.xaml.cs
        - CustomControl.cs

        All files have been updated. Would you like me to analyze the
        project again to see remaining issues?
```

### Workflow 4: Get Migration Guidance

```
You: I'm not sure what WPFAV003 means. Can you explain it?

Claude: I'll get detailed information about that diagnostic.
        [Uses get_diagnostic_info tool]

        WPFAV003: XAML Namespace Migration

        Category: XamlConversion
        Severity: Warning

        Description: WPF XAML namespaces need to be updated to Avalonia equivalents.

        Common patterns:
        - xmlns:wpf="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          → xmlns="https://github.com/avaloniaui"

        Migration guide:
        [Shows detailed migration steps]

        Would you like me to find and fix these issues in your project?
```

### Workflow 5: Validate Project Structure

```
You: Before I start migrating, can you validate my project is ready?

Claude: I'll validate your project structure.
        [Uses validate_project tool]

        Project validation results:
        ✓ Project path exists
        ✓ Can load workspace
        ✓ Found 3 projects in solution

        Projects:
        1. MyWpfApp (C#, WPF, .NET 8.0)
        2. MyWpfApp.Core (C#, Class Library, .NET 8.0)
        3. MyWpfApp.Tests (C#, xUnit, .NET 8.0)

        Recommendations:
        - Consider migrating MyWpfApp.Core first (no UI dependencies)
        - Ensure all NuGet packages are up to date
        - Review test project compatibility with Avalonia

        Ready to proceed with migration!
```

## Advanced Configuration

### Custom Configuration File

You can specify a custom configuration file for the MCP server:

```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "/Users/YOUR_USERNAME/.dotnet/tools/wpf-to-avalonia-mcp",
      "args": ["--config", "/path/to/custom-config.json"],
      "env": {}
    }
  }
}
```

### Environment Variables

Configure the server behavior using environment variables:

```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "/Users/YOUR_USERNAME/.dotnet/tools/wpf-to-avalonia-mcp",
      "args": [],
      "env": {
        "WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES": "20",
        "WPF_TO_AVALONIA_MCP_TIMEOUTS__ANALYSIS_TIMEOUT_SECONDS": "300",
        "WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL": "Debug"
      }
    }
  }
}
```

Common environment variables:
- `WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES` - Maximum cached workspaces (default: 10)
- `WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__IDLE_TIMEOUT_MINUTES` - Cache timeout (default: 30)
- `WPF_TO_AVALONIA_MCP_TIMEOUTS__ANALYSIS_TIMEOUT_SECONDS` - Analysis timeout (default: 120)
- `WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL` - Log level (default: Information)

### Logging Configuration

To enable detailed logging for debugging:

```json
{
  "mcpServers": {
    "wpf-to-avalonia": {
      "command": "/Users/YOUR_USERNAME/.dotnet/tools/wpf-to-avalonia-mcp",
      "args": [],
      "env": {
        "WPF_TO_AVALONIA_MCP_LOGGING__MINIMUM_LEVEL": "Debug",
        "WPF_TO_AVALONIA_MCP_LOGGING__CONSOLE_ENABLED": "true",
        "WPF_TO_AVALONIA_MCP_LOGGING__FILE_ENABLED": "true",
        "WPF_TO_AVALONIA_MCP_LOGGING__FILE_PATH": "/tmp/wpf-to-avalonia-mcp.log"
      }
    }
  }
}
```

## Effective Prompts for Claude

### Initial Project Analysis
```
I have a WPF project at [PATH]. Can you:
1. Validate the project structure
2. Analyze for Avalonia migration issues
3. Provide a summary of what needs to be migrated
4. Suggest a migration order (easiest to hardest)
```

### Incremental Migration
```
Let's migrate my WPF project incrementally:
1. Start with [SPECIFIC_FILE]
2. Show me what will change before applying fixes
3. Apply the fixes only if I approve
4. Re-analyze after each fix to verify success
```

### Batch Processing
```
I want to batch convert all issues of type [DIAGNOSTIC_ID] in my project.
Please:
1. Show me how many instances exist
2. Preview a sample of the changes
3. Apply all fixes if the preview looks good
4. Report any files that couldn't be fixed automatically
```

### Learning Mode
```
I'm new to Avalonia. For each type of issue you find:
1. Explain what the WPF pattern is
2. Explain the Avalonia equivalent
3. Show me the code before and after
4. Provide links to relevant Avalonia documentation
```

### Safety-First Approach
```
I want to be very careful with this migration:
1. Use dry-run mode for all batch operations
2. Always preview changes before applying
3. Only modify one file at a time
4. After each change, re-analyze to catch any new issues
```

## Troubleshooting

### Issue: Claude doesn't see the MCP tools

**Symptoms:** Claude responds "I don't have access to those tools" or similar.

**Solutions:**
1. Verify the configuration file path is correct for your OS
2. Check that the `command` path points to the actual executable
3. Ensure the JSON syntax is valid (use a JSON validator)
4. Completely quit Claude Desktop (not just close the window) and restart
5. Check Claude Desktop logs for errors:
   - macOS: `~/Library/Logs/Claude/`
   - Windows: `%APPDATA%\Claude\Logs\`

### Issue: Server fails to start

**Symptoms:** Claude reports "Failed to connect to MCP server" or timeout errors.

**Solutions:**
1. Verify .NET 9.0 SDK is installed: `dotnet --version`
2. Test the server manually: `wpf-to-avalonia-mcp --version`
3. Check for port conflicts or firewall issues
4. Review server logs if file logging is enabled
5. Try running the server with `--verbose` flag for detailed output

### Issue: Tools run but produce errors

**Symptoms:** Tools execute but return error messages.

**Solutions:**
1. Verify the project path is absolute, not relative
2. Check that MSBuild can load the project: `dotnet build [PROJECT_PATH]`
3. Ensure all NuGet packages are restored
4. Check for special characters in file paths
5. Verify you have read/write permissions on the project directory

### Issue: Performance is slow

**Symptoms:** Tools take a long time to respond, especially on large projects.

**Solutions:**
1. Increase workspace cache size: `WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__MAX_WORKSPACES=20`
2. Increase timeouts: `WPF_TO_AVALONIA_MCP_TIMEOUTS__ANALYSIS_TIMEOUT_SECONDS=300`
3. Close other applications to free up memory
4. Use `target_files` parameter to analyze specific files instead of entire project
5. Consider breaking large solutions into smaller projects

### Issue: Changes not being detected

**Symptoms:** File system watcher doesn't trigger workspace reloads after external changes.

**Solutions:**
1. Enable file watching: `WPF_TO_AVALONIA_MCP_WORKSPACE_CACHE__ENABLE_FILE_WATCHING=true`
2. Manually clear the cache: Use `clear_workspace_cache` tool
3. Restart Claude Desktop to reinitialize the server
4. Check that the project directory is not on a network drive (watchers may not work)

## Best Practices

### 1. Start Small
Begin with a single file or small component before migrating entire projects. This helps you:
- Understand the migration patterns
- Verify the tools work correctly
- Build confidence with the process

### 2. Use Version Control
Always commit your changes before running batch operations:
```bash
git add .
git commit -m "Pre-migration snapshot"
```

This allows you to easily revert if needed.

### 3. Preview Before Applying
Always use `preview_fixes` before `apply_fix` or `batch_convert` to:
- Understand what will change
- Verify the fix is appropriate
- Catch unexpected modifications

### 4. Incremental Verification
After applying fixes:
1. Re-analyze the project
2. Build the project: `dotnet build`
3. Run tests: `dotnet test`
4. Verify functionality manually

### 5. Understand the Diagnostics
Use `get_diagnostic_info` to learn about each diagnostic type before fixing it. This helps you:
- Understand the migration pattern
- Know when manual intervention is needed
- Learn Avalonia best practices

### 6. Keep Dependencies Updated
Ensure all packages are up to date:
```bash
dotnet list package --outdated
dotnet add package Avalonia --version [LATEST]
```

## Integration with Development Workflow

### Daily Development
- Keep Claude Desktop open while working on WPF-to-Avalonia migration
- Ask Claude to analyze files as you work on them
- Use Claude to explain unfamiliar Avalonia patterns
- Let Claude suggest optimal migration strategies

### Code Review
- Before committing, ask Claude to analyze your changes
- Use `validate_project` to ensure workspace is healthy
- Check for any new issues introduced during manual edits

### Continuous Integration
While the MCP server is designed for interactive use, you can also use the underlying CLI tools in CI:
```bash
# In your CI pipeline
dotnet tool install --global WpfToAvaloniaAnalyzers.Cli
wpf-to-avalonia analyze --project MyProject.csproj --format json > analysis.json
```

## Security Considerations

### File System Access
The MCP server requires read/write access to your project files. To limit access:
1. Only run on trusted projects
2. Review preview diffs before applying changes
3. Use version control to track all changes
4. Consider using a dedicated user account with limited permissions

### Network Security
The MCP server runs locally and does not make network requests. However:
- MSBuild may download NuGet packages during workspace loading
- Ensure your NuGet configuration is secure
- Use package signature verification

### Sensitive Data
The MCP server processes your source code locally:
- No code is sent to external servers (except Claude's processing)
- Workspace data is cached in memory only
- No persistent storage of source code outside your project directory

## Additional Resources

- [MCP Server Guide](./MCP_SERVER_GUIDE.md) - Complete server reference
- [Avalonia Documentation](https://docs.avaloniaui.net/) - Official Avalonia docs
- [WPF to Avalonia Migration Guide](https://docs.avaloniaui.net/guides/migration) - Official migration guide
- [Claude Desktop Documentation](https://claude.ai/docs) - Claude Desktop help

## Support

If you encounter issues not covered in this guide:

1. Check the [GitHub Issues](https://github.com/YourRepo/WpfToAvaloniaAnalyzer/issues)
2. Review server logs (if enabled)
3. Test the underlying CLI tools directly
4. Open a new issue with:
   - Claude Desktop version
   - .NET SDK version
   - MCP server version
   - Configuration file content (sanitized)
   - Error messages or unexpected behavior description

## Version History

- **1.0.0** (2024-01) - Initial release with Claude Desktop support
