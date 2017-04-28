// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Newtonsoft.Json;

    public class PreviewCommand
    {
        public static PreviewJsonConfig ParsePreviewCommand(string workspacePath)
        {
            PreviewJsonConfig config = new PreviewJsonConfig();
            config.References = new Dictionary<string, string>(PreviewConstants.References);
            config.TocMetadataName = PreviewConstants.TocMetadataName;
            return config;
        }
    }
}
