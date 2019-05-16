// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public class ReportTest
    {
        [Fact]
        public void DedupErrors()
        {
            using (var errorLog = new ErrorLog("DedupErrors"))
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
            using (var errorLog = new ErrorLog("MaxErrors"))
            {
                for (var i = 0; i < OutputConfig.DefaultMaxErrors; i++)
                {
                    errorLog.Write(new Error(ErrorLevel.Error, "an-error-code", i.ToString()));
                }

                Assert.Equal(OutputConfig.DefaultMaxErrors, errorLog.ErrorCount);

                Assert.Throws<DocfxException>(() =>
                {
                    errorLog.Write(new Error(ErrorLevel.Error, "an-error-code", "another message"));
                });
            }
        }
    }
}
