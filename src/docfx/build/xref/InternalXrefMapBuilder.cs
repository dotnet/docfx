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
        private readonly ErrorBuilder _errors;
        private readonly TemplateEngine _templateEngine;
        private readonly DocumentProvider _documentProvider;
        private readonly MetadataProvider _metadataProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly Input _input;
        private readonly BuildScope _buildScope;
        private readonly JsonSchemaTransformer _jsonSchemaTransformer;

        public InternalXrefMapBuilder(
            ErrorBuilder errors,
            TemplateEngine templateEngine,
            DocumentProvider documentProvider,
            MetadataProvider metadataProvider,
            MonikerProvider monikerProvider,
            Input input,
            BuildScope buildScope,
            JsonSchemaTransformer jsonSchemaTransformer)
        {
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
                        var specs = LoadSchemaDocument(errors, token, file);
                        xrefs.AddRange(specs);
                        break;
                    }
                case FileFormat.Json:
                    {
                        var token = _input.ReadJson(errors, file);
                        var specs = LoadSchemaDocument(errors, token, file);
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

        private IReadOnlyList<InternalXrefSpec> LoadSchemaDocument(ErrorBuilder errors, JToken token, FilePath file)
        {
            var schema = _templateEngine.GetSchema(_documentProvider.GetMime(file));

            return _jsonSchemaTransformer.LoadXrefSpecs(errors, schema, file, token);
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

            // multiple uid conflicts without moniker range definition
            // log an warning and take the first one order by the declaring file
            var duplicatedSpecs = specsWithSameUid.Where(item => item.Monikers.Count == 0).ToArray();
            if (duplicatedSpecs.Length > 1)
            {
                var duplicatedSources = (from spec in duplicatedSpecs where spec.Uid.Source != null select spec.Uid.Source).ToArray();
                foreach (var spec in duplicatedSpecs)
                {
                    _errors.Add(Errors.Xref.DuplicateUid(spec.Uid, duplicatedSources));
                }
            }

            // uid conflicts with overlapping monikers
            // when an uid's monikers are completely same, log a 'duplicated-uid' warning
            var conflictsWithMoniker = specsWithSameUid.Where(x => x.Monikers.Count > 0).ToArray();
            InternalXrefSpec[] conflictWithoutDuplicated;
            if (CheckDuplicatedUidWithMonikers(conflictsWithMoniker, out var duplicatedUidSpecWithMonikers, out conflictWithoutDuplicated))
            {
                var duplicatedSourcesWithMonikers = (from spec in duplicatedUidSpecWithMonikers where spec.Uid.Source != null select spec.Uid.Source).ToArray();
                foreach (var spec in duplicatedUidSpecWithMonikers)
                {
                    _errors.Add(Errors.Xref.DuplicateUid(spec.Uid, duplicatedSourcesWithMonikers));
                }
            }

            // when the MonikerList is not equal but overlapped, log an moniker-overlapping warning and take the first one order by the declaring file
            if (CheckOverlappingMonikers(conflictWithoutDuplicated, out var overlappingMonikers))
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

        private static bool CheckDuplicatedUidWithMonikers(
            InternalXrefSpec[] conflictWithMonikers,
            out List<InternalXrefSpec> duplicatedUidSpec,
            out InternalXrefSpec[] conflictWithoutDuplicated)
        {
            var isDuplicated = false;

            var allNoDupMonikerDic = new Dictionary<MonikerList, InternalXrefSpec>();
            var duplicatedMonikerList = new HashSet<MonikerList>();
            duplicatedUidSpec = new List<InternalXrefSpec>();

            foreach (var spec in conflictWithMonikers)
            {
                if (allNoDupMonikerDic.Keys.Contains(spec.Monikers))
                {
                    isDuplicated = true;
                    duplicatedMonikerList.Add(spec.Monikers);
                    duplicatedUidSpec.Add(spec);
                    duplicatedUidSpec.Add(allNoDupMonikerDic[spec.Monikers]);
                }
                else
                {
                    allNoDupMonikerDic.Add(spec.Monikers, spec);
                }
            }

            // remove duplicated-uid specs in confictWithMonikers array
            conflictWithoutDuplicated = conflictWithMonikers.Where(spec => !duplicatedMonikerList.Contains(spec.Monikers)).ToArray();

            return isDuplicated;
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
