// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.MarkdigExtensions.Tests;

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
