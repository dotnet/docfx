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
        public static IReadOnlyDictionary<string, InternalXrefSpec> Build(Context context)
        {
            var builder = new ListBuilder<InternalXrefSpec>();

            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(
                    context.ErrorLog,
                    context.BuildScope.GetFiles(ContentType.Page),
                    file => Load(context, builder, file));
            }

            var xrefmap =
                from spec in builder.ToList()
                group spec by spec.Uid.Value into g
                let uid = g.Key
                let spec = AggregateXrefSpecs(context, uid, g.ToArray())
                select (uid, spec);

            var result = xrefmap.ToDictionary(item => item.uid, item => item.spec);
            result.TrimExcess();

            return result;
        }

        private static void Load(Context context, ListBuilder<InternalXrefSpec> xrefs, FilePath path)
        {
            var file = context.DocumentProvider.GetDocument(path);
            if (file.ContentType != ContentType.Page)
            {
                return;
            }

            var errors = new List<Error>();
            switch (file.FilePath.Format)
            {
                case FileFormat.Markdown:
                    {
                        var (fileMetaErrors, fileMetadata) = context.MetadataProvider.GetMetadata(file.FilePath);
                        errors.AddRange(fileMetaErrors);
                        var (markdownErrors, spec) = LoadMarkdown(context, fileMetadata, file);
                        errors.AddRange(markdownErrors);
                        if (spec != null)
                        {
                            xrefs.Add(spec);
                        }
                        break;
                    }
                case FileFormat.Yaml:
                    {
                        var (yamlErrors, token) = context.Input.ReadYaml(file.FilePath);
                        errors.AddRange(yamlErrors);
                        var (schemaErrors, specs) = LoadSchemaDocument(context, token, file);
                        errors.AddRange(schemaErrors);
                        xrefs.AddRange(specs);
                        break;
                    }
                case FileFormat.Json:
                    {
                        var (jsonErrors, token) = context.Input.ReadJson(file.FilePath);
                        errors.AddRange(jsonErrors);
                        var (schemaErrors, specs) = LoadSchemaDocument(context, token, file);
                        errors.AddRange(schemaErrors);
                        xrefs.AddRange(specs);
                        break;
                    }
            }
            context.ErrorLog.Write(errors);
        }

        private static (List<Error> errors, InternalXrefSpec? spec) LoadMarkdown(Context context, UserMetadata metadata, Document file)
        {
            if (string.IsNullOrEmpty(metadata.Uid))
            {
                return (new List<Error>(), default);
            }

            var xref = new InternalXrefSpec(metadata.Uid, file.SiteUrl, file);
            xref.XrefProperties["name"] = new Lazy<JToken>(() => new JValue(string.IsNullOrEmpty(metadata.Title) ? metadata.Uid : metadata.Title));

            var (errors, monikers) = context.MonikerProvider.GetFileLevelMonikers(file.FilePath);
            xref.Monikers = monikers;
            return (errors, xref);
        }

        private static (List<Error> errors, IReadOnlyList<InternalXrefSpec> specs) LoadSchemaDocument(Context context, JToken token, Document file)
        {
            var schemaTemplate = context.TemplateEngine.GetSchema(file.Mime);

            return schemaTemplate.JsonSchemaTransformer.LoadXrefSpecs(file, context, token);
        }

        private static InternalXrefSpec AggregateXrefSpecs(Context context, string uid, InternalXrefSpec[] specsWithSameUid)
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
                    context.ErrorLog.Write(Errors.Xref.DuplicateUid(spec.Uid, duplicatedSources));
                }
            }

            // uid conflicts with overlapping monikers
            // log an warning and take the first one order by the declaring file
            var conflictsWithMoniker = specsWithSameUid.Where(x => x.Monikers.Count > 0).ToArray();
            if (CheckOverlappingMonikers(specsWithSameUid, out var overlappingMonikers))
            {
                context.ErrorLog.Write(Errors.Versioning.MonikerOverlapping(uid, specsWithSameUid.Select(spec => spec.DeclaringFile).ToList(), overlappingMonikers));
            }

            // uid conflicts with different values of the same xref property
            // log an warning and take the first one order by the declaring file
            var xrefProperties = specsWithSameUid.SelectMany(x => x.XrefProperties.Keys).Distinct();
            foreach (var xrefProperty in xrefProperties)
            {
                var conflictingNames = specsWithSameUid.Select(x => x.GetXrefPropertyValueAsString(xrefProperty)).Distinct();
                if (conflictingNames.Count() > 1)
                {
                    context.ErrorLog.Write(Errors.Xref.UidPropertyConflict(uid, xrefProperty, conflictingNames));
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
