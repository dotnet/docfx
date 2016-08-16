// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Incrementals
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    public class DependencyGraph
    {
        private readonly Dictionary<string, SortedSet<string>> _dictionary;

        public DependencyGraph()
            : this(new Dictionary<string, SortedSet<string>>())
        {
        }

        private DependencyGraph(Dictionary<string, SortedSet<string>> dictionary)
        {
            _dictionary = dictionary;
        }

        public void ReportDependency(string key, string value)
        {
            SortedSet<string> set;
            if (_dictionary.TryGetValue(key, out set))
            {
                set.Add(value);
            }
            else
            {
                _dictionary[key] = new SortedSet<string>() { value };
            }
        }

        public void ReportDependency(string key, IEnumerable<string> values)
        {
            SortedSet<string> set;
            if (_dictionary.TryGetValue(key, out set))
            {
                set.UnionWith(values);
            }
            else
            {
                _dictionary[key] = new SortedSet<string>(values);
            }
        }

        public bool HasDependency(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public IEnumerable<string> Keys
        {
            get { return _dictionary.Keys; }
        }

        public SortedSet<string> GetAllDependency(string key)
        {
            var result = new SortedSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(key);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (result.Add(current))
                {
                    SortedSet<string> set;
                    if (_dictionary.TryGetValue(current, out set))
                    {
                        foreach (var item in set)
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
                JsonUtility.Deserialize<Dictionary<string, SortedSet<string>>>(
                    reader));
        }
    }
}
