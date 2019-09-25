// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public class ErrorLogTest
    {
        [Fact]
        public void DedupErrors()
        {
            using (var errorLog = new ErrorLog("test", ".", "DedupErrors", () => new Config()))
            {
                errorLog.Write(new Error(ErrorLevel.Error, "an-error-code", "message 1"));
                errorLog.Write(new Error(ErrorLevel.Error, "an-error-code", "message 1"));
                errorLog.Write(new Error(ErrorLevel.Warning, "an-error-code", "message 2"));

                Assert.Equal(1, errorLog.ErrorCount);
                Assert.Equal(1, errorLog.WarningCount);
            }
        }

        [Fact]
        public void MaxErrors()
        {
            var maxErrors = 1000;
            using (var errorLog = new ErrorLog("test", ".", "DedupErrors", () => new Config()))
            {
                for (var i = 0; i < maxErrors; i++)
                {
                    errorLog.Write(new Error(ErrorLevel.Error, "an-error-code", i.ToString()));
                }

                Assert.Equal(maxErrors, errorLog.ErrorCount);

                errorLog.Write(new Error(ErrorLevel.Error, "an-error-code", "another message"));
                Assert.Equal(maxErrors, errorLog.ErrorCount);
            }
        }
    }
}
