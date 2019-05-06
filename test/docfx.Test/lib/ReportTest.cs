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
            using (var report = new Report("DedupErrors"))
            {
                report.Write(new Error(ErrorLevel.Error, "an-error-code", "message 1"));
                report.Write(new Error(ErrorLevel.Error, "an-error-code", "message 1"));
                report.Write(new Error(ErrorLevel.Warning, "an-error-code", "message 2"));

                Assert.Equal(1, report.ErrorCount);
                Assert.Equal(1, report.WarningCount);
            }
        }

        [Fact]
        public void MaxErrors()
        {
            using (var report = new Report("MaxErrors"))
            {
                for (var i = 0; i < OutputConfig.DefaultMaxErrors; i++)
                {
                    report.Write(new Error(ErrorLevel.Error, "an-error-code", i.ToString()));
                }

                Assert.Equal(OutputConfig.DefaultMaxErrors, report.ErrorCount);

                report.Write(new Error(ErrorLevel.Error, "an-error-code", "another message"));
                Assert.Equal(OutputConfig.DefaultMaxErrors, report.ErrorCount);
            }
        }
    }
}
