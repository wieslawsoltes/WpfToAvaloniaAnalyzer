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
}