// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;
    using System.ComponentModel;

    using Newtonsoft.Json;

    public class OutputFileInfo : INotifyPropertyChanged
    {
        private string _relativePath;
        private string _linkToPath;
        private string _hash;

        [JsonProperty("relative_path")]
        public string RelativePath
        {
            get { return _relativePath; }
            set
            {
                var o = _relativePath;
                _relativePath = value;
                OnPropertyChanged(nameof(RelativePath), o, value);
            }
        }

        [JsonProperty("link_to_path")]
        public string LinkToPath
        {
            get { return _linkToPath; }
            set
            {
                var o = _linkToPath;
                _linkToPath = value;
                OnPropertyChanged(nameof(LinkToPath), o, value);
            }
        }

        [JsonProperty("hash")]
        public string Hash
        {
            get { return _hash; }
            set
            {
                var o = _hash;
                _hash = value;
                OnPropertyChanged(nameof(Hash), o, value);
            }
        }

        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName, string original, string current)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs<string>(propertyName, original, current));
        }
    }
}
