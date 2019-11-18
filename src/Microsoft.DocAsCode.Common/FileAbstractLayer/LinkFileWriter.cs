// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class LinkFileWriter : FileWriterBase
    {
        private readonly Dictionary<RelativePath, PathMapping> _mapping =
            new Dictionary<RelativePath, PathMapping>();

        public LinkFileWriter(string outputFolder)
            : base(outputFolder) { }

        #region Overrides

        public override void Copy(PathMapping sourceFilePath, RelativePath destFilePath)
        {
            var key = destFilePath.GetPathFromWorkingFolder();
            var pm = new PathMapping(key, sourceFilePath.PhysicalPath)
            {
                Properties = sourceFilePath.Properties,
            };
            lock (_mapping)
            {
                _mapping[key] = pm;
            }
        }

        public override Stream Create(RelativePath file)
        {
            var key = file.GetPathFromWorkingFolder();
            bool getResult;
            PathMapping pm;
            lock (_mapping)
            {
                getResult = _mapping.TryGetValue(key, out pm);
            }
            if (getResult && pm.PhysicalPath.StartsWith(OutputFolder, StringComparison.Ordinal))
            {
                try
                {
                    return File.Create(Environment.ExpandEnvironmentVariables(pm.PhysicalPath));
                }
                catch (IOException)
                {
                }
            }
            var pair = CreateRandomFileStream();
            pm = new PathMapping(key, Path.Combine(OutputFolder, pair.Item1));
            lock (_mapping)
            {
                _mapping[key] = pm;
            }
            return pair.Item2;
        }

        public override IFileReader CreateReader()
        {
            lock (_mapping)
            {
                return new IndexedLinkFileReader(_mapping);
            }
        }

        #endregion
    }
}
