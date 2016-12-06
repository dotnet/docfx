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
            _mapping[key] = new PathMapping(key, sourceFilePath.PhysicalPath)
            {
                Properties = sourceFilePath.Properties,
            };
        }

        public override FileStream Create(RelativePath file)
        {
            var key = file.GetPathFromWorkingFolder();
            PathMapping pm;
            if (_mapping.TryGetValue(key, out pm) &&
                pm.PhysicalPath.StartsWith(OutputFolder))
            {
                return File.Create(Environment.ExpandEnvironmentVariables(pm.PhysicalPath));
            }
            var pair = CreateRandomFileStream();
            _mapping[key] =
                new PathMapping(
                    key,
                    Path.Combine(OutputFolder, pair.Item1));
            return pair.Item2;
        }

        public override IFileReader CreateReader()
        {
            return new LinkFileReader(_mapping.Values);
        }

        #endregion
    }
}
