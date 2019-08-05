// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DocAsTest
{
    internal interface ITestAttribute
    {
        string Path { get; }

        string SearchPattern { get; }

        void DiscoverTests(string path, Action<TestData> report);
    }
}
