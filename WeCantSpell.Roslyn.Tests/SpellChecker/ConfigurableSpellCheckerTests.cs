using System.Collections.Generic;
using FluentAssertions.Execution;
using WeCantSpell.Roslyn.Config;
using WeCantSpell.Roslyn.Tests.Utilities;

namespace WeCantSpell.Roslyn.Tests.SpellChecker
{
    [TestCategory("SpellChecker")]
    public class ConfigurableSpellCheckerTests
    {
        [Fact]
        public void ShouldReadLanguageListFromFileSystem()
        {
            var options = new SpellCheckerOptions(
                Path.Combine(Directory.GetCurrentDirectory(), "SpellChecker", "TestDirectory")
            );
            using (new AssertionScope())
            {
                options.LanguageCodes.Should().BeEquivalentTo(new[] { "en-US", "ru-RU" });
                options.AdditionalDictionaryPaths.Should().HaveCount(1);
            }
        }

        [Fact]
        public void ShouldReadDictionaryFromFileSystem()
        {
            var spellchecker = new ConfigurableSpellChecker(
                new SpellCheckerOptions
                {
                    LanguageCodes = new HashSet<string>(),
                    AdditionalDictionaryPaths = new List<string>
                    {
                        Path.Combine(Directory.GetCurrentDirectory(), "SpellChecker", "Files", "FantasyWords.dic")
                    }
                }
            );
            spellchecker.Check("Bazinga").Should().BeTrue();
            spellchecker.Check("Froomplestoot").Should().BeFalse();
        }

        [Fact]
        public void ShouldReadDictionaryFromResourceFileSystem()
        {
            var fileSystem = new ResourceFileSystem();
            var spellchecker = new ConfigurableSpellChecker(
                new SpellCheckerOptions(fileSystem, "SpellChecker.Files")
                {
                    LanguageCodes = new HashSet<string>(),
                    AdditionalDictionaryPaths = new List<string> { "Files.FantasyWords.dic" }
                }
            );
            spellchecker.Check("Bazinga").Should().BeTrue();
            spellchecker.Check("Froomplestoot").Should().BeFalse();
        }
    }
}