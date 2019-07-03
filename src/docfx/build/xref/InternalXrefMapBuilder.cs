// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class InternalXrefMapBuilder
    {
        public static IReadOnlyDictionary<string, InternalXrefSpec[]> Build(Context context)
        {
            var builder = new ListBuilder<InternalXrefSpec>();

            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(
                    context.BuildScope.Files.Where(f => f.ContentType == ContentType.Page),
                    file => Load(context, builder, file),
                    Progress.Update);
            }

            var result =
                from spec in builder.ToList()
                group spec by spec.Uid into g
                let uid = g.Key
                let specs = AggregateXrefSpecs(context, uid, g.ToArray())
                where specs.Length > 0
                select (uid, specs);

            return result.ToDictionary(item => item.uid, item => item.specs);
        }

        private static void Load(Context context, ListBuilder<InternalXrefSpec> xrefs, Document file)
        {
            try
            {
                var errors = new List<Error>();
                var content = file.ReadText();
                var callStack = new List<Document> { file };
                if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
                {
                    var (fileMetaErrors, fileMetadata) = context.MetadataProvider.GetMetadata(file);
                    errors.AddRange(fileMetaErrors);

                    if (!string.IsNullOrEmpty(fileMetadata.Uid))
                    {
                        var (error, spec, _) = LoadMarkdown(context, fileMetadata, file);
                        errors.AddIfNotNull(error);
                        xrefs.Add(spec);
                    }
                }
                else if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
                {
                    var (yamlErrors, token) = YamlUtility.Parse(file, context);
                    errors.AddRange(yamlErrors);
                    var (schemaErrors, specs) = LoadSchemaDocument(context, token, file);
                    errors.AddRange(schemaErrors);
                    xrefs.AddRange(specs);
                }
                else if (file.FilePath.EndsWith(".json", PathUtility.PathComparison))
                {
                    var (jsonErrors, token) = JsonUtility.Parse(file, context);
                    errors.AddRange(jsonErrors);
                    var (schemaErrors, specs) = LoadSchemaDocument(context, token, file);
                    errors.AddRange(schemaErrors);
                    xrefs.AddRange(specs);
                }
                context.ErrorLog.Write(file.ToString(), errors);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.ErrorLog.Write(file.ToString(), dex.Error, isException: true);
            }
            catch
            {
                Console.WriteLine($"Load {file.FilePath} xref failed");
                throw;
            }
        }

        private static (Error error, InternalXrefSpec spec, Document doc) LoadMarkdown(Context context, InputMetadata metadata, Document file)
        {
            var xref = new InternalXrefSpec
            {
                Uid = metadata.Uid,
                Href = file.SiteUrl,
                DeclairingFile = file,
            };
            xref.ExtensionData["name"] = new Lazy<JToken>(() => new JValue(string.IsNullOrEmpty(metadata.Title) ? metadata.Uid : metadata.Title));

            var (error, monikers) = context.MonikerProvider.GetFileLevelMonikers(file);
            xref.Monikers = monikers.ToHashSet();
            return (error, xref, file);
        }

        private static (List<Error> errors, IReadOnlyList<InternalXrefSpec> specs) LoadSchemaDocument(Context context, JToken token, Document file)
        {
            var errors = new List<Error>();
            var schemaTemplate = context.TemplateEngine.GetJsonSchema(file.Mime);
            if (schemaTemplate is null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var (schemaErrors, xrefPropertiesGroupByUid) = schemaTemplate.JsonSchemaTransformer.TransformXref(file, context, token);
            errors.AddRange(schemaErrors);

            var specs = xrefPropertiesGroupByUid.Select(item =>
            {
                var (isRoot, properties) = item.Value;
                var xref = new InternalXrefSpec
                {
                    Uid = item.Key,
                    Href = isRoot ? file.SiteUrl : $"{file.SiteUrl}#{GetBookmarkFromUid(item.Key)}",
                    DeclairingFile = file,
                };
                xref.ExtensionData.AddRange(properties);
                return xref;
            }).ToList();

            return (errors, specs);

            string GetBookmarkFromUid(string uid)
                => Regex.Replace(uid, @"\W", "_");
        }

        private static void GetUids(Context context, string filePath, JObject token, Dictionary<string, string> uidToJsonPath, Dictionary<string, string> jsonPathToUid)
        {
            if (token is null)
                return;

            if (token.TryGetValue("uid", out var value) && value is JValue v && v.Value is string str)
            {
                if (!uidToJsonPath.TryAdd(str, token.Path))
                {
                    context.ErrorLog.Write(filePath, Errors.UidConflict(str));
                }
                else
                {
                    jsonPathToUid.TryAdd(token.Path, str);
                }
            }

            foreach (var item in token.Children())
            {
                var property = item as JProperty;
                if (property.Value is JObject obj)
                {
                    GetUids(context, filePath, obj, uidToJsonPath, jsonPathToUid);
                }

                if (property.Value is JArray array)
                {
                    foreach (var child in array.Children())
                    {
                        GetUids(context, filePath, child as JObject, uidToJsonPath, jsonPathToUid);
                    }
                }
            }
        }

        private static InternalXrefSpec[] AggregateXrefSpecs(Context context, string uid, InternalXrefSpec[] specsWithSameUid)
        {
            // no conflicts
            if (specsWithSameUid.Length <= 1)
            {
                return specsWithSameUid;
            }

            // multiple uid conflicts without moniker range definition, drop the uid and log an error
            var conflictsWithoutMoniker = specsWithSameUid.Where(item => item.Monikers.Count == 0).ToArray();
            if (conflictsWithoutMoniker.Length > 1)
            {
                var orderedConflict = conflictsWithoutMoniker.OrderBy(item => item.Href);
                context.ErrorLog.Write(Errors.UidConflict(uid, orderedConflict.Select(x => x.DeclairingFile.FilePath)));
                return Array.Empty<InternalXrefSpec>();
            }

            // uid conflicts with overlapping monikers, drop the uid and log an error
            var conflictsWithMoniker = specsWithSameUid.Where(x => x.Monikers.Count > 0).ToArray();
            if (CheckOverlappingMonikers(specsWithSameUid, out var overlappingMonikers))
            {
                context.ErrorLog.Write(Errors.MonikerOverlapping(overlappingMonikers));
                return Array.Empty<InternalXrefSpec>();
            }

            // Sort by monikers
            return conflictsWithoutMoniker.Concat(
                conflictsWithMoniker.OrderByDescending(
                    spec => spec.Monikers.First(), context.MonikerProvider.Comparer)).ToArray();
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
