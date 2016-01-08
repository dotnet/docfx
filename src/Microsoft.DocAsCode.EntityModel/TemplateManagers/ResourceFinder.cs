// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.DocAsCode.Common;

    public class ResourceFinder
    {
        private IEnumerable<string> _embeddedResourceNames;
        private string _resourcePrefix;
        private Assembly _assembly;
        private static Func<string, string, string, bool> resourceNamePredicator = (resourceName, name, prefix) => Path.GetFileNameWithoutExtension(resourceName).Substring(prefix.Length).Equals(name, StringComparison.OrdinalIgnoreCase);
        private string _baseDirectory;

        public ResourceFinder(Assembly assembly, string rootNamespace, string baseDirectory = null)
        {
            _baseDirectory = baseDirectory ?? Environment.CurrentDirectory;
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
        /// 1. Inside OverrideFolder, *NOTE* sub-folders are **NOT** included
        ///     a. Folder with provided name
        ///     b. ZIP file with provided name
        /// 2. Inside Embedded Resources
        ///     a. ZIP file with provided name
        /// </summary>
        /// <param name="name">The resource name provided</param>
        /// <returns>If found, return the resource collection; if not, return null</returns>
        public ResourceCollection Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var directory = Path.Combine(_baseDirectory, name);
            if (Directory.Exists(directory))
            {
                Logger.LogVerbose($"Resource {name} is found from {directory}.");
                return new FileResourceCollection(directory);
            }

            var fileName = Path.Combine(_baseDirectory, $"{name}.zip");
            if (File.Exists(fileName))
            {
                Logger.LogVerbose($"Resource {name} is found from {fileName}.");
                return new ArchiveResourceCollection(new FileStream(fileName, FileMode.Open, FileAccess.Read), fileName);
            }

            var resourceName = _embeddedResourceNames.FirstOrDefault(s => resourceNamePredicator(s, name, _resourcePrefix));
            if (resourceName == null)
            {
                Logger.LogWarning($"Unable to find matching resource {name}.");
                return null;
            }
            else
            {
                Logger.LogVerbose($"Resource {name} is found in embedded resources.");
                return new ArchiveResourceCollection(_assembly.GetManifestResourceStream(resourceName), $"embedded resource {resourceName}");
            }
        }
    }
}
