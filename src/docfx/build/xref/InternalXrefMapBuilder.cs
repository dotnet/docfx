// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class InternalXrefMapBuilder
    {
        private readonly Config _config;
        private readonly ErrorBuilder _errors;
        private readonly TemplateEngine _templateEngine;
        private readonly DocumentProvider _documentProvider;
        private readonly MetadataProvider _metadataProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly Input _input;
        private readonly BuildScope _buildScope;
        private readonly JsonSchemaTransformer _jsonSchemaTransformer;

        public InternalXrefMapBuilder(
            Config config,
            ErrorBuilder errors,
            TemplateEngine templateEngine,
            DocumentProvider documentProvider,
            MetadataProvider metadataProvider,
            MonikerProvider monikerProvider,
            Input input,
            BuildScope buildScope,
            JsonSchemaTransformer jsonSchemaTransformer)
        {
            _config = config;
            _errors = errors;
            _templateEngine = templateEngine;
            _documentProvider = documentProvider;
            _metadataProvider = metadataProvider;
            _monikerProvider = monikerProvider;
            _input = input;
            _buildScope = buildScope;
            _jsonSchemaTransformer = jsonSchemaTransformer;
        }

        public IReadOnlyDictionary<string, InternalXrefSpec[]> Build()
        {
            var builder = new ListBuilder<InternalXrefSpec>();

            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(
                    _errors,
                    _buildScope.GetFiles(ContentType.Page),
                    file => Load(_errors, builder, file));
            }

            var xrefmap =
                from spec in builder.AsList()
                group spec by spec.Uid.Value into g
                let uid = g.Key
                let spec = AggregateXrefSpecs(uid, g.ToArray())
                select (uid, spec);

            var result = xrefmap.ToDictionary(item => item.uid, item => item.spec);
            result.TrimExcess();

            return result;
        }

        private void Load(ErrorBuilder errors, ListBuilder<InternalXrefSpec> xrefs, FilePath file)
        {
            switch (file.Format)
            {
                case FileFormat.Markdown:
                    {
                        var fileMetadata = _metadataProvider.GetMetadata(errors, file);
                        var spec = LoadMarkdown(errors, fileMetadata, file);
                        if (spec != null)
                        {
                            xrefs.Add(spec);
                        }
                        break;
                    }
                case FileFormat.Yaml:
                    {
                        var token = _input.ReadYaml(errors, file);
                        var specs = _jsonSchemaTransformer.LoadXrefSpecs(errors, file, token);
                        xrefs.AddRange(specs);
                        break;
                    }
                case FileFormat.Json:
                    {
                        var token = _input.ReadJson(errors, file);
                        var specs = _jsonSchemaTransformer.LoadXrefSpecs(errors, file, token);
                        xrefs.AddRange(specs);
                        break;
                    }
            }
        }

        private InternalXrefSpec? LoadMarkdown(ErrorBuilder errors, UserMetadata metadata, FilePath file)
        {
            if (string.IsNullOrEmpty(metadata.Uid))
            {
                return default;
            }

            var monikers = _monikerProvider.GetFileLevelMonikers(errors, file);
            var xref = new InternalXrefSpec(metadata.Uid, _documentProvider.GetSiteUrl(file), file, monikers);

            xref.XrefProperties["name"] = new Lazy<JToken>(() => new JValue(string.IsNullOrEmpty(metadata.Title) ? metadata.Uid : metadata.Title.Value));

            return xref;
        }

        private InternalXrefSpec[] AggregateXrefSpecs(string uid, InternalXrefSpec[] specsWithSameUid)
        {
            // no conflicts
            if (specsWithSameUid.Length == 1)
            {
                return specsWithSameUid;
            }

            // loc override fallback uid
            if (specsWithSameUid.Any(spec => spec.DeclaringFile.Origin == FileOrigin.Main))
            {
                specsWithSameUid = specsWithSameUid.Where(spec => spec.DeclaringFile.Origin != FileOrigin.Fallback).ToArray();
            }

            // multiple uid conflicts
            // log an warning and take the first one order by the declaring file
            var duplicateGroups = specsWithSameUid.GroupBy(spec => spec.Monikers);
            var duplicateSpecs = new HashSet<InternalXrefSpec>();
            foreach (var monikerGroup in duplicateGroups)
            {
                var specsWithSameMonikerList = monikerGroup.ToList();
                if (specsWithSameMonikerList.Count > 1)
                {
                    var duplicateSource = (from spec in specsWithSameMonikerList where spec.Uid.Value != null select spec.Uid.Source).ToArray();

                    foreach (var spec in specsWithSameMonikerList)
                    {
                        duplicateSpecs.Add(spec);
                        _errors.Add(Errors.Xref.DuplicateUid(spec.Uid, duplicateSource, spec.PropertyPath)
                            .WithLevel(_config.RunLearnValidation ? ErrorLevel.Error : ErrorLevel.Warning));
                    }
                }
            }

            var conflictsWithoutDuplicated = specsWithSameUid.Where(spec => !duplicateSpecs.Contains(spec)).ToArray();

            // when the MonikerList is not equal but overlapped, log an moniker-overlapping warning and take the first one order by the declaring file
            if (CheckOverlappingMonikers(conflictsWithoutDuplicated, out var overlappingMonikers))
            {
                _errors.Add(Errors.Versioning.MonikerOverlapping(uid, specsWithSameUid.Select(spec => spec.DeclaringFile).ToList(), overlappingMonikers));
            }

            return specsWithSameUid
                   .OrderByDescending(spec => spec.Monikers.HasMonikers
                        ? spec.Monikers.Select(moniker => _monikerProvider.GetMonikerOrder(moniker)).Max()
                        : int.MaxValue)
                   .ThenBy(spec => spec.DeclaringFile)
                   .ToArray();
        }

        private static bool CheckOverlappingMonikers(InternalXrefSpec[] specsWithSameUid, out HashSet<string> overlappingMonikers)
        {
            var isOverlapping = false;
            overlappingMonikers = new HashSet<string>();
            var monikerHashSet = new HashSet<string>();

            foreach (var spec in specsWithSameUid)
            {
                foreach (var moniker in spec.Monikers)
                {
                    if (!monikerHashSet.Add(moniker))
                    {
                        overlappingMonikers.Add(moniker);
                        isOverlapping = true;
                    }
                }
            }
            return isOverlapping;
        }
    }
}
