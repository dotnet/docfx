// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Builders
{
    using YamlDotNet.Serialization;

    public class ManifestItem
    {
        [YamlMember(Alias = "type")]
        public string DocumentType { get; set; }
        [YamlMember(Alias = "model")]
        public string ModelFile { get; set; }
        [YamlMember(Alias = "pathFromRoot")]
        public string LocalPathFromRepoRoot { get; set; }
        [YamlMember(Alias = "original")]
        public string OriginalFile { get; set; }
        [YamlMember(Alias = "resource")]
        public string ResourceFile { get; set; }
    }
}
