// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.CodeAnalysis;

    using Microsoft.DocAsCode.Common;

    public class IncrementalCheck
    {
        private VersionStamp _versionToBeCompared;

        private ConcurrentDictionary<string, VersionStamp> _metadataVersionCache;
        private AsyncConcurrentCache<string, bool> _projectUpToDateSnapshot;
        private bool _versionChanged;
        private readonly BuildInfo _buildInfo;

        public BuildInfo BuildInfo => _buildInfo;

        public IncrementalCheck(BuildInfo buildInfo)
        {
            var checkUtcTime = buildInfo.TriggeredUtcTime;
            var version = buildInfo.BuildAssembly;
            var currentVersion = CacheBase.AssemblyName;
            if (currentVersion != version)
            {
                _versionChanged = true;
                if (_versionChanged)
                {
                    Logger.Log(LogLevel.Verbose, $"Assembly '{version ?? "<undefined>"}' when last build took place is not current assembly '{currentVersion}', rebuild required");
                }
            }
            _buildInfo = buildInfo;
            _versionToBeCompared = VersionStamp.Create(checkUtcTime);
            _metadataVersionCache = new ConcurrentDictionary<string, VersionStamp>();
            _projectUpToDateSnapshot = new AsyncConcurrentCache<string, bool>();
        }

        public bool AreFilesModified(IEnumerable<string> files)
        {
            if (_versionChanged)
            {
                return true;
            }
            foreach (var file in files)
            {
                if (IsFileModified(file))
                {
                    return true;
                }
            }

            return false;
        }

        public bool MSBuildPropertiesUpdated(IDictionary<string, string> newProperties)
        {
            return !DictionaryEqual(_buildInfo.Options.MSBuildProperties, newProperties);
        }

        /// <summary>
        /// If file does not exists, return **true**?? ==> should have checked exists before calling.
        /// If file's last modified time is newer, return true; otherwise, return false
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool IsFileModified(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return false;
            }
            if (!File.Exists(file))
            {
                Logger.Log(LogLevel.Verbose, $"File '{file}' does not exist anymore, rebuild needed");
                return true;
            }

            var version = GetLastModifiedVersionForFile(file);
            if (VersionNewer(version))
            {
                Logger.Log(LogLevel.Verbose, $"File '{file}' version '{version.ToString()}' newer than '{_versionToBeCompared.ToString()}'.");
                return true;
            }
            else
            {
                Logger.Log(LogLevel.Verbose, $"File '{file}' version '{version.ToString()}' older than '{_versionToBeCompared.ToString()}', no need to rebuild.");
            }

            return false;
        }

        private bool VersionNewer(VersionStamp thisVersion)
        {
            return VersionNewer(thisVersion, _versionToBeCompared);
        }

        private static bool VersionNewer(VersionStamp thisVersion, VersionStamp thatVersion)
        {
            var version = thisVersion.GetNewerVersion(thatVersion);
            if (version == thisVersion)
            {
                return true;
            }
            return false;
        }

        private static VersionStamp GetLastModifiedVersionForFile(string filePath)
        {
            var dateTime = File.GetLastWriteTimeUtc(filePath);
            return VersionStamp.Create(dateTime);
        }

        private static bool DictionaryEqual<TKey, TValue>(IDictionary<TKey, TValue> dict1, IDictionary<TKey, TValue> dict2, IEqualityComparer<TValue> equalityComparer = null)
        {
            if (Equals(dict1, dict2))
            {
                return true;
            }

            if (dict1 == null || dict2 == null || dict1.Count != dict2.Count)
            {
                return false;
            }

            if (equalityComparer == null)
            {
                equalityComparer = EqualityComparer<TValue>.Default;
            }

            return dict1.All(
                pair =>
                    dict2.TryGetValue(pair.Key, out TValue val) &&
                    equalityComparer.Equals(pair.Value, val));
        }
    }
}
