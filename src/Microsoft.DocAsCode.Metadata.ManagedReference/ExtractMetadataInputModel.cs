// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;

    public class ExtractMetadataInputModel
    {
        public Dictionary<string, List<string>> Items { get; set; }

        public string ApiFolderName { get; set; } = "api";

        public string TocFileName { get; set; } = "toc.yml";

        public string IndexFileName { get; set; } = ".manifest";

        public bool PreserveRawInlineComments { get; set; }

        public List<string> ExternalReferences { get; set; }

        public bool ForceRebuild { get; set; }

        public bool ShouldSkipMarkup { get; set; }

        public string FilterConfigFile { get; set; }

        public bool UseCompatibilityFileName { get; set; }

        public string GlobalNamespaceId { get; set; }

        public Dictionary<string, string> MSBuildProperties { get; set; }

        public override string ToString()
        {
            using(StringWriter writer = new StringWriter())
            {
                JsonUtility.Serialize(writer, this);
                return writer.ToString();
            }
        }

        public ExtractMetadataInputModel Clone()
        {
            var cloned = (ExtractMetadataInputModel)this.MemberwiseClone();
            cloned.Items = new Dictionary<string, List<string>>(Items);
            return cloned;
        }
    }
}
