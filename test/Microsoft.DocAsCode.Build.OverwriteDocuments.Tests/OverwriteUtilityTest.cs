// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments.Tests
{
    using System;
    using System.Linq;

    using Xunit;

    [Trait("Owner", "jipe")]
    [Trait("EntityType", nameof(OverwriteUtility))]
    public class OverwriteUtilityTest
    {
        [Fact]
        public void ParseOPathTest()
        {
            var OPathstring = "a/f[c= \"d\"]/g/b[c =\"d/d_d d\"]/e";
            var OPathSegments = OverwriteUtility.ParseOPath(OPathstring);
            Assert.Equal(5, OPathSegments.Count);
            Assert.Equal("a,f,g,b,e", OPathSegments.Select(o => o.SegmentName).Aggregate((a, b) => a + "," + b));
            Assert.Equal("c", OPathSegments[1].Key);
            Assert.Equal("d", OPathSegments[1].Value);
            Assert.Equal("d/d_d d", OPathSegments[3].Value);
        }

        [Theory]
        [InlineData("abc[]d=\"e\"/g]")]
        [InlineData("abc[d='e']/g")]
        [InlineData("abc[d=\"e\"]/g/")]
        [InlineData("abc[d=2]/g/")]
        [InlineData("abc[d=true]/g/")]
        [InlineData("abc[a=\"b]\b")]
        [InlineData("abc/efg[a=\"b\"]")]
        [InlineData("abc/efg[a=\"b\"]e/g")]
        [InlineData("abc/[efg=\"hij\"]/e")]
        public void ParseInvalidOPathsTest(string OPathString)
        {
            var ex = Assert.Throws<ArgumentException>(() => OverwriteUtility.ParseOPath(OPathString));
            Assert.Equal($"{OPathString} is not a valid OPath", ex.Message);
        }
    }
}
