// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XRefService.Common.Models
{
    using System.Collections.Generic;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Plugins;

    public class XRefMap
    {
        [YamlMember(Alias = "references")]
        public List<XRefSpec> References { get; set; }
    }
}
