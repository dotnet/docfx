// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class PublishModelBuilder
    {
        private readonly Config _config;
        private readonly ErrorBuilder _errors;
        private readonly MonikerProvider _monikerProvider;
        private readonly string _locale;
        private readonly PublishUrlMap _publishUrlMap;
        private readonly SourceMap _sourceMap;
        private readonly LinkResolver _linkResolver;

        private readonly ConcurrentDictionary<FilePath, (JObject? metadata, string? outputPath)> _buildOutput =
                     new ConcurrentDictionary<FilePath, (JObject? metadata, string? outputPath)>();

        public PublishModelBuilder(
            Config config,
            ErrorBuilder errors,
            MonikerProvider monikerProvider,
            BuildOptions buildOptions,
            PublishUrlMap publishUrlMap,
            SourceMap sourceMap,
            LinkResolver linkResolver)
        {
            _config = config;
            _errors = errors;
            _monikerProvider = monikerProvider;
            _locale = buildOptions.Locale;
            _publishUrlMap = publishUrlMap;
            _sourceMap = sourceMap;
            _linkResolver = linkResolver;
        }

        public void SetPublishItem(FilePath file, JObject? metadata, string? outputPath)
        {
            _buildOutput.TryAdd(file, (metadata, outputPath));
        }

        public (PublishModel, Dictionary<FilePath, PublishItem>) Build()
        {
            var publishItems = new Dictionary<FilePath, PublishItem>();
            var additionalResource = _linkResolver.GetAdditionalResources().Select(_publishUrlMap.GeneratePublishUrlMapItem);
            foreach (var (url, sourcePath, monikers) in
                _publishUrlMap.GetPublishOutput().Concat(additionalResource.Select(x => (x.Url, x.SourcePath, x.Monikers))))
            {
                var buildOutput = _buildOutput.TryGetValue(sourcePath, out var result);
                var publishItem = new PublishItem(
                    url,
                    buildOutput ? result.outputPath : null,
                    _sourceMap.GetOriginalFilePath(sourcePath)?.Path ?? sourcePath.Path,
                    _locale,
                    monikers,
                    _monikerProvider.GetConfigMonikerRange(sourcePath),
                    _errors.FileHasError(sourcePath),
                    buildOutput ? RemoveComplexValue(result.metadata) : null);
                publishItems.Add(sourcePath, publishItem);
            }

            var items = (
                   from item in publishItems.Values
                   orderby item.Locale, item.Path, item.Url, item.MonikerGroup
                   select item).ToArray();

            var monikerGroups = new Dictionary<string, MonikerList>(
                from item in publishItems.Values
                let monikerGroup = item.MonikerGroup
                where !string.IsNullOrEmpty(monikerGroup)
                orderby monikerGroup
                group item by monikerGroup into g
                select new KeyValuePair<string, MonikerList>(g.Key, g.First().Monikers));

            var model = new PublishModel(
                _config.Name,
                _config.Product,
                _config.BasePath.ValueWithLeadingSlash,
                _config.Template.IsMainOrMaster ? null : _config.Template.Branch,
                items,
                monikerGroups);

            var fileManifests = publishItems.ToDictionary(item => item.Key, item => item.Value);

            return (model, fileManifests);
        }

        private static JObject? RemoveComplexValue(JObject? metadata)
        {
            if (metadata is null)
            {
                return null;
            }

            var keysToRemove = default(List<string>);

            foreach (var (key, value) in metadata)
            {
                if (value is JObject)
                {
                    keysToRemove ??= new List<string>();
                    keysToRemove.Add(key);
                    continue;
                }

                if (value is JArray array && !array.All(item => item is JValue))
                {
                    keysToRemove ??= new List<string>();
                    keysToRemove.Add(key);
                    continue;
                }
            }

            if (keysToRemove != null)
            {
                foreach (var key in keysToRemove)
                {
                    metadata.Remove(key);
                }
            }

            return metadata;
        }
    }
}
