// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Microsoft.Docs.Build
{
    internal class ExternalXrefMapLoader
    {
        private static readonly byte[] s_uidBytes = Encoding.UTF8.GetBytes("uid");
        private static readonly byte[] s_repositoryUrlBytes = Encoding.UTF8.GetBytes("repository_url");
        private static readonly byte[] s_docsetNameBytes = Encoding.UTF8.GetBytes("docset_name");
        private static readonly byte[] s_referencesBytes = Encoding.UTF8.GetBytes("references");
        private static readonly byte[] s_externalXrefsBytes = Encoding.UTF8.GetBytes("external_xrefs");

        public static ExternalXrefMap Load(Config config, FileResolver fileResolver, ErrorBuilder errors)
        {
            using (Progress.Start("Loading external xref map"))
            {
                return new ExternalXrefMap(config.Xref.AsParallel().AsOrdered().Select(url => LoadXrefMap(errors, fileResolver, url)));
            }
        }

        internal static ExternalXrefMap LoadJsonFile(Dictionary<string, Lazy<ExternalXrefSpec>> xrefSpecs, List<ExternalXref> externalXrefs, string filePath)
        {
            var content = File.ReadAllBytes(filePath);

            // TODO: cache this position mapping if xref map file not updated, reuse it
            var (xrefSpecPositions, xrefPositions, repositoryUrl, docsetName) = GetXrefSpecPosXrefPosAndRepoUrl(content, filePath);

            foreach (var (uid, start, end) in xrefSpecPositions)
            {
                xrefSpecs.TryAdd(uid, new Lazy<ExternalXrefSpec>(() =>
                {
                    using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var spec = JsonUtility.DeserializeData<ExternalXrefSpec>(ReadJsonFragment(stream, start, end), new FilePath(filePath));
                    spec.RepositoryUrl = repositoryUrl;
                    spec.DocsetName = docsetName;
                    return spec;
                }));
            }

            // TODO: for externalXref, where data loading is not so costly, so we will remove the following optimized process in another PR
            foreach (var (start, end) in xrefPositions)
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var xref = JsonUtility.DeserializeData<ExternalXref>(ReadJsonFragment(stream, start, end), new FilePath(filePath));
                xref.ReferencedRepositoryUrl = repositoryUrl;
                externalXrefs.Add(xref);
            }

            return new ExternalXrefMap(xrefSpecs, externalXrefs);
        }

        private static ExternalXrefMap LoadXrefMap(ErrorBuilder errors, FileResolver fileResolver, SourceInfo<string> url)
        {
            var path = new FilePath(url);
            var physicalPath = fileResolver.ResolveFilePath(url);
            var xrefSpecs = new Dictionary<string, Lazy<ExternalXrefSpec>>();
            var externalXrefs = new List<ExternalXref>();

            if (url.Value.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                LoadZipFile(xrefSpecs, externalXrefs, path, physicalPath, errors);
            }
            else if (url.Value.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(physicalPath);
                var xrefMap = YamlUtility.Deserialize<XrefMapModel>(errors, reader, path);
                foreach (var spec in xrefMap.References)
                {
                    spec.RepositoryUrl = xrefMap.RepositoryUrl;
                    spec.DocsetName = xrefMap.DocsetName;
                    xrefSpecs.TryAdd(spec.Uid, new Lazy<ExternalXrefSpec>(() => spec));
                }
                externalXrefs.AddRange(xrefMap.ExternalXrefs);
            }
            else
            {
                LoadJsonFile(xrefSpecs, externalXrefs, physicalPath);
            }

            return new ExternalXrefMap(xrefSpecs, externalXrefs);
        }

        private static void LoadZipFile(
            Dictionary<string, Lazy<ExternalXrefSpec>> xrefSpecs,
            List<ExternalXref> externalXrefs,
            FilePath path,
            string physicalPath,
            ErrorBuilder errors)
        {
            using var stream = File.OpenRead(physicalPath);
            using var archive = new ZipArchive(stream);
            foreach (var entry in archive.Entries)
            {
                using var entryStream = entry.Open();
                if (entry.FullName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(entryStream);
                    var xrefMap = YamlUtility.Deserialize<XrefMapModel>(errors, reader, path);
                    foreach (var spec in xrefMap.References)
                    {
                        spec.RepositoryUrl = xrefMap.RepositoryUrl;
                        spec.DocsetName = xrefMap.DocsetName;
                        xrefSpecs.TryAdd(spec.Uid, new Lazy<ExternalXrefSpec>(() => spec));
                    }
                    externalXrefs.AddRange(xrefMap.ExternalXrefs);
                }
                else if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(entryStream);
                    var xrefMap = JsonUtility.Deserialize<XrefMapModel>(errors, reader, path);
                    foreach (var spec in xrefMap.References)
                    {
                        spec.RepositoryUrl = xrefMap.RepositoryUrl;
                        spec.DocsetName = xrefMap.DocsetName;
                        xrefSpecs.TryAdd(spec.Uid, new Lazy<ExternalXrefSpec>(() => spec));
                    }
                    externalXrefs.AddRange(xrefMap.ExternalXrefs);
                }
            }
        }

        private static (List<(string uid, long start, long end)>, List<(long start, long end)>, string?, string?) GetXrefSpecPosXrefPosAndRepoUrl(
            ReadOnlySpan<byte> content, string filePath)
        {
            var xrefSpecPos = new List<(string uid, long start, long end)>();
            var xrefPos = new List<(long start, long end)>();
            string repositoryUrl = "";
            string docsetName = "";
            var stack = new Stack<(string? uid, long start)>();
            var reader = new Utf8JsonReader(content, isFinalBlock: true, default);
            var inReferencesObj = true;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        if (reader.ValueTextEquals(s_uidBytes) && reader.Read() && reader.TokenType == JsonTokenType.String && stack.TryPop(out var top))
                        {
                            stack.Push((Encoding.UTF8.GetString(reader.ValueSpan), top.start));
                        }
                        if (stack.Count == 1)
                        {
                            if (reader.ValueTextEquals(s_referencesBytes))
                            {
                                inReferencesObj = true;
                            }
                            else if (reader.ValueTextEquals(s_externalXrefsBytes))
                            {
                                inReferencesObj = false;
                            }
                            else if (string.IsNullOrEmpty(repositoryUrl) && reader.ValueTextEquals(s_repositoryUrlBytes) && reader.Read())
                            {
                                if (reader.TokenType == JsonTokenType.String)
                                {
                                    repositoryUrl = Encoding.UTF8.GetString(reader.ValueSpan);
                                }
                                else
                                {
                                    throw Errors.JsonSchema.UnexpectedType(new SourceInfo<string>(filePath), "string", reader.TokenType).ToException();
                                }
                            }
                            else if (string.IsNullOrEmpty(docsetName) && reader.ValueTextEquals(s_docsetNameBytes) && reader.Read())
                            {
                                if (reader.TokenType == JsonTokenType.String)
                                {
                                    docsetName = Encoding.UTF8.GetString(reader.ValueSpan);
                                }
                                else
                                {
                                    throw Errors.JsonSchema.UnexpectedType(new SourceInfo<string>(filePath), "string", reader.TokenType).ToException();
                                }
                            }
                        }
                        break;

                    case JsonTokenType.StartObject:
                        stack.Push((null, (int)reader.TokenStartIndex));
                        break;

                    case JsonTokenType.EndObject:
                        if (stack.TryPop(out var item) && item.uid != null)
                        {
                            if (inReferencesObj)
                            {
                                xrefSpecPos.Add((item.uid, item.start, (int)reader.TokenStartIndex + 1));
                            }
                            else
                            {
                                xrefPos.Add((item.start, (int)reader.TokenStartIndex + 1));
                            }
                        }
                        break;
                }
            }
            return (xrefSpecPos, xrefPos, repositoryUrl, docsetName);
        }

        private static string ReadJsonFragment(Stream stream, long start, long end)
        {
            var offset = 0;
            var bytesRead = 0;
            var bytesToRead = (int)(end - start);
            var bytes = new byte[bytesToRead];
            stream.Position = start;

            while (bytesToRead > 0 && (bytesRead = stream.Read(bytes, offset, bytesToRead)) > 0)
            {
                offset += bytesRead;
                bytesToRead -= bytesRead;
            }

            return Encoding.UTF8.GetString(bytes);
        }
    }
}
