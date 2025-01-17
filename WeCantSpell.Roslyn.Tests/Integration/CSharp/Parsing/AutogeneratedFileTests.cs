using System.Threading.Tasks;
using WeCantSpell.Roslyn.Tests.Utilities;

namespace WeCantSpell.Roslyn.Tests.Integration.CSharp.Parsing
{
    public class AutogeneratedFileTests : CSharpParsingTestBase
    {
        [Fact]
        public async Task ShouldIgnoreAutoGeneratedFiles()
        {
            var analyzer = new SpellingAnalyzerCSharp(new WrongWordChecker("auto-generated"));
            var project = await ReadCodeFileAsProjectAsync("Autogenerated.SimpleExamples.csx");

            var diagnostics = await GetDiagnosticsAsync(project, analyzer);
            diagnostics.Should().BeEmpty();
        }
    }
}
