// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class XrefMapBuilder
    {
        public static XrefMap Build(Context context, Docset docset)
        {
            // TODO: not considering same uid with multiple specs, it will be Dictionary<string, List<T>>
            // https://github.com/dotnet/corefx/issues/12067
            // Prefer Dictionary with manual lock to ConcurrentDictionary while only adding
            var externalXrefMap = new DictionaryBuilder<string, Lazy<IXrefSpec>>();
            ParallelUtility.ForEach(docset.Config.Xref, url =>
            {
                var (_, content, _) = RestoreMap.GetRestoredFileContent(docset, url);
                XrefMapModel xrefMap = new XrefMapModel();
                if (url?.Value.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) != false)
                {
                    xrefMap = YamlUtility.Deserialize<XrefMapModel>(content, url);
                    foreach (var spec in xrefMap.References)
                    {
                        externalXrefMap.TryAdd(spec.Uid, new Lazy<IXrefSpec>(() => spec));
                    }
                }
                else
                {
                    DeserializeAndPopulateXrefMap(
                    (uid, startLine, startColumn, endLine, endColumn) =>
                    {
                        externalXrefMap.TryAdd(uid, new Lazy<IXrefSpec>(() =>
                        {
                            var str = GetSubstringFromContent(content, startLine, endLine, startColumn, endColumn);
                            return JsonUtility.Deserialize<ExternalXrefSpec>(str, url);
                        }));
                    }, content);
                }
            });
            var internalXrefMap = CreateInternalXrefMap(context, docset.ScanScope);
            return new XrefMap(context, BuildMap(externalXrefMap.ToDictionary(), internalXrefMap), internalXrefMap);
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

        private static void DeserializeAndPopulateXrefMap(Action<string, int, int, int, int> populate, string content)
        {
            using (var reader = new StringReader(content))
            using (var json = new JsonTextReader(reader))
            {
                var currentProperty = string.Empty;
                string uid = null;
                var startLine = 1;
                var endLine = 1;
                var startColumn = 1;
                var endColumn = 1;
                while (json.Read())
                {
                    if (json.Value != null)
                    {
                        if (json.TokenType == JsonToken.PropertyName)
                            currentProperty = json.Value.ToString();

                        if (json.TokenType == JsonToken.String && currentProperty == "uid")
                        {
                            uid = json.Value.ToString();
                        }
                    }
                    else
                    {
                        if (json.TokenType == JsonToken.StartObject)
                        {
                            startLine = json.LineNumber;
                            startColumn = json.LinePosition + 1;
                        }
                        else if (json.TokenType == JsonToken.EndObject)
                        {
                            endLine = json.LineNumber;
                            endColumn = json.LinePosition + 1;
                            if (uid != null)
                            {
                                populate(uid, startLine, endLine, startColumn, endColumn);
                                uid = null;
                            }
                        }
                    }
                }
            }
        }

        private static string GetSubstringFromContent(string content, int startLine, int startColumn, int endLine, int endColumn)
        {
            var result = new StringBuilder();
            var currentLine = 1;
            var currentColumn = 1;

            // for better performance by accessing index when content is 1 line
            if (currentLine == startLine && currentLine == endLine)
            {
                return content.Substring(startColumn - 1, endColumn - startColumn + 1);
            }

            foreach (var ch in content)
            {
                if (ch == '\n')
                {
                    currentLine += 1;
                    currentColumn = 1;
                }

                // start and end in the same line
                if (currentLine == startLine && currentLine == endLine)
                {
                    if (currentColumn >= startColumn && currentColumn <= endColumn)
                    {
                        result.Append(ch);
                    }
                }

                // start and end in multiple lines
                else
                {
                    if ((currentLine == startLine && currentColumn >= startColumn)
                        || (currentLine == endLine && currentColumn <= endColumn)
                        || (currentLine > startLine && currentLine < endLine))
                    {
                        result.Append(ch);
                    }
                }

                currentColumn += 1;
            }
            return result.ToString();
        }

        private static Dictionary<string, List<Lazy<IXrefSpec>>>
            CreateInternalXrefMap(Context context, IEnumerable<Document> files)
        {
            var xrefsByUid = new ConcurrentDictionary<string, ConcurrentBag<IXrefSpec>>();
            Debug.Assert(files != null);
            using (Progress.Start("Building Xref map"))
            {
                ParallelUtility.ForEach(files.Where(f => f.ContentType == ContentType.Page), file => Load(context, xrefsByUid, file), Progress.Update);
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
                    var (yamlHeaderErrors, yamlHeader) = ExtractYamlHeader.Extract(file, context);
                    errors.AddRange(yamlHeaderErrors);

                    var (fileMetaErrors, fileMetadata) = context.MetadataProvider.GetInputMetadata<InputMetadata>(file, yamlHeader);
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
                Href = file.CanonicalUrlWithoutLocale,
                DeclairingFile = file,
            };
            xref.ExtensionData["name"] = new Lazy<JValue>(() => new JValue(string.IsNullOrEmpty(metadata.Title) ? metadata.Uid : metadata.Title));

            var (error, monikers) = context.MonikerProvider.GetFileLevelMonikers(file, metadata.MonikerRange);
            foreach (var moniker in monikers)
            {
                xref.Monikers.Add(moniker);
            }
            return (error, xref, file);
        }

        private static (List<Error> errors, IReadOnlyList<InternalXrefSpec> specs) LoadSchemaDocument(Context context, JObject obj, Document file)
        {
            var uidToJsonPath = new Dictionary<string, string>();
            var jsonPathToUid = new Dictionary<string, string>();
            GetUids(context, file.FilePath, obj, uidToJsonPath, jsonPathToUid);
            if (uidToJsonPath.Count == 0)
            {
                return (new List<Error>(), new List<InternalXrefSpec>());
            }

            if (file.Schema is null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var errors = new List<Error>();
            var (schemaValidator, schemaTransformer) = TemplateEngine.GetJsonSchema(file.Schema);
            if (schemaValidator is null || schemaTransformer is null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var (schemaErrors, extensionData) = schemaTransformer.TransformXref(file, context, obj);
            errors.AddRange(schemaErrors);

            var extensionDataByUid = new Dictionary<string, (bool isRoot, Dictionary<string, Lazy<JValue>> properties)>();

            foreach (var (uid, jsonPath) in uidToJsonPath)
            {
                extensionDataByUid.Add(uid, (string.IsNullOrEmpty(jsonPath), new Dictionary<string, Lazy<JValue>>()));
            }

            foreach (var (jsonPath, xrefProperty) in extensionData)
            {
                var (uid, resolvedJsonPath) = MatchExtensionDataToUid(jsonPath);
                if (extensionDataByUid.ContainsKey(uid))
                {
                    var (_, properties) = extensionDataByUid[uid];
                    properties.Add(resolvedJsonPath, xrefProperty);
                }
                else
                {
                    extensionDataByUid.Add(uid, (string.IsNullOrEmpty(uidToJsonPath[uid]), new Dictionary<string, Lazy<JValue>> { { resolvedJsonPath, xrefProperty } }));
                }
            }

            var specs = extensionDataByUid.Select(item =>
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

            (string uid, string jsonPath) MatchExtensionDataToUid(string jsonPath)
            {
                string subString;
                var index = jsonPath.LastIndexOf('.');
                if (index == -1)
                {
                    subString = string.Empty;
                }
                else
                {
                    subString = jsonPath.Substring(0, index);
                }

                return jsonPathToUid.ContainsKey(subString) ? (jsonPathToUid[subString], jsonPath.Substring(index + 1)) : MatchExtensionDataToUid(subString);
            }
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
