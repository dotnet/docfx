// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using YamlDotNet.Serialization;

    public class ReferenceViewModel
    {
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "parent")]
        public string Parent { get; set; }

        [YamlMember(Alias = "definition")]
        public string Definition { get; set; }

        [YamlMember(Alias = "isExternal")]
        public bool? IsExternal { get; set; }

        [YamlMember(Alias = "href")]
        public string Href { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "name.csharp")]
        public string NameForCSharp { get; set; }

        [YamlMember(Alias = "name.vb")]
        public string NameForVB { get; set; }

        [YamlMember(Alias = "fullName")]
        public string FullName { get; set; }

        [YamlMember(Alias = "fullName.csharp")]
        public string FullNameForCSharp { get; set; }

        [YamlMember(Alias = "fullName.vb")]
        public string FullNameForVB { get; set; }

        [YamlMember(Alias = "type")]
        public MemberType? Type { get; set; }

        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "spec.csharp")]
        public List<SpecViewModel> SpecForCSharp { get; set; }

        [YamlMember(Alias = "spec.vb")]
        public List<SpecViewModel> SpecForVB { get; set; }

        public static ReferenceViewModel FromModel(KeyValuePair<string, ReferenceItem> model)
        {
            Debug.Assert(model.Value != null, "Unexpected reference.");
            var result = new ReferenceViewModel
            {
                Uid = model.Key,
                Parent = model.Value.Parent,
                Definition = model.Value.Definition,
                Type = model.Value.Type,
                Summary = model.Value.Summary,
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
