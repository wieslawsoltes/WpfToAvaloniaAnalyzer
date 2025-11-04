# Example Workflows

This document provides complete, end-to-end example workflows for using the WpfToAvalonia MCP Server with AI agents. Each workflow includes the agent prompts, expected tool calls, and results.

## Table of Contents

1. [Workflow 1: Initial Project Analysis](#workflow-1-initial-project-analysis)
2. [Workflow 2: Step-by-Step File Migration](#workflow-2-step-by-step-file-migration)
3. [Workflow 3: Batch Convert Specific Issue Type](#workflow-3-batch-convert-specific-issue-type)
4. [Workflow 4: Learning Mode Migration](#workflow-4-learning-mode-migration)
5. [Workflow 5: Project Validation and Planning](#workflow-5-project-validation-and-planning)
6. [Workflow 6: Fix with Preview and Approval](#workflow-6-fix-with-preview-and-approval)
7. [Workflow 7: Incremental Team Migration](#workflow-7-incremental-team-migration)
8. [Workflow 8: Troubleshooting Failed Fixes](#workflow-8-troubleshooting-failed-fixes)

---

## Workflow 1: Initial Project Analysis

**Scenario:** You have a WPF project and want to understand the scope of migration work required.

### Agent Prompt

```
I have a WPF project at C:\Projects\MyWpfApp\MyWpfApp.csproj. Can you analyze it
for Avalonia migration issues and provide a summary of what needs to be migrated?
```

### Agent Actions

1. **Validate the project first:**

```json
Tool: validate_project
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj"
}
```

**Response:**
```json
{
  "success": true,
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj",
  "pathExists": true,
  "canLoadWorkspace": true,
  "projectCount": 1,
  "projects": [
    {
      "name": "MyWpfApp",
      "language": "C#",
      "targetFramework": "net8.0-windows",
      "outputType": "WinExe"
    }
  ],
  "recommendations": [
    "Project is ready for migration",
    "Consider adding unit tests before migration"
  ]
}
```

2. **Run full analysis:**

```json
Tool: analyze_project
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj"
}
```

**Response:**
```json
{
  "success": true,
  "diagnostics": [
    {
      "id": "WPFAV001",
      "severity": "Warning",
      "message": "DependencyProperty should be converted to StyledProperty",
      "filePath": "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs",
      "line": 45,
      "column": 9,
      "projectName": "MyWpfApp"
    },
    // ... 14 more diagnostics
  ],
  "summary": {
    "totalDiagnostics": 15,
    "errorCount": 0,
    "warningCount": 15,
    "infoCount": 0,
    "bySeverity": {
      "Error": 0,
      "Warning": 15,
      "Info": 0,
      "Hidden": 0
    },
    "byDiagnosticId": {
      "WPFAV001": 8,
      "WPFAV002": 4,
      "WPFAV003": 3
    },
    "affectedFiles": [
      "MainWindow.xaml.cs",
      "SettingsWindow.xaml.cs",
      "UserControl1.xaml.cs",
      "App.xaml"
    ]
  },
  "projectInfo": {
    "projectName": "MyWpfApp",
    "totalFiles": 25,
    "analyzedFiles": 25
  }
}
```

### Agent Response

```
I've analyzed your WPF project and found 15 migration issues:

**Summary:**
- 8 WPFAV001 issues: DependencyProperty declarations that need conversion to StyledProperty
- 4 WPFAV002 issues: Event routing patterns that need updates
- 3 WPFAV003 issues: XAML namespaces that need to be updated

**Affected files:**
- MainWindow.xaml.cs
- SettingsWindow.xaml.cs
- UserControl1.xaml.cs
- App.xaml

**Recommended migration order:**
1. Start with WPFAV003 (XAML namespaces) - these are straightforward
2. Then tackle WPFAV001 (DependencyProperty) - most common issue
3. Finally handle WPFAV002 (Event routing) - may require some manual work

**Next steps:**
Would you like me to:
- Explain what each diagnostic type means?
- Show you the specific issues in each file?
- Start migrating a specific file?
- Run a batch conversion for a specific issue type?
```

---

## Workflow 2: Step-by-Step File Migration

**Scenario:** You want to migrate a single file carefully, reviewing each change before applying it.

### Agent Prompt

```
Let's migrate MainWindow.xaml.cs step by step. For each issue:
1. Show me what the problem is
2. Preview the fix
3. Wait for my approval before applying
4. Re-analyze after each fix
```

### Agent Actions

**Step 1: Analyze the specific file**

```json
Tool: analyze_project
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj",
  "diagnosticIds": null,
  "minimumSeverity": "Warning"
}
```

Filter results to MainWindow.xaml.cs - found 3 issues in this file.

**Step 2: Preview first fix**

```json
Tool: preview_fixes
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj",
  "filePath": "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs",
  "diagnosticId": "WPFAV001",
  "line": 45,
  "column": 9
}
```

**Response:**
```json
{
  "success": true,
  "availableFixes": [
    {
      "title": "Convert DependencyProperty to StyledProperty",
      "description": "Converts WPF DependencyProperty to Avalonia StyledProperty",
      "index": 0
    },
    {
      "title": "Convert DependencyProperty to DirectProperty",
      "description": "Converts WPF DependencyProperty to Avalonia DirectProperty",
      "index": 1
    }
  ],
  "preview": {
    "filePath": "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs",
    "originalCode": "public static readonly DependencyProperty NameProperty =\n    DependencyProperty.Register(nameof(Name), typeof(string), typeof(MainWindow));",
    "fixedCode": "public static readonly StyledProperty<string> NameProperty =\n    AvaloniaProperty.Register<MainWindow, string>(nameof(Name));",
    "diff": "@@ -45,2 +45,2 @@\n-public static readonly DependencyProperty NameProperty =\n-    DependencyProperty.Register(nameof(Name), typeof(string), typeof(MainWindow));\n+public static readonly StyledProperty<string> NameProperty =\n+    AvaloniaProperty.Register<MainWindow, string>(nameof(Name));"
  }
}
```

### Agent Response (Step 2)

```
I found the first issue in MainWindow.xaml.cs at line 45:

**Current WPF code:**
```csharp
public static readonly DependencyProperty NameProperty =
    DependencyProperty.Register(nameof(Name), typeof(string), typeof(MainWindow));
```

**Problem:** This is a WPF DependencyProperty that needs to be converted to Avalonia's property system.

**Available fixes:**
1. Convert to StyledProperty (recommended for properties that support styling)
2. Convert to DirectProperty (for simple properties without styling)

**Proposed change (option 1 - StyledProperty):**
```csharp
public static readonly StyledProperty<string> NameProperty =
    AvaloniaProperty.Register<MainWindow, string>(nameof(Name));
```

**Explanation:**
- StyledProperty is the Avalonia equivalent of DependencyProperty
- It uses generic type parameters for better type safety
- The registration syntax is slightly different but provides the same functionality

Should I apply this fix?
```

### User Response

```
Yes, apply fix option 1
```

### Agent Actions (Step 3)

```json
Tool: apply_fix
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj",
  "filePath": "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs",
  "diagnosticId": "WPFAV001",
  "line": 45,
  "column": 9,
  "fixIndex": 0
}
```

**Response:**
```json
{
  "success": true,
  "appliedFixTitle": "Convert DependencyProperty to StyledProperty",
  "modifiedFiles": [
    "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs"
  ],
  "diffs": [
    {
      "filePath": "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs",
      "diff": "@@ -45,2 +45,2 @@\n-public static readonly DependencyProperty NameProperty =\n-    DependencyProperty.Register(nameof(Name), typeof(string), typeof(MainWindow));\n+public static readonly StyledProperty<string> NameProperty =\n+    AvaloniaProperty.Register<MainWindow, string>(nameof(Name));"
    }
  ]
}
```

**Step 4: Re-analyze to verify**

```json
Tool: analyze_project
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj"
}
```

Check that the WPFAV001 at line 45 is gone and no new issues introduced.

### Agent Response (Step 3-4)

```
✓ Fix applied successfully!

**Modified files:**
- MainWindow.xaml.cs

I've re-analyzed the file and confirmed:
- The WPFAV001 issue at line 45 is resolved
- No new issues were introduced
- 2 more issues remain in this file

**Next issue:**
There's another WPFAV001 at line 58. Would you like me to show you that one next?
```

**Process repeats for remaining issues...**

---

## Workflow 3: Batch Convert Specific Issue Type

**Scenario:** You want to convert all DependencyProperty declarations across your entire project at once.

### Agent Prompt

```
Convert all WPFAV001 (DependencyProperty) issues in my project at
C:\Projects\MyWpfApp\MyWpfApp.csproj. First show me how many instances
there are and which files will be affected, then ask for confirmation.
```

### Agent Actions

**Step 1: Analyze to get count**

```json
Tool: analyze_project
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj",
  "diagnosticIds": ["WPFAV001"]
}
```

**Response:** Found 8 WPFAV001 issues across 3 files.

### Agent Response (Step 1)

```
I found 8 WPFAV001 (DependencyProperty) issues to convert:

**Files affected:**
- MainWindow.xaml.cs: 3 instances
- SettingsWindow.xaml.cs: 2 instances
- UserControl1.xaml.cs: 3 instances

**What will happen:**
All DependencyProperty declarations will be converted to StyledProperty, which is
the Avalonia equivalent. This includes:
- Changing the property type from DependencyProperty to StyledProperty<T>
- Updating the registration call to use AvaloniaProperty.Register<>()
- Adding proper generic type parameters

**This is a safe, automated conversion.** However, I recommend:
1. Ensuring your project is committed to version control
2. Running a build after conversion to catch any issues
3. Reviewing the changes in a diff tool

Would you like me to proceed with the batch conversion?
```

### User Response

```
Yes, proceed
```

### Agent Actions (Step 2)

```json
Tool: batch_convert
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj",
  "diagnosticIds": ["WPFAV001"],
  "dryRun": false
}
```

**Response:**
```json
{
  "success": true,
  "fixedDiagnostics": 8,
  "modifiedFiles": [
    "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs",
    "C:\\Projects\\MyWpfApp\\SettingsWindow.xaml.cs",
    "C:\\Projects\\MyWpfApp\\UserControl1.xaml.cs"
  ],
  "failedFixes": [],
  "summaryByDiagnosticId": {
    "WPFAV001": 8
  },
  "diffs": [
    {
      "filePath": "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs",
      "diff": "@@ -45,2 +45,2 @@\n-public static readonly DependencyProperty NameProperty =\n..."
    }
    // ... more diffs
  ]
}
```

### Agent Response (Step 2)

```
✓ Batch conversion complete!

**Summary:**
- Fixed: 8 diagnostics
- Modified files: 3
- Failed: 0

**Modified files:**
1. MainWindow.xaml.cs (3 conversions)
2. SettingsWindow.xaml.cs (2 conversions)
3. UserControl1.xaml.cs (3 conversions)

**All DependencyProperty declarations have been converted to StyledProperty.**

**Next steps:**
1. Review the changes: `git diff`
2. Build the project: `dotnet build`
3. Fix any compilation errors (usually just missing using statements)
4. Test your application

Would you like me to:
- Analyze the project again to see remaining issues?
- Help you with the next issue type (WPFAV002 - Event routing)?
- Check if the project builds successfully?
```

---

## Workflow 4: Learning Mode Migration

**Scenario:** You're new to Avalonia and want to learn while migrating.

### Agent Prompt

```
I'm new to Avalonia. For each type of issue you find in my project at
C:\Projects\MyWpfApp\MyWpfApp.csproj, teach me about:
1. What the WPF pattern is
2. Why it needs to change
3. The Avalonia equivalent
4. Show me examples from my actual code
```

### Agent Actions

**Step 1: Get diagnostic info and examples**

```json
Tool: analyze_project
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj"
}
```

Then for each diagnostic type found:

```json
Tool: get_diagnostic_info
{
  "diagnosticId": "WPFAV001"
}
```

**Response:**
```json
{
  "success": true,
  "id": "WPFAV001",
  "title": "DependencyProperty Migration",
  "description": "WPF DependencyProperty declarations should be converted to Avalonia StyledProperty or DirectProperty",
  "category": "Property System",
  "defaultSeverity": "Warning",
  "isEnabledByDefault": true,
  "helpLinkUri": "https://docs.avaloniaui.net/docs/data-binding/creating-and-binding-attached-properties",
  "examples": [
    {
      "before": "public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(...)",
      "after": "public static readonly StyledProperty<int> ValueProperty = AvaloniaProperty.Register<MyControl, int>(...)",
      "explanation": "StyledProperty is used for properties that can be styled"
    }
  ],
  "migrationGuide": "1. Determine if property needs styling support...",
  "commonIssues": ["Missing using statements", "Type parameter complexity"]
}
```

### Agent Response

```
I found 3 types of issues in your project. Let me teach you about each one:

---

## Issue Type 1: WPFAV001 - DependencyProperty Migration (8 instances)

### What is it in WPF?

In WPF, `DependencyProperty` is the foundation of the property system. It enables:
- Data binding
- Styling and templating
- Property value inheritance
- Change notification

**Example from your code (MainWindow.xaml.cs, line 45):**
```csharp
public static readonly DependencyProperty NameProperty =
    DependencyProperty.Register(nameof(Name), typeof(string),
        typeof(MainWindow), new PropertyMetadata(default(string)));

public string Name
{
    get => (string)GetValue(NameProperty);
    set => SetValue(NameProperty, value);
}
```

### Why does it need to change for Avalonia?

Avalonia has its own property system that's inspired by WPF but with improvements:
- **Better type safety**: Uses generics instead of object
- **Simpler API**: Cleaner registration syntax
- **Performance**: More efficient property lookup
- **Two types**: StyledProperty (for styling) vs DirectProperty (for simple properties)

### The Avalonia Equivalent

**For properties that need styling (most common):**
```csharp
public static readonly StyledProperty<string> NameProperty =
    AvaloniaProperty.Register<MainWindow, string>(
        nameof(Name),
        defaultValue: default(string));

public string Name
{
    get => GetValue(NameProperty);
    set => SetValue(NameProperty, value);
}
```

**Key differences:**
1. `StyledProperty<string>` instead of `DependencyProperty` - type-safe!
2. `AvaloniaProperty.Register<TOwner, TValue>` - explicit types
3. No need to cast in getter - it's already type-safe
4. Same GetValue/SetValue pattern - familiar!

### When to use StyledProperty vs DirectProperty?

- **StyledProperty**: Properties that can be styled (Background, Foreground, Margin, etc.)
- **DirectProperty**: Simple properties, performance-critical properties, or properties that wrap existing fields

**Your code uses StyledProperty, which is correct for UI properties.**

---

## Issue Type 2: WPFAV002 - Event Routing (4 instances)

### What is it in WPF?

WPF uses routed events that bubble or tunnel through the visual tree...

[Detailed explanation continues for each issue type]

---

Would you like me to:
1. Start migrating the DependencyProperty issues now that you understand them?
2. Continue learning about the other issue types first?
3. Show you more examples from your code?
```

---

## Workflow 5: Project Validation and Planning

**Scenario:** Before starting migration, you want to validate your project and create a detailed plan.

### Agent Prompt

```
Before I start migrating my WPF project at C:\Projects\MyWpfApp\MyWpfApp.csproj,
help me create a comprehensive migration plan. Include:
1. Project validation
2. Issue analysis with effort estimates
3. Recommended migration order
4. Potential risks
5. Testing strategy
```

### Agent Actions

**Step 1: Validate project**

```json
Tool: validate_project
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj"
}
```

**Step 2: Analyze comprehensively**

```json
Tool: analyze_project
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj"
}
```

**Step 3: Get analyzer list**

```json
Tool: list_analyzers
{}
```

### Agent Response

```
# Migration Plan for MyWpfApp

## 1. Project Validation ✓

**Status:** Ready for migration

**Project Details:**
- Name: MyWpfApp
- Framework: .NET 8.0 (Windows)
- Type: WinExe (Desktop Application)
- Files: 25 C# files, 8 XAML files

**Dependencies:**
- No blockers detected
- All NuGet packages are compatible

**Recommendations:**
- ✓ Project structure is good
- ⚠ Add unit tests before migration (currently 0 tests)
- ✓ Using version control (Git detected)

---

## 2. Issue Analysis

**Total Issues: 15**

| Diagnostic | Count | Effort | Auto-Fix | Description |
|------------|-------|--------|----------|-------------|
| WPFAV001 | 8 | Low | ✓ Yes | DependencyProperty → StyledProperty |
| WPFAV002 | 4 | Medium | ✓ Yes | Event routing updates |
| WPFAV003 | 3 | Low | ✓ Yes | XAML namespace updates |

**Effort Estimates:**
- WPFAV001: ~30 minutes (8 instances × ~3-4 min each, all auto-fixable)
- WPFAV002: ~1-2 hours (4 instances, may need manual review)
- WPFAV003: ~15 minutes (3 XAML files, straightforward)

**Total Estimated Effort: 2-3 hours** (including testing)

---

## 3. Recommended Migration Order

### Phase 1: XAML Namespaces (15 min)
**Why first:** Foundation for other changes, no code impact
**Files:** App.xaml, MainWindow.xaml, SettingsWindow.xaml
**Risk:** Very Low
**Action:** Batch convert WPFAV003

### Phase 2: DependencyProperty (30-45 min)
**Why second:** Most common issue, fully automated
**Files:** MainWindow.xaml.cs, SettingsWindow.xaml.cs, UserControl1.xaml.cs
**Risk:** Low (automated with verification)
**Action:** Batch convert WPFAV001
**Verification:** Build project, run any existing tests

### Phase 3: Event Routing (1-2 hours)
**Why last:** May require manual review and adjustments
**Files:** MainWindow.xaml.cs (2), UserControl1.xaml.cs (2)
**Risk:** Medium (behavior changes possible)
**Action:** Manual review of each fix
**Verification:** Test all event-driven functionality

---

## 4. Potential Risks

### Low Risk Issues:
- ✓ All identified issues have automated fixes
- ✓ No breaking API changes required
- ✓ Project size is manageable (25 files)

### Medium Risk Issues:
- ⚠ Event routing changes may affect behavior (4 instances)
  - **Mitigation:** Test all button clicks, menu items, custom events
  - **Fallback:** Review each fix individually, don't batch

### Areas Requiring Manual Work:
- Custom WPF controls (none detected)
- P/Invoke or Windows-specific code (scan for this manually)
- Third-party WPF controls (check Dependencies)

---

## 5. Testing Strategy

### Before Migration:
1. ✓ Create git branch: `git checkout -b feature/avalonia-migration`
2. ✓ Commit current state: `git commit -m "Pre-migration snapshot"`
3. ⚠ Add basic smoke tests (recommended)

### During Migration (After Each Phase):
1. Build project: `dotnet build`
2. Fix any compilation errors
3. Run tests: `dotnet test`
4. Manual smoke test:
   - Launch application
   - Test basic navigation
   - Test data binding
   - Test events (buttons, menus)

### After Migration:
1. Comprehensive testing:
   - All windows and dialogs
   - All user interactions
   - Data binding scenarios
   - Styling and themes
2. Performance check (compare startup time, memory usage)
3. Create test report

---

## 6. Rollback Plan

If issues are encountered:

```bash
# Discard all changes
git reset --hard HEAD

# Or revert to specific commit
git log --oneline
git reset --hard <commit-id>
```

---

## 7. Success Criteria

- [ ] All 15 diagnostics resolved
- [ ] Project builds successfully
- [ ] All existing tests pass (or new tests created)
- [ ] Manual smoke test passes
- [ ] No performance regression
- [ ] Code reviewed and approved

---

## Next Steps

Would you like me to:
1. **Start Phase 1** - Migrate XAML namespaces (15 min, low risk)
2. **Create a backup** - Make sure version control is set up properly
3. **Add tests first** - Create some basic tests before migrating
4. **Review a specific issue** - Look at one of the event routing issues in detail

I recommend starting with option 1 (Phase 1) if you're ready to begin!
```

---

## Workflow 6: Fix with Preview and Approval

**Scenario:** You want to see exactly what will change before applying any fix.

### Agent Prompt

```
I want to fix the WPFAV001 issue at line 45, column 9 in
C:\Projects\MyWpfApp\MainWindow.xaml.cs, but show me exactly what
will change first. Don't apply anything until I approve.
```

### Agent Actions

```json
Tool: preview_fixes
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj",
  "filePath": "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs",
  "diagnosticId": "WPFAV001",
  "line": 45,
  "column": 9
}
```

**Response:**
```json
{
  "success": true,
  "diagnostic": {
    "id": "WPFAV001",
    "message": "DependencyProperty should be converted to StyledProperty",
    "severity": "Warning",
    "location": {
      "filePath": "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs",
      "line": 45,
      "column": 9
    }
  },
  "availableFixes": [
    {
      "title": "Convert DependencyProperty to StyledProperty",
      "description": "Converts WPF DependencyProperty to Avalonia StyledProperty for styleable properties",
      "index": 0
    },
    {
      "title": "Convert DependencyProperty to DirectProperty",
      "description": "Converts WPF DependencyProperty to Avalonia DirectProperty for simple properties",
      "index": 1
    }
  ],
  "preview": {
    "filePath": "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs",
    "originalCode": "        public static readonly DependencyProperty NameProperty =\n            DependencyProperty.Register(nameof(Name), typeof(string), typeof(MainWindow),\n                new PropertyMetadata(default(string)));",
    "fixedCode": "        public static readonly StyledProperty<string> NameProperty =\n            AvaloniaProperty.Register<MainWindow, string>(nameof(Name),\n                defaultValue: default(string));",
    "diff": "@@ -45,3 +45,3 @@\n-        public static readonly DependencyProperty NameProperty =\n-            DependencyProperty.Register(nameof(Name), typeof(string), typeof(MainWindow),\n-                new PropertyMetadata(default(string)));\n+        public static readonly StyledProperty<string> NameProperty =\n+            AvaloniaProperty.Register<MainWindow, string>(nameof(Name),\n+                defaultValue: default(string));"
  }
}
```

### Agent Response

```
I found the issue and here's what would change:

---

## Current Code (WPF)

**File:** MainWindow.xaml.cs
**Lines:** 45-47

```csharp
public static readonly DependencyProperty NameProperty =
    DependencyProperty.Register(nameof(Name), typeof(string), typeof(MainWindow),
        new PropertyMetadata(default(string)));
```

---

## Available Fixes

### Option 1: Convert to StyledProperty (Recommended)
Use this for properties that can be styled (most common case).

**After:**
```csharp
public static readonly StyledProperty<string> NameProperty =
    AvaloniaProperty.Register<MainWindow, string>(nameof(Name),
        defaultValue: default(string));
```

### Option 2: Convert to DirectProperty
Use this for simple properties without styling, or performance-critical properties.

**After:**
```csharp
private string _name = default(string);
public static readonly DirectProperty<MainWindow, string> NameProperty =
    AvaloniaProperty.RegisterDirect<MainWindow, string>(
        nameof(Name),
        o => o._name,
        (o, v) => o._name = v);
```

---

## Unified Diff (Option 1)

```diff
@@ -45,3 +45,3 @@
-        public static readonly DependencyProperty NameProperty =
-            DependencyProperty.Register(nameof(Name), typeof(string), typeof(MainWindow),
-                new PropertyMetadata(default(string)));
+        public static readonly StyledProperty<string> NameProperty =
+            AvaloniaProperty.Register<MainWindow, string>(nameof(Name),
+                defaultValue: default(string));
```

---

## What Changes

1. **Type changes:** `DependencyProperty` → `StyledProperty<string>`
2. **Registration method:** `DependencyProperty.Register(...)` → `AvaloniaProperty.Register<TOwner, TValue>(...)`
3. **Type safety:** Generic type parameters eliminate the need for `typeof()` and casts
4. **Metadata:** `PropertyMetadata` → named parameter `defaultValue:`

---

## Impact Analysis

- **Breaking changes:** None (property accessors remain the same)
- **Behavioral changes:** None (same functionality)
- **Additional changes needed:**
  - Add `using Avalonia;` if not present
  - Update property accessor (getter/setter) if it casts the value

---

**Which option would you like me to apply?**
1. Option 1: Convert to StyledProperty (recommended)
2. Option 2: Convert to DirectProperty
3. Cancel - Don't apply any changes
```

### User Response

```
Apply option 1
```

### Agent Actions

```json
Tool: apply_fix
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj",
  "filePath": "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs",
  "diagnosticId": "WPFAV001",
  "line": 45,
  "column": 9,
  "fixIndex": 0
}
```

**Response:**
```json
{
  "success": true,
  "appliedFixTitle": "Convert DependencyProperty to StyledProperty",
  "modifiedFiles": [
    "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs"
  ],
  "availableFixes": [...],
  "diffs": [
    {
      "filePath": "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs",
      "changeDescription": "Converted DependencyProperty to StyledProperty",
      "linesAdded": 3,
      "linesRemoved": 3,
      "diff": "..."
    }
  ]
}
```

### Agent Response

```
✓ Fix applied successfully!

**Summary:**
- Applied: "Convert DependencyProperty to StyledProperty"
- Modified: MainWindow.xaml.cs
- Lines changed: 3 removed, 3 added

**The property has been successfully migrated to Avalonia's StyledProperty system.**

**Next steps:**
1. Review the change: `git diff MainWindow.xaml.cs`
2. Ensure the file builds: `dotnet build`
3. Check for other issues: Would you like me to analyze the file again?

Note: You may need to add `using Avalonia;` at the top of the file if you see compilation errors.
```

---

## Workflow 7: Incremental Team Migration

**Scenario:** Your team is migrating a large project incrementally, with multiple developers working on different parts.

### Agent Prompt

```
Our team is migrating a large project at C:\Projects\LargeApp\LargeApp.sln.
Help us divide the work:
1. Analyze the entire solution
2. Suggest how to divide work by folders or components
3. Identify dependencies between components
4. Estimate effort for each part
5. Create a team task list
```

### Agent Actions

```json
Tool: validate_project
{
  "projectPath": "C:\\Projects\\LargeApp\\LargeApp.sln"
}
```

```json
Tool: analyze_project
{
  "projectPath": "C:\\Projects\\LargeApp\\LargeApp.sln"
}
```

### Agent Response

```
# Team Migration Plan for LargeApp Solution

## Solution Structure

**Projects:** 5
1. LargeApp.Core (Class Library) - 45 files
2. LargeApp.UI (WPF Application) - 85 files
3. LargeApp.Controls (Custom Controls) - 32 files
4. LargeApp.Services (Services) - 28 files
5. LargeApp.Tests (Unit Tests) - 67 files

**Total Issues: 247 diagnostics**

---

## Work Division Strategy

### Option A: By Project (Recommended for your structure)

#### Team Assignment: 5 developers

**Developer 1: LargeApp.Core** (Low complexity, foundational)
- Issues: 12 (all WPFAV001)
- Effort: 2-3 hours
- Priority: HIGH (other projects depend on this)
- No UI dependencies, pure business logic

**Developer 2: LargeApp.Services** (Medium complexity)
- Issues: 18 (mixed types)
- Effort: 4-5 hours
- Priority: HIGH (UI depends on this)
- Some event handling, mostly property changes

**Developer 3: LargeApp.Controls** (High complexity)
- Issues: 95 (custom control patterns)
- Effort: 2-3 days
- Priority: MEDIUM (complex but isolated)
- Custom controls, templates, styles

**Developer 4 & 5: LargeApp.UI** (High volume)
- Issues: 122 (all types)
- Effort: 3-4 days (split between two devs)
- Priority: MEDIUM (depends on Core, Services, Controls)
- Split by folders:
  - Dev 4: Views/ and ViewModels/ (65 issues)
  - Dev 5: Dialogs/ and Windows/ (57 issues)

**Everyone: Update Tests**
- After individual work is merged
- Update test mocks for Avalonia
- Add integration tests

---

## Dependency Order

```
1. LargeApp.Core (no dependencies)
   ↓
2. LargeApp.Services (depends on Core)
   ↓
3. LargeApp.Controls (depends on Core)
   ↓
4. LargeApp.UI (depends on all above)
   ↓
5. LargeApp.Tests (depends on all above)
```

**Migration Schedule:**
- **Week 1:** Core + Services (parallel)
- **Week 2:** Controls + UI (Dev 4)
- **Week 3:** UI (Dev 5) + Integration
- **Week 4:** Tests + Bug Fixes

---

## Detailed Task Breakdown

### Developer 1: LargeApp.Core

**Branch:** `feature/avalonia-core`

**Tasks:**
1. Analyze Core project: `analyze_project LargeApp.Core.csproj`
2. Batch convert all issues (12 WPFAV001): `batch_convert --diagnostic WPFAV001`
3. Build and verify: `dotnet build`
4. Run existing tests: `dotnet test`
5. Create PR: "Migrate LargeApp.Core to Avalonia"

**Files to modify:** (estimated)
- BaseViewModel.cs (3 properties)
- ObservableObject.cs (4 properties)
- ValidationBase.cs (5 properties)

**Blockers:** None
**Duration:** 2-3 hours
**Priority:** Start immediately

---

### Developer 2: LargeApp.Services

**Branch:** `feature/avalonia-services`
**Depends on:** Core (merged)

**Tasks:**
1. Wait for Core to be merged
2. Pull latest main, create branch
3. Analyze Services project
4. Migrate event handlers (8 WPFAV002) - review each one
5. Migrate properties (10 WPFAV001) - batch convert
6. Build, test, PR

**Files to modify:**
- UserService.cs
- DataService.cs
- NotificationService.cs
- EventAggregator.cs

**Blockers:** Depends on Core PR being merged
**Duration:** 4-5 hours
**Priority:** Start after Core is done

---

### Developer 3: LargeApp.Controls

**Branch:** `feature/avalonia-controls`
**Depends on:** Core (merged)

**Tasks:**
1. Wait for Core merge
2. Analyze Controls project (expect custom patterns)
3. Create migration guide for custom controls
4. Migrate one control as example
5. Get team review on approach
6. Migrate remaining controls
7. Test each control in isolation
8. Create control migration documentation

**Files to modify:**
- CustomButton.cs (15 issues)
- DataGridExtended.cs (22 issues)
- TreeViewCustom.cs (18 issues)
- ... and 10 more custom controls

**Blockers:**
- Depends on Core
- May need to define team standards for control patterns

**Duration:** 2-3 days
**Priority:** Can start in parallel with Services

**Special Notes:**
- This is the most complex area
- May discover patterns that need team discussion
- Some controls may need redesign

---

### Developer 4: LargeApp.UI (Views + ViewModels)

**Branch:** `feature/avalonia-ui-views`
**Depends on:** Core, Services, Controls (all merged)

**Tasks:**
1. Wait for Core, Services, Controls to be merged
2. Analyze Views/ and ViewModels/ folders
3. Start with ViewModels (less UI-specific)
4. Then migrate Views (XAML and code-behind)
5. Test each view as you go
6. Create PR in chunks (every 5-10 files)

**Files to modify:** ~40 files
- ViewModels/MainViewModel.cs
- ViewModels/SettingsViewModel.cs
- Views/MainView.xaml + .cs
- Views/SettingsView.xaml + .cs
- ... (and 35 more)

**Blockers:** Depends on Core, Services, Controls
**Duration:** 1.5-2 days
**Priority:** Week 2-3

---

### Developer 5: LargeApp.UI (Dialogs + Windows)

**Branch:** `feature/avalonia-ui-dialogs`
**Depends on:** Core, Services, Controls (all merged)

**Tasks:**
1. Wait for all dependencies
2. Analyze Dialogs/ and Windows/ folders
3. Start with simple dialogs
4. Migrate main windows
5. Test dialog interactions
6. Coordinate with Dev 4 on shared patterns

**Files to modify:** ~35 files
- Dialogs/ConfirmDialog.xaml + .cs
- Windows/MainWindow.xaml + .cs
- Windows/SettingsWindow.xaml + .cs
- ... (and 30 more)

**Blockers:** Depends on Core, Services, Controls
**Duration:** 1.5-2 days
**Priority:** Week 2-3

---

## Integration & Testing (All Team)

**Week 4:**

1. Merge all feature branches
2. Resolve any conflicts
3. Full solution build
4. Run all tests
5. Manual testing:
   - Each developer tests their components
   - QA tests complete application
6. Fix bugs found during integration
7. Performance testing
8. Documentation updates

---

## Communication & Coordination

### Daily Standups (15 min)
- What I migrated yesterday
- What I'm migrating today
- Any blockers or patterns discovered

### Pattern Discussions (as needed)
- If you discover a pattern that doesn't fit the tools
- Share in team chat with example
- Schedule quick call if needed

### Code Reviews
- Each PR reviewed by at least one other team member
- Focus on correct Avalonia patterns
- Share learnings in PR comments

---

## Tools & Prompts for Team

### For Each Developer

**Daily start:**
```
I'm working on migrating [PROJECT/FOLDER]. Show me:
1. How many issues remain in my area
2. What I should work on today
3. Any blockers or dependencies
```

**Before committing:**
```
I've migrated [FILES]. Verify:
1. All issues in these files are resolved
2. No new issues introduced
3. Generate a commit message
```

**Code review:**
```
Review these migration changes in [FILES]:
1. Are the Avalonia patterns correct?
2. Any potential issues?
3. Suggestions for improvement?
```

---

## Risk Management

### High Risk Areas
- **LargeApp.Controls:** Custom controls may not have direct equivalents
  - **Mitigation:** Migrate one control first, get team review

- **Integration:** Conflicts when merging 5 branches
  - **Mitigation:** Frequent communication, small PRs

### Medium Risk Areas
- **Event handling in Services:** Behavior may change
  - **Mitigation:** Extra testing, review each event pattern

### Low Risk Areas
- **Core library:** Mostly property changes, well-automated

---

## Success Metrics

- [ ] All 247 diagnostics resolved
- [ ] Solution builds without errors
- [ ] All existing tests pass (or updated)
- [ ] Manual smoke test passes
- [ ] Performance is acceptable
- [ ] All PRs reviewed and merged
- [ ] Documentation updated

---

## Next Steps

1. **Team Meeting:** Review this plan, assign developers
2. **Setup:** Each developer creates their branch
3. **Start:** Developer 1 begins with Core
4. **Daily Updates:** Keep team informed of progress
5. **Integration:** Week 4 - bring it all together

**Questions?**
- Need help with a specific component?
- Want to see examples for your assigned area?
- Need clarification on any tasks?
```

---

## Workflow 8: Troubleshooting Failed Fixes

**Scenario:** A code fix failed to apply and you need to understand why and how to fix it manually.

### Agent Prompt

```
I tried to apply a fix for WPFAV001 at line 45 in MainWindow.xaml.cs but it failed.
Can you help me understand what went wrong and how to fix it manually?
```

### Agent Actions

**Step 1: Get diagnostic details**

```json
Tool: get_diagnostic_info
{
  "diagnosticId": "WPFAV001"
}
```

**Step 2: Try to preview the fix**

```json
Tool: preview_fixes
{
  "projectPath": "C:\\Projects\\MyWpfApp\\MyWpfApp.csproj",
  "filePath": "C:\\Projects\\MyWpfApp\\MainWindow.xaml.cs",
  "diagnosticId": "WPFAV001",
  "line": 45,
  "column": 9
}
```

**Response:**
```json
{
  "success": false,
  "error": "No code fixes available for this diagnostic at the specified location",
  "diagnostic": {
    "id": "WPFAV001",
    "message": "DependencyProperty should be converted to StyledProperty",
    "location": "MainWindow.xaml.cs(45,9)"
  }
}
```

### Agent Response

```
I see the fix failed. Let me help you troubleshoot this.

## Issue Analysis

**Error:** No code fixes available for this diagnostic at the specified location

**Common causes:**
1. The code has already been modified (fix was already applied)
2. The code doesn't match the expected pattern exactly
3. There are compilation errors preventing the fix
4. The code uses a custom pattern not recognized by the analyzer

---

Let me check the actual code at that location...

[Agent would need to examine the file or ask for the code snippet]

Please share the code around line 45 in MainWindow.xaml.cs (about 10 lines before and after),
and I'll help you figure out why the fix isn't working and how to apply it manually.
```

### User Response

```csharp
// Lines 40-55 of MainWindow.xaml.cs

public partial class MainWindow : Window
{
    // This uses a helper method instead of direct registration
    public static readonly DependencyProperty NameProperty =
        DependencyPropertyHelper.Register<MainWindow, string>(
            nameof(Name),
            default(string),
            OnNameChanged);

    public string Name
    {
        get => (string)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    private static void OnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Custom logic here
    }
}
```

### Agent Response (After Seeing Code)

```
Ah, I see the issue now! Your code uses a custom `DependencyPropertyHelper.Register` method
instead of the standard `DependencyProperty.Register`. The automated fix doesn't recognize
this custom pattern.

## Why the Fix Failed

The code fix provider looks for the standard WPF pattern:
```csharp
DependencyProperty.Register(...)
```

But your code uses:
```csharp
DependencyPropertyHelper.Register<MainWindow, string>(...)
```

This is a custom wrapper, which is actually pretty clever! However, it means we need to
migrate this manually.

---

## Manual Migration Steps

### Step 1: Update the DependencyPropertyHelper (or create AvaloniaPropertyHelper)

First, you'll need to create an Avalonia equivalent of your helper. Here's how:

**Before (WPF):**
```csharp
public static class DependencyPropertyHelper
{
    public static DependencyProperty Register<TOwner, TValue>(
        string name,
        TValue defaultValue,
        PropertyChangedCallback callback)
    {
        return DependencyProperty.Register(
            name,
            typeof(TValue),
            typeof(TOwner),
            new PropertyMetadata(defaultValue, callback));
    }
}
```

**After (Avalonia):**
```csharp
public static class AvaloniaPropertyHelper
{
    public static StyledProperty<TValue> RegisterStyled<TOwner, TValue>(
        string name,
        TValue defaultValue = default,
        Action<TOwner, AvaloniaPropertyChangedEventArgs<TValue>>? callback = null)
        where TOwner : AvaloniaObject
    {
        var property = AvaloniaProperty.Register<TOwner, TValue>(
            name,
            defaultValue);

        if (callback != null)
        {
            // Register callback separately
            property.Changed.Subscribe(e =>
            {
                if (e.Sender is TOwner owner)
                {
                    callback(owner, e);
                }
            });
        }

        return property;
    }
}
```

### Step 2: Update MainWindow.xaml.cs

**Current (WPF):**
```csharp
public static readonly DependencyProperty NameProperty =
    DependencyPropertyHelper.Register<MainWindow, string>(
        nameof(Name),
        default(string),
        OnNameChanged);

public string Name
{
    get => (string)GetValue(NameProperty);
    set => SetValue(NameProperty, value);
}

private static void OnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    if (d is MainWindow window)
    {
        // Custom logic here
    }
}
```

**Migrated (Avalonia):**
```csharp
public static readonly StyledProperty<string> NameProperty =
    AvaloniaPropertyHelper.RegisterStyled<MainWindow, string>(
        nameof(Name),
        default(string),
        OnNameChanged);

public string Name
{
    get => GetValue(NameProperty);
    set => SetValue(NameProperty, value);
}

private static void OnNameChanged(MainWindow window, AvaloniaPropertyChangedEventArgs<string> e)
{
    // Custom logic here
    // Note: e.OldValue and e.NewValue are now typed as string
}
```

### Step 3: Alternative - Use Standard Avalonia Pattern

If you don't want to maintain a helper, use the standard pattern with a callback:

```csharp
public static readonly StyledProperty<string> NameProperty =
    AvaloniaProperty.Register<MainWindow, string>(
        nameof(Name),
        defaultValue: default(string));

static MainWindow()
{
    NameProperty.Changed.AddClassHandler<MainWindow>((window, e) => window.OnNameChanged(e));
}

public string Name
{
    get => GetValue(NameProperty);
    set => SetValue(NameProperty, value);
}

private void OnNameChanged(AvaloniaPropertyChangedEventArgs<string> e)
{
    // Custom logic here
    // e.OldValue and e.NewValue are strongly typed
}
```

---

## Recommended Approach

**I recommend Option 2 (Standard Pattern)** because:
1. ✓ No custom helper to maintain
2. ✓ Standard Avalonia pattern - easier for team members
3. ✓ Better type safety (generic callbacks)
4. ✓ IDE support and documentation

---

## Action Plan

1. **Search for all uses of DependencyPropertyHelper:**
   ```
   Would you like me to search your project for all uses of this helper?
   ```

2. **Migrate all instances:**
   - I can provide the correct Avalonia pattern for each one
   - We can do this file by file
   - I'll help verify each migration

3. **Remove the helper:**
   - Once all uses are migrated, you can remove DependencyPropertyHelper

---

Would you like me to:
1. Search your project for all uses of `DependencyPropertyHelper`
2. Help you migrate each instance one by one
3. Provide a conversion script/tool
4. Something else?
```

---

## Summary

These workflows demonstrate how the WpfToAvalonia MCP Server integrates with AI agents to provide:

1. **Comprehensive Analysis** - Understanding project structure and migration scope
2. **Careful Migration** - Step-by-step with previews and approvals
3. **Batch Operations** - Efficient conversion of multiple issues at once
4. **Learning Support** - Educational guidance while migrating
5. **Planning & Validation** - Strategic approach to complex projects
6. **Preview & Review** - Safety through verification before changes
7. **Team Coordination** - Managing large migrations across multiple developers
8. **Troubleshooting** - Handling edge cases and custom patterns

Each workflow showcases different aspects of the MCP tools and how they work together to provide a comprehensive migration solution.

## Additional Resources

- [MCP Server Guide](./MCP_SERVER_GUIDE.md) - Complete tool reference
- [Example Prompts](./EXAMPLE_PROMPTS.md) - 100+ additional prompts
- [Claude Desktop Integration](./CLAUDE_DESKTOP_INTEGRATION.md) - Platform-specific setup
- [GitHub Copilot Integration](./GITHUB_COPILOT_INTEGRATION.md) - Copilot-specific usage
