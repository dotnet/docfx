// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Common.EntityMergers;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class InheritanceTree
    {
        [YamlMember(Alias = Constants.PropertyName.Uid)]
        [JsonProperty(Constants.PropertyName.Uid)]
        [UniqueIdentityReference]
        public string Uid { get; set; }

        /// <summary>
        /// item's inheritance
        /// multiple inheritance is allowed in languages like Python
        /// </summary>
        [YamlMember(Alias = Constants.PropertyName.Inheritance)]
        [MergeOption(MergeOption.Ignore)]
        [JsonProperty(Constants.PropertyName.Inheritance)]
        public List<InheritanceTree> Inheritance { get; set; }

    }
}
