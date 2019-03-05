// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class MonikerProvider
    {
        private readonly List<(Func<string, bool> glob, string monikerRange)> _rules = new List<(Func<string, bool>, string)>();
        private readonly MonikerRangeParser _rangeParser;

        public MonikerComparer Comparer { get; }

        private MonikerProvider(Docset docset, MonikerDefinitionModel monikerDefinition)
        {
            foreach (var (key, monikerRange) in docset.Config.MonikerRange)
            {
                _rules.Add((GlobUtility.CreateGlobMatcher(key), monikerRange));
            }
            _rules.Reverse();

            _rangeParser = new MonikerRangeParser(monikerDefinition);
            Comparer = new MonikerComparer(monikerDefinition);
        }

        public static async Task<MonikerProvider> Create(Docset docset)
        {
            var monikerDefinition = new MonikerDefinitionModel();
            if (!string.IsNullOrEmpty(docset.Config.MonikerDefinition))
            {
                var (_, content, _) = await docset.GetRestoredFileContent(docset.Config.MonikerDefinition);
                monikerDefinition = JsonUtility.Deserialize<MonikerDefinitionModel>(content);
            }

            return new MonikerProvider(docset, monikerDefinition);
        }

        public (List<Error> errors, List<string> monikers) GetFileLevelMonikers(Document file, MetadataProvider metadataProvider)
        {
            var errors = new List<Error>();
            var (metaErrors, metadata) = metadataProvider.GetMetadata<FileMetadata>(file, null);
            errors.AddRange(metaErrors);

            var (monikerError, monikers) = GetFileLevelMonikers(file, metadata.MonikerRange);
            errors.AddIfNotNull(monikerError);

            return (errors, monikers);
        }

        public (Error, List<string>) GetFileLevelMonikers(Document file, string fileMonikerRange = null)
        {
            Error error = null;
            string configMonikerRange = null;
            var configMonikers = new List<string>();
            foreach (var (glob, monikerRange) in _rules)
            {
                if (glob(file.FilePath))
                {
                    configMonikerRange = monikerRange;
                    configMonikers.AddRange(_rangeParser.Parse(monikerRange));
                    break;
                }
            }

            if (!string.IsNullOrEmpty(fileMonikerRange))
            {
                // Moniker range not defined in docfx.yml/docfx.json,
                // user should not define it in file metadata
                if (configMonikers.Count == 0)
                {
                    error = Errors.MonikerConfigMissing();
                    return (error, configMonikers);
                }

                var fileMonikers = _rangeParser.Parse(fileMonikerRange);
                var intersection = configMonikers.Intersect(fileMonikers, StringComparer.OrdinalIgnoreCase).ToList();

                // With non-empty config monikers,
                // warn if no intersection of config monikers and file monikers
                if (intersection.Count == 0)
                {
                    error = Errors.EmptyMonikers($"No moniker intersection between docfx.yml/docfx.json and file metadata. Config moniker range '{configMonikerRange}' is '{string.Join(',', configMonikers)}', while file moniker range '{fileMonikerRange}' is '{string.Join(',', fileMonikers)}'");
                }
                intersection.Sort(Comparer);
                return (error, intersection);
            }

            configMonikers.Sort(Comparer);
            return (error, configMonikers);
        }

        public List<string> GetZoneMonikers(string rangeString, List<string> fileLevelMonikers, List<Error> errors)
        {
            var monikers = new List<string>();

            // Moniker range not defined in docfx.yml/docfx.json,
            // User should not define it in moniker zone
            if (fileLevelMonikers.Count == 0)
            {
                errors.Add(Errors.MonikerConfigMissing());
                return new List<string>();
            }

            var zoneLevelMonikers = _rangeParser.Parse(rangeString);
            monikers = fileLevelMonikers.Intersect(zoneLevelMonikers, StringComparer.OrdinalIgnoreCase).ToList();

            if (monikers.Count == 0)
            {
                errors.Add(Errors.EmptyMonikers($"No intersection between zone and file level monikers. The result of zone level range string '{rangeString}' is '{string.Join(',', zoneLevelMonikers)}', while file level monikers is '{string.Join(',', fileLevelMonikers)}'."));
            }
            monikers.Sort(Comparer);
            return monikers;
        }
    }
}
