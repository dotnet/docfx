// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Microsoft.DocAsCode.Common;
    using Xunit;

    public class FileLinkInfoTest
    {
        [Fact]
        public void TestFileLinkInfo_EncodedWorkspaceCharacter()
        {
            string fromFileInSource = "articles/vpn-gateway/vpn-gateway-verify-connection-resource-manager.md";
            string fromFileInDest = "vpn-gateway/vpn-gateway-verify-connection-resource-manager.html";
            string href = "%7E/includes/media/vpn-gateway-verify-connection-portal-rm-include/connectionsucceeded.png";
            var context = new Build.Engine.DocumentBuildContext("_output");

            var expected = new FileLinkInfo
            {
                FileLinkInDest = null,
                FileLinkInSource = "~/includes/media/vpn-gateway-verify-connection-portal-rm-include/connectionsucceeded.png",
                FromFileInDest = "vpn-gateway/vpn-gateway-verify-connection-resource-manager.html",
                FromFileInSource = "articles/vpn-gateway/vpn-gateway-verify-connection-resource-manager.md",
                GroupInfo = null,
                Href = "../../includes/media/vpn-gateway-verify-connection-portal-rm-include/connectionsucceeded.png",
                ToFileInDest = null,
                ToFileInSource = "includes/media/vpn-gateway-verify-connection-portal-rm-include/connectionsucceeded.png"
            };

            var result = FileLinkInfo.Create(fromFileInSource, fromFileInDest, href, context);

            Assert.Equal(result, expected);
        }
    }
}
