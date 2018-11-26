// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    public class DefaultFileAbstractLayer : IFileAbstractLayer
    {
        public bool CanRead => true;

        public bool CanWrite => true;

        public IEnumerable<string> GetAllInputFiles()
        {
            var folder = Path.GetFullPath(GetPhysicalPath("."));
            return from f in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                   select f.Substring(folder.Length + 1);
        }

        public bool Exists(string file) =>
                File.Exists(GetPhysicalPath(file));

        public Stream OpenRead(string file) =>
            File.OpenRead(GetPhysicalPath(file));

        public Stream Create(string file)
        {
            var f = GetOutputPhysicalPath(file);
            var dir = Path.GetDirectoryName(f);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return File.Create(f);
        }

        public void Copy(string sourceFileName, string destFileName)
        {
            var source = GetPhysicalPath(sourceFileName);
            var dest = GetOutputPhysicalPath(destFileName);
            var dir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.Copy(source, dest, true);
            File.SetAttributes(dest, FileAttributes.Normal);
        }

        public ImmutableDictionary<string, string> GetProperties(string file) =>
            ImmutableDictionary<string, string>.Empty;

        public string GetPhysicalPath(string file) =>
            Path.Combine(
                Environment.ExpandEnvironmentVariables(EnvironmentContext.BaseDirectory),
                file);

        public string GetOutputPhysicalPath(string file) =>
            Path.Combine(
                Environment.ExpandEnvironmentVariables(EnvironmentContext.OutputDirectory),
                file);

        public IEnumerable<string> GetExpectedPhysicalPath(string file) =>
            new[] { Path.Combine(EnvironmentContext.OutputDirectory, file) };
    }
}