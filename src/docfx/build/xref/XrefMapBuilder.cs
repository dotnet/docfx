// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class XrefMapBuilder
    {
        public static XrefMap Build(Context context, Docset docset)
        {
            var externalXrefMap = new Dictionary<string, Lazy<IXrefSpec>>();
            foreach (var url in docset.Config.Xref)
            {
                XrefMapModel xrefMap = new XrefMapModel();
                if (url.Value.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                {
                    var (_, content, _) = RestoreMap.GetRestoredFileContent(docset, url);
                    xrefMap = YamlUtility.Deserialize<XrefMapModel>(content, url);
                    foreach (var spec in xrefMap.References)
                    {
                        externalXrefMap.TryAdd(spec.Uid, new Lazy<IXrefSpec>(() => spec));
                    }
                }
                else
                {
                    // Convert to array since ref local is not allowed to be used in lambda expression
                    //
                    // TODO: It is very easy to forget passing fallbackDocsetPath, the RestoreMap interface needs improvement
                    var filePath = RestoreMap.GetRestoredFilePath(docset.DocsetPath, url, docset.FallbackDocset?.DocsetPath);
                    var result = XrefMapLoader.Load(filePath);
                    foreach (var (uid, spec) in result.ToList())
                    {
                        // for same uid with multiple specs, we should respect the order of the list
                        externalXrefMap.TryAdd(uid, spec);
                    }
                }
            }
            var internalXrefMap = CreateInternalXrefMap(context);
            return new XrefMap(context, BuildMap(externalXrefMap, internalXrefMap), internalXrefMap);
        }

        private static Dictionary<string, List<Lazy<IXrefSpec>>> BuildMap(IReadOnlyDictionary<string, Lazy<IXrefSpec>> externalXrefMap, Dictionary<string, List<Lazy<IXrefSpec>>> internalXrefMap)
        {
            var map = new Dictionary<string, List<Lazy<IXrefSpec>>>();
            map.AddRange(internalXrefMap);

            foreach (var (key, value) in externalXrefMap)
            {
                if (!map.TryGetValue(key, out var specs))
                {
                    map[key] = new List<Lazy<IXrefSpec>> { value };
                }
            }
            return map;
        }

        private static Dictionary<string, List<Lazy<IXrefSpec>>> CreateInternalXrefMap(Context context)
        {
            var xrefsByUid = new ConcurrentDictionary<string, ConcurrentBag<IXrefSpec>>();
            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(context.BuildScope.Files.Where(f => f.ContentType == ContentType.Page), file => Load(context, xrefsByUid, file), Progress.Update);
                return xrefsByUid.ToList().OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Value.Select(x => new Lazy<IXrefSpec>(() => x)).ToList());
            }
        }

        private static void Load(
            Context context,
            ConcurrentDictionary<string, ConcurrentBag<IXrefSpec>> xrefsByUid,
            Document file)
        {
            try
            {
                var errors = new List<Error>();
                var content = file.ReadText();
                var callStack = new List<Document> { file };
                if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
                {
                    var (fileMetaErrors, _, fileMetadata) = context.MetadataProvider.GetMetadata(file);
                    errors.AddRange(fileMetaErrors);

                    if (!string.IsNullOrEmpty(fileMetadata.Uid))
                    {
                        var (error, spec, _) = LoadMarkdown(context, fileMetadata, file);
                        errors.AddIfNotNull(error);
                        TryAddXref(xrefsByUid, fileMetadata.Uid, spec);
                    }
                }
                else if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
                {
                    var (yamlErrors, token) = YamlUtility.Parse(file, context);
                    errors.AddRange(yamlErrors);
                    var (schemaErrors, specs) = LoadSchemaDocument(context, token as JObject, file);
                    errors.AddRange(schemaErrors);
                    foreach (var spec in specs)
                    {
                        TryAddXref(xrefsByUid, spec.Uid, spec);
                    }
                }
                else if (file.FilePath.EndsWith(".json", PathUtility.PathComparison))
                {
                    var (jsonErrors, token) = JsonUtility.Parse(file, context);
                    errors.AddRange(jsonErrors);
                    var (schemaErrors, specs) = LoadSchemaDocument(context, token as JObject, file);
                    errors.AddRange(schemaErrors);
                    foreach (var spec in specs)
                    {
                        TryAddXref(xrefsByUid, spec.Uid, spec);
                    }
                }
                context.ErrorLog.Write(file.ToString(), errors);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.ErrorLog.Write(file.ToString(), dex.Error);
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
            foreach (var moniker in monikers)
            {
                xref.Monikers.Add(moniker);
            }
            return (error, xref, file);
        }

        private static (List<Error> errors, IReadOnlyList<InternalXrefSpec> specs) LoadSchemaDocument(Context context, JObject obj, Document file)
        {
            var errors = new List<Error>();
            var (schemaValidator, schemaTransformer) = TemplateEngine.GetJsonSchema(file.Mime);
            if (schemaValidator is null || schemaTransformer is null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var (schemaErrors, xrefPropertiesGroupByUid) = schemaTransformer.TransformXref(file, context, obj);
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

        private static void TryAddXref(ConcurrentDictionary<string, ConcurrentBag<IXrefSpec>> xrefsByUid, string uid, InternalXrefSpec spec)
        {
            if (spec is null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            xrefsByUid.GetOrAdd(uid, _ => new ConcurrentBag<IXrefSpec>()).Add(spec);
        }
    }
}
