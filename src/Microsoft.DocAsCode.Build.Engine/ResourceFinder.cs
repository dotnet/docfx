// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.DocAsCode.Common;

    [Serializable]
    public class ResourceFinder
    {
        private IEnumerable<string> _embeddedResourceNames;
        private string _resourcePrefix;
        private Assembly _assembly;
        private static Func<string, string, string, bool> resourceNamePredicator = (resourceName, name, prefix) => Path.GetFileNameWithoutExtension(resourceName).Substring(prefix.Length).Equals(name, StringComparison.OrdinalIgnoreCase);
        private string _baseDirectory;

        public ResourceFinder(Assembly assembly, string rootNamespace, string baseDirectory = null)
        {
            _baseDirectory = baseDirectory ?? Directory.GetCurrentDirectory();
            _assembly = assembly;
            if (assembly != null)
            {
                _resourcePrefix = string.Format("{0}.{1}.", assembly.GetName().Name, rootNamespace);
                _embeddedResourceNames = assembly.GetManifestResourceNames();
            }
            else
            {
                _resourcePrefix = string.Empty;
                _embeddedResourceNames = new List<string>();
            }
        }

        /// <summary>
        /// Search in order:
        /// 1. Inside Embedded Resources
        ///     a. ZIP file with provided name
        /// 2. Inside OverrideFolder, *NOTE* sub-folders are **NOT** included
        ///     a. Folder with provided name
        ///     b. ZIP file with provided name
        /// </summary>
        /// <param name="name">The resource name provided</param>
        /// <returns>If found, return the resource collection; if not, return null</returns>
        public ResourceFileReader Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var resourceName = _embeddedResourceNames.FirstOrDefault(s => resourceNamePredicator(s, name, _resourcePrefix));
            if (resourceName != null)
            {
                return new ArchiveResourceReader(_assembly.GetManifestResourceStream(resourceName),
                    $"embedded resource {resourceName}");
            }

            var directory = Path.Combine(_baseDirectory, name);
            if (Directory.Exists(directory))
            {
                return new LocalFileResourceReader(directory);
            }

            var fileName = Path.Combine(_baseDirectory, $"{name}.zip");
            if (File.Exists(fileName))
            {
                return new ArchiveResourceReader(new FileStream(fileName, FileMode.Open, FileAccess.Read), fileName);
            }

            return null;
        }
    }
}
