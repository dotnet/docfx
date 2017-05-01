// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.YamlSerialization;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class SyntaxDetailViewModel
    {
        [YamlMember(Alias = Constants.PropertyName.Content)]
        [JsonProperty(Constants.PropertyName.Content)]
        public string Content { get; set; }

        [ExtensibleMember(Constants.ExtensionMemberPrefix.Content)]
        [JsonIgnore]
        public SortedList<string, string> Contents { get; set; } = new SortedList<string, string>();

        [YamlIgnore]
        [JsonIgnore]
        public string ContentForCSharp
        {
            get
            {
                string result;
                Contents.TryGetValue("csharp", out result);
                return result;
            }
            set
            {
                if (value == null)
                {
                    Contents.Remove("csharp");
                }
                else
                {
                    Contents["csharp"] = value;
                }
            }
        }

        [YamlIgnore]
        [JsonIgnore]
        public string ContentForVB
        {
            get
            {
                string result;
                Contents.TryGetValue("vb", out result);
                return result;
            }
            set
            {
                if (value == null)
                {
                    Contents.Remove("vb");
                }
                else
                {
                    Contents["vb"] = value;
                }
            }
        }

        [YamlMember(Alias = "parameters")]
        [JsonProperty("parameters")]
        public List<ApiParameter> Parameters { get; set; }

        [YamlMember(Alias = "typeParameters")]
        [JsonProperty("typeParameters")]
        public List<ApiParameter> TypeParameters { get; set; }

        [YamlMember(Alias = "return")]
        [JsonProperty("return")]
        public ApiParameter Return { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [YamlIgnore]
        [JsonExtensionData]
        [UniqueIdentityReferenceIgnore]
        [MarkdownContentIgnore]
        public CompositeDictionary ExtensionData =>
            CompositeDictionary
                .CreateBuilder()
                .Add(Constants.ExtensionMemberPrefix.Content, Contents, JTokenConverter.Convert<string>)
                .Create();
    }
}
