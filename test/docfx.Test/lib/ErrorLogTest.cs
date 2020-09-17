// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class ErrorLogTest
    {
        [Fact]
        public void DedupErrors()
        {
            using var errors = new ErrorWriter();
            var errorLog = new ErrorLog(errors, new Config());
            errorLog.Add(new Error(ErrorLevel.Error, "an-error-code", "message 1"));
            errorLog.Add(new Error(ErrorLevel.Error, "an-error-code", "message 1"));
            errorLog.Add(new Error(ErrorLevel.Warning, "an-error-code", "message 2"));

            Assert.Equal(1, errors.ErrorCount);
            Assert.Equal(1, errors.WarningCount);
        }

        [Fact]
        public void MaxErrors()
        {
            using var errors = new ErrorWriter();
            var config = new Config();
            var errorLog = new ErrorLog(errors, config);

            var testFiles = 100;
            var testErrors = new List<Error>();
            var testFileErrors = config.MaxFileErrors + 10;
            var testEmptyFileErrors = 200;

            for (var i = 0; i < testFiles; i++)
            {
                for (var j = 0; j < testFileErrors; j++)
                {
                    testErrors.Add(new Error(ErrorLevel.Error, "an-error-code", j.ToString(), new SourceInfo(new FilePath($"file-{i}"))));
                }
            }

            for (var i = 0; i < testEmptyFileErrors; i++)
            {
                testErrors.Add(new Error(ErrorLevel.Error, "an-error-code", i.ToString()));
            }

            ParallelUtility.ForEach(errorLog, testErrors, testError => errorLog.Add(testError));

            Assert.Equal((config.MaxFileErrors * testFiles) + testEmptyFileErrors, errors.ErrorCount);
        }
    }
}
