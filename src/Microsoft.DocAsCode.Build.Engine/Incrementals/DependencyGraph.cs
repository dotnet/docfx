// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public sealed class DependencyGraph
    {
        #region Fields

        private static readonly ImmutableList<DependencyType> _defaultTypes = ImmutableList.Create<DependencyType>(
            new DependencyType
            {
                Name = DependencyTypeName.Include,
                IsTransitive = true,
                Phase = BuildPhase.Compile,
                Transitivity = DependencyTransitivity.All,
            },
            new DependencyType
            {
                Name = DependencyTypeName.Overwrite,
                IsTransitive = true,
                Phase = BuildPhase.Link,
                Transitivity = DependencyTransitivity.All,
            },
            new DependencyType
            {
                Name = DependencyTypeName.Uid,
                IsTransitive = false,
                Phase = BuildPhase.Link,
                Transitivity = DependencyTransitivity.None,
            },
            new DependencyType
            {
                Name = DependencyTypeName.File,
                IsTransitive = false,
                Phase = BuildPhase.Link,
                Transitivity = DependencyTransitivity.None,
            },
            new DependencyType
            {
                Name = DependencyTypeName.Bookmark,
                IsTransitive = false,
                Phase = BuildPhase.Link,
                Transitivity = DependencyTransitivity.None,
            },
            new DependencyType
            {
                Name = DependencyTypeName.Metadata,
                IsTransitive = false,
                Phase = BuildPhase.Link,
                Transitivity = DependencyTransitivity.None,
            });

        private readonly HashSet<DependencyItem> _dependencyItems;
        private readonly object _typeSync = new object();
        private readonly object _referenceSync = new object();
        private readonly ReaderWriterLockSlim _itemsSync = new ReaderWriterLockSlim();
        private readonly OSPlatformSensitiveDictionary<HashSet<DependencyItem>> _indexOnFrom = new OSPlatformSensitiveDictionary<HashSet<DependencyItem>>();
        private readonly OSPlatformSensitiveDictionary<HashSet<DependencyItem>> _indexOnReportedBy = new OSPlatformSensitiveDictionary<HashSet<DependencyItem>>();
        private ImmutableDictionary<string, DependencyType> _types;
        private readonly Dictionary<DependencyItemSourceInfo, string> _referenceItems;
        private bool _isResolved = false;

        #endregion

        #region Constuctors

        internal DependencyGraph()
            : this(
                new HashSet<DependencyItem>(),
                _defaultTypes.ToDictionary(item => item.Name, item => item))
        {
        }

        private DependencyGraph(HashSet<DependencyItem> dependencies, Dictionary<string, DependencyType> types)
        {
            _dependencyItems = dependencies;
            _types = types.ToImmutableDictionary();
            RebuildIndex();
        }

        #endregion

        #region Public Members

        public IReadOnlyDictionary<string, DependencyType> DependencyTypes
        {
            get { return _types; }
        }

        public void RegisterDependencyType(DependencyType dt)
        {
            if (dt == null)
            {
                throw new ArgumentNullException(nameof(dt));
            }
            RegisterDependencyTypeCore(dt);
        }

        public void RegisterDependencyType(IEnumerable<DependencyType> dts)
        {
            if (dts == null)
            {
                throw new ArgumentNullException(nameof(dts));
            }
            foreach (var dt in dts)
            {
                if (dt == null)
                {
                    throw new ArgumentException("Elements cannot contain null.", nameof(dt));
                }
                RegisterDependencyTypeCore(dt);
            }
        }

        public void ReportDependency(DependencyItem dependency)
        {
            if (dependency == null)
            {
                throw new ArgumentNullException(nameof(dependency));
            }
            ReportDependencyCore(dependency);
        }

        public void ReportDependency(IEnumerable<DependencyItem> dependencies)
        {
            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }
            foreach (var dependency in dependencies)
            {
                if (dependency == null)
                {
                    throw new ArgumentException("Elements cannot contain null.", nameof(dependency));
                }
                ReportDependencyCore(dependency);
            }
        }

        public void ReportReference(string file, IEnumerable<DependencyItemSourceInfo> references)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (references == null)
            {
                throw new ArgumentNullException(nameof(references));
            }
            lock (_referenceSync)
            {
                foreach (var r in references)
                {
                    _referenceItems[r] = file;
                }
            }
        }

        public void ResolveReference()
        {
            Write(() => ResolveReferenceNoLock());
        }

        public bool HasDependencyReportedBy(string reportedBy)
        {
            return Read(() => HasDependencyReportedByNoLock(reportedBy));
        }

        public bool HasDependencyFrom(string from)
        {
            return Read(() => HasDependencyFromNoLock(from));
        }

        public IEnumerable<string> FromNodes => Read(() =>
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            return _indexOnFrom.Keys;
        });

        public IEnumerable<string> ReportedBys => Read(() =>
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            return _indexOnReportedBy.Keys;
        });

        public HashSet<DependencyItem> GetDependencyReportedBy(string reportedBy)
        {
            return Read(() => GetDependencyReportedByNoLock(reportedBy));
        }

        public HashSet<DependencyItem> GetDependencyFrom(string from)
        {
            return Read(() => GetDependencyFromNoLock(from));
        }

        public HashSet<DependencyItem> GetAllDependencyFrom(string from)
        {
            return Read(() => GetAllDependencyFromNoLock(from));
        }

        public HashSet<string> GetAllDependentNodes()
        {
            return Read(GetAllDependentNodesNoLock);
        }

        public void Save(TextWriter writer)
        {
            Read(() => SaveNoLock(writer));
        }

        public static DependencyGraph Load(TextReader reader)
        {
            var dependencies = JsonUtility.Deserialize<Tuple<HashSet<DependencyItem>, Dictionary<string, DependencyType>>>(reader);
            return new DependencyGraph(dependencies.Item1, dependencies.Item2);
        }

        #endregion

        #region Private Members

        private void RegisterDependencyTypeCore(DependencyType dt)
        {
            lock (_typeSync)
            {
                DependencyType stored;
                if (_types.TryGetValue(dt.Name, out stored))
                {
                    // to-do: add check for phase/transitivity when new value overwrites old value
                    if (stored.IsTransitive != dt.IsTransitive)
                    {
                        Logger.LogError($"Dependency type {JsonUtility.Serialize(dt)} isn't registered successfully because a different type with name {dt.Name} is already registered. Already registered one: {JsonUtility.Serialize(stored)}.");
                        throw new InvalidDataException($"A different dependency type with name {dt.Name} is already registered");
                    }
                    if (stored.Phase != null && stored.Transitivity != null)
                    {
                        Logger.LogVerbose($"Same dependency type with name {dt.Name} has already been registered, ignored.");
                        return;
                    }
                }
                _types = _types.SetItem(dt.Name, dt);
                Logger.LogVerbose($"Dependency type is successfully registered. Name: {dt.Name}, IsTransitive: {dt.IsTransitive}, Phase to work on: {dt.Phase}, Transitivity: {dt.Transitivity}.");
            }
        }

        private void ReportDependencyCore(DependencyItem dependency)
        {
            if (IsValidDependency(dependency))
            {
                Write(() => ReportDependencyCoreNoLock(dependency));
            }
        }

        private void ReportDependencyCoreNoLock(DependencyItem dependency)
        {
            if (_dependencyItems.Add(dependency))
            {
                if (CanReadDependency(dependency))
                {
                    CreateOrUpdate(_indexOnFrom, dependency.From.Value, dependency);
                    CreateOrUpdate(_indexOnReportedBy, dependency.ReportedBy.Value, dependency);
                }
                else
                {
                    _isResolved = false;
                }

                Logger.LogDiagnostic($"Dependency item is successfully reported: {JsonUtility.Serialize(dependency)}.");
            }
        }

        private void ResolveReferenceNoLock()
        {
            lock (_referenceSync)
            {
                foreach (var item in _dependencyItems.Where(i => !CanReadDependency(i)))
                {
                    item.UpdateFrom(ResolveReferenceCoreNoLock(item.From));
                    item.UpdateTo(ResolveReferenceCoreNoLock(item.To));
                    item.UpdateReportedBy(ResolveReferenceCoreNoLock(item.ReportedBy));

                    // update index
                    CreateOrUpdate(_indexOnFrom, item.From.Value, item);
                    CreateOrUpdate(_indexOnReportedBy, item.ReportedBy.Value, item);
                }
            }

            _isResolved = true;
        }

        private DependencyItemSourceInfo ResolveReferenceCoreNoLock(DependencyItemSourceInfo source)
        {
            if (source.SourceType != DependencyItemSourceType.Reference)
            {
                return source;
            }
            string file;
            if (!_referenceItems.TryGetValue(source, out file))
            {
                throw new InvalidDataException($"Failed to resolve reference: {JsonUtility.Serialize(source)}.");
            }
            return source.ChangeSourceType(DependencyItemSourceType.File).ChangeValue(file);
        }

        private HashSet<DependencyItem> GetDependencyReportedByNoLock(string reportedBy)
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            HashSet<DependencyItem> indice;
            if (!_indexOnReportedBy.TryGetValue(reportedBy, out indice))
            {
                return new HashSet<DependencyItem>();
            }
            return indice;
        }

        private HashSet<DependencyItem> GetDependencyFromNoLock(string from)
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            HashSet<DependencyItem> indice;
            if (!_indexOnFrom.TryGetValue(from, out indice))
            {
                return new HashSet<DependencyItem>();
            }
            return indice;
        }

        private HashSet<DependencyItem> GetAllDependencyFromNoLock(string from)
        {
            var dp = GetDependencyFromNoLock(from);
            var result = new HashSet<DependencyItem>(dp);
            var queue = new Queue<DependencyItem>(dp);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var item in GetDependencyFromNoLock(current.To.Value))
                {
                    if (_types[current.Type].CouldTransit(_types[item.Type]) && result.Add(item))
                    {
                        queue.Enqueue(item);
                    }
                }
            }
            return result;
        }

        private HashSet<string> GetAllDependentNodesNoLock()
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            return new HashSet<string>(from item in _dependencyItems
                                       select item.To.Value);
        }

        private bool HasDependencyReportedByNoLock(string reportedBy)
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            return _indexOnReportedBy.ContainsKey(reportedBy);
        }

        private bool HasDependencyFromNoLock(string from)
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            return _indexOnFrom.ContainsKey(from);
        }

        private void SaveNoLock(TextWriter writer)
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            JsonUtility.Serialize(writer, Tuple.Create(_dependencyItems, _types));
        }

        private void RebuildIndex()
        {
            bool flag = true;
            foreach (var item in _dependencyItems)
            {
                if (CanReadDependency(item))
                {
                    CreateOrUpdate(_indexOnFrom, item.From.Value, item);
                    CreateOrUpdate(_indexOnReportedBy, item.ReportedBy.Value, item);
                }
                else
                {
                    flag = false;
                }
            }
            _isResolved = flag;
        }

        private static void CreateOrUpdate(Dictionary<string, HashSet<DependencyItem>> index, string key, DependencyItem value)
        {
            HashSet<DependencyItem> items;
            if (!index.TryGetValue(key, out items))
            {
                items = new HashSet<DependencyItem>();
                index[key] = items;
            }
            items.Add(value);
        }

        private bool CanReadDependency(DependencyItem dependency)
        {
            return dependency.From.SourceType == DependencyItemSourceType.File &&
                dependency.To.SourceType == DependencyItemSourceType.File &&
                dependency.ReportedBy.SourceType == DependencyItemSourceType.File;
        }

        private bool IsValidDependency(DependencyItem dependency)
        {
            if (!_types.ContainsKey(dependency.Type))
            {
                Logger.LogWarning($"dependency type {dependency.Type} isn't registered yet.");
                return false;
            }
            return true;
        }

        private void Read(Action reader)
        {
            _itemsSync.EnterReadLock();
            try
            {
                reader();
            }
            finally
            {
                _itemsSync.ExitReadLock();
            }
        }

        private T Read<T>(Func<T> reader)
        {
            _itemsSync.EnterReadLock();
            try
            {
                return reader();
            }
            finally
            {
                _itemsSync.ExitReadLock();
            }
        }

        private void Write(Action writer)
        {
            _itemsSync.EnterWriteLock();
            try
            {
                writer();
            }
            finally
            {
                _itemsSync.ExitWriteLock();
            }
        }

        private T Write<T>(Func<T> writer)
        {
            _itemsSync.EnterWriteLock();
            try
            {
                return writer();
            }
            finally
            {
                _itemsSync.ExitWriteLock();
            }
        }

        #endregion
    }
}
