using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using WeCantSpell.Hunspell;
using WeCantSpell.Roslyn.Infrastructure;

namespace WeCantSpell.Roslyn.Config
{
    /// <summary>
    /// Resolves the spell-check dictionaries and configuration for a syntax tree from the set of
    /// Roslyn <c>AdditionalFiles</c> declared for the compilation.
    /// </summary>
    /// <remarks>
    /// Reading dictionaries this way makes them tracked compilation inputs, so editing a dictionary
    /// invalidates the analyzer's cached results and triggers re-analysis automatically — no build
    /// server restart and no file-time polling. The index is built once per compilation; parsed word
    /// lists are cached against their <see cref="SourceText"/> so an edit (which produces a new
    /// <see cref="SourceText"/>) is picked up while unchanged content is reused.
    /// </remarks>
    internal sealed class AdditionalFilesDictionaryIndex
    {
        // Parsed word lists keyed on source-text identity. When Roslyn replaces a changed file's
        // SourceText, the stale WordList becomes collectible and the new content is parsed afresh.
        private static readonly ConditionalWeakTable<SourceText, WordList> s_wordListCache = new();

        private readonly IReadOnlyDictionary<string, AdditionalText> _dictionariesByDirectory;
        private readonly IReadOnlyDictionary<string, ConfigFile?> _configsByDirectory;
        private readonly IReadOnlyList<AdditionalText> _globalDictionaries;
        private readonly IReadOnlyList<ConfigFile?> _globalConfigs;

        // Per-compilation cache of the resolved checker by directory (resolution depends only on the
        // syntax tree's directory chain). Null means "no dictionary in AdditionalFiles → fall back".
        private readonly ConcurrentDictionary<string, ISpellChecker?> _spellCheckerByDirectory =
            new(StringComparer.OrdinalIgnoreCase);

        private AdditionalFilesDictionaryIndex(
            IReadOnlyDictionary<string, AdditionalText> dictionariesByDirectory,
            IReadOnlyDictionary<string, ConfigFile?> configsByDirectory,
            IReadOnlyList<AdditionalText> globalDictionaries,
            IReadOnlyList<ConfigFile?> globalConfigs
        )
        {
            _dictionariesByDirectory = dictionariesByDirectory;
            _configsByDirectory = configsByDirectory;
            _globalDictionaries = globalDictionaries;
            _globalConfigs = globalConfigs;
        }

        public static AdditionalFilesDictionaryIndex Build(ImmutableArray<AdditionalText> additionalFiles)
        {
            var dictionaries = new Dictionary<string, (int Priority, AdditionalText File)>(
                StringComparer.OrdinalIgnoreCase
            );
            var configs = new Dictionary<string, (int Priority, ConfigFile? Config)>(StringComparer.OrdinalIgnoreCase);
            var globalDictionaries = new List<AdditionalText>();
            var globalConfigs = new List<ConfigFile?>();

            foreach (var file in additionalFiles)
            {
                var path = file.Path;
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var normalized = Normalize(path);
                var fileName = GetFileName(normalized);

                var dictionaryPriority = IndexOf(SpellCheckerOptions.DictionaryFileNames, fileName);
                if (dictionaryPriority >= 0)
                {
                    if (IsRooted(path))
                    {
                        AddByPriority(dictionaries, GetDirectory(normalized), dictionaryPriority, file);
                    }
                    else
                    {
                        globalDictionaries.Add(file);
                    }

                    continue;
                }

                var configPriority = IndexOf(SpellCheckerOptions.ConfigFileNames, fileName);
                if (configPriority >= 0)
                {
                    var config = ParseConfig(file);
                    if (IsRooted(path))
                    {
                        AddByPriority(configs, GetDirectory(normalized), configPriority, config);
                    }
                    else
                    {
                        globalConfigs.Add(config);
                    }
                }
            }

            var dictionariesByDirectory = new Dictionary<string, AdditionalText>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in dictionaries)
            {
                dictionariesByDirectory[entry.Key] = entry.Value.File;
            }

            var configsByDirectory = new Dictionary<string, ConfigFile?>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in configs)
            {
                configsByDirectory[entry.Key] = entry.Value.Config;
            }

