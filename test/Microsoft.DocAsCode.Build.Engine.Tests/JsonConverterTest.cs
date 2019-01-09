// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Glob;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    public class JsonConverterTest
    {
        [Fact]
        public void TestFileMetadataConverterCouldSerializeAndDeserialize()
        {
            var baseDir = "inputFolder";
            var raw = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher("*.md"), "meta", 1L),
                    new FileMetadataItem(new GlobMatcher("*.m"), "meta", true),
                    new FileMetadataItem(new GlobMatcher("abc"), "meta", "string"),
                    new FileMetadataItem(new GlobMatcher("/[]\\*.cs"), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                    new FileMetadataItem(new GlobMatcher("*/*.cs"), "meta", new object[] { "1", "2" }),
                    new FileMetadataItem(new GlobMatcher("**"), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
                )
            });
            var serialized = JsonUtility.Serialize(raw);
            var expected = "{'baseDir':'inputFolder','dict':{'meta':[{'glob':'*.md','key':'meta','value':1},{'glob':'*.m','key':'meta','value':true},{'glob':'abc','key':'meta','value':'string'},{'glob':'/[]\\\\*.cs','key':'meta','value':{'key':'2'}},{'glob':'*/*.cs','key':'meta','value':['1','2']},{'glob':'**','key':'meta','value':{'key':['1','2']}}]}}".Replace('\'', '\"');
            Assert.Equal(expected, serialized);

            using (var reader = new StringReader(serialized))
            {
                var actual = JsonUtility.Deserialize<FileMetadata>(reader);

                Assert.Equal(baseDir, actual.BaseDir);
                Assert.Equal(raw.Count, actual.Count);
                foreach(var pair in raw)
                {
                    Assert.True(actual.TryGetValue(pair.Key, out var array));
                    Assert.Equal(pair.Value.Length, array.Length);
                    for (var i = 0; i < array.Length; i++)
                    {
                        Assert.Equal(pair.Value[i].Glob.Raw, array[i].Glob.Raw);
                        Assert.Equal(pair.Value[i].Key, array[i].Key);
                        Assert.Equal(pair.Value[i].Value, array[i].Value);
                    }
                }
            }
        }
    }
}
