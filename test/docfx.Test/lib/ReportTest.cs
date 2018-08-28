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
            using (var report = new Report())
            {
                report.Write(new Error(ErrorLevel.Error, "an-error-code", "message 1"));
                report.Write(new Error(ErrorLevel.Error, "an-error-code", "message 1"));
                report.Write(new Error(ErrorLevel.Warning, "an-error-code", "message 2"));

                Assert.Equal(1, report.Errors);
                Assert.Equal(1, report.Warnings);
            }
        }

        [Fact]
        public void MaxErrors()
        {
            using (var report = new Report())
            {
                for (var i = 0; i < Report.MaxErrors; i++)
                {
                    report.Write(new Error(ErrorLevel.Error, "an-error-code", i.ToString()));
                }

                Assert.Equal(Report.MaxErrors, report.Errors);

                report.Write(new Error(ErrorLevel.Error, "an-error-code", "another message"));
                Assert.Equal(Report.MaxErrors, report.Errors);
            }
        }
    }
}
