// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using FluentAssertions;
using Xunit;

namespace Docfx.MarkdigEngine.Tests;

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

        var result = new FileLinkInfo(fromFileInSource, fromFileInDest, href, context);

        result.Should().NotBe(expected); // FileLinkInfo is not override object.Equals method.
        result.Should().BeEquivalentTo(expected);
    }
}
