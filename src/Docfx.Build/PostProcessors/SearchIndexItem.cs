// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Docfx.Build.Engine;

class SearchIndexItem
{
    [JsonPropertyName("href")]
    public string Href { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("keywords")]
    public string Keywords { get; set; }

    public override bool Equals(object obj)
    {
        return Equals(obj as SearchIndexItem);
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
        return string.Equals(Title, other.Title) && string.Equals(Href, other.Href) && string.Equals(Keywords, other.Keywords);
    }

    public override int GetHashCode()
    {
        return Title.GetHashCode() ^ Href.GetHashCode() ^ Keywords.GetHashCode();
    }
}
