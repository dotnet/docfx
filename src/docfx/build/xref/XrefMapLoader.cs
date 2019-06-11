// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Microsoft.Docs.Build
{
    internal class XrefMapLoader
    {
        private static byte[] s_uidBytes = Encoding.UTF8.GetBytes("uid");

        public static ListBuilder<(string, Lazy<IXrefSpec>)> Load(string filePath)
        {
            var result = new ListBuilder<(string, Lazy<IXrefSpec>)>();
            var content = File.ReadAllBytes(filePath);

            // TODO: cache this position mapping if xref map file not updated, reuse it
            var xrefSpecPositions = GetXrefSpecPositions(content);

            foreach (var (uid, start, end) in xrefSpecPositions)
            {
                result.Add((uid, new Lazy<IXrefSpec>(() =>
                {
                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var json = ReadJsonFragment(stream, start, end);
                        return JsonUtility.Deserialize<ExternalXrefSpec>(json, filePath);
                    }
                })));
            }
            return result;
        }

        private static List<(string uid, long start, long end)> GetXrefSpecPositions(ReadOnlySpan<byte> content)
        {
            var result = new List<(string uid, long start, long end)>();
            var stack = new Stack<(string uid, long start)>();
            var reader = new Utf8JsonReader(content, isFinalBlock: true, default);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        if (reader.TextEquals(s_uidBytes) && reader.Read() && reader.TokenType == JsonTokenType.String && stack.TryPop(out var top))
                        {
                            stack.Push((Encoding.UTF8.GetString(reader.ValueSpan), top.start));
                        }
                        break;
                    case JsonTokenType.StartObject:
                        stack.Push((null, (int)reader.TokenStartIndex));
                        break;
                    case JsonTokenType.EndObject:
                        if (stack.TryPop(out var item))
                        {
                            if (item.uid != null)
                            {
                                result.Add((item.uid, item.start, (int)reader.TokenStartIndex + 1));
                            }
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
