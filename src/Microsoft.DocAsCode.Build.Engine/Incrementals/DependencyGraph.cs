// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;

    using Microsoft.DocAsCode.Common;

    public class DependencyGraph
    {
        private readonly HashSet<DependencyItem> _dependencyItems;
        private readonly Dictionary<string, HashSet<DependencyItem>> _indexOnFrom = new Dictionary<string, HashSet<DependencyItem>>();
        private readonly Dictionary<string, HashSet<DependencyItem>> _indexOnReportedBy = new Dictionary<string, HashSet<DependencyItem>>();
        private static readonly Dictionary<string, DependencyType> _types = new Dictionary<string, DependencyType>();

        public DependencyGraph()
            : this(new HashSet<DependencyItem>())
        {
        }

        private DependencyGraph(HashSet<DependencyItem> dependencies)
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
                    if (_dependencyItems.Add(dependency))
                    {
                        CreateOrUpdate(_indexOnFrom, dependency.From, dependency);
                        CreateOrUpdate(_indexOnReportedBy, dependency.ReportedBy, dependency);
                    }
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

        public HashSet<DependencyItem> GetDependencyReportedBy(string reportedBy)
        {
            HashSet<DependencyItem> indice;
            if (!_indexOnReportedBy.TryGetValue(reportedBy, out indice))
            {
                return new HashSet<DependencyItem>();
            }
            return indice;
        }

        public HashSet<DependencyItem> GetDependencyFrom(string from)
        {
            HashSet<DependencyItem> indice;
            if (!_indexOnFrom.TryGetValue(from, out indice))
            {
                return new HashSet<DependencyItem>();
            }
            return indice;
        }

        public HashSet<DependencyItem> GetAllDependencyFrom(string from)
        {
            var dp = GetDependencyFrom(from);
            var result = new HashSet<DependencyItem>(dp);
            var queue = new Queue<DependencyItem>(dp);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var item in GetDependencyFrom(current.To))
                {
                    if (current.Type == item.Type && _types[item.Type].IsTransitive && result.Add(item))
                    {
                        queue.Enqueue(item);
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
            var dependencies = JsonUtility.Deserialize<HashSet<DependencyItem>>(reader);
            return new DependencyGraph(dependencies);
        }

        private void RebuildIndex()
        {
            foreach (var item in _dependencyItems)
            {
                CreateOrUpdate(_indexOnFrom, item.From, item);
                CreateOrUpdate(_indexOnReportedBy, item.ReportedBy, item);
            }
        }

        private static void CreateOrUpdate(Dictionary<string, HashSet<DependencyItem>> dict, string key, DependencyItem value)
        {
            HashSet<DependencyItem> items;
            if (!dict.TryGetValue(key, out items))
            {
                items = new HashSet<DependencyItem>();
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
