// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.Docs.Build.build
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

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private string WriteJsonToTempFile(string json)
        {
            var tempFilePath = Path.GetTempFileName();
            tempFilePath = Path.ChangeExtension(tempFilePath, ".json");
            File.WriteAllText(tempFilePath, json.Replace('\'', '"'));
            return tempFilePath;
        }
    }
}
