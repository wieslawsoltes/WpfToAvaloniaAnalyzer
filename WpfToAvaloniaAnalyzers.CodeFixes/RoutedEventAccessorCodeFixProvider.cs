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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RoutedEventAccessorCodeFixProvider)), Shared]
public sealed class RoutedEventAccessorCodeFixProvider : CodeFixProvider
{
    private const string Title = "Convert event to Avalonia pattern";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.WA016_ConvertRoutedEventAccessors.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
        var eventDeclaration = node.FirstAncestorOrSelf<EventDeclarationSyntax>();
        if (eventDeclaration == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                cancellationToken => ConvertAsync(context.Document, eventDeclaration, cancellationToken),
                Title),
            diagnostic);
    }

    private static async Task<Document> ConvertAsync(Document document, EventDeclarationSyntax eventDeclaration, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        var newRoot = RoutedEventConversionService.ConvertRoutedEventAccessor(root, eventDeclaration, semanticModel);
        return document.WithSyntaxRoot(newRoot);
    }
}
