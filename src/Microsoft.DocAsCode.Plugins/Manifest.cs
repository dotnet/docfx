// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Threading;

    using Newtonsoft.Json;

    public class Manifest
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly Dictionary<string, List<OutputFileInfo>> _index = new Dictionary<string, List<OutputFileInfo>>();

        public Manifest()
        {
            Files = new ManifestItemCollection();
            Files.CollectionChanged += FileCollectionChanged;
        }

        public Manifest(IEnumerable<ManifestItem> files)
            : this()
        {
            Files.AddRange(files);
        }

        [JsonProperty("templates")]
        public List<string> Templates { get; set; }

        [JsonProperty("homepages")]
        public List<HomepageInfo> Homepages { get; set; }

        [JsonProperty("source_base_path")]
        public string SourceBasePath { get; set; }

        [JsonProperty("xrefmap")]
        public object XRefMap { get; set; }

        [JsonProperty("files")]
        public ManifestItemCollection Files { get; }

        [JsonProperty("incremental_info")]
        public List<IncrementalInfo> IncrementalInfo { get; set; }

        [JsonProperty("version_info")]
        public Dictionary<string, VersionInfo> VersionInfo { get; set; }

        #region Public Methods

        public OutputFileInfo FindOutputFileInfo(string relativePath)
        {
            List<OutputFileInfo> list;
            _lock.EnterReadLock();
            try
            {
                _index.TryGetValue(relativePath, out list);
            }
            finally
            {
                _lock.ExitReadLock();
            }
            if (list?.Count > 0)
            {
                return list[0];
            }
            return null;
        }

        #endregion

        #region EventHandlers

        private void FileCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _lock.EnterWriteLock();
            try
            {
                if (e.NewItems != null)
                {
                    foreach (ManifestItem item in e.NewItems)
                    {
                        foreach (var ofi in item.OutputFiles.Values)
                        {
                            AddItem(ofi.RelativePath, ofi);
                            ofi.PropertyChanged += OutputFileInfoPropertyChanged;
                        }
                        ((INotifyCollectionChanged)item.OutputFiles).CollectionChanged += ManifestItemOutputChanged;
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (ManifestItem item in e.OldItems)
                    {
                        foreach (var ofi in item.OutputFiles.Values)
                        {
                            RemoveItem(ofi.RelativePath, ofi);
                            ofi.PropertyChanged -= OutputFileInfoPropertyChanged;
                        }
                       ((INotifyCollectionChanged)item.OutputFiles).CollectionChanged -= ManifestItemOutputChanged;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private void ManifestItemOutputChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _lock.EnterWriteLock();
            try
            {
                if (e.NewItems != null)
                {
                    foreach (KeyValuePair<string, OutputFileInfo> item in e.NewItems)
                    {
                        AddItem(item.Value.RelativePath, item.Value);
                        item.Value.PropertyChanged += OutputFileInfoPropertyChanged;
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (KeyValuePair<string, OutputFileInfo> item in e.OldItems)
                    {
                        RemoveItem(item.Value.RelativePath, item.Value);
                        item.Value.PropertyChanged -= OutputFileInfoPropertyChanged;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private void OutputFileInfoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var args = e as PropertyChangedEventArgs<string>;
            if (args == null)
            {
                return;
            }
            if (args.PropertyName != nameof(OutputFileInfo.RelativePath))
            {
                return;
            }
            _lock.EnterWriteLock();
            try
            {
                RemoveItem(args.Original, (OutputFileInfo)sender);
                AddItem(args.Current, (OutputFileInfo)sender);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #endregion

        #region Private Methods

        private void AddItem(string relativePath, OutputFileInfo item)
        {
            if (_index.TryGetValue(relativePath, out List<OutputFileInfo> list))
            {
                list.Add(item);
            }
            else
            {
                _index[relativePath] = new List<OutputFileInfo> { item };
            }
        }

        private void RemoveItem(string relativePath, OutputFileInfo item)
        {
            if (_index.TryGetValue(relativePath, out List<OutputFileInfo> list))
            {
                list.Remove(item);
            }
        }

        #endregion
    }
}
