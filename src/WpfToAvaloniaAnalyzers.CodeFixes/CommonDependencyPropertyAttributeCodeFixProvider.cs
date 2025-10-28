using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace WpfToAvaloniaAnalyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CommonDependencyPropertyAttributeCodeFixProvider)), Shared]
public sealed class CommonDependencyPropertyAttributeCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove CommonDependencyProperty attribute";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.WA011_RemoveCommonDependencyPropertyAttribute.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                cancellationToken => RemoveAttributeAsync(context.Document, node, cancellationToken),
                Title),
            diagnostic);
    }

    private static async Task<Document> RemoveAttributeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var attribute = node as AttributeSyntax ?? node.FirstAncestorOrSelf<AttributeSyntax>();
        if (attribute == null)
            return document;

        if (attribute.Parent is AttributeListSyntax attributeList)
        {
            if (attributeList.Attributes.Count == 1)
            {
                editor.RemoveNode(attributeList);
            }
            else
            {
                editor.ReplaceNode(attributeList, attributeList.WithAttributes(attributeList.Attributes.Remove(attribute)));
            }
        }
        else
        {
            editor.RemoveNode(attribute);
        }

        return editor.GetChangedDocument();
    }
}
