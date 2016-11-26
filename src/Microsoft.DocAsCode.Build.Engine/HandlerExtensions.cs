// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    internal static class HandlerExtensions
    {
        public static PrebuildBuildPhaseHandlerWithIncremental WithIncremental(this PrebuildBuildPhaseHandler inner)
        {
            return new PrebuildBuildPhaseHandlerWithIncremental(inner);
        }

        public static PostbuildPhaseHandlerWithIncremental WithIncremental(this PostbuildPhaseHandler inner)
        {
            return new PostbuildPhaseHandlerWithIncremental(inner);
        }
    }
}
