// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class XrefMapLoaderTest
    {
        [Theory]
        [InlineData(@"
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
        [InlineData(@"{'references':[{'uid': 'a','href': 'https://docs.com/docs/a','name': 'Title from yaml header a'},{'uid': 'b','href': 'https://docs.com/docs/b','name': 'Title from yaml header b'}]}",
            "a", "b")]
        [InlineData(@"{'references':[ { 'uid': 'a',
'href': 'https://docs.com/docs/a',
'name': 'Title from yaml header a' },
{
'uid': 'b',
'href': 'https://docs.com/docs/b',
'name': 'Title from yaml header b' } ]
}",
            "a", "b")]
        public void LoadXrefMapFile(string json, params string[] uids)
        {
            var filePath = WriteJsonToTempFile(json);
            var result = XrefMapLoader.Load(filePath);
            foreach (var uid in uids)
            {
                Assert.Contains(uid, result.ToList().Select(x => x.Item1));
            }
        }

        private string WriteJsonToTempFile(string json)
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
