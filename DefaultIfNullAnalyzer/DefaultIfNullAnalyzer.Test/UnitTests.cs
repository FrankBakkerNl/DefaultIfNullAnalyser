using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace DefaultIfNullAnalyzer.Test
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void CanNotFixTest()
        {
            TestCanNotFix("x.balba(y => y.Length)");
            TestCanNotFix("x.DefaultIfNull");
            TestCanNotFix("x.DefaultIfNull(y => bla)");
            TestCanNotFix("x.DefaultIfNull(y)");
            TestCanNotFix("x.DefaultIfNull()");
            TestCanNotFix("x.DefaultIfNull(y => bla, 2)");
            TestCanNotFix("x.DefaultIfNull((y) => y.Length)");
            TestCanNotFix("x.DefaultIfNull(y => { return y.Length; })");
            TestCanNotFix("x.DefaultIfNull(y => x)");
            TestCanNotFix("x.DefaultIfNull(y => y + 2)");
            TestCanNotFix("x.DefaultIfNull(y => x.y)");
            TestCanNotFix("x.DefaultIfNull(y =>)");
        }

        [TestMethod]
        public void FixTest()
        {
            TestFix("x.DefaultIfNull(y => y.Length)", "x?.Length");
            TestFix("x.IfNotNull(y => y.Length)", "x?.Length");
            TestFix("x.NullSafe(y => y.Length)", "x?.Length");
            TestFix("x.DefaultIfNull(y => y.Length[1]", "x?.Length[1]");
            TestFix("x.DefaultIfNull(y => y[2])", "x?[2]");
            TestFix("x.DefaultIfNull(y => y.ToString())", "x?.ToString()");
            TestFix("x.DefaultIfNull(y => y.ToString().ToString())", "x?.ToString().ToString()");
            TestFix("x.DefaultIfNull(y => y.ToString().ToString())", "x?.ToString().ToString()");
            TestFix("x.DefaultIfNull(y => y.Length).DefaultIfNull(y => y.Length)", "x.DefaultIfNull(y => y.Length)?.Length");
            TestFix("x.DefaultIfNull(y => y.x + z)", "x?.x + z");
        }


        private static void TestCanNotFix(string codeSnippet)
        {
            var expressionSyntax = SyntaxFactory.ParseExpression(codeSnippet);
            var memberAccessExpressionSyntax = expressionSyntax.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().First();
            var result = DefaulIfNullExpressionHelper.CanFix(memberAccessExpressionSyntax);
            Assert.IsFalse(result);
        }

        private static void TestFix(string codeSnippet, string expectedResult)
        {
            var expressionSyntax = SyntaxFactory.ParseExpression(codeSnippet);
            var memberAccessExpressionSyntax = expressionSyntax.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().First();

            var canFix = DefaulIfNullExpressionHelper.CanFix(memberAccessExpressionSyntax);
            Assert.IsTrue(canFix);

            var resultSyntax = DefaulIfNullExpressionHelper.CreateRelacementNode((InvocationExpressionSyntax) expressionSyntax);
            var resultSTring = resultSyntax.ToFullString();

            Assert.AreEqual(expectedResult, resultSTring);
        }
    }
}