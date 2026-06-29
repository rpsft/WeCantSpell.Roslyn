using System.Linq;
using System.Threading.Tasks;
using WeCantSpell.Roslyn.Tests.Utilities;

namespace WeCantSpell.Roslyn.Tests.Integration.CSharp.Parsing
{
    public class CatchDeclarationSpellingTests : CSharpParsingTestBase
    {
        [Fact]
        public async Task can_check_spelling_of_catches()
        {
            var analyzer = new SpellingAnalyzerCSharp(new WrongWordChecker("bad", "Value"));
            var project = await ReadCodeFileAsProjectAsync("Catch.SimpleExamples.csx");

            var diagnostics = (await GetDiagnosticsAsync(project, analyzer)).ToList();

            using (new AssertionScope())
            {
                diagnostics.Should().HaveCount(2);
                diagnostics
                    .Should()
                    .SatisfyRespectively(
                        first =>
                            first
                                .Should()
                                .HaveMessageContaining("bad")
                                .And.HaveLineLocation(13, 45, 3, "Catch.SimpleExamples.csx"),
                        second =>
                            second
                                .Should()
                                .HaveMessageContaining("Value")
                                .And.HaveLineLocation(13, 48, 5, "Catch.SimpleExamples.csx")
                    );
            }
        }
    }
}
