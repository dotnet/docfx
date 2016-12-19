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
            var folder = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(
                    EnvironmentContext.BaseDirectory ?? string.Empty));
            return from f in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                   select f.Substring(folder.Length + 1);
        }

        public IEnumerable<string> GetAllOutputFiles()
        {
            var folder = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(
                    EnvironmentContext.OutputDirectory ?? string.Empty));
            return from f in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                   select f.Substring(folder.Length + 1);
        }

        public bool Exists(string file) =>
            File.Exists(
                Path.Combine(
                    Environment.ExpandEnvironmentVariables(EnvironmentContext.BaseDirectory),
                    file));

        public Stream OpenRead(string file) =>
            File.OpenRead(
                Path.Combine(
                    Environment.ExpandEnvironmentVariables(EnvironmentContext.BaseDirectory),
                    file));

        public Stream Create(string file)
        {
            var f = Path.Combine(
                Environment.ExpandEnvironmentVariables(EnvironmentContext.OutputDirectory),
                file);
            Directory.CreateDirectory(Path.GetDirectoryName(f));
            return File.Create(f);
        }


        public void Copy(string sourceFileName, string destFileName) =>
            File.Copy(
                Path.Combine(
                    Environment.ExpandEnvironmentVariables(EnvironmentContext.BaseDirectory),
                    sourceFileName),
                Path.Combine(
                    Environment.ExpandEnvironmentVariables(EnvironmentContext.OutputDirectory),
                    destFileName));

        public ImmutableDictionary<string, string> GetProperties(string file) =>
            ImmutableDictionary<string, string>.Empty;

        public string GetPhysicalPath(string file) =>
            Path.Combine(
                Environment.ExpandEnvironmentVariables(EnvironmentContext.BaseDirectory),
                file);
    }
}