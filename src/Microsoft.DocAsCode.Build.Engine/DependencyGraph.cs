// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    public class DependencyGraph
    {
        private readonly Dictionary<string, SortedSet<string>> _dictionary =
            new Dictionary<string, SortedSet<string>>();

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
    }
}
