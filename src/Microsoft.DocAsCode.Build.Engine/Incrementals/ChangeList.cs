// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Utility;

    public sealed class ChangeList
    {
        private readonly List<ChangeItem> _list = new List<ChangeItem>();

        public static ChangeList Parse(string tsvFile, string baseDir)
        {
            if (tsvFile == null)
            {
                throw new ArgumentNullException(nameof(tsvFile));
            }
            if (baseDir == null)
            {
                throw new ArgumentNullException(nameof(baseDir));
            }
            if (!File.Exists(tsvFile))
            {
                throw new FileNotFoundException("File not found.", tsvFile);
            }
            return ParseCore(tsvFile, baseDir);
        }

        public void Add(string filePath, ChangeKind kind)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            if (filePath.Length == 0)
            {
                throw new ArgumentException("File path cannot be empty", nameof(filePath));
            }
            if (!PathUtility.IsRelativePath(filePath))
            {
                throw new ArgumentException("Expect relative path.", nameof(filePath));
            }
            AddCore(filePath, kind);
        }

        public IEnumerable<string> GetCreatedFiles() =>
            from item in _list
            where item.Kind == ChangeKind.Created
            select item.FilePath;

        public IEnumerable<string> GetUpdatedFiles() =>
            from item in _list
            where item.Kind == ChangeKind.Updated
            select item.FilePath;

        public IEnumerable<string> GetDeletedFiles() =>
            from item in _list
            where item.Kind == ChangeKind.Deleted
            select item.FilePath;

        private void AddCore(string filePath, ChangeKind kind)
        {
            _list.Add(new ChangeItem { FilePath = filePath, Kind = kind });
        }

        private static ChangeList ParseCore(string tsvFile, string baseDir)
        {
            var result = new ChangeList();
            bool hasError = false;

            foreach (var line in File.ReadLines(tsvFile))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                var columns = line.Split('\t');
                if (columns.Length >= 2)
                {
                    string path;
                    if (PathUtility.IsRelativePath(columns[0]))
                    {
                        path = columns[0];
                    }
                    else
                    {
                        path = PathUtility.MakeRelativePath(baseDir, columns[0]);
                    }
                    if (path != null)
                    {
                        ChangeKind kind;
                        if (Enum.TryParse(columns[1], true, out kind))
                        {
                            if (kind != ChangeKind.Deleted)
                            {
                                if (!File.Exists(Path.Combine(baseDir, path)))
                                {
                                    Logger.LogError($"File:{path} not existed.");
                                    hasError = true;
                                    continue;
                                }
                            }
                            result.Add(path, kind);
                            continue;
                        }
                    }
                }
                Logger.LogWarning($"Ignore unknown line: {line}");
            }
            if (hasError)
            {
                throw new DocfxException($"Some error ocurred while parsing changelist file: {tsvFile}.");
            }
            return result;
        }

        private sealed class ChangeItem
        {
            public string FilePath { get; set; }
            public ChangeKind Kind { get; set; }
        }
    }
}
