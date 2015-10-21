// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Utility;
    using Microsoft.CodeAnalysis;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Reflection;

    public class BuildInfo
    {
        public string BuildAssembly { get; set; }

        public string InputFilesKey { get; set; }

        public DateTime TriggeredUtcTime { get; set; }

        public DateTime CompleteUtcTime { get; set; }

        public string OutputFolder { get; set; }

        public IEnumerable<string> RelatvieOutputFiles { get; set; }

        /// <summary>
        /// Save the files involved in the build
        /// </summary>
        public IDictionary<string, List<string>> ContainedFiles { get; set; }

        public string CheckSum { get; set; }
    }

    public abstract class CacheBase
    {
        private static readonly int CleanupIntervalInDays = 5; // 5 days and clean up
        private static readonly int CleanupMaxCount = 100; // 100 items before clean up
        private static readonly int CleanupTo = 10; // clean up and keep latest 10 items
        private Dictionary<string, BuildInfo> _configs = new Dictionary<string, BuildInfo>();
        private readonly string _path;
        public static readonly string AssemblyName;
        static CacheBase()
        {
            AssemblyName = Assembly.GetExecutingAssembly().GetName().ToString();
        }

        public CacheBase(string path)
        {
            _path = path;

            _configs = ReadCacheFile(path);
        }

        public BuildInfo GetValidConfig(IEnumerable<string> inputProjects)
        {
            var key = inputProjects.GetNormalizedFullPathKey();
            return GetConfig(key);
        }

        public void SaveToCache(IEnumerable<string> inputProjects, IDictionary<string, List<string>> containedFiles, DateTime triggeredTime, string outputFolder, IList<string> fileRelativePaths)
        {
            var key = inputProjects.GetNormalizedFullPathKey();
            DateTime completeTime = DateTime.UtcNow;
            BuildInfo info = new BuildInfo
            {
                InputFilesKey = key,
                ContainedFiles = containedFiles,
                TriggeredUtcTime = triggeredTime,
                CompleteUtcTime = completeTime,
                OutputFolder = outputFolder.ToNormalizedFullPath(),
                RelatvieOutputFiles = fileRelativePaths.GetNormalizedPathList(),
                BuildAssembly = AssemblyName,
            };
            this.SaveConfig(key, info);
        }

        #region Virtual Methods
        protected virtual BuildInfo GetConfig(string key)
        {
            BuildInfo buildInfo = this.ReadConfig(key);
            if (buildInfo != null)
            {
                var checksum = buildInfo.CheckSum;
                try
                {
                    var resultCorrupted = GetMd5(buildInfo.OutputFolder, buildInfo.RelatvieOutputFiles) != checksum;

                    if (!resultCorrupted && checksum != null)
                    {
                        return buildInfo;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Warning, $"Cache for {key} in {_path} is corrupted");
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warning, $"Cache for {key} in {_path} is not valid: {e.Message}");
                }
            }

            return null;
        }

        protected virtual BuildInfo ReadConfig(string key)
        {
            BuildInfo info;
            if (_configs.TryGetValue(key, out info)) return info;
            return null;
        }

        protected virtual void SaveConfig(string key, BuildInfo config)
        {
            config.CheckSum = GetMd5(config.OutputFolder, config.RelatvieOutputFiles);
            _configs[key] = config;
            CleanupConfig();

            JsonUtility.Serialize(_path, _configs);
        }

        protected virtual void CleanupConfig()
        {
            // Copy oldkeys to a new list
            var oldKeys = _configs.Where(s => s.Value.TriggeredUtcTime.CompareTo(DateTime.UtcNow.AddDays(-CleanupIntervalInDays)) < 1).ToList();
            foreach (var key in oldKeys)
            {
                _configs.Remove(key.Key);
            }


            if (_configs.Count > CleanupMaxCount)
            {
                var cleanUpTo = Math.Min(CleanupMaxCount, CleanupTo);
                // Cleanup the old ones
                _configs = _configs.OrderByDescending(s => s.Value.TriggeredUtcTime).Take(cleanUpTo).ToDictionary(s => s.Key, s => s.Value);
            }
        }
        #endregion

        #region Private Methodes
        private static Dictionary<string, BuildInfo> ReadCacheFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return JsonUtility.Deserialize<Dictionary<string, BuildInfo>>(path);
                }
            }
            catch
            {
            } 

            return new Dictionary<string, BuildInfo>();
        }

        private static string GetMd5(string rootFolder, IEnumerable<string> relativeFilePath)
        {
            if (relativeFilePath == null) return null;
            var files = (from p in relativeFilePath select Path.Combine(rootFolder, p)).ToList();

            MD5 md5 = MD5.Create();

            using (FileCollectionStream reader = new FileCollectionStream(files))
            {
                var hash = md5.ComputeHash(reader);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        class FileCollectionStream : Stream
        {
            private IEnumerator<string> _fileEnumerator;
            private FileStream _stream;

            public FileCollectionStream(IEnumerable<string> files)
            {
                if (files == null) _fileEnumerator = null;
                else _fileEnumerator = files.GetEnumerator();
            }

            public override bool CanRead
            {
                get
                {
                    return true;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return false;
                }
            }

            public override long Length
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotSupportedException();
                }

                set
                {
                    throw new NotSupportedException();
                }
            }

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_fileEnumerator == null) return 0;

                if (_stream == null)
                {
                    if (!TryGetNextFileStream(out _stream)) return 0;
                }

                int readed;
                while (true)
                {
                    readed = _stream.Read(buffer, offset, count);
                    if (readed == 0)
                    {
                        // Dispose current stream before fetching the next one
                        _stream.Dispose();
                        if (!TryGetNextFileStream(out _stream)) return 0;
                    }
                    else
                    {
                        return readed;
                    }
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (_fileEnumerator != null) _fileEnumerator.Dispose();
                    if (_stream != null) _stream.Dispose();
                }

                base.Dispose(disposing);
            }

            private bool TryGetNextFileStream(out FileStream stream)
            {
                var next = _fileEnumerator.MoveNext();
                if (!next)
                {
                    stream = null;
                    return false;
                }

                stream = new FileStream(_fileEnumerator.Current, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return true;
            }
        }

        #endregion
    }

    public class ProjectLevelCache : CacheBase
    {
        private static Func<string, string> GetProjectLevelConfig = projectPath => Path.Combine(Path.GetDirectoryName(projectPath) ?? string.Empty, "obj", "xdoc", "cache", "obj", ".inter").ToNormalizedFullPath();
        private static ConcurrentDictionary<string, ProjectLevelCache> _cache = new ConcurrentDictionary<string, ProjectLevelCache>();
        public readonly string OutputFolder;

        private ProjectLevelCache(string projectPath) : base(projectPath)
        {
            OutputFolder = Path.GetDirectoryName(Path.GetFullPath(projectPath)).ToNormalizedFullPath();
        }

        public static ProjectLevelCache Get(IEnumerable<string> files)
        {
            if (files == null || !files.Any()) return null;
            var normalizedFiles = files.GetNormalizedFullPathList();

            // Use the folder for the first file as the cache folder
            var firstFile = normalizedFiles.First();

            string path = GetProjectLevelConfig(firstFile);
            return _cache.GetOrAdd(path, new ProjectLevelCache(path));
        }
    }

    public class ApplicationLevelCache : CacheBase
    {
        private static Func<string, string, string> GetApplicationLevelCacheFilePath = (projectPath, fileName) => Path.Combine(Path.GetDirectoryName(projectPath) ?? string.Empty, "obj", "xdoc", "cache", "final", fileName);
        private static ConcurrentDictionary<string, ApplicationLevelCache> _cache = new ConcurrentDictionary<string, ApplicationLevelCache>();

        private ApplicationLevelCache(string projectPath) : base(projectPath)
        {
        }

        public static ApplicationLevelCache Get(IEnumerable<string> files)
        {
            string path = GetApplicationLevelCache(files);
            if (string.IsNullOrEmpty(path)) return null;
            return _cache.GetOrAdd(path, new ApplicationLevelCache(path));
        }

        private static string GetApplicationLevelCache(IEnumerable<string> files)
        {
            if (files == null || !files.Any()) return null;
            var normalizedFiles = files.GetNormalizedFullPathList();

            // Application Level cache locates in the same folder with the top one file in the file list
            var firstFile = normalizedFiles.First();

            // Use hash code
            var fileName = files.GetNormalizedFullPathKey().GetHashCode().ToString();
            return GetApplicationLevelCacheFilePath(firstFile, fileName);
        }
    }
}
