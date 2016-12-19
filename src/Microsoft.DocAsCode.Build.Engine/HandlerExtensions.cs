// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    internal static class HandlerExtensions
    {
        public static CompilePhaseHandlerWithIncremental WithIncremental(this CompilePhaseHandler inner)
        {
            return new CompilePhaseHandlerWithIncremental(inner);
        }

        public static LinkPhaseHandlerWithIncremental WithIncremental(this LinkPhaseHandler inner)
        {
            return new LinkPhaseHandlerWithIncremental(inner);
        }
    }
}
