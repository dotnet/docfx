// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Newtonsoft.Json;

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
}