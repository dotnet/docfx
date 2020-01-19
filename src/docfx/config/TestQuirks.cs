// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal static class TestQuirks
    {
        public static Func<string>? CachePath;
        public static Func<string>? StatePath;
        public static Func<string, string>? GitRemoteProxy;
        public static Func<bool>? RestoreUseCache;
        public static bool? Verbose;
    }
}
