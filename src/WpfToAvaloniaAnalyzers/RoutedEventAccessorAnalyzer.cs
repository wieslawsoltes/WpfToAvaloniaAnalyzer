using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RoutedEventAccessorAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA016_ConvertRoutedEventAccessors);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeEventDeclaration, SyntaxKind.EventDeclaration);
    }

    private static void AnalyzeEventDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not EventDeclarationSyntax eventDeclaration)
            return;

        var accessorList = eventDeclaration.AccessorList;
        if (accessorList == null)
            return;

        var addAccessor = accessorList.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.AddAccessorDeclaration));
        var removeAccessor = accessorList.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.RemoveAccessorDeclaration));

        if (addAccessor == null || removeAccessor == null)
            return;

        if (!ContainsHandlerInvocation(addAccessor, "AddHandler", context.SemanticModel, context.CancellationToken,
                out var addHandlerIdentifier))
        {
            return;
        }

        if (!ContainsHandlerInvocation(removeAccessor, "RemoveHandler", context.SemanticModel, context.CancellationToken,
                out var removeHandlerIdentifier))
        {
            return;
        }

        if (addHandlerIdentifier == null || removeHandlerIdentifier == null)
            return;

        if (!SymbolEqualityComparer.Default.Equals(addHandlerIdentifier, removeHandlerIdentifier))
            return;

        var eventTypeInfo = context.SemanticModel.GetTypeInfo(eventDeclaration.Type, context.CancellationToken);
        if (eventTypeInfo.Type is not INamedTypeSymbol eventTypeSymbol)
            return;

        if (!IsRoutedEventHandler(eventTypeSymbol))
            return;

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.WA016_ConvertRoutedEventAccessors,
            eventDeclaration.Identifier.GetLocation(),
            eventDeclaration.Identifier.Text);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool ContainsHandlerInvocation(
        AccessorDeclarationSyntax accessor,
        string methodName,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        out ISymbol? routedEventFieldSymbol)
    {
        routedEventFieldSymbol = null;

        ExpressionSyntax? expression = accessor.ExpressionBody?.Expression;

        if (expression == null && accessor.Body != null)
        {
            var statement = accessor.Body.Statements.FirstOrDefault() as ExpressionStatementSyntax;
            expression = statement?.Expression;
        }

        if (expression is not InvocationExpressionSyntax invocation)
            return false;

        var symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return false;

        if (!string.Equals(methodSymbol.Name, methodName, System.StringComparison.Ordinal))
            return false;

        if (methodSymbol.Parameters.Length < 2)
            return false;

        if (invocation.ArgumentList.Arguments.Count < 1)
            return false;

        var routedEventExpression = invocation.ArgumentList.Arguments[0].Expression;
        var symbol = semanticModel.GetSymbolInfo(routedEventExpression, cancellationToken).Symbol;

        if (symbol == null)
            return false;

        routedEventFieldSymbol = symbol;
        return true;
    }

    private static bool IsRoutedEventHandler(INamedTypeSymbol eventTypeSymbol)
    {
        // Accept RoutedEventHandler, RoutedEventHandler<T>, RoutedPropertyChangedEventHandler<T>, and delegates derived from MulticastDelegate with RoutedEventArgs
        if (eventTypeSymbol.ContainingNamespace?.ToDisplayString().StartsWith("System.Windows", StringComparison.Ordinal) == true &&
            eventTypeSymbol.Name == "RoutedEventHandler")
        {
            return true;
        }

        if (eventTypeSymbol.ContainingNamespace?.ToDisplayString().StartsWith("System.Windows", StringComparison.Ordinal) == true &&
            eventTypeSymbol.Name.StartsWith("RoutedEventHandler", StringComparison.Ordinal))
            return true;

        if (eventTypeSymbol.DelegateInvokeMethod is IMethodSymbol invokeMethod)
        {
            if (invokeMethod.Parameters.Length == 2)
            {
                var secondParameter = invokeMethod.Parameters[1];
                var namespaceName = secondParameter.Type.ContainingNamespace?.ToDisplayString();
                if (!string.IsNullOrEmpty(namespaceName) && namespaceName.StartsWith("System.Windows", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
