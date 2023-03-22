// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Build.Engine;

public class XRefMapRedirection
{
    [YamlMember(Alias = "uidPrefix")]
    public string UidPrefix { get; set; }

    [YamlMember(Alias = "href")]
    public string Href { get; set; }
}
