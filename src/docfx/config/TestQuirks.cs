// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal static class TestQuirks
    {
        public static Func<string>? CachePath { get; set; }

        public static Func<string>? StatePath { get; set; }

        public static Func<string, string>? GitRemoteProxy { get; set; }

        public static Func<bool>? RestoreUseCache { get; set; }

        public static bool? Verbose { get; set; }
    }
}
