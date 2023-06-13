// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.DocAsCode.MarkdigEngine.Tests;

public class EmojiTest
{
    [Fact]
    public void EmojiTestGeneral()
    {
        var content = @"**content :** :smile:";
        var expected = @"<p><strong>content :</strong> 😄</p>";

        TestUtility.VerifyMarkup(content, expected);
    }
}
