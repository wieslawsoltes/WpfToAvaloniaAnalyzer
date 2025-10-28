using System.Collections.Immutable;
using System.Linq;
using WpfToAvaloniaAnalyzers.Cli;
using WpfToAvaloniaAnalyzers.CodeFixes;

namespace WpfToAvaloniaAnalyzers.Cli.Tests;

public class AnalyzerLoaderTests
{
    [Fact]
    public void LoadCodeFixProviders_IncludesRoutedEventAddOwner()
    {
        var requestedIds = ImmutableHashSet.Create("WA020");

        var codeFixSet = AnalyzerLoader.LoadCodeFixProviders(requestedIds);

        Assert.Contains("WA020", codeFixSet.FixableDiagnosticIds);
        Assert.Contains(codeFixSet.Providers, provider => provider is RoutedEventAddOwnerCodeFixProvider);
        Assert.Contains(codeFixSet.ProvidersByDiagnosticId["WA020"], provider => provider is RoutedEventAddOwnerCodeFixProvider);
        Assert.Contains("WA020", codeFixSet.FixAllCapableDiagnosticIds);
    }

    [Fact]
    public void LoadCodeFixProviders_WithNoFilter_ExposesRoutedEventDiagnostics()
    {
        var codeFixSet = AnalyzerLoader.LoadCodeFixProviders(ImmutableHashSet<string>.Empty);

        Assert.Contains("WA020", codeFixSet.FixableDiagnosticIds);
        var providerNames = codeFixSet.Providers.Select(provider => provider.GetType().FullName).ToArray();
        var orderedNames = providerNames.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        Assert.Equal(orderedNames, providerNames);
    }
}
