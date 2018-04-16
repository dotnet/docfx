// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.Docs.Build
{
    internal class TestFileSystem : IFileSystem
    {
        private readonly BuildTestSpec _spec;
        private readonly ConcurrentDictionary<string, MemoryStream> _actualOutputs = new ConcurrentDictionary<string, MemoryStream>(StringComparer.OrdinalIgnoreCase);

        public TestFileSystem(BuildTestSpec spec) => _spec = spec;

        public bool Exists(string relativePath, string docsetPath)
        {
            Debug.Assert(!Path.IsPathRooted(relativePath));

            return _spec.Inputs.ContainsKey(relativePath);
        }

        public Stream Read(string relativePath, string docsetPath)
        {
            Debug.Assert(!Path.IsPathRooted(relativePath));

            return _spec.Inputs.TryGetValue(relativePath, out var text)
                ? new MemoryStream(Encoding.UTF8.GetBytes(text))
                : throw new FileNotFoundException($"Cannot find '{relativePath}' in docset '{docsetPath}'");
        }

        public Stream Write(string relativePath, string outputPath)
        {
            Debug.Assert(!Path.IsPathRooted(relativePath));

            return _actualOutputs.GetOrAdd(relativePath, _ => new MemoryStream());
        }

        public void Copy(string sourceRelativePath, string destRelativePath, string docsetPath, string outputPath)
        {
            Read(sourceRelativePath, docsetPath).CopyTo(Write(destRelativePath, outputPath));
        }
    }
}
