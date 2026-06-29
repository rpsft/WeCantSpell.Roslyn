using System.Collections.Generic;
using System.IO;
using WeCantSpell.Roslyn.Config;

namespace WeCantSpell.Roslyn.Infrastructure
{
    public sealed class FileSystem : IFileSystem
    {
        public string FileSystemType => "System.IO";

        public string? GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public string? GetParentDirectory(string path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);

            DirectoryInfo? parentDirectoryInfo = directoryInfo.Parent;

            return parentDirectoryInfo?.FullName;
        }

        public string? ReadAllText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public string CombinePath(string directoryName, string fileName)
        {
            return Path.Combine(directoryName, fileName);
        }

        public bool FileExists(string tryFilePath)
        {
            return File.Exists(tryFilePath);
        }

        public Stream ReadStream(string path)
        {
            return new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.SequentialScan
            );
        }

        public string ChangeExtension(string path, string extension)
        {
            return Path.ChangeExtension(path, extension);
        }

        public IEnumerable<string> GetFiles(string directory, string pattern, SearchOption searchOption)
        {
            return Directory.GetFiles(directory, pattern, searchOption);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public StreamWriter AppendText(string path)
        {
            return File.AppendText(path);
        }
    }
}
