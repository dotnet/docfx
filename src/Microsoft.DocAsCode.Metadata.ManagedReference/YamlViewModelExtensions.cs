// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public static class YamlViewModelExtensions
    {
        public static bool IsPageLevel(this MemberType type)
        {
            return type == MemberType.Namespace || type == MemberType.Class || type == MemberType.Enum || type == MemberType.Delegate || type == MemberType.Interface || type == MemberType.Struct;
        }

        /// <summary>
        /// Allow multiple items in one yml file
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool AllowMultipleItems(this MemberType type)
        {
            return type == MemberType.Class || type == MemberType.Enum || type == MemberType.Delegate || type == MemberType.Interface || type == MemberType.Struct;
        }

        public static MetadataItem ShrinkToSimpleToc(this MetadataItem item)
        {
            MetadataItem shrinkedItem = new MetadataItem()
            {
                Name = item.Name,
                DisplayNames = item.DisplayNames,
                Items = null
            };
            if (item.Items == null)
            {
                return shrinkedItem;
            }

            if (item.Type == MemberType.Toc || item.Type == MemberType.Namespace)
            {
                foreach (var i in item.Items)
                {
                    if (shrinkedItem.Items == null)
                    {
                        shrinkedItem.Items = new List<MetadataItem>();
                    }

                    if (i.IsInvalid)
                    {
                        continue;
                    }
                    var shrinkedI = i.ShrinkToSimpleToc();
                    shrinkedItem.Items.Add(shrinkedI);
                }

            }

            return shrinkedItem;
        }

        /// <summary>
        /// Only when Namespace is not empty, return it
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static MetadataItem ShrinkToSimpleTocWithNamespaceNotEmpty(this MetadataItem item)
        {
            MetadataItem shrinkedItem = new MetadataItem()
            {
                Name = item.Name,
                DisplayNames = item.DisplayNames,
                Type = item.Type,
                Items = null
            };
            if (item.Type == MemberType.Toc || item.Type == MemberType.Namespace)
            {
                if (item.Items != null)
                {
                    foreach (var i in item.Items)
                    {
                        if (shrinkedItem.Items == null)
                        {
                            shrinkedItem.Items = new List<MetadataItem>();
                        }

                        if (i.IsInvalid)
                        {
                            continue;
                        }
                        var shrinkedI = i.ShrinkToSimpleTocWithNamespaceNotEmpty();
                        if (shrinkedI != null)
                        {
                            shrinkedItem.Items.Add(shrinkedI);
                        }
                    }
                }
            }

            if (item.Type == MemberType.Namespace)
            {
                if (shrinkedItem.Items == null || shrinkedItem.Items.Count == 0)
                {
                    return null;
                }
            }

            return shrinkedItem;
        }

        public static TocViewModel ToTocViewModel(this MetadataItem item)
        {
            if (item == null)
            {
                Debug.Fail("item is null.");
                return null;
            }
            switch (item.Type)
            {
                case MemberType.Toc:
                case MemberType.Namespace:
                    var result = new List<TocItemViewModel>();
                    foreach (var child in item.Items)
                    {
                        result.Add(child.ToTocItemViewModel());
                    }
                    return new TocViewModel(result.OrderBy(s => s.Name));
                default:
                    return null;
            }
        }

        public static TocItemViewModel ToTocItemViewModel(this MetadataItem item)
        {
            var result = new TocItemViewModel
            {
                Uid = item.Name,
                Name = item.DisplayNames.GetLanguageProperty(SyntaxLanguage.Default),
            };
            var nameForCSharp = item.DisplayNames.GetLanguageProperty(SyntaxLanguage.CSharp);
            if (nameForCSharp != result.Name)
            {
                result.NameForCSharp = nameForCSharp;
            }
            var nameForVB = item.DisplayNames.GetLanguageProperty(SyntaxLanguage.VB);
            if (nameForVB != result.Name)
            {
                result.NameForVB = nameForVB;
            }
            if (item.Items != null)
            {
                result.Items = item.ToTocViewModel();
            }
            return result;
        }

        public static PageViewModel ToPageViewModel(this MetadataItem model)
        {
            if (model == null)
            {
                return null;
            }
            var result = new PageViewModel();
            result.Items.Add(model.ToItemViewModel());
            if (model.Type.AllowMultipleItems())
            {
                AddChildren(model, result);
            }
            foreach (var item in model.References)
            {
                result.References.Add(ToReferenceViewModel(item));
            }
            return result;
        }

        private static ReferenceViewModel ToReferenceViewModel(KeyValuePair<string, ReferenceItem> model)
        {
            Debug.Assert(model.Value != null, "Unexpected reference.");
            var result = new ReferenceViewModel
            {
                Uid = model.Key,
                CommentId = model.Value.CommentId,
                Parent = model.Value.Parent,
                Definition = model.Value.Definition,
            };
            if (model.Value.Parts != null && model.Value.Parts.Count > 0)
            {
                result.Name = GetName(model.Value, SyntaxLanguage.Default, l => l.DisplayName);
                var nameForCSharp = GetName(model.Value, SyntaxLanguage.CSharp, l => l.DisplayName);
                if (result.Name != nameForCSharp)
                {
                    result.NameInDevLangs[Constants.DevLang.CSharp] = nameForCSharp;
                }
                var nameForVB = GetName(model.Value, SyntaxLanguage.VB, l => l.DisplayName);
                if (result.Name != nameForVB)
                {
                    result.NameInDevLangs[Constants.DevLang.VB] = nameForVB;
                }

                result.NameWithType = GetName(model.Value, SyntaxLanguage.Default, l => l.DisplayNamesWithType);
                var nameWithTypeForCSharp = GetName(model.Value, SyntaxLanguage.CSharp, l => l.DisplayNamesWithType);
                if (result.NameWithType != nameWithTypeForCSharp)
                {
                    result.NameWithTypeInDevLangs[Constants.DevLang.CSharp] = nameWithTypeForCSharp;
                }
                var nameWithTypeForVB = GetName(model.Value, SyntaxLanguage.VB, l => l.DisplayNamesWithType);
                if (result.NameWithType != nameWithTypeForVB)
                {
                    result.NameWithTypeInDevLangs[Constants.DevLang.VB] = nameWithTypeForVB;
                }

                result.FullName = GetName(model.Value, SyntaxLanguage.Default, l => l.DisplayQualifiedNames);
                var fullnameForCSharp = GetName(model.Value, SyntaxLanguage.CSharp, l => l.DisplayQualifiedNames);
                if (result.FullName != fullnameForCSharp)
                {
                    result.FullNameInDevLangs[Constants.DevLang.CSharp] = fullnameForCSharp;
                }
                var fullnameForVB = GetName(model.Value, SyntaxLanguage.VB, l => l.DisplayQualifiedNames);
                if (result.FullName != fullnameForVB)
                {
                    result.FullNameInDevLangs[Constants.DevLang.VB] = fullnameForVB;
                }

                result.Specs[Constants.DevLang.CSharp] = GetSpec(model.Value, SyntaxLanguage.CSharp);
                result.Specs[Constants.DevLang.VB] = GetSpec(model.Value, SyntaxLanguage.VB);
                result.IsExternal = GetIsExternal(model.Value);
                result.Href = GetHref(model.Value);
            }
            else
            {
                result.IsExternal = true;
            }
            return result;
        }

        public static ItemViewModel ToItemViewModel(this MetadataItem model)
        {
            if (model == null)
            {
                return null;
            }
            var result = new ItemViewModel
            {
                Uid = model.Name,
                CommentId = model.CommentId,
                IsExplicitInterfaceImplementation = model.IsExplicitInterfaceImplementation,
                IsExtensionMethod = model.IsExtensionMethod,
                Parent = model.Parent?.Name,
                Children = model.Items?.Select(x => x.Name).OrderBy(s => s).ToList(),
                Type = model.Type,
                Source = model.Source,
                Documentation = model.Documentation,
                AssemblyNameList = model.AssemblyNameList,
                NamespaceName = model.NamespaceName,
                Summary = model.Summary,
                Remarks = model.Remarks,
                Examples = model.Examples,
                Syntax = model.Syntax.ToSyntaxDetailViewModel(),
                Overridden = model.Overridden,
                Overload = model.Overload,
                Exceptions = model.Exceptions,
                Sees = model.Sees,
                SeeAlsos = model.SeeAlsos,
                DerivedClasses = model.DerivedClasses,
                Inheritance = model.Inheritance,
                Implements = model.Implements,
                InheritedMembers = model.InheritedMembers,
                ExtensionMethods = model.ExtensionMethods,
                Attributes = model.Attributes,
            };

            result.Id = model.Name.Substring((model.Parent?.Name?.Length ?? -1) + 1);

            result.Name = model.DisplayNames.GetLanguageProperty(SyntaxLanguage.Default);
            var nameForCSharp = model.DisplayNames.GetLanguageProperty(SyntaxLanguage.CSharp);
            if (result.Name != nameForCSharp)
            {
                result.NameForCSharp = nameForCSharp;
            }
            var nameForVB = model.DisplayNames.GetLanguageProperty(SyntaxLanguage.VB);
            if (result.Name != nameForVB)
            {
                result.NameForVB = nameForVB;
            }

            result.NameWithType = model.DisplayNamesWithType.GetLanguageProperty(SyntaxLanguage.Default);
            var nameWithTypeForCSharp = model.DisplayNamesWithType.GetLanguageProperty(SyntaxLanguage.CSharp);
            if (result.NameWithType != nameWithTypeForCSharp)
            {
                result.NameWithTypeForCSharp = nameWithTypeForCSharp;
            }
            var nameWithTypeForVB = model.DisplayNamesWithType.GetLanguageProperty(SyntaxLanguage.VB);
            if (result.NameWithType != nameWithTypeForVB)
            {
                result.NameWithTypeForVB = nameWithTypeForVB;
            }

            result.FullName = model.DisplayQualifiedNames.GetLanguageProperty(SyntaxLanguage.Default);
            var fullnameForCSharp = model.DisplayQualifiedNames.GetLanguageProperty(SyntaxLanguage.CSharp);
            if (result.FullName != fullnameForCSharp)
            {
                result.FullNameForCSharp = fullnameForCSharp;
            }
            var fullnameForVB = model.DisplayQualifiedNames.GetLanguageProperty(SyntaxLanguage.VB);
            if (result.FullName != fullnameForVB)
            {
                result.FullNameForVB = fullnameForVB;
            }

            var modifierCSharp = model.Modifiers.GetLanguageProperty(SyntaxLanguage.CSharp);
            if (modifierCSharp?.Count > 0)
            {
                result.Modifiers["csharp"] = modifierCSharp;
            }
            var modifierForVB = model.Modifiers.GetLanguageProperty(SyntaxLanguage.VB);
            if (modifierForVB?.Count > 0)
            {
                result.Modifiers["vb"] = modifierForVB;
            }

            return result;
        }

        public static SyntaxDetailViewModel ToSyntaxDetailViewModel(this SyntaxDetail model)
        {
            if (model == null)
            {
                return null;
            }
            var result = new SyntaxDetailViewModel
            {
                Parameters = model.Parameters,
                TypeParameters = model.TypeParameters,
                Return = model.Return,
            };
            if (model.Content != null && model.Content.Count > 0)
            {
                result.Content = model.Content.GetLanguageProperty(SyntaxLanguage.Default);
                var contentForCSharp = model.Content.GetLanguageProperty(SyntaxLanguage.CSharp);
                if (result.Content != contentForCSharp)
                {
                    result.ContentForCSharp = contentForCSharp;
                }
                var contentForVB = model.Content.GetLanguageProperty(SyntaxLanguage.VB);
                if (result.Content != contentForVB)
                {
                    result.ContentForVB = contentForVB;
                }
            }
            return result;
        }

        public static SpecViewModel ToSpecViewModel(this LinkItem model)
        {
            if (model == null)
            {
                return null;
            }
            var result = new SpecViewModel
            {
                Uid = model.Name,
                Name = model.DisplayName,
                NameWithType = model.DisplayNamesWithType,
                FullName = model.DisplayQualifiedNames,
                IsExternal = model.IsExternalPath,
                Href = model.Href,
            };
            return result;
        }

        public static TValue GetLanguageProperty<TValue>(this SortedList<SyntaxLanguage, TValue> dict, SyntaxLanguage language, TValue defaultValue = null)
            where TValue : class
        {
            if (dict.TryGetValue(language, out TValue result))
            {
                return result;
            }
            if (language == SyntaxLanguage.Default && dict.Count > 0)
            {
                return dict.Values[0];
            }
            return defaultValue;
        }

        private static void AddChildren(MetadataItem model, PageViewModel result)
        {
            if (model.Items != null && model.Items.Count > 0)
            {
                foreach (var item in model.Items)
                {
                    result.Items.Add(item.ToItemViewModel());
                    AddChildren(item, result);
                }
            }
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
            return list.ConvertAll(s => s.ToSpecViewModel());
        }

        private static bool? GetIsExternal(ReferenceItem reference)
        {
            if (reference.IsDefinition == null)
            {
                return null;
            }
            if (reference.IsDefinition == false)
            {
                if (reference.Definition != null)
                {
                    return null;
                }
                return true;
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
            return null;
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
