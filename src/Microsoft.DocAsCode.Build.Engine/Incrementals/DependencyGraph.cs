// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;

    public sealed class DependencyGraph
    {
        private readonly HashSet<DependencyItem> _dependencyItems;
        private readonly Dictionary<string, DependencyType> _types;
        private readonly Dictionary<string, HashSet<DependencyItem>> _indexOnFrom = new Dictionary<string, HashSet<DependencyItem>>();
        private readonly Dictionary<string, HashSet<DependencyItem>> _indexOnReportedBy = new Dictionary<string, HashSet<DependencyItem>>();
        private static readonly Dictionary<string, DependencyType> _defaultTypes = new Dictionary<string, DependencyType>
        {
            { DependencyTypeName.Uid, new DependencyType { Name = DependencyTypeName.Uid, IsTransitive = false, TriggerBuild = false } },
        };

        internal DependencyGraph()
            : this(new HashSet<DependencyItem>(), _defaultTypes)
        {
        }

        private DependencyGraph(HashSet<DependencyItem> dependencies, Dictionary<string, DependencyType> types)
        {
            _dependencyItems = dependencies;
            _types = types;
            RebuildIndex();
        }

        public IReadOnlyDictionary<string, DependencyType> DependencyTypes
        {
            get { return _types; }
        }

        public void RegisterDependencyType(DependencyType dt)
        {
            RegisterDependencyType(new List<DependencyType> { dt });
        }

        public void RegisterDependencyType(IEnumerable<DependencyType> dts)
        {
            if (dts == null)
            {
                throw new ArgumentNullException(nameof(dts));
            }
            foreach (var dt in dts)
            {
                DependencyType stored;
                if (_types.TryGetValue(dt.Name, out stored) && (stored.TriggerBuild != dt.TriggerBuild || stored.IsTransitive != dt.IsTransitive))
                {
                    Logger.LogError($"Dependency type {JsonUtility.Serialize(dt)} isn't registered successfully because a different type with name {dt.Name} is already registered. Already registered one: {JsonUtility.Serialize(stored)}.");
                    throw new InvalidDataException($"A different dependency type with name {dt.Name} is already registered");
                }
                _types[dt.Name] = dt;
            }
        }

        public void ReportDependency(DependencyItem dependency)
        {
            ReportDependency(new List<DependencyItem> { dependency });
        }

        public void ReportDependency(IEnumerable<DependencyItem> dependencies)
        {
            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }
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
            JsonUtility.Serialize(writer, Tuple.Create(_dependencyItems, _types));
        }

        public static DependencyGraph Load(TextReader reader)
        {
            var dependencies = JsonUtility.Deserialize<Tuple<HashSet<DependencyItem>, Dictionary<string, DependencyType>>>(reader);
            return new DependencyGraph(dependencies.Item1, dependencies.Item2);
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
