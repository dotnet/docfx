// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using Microsoft.DocAsCode.Plugins;

    public interface IProcessContext
    {
        IHostService Host { get; }
        FileModel Model { get; }
        dynamic Properties { get; }
        IDocumentBuildContext BuildContext { get; }
    }
}
