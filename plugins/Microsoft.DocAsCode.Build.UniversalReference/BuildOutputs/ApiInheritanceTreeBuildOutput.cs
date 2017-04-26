namespace Microsoft.DocAsCode.Build.UniversalReference.BuildOutputs
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Common.EntityMergers;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ApiInheritanceTreeBuildOutput
    {
        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public ApiNames Type { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Inheritance)]
        [MergeOption(MergeOption.Ignore)]
        [JsonProperty(Constants.PropertyName.Inheritance)]
        public List<ApiInheritanceTreeBuildOutput> Inheritance { get; set; }
    }
}
