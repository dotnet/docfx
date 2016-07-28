// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Plugins;

    internal class InternalManifestItem
    {
        public string DocumentType { get; set; }

        /// <summary>
        /// relative path from docfx.json
        /// </summary>
        public string LocalPathFromRoot { get; set; }

        public string Key { get; set; }

        public string FileWithoutExtension { get; set; }

        public string Extension { get; set; }

        public string ResourceFile { get; set; }

        public string InputFolder { get; set; }

        public ModelWithCache Model { get; set; }

        public Dictionary<string, object> Metadata { get; set; }
    }
}
