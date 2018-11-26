// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    public sealed class LocalFileResourceReader : ResourceFileReader
    {
        private const int MaxSearchLevel = 5;
        // keep comparer to be case sensitive as to be consistent with zip entries
        private static StringComparer ResourceComparer = StringComparer.Ordinal;
        private string _directory = null;
        private readonly int _maxDepth;

        public override string Name { get; }
        public override IEnumerable<string> Names { get; }
        public override bool IsEmpty { get; }

        public LocalFileResourceReader(string directory, int maxSearchLevel = MaxSearchLevel)
        {
            if (string.IsNullOrEmpty(directory)) _directory = Directory.GetCurrentDirectory();
            else _directory = directory;
            Name = _directory;
            _maxDepth = maxSearchLevel;
            var includedFiles = GetFiles(_directory, "*", maxSearchLevel);
            Names = includedFiles.Select(s => PathUtility.MakeRelativePath(_directory, s)).Where(s => s != null);

            IsEmpty = !Names.Any();
        }

        public override Stream GetResourceStream(string name)
        {
            if (IsEmpty) return null;

            // incase relative path is combined by backslash \
            if (!Names.Contains(StringExtension.ToNormalizedPath(name.Trim()), ResourceComparer)) return null;
            var filePath = Path.Combine(_directory, name);
            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }

        private IEnumerable<string> GetFiles(string directory, string searchPattern, int searchLevel)
        {
            if (searchLevel < 1)
            {
                return Enumerable.Empty<string>();
            }
            var files = Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
            var dirs = Directory.GetDirectories(directory);
            if (searchLevel == 1)
            {
                if (dirs.Length > 0)
                {
                    var dirPaths = (from dir in dirs select PathUtility.MakeRelativePath(_directory, dir)).ToDelimitedString();
                    Logger.LogInfo($"The following directories exceed max allowed depth {_maxDepth}, ignored: {dirPaths}.");
                }

                return files;
            }
            List<string> allFiles = new List<string>(files);
            foreach (var dir in dirs)
            {
                allFiles.AddRange(GetFiles(dir, searchPattern, searchLevel - 1));
            }
            return allFiles;
        }
    }
}
