using Microsoft.CodeAnalysis;

namespace WpfToAvaloniaAnalyzers;

public static class DiagnosticDescriptors
{
    private const string Category = "WpfToAvalonia";

    public static readonly DiagnosticDescriptor WA001_ConvertDependencyPropertyToAvaloniaProperty = new(
        id: "WA001",
        title: "Convert WPF DependencyProperty to Avalonia StyledProperty",
        messageFormat: "DependencyProperty '{0}' should be converted to Avalonia StyledProperty",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "WPF DependencyProperty declarations should be converted to Avalonia StyledProperty for compatibility with Avalonia UI framework.");

    public static readonly DiagnosticDescriptor WA002_RemoveWpfUsings = new(
        id: "WA002",
        title: "Remove WPF using directives",
        messageFormat: "WPF using directive '{0}' should be removed when migrating to Avalonia",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "WPF using directives (System.Windows, System.Windows.Controls) should be removed and replaced with Avalonia equivalents.");

    public static readonly DiagnosticDescriptor WA003_ConvertWpfBaseClass = new(
        id: "WA003",
        title: "Convert WPF Control base class to Avalonia Control",
        messageFormat: "WPF Control base class should be converted to Avalonia.Controls.Control",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "WPF Control base class should be replaced with Avalonia.Controls.Control for Avalonia compatibility.");

    public static readonly DiagnosticDescriptor WA004_RemoveCastsFromGetValue = new(
        id: "WA004",
        title: "Remove casts from GetValue calls",
        messageFormat: "Cast on GetValue can be removed in Avalonia typed properties",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Avalonia StyledProperty<T> is strongly typed, so casts on GetValue calls are not needed.");

    public static readonly DiagnosticDescriptor WA005_ConvertPropertyMetadata = new(
        id: "WA005",
        title: "Convert WPF PropertyMetadata to Avalonia property options",
        messageFormat: "PropertyMetadata with property changed callback should be converted to Avalonia AvaloniaProperty.Register overload",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "WPF PropertyMetadata with property changed callbacks should be converted to use Avalonia's property changed handling patterns.");

    public static readonly DiagnosticDescriptor WA006_ConvertPropertyChangedCallback = new(
        id: "WA006",
        title: "Convert WPF property changed callback signature to Avalonia",
        messageFormat: "Property changed callback '{0}' should be converted to Avalonia signature",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "WPF property changed callbacks use (DependencyObject, DependencyPropertyChangedEventArgs) signature, while Avalonia uses (AvaloniaObject, AvaloniaPropertyChangedEventArgs<T>) or a notify pattern with (AvaloniaObject, bool).");


    public static readonly DiagnosticDescriptor WA008_ConvertPropertyMetadataCallbackToClassHandler = new(
        id: "WA008",
        title: "Convert PropertyMetadata callback to Avalonia class handler",
        messageFormat: "Convert PropertyMetadata callback to Avalonia class handler pattern",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Adds a static constructor with Property.Changed.AddClassHandler and updates the callback signature to match Avalonia conventions.");

    public static readonly DiagnosticDescriptor WA010_RemoveTelemetryInstrumentation = new(
        id: "WA010",
        title: "Remove MS.Internal telemetry instrumentation",
        messageFormat: "Telemetry construct '{0}' should be removed when migrating to Avalonia",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "WPF-only MS.Internal telemetry hooks should be removed or replaced when porting to Avalonia.");

    public static readonly DiagnosticDescriptor WA011_RemoveCommonDependencyPropertyAttribute = new(
        id: "WA011",
        title: "Remove CommonDependencyProperty attribute",
        messageFormat: "Attribute '{0}' should be removed when migrating to Avalonia",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "CommonDependencyProperty attributes are WPF-specific markers and should be removed in Avalonia.");

    public static readonly DiagnosticDescriptor WA012_ConvertAttachedDependencyProperty = new(
        id: "WA012",
        title: "Convert WPF attached DependencyProperty to Avalonia attached property",
        messageFormat: "Attached DependencyProperty '{0}' should be converted to an Avalonia attached property",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Attached DependencyProperty declarations should be converted to AvaloniaProperty.RegisterAttached for Avalonia compatibility.");

    public static readonly DiagnosticDescriptor WA013_TranslateFrameworkPropertyMetadata = new(
        id: "WA013",
        title: "Translate FrameworkPropertyMetadata to Avalonia metadata",
        messageFormat: "FrameworkPropertyMetadata should be converted to Avalonia property metadata",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "FrameworkPropertyMetadata default values, callbacks, and layout flags should be translated to Avalonia property metadata patterns.");

    public static readonly DiagnosticDescriptor WA014_RemoveEffectiveValuesInitialSize = new(
        id: "WA014",
        title: "Remove EffectiveValuesInitialSize overrides",
        messageFormat: "Override '{0}' should be removed when migrating to Avalonia",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "EffectiveValuesInitialSize overrides are WPF-specific property system optimizations and should be removed in Avalonia.");

    public static readonly DiagnosticDescriptor WA007_ApplyAllAnalyzers = new(
        id: "WA007",
        title: "Apply all WPF to Avalonia conversions",
        messageFormat: "Apply all WPF to Avalonia conversions in this file",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Applies all available WPF to Avalonia code fixes for this file.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor WA015_ConvertRoutedEventField = new(
        id: "WA015",
        title: "Convert WPF routed event field to Avalonia routed event",
        messageFormat: "RoutedEvent '{0}' should be converted to Avalonia RoutedEvent<TEventArgs>",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "WPF routed event registrations should be migrated to Avalonia RoutedEvent<TEventArgs> using RoutedEvent.Register.");

    public static readonly DiagnosticDescriptor WA016_ConvertRoutedEventAccessors = new(
        id: "WA016",
        title: "Convert WPF routed event accessors to Avalonia pattern",
        messageFormat: "Event '{0}' should be updated to use Avalonia routed event accessor pattern",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "WPF routed event CLR accessors should be migrated to Avalonia AddHandler/RemoveHandler usage with strongly typed event handlers.");

    public static readonly DiagnosticDescriptor WA017_ConvertRegisterClassHandler = new(
        id: "WA017",
        title: "Convert EventManager.RegisterClassHandler to Avalonia AddClassHandler",
        messageFormat: "Class handler registration for '{0}' should be converted to Avalonia AddClassHandler",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EventManager.RegisterClassHandler calls should be converted to Avalonia's AddClassHandler APIs.");

    public static readonly DiagnosticDescriptor WA018_ConvertAddRemoveHandler = new(
        id: "WA018",
        title: "Convert AddHandler/RemoveHandler usage to Avalonia overloads",
        messageFormat: "Handler for '{0}' should be migrated to Avalonia AddHandler/RemoveHandler overloads",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "WPF AddHandler/RemoveHandler calls should be updated to use Avalonia's strongly typed overloads and routing strategies.");

    public static readonly DiagnosticDescriptor WA019_ConvertRaiseEvent = new(
        id: "WA019",
        title: "Convert RaiseEvent usage to Avalonia",
        messageFormat: "RaiseEvent invocation should be converted to Avalonia syntax",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "WPF RaiseEvent calls should be converted to Avalonia's RaiseEvent helpers with Avalonia event args.");

    public static readonly DiagnosticDescriptor WA020_ConvertAddOwner = new(
        id: "WA020",
        title: "Convert routed event AddOwner usage",
        messageFormat: "Routed event AddOwner call should be converted to Avalonia AddOwner pattern",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "WPF routed event AddOwner invocations should be migrated to Avalonia's AddOwner APIs.");
}
