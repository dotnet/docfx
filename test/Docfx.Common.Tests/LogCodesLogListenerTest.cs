// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.Common.Tests;

[Collection("docfx STA")]
public class LogCodesLogListenerTest
{
    [Fact]
    public void TestLogCodes()
    {
        var logCodesLogListener = new LogCodesLogListener();
        Logger.RegisterListener(logCodesLogListener);
        Logger.LogWarning("message1", file: "file.md", code: WarningCodes.Build.InvalidFileLink);
        Logger.LogWarning("message2", file: "file.md", code: WarningCodes.Build.InvalidBookmark);
        Logger.LogWarning("message3", file: "anotherFile.md", code: WarningCodes.Build.InvalidFileLink);

        Assert.True(logCodesLogListener.Codes.TryGetValue("file.md", out var fileCodes));
        Assert.Contains(WarningCodes.Build.InvalidFileLink, fileCodes);
        Assert.Contains(WarningCodes.Build.InvalidBookmark, fileCodes);
        Assert.True(logCodesLogListener.Codes.TryGetValue("anotherFile.md", out var anotherFileCodes));
        Assert.Contains(WarningCodes.Build.InvalidFileLink, anotherFileCodes);

        Logger.UnregisterAllListeners();
    }
}
