// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Plugins;

public class OutputFileInfo : INotifyPropertyChanged
{
    private string _relativePath;
    private string _linkToPath;

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

    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName, string original, string current)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs<string>(propertyName, original, current));
    }
}
