using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ECMA2Yaml.IO
{
    public class FileAbstractLayer
    {
        public string RootPath { get; private set; }
        public bool IsVirtual { get; private set; }

        public FileAbstractLayer(string rootPath, bool isVirtual)
        {
            RootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
            IsVirtual = isVirtual;

            RootPath = Path.GetFullPath(RootPath);
            if (!Directory.Exists(RootPath))
            {
                throw new DirectoryNotFoundException(RootPath);
            }
        }

        public virtual bool Exists(string relativePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(relativePath);
            }

            var filePath = Path.Combine(RootPath, relativePath);
            if (!File.Exists(filePath))
            {
                return false;
            }

            return true;
        }

        public virtual IEnumerable<FileItem> ListFiles(string wildCardPattern, string subFolder, bool allDirectories = false)
        {
            var directoryPath = Path.Combine(RootPath, subFolder ?? string.Empty);
            if (!Directory.Exists(directoryPath))
            {
                return Array.Empty<FileItem>();
            }
            var allFiles = Directory.EnumerateFiles(
                Path.Combine(RootPath, subFolder ?? string.Empty),
                wildCardPattern,
                allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            var filteredFiles = allFiles.Select(f => new FileItem { RelativePath = RelativePath(f, RootPath, false), AbsolutePath = f, IsVirtual = IsVirtual });
            return filteredFiles;
        }

        public virtual Stream OpenRead(string relativePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            if (!Exists(relativePath))
            {
                return null;
            }

            var filePath = Path.Combine(RootPath, relativePath);
            return File.OpenRead(filePath);
        }

        public virtual Stream OpenWrite(string relativePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            var filePath = Path.Combine(RootPath, relativePath);
            var directoryName = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            return File.OpenWrite(filePath);
        }

        public static string RelativePath(string path, string relativeTo, bool relativeToFile)
        {
            path = Path.GetFullPath(path);
            relativeTo = Path.GetFullPath(relativeTo);

            if (path.StartsWith(relativeTo))
            {
                return path.Remove(0, relativeTo.Length).TrimStart(new[] { '\\', '/' });
            }
            else
            {
                if (!relativeToFile && !relativeTo.EndsWith("\\"))
                {
                    relativeTo += "\\";
                }
                Uri baseUri = new Uri(relativeTo);
                Uri fullUri = new Uri(path);
                Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
                // Uri's use forward slashes so convert back to backward slashes
                return relativeUri.ToString().Replace("/", "\\").Replace("%60", "`");
            }
        }
    }
}
