namespace XRefService.Common.Models
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using YamlDotNet.Serialization;

    public class XRefMap
    {
        [YamlMember(Alias = "references")]
        public List<XRefSpec> References { get; set; }
    }
}
