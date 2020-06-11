// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class InternalXrefMapBuilder
    {
        public static IReadOnlyDictionary<string, InternalXrefSpec> Build(
            MarkdownEngine markdownEngine,
            LinkResolver linkResolver,
            XrefResolver xrefResolver,
            ErrorLog errorLog,
            TemplateEngine templateEngine,
            DocumentProvider documentProvider,
            MetadataProvider metadataProvider,
            MonikerProvider monikerProvider,
            Input input,
            BuildScope buildScope)
        {
            var builder = new ListBuilder<InternalXrefSpec>();

            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(
                    errorLog,
                    buildScope.GetFiles(ContentType.Page),
                    file => Load(builder, file, markdownEngine, linkResolver, xrefResolver, errorLog, templateEngine, documentProvider, metadataProvider, monikerProvider, input));
            }

            var result =
                from spec in builder.ToList()
                group spec by spec.Uid.Value into g
                let uid = g.Key
                let spec = AggregateXrefSpecs(errorLog, uid, g.ToArray())
                select (uid, spec);

            return result.ToDictionary(item => item.uid, item => item.spec);
        }

        private static void Load(
            ListBuilder<InternalXrefSpec> xrefs,
            FilePath path,
            MarkdownEngine markdownEngine,
            LinkResolver linkResolver,
            XrefResolver xrefResolver,
            ErrorLog errorLog,
            TemplateEngine templateEngine,
            DocumentProvider documentProvider,
            MetadataProvider metadataProvider,
            MonikerProvider monikerProvider,
            Input input)
        {
            var file = documentProvider.GetDocument(path);
            if (file.ContentType != ContentType.Page)
            {
                return;
            }

            var errors = new List<Error>();
            switch (file.FilePath.Format)
            {
                case FileFormat.Markdown:
                    {
                        var (fileMetaErrors, fileMetadata) = metadataProvider.GetMetadata(file.FilePath);
                        errors.AddRange(fileMetaErrors);
                        var (markdownErrors, spec) = LoadMarkdown(fileMetadata, file, monikerProvider);
                        errors.AddRange(markdownErrors);
                        if (spec != null)
                        {
                            xrefs.Add(spec);
                        }
                        break;
                    }
                case FileFormat.Yaml:
                    {
                        var (yamlErrors, token) = input.ReadYaml(file.FilePath);
                        errors.AddRange(yamlErrors);
                        var (schemaErrors, specs) = LoadSchemaDocument(token, file, markdownEngine, linkResolver, xrefResolver, errorLog, templateEngine);
                        errors.AddRange(schemaErrors);
                        xrefs.AddRange(specs);
                        break;
                    }
                case FileFormat.Json:
                    {
                        var (jsonErrors, token) = input.ReadJson(file.FilePath);
                        errors.AddRange(jsonErrors);
                        var (schemaErrors, specs) = LoadSchemaDocument(token, file, markdownEngine, linkResolver, xrefResolver, errorLog, templateEngine);
                        errors.AddRange(schemaErrors);
                        xrefs.AddRange(specs);
                        break;
                    }
            }
            errorLog.Write(errors);
        }

        private static (List<Error> errors, InternalXrefSpec? spec) LoadMarkdown(UserMetadata metadata, Document file, MonikerProvider monikerProvider)
        {
            if (string.IsNullOrEmpty(metadata.Uid))
            {
                return (new List<Error>(), default);
            }

            var xref = new InternalXrefSpec(metadata.Uid, file.SiteUrl, file);
            xref.XrefProperties["name"] = new Lazy<JToken>(() => new JValue(string.IsNullOrEmpty(metadata.Title) ? metadata.Uid : metadata.Title));

            var (errors, monikers) = monikerProvider.GetFileLevelMonikers(file.FilePath);
            xref.Monikers = monikers;
            return (errors, xref);
        }

        private static (List<Error> errors, IReadOnlyList<InternalXrefSpec> specs) LoadSchemaDocument(
            JToken token,
            Document file,
            MarkdownEngine markdownEngine,
            LinkResolver linkResolver,
            XrefResolver xrefResolver,
            ErrorLog errorLog,
            TemplateEngine templateEngine)
        {
            var schemaTemplate = templateEngine.GetSchema(file.Mime);

            return schemaTemplate.JsonSchemaTransformer.LoadXrefSpecs(file, token, markdownEngine, linkResolver, xrefResolver, errorLog);
        }

        private static InternalXrefSpec AggregateXrefSpecs(ErrorLog errorLog, string uid, InternalXrefSpec[] specsWithSameUid)
        {
            // no conflicts
            if (specsWithSameUid.Length == 1)
            {
                return specsWithSameUid.First();
            }

            // multiple uid conflicts without moniker range definition
            // log an warning and take the first one order by the declaring file
            var duplicatedSpecs = specsWithSameUid.Where(item => item.Monikers.Count == 0).ToArray();
            if (duplicatedSpecs.Length > 1)
            {
                var duplicatedSources = (from spec in duplicatedSpecs where spec.Uid.Source != null select spec.Uid.Source).ToArray();
                foreach (var spec in duplicatedSpecs)
                {
                    errorLog.Write(Errors.Xref.DuplicateUid(spec.Uid, duplicatedSources));
                }
            }

            // uid conflicts with overlapping monikers
            // log an warning and take the first one order by the declaring file
            var conflictsWithMoniker = specsWithSameUid.Where(x => x.Monikers.Count > 0).ToArray();
            if (CheckOverlappingMonikers(specsWithSameUid, out var overlappingMonikers))
            {
                errorLog.Write(Errors.Versioning.MonikerOverlapping(uid, specsWithSameUid.Select(spec => spec.DeclaringFile).ToList(), overlappingMonikers));
            }

            // uid conflicts with different values of the same xref property
            // log an warning and take the first one order by the declaring file
            var xrefProperties = specsWithSameUid.SelectMany(x => x.XrefProperties.Keys).Distinct();
            foreach (var xrefProperty in xrefProperties)
            {
                var conflictingNames = specsWithSameUid.Select(x => x.GetXrefPropertyValueAsString(xrefProperty)).Distinct();
                if (conflictingNames.Count() > 1)
                {
                    errorLog.Write(Errors.Xref.UidPropertyConflict(uid, xrefProperty, conflictingNames));
                }
            }

            return specsWithSameUid.OrderBy(spec => spec.DeclaringFile).First();
        }

        private static bool CheckOverlappingMonikers(IXrefSpec[] specsWithSameUid, out HashSet<string> overlappingMonikers)
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
