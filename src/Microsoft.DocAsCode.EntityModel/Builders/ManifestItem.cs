// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Builders
{
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class ManifestItem
    {
        public string DocumentType { get; set; }
        public string LocalPathFromRepoRoot { get; set; }
        public string Key { get; set; }
        public string ModelFile { get; set; }
        public string ResourceFile { get; set; }
        public string InputFolder { get; set; }
        public ModelWithCache Model { get; set; }
    }
}
