# Example Prompts for AI Agents

This document provides a comprehensive collection of effective prompts for using the WpfToAvalonia MCP Server with AI agents like Claude, GitHub Copilot, and other LLM-based assistants.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Analysis Prompts](#analysis-prompts)
3. [Code Transformation Prompts](#code-transformation-prompts)
4. [Learning and Discovery](#learning-and-discovery)
5. [Batch Operations](#batch-operations)
6. [Project Management](#project-management)
7. [Debugging and Troubleshooting](#debugging-and-troubleshooting)
8. [Advanced Workflows](#advanced-workflows)
9. [Team Collaboration](#team-collaboration)
10. [Prompt Templates](#prompt-templates)

## Getting Started

### Initial Setup Verification

```
Verify that the WpfToAvalonia MCP tools are available and working correctly.
```

### List Available Tools

```
What WPF to Avalonia migration tools do you have access to? Please list them with brief descriptions.
```

### Check Server Information

```
Show me the WpfToAvalonia MCP server information including version, capabilities, and configuration.
```

### Understand Available Analyzers

```
List all available WPF to Avalonia analyzers, including their IDs, categories, and what they detect.
```

## Analysis Prompts

### Basic Project Analysis

```
I have a WPF project at [PROJECT_PATH]. Please analyze it for Avalonia migration issues and provide a summary of what needs to be migrated.
```

Example with specific path:
```
Analyze C:\Projects\MyWpfApp\MyWpfApp.csproj for WPF to Avalonia migration issues. Show me:
1. Total number of issues
2. Breakdown by diagnostic type
3. Most common issues
4. Recommended migration order
```

### Focused Analysis

```
Analyze only [DIAGNOSTIC_ID] issues in my project at [PROJECT_PATH]. For example, only show me DependencyProperty migration issues (WPFAV001).
```

### Severity-Based Analysis

```
Analyze my project at [PROJECT_PATH] but only show me errors and warnings, not informational messages.
```

### File-Specific Analysis

```
I'm working on the file [FILE_PATH] in my WPF project. What Avalonia migration issues does this file have?
```

### Analysis with Context

```
I'm migrating a WPF project to Avalonia. Analyze [PROJECT_PATH] and explain:
1. Which parts will be easiest to migrate
2. Which parts will require manual work
3. Any breaking changes I should be aware of
4. Estimated effort for migration
```

### Comparative Analysis

```
I've manually migrated some files. Compare the current state of my project at [PROJECT_PATH] with WPF best practices. What issues remain?
```

## Code Transformation Prompts

### Preview Before Applying

```
Show me what changes would be made if I fix [DIAGNOSTIC_ID] at line [LINE], column [COLUMN] in file [FILE_PATH]. Don't apply the changes yet, just preview them.
```

Example:
```
Preview the fix for WPFAV001 at line 45, column 9 in C:\Projects\MyApp\MainWindow.xaml.cs. Show me the before and after code in a diff format.
```

### Apply Single Fix

```
Apply the fix for [DIAGNOSTIC_ID] at line [LINE], column [COLUMN] in file [FILE_PATH]. Use fix option [INDEX] if there are multiple available fixes.
```

Example with approval:
```
I reviewed the preview. Please apply the "Convert to StyledProperty" fix for WPFAV001 at line 45, column 9 in MainWindow.xaml.cs.
```

### Step-by-Step Migration

```
Let's migrate [FILE_PATH] step by step. For each issue:
1. Explain what the WPF code is doing
2. Show the Avalonia equivalent
3. Preview the change
4. Wait for my approval before applying
5. After each fix, re-analyze to catch any new issues
```

### Safe Migration

```
I want to be very careful. For each fix in [FILE_PATH]:
1. Show me exactly what will change
2. Explain why the change is necessary
3. Tell me about any potential side effects
4. Only apply if I explicitly approve
```

### Batch Convert Specific Type

```
Convert all [DIAGNOSTIC_ID] issues in my project at [PROJECT_PATH]. First, show me how many instances there are, then ask for confirmation before proceeding.
```

Example:
```
Find all WPFAV001 (DependencyProperty) issues in my project and convert them to Avalonia StyledProperty. Show me a summary first, then ask if I want to proceed with batch conversion.
```

## Learning and Discovery

### Explain a Diagnostic

```
I don't understand [DIAGNOSTIC_ID]. Can you explain:
1. What WPF pattern it detects
2. Why it needs to change for Avalonia
3. The Avalonia equivalent
4. Common migration patterns
5. Any gotchas or edge cases
```

Example:
```
Explain WPFAV003 to me. What's the difference between WPF and Avalonia XAML namespaces? Show me examples of before and after code.
```

### Learn By Example

```
For each type of issue in my project at [PROJECT_PATH], show me:
1. An example from my actual code
2. What the WPF pattern does
3. The Avalonia equivalent
4. Why the change is necessary
5. A link to relevant Avalonia documentation
```

### Pattern Discovery

```
What are the most common WPF patterns in my project at [PROJECT_PATH] that need to change for Avalonia? Teach me about each one with examples from my code.
```

### Compare WPF and Avalonia

```
I'm new to Avalonia. For the issues you found in my project, explain the architectural differences between WPF and Avalonia that make these changes necessary.
```

### Migration Guide Generation

```
Based on the analysis of my project at [PROJECT_PATH], create a personalized migration guide that covers:
1. Overview of my project's WPF patterns
2. Avalonia equivalents for each pattern
3. Step-by-step migration instructions
4. Code examples from my actual project
5. Testing strategies
```

## Batch Operations

### Dry Run First

```
I want to batch convert all [DIAGNOSTIC_ID] issues in my project at [PROJECT_PATH]. First, run in dry-run mode to show me what would change without actually modifying files.
```

### Folder-Specific Batch

```
Batch convert all WPF issues in the [FOLDER_PATH] folder. Only process files in this folder and its subfolders.
```

Example:
```
Convert all DependencyProperty issues (WPFAV001) in the C:\Projects\MyApp\Views folder. Show me which files will be modified before starting.
```

### Multiple Diagnostic Types

```
Batch convert multiple issue types in my project:
1. WPFAV001 (DependencyProperty)
2. WPFAV002 (Event routing)
3. WPFAV003 (XAML namespaces)

Show progress as you go and report any issues that couldn't be fixed automatically.
```

### Incremental Batch

```
I want to batch convert files one at a time so I can review each change. For each file with [DIAGNOSTIC_ID] issues:
1. Show me the file path
2. Show me how many issues it has
3. Apply fixes to that file
4. Wait for my confirmation before moving to the next file
```

### Safe Batch with Rollback

```
Batch convert all [DIAGNOSTIC_ID] issues in [PROJECT_PATH]. Before starting:
1. Check that I'm using version control
2. Create a commit or warn me to create one
3. Apply the fixes
4. Tell me how to rollback if needed
```

## Project Management

### Project Validation

```
Before I start migrating, validate my WPF project at [PROJECT_PATH]. Check:
1. Can the project be loaded?
2. How many projects are in the solution?
3. What are the project dependencies?
4. Are there any blockers to migration?
5. Do you recommend any preparation steps?
```

### Migration Roadmap

```
Create a migration roadmap for my project at [PROJECT_PATH]. Include:
1. Analysis of current state
2. Recommended migration order (files/components)
3. Estimated effort for each phase
4. Potential risks or challenges
5. Milestones and checkpoints
```

### Progress Tracking

```
I'm in the middle of migrating my project. Analyze [PROJECT_PATH] and show me:
1. What percentage is migrated
2. Which files are fully migrated
3. Which files still have issues
4. What types of issues remain
5. Recommended next steps
```

### Impact Analysis

```
I'm planning to migrate [FILE_PATH] or [FOLDER_PATH]. Before I start, tell me:
1. What files depend on this code
2. What other files might be affected
3. What risks should I be aware of
4. What should I test after migration
```

### Workspace Management

```
Show me the current workspace cache statistics. How many workspaces are cached? Should I clear the cache?
```

## Debugging and Troubleshooting

### Understand Failures

```
I tried to apply a fix but it failed. For the diagnostic at [FILE_PATH] line [LINE], column [COLUMN]:
1. What fix was attempted?
2. Why did it fail?
3. What can I do manually?
4. Are there alternative approaches?
```

### Compare Expected vs Actual

```
I applied a fix but the result doesn't look right. Analyze [FILE_PATH] and tell me:
1. What changed
2. If there are any remaining issues
3. If the fix introduced new problems
4. What I should verify manually
```

### Re-analyze After Manual Edits

```
I manually edited some files. Re-analyze my project at [PROJECT_PATH] to:
1. Verify my changes are correct
2. Check if I introduced new issues
3. Find any remaining migration work
4. Suggest improvements
```

### Build Error Investigation

```
After applying fixes, my project doesn't build. The error is [ERROR_MESSAGE]. Can you:
1. Analyze what might have caused this
2. Check if the migration tools missed something
3. Suggest how to fix it
```

### Performance Issues

```
The analysis is running slowly on my large project at [PROJECT_PATH]. Can you:
1. Check the workspace cache status
2. Recommend configuration optimizations
3. Suggest ways to analyze incrementally
```

## Advanced Workflows

### Incremental Migration Strategy

```
I want to migrate my project incrementally while keeping it buildable. Help me plan:
1. Which files/components can be migrated first without breaking the build
2. How to temporarily maintain compatibility between migrated and non-migrated code
3. What interfaces or abstractions might help
4. Testing strategy for each increment
```

### Multi-Project Solution

```
My solution at [SOLUTION_PATH] has multiple projects. Help me:
1. Analyze all projects for migration issues
2. Determine project dependencies
3. Recommend migration order based on dependencies
4. Identify shared code that affects multiple projects
```

### Custom Migration Patterns

```
In my project, we have custom WPF patterns like [DESCRIBE_PATTERN]. Can you:
1. Analyze how common this pattern is in our codebase
2. Suggest an Avalonia equivalent
3. Help me find all instances
4. Create a migration strategy for this pattern
```

### Testing-Driven Migration

```
For each file I migrate, help me:
1. Identify what functionality should be tested
2. Suggest test cases for the migrated code
3. Compare behavior before and after migration
4. Verify the migration preserves functionality
```

### Documentation Generation

```
As I migrate files, generate documentation that explains:
1. What WPF patterns were used
2. What Avalonia patterns replaced them
3. Why the changes were made
4. Any behavioral differences
5. Migration notes for future reference
```

## Team Collaboration

### Onboarding Prompt

```
I'm new to this WPF to Avalonia migration project. Analyze [PROJECT_PATH] and give me:
1. Overview of the project structure
2. Current migration status
3. Common issues and how we're handling them
4. Where I should start contributing
5. Resources to learn about our migration patterns
```

### Code Review Prompt

```
I'm reviewing a pull request that migrates [FILES]. Analyze these files and help me verify:
1. All WPF issues were addressed
2. The Avalonia patterns are correct
3. No new issues were introduced
4. The changes follow best practices
5. Any potential improvements
```

### Migration Assignment

```
Our team needs to divide migration work. Analyze [PROJECT_PATH] and suggest:
1. Natural boundaries for dividing work (by folder, component, etc.)
2. Which parts are independent and can be done in parallel
3. Which parts have dependencies and need coordination
4. Estimated effort for each part
```

### Knowledge Sharing

```
Create a migration guide for our team based on [PROJECT_PATH] that includes:
1. Common patterns we've encountered
2. How we've solved each type of issue
3. Gotchas and lessons learned
4. Code examples from our actual codebase
5. Best practices we've established
```

### Status Report

```
Generate a migration status report for [PROJECT_PATH] suitable for sharing with stakeholders:
1. Overall progress percentage
2. What's been completed
3. What's in progress
4. What's remaining
5. Any blockers or risks
6. Timeline estimate
```

## Prompt Templates

### Template: Analyze and Plan

```
[Action: analyze] [Target: project/file/folder at PATH]
[Focus: specific diagnostics or "all"]
[Output: summary/detailed/with-examples]
[Additional: migration plan/effort estimate/risk assessment]
```

Example:
```
Analyze my entire project at C:\Projects\MyApp\MyApp.sln focusing on all diagnostics. Provide a detailed output with examples from my code and create a migration plan with effort estimates.
```

### Template: Preview and Apply

```
[Action: preview/apply] [Diagnostic: ID] [Location: file, line, column]
[Options: specific fix index or "default"]
[Safety: require-approval/auto-apply]
[Post-action: re-analyze/verify-build/none]
```

Example:
```
Preview WPFAV001 at MainWindow.xaml.cs line 45 column 9. If it looks good, apply the default fix and then re-analyze the file to check for new issues.
```

### Template: Batch Operation

```
[Action: batch-convert] [Diagnostic: IDs or "all"]
[Scope: project/folder/specific-files]
[Mode: dry-run/apply]
[Progress: show-progress/silent]
[Reporting: summary/detailed/per-file]
```

Example:
```
Batch-convert WPFAV001 and WPFAV002 in the Views folder. Use dry-run mode first, show progress, and provide a detailed report of what would change.
```

### Template: Learning

```
[Action: explain/teach/show-examples] [Topic: diagnostic/pattern/concept]
[Depth: brief/detailed/comprehensive]
[Include: code-examples/documentation-links/best-practices]
[Source: my-project/general]
```

Example:
```
Teach me about DependencyProperty migration (WPFAV001) in detail. Include code examples from my project, links to Avalonia documentation, and best practices.
```

### Template: Project Management

```
[Action: validate/plan/track/report] [Project: PATH]
[Focus: structure/dependencies/progress/risks]
[Output: checklist/roadmap/status-report/recommendations]
[Audience: me/team/stakeholders]
```

Example:
```
Create a migration roadmap for my project at C:\Projects\MyApp including structure analysis, risk assessment, and a checklist of steps. Format it for sharing with my team.
```

## Best Practices for Prompts

### Be Specific

**Less Effective:**
```
Help me migrate my project.
```

**More Effective:**
```
Analyze my WPF project at C:\Projects\MyApp\MyApp.csproj and create a step-by-step migration plan. Start with the easiest changes and explain each type of issue you find.
```

### Provide Context

**Less Effective:**
```
Fix the error at line 45.
```

**More Effective:**
```
I'm migrating MainWindow.xaml.cs from WPF to Avalonia. There's a WPFAV001 diagnostic at line 45, column 9 about DependencyProperty. Preview the fix first, explain what will change, then apply it if it looks correct.
```

### Request Explanations

**Less Effective:**
```
Apply all fixes.
```

**More Effective:**
```
Apply fixes for all WPFAV001 issues, but for each file, explain what pattern you're changing and why the Avalonia approach is different.
```

### Use Step-by-Step Approach

**Less Effective:**
```
Migrate everything at once.
```

**More Effective:**
```
Let's migrate incrementally:
1. First, analyze and show me all issue types
2. Then, we'll tackle WPFAV001 issues one file at a time
3. After each file, I'll review and approve
4. Then we'll move to the next issue type
```

### Ask for Verification

**Less Effective:**
```
Apply the fix.
```

**More Effective:**
```
Apply the fix, then re-analyze the file to verify no new issues were introduced and confirm the fix worked correctly.
```

## Context-Specific Prompts

### For Large Projects

```
My project at [PATH] is very large (1000+ files). Help me:
1. Analyze just the Core/Business logic first (no UI)
2. Identify which parts have no WPF dependencies
3. Create a plan to migrate in phases
4. Suggest ways to maintain a working build throughout
```

### For Legacy Projects

```
My WPF project at [PATH] is 10+ years old and uses older patterns. Help me:
1. Identify not just WPF-to-Avalonia issues, but also modernization opportunities
2. Suggest which legacy patterns should be updated
3. Balance migration with code modernization
4. Prioritize changes that provide the most value
```

### For Projects with Custom Controls

```
My project at [PATH] has many custom WPF controls. Help me:
1. Identify all custom controls
2. Analyze their dependencies on WPF
3. Suggest migration strategies for each control
4. Identify which controls can share migration patterns
```

### For MVVM Projects

```
My project at [PATH] uses MVVM pattern extensively. Help me:
1. Analyze ViewModels separately from Views
2. Identify which parts are framework-agnostic
3. Focus migration on View layer
4. Ensure ViewModel compatibility with Avalonia
```

## Interactive Prompt Patterns

### Question-Answer Pattern

```
I'll ask you questions about migrating specific parts of my project at [PATH]. For each question:
1. Analyze the relevant code
2. Explain the WPF pattern and Avalonia equivalent
3. Show me code examples
4. Provide migration recommendations

My first question: [YOUR_QUESTION]
```

### Pair Programming Pattern

```
Let's pair program this migration. I'll work on [FILE_PATH] and you:
1. Watch for potential issues as I describe my changes
2. Suggest Avalonia patterns when I'm unsure
3. Analyze my code after I make changes
4. Point out if I'm introducing issues
5. Help me test my changes
```

### Teaching Pattern

```
Teach me Avalonia by migrating [PROJECT_PATH]. For each issue:
1. Stop and explain the concept
2. Show me documentation or examples
3. Let me try to write the Avalonia code
4. Review my attempt and provide feedback
5. Show the correct solution if I struggle
```

## Version Control Integration Prompts

### Pre-Commit Check

```
Before I commit my migration changes, analyze [PROJECT_PATH] and verify:
1. All intended changes were made
2. No new issues were introduced
3. The migration is complete for this phase
4. Build should succeed
5. Generate a suitable commit message
```

### Branch Comparison

```
I'm working on a migration branch. Compare my current branch with main/master for [PROJECT_PATH]:
1. What files changed
2. What issues were fixed
3. What issues remain
4. If the changes are safe to merge
```

### Merge Conflict Resolution

```
I have merge conflicts in [FILE_PATH] during my migration. Help me:
1. Understand what changed in both branches
2. Determine the correct Avalonia pattern
3. Resolve conflicts correctly
4. Verify the merged code is correct
```

## Documentation Generation Prompts

### Migration Notes

```
Generate migration notes for [FILE_PATH] that document:
1. What WPF patterns were used
2. What Avalonia patterns replaced them
3. Why the changes were necessary
4. Any manual adjustments needed
5. Testing recommendations
```

### API Comparison

```
Create an API comparison document for my project showing:
1. WPF APIs we used
2. Avalonia equivalents
3. Behavioral differences
4. Code examples of each
5. Migration complexity (easy/medium/hard)
```

### Team Wiki

```
Generate wiki pages for our team about migrating this project:
1. Overview page with project structure
2. Pattern guide (WPF → Avalonia mappings)
3. FAQ based on issues we've encountered
4. Troubleshooting guide
5. Best practices we've established
```

## Additional Resources

For more information on using these prompts effectively:

- [MCP Server Guide](./MCP_SERVER_GUIDE.md) - Complete tool reference
- [Claude Desktop Integration](./CLAUDE_DESKTOP_INTEGRATION.md) - Claude-specific usage
- [GitHub Copilot Integration](./GITHUB_COPILOT_INTEGRATION.md) - Copilot-specific usage
- [VS Code MCP Integration](./VSCODE_MCP_INTEGRATION.md) - VS Code-specific usage
- [Agent Configuration Reference](./AGENT_CONFIGURATION_REFERENCE.md) - Configuration options

## Contributing Prompts

Have you discovered effective prompts that aren't listed here? We welcome contributions! Please submit:

1. The prompt text
2. Context where it's useful
3. Example output or results
4. Any tips for using it effectively

## Version History

- **1.0.0** (2024-01) - Initial collection of example prompts
