// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;

    public class DependencyGraph
    {
        private readonly List<DependencyItem> _dependencyItems;
        private readonly Dictionary<string, List<int>> _indexOnFrom = new Dictionary<string, List<int>>();
        private readonly Dictionary<string, List<int>> _indexOnReportedBy = new Dictionary<string, List<int>>();
        private readonly Dictionary<string, List<int>> _indexOnType = new Dictionary<string, List<int>>();

        public DependencyGraph()
            : this(new List<DependencyItem>())
        {
        }

        private DependencyGraph(List<DependencyItem> dependencies)
        {
            _dependencyItems = dependencies;
            RebuildIndex();
        }

        public void ReportDependency(DependencyItem dependency)
        {
            ReportDependency(new List<DependencyItem> { dependency });
        }

        public void ReportDependency(IEnumerable<DependencyItem> dependencies)
        {
            foreach (var dependency in dependencies)
            {
                if (CheckDependencyConsistency(dependency))
                {
                    int index = _dependencyItems.Count;
                    _dependencyItems.Add(dependency);
                    CreateOrUpdate(_indexOnFrom, dependency.From, index);
                    CreateOrUpdate(_indexOnReportedBy, dependency.ReportedBy, index);
                    CreateOrUpdate(_indexOnType, dependency.Type, index);
                }
            }
        }

        public bool HasDependencyReportedBy(string reportedBy)
        {
            return _indexOnReportedBy.ContainsKey(reportedBy);
        }

        public bool HasDependencyFrom(string from)
        {
            return _indexOnFrom.ContainsKey(from);
        }

        public bool HasDependencyWithType(string type)
        {
            return _indexOnType.ContainsKey(type);
        }

        public IEnumerable<string> FromNodes
        {
            get
            {
                return _indexOnFrom.Keys;
            }
        }

        public IEnumerable<string> Types
        {
            get
            {
                return _indexOnType.Keys;
            }
        }

        public IEnumerable<string> ReportedBys
        {
            get
            {
                return _indexOnReportedBy.Keys;
            }
        }

        public IEnumerable<DependencyItem> GetDependencyReportedBy(string reportedBy)
        {
            List<int> indice;
            if (!_indexOnReportedBy.TryGetValue(reportedBy, out indice))
            {
                yield break;
            }
            foreach (int i in indice)
            {
                yield return _dependencyItems[i];
            }
        }

        public IEnumerable<DependencyItem> GetDependencyFrom(string from)
        {
            List<int> indice;
            if (!_indexOnFrom.TryGetValue(from, out indice))
            {
                yield break;
            }
            foreach (int i in indice)
            {
                yield return _dependencyItems[i];
            }
        }

        public IEnumerable<DependencyItem> GetDependencyWithType(string type)
        {
            List<int> indice;
            if (!_indexOnType.TryGetValue(type, out indice))
            {
                yield break;
            }
            foreach (int i in indice)
            {
                yield return _dependencyItems[i];
            }
        }

        public SortedSet<DependencyItem> GetAllDependencyFrom(string from)
        {
            var result = new SortedSet<DependencyItem>();
            var queue = new Queue<string>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var item in GetDependencyFrom(current))
                {
                    if (result.Add(item) && item.IsTransitive)
                    {
                        queue.Enqueue(item.From);
                    }
                }
            }
            return result;
        }

        public void Save(TextWriter writer)
        {
            JsonUtility.Serialize(writer, _dependencyItems);
        }

        public static DependencyGraph Load(TextReader reader)
        {
            var dependencies = JsonUtility.Deserialize<List<DependencyItem>>(reader);
            return new DependencyGraph(dependencies);
        }

        private void RebuildIndex()
        {
            for (var i = 0; i < _dependencyItems.Count; i++)
            {
                var item = _dependencyItems[i];
                CreateOrUpdate(_indexOnFrom, item.From, i);
                CreateOrUpdate(_indexOnReportedBy, item.ReportedBy, i);
                CreateOrUpdate(_indexOnType, item.Type, i);
            }
        }

        private static void CreateOrUpdate(Dictionary<string, List<int>> dict, string key, int value)
        {
            List<int> items;
            if (!dict.TryGetValue(key, out items))
            {
                items = new List<int>();
                dict[key] = items;
            }
            items.Add(value);
        }

        private bool CheckDependencyConsistency(DependencyItem dependency)
        {
            List<int> items;
            if (_indexOnType.TryGetValue(dependency.Type, out items) && items.Count > 0)
            {
                bool isTransitive = _dependencyItems[items[0]].IsTransitive;
                if (dependency.IsTransitive != isTransitive)
                {
                    Logger.LogWarning($"Below dependency doesn't match dependecies already reported and isn't reported: {JsonUtility.Serialize(dependency)}.");
                    return false;
                }
            }
            return true;
        }
    }
}
