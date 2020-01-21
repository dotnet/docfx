// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal static class TestQuirks
    {
        public static Func<string>? GetCachePath;
        public static Func<string>? GetStatePath;
        public static Func<string, string>? GitRemoteProxy;
        public static bool? Verbose;
    }
}
