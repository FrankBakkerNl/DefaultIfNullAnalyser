using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;

namespace DefaultIfNullAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DefaultIfNullCodeFixProvider)), Shared]
    public class DefaultIfNullCodeFixProvider : CodeFixProvider
    {
        public const string Title = "Replace with ?.";

        public sealed override ImmutableArray<string> FixableDiagnosticIds 
            => ImmutableArray.Create(DefaultIfNullAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => DefaultIfNullFixAllProvider.Instance;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(CodeAction.Create(
                                        title: Title,
                                        createChangedDocument: c => CreateChangedDocument(context, c),
                                        equivalenceKey: Title), 
                                    context.Diagnostics.First());

            return Task.FromResult(true);
        }


        private static async Task<Document> CreateChangedDocument(CodeFixContext codeFixContext, CancellationToken cancellationToken)
        {
            var document = codeFixContext.Document;
            var diagnostic = codeFixContext.Diagnostics.First();

            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var invocationExpression = DefaulIfNullExpressionHelper.GetTargetExpression(diagnostic, root);
            var replacement = DefaulIfNullExpressionHelper.CreateRelacementNode(invocationExpression);
            var newRoot = root.ReplaceNode(invocationExpression, replacement);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}