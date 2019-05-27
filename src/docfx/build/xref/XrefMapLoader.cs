// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal class XrefMapLoader
    {
        private static readonly byte[] s_uidSpan = Encoding.UTF8.GetBytes("uid");

        public static Dictionary<string, List<Lazy<XrefSpec>>> LoadXrefMap(string path, Action<string, Lazy<XrefSpec>> callback)
        {
            var utf8Json = File.ReadAllBytes(path);
            var result = new Dictionary<string, List<Lazy<XrefSpec>>>();
            var uidPositions = GetUidPositions(utf8Json);

            using (var stream = File.OpenRead(path))
            {
                foreach (var (uid, start, end) in uidPositions)
                {
                    if (!result.TryGetValue(uid, out var xrefspecs))
                    {
                        result.Add(uid, xrefspecs = new List<Lazy<XrefSpec>>(xrefspecs));
                    }

                    var json = ReadJsonFragment(stream, start, end);
                    xrefspecs.Add(new Lazy<XrefSpec>(() => JsonUtility.Deserialize<XrefSpec>(json, path)));
                }
            }

            return result;
        }

        private static List<(string uid, long start, long end)> GetUidPositions(ReadOnlySpan<byte> utf8Json)
        {
            var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, new JsonReaderState());
            var result = new List<(string uid, long start, long end)>();
            var stack = new Stack<(string uid, long start)>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        if (reader.TextEquals(s_uidSpan) &&
                            reader.Read() &&
                            reader.TokenType == JsonTokenType.String &&
                            stack.TryPop(out var top))
                        {
                            var uid = Encoding.UTF8.GetString(reader.ValueSpan);
                            stack.Push((uid, top.start));
                        }
                        break;

                    case JsonTokenType.StartObject:
                        stack.Push((null, reader.TokenStartIndex));
                        break;

                    case JsonTokenType.EndObject:
                        if (stack.TryPop(out var node))
                        {
                            result.Add((node.uid, node.start, reader.TokenStartIndex));
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
            var bytes = new byte[end - start];
            stream.Seek(start, SeekOrigin.Begin);

            while ((bytesRead = stream.Read(bytes, offset, bytes.Length)) > 0)
            {
                offset += bytesRead;
            }

            return Encoding.UTF8.GetString(bytes, 0, offset);
        }
    }
}
