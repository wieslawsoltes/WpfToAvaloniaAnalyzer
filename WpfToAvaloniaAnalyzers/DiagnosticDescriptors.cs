using Microsoft.CodeAnalysis;

namespace WpfToAvaloniaAnalyzers;

public static class DiagnosticDescriptors
{
    private const string Category = "WpfToAvalonia";

    // Example diagnostic descriptor
    public static readonly DiagnosticDescriptor WPF001_UseAvaloniaControl = new(
        id: "WPF001",
        title: "Use Avalonia control instead of WPF control",
        messageFormat: "Consider using Avalonia.Controls.{0} instead of System.Windows.Controls.{0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "WPF controls should be replaced with their Avalonia equivalents during migration.");
}