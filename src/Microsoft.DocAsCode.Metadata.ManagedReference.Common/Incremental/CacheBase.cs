// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Reflection;

    using Microsoft.DocAsCode.Common;

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

        public BuildInfo GetValidConfig(string key)
        {
            return GetConfig(key);
        }

        public BuildInfo GetValidConfig(IEnumerable<string> inputProjects)
        {
            var key = StringExtension.GetNormalizedFullPathKey(inputProjects);
            return GetConfig(key);
        }

        public void SaveToCache(string key, IDictionary<string, List<string>> containedFiles, DateTime triggeredTime, string outputFolder, IList<string> fileRelativePaths, ExtractMetadataOptions options)
        {
            DateTime completeTime = DateTime.UtcNow;
            BuildInfo info = new BuildInfo
            {
                InputFilesKey = key,
                ContainedFiles = containedFiles,
                TriggeredUtcTime = triggeredTime,
                CompleteUtcTime = completeTime,
                OutputFolder = StringExtension.ToNormalizedFullPath(outputFolder),
                RelativeOutputFiles = StringExtension.GetNormalizedPathList(fileRelativePaths),
                BuildAssembly = AssemblyName,
                Options = options,
            };
            SaveConfig(key, info);
        }

        public void SaveToCache(IEnumerable<string> inputProjects, IDictionary<string, List<string>> containedFiles, DateTime triggeredTime, string outputFolder, IList<string> fileRelativePaths, ExtractMetadataOptions options)
        {
            var key = StringExtension.GetNormalizedFullPathKey(inputProjects);
            SaveToCache(key, containedFiles, triggeredTime, outputFolder, fileRelativePaths, options);
        }

        public void SaveToCache(string key, IEnumerable<string> containedFiles, DateTime triggeredTime, string outputFolder, IList<string> fileRelativePaths, ExtractMetadataOptions options)
        {
            var dict = new Dictionary<string, List<string>> { { key, containedFiles.ToList() } };
            SaveToCache(key, dict, triggeredTime, outputFolder, fileRelativePaths, options);
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
                    var resultCorrupted = GetMd5(buildInfo.OutputFolder, buildInfo.RelativeOutputFiles) != checksum;

                    if (!resultCorrupted && checksum != null)
                    {
                        return buildInfo;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Info, $"Cache for {key} in {_path} is corrupted, rebuild...");
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Info, $"Cache for {key} in {_path} is not valid: {e.Message}, rebuild...");
                }
            }

            return null;
        }

        protected virtual BuildInfo ReadConfig(string key)
        {
            _configs.TryGetValue(key, out BuildInfo info);
            return info;
        }

        protected virtual void SaveConfig(string key, BuildInfo config)
        {
            config.CheckSum = GetMd5(config.OutputFolder, config.RelativeOutputFiles);
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
            catch (Exception e)
            {
                Logger.LogWarning($"Cache file {path} is invalid, ignored: {e.Message}.");
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

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override void Flush() => throw new NotSupportedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_fileEnumerator == null)
                {
                    return 0;
                }

                if (_stream == null && !TryGetNextFileStream(out _stream))
                {
                    return 0;
                }

                int readed;
                while (true)
                {
                    readed = _stream.Read(buffer, offset, count);
                    if (readed == 0)
                    {
                        // Dispose current stream before fetching the next one
                        _stream.Dispose();
                        if (!TryGetNextFileStream(out _stream))
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        return readed;
                    }
                }
            }

            public override long Seek(long offset, SeekOrigin origin)=>
                throw new NotSupportedException();

            public override void SetLength(long value) =>
                throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (_fileEnumerator != null)
                    {
                        _fileEnumerator.Dispose();
                    }
                    if (_stream != null)
                    {
                        _stream.Dispose();
                    }
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
}
