// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DocAsTest
{
    /// <summary>
    /// Marks the current test as skipped.
    /// </summary>
    public class TestSkippedException : Exception
    {
        public string Reason { get; set; }

        public TestSkippedException() { }

        public TestSkippedException(string reason) => Reason = reason;
    }
}
