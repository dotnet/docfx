// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DocAsTest
{
    /// <summary>
    /// Marks the current test as not found.
    /// </summary>
    public class TestNotFoundException : Exception
    {
        public TestNotFoundException() { }
    }
}
