// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Build.Engine;

public class SearchIndexItem
{
    [JsonProperty("href")]
    public string Href { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("keywords")]
    public string Keywords { get; set; }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as SearchIndexItem);
    }

    public bool Equals(SearchIndexItem other)
    {
        if (other == null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return string.Equals(this.Title, other.Title) && string.Equals(this.Href, other.Href) && string.Equals(this.Keywords, other.Keywords);
    }

    public override int GetHashCode()
    {
        return Title.GetHashCode() ^ Href.GetHashCode() ^ Keywords.GetHashCode();
    }
}
