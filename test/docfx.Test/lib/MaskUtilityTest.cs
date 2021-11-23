// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Docs.Build;

public static class MaskUtilityTest
{
    [Theory]
    [InlineData("'4d6fbb8c3cd304d9a8183ac85f1078568cf1d5'", "'4d***d5'")]
    [InlineData("['244b491c91b2e37ee0cfd5e5ebeba62c14781', 'password']", "['24***81','***']")]
    [InlineData("{'key': 'c037851c464bd1256b648196397adab47de4'}", "{'key':'c0***e4'}")]
    [InlineData(
        "{'key1': '966f9a3d654f2f99e60b831ee289fd786987', 'key2': '0f903519-168e-aa8a-b03f72a1717d'}",
        "{'key1':'96***87','key2':'0f***7d'}")]
    [InlineData(
        "{'key':['a7480eccbe3820e09dc1163687c63ca9d87','10d0cf03-6df8-b765-614a4f01cd32']}",
        "{'key':['a7***87','10***32']}")]
    [InlineData("{'key':{'fakeSecret':'fakeToken'}}", "{'key':{'fakeSecret':'***'}}")]
    public static void HideSecret(string json, string expectedMasked)
    {
        var token = JsonUtility.Parse(new ErrorList(), json, null);
        var maskedToken = MaskUtility.HideSecret(token);
        Assert.Equal(expectedMasked.Replace('\'', '"'), maskedToken.ToString(Formatting.None));
    }
}
