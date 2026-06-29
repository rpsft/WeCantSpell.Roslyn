using Microsoft.CodeAnalysis;

namespace WeCantSpell.Roslyn.Tests.Utilities
{
    public static class AssertionExtensions
    {
        public static DiagnosticAssertions Should(this Diagnostic diagnostic) =>
            new(diagnostic, AssertionChain.GetOrCreate());
    }
}
