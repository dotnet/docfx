// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Xunit;

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
}
