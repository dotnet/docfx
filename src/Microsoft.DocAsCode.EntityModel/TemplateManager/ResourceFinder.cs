// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.IO.Compression;
    
    public class ResourceFinder
    {
        private IEnumerable<string> _embeddedResourceNames;
        private string _overrideFolder = null;
        private string _resourcePrefix;
        private Assembly _assembly;
        private static Func<string, string, string, bool> resourceNamePredicator = (resourceName, name, prefix) => Path.GetFileNameWithoutExtension(resourceName).Substring(prefix.Length).Equals(name, StringComparison.OrdinalIgnoreCase);

        public ResourceFinder(Assembly assembly, string rootNamespace)
        {
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

        public ResourceFinder(string folder) : this(null, null, folder) { }

        public ResourceFinder(Assembly assembly, string rootNamespace, string overrideFolder) : this(assembly, rootNamespace)
        {
            if (string.IsNullOrEmpty(overrideFolder))
            {
                _overrideFolder = Environment.CurrentDirectory;
            }
            else
            {
                if (!Directory.Exists(overrideFolder))
                {
                    ParseResult.WriteToConsole(ResultLevel.Warning, "Folder {0} does not exist", overrideFolder);
                }
                else
                {
                    _overrideFolder = overrideFolder;
                }
            }
        }

        /// <summary>
        /// Search in order:
        /// 1. Inside OverrideFolder, *NOTE* sub-folders are **NOT** included
        ///     a. ZIP file with provided name
        ///     b. Folder with provided name
        /// 2. Inside Embedded Resources
        ///     a. ZIP file with provided name
        /// </summary>
        /// <param name="name">The resource name provided</param>
        /// <returns>If found, return the resource collection; if not, return null</returns>
        public ResourceCollection Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (_overrideFolder != null)
            {
                var fileName = Path.Combine(_overrideFolder, $"{name}.zip");
                if (File.Exists(fileName))
                {
                    ParseResult.WriteToConsole(ResultLevel.Success, "Resource {0} is found in {1}.", name, _overrideFolder);
                    return new ArchiveResourceCollection(new FileStream(fileName, FileMode.Open, FileAccess.Read));
                }
                else
                {
                    var directory = Path.Combine(_overrideFolder, name);
                    if (Directory.Exists(directory))
                    {
                        return new FileResourceCollection(directory);
                    }
                }
            }

            var resourceName = _embeddedResourceNames.FirstOrDefault(s => resourceNamePredicator(s, name, _resourcePrefix));
            if (resourceName == null)
            {
                ParseResult.WriteToConsole(ResultLevel.Warning, "Unable to find matching resource {0}.", name);
                return null;
            }
            else
            {
                ParseResult.WriteToConsole(ResultLevel.Success, "Resource {0} is found in embedded resources.", name);
                return new ArchiveResourceCollection(_assembly.GetManifestResourceStream(resourceName));
            }
        }
    }
}
