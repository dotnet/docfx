using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ECMA2Yaml.IO
{
    public class FileAccessor
    {
        internal FileAbstractLayer FallbackFileAbstractLayer { get; private set; }

        internal FileAbstractLayer RealFileAbstractLayer { get; private set; }

        internal FileAbstractLayer[] CombinedFileAbstractLayer { get; private set; }

        public FileAccessor(string realRootPath, string fallbackRootPath = null)
        {
            RealFileAbstractLayer = new FileAbstractLayer(realRootPath, false);
            if (!string.IsNullOrEmpty(fallbackRootPath) && Directory.Exists(fallbackRootPath))
            {
                FallbackFileAbstractLayer = new FileAbstractLayer(fallbackRootPath, true);
                CombinedFileAbstractLayer = new[] { RealFileAbstractLayer, FallbackFileAbstractLayer };
            }
            else
            {
                CombinedFileAbstractLayer = new[] { RealFileAbstractLayer };
            }
        }

        /// <summary>
        /// Return the first result and meet the requirement, real file abstract layer has higher priority
        /// </summary>
        public bool Exists(string relativePath)
        {
            return CombinedFileAbstractLayer.Select(i => i.Exists(relativePath)).Any(_ => _);
        }

        /// <summary>
        /// Return the first file stream which is not null and meet the requirement, real file abstract layer has higher priority
        /// </summary>
        public Stream OpenRead(string relativePath)
        {
            return CombinedFileAbstractLayer.Select(i => i.OpenRead(relativePath)).FirstOrDefault(_ => _ != null);
        }

        /// <summary>
        /// Alwasy write to real file abstract layer
        /// </summary>
        public Stream OpenWrite(string relativePath)
        {
            return RealFileAbstractLayer.OpenWrite(relativePath);
        }

        public string ReadAllText(string relativePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            if (!Exists(relativePath))
            {
                return null;
            }

            using (var stream = OpenRead(relativePath))
            {
                stream.Position = 0;
                using (var streamReader = new StreamReader(stream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Return the combined file lists, real file abstract layer has higher priority
        /// </summary>
        /// <param name="wildCardPattern"></param>
        /// <param name="subFolder"></param>
        /// <returns></returns>
        public IEnumerable<FileItem> ListFiles(string wildCardPattern, string subFolder, bool allDirectories = false)
        {
            if (wildCardPattern == null)
            {
                throw new ArgumentNullException(nameof(wildCardPattern));
            }

            var currentFiles = RealFileAbstractLayer.ListFiles(wildCardPattern, subFolder, allDirectories);

            if (FallbackFileAbstractLayer != null)
            {
                var currentFileFilePathSet = new HashSet<string>(currentFiles.Select(a => a.RelativePath));

                var fallbackFiles = FallbackFileAbstractLayer.ListFiles(wildCardPattern, subFolder, allDirectories).Where(i => !currentFileFilePathSet.Contains(i.RelativePath));

                return currentFiles.Concat(fallbackFiles).ToList();
            }
            else
            {
                return currentFiles;
            }
        }
    }
}
