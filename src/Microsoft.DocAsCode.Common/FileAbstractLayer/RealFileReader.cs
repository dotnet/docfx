// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    public class RealFileReader : IFileReader
    {
        public RealFileReader(string inputFolder, ImmutableDictionary<string, string> properties)
        {
            if (inputFolder == null)
            {
                throw new ArgumentNullException(nameof(inputFolder));
            }
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"Directory ({inputFolder}) not found.");
            }
            if (inputFolder.Length > 0 &&
                !inputFolder.EndsWith("\\") &&
                !inputFolder.EndsWith("/"))
            {
                inputFolder += "/";
            }
            InputFolder = inputFolder;
            Properties = properties;
        }

        public string InputFolder { get; }

        public ImmutableDictionary<string, string> Properties { get; }

        #region IFileReader Members

        public PathMapping? FindFile(RelativePath file)
        {
            var pp = Path.Combine(InputFolder, file.RemoveWorkingFolder());
            if (!File.Exists(pp))
            {
                return null;
            }
            return new PathMapping(file, pp) { Properties = Properties };
        }

        public IEnumerable<RelativePath> EnumerateFiles()
        {
            var length = InputFolder.Length;
            return from f in Directory.EnumerateFiles(InputFolder, "*.*", SearchOption.AllDirectories)
                   select ((RelativePath)f.Substring(length)).GetPathFromWorkingFolder();
        }

        #endregion
    }
}
