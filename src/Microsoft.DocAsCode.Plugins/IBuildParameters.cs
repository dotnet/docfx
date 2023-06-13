﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsCode.Plugins;

public interface IBuildParameters
{
    IReadOnlyDictionary<string, JArray> TagParameters { get; }
}
