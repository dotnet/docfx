// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Graph;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class ErrorLogTest
    {
        [Fact]
        public void DedupErrors()
        {
            using var errorLog = new ErrorLog("DedupErrors");
            errorLog.Configure(new Config(), ".", null);
            errorLog.Write(new Error(ErrorLevel.Error, "an-error-code", "message 1"));
            errorLog.Write(new Error(ErrorLevel.Error, "an-error-code", "message 1"));
            errorLog.Write(new Error(ErrorLevel.Warning, "an-error-code", "message 2"));

            Assert.Equal(1, errorLog.ErrorCount);
            Assert.Equal(1, errorLog.WarningCount);
        }

        [Fact]
        public void MaxErrors()
        {
            using var errorLog = new ErrorLog("MaxErrors");
            var config = new Config();
            errorLog.Configure(config, ".", null);

            var testFiles = 10;
            var testErrors = new List<Error>();
            var testFileErrors = config.MaxFileErrors + 10;
            var testEmptyFileErrors = config.MaxErrors + 10;
            for (var i = 0; i < testFiles; i++)
            {
                for (var j = 0; j < testFileErrors; j++)
                {
                    errorLog.Write(new Error(ErrorLevel.Error, "an-error-code", j.ToString(), new FilePath($"file-{i}")));
                }
            }

            for (var i = 0; i < testEmptyFileErrors; i++)
            {
                errorLog.Write(new Error(ErrorLevel.Error, "an-error-code", i.ToString()));
            }

            Assert.Equal(config.MaxFileErrors * testFiles + config.MaxErrors, errorLog.ErrorCount);
        }
    }
}
