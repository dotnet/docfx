// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;

    public sealed class ChangeList : IEnumerable<ChangeItem>
    {
        private readonly List<ChangeItem> _list = new List<ChangeItem>();

        public string From { get; set; }

        public string To { get; set; }

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
            where item.Kind == ChangeKindWithDependency.Created
            select item.FilePath;

        public IEnumerable<string> GetUpdatedFiles() =>
            from item in _list
            where item.Kind == ChangeKindWithDependency.Updated
            select item.FilePath;

        public IEnumerable<string> GetDeletedFiles() =>
            from item in _list
            where item.Kind == ChangeKindWithDependency.Deleted
            select item.FilePath;

        private void AddCore(string filePath, ChangeKind kind)
        {
            _list.Add(
                new ChangeItem
                {
                    FilePath = filePath,
                    Kind = (ChangeKindWithDependency)kind
                });
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
                    if (string.Equals(columns[0], "<from>", StringComparison.OrdinalIgnoreCase))
                    {
                        result.From = columns[1];
                        continue;
                    }
                    if (string.Equals(columns[0], "<to>", StringComparison.OrdinalIgnoreCase))
                    {
                        result.To = columns[1];
                        continue;
                    }
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
                        if (Enum.TryParse(columns[1], true, out ChangeKind kind))
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public IEnumerator<ChangeItem> GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
