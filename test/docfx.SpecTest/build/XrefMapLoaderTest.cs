// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build;

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
        ],
        'external_xrefs':[
            {
                'uid': 'c',
                'repositoryUrl': 'c_repo',
                'count': 1
            },
            {
                'uid': 'd',
                'repositoryUrl': 'd_repo',
                'count': 2
            }
        ]
    }
", new string[] { "a", "b" }, new string[] { "c", "d" })]

    [InlineData(
        @"{'references':[{'uid': 'a','href': 'https://docs.com/docs/a','name': 'Title from yaml header a'},{'uid': 'b','href': 'https://docs.com/docs/b','name': 'Title from yaml header b'}],'external_xrefs':[{'uid':'c','repositoryUrl':'c_repo','count':1},{'uid':'d','repositoryUrl':'d_repo','count':2}]}", new string[] { "a", "b" }, new string[] { "c", "d" })]

    [InlineData(
        @"{'references':[ { 'uid': 'a',
'href': 'https://docs.com/docs/a',
'name': 'Title from yaml header a' },
{
'uid': 'b',
'href': 'https://docs.com/docs/b',
'name': 'Title from yaml header b' } ],
        'external_xrefs':[ { 'uid': 'c',
'repositoryUrl': 'c_repo',
'count': 1
},
{
'uid': 'd',
'repositoryUrl': 'd_repo',
'count': 2
}]}",
        new string[] { "a", "b" }, new string[] { "c", "d" })]

    [InlineData(@"{'references':[{'uid': 'a', 'prop': {'test':1}}, {'uid': 'b', 'prop': {'test':2}}], 'external_xrefs':[{'uid':'c','prop': {'test':1}},{'uid':'d','prop': {'test':2}}]}", new string[] { "a", "b" }, new string[] { "c", "d" })]
    public void LoadXrefMapFile(string json, string[] uids, string[] externalUids)
    {
        var filePath = WriteJsonToTempFile(json);
        var xrefSpecs = new Dictionary<string, Lazy<ExternalXrefSpec>>();
        var externalXrefs = new List<ExternalXref>();
        ExternalXrefMapLoader.LoadJsonFile(xrefSpecs, externalXrefs, new FileResolver(new LocalPackage()), new SourceInfo<string>(filePath));

        Assert.Equal(uids, xrefSpecs.Keys);
        Assert.Equal(externalUids, externalXrefs.Select(item => item.Uid));
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
