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
    internal static class InternalXrefSpecBuilder
    {
        public static IReadOnlyDictionary<string, InternalXrefSpec[]> Build(Context context, Docset docset)
        {
            var result = new ListBuilder<InternalXrefSpec>();

            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(
                    docset.ScanScope.Where(f => f.ContentType == ContentType.Page),
                    file => Load(context, result, file),
                    Progress.Update);
            }

            return result.ToList().GroupBy(item => item.Uid).ToDictionary(g => g.Key, g => g.ToArray());
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
                    var (fileMetaErrors, _, fileMetadata) = context.MetadataProvider.GetMetadata(file);
                    errors.AddRange(fileMetaErrors);

                    if (!string.IsNullOrEmpty(fileMetadata.Uid))
                    {
                        var (error, spec) = LoadMarkdown(context, fileMetadata, file);
                        errors.AddIfNotNull(error);
                        xrefs.Add(spec);
                    }
                }
                else if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
                {
                    var (yamlErrors, token) = YamlUtility.Parse(file, context);
                    errors.AddRange(yamlErrors);
                    var (schemaErrors, specs) = LoadSchemaDocument(context, token as JObject, file);
                    errors.AddRange(schemaErrors);
                    xrefs.AddRange(specs);
                }
                else if (file.FilePath.EndsWith(".json", PathUtility.PathComparison))
                {
                    var (jsonErrors, token) = JsonUtility.Parse(file, context);
                    errors.AddRange(jsonErrors);
                    var (schemaErrors, specs) = LoadSchemaDocument(context, token as JObject, file);
                    errors.AddRange(schemaErrors);
                    xrefs.AddRange(specs);
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

        private static (Error error, InternalXrefSpec spec) LoadMarkdown(Context context, InputMetadata metadata, Document file)
        {
            var xref = new InternalXrefSpec
            {
                Uid = metadata.Uid,
                Href = file.CanonicalUrlWithoutLocale,
                DeclairingFile = file,
            };

            var (error, monikers) = context.MonikerProvider.GetFileLevelMonikers(file);

            xref.Monikers = monikers.ToArray();

            return (error, xref);
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
                    Href = isRoot ? file.CanonicalUrlWithoutLocale : $"{file.CanonicalUrlWithoutLocale}#{GetBookmarkFromUid(item.Key)}",
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
    }
}
