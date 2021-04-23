// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class MaskUtilityTest
    {
        [Theory]
        [InlineData("'ThisIsAGithubToken'", "'Th***en'")]
        [InlineData("['someLongPat', 'password']", "['so***at','***']")]
        [InlineData("{'key': 'AnotherSecret'}", "{'key':'An***et'}")]
        [InlineData(
            "{'key1': 'someSHA1Hash1', 'key2': 'someSHA1Hash2'}",
            "{'key1':'so***h1','key2':'so***h2'}")]
        [InlineData(
            "{'key':['ThisIsAGithubToken','ThisIsAGithubToken']}",
            "{'key':['Th***en','Th***en']}")]
        [InlineData("{'key':{'secret':'token'}}", "{'key':{'secret':'***'}}")]
        public static void HideSecret(string json, string expectedMasked)
        {
            var token = JsonUtility.Parse(new ErrorList(), json, null);
            var maskedToken = MaskUtility.HideSecret(token);
            Assert.Equal(expectedMasked.Replace('\'', '"'), maskedToken.ToString(Formatting.None));
        }
    }
}
