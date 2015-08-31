using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DefaultIfNullAnalyzer
{
    class DefaultIfNullFixAllProvider : FixAllProvider
    {
        /// <summary>Singleton Instance</summary>
        public static FixAllProvider Instance { get; } = new DefaultIfNullFixAllProvider();

        private DefaultIfNullFixAllProvider() {}

        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() 
            => new[] {FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution};

        public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext) 
            => Task.FromResult(GetFix(fixAllContext));

        private static CodeAction GetFix(FixAllContext fixAllContext)
        {
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Solution:
                case FixAllScope.Project:
                    return CodeAction.Create(
                        title: DefaultIfNullCodeFixProvider.Title,
                        createChangedSolution: (c => CreateChangedSolutionAsync(fixAllContext, c)),
                        equivalenceKey: DefaultIfNullCodeFixProvider.Title);

                case FixAllScope.Document:
                    return CodeAction.Create(
                        title: DefaultIfNullCodeFixProvider.Title,
                        createChangedDocument: c => CreateChangedDocumentAsync(fixAllContext, fixAllContext.Document, c),
                        equivalenceKey: DefaultIfNullCodeFixProvider.Title);
                default:
                    return null;
            }
        }

        private static async Task<Solution> CreateChangedSolutionAsync(FixAllContext fixAllContext, CancellationToken cancellationToken)
        {
            // Find all documents in scope
            var projects = fixAllContext.Scope == FixAllScope.Solution ? fixAllContext.Solution.Projects : new[] { fixAllContext.Project };
            var documents = projects.SelectMany(p => p.Documents).ToImmutableArray();

            // Start a tasks for each document
            var documentTasks = documents.Select(d => CreateNewDocumentSyntaxRootAsync(fixAllContext, d, cancellationToken)).ToArray();

            // Now update the solution with all the changed documents
            var newSolution = fixAllContext.Solution;
            for (var index = 0; index < documentTasks.Length; index++)
            {
                var newDocRoot = await documentTasks[index];
                newSolution = newSolution.WithDocumentSyntaxRoot(documents[index].Id, newDocRoot);
            }
            return newSolution;
        }

        private static async Task<Document> CreateChangedDocumentAsync(FixAllContext fixAllContext, Document document, CancellationToken cancellationToken)
        {
            var newRoot = await CreateNewDocumentSyntaxRootAsync(fixAllContext, document, cancellationToken);

            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<SyntaxNode> CreateNewDocumentSyntaxRootAsync(FixAllContext fixAllContext, Document document, CancellationToken cancellationToken)
        {
            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document);

            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var nodesToFix = diagnostics.Select(diagnostic => DefaulIfNullExpressionHelper.GetTargetExpression(diagnostic, root));
            var newRoot = root.ReplaceNodes(nodesToFix,
                (orignalNode, rewritten) => DefaulIfNullExpressionHelper.CreateRelacementNode(rewritten));
            return newRoot;
        }
    }
}
