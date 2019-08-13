// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;

    using Microsoft.DocAsCode.Common;

    using Xunit;

    [Trait("Owner", "superyyrrzz")]
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
            Assert.True(fileCodes.Contains(WarningCodes.Build.InvalidFileLink));
            Assert.True(fileCodes.Contains(WarningCodes.Build.InvalidBookmark));
            Assert.True(logCodesLogListener.Codes.TryGetValue("anotherFile.md", out var anotherFileCodes));
            Assert.True(anotherFileCodes.Contains(WarningCodes.Build.InvalidFileLink));

            Logger.UnregisterAllListeners();
        }
    }
}
