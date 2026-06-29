using System.Collections.Generic;

namespace WeCantSpell.Roslyn
{
    /// <summary>
    /// Spell checker backed by embedded language dictionaries plus a fixed set of project
    /// dictionaries that were already parsed (e.g. read from Roslyn <c>AdditionalFiles</c>).
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ConfigurableSpellChecker"/> this type does not read from disk and holds no
    /// file watchers: invalidation is driven entirely by Roslyn replacing the underlying source text.
    /// </remarks>
    internal sealed class StaticSpellChecker : EmbeddedSpellChecker
    {
        public StaticSpellChecker(IEnumerable<string> languageCodes, IEnumerable<IWordListProvider> dictionaries)
            : base(languageCodes)
        {
            foreach (var dictionary in dictionaries)
            {
                Providers.Add(dictionary);
            }
        }
    }
}
