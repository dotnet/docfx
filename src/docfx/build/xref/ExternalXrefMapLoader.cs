// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Microsoft.Docs.Build
{
    internal class ExternalXrefMapLoader
    {
        private static readonly byte[] s_uidBytes = Encoding.UTF8.GetBytes("uid");

        public static IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>> Load(Config config, FileResolver fileResolver, ErrorBuilder errors)
        {
            using (Progress.Start("Loading external xref map"))
            {
                var result = new Dictionary<string, Lazy<ExternalXrefSpec>>();

                foreach (var url in config.Xref)
                {
                    var path = new FilePath(url);
                    var physicalPath = fileResolver.ResolveFilePath(url);
                    if (url.Value.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        LoadZipFile(result, path, physicalPath, errors);
                    }
                    else if (url.Value.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                    {
                        using var reader = new StreamReader(physicalPath);
                        var xrefMap = YamlUtility.Deserialize<XrefMapModel>(errors, reader, path);
                        foreach (var spec in xrefMap.References)
                        {
                            result.TryAdd(spec.Uid, new Lazy<ExternalXrefSpec>(() => spec));
                        }
                    }
                    else
                    {
                        // Fast pass for JSON xref files
                        foreach (var (uid, spec) in LoadJsonFile(physicalPath))
                        {
                            // for same uid with multiple specs, we should respect the order of the list
                            result.TryAdd(uid, spec);
                        }
                    }
                }

                result.TrimExcess();
                return result;
            }
        }

        public static List<(string, Lazy<ExternalXrefSpec>)> LoadJsonFile(string filePath)
        {
            var result = new List<(string, Lazy<ExternalXrefSpec>)>();
            var content = File.ReadAllBytes(filePath);

            // TODO: cache this position mapping if xref map file not updated, reuse it
            var xrefSpecPositions = GetXrefSpecPositions(content);

            foreach (var (uid, start, end) in xrefSpecPositions)
            {
                result.Add((uid, new Lazy<ExternalXrefSpec>(() =>
                {
                    using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var json = ReadJsonFragment(stream, start, end);
                    return JsonUtility.DeserializeData<ExternalXrefSpec>(json, new FilePath(filePath));
                })));
            }
            return result;
        }

        private static void LoadZipFile(Dictionary<string, Lazy<ExternalXrefSpec>> result, FilePath path, string physicalPath, ErrorBuilder errors)
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
                        result.TryAdd(spec.Uid, new Lazy<ExternalXrefSpec>(() => spec));
                    }
                }
                else if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(entryStream);
                    var xrefMap = JsonUtility.Deserialize<XrefMapModel>(errors, reader, path);
                    foreach (var spec in xrefMap.References)
                    {
                        result.TryAdd(spec.Uid, new Lazy<ExternalXrefSpec>(() => spec));
                    }
                }
            }
        }

        private static List<(string uid, long start, long end)> GetXrefSpecPositions(ReadOnlySpan<byte> content)
        {
            var result = new List<(string uid, long start, long end)>();
            var stack = new Stack<(string? uid, long start)>();
            var reader = new Utf8JsonReader(content, isFinalBlock: true, default);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        if (reader.ValueTextEquals(s_uidBytes) && reader.Read() && reader.TokenType == JsonTokenType.String && stack.TryPop(out var top))
                        {
                            stack.Push((Encoding.UTF8.GetString(reader.ValueSpan), top.start));
                        }
                        break;
                    case JsonTokenType.StartObject:
                        stack.Push((null, (int)reader.TokenStartIndex));
                        break;
                    case JsonTokenType.EndObject:
                        if (stack.TryPop(out var item) && item.uid != null)
                        {
                            result.Add((item.uid, item.start, (int)reader.TokenStartIndex + 1));
                        }
                        break;
                }
            }
            return result;
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
