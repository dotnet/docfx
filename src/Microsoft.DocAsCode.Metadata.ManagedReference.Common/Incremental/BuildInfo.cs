// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;

    public class BuildInfo
    {
        public string BuildAssembly { get; set; }

        public string InputFilesKey { get; set; }

        public DateTime TriggeredUtcTime { get; set; }

        public DateTime CompleteUtcTime { get; set; }

        public string OutputFolder { get; set; }

        public ExtractMetadataOptions Options { get; set; }

        public IEnumerable<string> RelativeOutputFiles { get; set; }

        /// <summary>
        /// Save the files involved in the build
        /// </summary>
        public IDictionary<string, List<string>> ContainedFiles { get; set; }

        public string CheckSum { get; set; }
    }
}
