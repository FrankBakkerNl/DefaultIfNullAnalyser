using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DefaultIfNullAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DefaultIfNullAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DefaultIfNullAnalyzer";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Pre C#6 code";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            // Register that we want to see SimpleMemberAccessExpressions
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var node = (context.Node as MemberAccessExpressionSyntax);

            if (node == null || !DefaulIfNullExpressionHelper.CanFix(node)) return;

            var diagnostic = Diagnostic.Create(Rule, node.Name.GetLocation(), node.Name.ToString());

            context.ReportDiagnostic(diagnostic);
        }
    }
}
