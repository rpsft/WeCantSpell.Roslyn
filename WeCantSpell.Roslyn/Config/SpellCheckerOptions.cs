using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WeCantSpell.Roslyn.Infrastructure;

namespace WeCantSpell.Roslyn.Config
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    public sealed class SpellCheckerOptions
    {
        internal static string[] DefaultLanguageCodes { get; } = new[] { "en-US" };

        public static string[] ConfigFileNames { get; } = { ".wecantspell", ".wecantspell.json" };

        public static string[] DictionaryFileNames { get; } = { ".spelling.dic", ".directory.dic", ".wecantspell.dic" };
        public HashSet<string> LanguageCodes { get; init; } = new(DefaultLanguageCodes);

        public IList<string> AdditionalDictionaryPaths { get; init; } = new List<string>();

        internal IFileSystem FileSystem { get; }

        public SpellCheckerOptions(IFileSystem fileSystem, string path)
            : this(fileSystem)
        {
            ReadFromDirectory(path);
        }

        public SpellCheckerOptions(string path)
            : this()
        {
            ReadFromDirectory(path);
        }

        public SpellCheckerOptions()
            : this(new FileSystem()) { }

        public SpellCheckerOptions(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        public override int GetHashCode()
        {
            return GetStringArrayHashCode(LanguageCodes) * 17 + GetStringArrayHashCode(AdditionalDictionaryPaths);
        }

        public override bool Equals(object obj)
        {
            if (obj is not SpellCheckerOptions options)
                return false;
            return GetHashCode() == options.GetHashCode();
        }

        private int GetStringArrayHashCode(IEnumerable<string>? array)
        {
            return array?.Aggregate(17, (current, item) => current * 23 + (item?.GetHashCode() ?? 0)) ?? 0;
        }

        private void ReadFromDirectory(string path)
        {
            var directory = FileSystem.DirectoryExists(path) ? path : FileSystem.GetDirectoryName(path);

            while (directory != null)
            {
                var dictionaryName = GuessDictionaryFileName(directory);
                if (dictionaryName != null)
                {
                    AdditionalDictionaryPaths.Add(dictionaryName);
                }

                var configFileName = GuessConfigFileName(directory);
                if (configFileName != null)
                {
                    ConfigFile? configFile = Parse(configFileName);
                    MergeFrom(configFile);
                    if (configFile?.IsRoot == true)
                        break;
                }

                directory = FileSystem.GetParentDirectory(directory);
            }
        }

        private ConfigFile? Parse(string filePath)
        {
            var text = FileSystem.ReadAllText(filePath);
            return text == null ? null : ParseJson(text);
        }

        internal static ConfigFile? ParseJson(string json)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy(), // Use CamelCaseNamingStrategy for camelCase naming
                },
                // Set the following property to ignore case sensitivity during deserialization
                // Note: This will match the property names case-insensitively but still use camelCase
                TypeNameHandling = TypeNameHandling.Auto,
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
            };
            ConfigFile? config = JsonConvert.DeserializeObject<ConfigFile>(json, settings);
            return config;
        }

        private void MergeFrom(ConfigFile? localOptions)
        {
            if (localOptions?.Languages == null)
                return;
            foreach (var code in localOptions.Languages.Where(code => !LanguageCodes.Contains(code)))
            {
                LanguageCodes.Add(code);
            }
        }

        private string? GuessConfigFileName(string directoryPath)
        {
            return ConfigFileNames
                .Select(fileName => TryFileExists(directoryPath, fileName))
                .FirstOrDefault(fullPath => fullPath != null);
        }

        private string? GuessDictionaryFileName(string directoryPath)
        {
            return DictionaryFileNames
                .Select(fileName => TryFileExists(directoryPath, fileName))
                .FirstOrDefault(fullPath => fullPath != null);
        }

        private string? TryFileExists(string directoryName, string fileName)
        {
            var tryFilePath = FileSystem.CombinePath(directoryName, fileName);
            return FileSystem.FileExists(tryFilePath) ? tryFilePath : null;
        }

        public string? FindDictionaryForPath(string? path)
        {
            if (path == null)
                return null;
            var directory = FileSystem.DirectoryExists(path) ? path : FileSystem.GetDirectoryName(path);

            while (directory != null)
            {
                var dictionaryName = GuessDictionaryFileName(directory);
                if (dictionaryName != null)
                {
                    return dictionaryName;
                }

                directory = FileSystem.GetParentDirectory(directory);
            }

            return null;
        }
    }
}
