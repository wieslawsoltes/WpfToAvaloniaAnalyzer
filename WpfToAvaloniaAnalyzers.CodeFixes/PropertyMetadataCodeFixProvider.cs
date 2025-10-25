using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvaloniaAnalyzers.CodeFixes.Services;

namespace WpfToAvaloniaAnalyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PropertyMetadataCodeFixProvider)), Shared]
public class PropertyMetadataCodeFixProvider : CodeFixProvider
{
    private const string Title = "Convert PropertyMetadata to Avalonia property with callback";

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.WA005_ConvertPropertyMetadata.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the PropertyMetadata creation
        var propertyMetadata = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<ObjectCreationExpressionSyntax>()
            .First();

        if (propertyMetadata == null)
            return;

        // Find the containing DependencyProperty field
        var fieldVariable = propertyMetadata.Ancestors()
            .OfType<VariableDeclaratorSyntax>()
            .FirstOrDefault();

        if (fieldVariable == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => ConvertToAvaloniaPropertyWithCallbackAsync(context.Document, fieldVariable, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> ConvertToAvaloniaPropertyWithCallbackAsync(
        Document document,
        VariableDeclaratorSyntax fieldVariable,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        var newRoot = DependencyPropertyService.ConvertDependencyPropertyToStyledProperty(root, fieldVariable, semanticModel);
        return document.WithSyntaxRoot(newRoot);
    }
}
