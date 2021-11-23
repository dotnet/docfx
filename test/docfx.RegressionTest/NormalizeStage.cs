// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

[Flags]
internal enum NormalizeStage
{
    PrettifyJsonFiles = 0b001, // indentation only
    NormalizeJsonFiles = 0b010, // apply additional to output json files
    PrettifyLogFiles = 0b100, // sort, apply additional rule and indentation
}
