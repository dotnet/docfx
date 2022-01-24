// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Microsoft.Docs.Build;

internal enum LegacyItemType
{
    [EnumMember(Value = "Toc")]
    Toc,

    [EnumMember(Value = "Content")]
    Content,

    [EnumMember(Value = "Resource")]
    Resource,
}
