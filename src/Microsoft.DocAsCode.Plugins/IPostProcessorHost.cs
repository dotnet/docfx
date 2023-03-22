// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Microsoft.DocAsCode.Plugins;

public interface IPostProcessorHost
{
    /// <summary>
    /// Source file information
    /// </summary>
    IImmutableList<SourceFileInfo> SourceFileInfos { get; }
}
