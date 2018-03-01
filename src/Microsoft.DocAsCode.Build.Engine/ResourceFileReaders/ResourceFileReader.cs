// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    public abstract class ResourceFileReader : IResourceFileReader, IDisposable
    {
        public abstract string Name { get; }

        public abstract bool IsEmpty { get; }

        public abstract IEnumerable<string> Names { get; }

        public virtual string GetResource(string name)
        {
            using (var stream = GetResourceStream(name))
                return GetString(stream);
        }

        public IEnumerable<ResourceInfo> GetResources(string selector = null)
        {
            foreach(var pair in GetResourceStreams(selector))
            {
                using (pair.Value)
                {
                    yield return new ResourceInfo(pair.Key, GetString(pair.Value));
                }
            }
        }

        public IEnumerable<KeyValuePair<string, Stream>> GetResourceStreams(string selector = null)
        {
            Func<string, bool> filter = s =>
            {
                if (selector != null)
                {
                    var regex = new Regex(selector, RegexOptions.IgnoreCase);
                    return regex.IsMatch(s);
                }
                else
                {
                    return true;
                }
            };
            foreach (var name in Names)
            {
                if (filter(name))
                {
                    yield return new KeyValuePair<string, Stream>(name, GetResourceStream(name));
                }
            }
        }

        public abstract Stream GetResourceStream(string name);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Override Object.Finalize by defining a destructor
        /// </summary>
        ~ResourceFileReader()
        {
            Dispose(false);
        }

        protected static string GetString(Stream stream)
        {
            if (stream == null) return null;

            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
