// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class ReferenceViewModel
    {
        [YamlMember(Alias = Constants.PropertyName.Uid)]
        [JsonProperty(Constants.PropertyName.Uid)]
        public string Uid { get; set; }

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

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [ExtensibleMember("name.")]
        [JsonIgnore]
        public SortedList<string, string> NameInDevLangs { get; } = new SortedList<string, string>();

        [YamlIgnore]
        [JsonIgnore]
        public string NameForCSharp
        {
            get
            {
                string result;
                NameInDevLangs.TryGetValue("csharp", out result);
                return result;
            }
            set { NameInDevLangs["csharp"] = value; }
        }

        [YamlIgnore]
        [JsonIgnore]
        public string NameForVB
        {
            get
            {
                string result;
                NameInDevLangs.TryGetValue("vb", out result);
                return result;
            }
            set { NameInDevLangs["vb"] = value; }
        }

        [YamlMember(Alias = "fullName")]
        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [ExtensibleMember("fullname.")]
        [JsonIgnore]
        public SortedList<string, string> FullNameInDevLangs { get; } = new SortedList<string, string>();

        [YamlIgnore]
        [JsonIgnore]
        public string FullNameForCSharp
        {
            get
            {
                string result;
                FullNameInDevLangs.TryGetValue("csharp", out result);
                return result;
            }
            set { FullNameInDevLangs["csharp"] = value; }
        }

        [YamlIgnore]
        [JsonIgnore]
        public string FullNameForVB
        {
            get
            {
                string result;
                FullNameInDevLangs.TryGetValue("vb", out result);
                return result;
            }
            set { FullNameInDevLangs["vb"] = value; }
        }

        [ExtensibleMember("spec.")]
        [JsonIgnore]
        public SortedList<string, List<SpecViewModel>> Specs { get; } = new SortedList<string, List<SpecViewModel>>();

        [YamlIgnore]
        [JsonIgnore]
        public List<SpecViewModel> SpecForCSharp
        {
            get
            {
                List<SpecViewModel> result;
                Specs.TryGetValue("csharp", out result);
                return result;
            }
            set { Specs["csharp"] = value; }
        }

        [YamlIgnore]
        [JsonIgnore]
        public List<SpecViewModel> SpecForVB
        {
            get
            {
                List<SpecViewModel> result;
                Specs.TryGetValue("vb", out result);
                return result;
            }
            set { Specs["vb"] = value; }
        }

        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public MemberType? Type { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "syntax")]
        [JsonProperty("syntax")]
        public SyntaxDetailViewModel Syntax { get; set; }

        [YamlMember(Alias = "platform")]
        [JsonProperty("platform")]
        public List<string> Platform { get; set; }

        [ExtensibleMember]
        [JsonIgnore]
        public Dictionary<string, object> Additional { get; } = new Dictionary<string, object>();

        [EditorBrowsable(EditorBrowsableState.Never)]
        [YamlIgnore]
        [JsonExtensionData(ReadData = false, WriteData = true)]
        public Dictionary<string, object> AdditionalJson
        {
            get
            {
                var dict = new Dictionary<string, object>();
                foreach (var item in NameInDevLangs)
                {
                    dict["name." + item.Key] = item.Value;
                }
                foreach (var item in FullNameInDevLangs)
                {
                    dict["fullname." + item.Key] = item.Value;
                }
                foreach (var item in Specs)
                {
                    dict["spec." + item.Key] = item.Value;
                }
                foreach (var item in Additional)
                {
                    dict[item.Key] = item.Value;
                }
                return dict;
            }
            set { }
        }

        public static ReferenceViewModel FromModel(KeyValuePair<string, ReferenceItem> model)
        {
            Debug.Assert(model.Value != null, "Unexpected reference.");
            var result = new ReferenceViewModel
            {
                Uid = model.Key,
                Parent = model.Value.Parent,
                Definition = model.Value.Definition,
            };
            if (model.Value.Parts != null && model.Value.Parts.Count > 0)
            {
                result.Name = GetName(model.Value, SyntaxLanguage.Default, l => l.DisplayName);
                var nameForCSharp = GetName(model.Value, SyntaxLanguage.CSharp, l => l.DisplayName);
                if (result.Name != nameForCSharp)
                {
                    result.NameForCSharp = nameForCSharp;
                }
                var nameForVB = GetName(model.Value, SyntaxLanguage.VB, l => l.DisplayName);
                if (result.Name != nameForVB)
                {
                    result.NameForVB = nameForVB;
                }

                result.FullName = GetName(model.Value, SyntaxLanguage.Default, l => l.DisplayQualifiedNames);
                var fullnameForCSharp = GetName(model.Value, SyntaxLanguage.CSharp, l => l.DisplayQualifiedNames);
                if (result.FullName != fullnameForCSharp)
                {
                    result.FullNameForCSharp = fullnameForCSharp;
                }
                var fullnameForVB = GetName(model.Value, SyntaxLanguage.VB, l => l.DisplayQualifiedNames);
                if (result.FullName != fullnameForVB)
                {
                    result.FullNameForVB = fullnameForVB;
                }

                result.SpecForCSharp = GetSpec(model.Value, SyntaxLanguage.CSharp);
                result.SpecForVB = GetSpec(model.Value, SyntaxLanguage.VB);
                result.IsExternal = GetIsExternal(model.Value);
                result.Href = GetHref(model.Value);
            }
            return result;
        }

        private static string GetName(ReferenceItem reference, SyntaxLanguage language, Converter<LinkItem, string> getName)
        {
            var list = reference.Parts.GetLanguageProperty(language);
            if (list == null)
            {
                return null;
            }
            if (list.Count == 0)
            {
                Debug.Fail("Unexpected reference.");
                return null;
            }
            if (list.Count == 1)
            {
                return getName(list[0]);
            }
            return string.Concat(list.ConvertAll(item => getName(item)).ToArray());
        }

        private static List<SpecViewModel> GetSpec(ReferenceItem reference, SyntaxLanguage language)
        {
            var list = reference.Parts.GetLanguageProperty(language);
            if (list == null || list.Count <= 1)
            {
                return null;
            }
            return list.ConvertAll(SpecViewModel.FromModel);
        }

        private static bool? GetIsExternal(ReferenceItem reference)
        {
            if (reference.IsDefinition != true)
            {
                return null;
            }
            foreach (var list in reference.Parts.Values)
            {
                foreach (var item in list)
                {
                    if (item.IsExternalPath)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static string GetHref(ReferenceItem reference)
        {
            foreach (var list in reference.Parts.Values)
            {
                foreach (var item in list)
                {
                    if (item.Href != null)
                    {
                        return item.Href;
                    }
                }
            }
            return null;
        }

    }
}
