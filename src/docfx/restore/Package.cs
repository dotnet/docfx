// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal abstract class Package
    {
        public abstract PathString BasePath { get; }

        public virtual Package CreateSubPackage(string relativePath) => new DirectoryPackage(this, new PathString(relativePath));

        public abstract bool Exists(PathString path);

        public abstract IEnumerable<PathString> GetFiles(string directory = ".", Func<string, bool>? fileNamePredicate = null);

        public abstract PathString GetFullFilePath(PathString path);

        public abstract DateTime? TryGetLastWriteTimeUtc(PathString path);

        public abstract Stream ReadStream(PathString path);

        public string ReadString(PathString path)
        {
            using var reader = ReadText(path);
            return reader.ReadToEnd();
        }

        public TextReader ReadText(PathString path)
        {
            return new StreamReader(ReadStream(path));
        }

        public PathString? TryGetFullFilePath(PathString path)
        {
            var fullPath = GetFullFilePath(path);
            if (Exists(fullPath))
            {
                return fullPath;
            }
            return null;
        }

        // TODO: Retire this method after abstracting git read operations in Package.
        public virtual PathString? TryGetGitFilePath(PathString path) => null;

        public abstract PathString? TryGetPhysicalPath(PathString path);

        public string? TryReadString(PathString path)
        {
            if (!Exists(path))
            {
                return default;
            }

            using var reader = ReadText(path);
            return reader.ReadToEnd();
        }

        public T? TryReadYamlOrJson<T>(ErrorBuilder errors, string pathWithoutExtension) where T : class, new()
        {
            var path = new PathString(pathWithoutExtension + ".yml");
            var content = TryReadString(path);
            if (content != null)
            {
                return YamlUtility.Deserialize<T>(errors, content, new FilePath(path));
            }

            path = new PathString(pathWithoutExtension + ".json");
            content = TryReadString(path);
            if (content != null)
            {
                return JsonUtility.Deserialize<T>(errors, content, new FilePath(path));
            }

            return null;
        }
    }
}
