// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    public class RootedFileAbstractLayer : IFileAbstractLayer
    {
        private readonly IFileAbstractLayer _impl;

        public RootedFileAbstractLayer(IFileAbstractLayer impl)
        {
            _impl = impl;
        }

        public bool CanRead => true;

        public bool CanWrite => true;

        public IEnumerable<string> GetAllInputFiles() => _impl.GetAllInputFiles();

        public bool Exists(string file) =>
            Path.IsPathRooted(file) ? File.Exists(file) : _impl.Exists(file);

        public Stream OpenRead(string file) =>
            Path.IsPathRooted(file) ? File.OpenRead(file) : _impl.OpenRead(file);

        public Stream Create(string file)
        {
            if (Path.IsPathRooted(file))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                return File.Create(file);
            }
            return _impl.Create(file);
        }

        public void Copy(string sourceFileName, string destFileName)
        {
            if (Path.IsPathRooted(sourceFileName) || Path.IsPathRooted(destFileName))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
                File.Copy(sourceFileName, destFileName, true);
                File.SetAttributes(destFileName, FileAttributes.Normal);
            }
            else
            {
                _impl.Copy(sourceFileName, destFileName);
            }
        }

        public ImmutableDictionary<string, string> GetProperties(string file) =>
            Path.IsPathRooted(file) ? ImmutableDictionary<string, string>.Empty : _impl.GetProperties(file);

        public string GetPhysicalPath(string file) =>
            Path.IsPathRooted(file) ? file : _impl.GetPhysicalPath(file);
    }
}