using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DefaultIfNullAnalyzer
{
    /// <summary>
    /// Class for analysing and fixing DefaulIfNullExpressions
    /// As a lot of the code is shared between the Analyzer, FixProvider and FixAllProvider, this is placed in this helper class
    /// </summary>
    public class DefaulIfNullExpressionHelper
    {
        private static readonly string[] MethodNamesToRefactor = { "DefaultIfNull", "NullSafe", "IfNotNull" };

        /// <summary>
        /// Determines if a MemberAccessExpressionSyntax can be fixed by our codefix
        /// </summary>
        public static bool CanFix(MemberAccessExpressionSyntax memberAccessExpression)
        {
            // Verify if it is a call to one of the methods we can fix
            if (!MethodNamesToRefactor.Contains(memberAccessExpression.Name.ToString())) return false;

            // The parent should be an InvocationExpressionSyntax
            var defaultIfNullexpression = memberAccessExpression.Parent as InvocationExpressionSyntax;

            // There should be one argument, the lambda expression
            if (defaultIfNullexpression?.ArgumentList.Arguments.Count != 1) return false;

            var lambda = GetLambdaFromFirstArgument(defaultIfNullexpression);
            if (lambda == null) return false;

            // Check if the Lambas body starts with a AccessExpression of the parameter
            return GetLeftMostAccessExpression(lambda) != null;
        }

        /// <summary>
        /// Finds the InvocationExpressionSyntax the diagnostic was created for
        /// </summary>
        /// <param name="diagnostic">The dignostic to find</param>
        /// <param name="rootNode">The documentRoot in which to look </param>
        public static InvocationExpressionSyntax GetTargetExpression(Diagnostic diagnostic, SyntaxNode rootNode)
        {
            // The diagnostic location points to the Token 'DefaultIfNull'
            // This Token has a parent SimpleName with a parent MemberAccessExpressionSyntax with a parent InvocationExpressionSyntax
            return (InvocationExpressionSyntax)rootNode.FindToken(diagnostic.Location.SourceSpan.Start).Parent.Parent.Parent;
        }

        /// <summary>
        /// Creates a new ExpressionSyntax to replace the defaultIfNull InvocationExpressionSyntax
        /// </summary>
        public static ExpressionSyntax CreateRelacementNode(InvocationExpressionSyntax defaultIfNullexpression)
        {
            // We should change from  x.DefaultIfNull(y => y.z...)  or  x.DefaultIfNull(y => y.[..]...)
            //                  into  x?.z...                           x?[..]...

            // Get the expression on which DefaultIfNull is invoked, this is the x
            var rootExpression = (defaultIfNullexpression.Expression as MemberAccessExpressionSyntax)?.Expression;

            // Get the first parameter to DefaultIfNull, this is the Lambda y => y.z...
            var lambda = GetLambdaFromFirstArgument(defaultIfNullexpression);
            if (lambda == null) return defaultIfNullexpression;

            // Get the MemberAccessExpression y.z or ElementAccessExpression y[..] from the lamba body
            var accessExpression = GetLeftMostAccessExpression(lambda);
            if (accessExpression == null) return defaultIfNullexpression;

            // A ConditionalAccessExpression requires a ..BindingExpression instead of the ..AccessExpression
            var bindingExpression = ToBindingExpression(accessExpression);

            // Replace the AccessExpression from the Labmda body with the BindingExpression this will result in the .z... or [..]... part
            var whenNotNullExpression = (ExpressionSyntax)lambda.Body.ReplaceNode(accessExpression, bindingExpression)
                                                                        .WithTriviaFrom(accessExpression);

            // Combine the x and .z... into x?.z...
            return SyntaxFactory.ConditionalAccessExpression(
                expression: rootExpression,
                whenNotNull: whenNotNullExpression).WithTriviaFrom(defaultIfNullexpression);
        }

        private static SimpleLambdaExpressionSyntax GetLambdaFromFirstArgument(InvocationExpressionSyntax defaultIfNullexpression) 
            => defaultIfNullexpression?.ArgumentList.Arguments.FirstOrDefault()?.Expression as SimpleLambdaExpressionSyntax;

        /// <summary>
        /// Translates a MemberAccessExpressionSyntax or ElementAccessExpressionSyntax into a MemberBindingExpression or ElementBindingExpression
        /// </summary>
        public static SyntaxNode ToBindingExpression(SyntaxNode accesExpression)
        {
            // We need to transform a MemberAccessExpressionSyntax 'a.b' into a MemberBindingExpression '.b'
            var memberAccesExpression = accesExpression as MemberAccessExpressionSyntax;
            if (memberAccesExpression != null)
            {
                return SyntaxFactory.MemberBindingExpression(memberAccesExpression.Name);
            }

            // or a ElementAccessExpressionSyntax 'a[2]' into a ElementBindingExpression '[2]'
            var elementAccesExpression = accesExpression as ElementAccessExpressionSyntax;
            if (elementAccesExpression != null)
            {
                return SyntaxFactory.ElementBindingExpression(elementAccesExpression.ArgumentList);
            }
            throw new ArgumentException("Cannot create binding Expression for " + accesExpression);
        }

        /// <summary>
        /// Finds the Left-most Expresison of the expression tree if it is a MemberAccessExpressionSyntax or a ElementAccessExpressionSyntax
        /// </summary>
        /// <param name="lambda"></param>
        /// <returns></returns>
        public static SyntaxNode GetLeftMostAccessExpression(SimpleLambdaExpressionSyntax lambda)
        {
            // The Lamba might look like
            // a => a.b.c().d or 
            // a => a[2].b.c().d

            // The left most part of the body should be a SimpleNameSyntax with the same name as the lambda argument
            var nameSyntax = lambda.Body.DescendantTokens().FirstOrDefault().Parent as SimpleNameSyntax;

            if (nameSyntax == null || nameSyntax.ToString() != lambda.Parameter.ToString()) return null;

            // We can convert it if we find a MemberAccessExpressionSyntax or an ElementAccessExpressionSyntax as the parent
            var parent = nameSyntax.Parent;
            if (parent is MemberAccessExpressionSyntax || parent is ElementAccessExpressionSyntax)
                return parent;

            return null;
        }
    }
}