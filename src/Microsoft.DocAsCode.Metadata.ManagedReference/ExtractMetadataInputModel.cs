// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;

    public class ExtractMetadataInputModel
    {
        public List<string> Files { get; set; }

        public List<string> References { get; set; }

        public string OutputFolder { get; set; }

        public bool PreserveRawInlineComments { get; set; }

        public bool ForceRebuild { get; set; }

        public bool ShouldSkipMarkup { get; set; }

        public string FilterConfigFile { get; set; }

        public bool UseCompatibilityFileName { get; set; }

        public string GlobalNamespaceId { get; set; }

        public string CodeSourceBasePath { get; set; }

        public bool DisableDefaultFilter { get; set; }

        public Dictionary<string, string> MSBuildProperties { get; set; }

        public override string ToString()
        {
            using(StringWriter writer = new StringWriter())
            {
                JsonUtility.Serialize(writer, this);
                return writer.ToString();
            }
        }
    }
}