            return new AdditionalFilesDictionaryIndex(
                dictionariesByDirectory,
                configsByDirectory,
                globalDictionaries,
                globalConfigs
            );
        }

        /// <summary>
        /// Builds (or returns a cached) spell checker for the given syntax tree from AdditionalFiles.
        /// Returns <c>null</c> when no dictionary is declared for the tree, signalling the caller to
        /// fall back to the legacy on-disk discovery.
        /// </summary>
        public ISpellChecker? GetSpellChecker(string? treeFilePath, CancellationToken cancellationToken)
        {
            var directory = treeFilePath == null ? string.Empty : GetDirectory(Normalize(treeFilePath));
            return _spellCheckerByDirectory.GetOrAdd(
                directory,
                _ => BuildSpellChecker(treeFilePath, cancellationToken)
            );
        }

        private ISpellChecker? BuildSpellChecker(string? treeFilePath, CancellationToken cancellationToken)
        {
            var resolution = Resolve(treeFilePath);
            if (!resolution.Matched)
            {
                return null;
            }

            var providers = new List<IWordListProvider>(resolution.Dictionaries.Count);
            foreach (var file in resolution.Dictionaries)
            {
                var text = file.GetText(cancellationToken);
                if (text == null)
                {
                    continue;
                }

                var wordList = s_wordListCache.GetValue(text, DictionaryWordListReader.Read);
                providers.Add(new StaticWordListProvider(wordList));
            }

            if (providers.Count == 0)
            {
                return null;
            }

            return new StaticSpellChecker(resolution.Languages, providers);
        }

        /// <summary>
        /// Resolves the applicable dictionaries (nearest directory first) and merged language set for
        /// a syntax tree, walking up the directory tree and stopping at a config with <c>isRoot</c>.
        /// </summary>
        public Resolution Resolve(string? treeFilePath)
        {
            var dictionaries = new List<AdditionalText>();
            var languages = new HashSet<string>(SpellCheckerOptions.DefaultLanguageCodes);
            var matched = false;

            if (!string.IsNullOrEmpty(treeFilePath))
            {
                var directory = GetDirectory(Normalize(treeFilePath!));
                while (directory.Length > 0)
                {
                    if (_dictionariesByDirectory.TryGetValue(directory, out var dictionary))
                    {
                        dictionaries.Add(dictionary);
                        matched = true;
                    }

                    var isRoot = false;
                    if (_configsByDirectory.TryGetValue(directory, out var config))
                    {
                        MergeLanguages(languages, config);
                        isRoot = config?.IsRoot == true;
                    }

                    if (isRoot)
                    {
                        break;
                    }

                    var parent = GetDirectory(directory);
                    if (parent.Length == directory.Length)
                    {
                        break;
                    }

                    directory = parent;
                }
            }

            // Non-rooted (relative) AdditionalFiles apply to every tree as the outermost dictionaries.
            foreach (var dictionary in _globalDictionaries)
            {
                dictionaries.Add(dictionary);
                matched = true;
            }

            foreach (var config in _globalConfigs)
            {
                MergeLanguages(languages, config);
            }

            return new Resolution(dictionaries, languages, matched);
        }

        private static ConfigFile? ParseConfig(AdditionalText file)
        {
            var text = file.GetText();
            if (text == null)
            {
                return null;
            }

            try
            {
                return SpellCheckerOptions.ParseJson(text.ToString());
            }
            catch
            {
                // A malformed config file should not crash analysis; treat it as empty.
                return null;
            }
        }

        private static void MergeLanguages(HashSet<string> languages, ConfigFile? config)
        {
            if (config?.Languages == null)
            {
                return;
            }

            foreach (var code in config.Languages)
            {
                languages.Add(code);
            }
        }

        private static void AddByPriority<T>(
            IDictionary<string, (int Priority, T Value)> map,
            string directory,
            int priority,
            T value
        )
        {
            if (!map.TryGetValue(directory, out var existing) || priority < existing.Priority)
            {
                map[directory] = (priority, value);
            }
        }

        private static int IndexOf(string[] names, string fileName)
        {
            for (var i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsRooted(string path)
        {
            try
            {
                return System.IO.Path.IsPathRooted(path);
            }
            catch
            {
                return false;
            }
        }

        private static string Normalize(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string GetDirectory(string normalizedPath)
        {
            var index = normalizedPath.LastIndexOf('/');
            return index < 0 ? string.Empty : normalizedPath.Substring(0, index);
        }

        private static string GetFileName(string normalizedPath)
        {
            var index = normalizedPath.LastIndexOf('/');
            return index < 0 ? normalizedPath : normalizedPath.Substring(index + 1);
        }

        internal readonly struct Resolution
        {
            public Resolution(IReadOnlyList<AdditionalText> dictionaries, ISet<string> languages, bool matched)
            {
                Dictionaries = dictionaries;
                Languages = languages;
                Matched = matched;
            }

            /// <summary>Applicable dictionaries, nearest directory first.</summary>
            public IReadOnlyList<AdditionalText> Dictionaries { get; }

            /// <summary>Merged language codes (defaults plus any configured languages).</summary>
            public ISet<string> Languages { get; }

            /// <summary>True when at least one dictionary was found in AdditionalFiles.</summary>
            public bool Matched { get; }
        }
    }
}
