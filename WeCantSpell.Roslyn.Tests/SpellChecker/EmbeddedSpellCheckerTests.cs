using WeCantSpell.Roslyn.Tests.Utilities;

namespace WeCantSpell.Roslyn.Tests.SpellChecker
{
    [TestCategory("SpellChecker")]
    public class EmbeddedSpellCheckerTests
    {
        [Fact]
        public void ShouldReadFromEnglishDictionary()
        {
            var checker = new EmbeddedSpellChecker(new[] { "en-US" });
            using (new AssertionScope())
            {
                checker.Check("Thas").Should().Be(false);
                checker.Check("This").Should().Be(true);
            }
        }

        [Fact]
        public void ShouldReadFromTwoDictionaries()
        {
            var checker = new EmbeddedSpellChecker(new[] { "en-US", "ru-RU" });
            using (new AssertionScope())
            {
                checker.Check("Thas").Should().Be(false);
                checker.Check("Этат").Should().Be(false);
                checker.Check("This").Should().Be(true);
                checker.Check("Тот").Should().Be(true);
            }
        }

        [Fact]
        public void ShouldProvideSuggestions()
        {
            ISpellChecker checker = new EmbeddedSpellChecker(new[] { "en-US", "ru-RU" });
            using (new AssertionScope())
            {
                checker.Suggest("Thes").Should().Contain("The");
                // The suggested words are deterministic, but their casing is not: on at least one
                // CI runner Hunspell returns the Cyrillic suggestion lower-cased ("этот") instead
                // of title-cased ("Этот"), because that node's .NET/ICU does not upper-case
                // Cyrillic (other runners and local runs do). This is a runner-environment quirk,
                // not the behaviour under test, so assert on the word irrespective of case.
                checker.Suggest("Этат").Should().Contain(s => s == "Этот" || s == "этот");
            }
        }
    }
}
