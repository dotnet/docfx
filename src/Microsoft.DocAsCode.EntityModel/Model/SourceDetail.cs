// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;

    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Utility;

    [Serializable]
    public class SourceDetail
    {
        [YamlMember(Alias = "remote")]
        public GitDetail Remote { get; set; }

        [YamlMember(Alias = "base")]
        public string BasePath { get; set; }

        [YamlMember(Alias = "id")]
        public string Name { get; set; }

        /// <summary>
        /// The url path for current source, should be resolved at some late stage
        /// </summary>
        [YamlMember(Alias = "href")]
        public string Href { get; set; }

        /// <summary>
        /// The local path for current source, should be resolved to be relative path at some late stage
        /// </summary>
        [YamlMember(Alias = "path")]
        public string Path { get; set; }

        [YamlMember(Alias = "startLine")]
        public int StartLine { get; set; }

        [YamlMember(Alias = "endLine")]
        public int EndLine { get; set; }

        [YamlMember(Alias = "content")]
        public string Content { get; set; }

        /// <summary>
        /// The external path for current source if it is not locally available
        /// </summary>
        [YamlMember(Alias = "isExternal")]
        public bool IsExternalPath { get; set; }
    }
}
