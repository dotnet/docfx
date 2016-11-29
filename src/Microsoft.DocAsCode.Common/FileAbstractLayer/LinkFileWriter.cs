// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.Collections.Generic;
    using System.IO;

    public class LinkFileWriter : FileWriterBase
    {
        private readonly List<PathMapping> _mapping = new List<PathMapping>();

        public LinkFileWriter(string outputFolder)
            : base(outputFolder) { }

        #region Overrides

        public override void Copy(PathMapping sourceFilePath, RelativePath destFilePath)
        {
            _mapping.Add(
                new PathMapping(
                    destFilePath.GetPathFromWorkingFolder(),
                    sourceFilePath.PhysicalPath)
                {
                    Properties = sourceFilePath.Properties,
                });
        }

        public override FileStream Create(RelativePath file)
        {
            var pair = CreateRandomFileStream();
            _mapping.Add(
                new PathMapping(
                    file.GetPathFromWorkingFolder(),
                    Path.Combine(OutputFolder, pair.Item1)));
            return pair.Item2;
        }

        public override IFileReader CreateReader()
        {
            return new LinkFileReader(_mapping);
        }

        #endregion
    }
}
