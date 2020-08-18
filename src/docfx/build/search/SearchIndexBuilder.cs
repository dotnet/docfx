// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    internal class SearchIndexBuilder
    {
        private readonly Config _config;
        private readonly ErrorBuilder _errors;
        private readonly MetadataProvider _metadataProvider;
        private readonly ConcurrentDictionary<string, LunrSearchItem> _searchIndex = new ConcurrentDictionary<string, LunrSearchItem>();

        public SearchIndexBuilder(Config config, ErrorBuilder errors, MetadataProvider metadataProvider)
        {
            _config = config;
            _errors = errors;
            _metadataProvider = metadataProvider;
        }

        public void SetTitle(Document file, string? title)
        {
            if (string.IsNullOrEmpty(title) || !IsLunrSearchEnabled(file.FilePath))
            {
                return;
            }

            _searchIndex.GetOrAdd(file.SiteUrl, _ => new LunrSearchItem { Id = file.SiteUrl }).Title = title;
        }

        public void SetBody(Document file, string? body)
        {
            if (string.IsNullOrEmpty(body) || !IsLunrSearchEnabled(file.FilePath))
            {
                return;
            }

            _searchIndex.GetOrAdd(file.SiteUrl, _ => new LunrSearchItem { Id = file.SiteUrl }).Body = body;
        }

        public string? Build()
        {
            if (_searchIndex.IsEmpty)
            {
                return null;
            }

            var documents = JToken.FromObject(_searchIndex.Values);
            var js = JavaScriptEngine.Create();
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "data/scripts/lunr.interop.js");

            return js.Run(scriptPath, "transform", documents).ToString();
        }

        private bool IsLunrSearchEnabled(FilePath file)
        {
            if (_config.SearchEngine != SearchEngineType.Lunr)
            {
                return false;
            }

            var metadata = _metadataProvider.GetMetadata(_errors, file);
            if (metadata.Robots != null && metadata.Robots.Contains("noindex", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        private class LunrSearchItem
        {
            public string? Id { get; set; }

            public string? Title { get; set; }

            public string? Body { get; set; }
        }
    }
}
