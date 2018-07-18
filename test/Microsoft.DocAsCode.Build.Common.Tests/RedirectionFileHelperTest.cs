// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common.Tests
{
    using Xunit;

    [Trait("Owner", "jipe")]
    public class RedirectionFileHelperTest
    {
        [Theory]
        [InlineData(
            @"
# h1",
            false)]
        [InlineData(
            @"---
title: Redirect file test
---

# h1",
            false)]
        [InlineData(
            @"---
redirect_url: /test_folder/test

---

# h1",
            true)]
        [InlineData(
            @"---

title: Redirect file test
redirect_url: /test_folder/test
author: 
---

# h1",
            true)]
        public void TestIsRedirectionFile(string markdown, bool isRedirectionFile)
        {
            Assert.Equal(RedirectionFileHelper.IsRedirectionFile(markdown), isRedirectionFile);
        }
    }
}
