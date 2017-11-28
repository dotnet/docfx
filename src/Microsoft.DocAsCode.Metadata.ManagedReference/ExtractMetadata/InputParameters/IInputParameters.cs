// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    interface IInputParameters
    {
        ExtractMetadataOptions Options { get; }

        IEnumerable<string> Files { get; }

        bool HasChanged(BuildInfo buildInfo);
        
        string Key { get; }

        ProjectLevelCache Cache { get; }

        BuildInfo BuildInfo { get; }
    }
}
