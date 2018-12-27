// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class Context
    {
        public readonly Cache Cache;
        public readonly Report Report;
        public readonly Output Output;

        public Context(Report report, Cache cache, Output output)
        {
            Report = report;
            Cache = cache;
            Output = output;
        }
    }
}
