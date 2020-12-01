// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Docs.Build
{
    internal class InMemoryPackage : Package
    {
        private readonly IReadOnlyDictionary<PathString, string> _files;

        public InMemoryPackage(IEnumerable<(string fileName, string content)> files)
        {
            _files = files.ToDictionary(file => new PathString(file.fileName), file => file.content);
        }

        public override bool Exists(PathString path) => _files.ContainsKey(path);

        public override IEnumerable<PathString> GetFiles() => _files.Keys;

        public override string ReadString(PathString path) => _files[path];

        public override string TryReadString(PathString path) => _files.TryGetValue(path, out var value) ? value : null;

        public override Stream ReadStream(PathString path) => new MemoryStream(Encoding.UTF8.GetBytes(_files[path]));

        public override PathString? TryGetPhysicalPath(PathString path) => null;

        public override PathString? TryGetFullFilePath(PathString path) => null;
    }
}
