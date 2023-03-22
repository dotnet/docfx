// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.Engine;

internal sealed class PostProcessor
{
    public string ContractName { get; set; }

    public IPostProcessor Processor { get; set; }
}
