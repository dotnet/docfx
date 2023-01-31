// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;

    public interface IInputParameters
    {
        ExtractMetadataOptions Options { get; }

        IEnumerable<string> Files { get; }

        bool HasChanged(BuildInfo buildInfo);
        
        string Key { get; }

        ProjectLevelCache Cache { get; }

        BuildInfo BuildInfo { get; }
    }
}
