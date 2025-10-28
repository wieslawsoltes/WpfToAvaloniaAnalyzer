# WpfToAvaloniaAnalyzers

| Package | NuGet |
| --- | --- |
| `WpfToAvaloniaAnalyzers` (bundle) | [![NuGet](https://img.shields.io/nuget/v/WpfToAvaloniaAnalyzers.svg)](https://www.nuget.org/packages/WpfToAvaloniaAnalyzers/) [![NuGet Preview](https://img.shields.io/nuget/vpre/WpfToAvaloniaAnalyzers.svg)](https://www.nuget.org/packages/WpfToAvaloniaAnalyzers/absoluteLatest) [![NuGet Downloads](https://img.shields.io/nuget/dt/WpfToAvaloniaAnalyzers.svg)](https://www.nuget.org/packages/WpfToAvaloniaAnalyzers/) |
| `WpfToAvaloniaAnalyzers.Analyzers` | [![NuGet](https://img.shields.io/nuget/v/WpfToAvaloniaAnalyzers.Analyzers.svg)](https://www.nuget.org/packages/WpfToAvaloniaAnalyzers.Analyzers/) [![NuGet Preview](https://img.shields.io/nuget/vpre/WpfToAvaloniaAnalyzers.Analyzers.svg)](https://www.nuget.org/packages/WpfToAvaloniaAnalyzers.Analyzers/absoluteLatest) |
| `WpfToAvaloniaAnalyzers.CodeFixes` | [![NuGet](https://img.shields.io/nuget/v/WpfToAvaloniaAnalyzers.CodeFixes.svg)](https://www.nuget.org/packages/WpfToAvaloniaAnalyzers.CodeFixes/) [![NuGet Preview](https://img.shields.io/nuget/vpre/WpfToAvaloniaAnalyzers.CodeFixes.svg)](https://www.nuget.org/packages/WpfToAvaloniaAnalyzers.CodeFixes/absoluteLatest) |
| `WpfToAvaloniaAnalyzers.Cli` (tool) | [![NuGet](https://img.shields.io/nuget/v/WpfToAvaloniaAnalyzers.Cli.svg)](https://www.nuget.org/packages/WpfToAvaloniaAnalyzers.Cli/) [![NuGet Preview](https://img.shields.io/nuget/vpre/WpfToAvaloniaAnalyzers.Cli.svg)](https://www.nuget.org/packages/WpfToAvaloniaAnalyzers.Cli/absoluteLatest) |

Roslyn analyzers and code fixes that accelerate migrating existing WPF code to Avalonia UI.

## Overview

The analyzers flag WPF‑specific APIs, `DependencyProperty` patterns, and other migration pain points. Each diagnostic ships with a fixer that rewrites the code to idiomatic Avalonia equivalents—including property registrations, class handlers, and file‑wide conversions.

## Highlights

- **Incremental & batch fixes** – Apply individual fixes or run the `WpfToAvaloniaFile` fixer to transform an entire file in one pass.
- **DependencyProperty conversion** – Automatically converts `DependencyProperty.Register` calls to `AvaloniaProperty.Register`, rewrites accessors, and adds Avalonia class handlers.
- **Callback upgrades** – Updates `DependencyObject/DependencyPropertyChangedEventArgs` callbacks to Avalonia signatures, creating static constructors and handler wiring when required.
- **Routed event migration** – Maps WPF `RoutedEvent` registrations, CLR accessors, class handlers, and instance handlers to Avalonia `RoutedEvent<TEventArgs>` APIs with strongly typed delegates.
- **Guided migration** – Diagnostics explain the WPF concept being replaced so you can review each change with context.

## Getting Started

Install the analyzer package into the project you want to migrate:

```bash
dotnet add package WpfToAvaloniaAnalyzers
```

Build the project (or run the IDE code analysis) to see diagnostics. Use your IDE’s light bulb/code fix menu to apply changes, or let the Fix All provider convert files and projects in bulk.

Prefer a more granular setup? Install `WpfToAvaloniaAnalyzers.Analyzers` for analyzer-only usage or `WpfToAvaloniaAnalyzers.CodeFixes` when you want to reference code fixes directly.

## Usage

1. Build your solution (`dotnet build`) or run the analyzer driver to surface the `WA###` diagnostics in the target project.
2. Apply fixes inline from the IDE, or install the CLI tool for larger migrations:

   ```bash
   dotnet tool install -g WpfToAvaloniaAnalyzers.Cli
   wpf-to-avalonia --path <path-to-sln> --scope solution
   ```

3. Review converted files, resolve any follow-up notes emitted by the analyzer, and rebuild to ensure the project compiles against Avalonia.

## Diagnostics & Fixers

| Id | Title | Analyzer summary | Code fix summary |
| --- | --- | --- | --- |
| `WA001` | Convert WPF DependencyProperty to Avalonia StyledProperty | Finds WPF `DependencyProperty.Register` fields. | Converts to `StyledProperty` registration and updates accessors. |
| `WA002` | Remove WPF using directives | Flags lingering `System.Windows*` usings. | Drops WPF namespaces and adds `Avalonia.*` usings. |
| `WA003` | Convert WPF Control base class to Avalonia Control | Detects classes inheriting from WPF base types. | Switches base type to Avalonia equivalent. |
| `WA004` | Remove casts from GetValue calls | Finds casts around `GetValue`/`SetValue`. | Removes redundant casts and normalises accessors. |
| `WA005` | Convert WPF PropertyMetadata to Avalonia property options | Identifies dependency properties using `PropertyMetadata`. | Converts registration to Avalonia overloads and threads callbacks. |
| `WA006` | Convert WPF property changed callback signature to Avalonia | Reports WPF callback signatures. | Rewrites method parameters to Avalonia pattern. |
| `WA007` | Apply all WPF to Avalonia conversions | Emits aggregator diagnostic per document. | Runs batch fixer to apply all available fixes. |
| `WA008` | Convert PropertyMetadata callback to Avalonia class handler | Finds metadata callbacks needing class handlers. | Adds static constructor with `AddClassHandler` and updates callback. |
| `WA010` | Remove MS.Internal telemetry instrumentation | Highlights telemetry hooks/usings. | Removes telemetry code and cleans up imports/constructors. |
| `WA011` | Remove CommonDependencyProperty attribute | Flags `[CommonDependencyProperty]` attributes. | Removes attribute and tidies imports. |
| `WA012` | Convert WPF attached DependencyProperty to Avalonia attached property | Targets attached property registrations. | Converts to `AvaloniaProperty.RegisterAttached` pattern. |
| `WA013` | Translate FrameworkPropertyMetadata to Avalonia metadata | Finds `FrameworkPropertyMetadata` usage. | Rewrites metadata to Avalonia options with callbacks. |
| `WA014` | Remove EffectiveValuesInitialSize overrides | Warns about the obsolete override. | Removes override/property declaration. |
| `WA015` | Convert WPF routed event field to Avalonia routed event | Detects routed event fields. | Creates `RoutedEvent<TEventArgs>` registrations. |
| `WA016` | Convert WPF routed event accessors to Avalonia pattern | Finds CLR routed event accessors. | Rewrites to Avalonia `AddHandler`/`RemoveHandler`. |
| `WA017` | Convert EventManager.RegisterClassHandler to Avalonia AddClassHandler | Flags class handler registrations. | Replaces with Avalonia `AddClassHandler` wiring. |
| `WA018` | Convert AddHandler/RemoveHandler usage to Avalonia overloads | Highlights WPF handler calls. | Converts to strongly typed Avalonia overloads. |
| `WA019` | Convert RaiseEvent usage to Avalonia | Warns about WPF `RaiseEvent`. | Rewrites to Avalonia raising helpers and args. |
| `WA020` | Convert routed event AddOwner usage | Detects routed event `AddOwner` calls. | Generates Avalonia `AddOwner` calls and follow-up stubs. |

See `src/WpfToAvaloniaAnalyzers/DiagnosticDescriptors.cs` for the complete catalog.

### Detailed Analyzer & Fix Coverage

#### WA001 – Convert WPF DependencyProperty to Avalonia StyledProperty
**When it triggers:** A dependency property is registered with `DependencyProperty.Register`.

**Fix transforms:** Converts the registration to `AvaloniaProperty.Register` and adjusts accessors to use the typed `StyledProperty` API.

```csharp
// Before
public static readonly DependencyProperty TitleProperty =
    DependencyProperty.Register("Title", typeof(string), typeof(SampleControl), new PropertyMetadata("Hello"));

public string Title
{
    get => (string)GetValue(TitleProperty);
    set => SetValue(TitleProperty, value);
}

// After
public static readonly StyledProperty<string> TitleProperty =
    AvaloniaProperty.Register<SampleControl, string>(nameof(Title), "Hello");

public string Title
{
    get => GetValue(TitleProperty);
    set => SetValue(TitleProperty, value);
}
```

#### WA002 – Remove WPF using directives
**When it triggers:** Files still import `System.Windows` namespaces after migration.

**Fix transforms:** Removes WPF usings and introduces the appropriate Avalonia namespaces.

```csharp
// Before
using System.Windows;
using System.Windows.Controls;

// After
using Avalonia;
using Avalonia.Controls;
```

#### WA003 – Convert WPF Control base class to Avalonia Control
**When it triggers:** A type derives from a WPF base class such as `Window` or `UserControl`.

**Fix transforms:** Updates the base type (and corresponding usings) to the Avalonia equivalent.

```csharp
// Before
public class SampleWindow : System.Windows.Window { }

// After
public class SampleWindow : Avalonia.Controls.Window { }
```

#### WA004 – Remove casts from GetValue calls
**When it triggers:** Accessors cast the result of `GetValue`.

**Fix transforms:** Drops the redundant cast because Avalonia styled properties are strongly typed.

```csharp
// Before
public string Title => (string)GetValue(TitleProperty);

// After
public string Title => GetValue(TitleProperty);
```

#### WA005 – Convert WPF PropertyMetadata to Avalonia property options
**When it triggers:** Dependency properties supply `PropertyMetadata` with callbacks or defaults.

**Fix transforms:** Rewrites the registration to Avalonia overloads and threads callbacks through the typed property system.

```csharp
// Before
public static readonly DependencyProperty CountProperty =
    DependencyProperty.Register(
        nameof(Count), typeof(int), typeof(SampleControl), new PropertyMetadata(0, OnCountChanged));

private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

// After
public static readonly StyledProperty<int> CountProperty =
    AvaloniaProperty.Register<SampleControl, int>(nameof(Count), 0);

private static void OnCountChanged(AvaloniaObject obj, AvaloniaPropertyChangedEventArgs<int> e) { }

static SampleControl()
{
    CountProperty.Changed.AddClassHandler<SampleControl>((control, args) => control.OnCountChanged(args));
}
```

#### WA006 – Convert WPF property changed callback signature to Avalonia
**When it triggers:** Property changed callbacks use the WPF `(DependencyObject, DependencyPropertyChangedEventArgs)` signature.

**Fix transforms:** Updates the method signature to Avalonia conventions and adjusts call sites.

```csharp
// Before
private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

// After
private static void OnCountChanged(AvaloniaObject obj, AvaloniaPropertyChangedEventArgs<int> e) { }
```

#### WA007 – Apply all WPF to Avalonia conversions
**When it triggers:** The document contains any analyzers from this suite.

**Fix transforms:** Running the Fix All action on the diagnostic applies every available analyzer fix in the file.

#### WA008 – Convert PropertyMetadata callback to Avalonia class handler
**When it triggers:** Metadata callbacks should be converted to Avalonia class handlers.

**Fix transforms:** Adds a static constructor, registers `Property.Changed.AddClassHandler`, and updates the callback signature.

```csharp
// Before
public static readonly DependencyProperty IsEnabledProperty =
    DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(SampleControl), new PropertyMetadata(false, OnIsEnabledChanged));

private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

// After
public static readonly StyledProperty<bool> IsEnabledProperty =
    AvaloniaProperty.Register<SampleControl, bool>(nameof(IsEnabled));

static SampleControl()
{
    IsEnabledProperty.Changed.AddClassHandler<SampleControl>((control, args) => control.OnIsEnabledChanged(args));
}

private void OnIsEnabledChanged(AvaloniaPropertyChangedEventArgs<bool> e) { }
```

#### WA010 – Remove MS.Internal telemetry instrumentation
**When it triggers:** MS.Internal telemetry helpers or related usings remain in code.

**Fix transforms:** Removes telemetry calls (and empty constructors if necessary) and cleans up unused imports.

```csharp
// Before
using MS.Internal.Telemetry;

public SampleControl()
{
    TelemetryFactory.Get<SampleControl>().Track();
}

// After
public SampleControl()
{
}
```

#### WA011 – Remove CommonDependencyProperty attribute
**When it triggers:** `[CommonDependencyProperty]` attributes annotate fields.

**Fix transforms:** Drops the attribute and prunes unused usings.

```csharp
// Before
[CommonDependencyProperty]
public static readonly DependencyProperty FooProperty = ...;

// After
public static readonly StyledProperty<int> FooProperty = ...;
```

#### WA012 – Convert WPF attached DependencyProperty to Avalonia attached property
**When it triggers:** Attached properties use WPF registrations and helper accessors.

**Fix transforms:** Switches to `AvaloniaProperty.RegisterAttached` and updates getter/setter helpers.

```csharp
// Before
public static readonly DependencyProperty DockProperty =
    DependencyProperty.RegisterAttached("Dock", typeof(Dock), typeof(DockPanel), new PropertyMetadata(Dock.Left));

public static Dock GetDock(DependencyObject obj) => (Dock)obj.GetValue(DockProperty);
public static void SetDock(DependencyObject obj, Dock value) => obj.SetValue(DockProperty, value);

// After
public static readonly AttachedProperty<Dock> DockProperty =
    AvaloniaProperty.RegisterAttached<Control, DockPanel, Dock>("Dock", Dock.Left);

public static Dock GetDock(Control control) => control.GetValue(DockProperty);
public static void SetDock(Control control, Dock value) => control.SetValue(DockProperty, value);
```

#### WA013 – Translate FrameworkPropertyMetadata to Avalonia metadata
**When it triggers:** Dependency properties use `FrameworkPropertyMetadata` for defaults or layout flags.

**Fix transforms:** Rewrites the registration to Avalonia options and preserves callbacks.

```csharp
// Before
public static readonly DependencyProperty SizeProperty =
    DependencyProperty.Register(
        nameof(Size), typeof(Size), typeof(SampleControl), new FrameworkPropertyMetadata(Size.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure));

// After
public static readonly StyledProperty<Size> SizeProperty =
    AvaloniaProperty.Register<SampleControl, Size>(nameof(Size));

static SampleControl()
{
    SizeProperty.Changed.AddClassHandler<SampleControl>((control, _) => control.InvalidateMeasure());
}
```

#### WA014 – Remove EffectiveValuesInitialSize overrides
**When it triggers:** A class overrides `EffectiveValuesInitialSize`.

**Fix transforms:** Removes the override because Avalonia does not expose the optimisation knob.

```csharp
// Before
protected override int EffectiveValuesInitialSize => 64;

// After
// Override removed – no replacement required in Avalonia.
```

#### WA015 – Convert WPF routed event field to Avalonia routed event
**When it triggers:** Routed events are registered via `EventManager.RegisterRoutedEvent`.

**Fix transforms:** Converts the field to an Avalonia `RoutedEvent<TEventArgs>`.

```csharp
// Before
public static readonly RoutedEvent ClickEvent =
    EventManager.RegisterRoutedEvent("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SampleControl));

// After
public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
    RoutedEvent.Register<SampleControl, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);
```

#### WA016 – Convert WPF routed event accessors to Avalonia pattern
**When it triggers:** CLR event accessors use WPF `AddHandler`/`RemoveHandler` signatures.

**Fix transforms:** Rewrites the accessors to use Avalonia helpers.

```csharp
// Before
public event RoutedEventHandler Click
{
    add => AddHandler(ClickEvent, value);
    remove => RemoveHandler(ClickEvent, value);
}

// After
public event EventHandler<RoutedEventArgs> Click
{
    add => AddHandler(ClickEvent, value);
    remove => RemoveHandler(ClickEvent, value);
}
```

#### WA017 – Convert EventManager.RegisterClassHandler to Avalonia AddClassHandler
**When it triggers:** Static constructors register class handlers through WPF APIs.

**Fix transforms:** Replaces `EventManager.RegisterClassHandler` calls with Avalonia `AddClassHandler` wiring.

```csharp
// Before
static SampleControl()
{
    EventManager.RegisterClassHandler(typeof(SampleControl), ClickEvent, new RoutedEventHandler(OnClick));
}

// After
static SampleControl()
{
    ClickEvent.AddClassHandler<SampleControl>((control, args) => control.OnClick(args));
}
```

#### WA018 – Convert AddHandler/RemoveHandler usage to Avalonia overloads
**When it triggers:** Code calls the WPF versions of `AddHandler` or `RemoveHandler`.

**Fix transforms:** Updates call sites to strongly typed Avalonia overloads and routing options.

```csharp
// Before
button.AddHandler(ClickEvent, new RoutedEventHandler(OnButtonClick));

// After
button.AddHandler(ClickEvent, OnButtonClick);
```

#### WA019 – Convert RaiseEvent usage to Avalonia
**When it triggers:** `RaiseEvent` is invoked with WPF `RoutedEventArgs`.

**Fix transforms:** Converts to Avalonia raising helpers.

```csharp
// Before
RaiseEvent(new RoutedEventArgs(ClickEvent));

// After
RaiseEvent(new RoutedEventArgs(ClickEvent)); // RoutedEventArgs now from Avalonia.Interactivity
```

#### WA020 – Convert routed event AddOwner usage
**When it triggers:** Routed events call `AddOwner` to attach additional types.

**Fix transforms:** Generates Avalonia `AddOwner` calls and stubs for manual follow-up if metadata is required.

```csharp
// Before
public static readonly RoutedEvent ClickEvent = Button.ClickEvent.AddOwner(typeof(SampleControl));

// After
public static readonly RoutedEvent<RoutedEventArgs> ClickEvent = Button.ClickEvent.AddOwner<SampleControl>();
```

| Id | Analyzer focus | Code fix behaviour |
| --- | --- | --- |
| `WA001` | Flags WPF `DependencyProperty` fields registered via `DependencyProperty.Register`. | Converts the field to an Avalonia `StyledProperty`, updates registrations, and adjusts accessors. |
| `WA002` | Detects `System.Windows*` using directives left in migrated files. | Removes WPF namespaces and inserts required `Avalonia.*` usings. |
| `WA003` | Finds controls inheriting from WPF base types (`Window`, `UserControl`, etc.). | Rewrites the base class to the Avalonia equivalent and fixes related usings. |
| `WA004` | Spots casts around `GetValue`/`SetValue` accessor calls. | Drops redundant casts and normalises accessors to use typed `StyledProperty` APIs. |
| `WA005` | Identifies `PropertyMetadata` initialisers on dependency properties. | Upgrades the registration to Avalonia overloads and threads callbacks through the typed property pipeline. |
| `WA006` | Reports WPF-style property changed callback signatures. | Adjusts parameters to Avalonia conventions and updates invocations. |
| `WA007` | Emits a single diagnostic per document to drive Fix All. | Runs the batch fixer to apply every applicable analyzer/code fix in the file. |
| `WA008` | Detects metadata callbacks that should be class handlers. | Adds a static constructor with `Property.Changed.AddClassHandler` and rewrites the callback signature. |
| `WA010` | Highlights MS.Internal telemetry instrumentation and supporting using directives. | Removes telemetry code paths and cleans up orphaned constructors/usings. |
| `WA011` | Flags `[CommonDependencyProperty]` attributes. | Drops the attribute and tidies any unused imports. |
| `WA012` | Targets attached dependency property registrations. | Converts them to Avalonia attached `StyledProperty` patterns and updates accessor helpers. |
| `WA013` | Finds `FrameworkPropertyMetadata` usage requiring migration. | Rewrites metadata to Avalonia property options and wires callbacks through the new property definition. |
| `WA014` | Warns about `EffectiveValuesInitialSize` overrides. | Removes the override/property because Avalonia does not expose the WPF optimisation. |
| `WA015` | Detects routed event field registrations. | Generates an Avalonia `RoutedEvent<TEventArgs>` field and updates registration. |
| `WA016` | Finds CLR accessors for routed events. | Rewrites accessors to use Avalonia `AddHandler`/`RemoveHandler` helpers with strongly typed delegates. |
| `WA017` | Flags `EventManager.RegisterClassHandler` calls. | Replaces them with `AddClassHandler` wiring and ensures static constructor setup. |
| `WA018` | Highlights `AddHandler`/`RemoveHandler` invocations that still use WPF APIs. | Moves usage to Avalonia overloads and applies routing options. |
| `WA019` | Warns about `RaiseEvent` calls using WPF patterns. | Converts the call site to Avalonia raising helpers and event args. |
| `WA020` | Detects routed event `AddOwner` chaining. | Generates Avalonia `AddOwner` calls and stubs for any follow-up metadata. |

### Routed Event Limitations

The routed-event fixers cover the most common WPF patterns, but a few scenarios still need manual follow-up:

- **Custom event-args types** – When a delegate uses a custom `RoutedEventArgs` derivative we cannot map, the fixer falls back to `Avalonia.Interactivity.RoutedEventArgs`. Update the generated `RoutedEvent<TEventArgs>` call and accessor signatures to the appropriate Avalonia args type.
- **`AddOwner` metadata** – WPF `AddOwner` calls that attach additional metadata (e.g., class handlers, override metadata) are reported but not re-created automatically. After conversion, re-register the routed event with Avalonia APIs and reapply any metadata or class handlers manually.
- **Specialised delegates** – We translate common WPF delegates (`RoutedEventHandler`, `MouseButtonEventHandler`, etc.) to the closest Avalonia equivalent. Less common delegates may still target `EventHandler<RoutedEventArgs>` and should be tightened up manually if Avalonia exposes a better typed delegate.
- **Advanced routing logic** – Scenarios that depend on tunnelling handlers, custom `RaiseEvent` flows, or inspecting the WPF `RoutingStrategy` enum may require application-level adjustments after the automated conversion.

## Project Layout

- `src/WpfToAvaloniaAnalyzers` – Analyzer implementations and diagnostic descriptors.
- `src/WpfToAvaloniaAnalyzers.CodeFixes` – All fixers plus reusable services (e.g., `ClassHandlerService`, batch fixer pipeline).
- `src/WpfToAvaloniaAnalyzers.Cli` – Command-line batch fixer that executes the analyzers/code fixes.
- `src/WpfToAvaloniaAnalyzers.NuGet` – Packaged analyzer distributable.
- `tests/WpfToAvaloniaAnalyzers.Tests` – Analyzer/code-fix regression tests. Helpers support optional compiler diagnostics so Avalonia references can be validated.
- `tests/WpfToAvaloniaAnalyzers.Cli.Tests` – CLI integration coverage.
- `samples/WpfToAvaloniaAnalyzers.Sample.Wpf` – Simple WPF app for manual migration experiments.
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
dotnet pack src/WpfToAvaloniaAnalyzers.NuGet/WpfToAvaloniaAnalyzers.NuGet.csproj
```

You can also publish the constituent projects individually (the build produces packages because `GeneratePackageOnBuild` is enabled):

```bash
dotnet build src/WpfToAvaloniaAnalyzers/WpfToAvaloniaAnalyzers.csproj -c Release
dotnet build src/WpfToAvaloniaAnalyzers.CodeFixes/WpfToAvaloniaAnalyzers.CodeFixes.csproj -c Release
dotnet build src/WpfToAvaloniaAnalyzers.Cli/WpfToAvaloniaAnalyzers.Cli.csproj -c Release
```

## Contributing

Issues and pull requests are welcome! If you are extending the analyzers:

1. Add or update diagnostics in `DiagnosticDescriptors`.
2. Cover the behavior with analyzer and code-fix tests (enable compiler diagnostics when the fix introduces Avalonia types).
3. Update this README and the sample project if new migration scenarios are supported.

## License

MIT License – see [`LICENSE`](LICENSE) for details.
