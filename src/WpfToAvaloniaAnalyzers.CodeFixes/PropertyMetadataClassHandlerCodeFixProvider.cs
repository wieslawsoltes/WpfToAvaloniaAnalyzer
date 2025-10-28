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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PropertyMetadataClassHandlerCodeFixProvider)), Shared]
public sealed class PropertyMetadataClassHandlerCodeFixProvider : CodeFixProvider
{
    private const string Title = "Convert PropertyMetadata callback to class handler";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.WA008_ConvertPropertyMetadataCallbackToClassHandler.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var propertyMetadata = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<ObjectCreationExpressionSyntax>()
            .FirstOrDefault();

        if (propertyMetadata == null)
            return;

        var fieldVariable = propertyMetadata.Ancestors()
            .OfType<VariableDeclaratorSyntax>()
            .FirstOrDefault();

        if (fieldVariable == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                c => ConvertAsync(context.Document, fieldVariable, c),
                Title),
            diagnostic);
    }

    private static async Task<Document> ConvertAsync(
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
