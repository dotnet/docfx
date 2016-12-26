// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Immutable;
    using System.IO;

    public class RealFileWriter : FileWriterBase
    {
        public RealFileWriter(string outputFolder)
            : base(outputFolder) { }

        #region Overrides

        public override void Copy(PathMapping sourceFileName, RelativePath destFileName)
        {
            var f = Path.Combine(ExpandedOutputFolder, destFileName.RemoveWorkingFolder());
            Directory.CreateDirectory(Path.GetDirectoryName(f));
            File.Copy(Environment.ExpandEnvironmentVariables(sourceFileName.PhysicalPath), f, true);
            File.SetAttributes(f, FileAttributes.Normal);
        }

        public override Stream Create(RelativePath file)
        {
            var f = Path.Combine(ExpandedOutputFolder, file.RemoveWorkingFolder());
            Directory.CreateDirectory(Path.GetDirectoryName(f));
            return File.Create(f);
        }

        public override IFileReader CreateReader()
        {
            return new RealFileReader(OutputFolder, ImmutableDictionary<string, string>.Empty);
        }

        #endregion
    }
}
