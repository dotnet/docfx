// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Immutable;
    using System.IO;

    public class RealFileWriter : FileWriterBase, ISupportRandomFileWriter
    {
        public RealFileWriter(string outputFolder)
            : base(outputFolder) { }

        #region Overrides

        public override void Copy(PathMapping sourceFileName, RelativePath destFileName)
        {
            var dest = Path.Combine(ExpandedOutputFolder, destFileName.RemoveWorkingFolder());
            EnsureFolder(Path.GetDirectoryName(dest));
            var source = Environment.ExpandEnvironmentVariables(sourceFileName.PhysicalPath);
            if (!FilePathComparer.OSPlatformSensitiveStringComparer.Equals(source, dest))
            {
                File.Copy(source, dest, true);
            }
            File.SetAttributes(dest, FileAttributes.Normal);
        }

        public override Stream Create(RelativePath file)
        {
            var f = Path.Combine(ExpandedOutputFolder, file.RemoveWorkingFolder());
            EnsureFolder(Path.GetDirectoryName(f));
            return File.Create(f);
        }

        public override IFileReader CreateReader()
        {
            return new RealFileReader(OutputFolder, ImmutableDictionary<string, string>.Empty);
        }

        #endregion

        #region ISupportRandomFileWriter Members

        public string CreateRandomFileName()
        {
            var tuple = CreateRandomFileStream();
            tuple.Item2.Close();
            return tuple.Item1;
        }

        public Tuple<string, Stream> CreateRandomFile()
        {
            var tuple = CreateRandomFileStream();
            return Tuple.Create(tuple.Item1, (Stream)tuple.Item2);
        }

        #endregion
    }
}
