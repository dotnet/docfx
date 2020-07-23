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
        private readonly ContentValidator _contentValidator;
        private readonly PublishUrlMap _publishUrlMapBuilder;
        private readonly DocumentProvider _documentProvider;
        private readonly SourceMap _sourceMap;

        private ConcurrentDictionary<FilePath, (JObject? metadata, string? outputPath)> _buildOutput =
            new ConcurrentDictionary<FilePath, (JObject? metadata, string? outputPath)>();

        public PublishModelBuilder(
            Config config,
            ErrorBuilder errors,
            MonikerProvider monikerProvider,
            BuildOptions buildOptions,
            ContentValidator contentValidator,
            PublishUrlMap publishUrlMapBuilder,
            DocumentProvider documentProvider,
            SourceMap sourceMap)
        {
            _config = config;
            _errors = errors;
            _monikerProvider = monikerProvider;
            _locale = buildOptions.Locale;
            _contentValidator = contentValidator;
            _publishUrlMapBuilder = publishUrlMapBuilder;
            _documentProvider = documentProvider;
            _sourceMap = sourceMap;
        }

        public void SetPublishItem(FilePath file, JObject? metadata, string? outputPath)
        {
            _buildOutput.TryAdd(file, (metadata, outputPath));
        }

        public (PublishModel, Dictionary<FilePath, PublishItem>) Build()
        {
            var publishItems = new Dictionary<FilePath, PublishItem>();
            foreach (var (url, sourcePath, monikers) in _publishUrlMapBuilder.GetPublishOutput())
            {
                var document = _documentProvider.GetDocument(sourcePath);
                var buildOutput = _buildOutput.TryGetValue(sourcePath, out var result);
                var publishItem = new PublishItem(
                    url,
                    buildOutput ? result.outputPath : null,
                    _sourceMap.GetOriginalFilePath(sourcePath) ?? sourcePath.Path,
                    _locale,
                    monikers,
                    _monikerProvider.GetConfigMonikerRange(sourcePath),
                    document.ContentType,
                    document.Mime,
                    _errors.FileHasError(sourcePath),
                    buildOutput ? RemoveComplexValue(result.metadata) : null);
                publishItems.Add(sourcePath, publishItem);
            }

            foreach (var (filePath, publishItem) in publishItems)
            {
                if (!publishItem.HasError)
                {
                    Telemetry.TrackBuildFileTypeCount(filePath, publishItem);
                    _contentValidator.ValidateManifest(filePath, publishItem);
                }
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
                items,
                monikerGroups);

            var fileManifests = publishItems.ToDictionary(item => item.Key, item => item.Value);

            return (model, fileManifests);
        }

        private JObject? RemoveComplexValue(JObject? metadata)
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
