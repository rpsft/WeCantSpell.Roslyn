using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using WeCantSpell.Hunspell;

namespace WeCantSpell.Roslyn.Infrastructure
{
    /// <summary>
    /// Builds a <see cref="WordList"/> from the textual content of a project dictionary.
    /// </summary>
    /// <remarks>
    /// Project dictionaries use a single, unified format: one word per line. Blank lines and
    /// lines starting with <c>#</c> are ignored, and a leading Hunspell word-count header (a first
    /// content line consisting only of digits) is skipped for backwards compatibility with the
    /// legacy <c>.wecantspell.dic</c> files.
    /// </remarks>
    internal static class DictionaryWordListReader
    {
        public static WordList Read(SourceText text)
        {
            return ReadFromLines(text.Lines.Select(line => line.ToString()));
        }

        public static WordList ReadFromLines(IEnumerable<string> lines)
        {
            return WordList.CreateFromWords(EnumerateWords(lines));
        }

        private static IEnumerable<string> EnumerateWords(IEnumerable<string> lines)
        {
            var sawContent = false;
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                if (!sawContent)
                {
                    sawContent = true;
                    // A Hunspell .dic begins with a word-count header; treat a leading all-digit
                    // line as that header rather than as a word.
                    if (IsCountHeader(line))
                    {
                        continue;
                    }
                }

                yield return line;
            }
        }

        private static bool IsCountHeader(string line)
        {
            foreach (var c in line)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
