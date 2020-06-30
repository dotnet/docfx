// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    [Flags]
    internal enum NormalizeStage
    {
        NormalizeJsonFiles = 0b001,
        PrettifyLogFiles = 0b010, // only sort, and remote date_time
        NormalizeLogFiles = 0b100, // sort, apply additional rule and indentation
    }
}
