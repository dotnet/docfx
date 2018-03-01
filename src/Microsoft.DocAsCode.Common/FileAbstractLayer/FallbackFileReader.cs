// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    internal sealed class FallbackFileReader : IFileReader
    {
        public FallbackFileReader(ImmutableArray<IFileReader> readers)
        {
            Readers = readers.ToImmutableArray();
        }

        public ImmutableArray<IFileReader> Readers { get; }

        #region IFileReader Members

        public PathMapping? FindFile(RelativePath file)
        {
            bool isFallback = false;
            foreach (var pm in from r in Readers
                               select r.FindFile(file))
            {
                if (pm != null)
                {
                    if (isFallback)
                    {
                        Logger.LogVerbose($"Load file from fallback: {pm.Value.PhysicalPath}.", file: file);
                    }
                    return pm;
                }
                isFallback = true;
            }
            return null;
        }

        public IEnumerable<RelativePath> EnumerateFiles() =>
            (from r in Readers
             from f in r.EnumerateFiles()
             select f).Distinct();

        public IEnumerable<string> GetExpectedPhysicalPath(RelativePath file) =>
            from r in Readers
            from f in r.GetExpectedPhysicalPath(file)
            select f;

        #endregion
    }
}
