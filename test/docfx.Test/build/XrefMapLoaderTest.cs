// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class XrefMapLoaderTest
    {
        [Theory]
        [InlineData(
            @"
{
      'references':[
        {
          'uid': 'a',
          'href': 'https://docs.com/docs/a',
          'name': 'Title from yaml header a'
        },
        {
          'uid': 'b',
          'href': 'https://docs.com/docs/b',
          'name': 'Title from yaml header b'
        }
      ]
    }
", "a", "b")]
        [InlineData(
            @"{'references':[{'uid': 'a','href': 'https://docs.com/docs/a','name': 'Title from yaml header a'},{'uid': 'b','href': 'https://docs.com/docs/b','name': 'Title from yaml header b'}]}",
            "a", "b")]
        [InlineData(
            @"{'references':[ { 'uid': 'a',
'href': 'https://docs.com/docs/a',
'name': 'Title from yaml header a' },
{
'uid': 'b',
'href': 'https://docs.com/docs/b',
'name': 'Title from yaml header b' } ]
}",
            "a", "b")]
        [InlineData(@"{'references':[{'uid': 'a', 'prop': {'test':1}}, {'uid': 'b', 'prop': {'test':2}}]}", "a", "b")]
        public void LoadXrefMapFile(string json, params string[] uids)
        {
            var filePath = WriteJsonToTempFile(json);
            var result = ExternalXrefMapLoader.LoadJsonFile(filePath);
            var resultUids = new List<string>();
            foreach (var (uid, spec) in result.externalXref)
            {
                resultUids.Add(((ExternalXrefSpec)spec.Value).Uid);
            }
            Assert.Equal(uids, resultUids);
        }

        private static string WriteJsonToTempFile(string json)
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "xref-map-loader");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var tempFilePath = Path.Combine(directory, Guid.NewGuid().ToString() + ".json");
            File.WriteAllText(tempFilePath, json.Replace('\'', '"'));
            return tempFilePath;
        }
    }
}
