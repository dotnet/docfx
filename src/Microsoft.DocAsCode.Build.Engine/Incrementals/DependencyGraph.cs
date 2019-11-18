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

        private static readonly ImmutableList<DependencyType> _defaultTypes = ImmutableList.Create(
            new DependencyType
            {
                Name = DependencyTypeName.Include,
                Phase = BuildPhase.Compile,
                Transitivity = DependencyTransitivity.All,
            },
            new DependencyType
            {
                Name = DependencyTypeName.OverwriteFragments,
                Phase = BuildPhase.Compile,
                Transitivity = DependencyTransitivity.All,
            },
            new DependencyType
            {
                Name = DependencyTypeName.Overwrite,
                Phase = BuildPhase.Link,
                Transitivity = DependencyTransitivity.All,
            },
            new DependencyType
            {
                Name = DependencyTypeName.Uid,
                Phase = BuildPhase.Link,
                Transitivity = DependencyTransitivity.None,
            },
            new DependencyType
            {
                Name = DependencyTypeName.File,
                Phase = BuildPhase.Link,
                Transitivity = DependencyTransitivity.None,
            },
            new DependencyType
            {
                Name = DependencyTypeName.Bookmark,
                Phase = BuildPhase.Link,
                Transitivity = DependencyTransitivity.None,
            },
            new DependencyType
            {
                Name = DependencyTypeName.Metadata,
                Phase = BuildPhase.Link,
                Transitivity = DependencyTransitivity.Never,
            });

        private readonly HashSet<DependencyItem> _dependencyItems;
        private readonly HashSet<ReferenceItem> _referenceItems = new HashSet<ReferenceItem>();
        private readonly object _typeSync = new object();
        private readonly ReaderWriterLockSlim _itemsSync = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim _referenceSync = new ReaderWriterLockSlim();
        private readonly OSPlatformSensitiveDictionary<HashSet<DependencyItem>> _indexOnFrom = new OSPlatformSensitiveDictionary<HashSet<DependencyItem>>();
        private readonly OSPlatformSensitiveDictionary<HashSet<DependencyItem>> _indexOnTo = new OSPlatformSensitiveDictionary<HashSet<DependencyItem>>();
        private readonly OSPlatformSensitiveDictionary<HashSet<DependencyItem>> _indexOnReportedBy = new OSPlatformSensitiveDictionary<HashSet<DependencyItem>>();
        private readonly OSPlatformSensitiveDictionary<HashSet<ReferenceItem>> _indexOnReferenceReportedBy = new OSPlatformSensitiveDictionary<HashSet<ReferenceItem>>();
        private ImmutableDictionary<string, DependencyType> _types;
        private Dictionary<(string fromDependencyType, string toDependencyType), bool> _couldTransit
            = new Dictionary<(string fromDependencyType, string toDependencyType), bool>();
        private bool _isResolved = false;

        #endregion

        #region Constuctors

        internal DependencyGraph()
            : this(
                new HashSet<DependencyItem>(),
                _defaultTypes.ToDictionary(item => item.Name, item => item),
                new HashSet<ReferenceItem>())
        {
        }

        private DependencyGraph(HashSet<DependencyItem> dependencies, Dictionary<string, DependencyType> types, HashSet<ReferenceItem> references)
        {
            _dependencyItems = dependencies;
            _types = types.ToImmutableDictionary();
            _referenceItems = references;
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
                ReportDependency(dependency);
            }
        }

        public void ReportReference(ReferenceItem reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }
            ReportReferenceCore(reference);
        }

        public void ReportReference(IEnumerable<ReferenceItem> references)
        {
            if (references == null)
            {
                throw new ArgumentNullException(nameof(references));
            }
            foreach (var reference in references)
            {
                ReportReference(reference);
            }
        }

        public void ResolveReference()
        {
            WriteDependency(() => ResolveReferenceCore());
        }

        public bool HasDependencyReportedBy(string reportedBy)
        {
            return ReadDependency(() => HasDependencyReportedByNoLock(reportedBy));
        }

        public bool HasDependencyFrom(string from)
        {
            return ReadDependency(() => HasDependencyFromNoLock(from));
        }

        public IEnumerable<string> FromNodes => ReadDependency(() =>
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            return _indexOnFrom.Keys;
        });

        public IEnumerable<string> ReportedBys => ReadDependency(() =>
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            return _indexOnReportedBy.Keys;
        });

        public IEnumerable<string> ToNodes => ReadDependency(() =>
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            return _indexOnTo.Keys;
        });

        public IEnumerable<string> ReferenceReportedBys => ReadReference(() => _indexOnReferenceReportedBy.Keys);

        public HashSet<DependencyItem> GetDependencyReportedBy(string reportedBy)
        {
            return ReadDependency(() => GetDependencyReportedByNoLock(reportedBy));
        }

        public HashSet<DependencyItem> GetDependencyFrom(string from)
        {
            return ReadDependency(() => GetDependencyFromNoLock(from));
        }

        public HashSet<DependencyItem> GetDependencyTo(string to)
        {
            return ReadDependency(() => GetDependencyToNoLock(to));
        }

        public HashSet<DependencyItem> GetAllDependencyFrom(string from)
        {
            return ReadDependency(() => GetAllDependencyFromNoLock(from));
        }

        public HashSet<DependencyItem> GetAllDependencyTo(string to)
        {
            return ReadDependency(() => GetAllDependencyToNoLock(to));
        }

        public HashSet<string> GetAllDependentNodes()
        {
            return ReadDependency(GetAllDependentNodesNoLock);
        }

        public HashSet<string> GetAllIncludeDependencyFrom(string from)
        {
            var files = new HashSet<string>();
            foreach (var item in GetAllDependencyFrom(from))
            {
                if (item.Type == DependencyTypeName.Include)
                {
                    files.Add(item.To.Value);
                }
            }
            return files;
        }

        public HashSet<ReferenceItem> GetReferenceReportedBy(string reportedBy)
        {
            return ReadReference(() => GetReferenceReportedByNoLock(reportedBy));
        }

        public void Save(TextWriter writer)
        {
            ReadDependency(() => SaveNoLock(writer));
        }

        public static DependencyGraph Load(TextReader reader)
        {
            var dependencies = JsonUtility.Deserialize<Tuple<HashSet<DependencyItem>, Dictionary<string, DependencyType>, HashSet<ReferenceItem>>>(reader);
            return new DependencyGraph(dependencies.Item1, dependencies.Item2, dependencies.Item3);
        }

        #endregion

        #region Private Members

        private void RegisterDependencyTypeCore(DependencyType dt)
        {
            lock (_typeSync)
            {
                if (_types.TryGetValue(dt.Name, out DependencyType stored))
                {
                    if (stored.Transitivity != dt.Transitivity || stored.Phase != dt.Phase)
                    {
                        Logger.LogError($"Dependency type {JsonUtility.Serialize(dt)} isn't registered successfully because a different type with name {dt.Name} is already registered. Already registered one: {JsonUtility.Serialize(stored)}.");
                        throw new InvalidDataException($"A different dependency type with name {dt.Name} is already registered");
                    }
                    Logger.LogVerbose($"Same dependency type with name {dt.Name} has already been registered, ignored.");
                    return;
                }
                _types = _types.SetItem(dt.Name, dt);
                Logger.LogVerbose($"Dependency type is successfully registered. Name: {dt.Name}, Phase to work on: {dt.Phase}, Transitivity: {dt.Transitivity}.");
            }
        }

        private void ReportDependencyCore(DependencyItem dependency)
        {
            if (IsValidDependency(dependency))
            {
                WriteDependency(() => ReportDependencyCoreNoLock(dependency));
            }
        }

        private void ReportDependencyCoreNoLock(DependencyItem dependency)
        {
            if (dependency.From == dependency.To)
            {
                Logger.LogDiagnostic($"Dependency item is ignored because it is a self-dependency: {JsonUtility.Serialize(dependency)}.");
                return;
            }
            if (_dependencyItems.Add(dependency))
            {
                if (CanReadDependency(dependency))
                {
                    CreateOrUpdate(_indexOnFrom, dependency.From.Value, dependency);
                    CreateOrUpdate(_indexOnReportedBy, dependency.ReportedBy.Value, dependency);
                    CreateOrUpdate(_indexOnTo, dependency.To.Value, dependency);
                }
                else
                {
                    _isResolved = false;
                }

                Logger.LogDiagnostic($"Dependency item is successfully reported: {JsonUtility.Serialize(dependency)}.");
            }
        }

        private void ReportReferenceCore(ReferenceItem reference)
        {
            WriteReference(() => ReportReferenceCoreNoLock(reference));
        }

        private void ReportReferenceCoreNoLock(ReferenceItem reference)
        {
            _referenceItems.Add(reference);
            CreateOrUpdate(_indexOnReferenceReportedBy, reference.ReportedBy, reference);
        }

        private void ResolveReferenceCore()
        {
            ReadReference(() => ResolveReferenceCoreNoLock());
            _isResolved = true;
        }

        private void ResolveReferenceCoreNoLock()
        {
            var indexer = (from r in _referenceItems
                           group r by r.Reference into g
                           select g).ToDictionary(gr => gr.Key, gr => gr.Select(i => i.File).First());

            var unresolved = new HashSet<DependencyItemSourceInfo>();

            foreach (var item in _dependencyItems.Where(i => !CanReadDependency(i)).ToList())
            {
                var updated = item;
                var from = TryResolveReference(indexer, item.From);
                var to = TryResolveReference(indexer, item.To);
                var reportedBy = TryResolveReference(indexer, item.ReportedBy);

                if (from != null)
                {
                    updated = updated.ChangeFrom(from);
                }
                else
                {
                    unresolved.Add(item.From);
                }

                if (to != null)
                {
                    updated = updated.ChangeTo(to);
                }
                else
                {
                    unresolved.Add(item.To);
                }

                if (reportedBy != null)
                {
                    updated = updated.ChangeReportedBy(reportedBy);
                }
                else
                {
                    unresolved.Add(item.ReportedBy);
                }

                if (updated != item)
                {
                    _dependencyItems.Remove(item);
                    if (from != null && from == to)
                    {
                        Logger.LogDiagnostic($"Dependency item is ignored because it is a self-dependency after the resolution: {item}.");
                    }
                    else
                    {
                        _dependencyItems.Add(updated);
                    }
                }

                // update index
                if (from != null && to != null && reportedBy != null && from != to)
                {
                    CreateOrUpdate(_indexOnFrom, updated.From.Value, updated);
                    CreateOrUpdate(_indexOnReportedBy, updated.ReportedBy.Value, updated);
                    CreateOrUpdate(_indexOnTo, updated.To.Value, updated);
                }
            }

            if (unresolved.Count > 0)
            {
                Logger.LogVerbose(
                    $"Dependency graph: {unresolved.Count} unresolved references, following is the top 100: {Environment.NewLine}" +
                    string.Join(Environment.NewLine, unresolved.Take(100)));
            }
        }

        private DependencyItemSourceInfo TryResolveReference(Dictionary<DependencyItemSourceInfo, string> indexer, DependencyItemSourceInfo source)
        {
            if (source.SourceType == DependencyItemSourceType.File)
            {
                return source;
            }
            if (!indexer.TryGetValue(source, out string file))
            {
                return null;
            }
            return source.ChangeSourceType(DependencyItemSourceType.File).ChangeValue(file);
        }

        private HashSet<DependencyItem> GetDependencyReportedByNoLock(string reportedBy)
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            if (!_indexOnReportedBy.TryGetValue(reportedBy, out HashSet<DependencyItem> indice))
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
            if (!_indexOnFrom.TryGetValue(from, out HashSet<DependencyItem> indice))
            {
                return new HashSet<DependencyItem>();
            }
            return indice;
        }

        private HashSet<DependencyItem> GetDependencyToNoLock(string to)
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            if (!_indexOnTo.TryGetValue(to, out HashSet<DependencyItem> indice))
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
                    if (CouldTransitNoLock(current.Type, item.Type) && result.Add(item))
                    {
                        queue.Enqueue(item);
                    }
                }
            }
            return result;
        }

        private HashSet<DependencyItem> GetAllDependencyToNoLock(string to)
        {
            var dp = GetDependencyToNoLock(to);
            var result = new HashSet<DependencyItem>(dp);
            var queue = new Queue<DependencyItem>(dp);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var item in GetDependencyToNoLock(current.From.Value))
                {
                    if (CouldTransitNoLock(item.Type, current.Type) && result.Add(item))
                    {
                        queue.Enqueue(item);
                    }
                }
            }
            return result;
        }

        private bool CouldTransitNoLock(string fromDependencyType, string toDependencyType)
        {
            var key = (fromDependencyType, toDependencyType);
            if (_couldTransit.TryGetValue(key, out var result))
            {
                return result;
            }
            else
            {
                result = _types[fromDependencyType].CouldTransit(_types[toDependencyType]);
                _couldTransit[key] = result;
                return result;
            }
        }

        private HashSet<string> GetAllDependentNodesNoLock()
        {
            if (!_isResolved)
            {
                throw new InvalidOperationException($"Dependency graph isn't resolved, cannot call the method.");
            }
            return new HashSet<string>(from item in _dependencyItems
                                       where CanReadDependency(item)
                                       select item.To.Value);
        }

        private HashSet<ReferenceItem> GetReferenceReportedByNoLock(string reportedBy)
        {
            if (!_indexOnReferenceReportedBy.TryGetValue(reportedBy, out HashSet<ReferenceItem> indice))
            {
                return new HashSet<ReferenceItem>();
            }
            return indice;
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
            JsonUtility.Serialize(writer, Tuple.Create(_dependencyItems, _types, _referenceItems));
        }

        private void RebuildIndex()
        {
            foreach (var item in _dependencyItems)
            {
                if (CanReadDependency(item))
                {
                    CreateOrUpdate(_indexOnFrom, item.From.Value, item);
                    CreateOrUpdate(_indexOnReportedBy, item.ReportedBy.Value, item);
                    CreateOrUpdate(_indexOnTo, item.To.Value, item);
                }
            }
            foreach (var reference in _referenceItems)
            {
                CreateOrUpdate(_indexOnReferenceReportedBy, reference.ReportedBy, reference);
            }
            _isResolved = true;
        }

        private static void CreateOrUpdate<T>(Dictionary<string, HashSet<T>> index, string key, T value)
        {
            if (!index.TryGetValue(key, out HashSet<T> items))
            {
                items = new HashSet<T>();
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
                // When the processor contains no files other than overwrites, this processor will not even loaded,
                // As a result, the dependency types inside this processer will not be registered
                // This is a common case from now on so there is no need to log warning
                // Logger.LogWarning($"dependency type {dependency.Type} isn't registered yet.");
                return false;
            }
            return true;
        }

        #region Read/Write

        private void ReadDependency(Action reader)
        {
            Read(_itemsSync, reader);
        }

        private T ReadDependency<T>(Func<T> reader)
        {
            return Read(_itemsSync, reader);
        }
        private void ReadReference(Action reader)
        {
            Read(_referenceSync, reader);
        }

        private T ReadReference<T>(Func<T> reader)
        {
            return Read(_referenceSync, reader);
        }

        private void WriteDependency(Action writer)
        {
            Write(_itemsSync, writer);
        }

        private T WriteDependency<T>(Func<T> writer)
        {
            return Write(_itemsSync, writer);
        }

        private void WriteReference(Action writer)
        {
            Write(_referenceSync, writer);
        }

        private T WriteReference<T>(Func<T> writer)
        {
            return Write(_referenceSync, writer);
        }

        private static void Read(ReaderWriterLockSlim slim, Action reader)
        {
            slim.EnterReadLock();
            try
            {
                reader();
            }
            finally
            {
                slim.ExitReadLock();
            }
        }

        private static T Read<T>(ReaderWriterLockSlim slim, Func<T> reader)
        {
            slim.EnterReadLock();
            try
            {
                return reader();
            }
            finally
            {
                slim.ExitReadLock();
            }
        }

        private static void Write(ReaderWriterLockSlim slim, Action writer)
        {
            slim.EnterWriteLock();
            try
            {
                writer();
            }
            finally
            {
                slim.ExitWriteLock();
            }
        }

        private static T Write<T>(ReaderWriterLockSlim slim, Func<T> writer)
        {
            slim.EnterWriteLock();
            try
            {
                return writer();
            }
            finally
            {
                slim.ExitWriteLock();
            }
        }
        #endregion

        #endregion
    }
}
