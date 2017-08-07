// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System.Dynamic;

    using Microsoft.DocAsCode.Plugins;

    public class ProcessContext : IProcessContext
    {
        public IHostService Host { get; }
        public FileModel Model { get; }
        public dynamic Properties { get; } = new ExpandoObject();
        public ProcessContext(IHostService hs, FileModel fm)
        {
            Host = hs;
            Model = fm;
        }
    }
}
