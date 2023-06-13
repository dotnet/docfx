// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Build.Engine;

public class XRefMapRedirection
{
    [YamlMember(Alias = "uidPrefix")]
    public string UidPrefix { get; set; }

    [YamlMember(Alias = "href")]
    public string Href { get; set; }
}
