// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.Common
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class ReferenceViewModel
    {
        [YamlMember(Alias = Constants.PropertyName.Uid)]
        [JsonProperty(Constants.PropertyName.Uid)]
        public string Uid { get; set; }

        [YamlMember(Alias = Constants.PropertyName.CommentId)]
        [JsonProperty(Constants.PropertyName.CommentId)]
        public string CommentId { get; set; }

        [YamlMember(Alias = "parent")]
        [JsonProperty("parent")]
        public string Parent { get; set; }

        [YamlMember(Alias = "definition")]
        [JsonProperty("definition")]
        public string Definition { get; set; }

        [JsonProperty("isExternal")]
        [YamlMember(Alias = "isExternal")]
        public bool? IsExternal { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Href)]
        [JsonProperty(Constants.PropertyName.Href)]
        public string Href { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Name)]
        [JsonProperty(Constants.PropertyName.Name)]
        public string Name { get; set; }

        [ExtensibleMember(Constants.ExtensionMemberPrefix.Name)]
        [JsonIgnore]
        public SortedList<string, string> NameInDevLangs { get; private set; } = new SortedList<string, string>();

        [YamlMember(Alias = Constants.PropertyName.NameWithType)]
        [JsonProperty(Constants.PropertyName.NameWithType)]
        public string NameWithType { get; set; }

        [ExtensibleMember(Constants.ExtensionMemberPrefix.NameWithType)]
        [JsonIgnore]
        public SortedList<string, string> NameWithTypeInDevLangs { get; private set; } = new SortedList<string, string>();

        [YamlMember(Alias = Constants.PropertyName.FullName)]
        [JsonProperty(Constants.PropertyName.FullName)]
        public string FullName { get; set; }

        [ExtensibleMember(Constants.ExtensionMemberPrefix.FullName)]
        [JsonIgnore]
        public SortedList<string, string> FullNameInDevLangs { get; private set; } = new SortedList<string, string>();

        [ExtensibleMember(Constants.ExtensionMemberPrefix.Spec)]
        [JsonIgnore]
        public SortedList<string, List<SpecViewModel>> Specs { get; private set; } = new SortedList<string, List<SpecViewModel>>();

        [ExtensibleMember]
        [JsonIgnore]
        public Dictionary<string, object> Additional { get; private set; } = new Dictionary<string, object>();

        [EditorBrowsable(EditorBrowsableState.Never)]
        [YamlIgnore]
        [JsonExtensionData]
        [UniqueIdentityReferenceIgnore]
        [MarkdownContentIgnore]
        public CompositeDictionary AdditionalJson =>
            CompositeDictionary
                .CreateBuilder()
                .Add(Constants.ExtensionMemberPrefix.Name, NameInDevLangs, JTokenConverter.Convert<string>)
                .Add(Constants.ExtensionMemberPrefix.NameWithType, NameWithTypeInDevLangs, JTokenConverter.Convert<string>)
                .Add(Constants.ExtensionMemberPrefix.FullName, FullNameInDevLangs, JTokenConverter.Convert<string>)
                .Add(Constants.ExtensionMemberPrefix.Spec, Specs, JTokenConverter.Convert<List<SpecViewModel>>)
                .Add(string.Empty, Additional)
                .Create();

        public ReferenceViewModel Clone()
        {
            var copied = (ReferenceViewModel)MemberwiseClone();
            copied.Additional = new Dictionary<string, object>(Additional);
            copied.FullNameInDevLangs = new SortedList<string, string>(FullNameInDevLangs);
            copied.NameInDevLangs = new SortedList<string, string>(NameInDevLangs);
            copied.NameWithTypeInDevLangs = new SortedList<string, string>(NameWithTypeInDevLangs);
            copied.Specs = new SortedList<string, List<SpecViewModel>>(Specs.ToDictionary(s => s.Key, s => new List<SpecViewModel>(s.Value)));
            return copied;
        }
    }
}
