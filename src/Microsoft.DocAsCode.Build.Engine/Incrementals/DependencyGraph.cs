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
        private static readonly Dictionary<string, DependencyType> _types = new Dictionary<string, DependencyType>();

        public DependencyGraph()
            : this(new List<DependencyItem>())
        {
        }

        private DependencyGraph(List<DependencyItem> dependencies)
        {
            _dependencyItems = dependencies;
            RebuildIndex();
        }

        static DependencyGraph()
        {
            // Register default dependency types
            RegisterDependencyType(new DependencyType
            {
                Name = DependencyTypeName.Include,
                IsTransitive = true,
                TriggerBuild = true,
            });
            RegisterDependencyType(new DependencyType
            {
                Name = DependencyTypeName.Uid,
                IsTransitive = false,
                TriggerBuild = false,
            });
        }

        public static IReadOnlyDictionary<string, DependencyType> DependencyTypes
        {
            get { return _types; }
        }

        public static void RegisterDependencyType(DependencyType dt)
        {
            DependencyType stored;
            if (_types.TryGetValue(dt.Name, out stored))
            {
                Logger.LogWarning($"Dependency type {JsonUtility.Serialize(dt)} isn't registered successfully because a type with name {dt.Name} is already registered. Already registered one: {JsonUtility.Serialize(stored)}.");
                return;
            }
            _types[dt.Name] = dt;
        }

        public void ReportDependency(DependencyItem dependency)
        {
            ReportDependency(new List<DependencyItem> { dependency });
        }

        public void ReportDependency(IEnumerable<DependencyItem> dependencies)
        {
            foreach (var dependency in dependencies)
            {
                if (IsValidDependency(dependency))
                {
                    int index = _dependencyItems.Count;
                    _dependencyItems.Add(dependency);
                    CreateOrUpdate(_indexOnFrom, dependency.From, index);
                    CreateOrUpdate(_indexOnReportedBy, dependency.ReportedBy, index);
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

        public IEnumerable<string> FromNodes
        {
            get
            {
                return _indexOnFrom.Keys;
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
                    if (result.Add(item) && _types[item.Type].IsTransitive)
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

        private bool IsValidDependency(DependencyItem dependency)
        {
            DependencyType dt;
            if (!_types.TryGetValue(dependency.Type, out dt))
            {
                Logger.LogWarning($"dependency type {dependency.Type} isn't registered yet.");
                return false;
            }
            return true;
        }
    }
}
