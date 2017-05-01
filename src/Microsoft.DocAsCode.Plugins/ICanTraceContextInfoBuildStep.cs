// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.IO;

    public interface ICanTraceContextInfoBuildStep : ISupportIncrementalBuildStep
    {
        void SaveContext(Stream stream);

        void LoadContext(Stream stream);
    }
}
