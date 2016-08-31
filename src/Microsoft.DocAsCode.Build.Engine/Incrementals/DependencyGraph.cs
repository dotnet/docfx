// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    public class DependencyGraph
    {
        private readonly Dictionary<string, Dependency> _dictionary;

        public DependencyGraph()
            : this(new Dictionary<string, Dependency>())
        {
        }

        private DependencyGraph(Dictionary<string, Dependency> dictionary)
        {
            _dictionary = dictionary;
        }

        public void ReportFileDependency(string key, string value)
        {
            Dependency d;
            if (!_dictionary.TryGetValue(key, out d))
            {
                d = new Dependency();
                _dictionary[key] = d;
            }

            d.Files.Add(value);
        }

        public void ReportFileDependency(string key, IEnumerable<string> values)
        {
            Dependency d;
            if (!_dictionary.TryGetValue(key, out d))
            {
                d = new Dependency();
                _dictionary[key] = d;
            }

            d.Files.UnionWith(values);
        }

        public void ReportUidDependency(string key, string value)
        {
            Dependency d;
            if (!_dictionary.TryGetValue(key, out d))
            {
                d = new Dependency();
                _dictionary[key] = d;
            }

            d.Uids.Add(value);
        }

        public void ReportUidDependency(string key, IEnumerable<string> values)
        {
            Dependency d;
            if (!_dictionary.TryGetValue(key, out d))
            {
                d = new Dependency();
                _dictionary[key] = d;
            }

            d.Uids.UnionWith(values);
        }

        public bool HasDependency(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public IEnumerable<string> Keys
        {
            get { return _dictionary.Keys; }
        }

        public SortedSet<string> GetUidDependency(string key)
        {
            Dependency d;
            _dictionary.TryGetValue(key, out d);
            return d?.Uids;
        }

        public SortedSet<string> GetDirectFileDependency(string key)
        {
            Dependency d;
            _dictionary.TryGetValue(key, out d);
            return d?.Files;
        }

        public SortedSet<string> GetAllFileDependency(string key)
        {
            var result = new SortedSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(key);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                Dependency d;
                if (_dictionary.TryGetValue(current, out d))
                {
                    foreach (var item in d.Files)
                    {
                        if (result.Add(item))
                        {
                            queue.Enqueue(item);
                        }
                    }
                }
            }
            return result;
        }

        public void Save(TextWriter writer)
        {
            JsonUtility.Serialize(writer, _dictionary);
        }

        public static DependencyGraph Load(TextReader reader)
        {
            return new DependencyGraph(
                JsonUtility.Deserialize<Dictionary<string, Dependency>>(
                    reader));
        }
    }

    public class Dependency
    {
        // files that one file depends on
        public SortedSet<string> Files { get; set; } = new SortedSet<string>();

        // uids that one file depends on
        public SortedSet<string> Uids { get; set; } = new SortedSet<string>();
    }
}
