// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    public class LinkFileReader : IFileReader
    {
        public LinkFileReader(IEnumerable<PathMapping> mappings)
        {
            Mappings = mappings.ToImmutableArray();
        }

        public ImmutableArray<PathMapping> Mappings { get; }

        #region IFileReader Members

        public PathMapping? FindFile(RelativePath file)
        {
            var path = file.GetPathFromWorkingFolder();
            foreach (var m in Mappings)
            {
                if (m.IsFolder)
                {
                    var localPath = path - m.LogicalPath;
                    if (m.AllowMoveOut || localPath.ParentDirectoryCount == 0)
                    {
                        var physicalPath = Path.Combine(m.PhysicalPath, localPath.ToString());
                        if (File.Exists(Environment.ExpandEnvironmentVariables(physicalPath)))
                        {
                            return new PathMapping(path, physicalPath) { Properties = m.Properties };
                        }
                    }
                }
                else if (m.LogicalPath == path)
                {
                    return m;
                }
            }
            return null;
        }

        public IEnumerable<RelativePath> EnumerateFiles()
        {
            var set = new HashSet<RelativePath>();
            foreach (var m in Mappings)
            {
                if (m.IsFolder)
                {
                    var fp = Path.GetFullPath(Environment.ExpandEnvironmentVariables(m.PhysicalPath));
                    foreach (var f in Directory.EnumerateFiles(fp, "*.*", SearchOption.AllDirectories))
                    {
                        var lf = f.Substring(fp.Length + 1);
                        var rp = m.LogicalPath + (RelativePath)lf;
                        set.Add(rp);
                    }
                }
                else
                {
                    set.Add(m.LogicalPath);
                }
            }
            return set;
        }

        public IEnumerable<string> GetExpectedPhysicalPath(RelativePath file)
        {
            var path = file.GetPathFromWorkingFolder();
            foreach (var m in Mappings)
            {
                if (m.IsFolder)
                {
                    var localPath = path - m.LogicalPath;
                    if (m.AllowMoveOut || localPath.ParentDirectoryCount == 0)
                    {
                        var physicalPath = Path.Combine(m.PhysicalPath, localPath.ToString());
                        if (File.Exists(Environment.ExpandEnvironmentVariables(physicalPath)))
                        {
                            yield return physicalPath;
                        }
                    }
                }
                else if (m.LogicalPath == path)
                {
                    yield return m.PhysicalPath;
                }
            }
        }

        #endregion
    }
}
