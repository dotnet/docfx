// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;

namespace Docfx.Dotnet;

internal static class YamlViewModelExtensions
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
        MetadataItem shrinkedItem = new()
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
                shrinkedItem.Items ??= [];

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
        MetadataItem shrinkedItem = new()
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
                    shrinkedItem.Items ??= [];

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

    public static List<TocItemViewModel> ToTocViewModel(this MetadataItem item, string parentNamespace = "")
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
                foreach (var child in item.Items
                    .OrderBy(x => x.Type == MemberType.Namespace ? 0 : 1)
                    .ThenBy(x => x.Name)
                )
                {
                    result.Add(child.ToTocItemViewModel(parentNamespace));
                }
                return result;
            default:
                return null;
        }
    }

    public static TocItemViewModel ToTocItemViewModel(this MetadataItem item, string parentNamespace)
    {
        var result = new TocItemViewModel
        {
            Uid = item.Name,
            Name = item.DisplayNames.GetLanguageProperty(SyntaxLanguage.Default),
            Type = item.Type.ToString(),
        };

        if (item.Type is MemberType.Namespace)
        {
            if (result.Name.StartsWith(parentNamespace))
                result.Name = result.Name.Substring(parentNamespace.Length);
            parentNamespace = $"{item.Name}.";
        }

        if (item.Items != null)
        {
            result.Items = item.ToTocViewModel(parentNamespace);
        }
        return result;
    }

    public static PageViewModel ToPageViewModel(this MetadataItem model, ExtractMetadataConfig config)
    {
        if (model == null)
        {
            return null;
        }
        var result = new PageViewModel();
        result.Items.Add(model.ToItemViewModel(config));
        if (model.Type.AllowMultipleItems())
        {
            AddChildren(model, result, config);
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
        if (model.Value.NameParts is { Count: > 0 })
        {
            result.Name = GetName(model.Value.NameParts, SyntaxLanguage.Default);
            var nameForCSharp = GetName(model.Value.NameParts, SyntaxLanguage.CSharp);
            if (result.Name != nameForCSharp)
            {
                result.NameInDevLangs[Constants.DevLang.CSharp] = nameForCSharp;
            }
            var nameForVB = GetName(model.Value.NameParts, SyntaxLanguage.VB);
            if (result.Name != nameForVB)
            {
                result.NameInDevLangs[Constants.DevLang.VB] = nameForVB;
            }

            result.NameWithType = GetName(model.Value.NameWithTypeParts, SyntaxLanguage.Default);
            var nameWithTypeForCSharp = GetName(model.Value.NameWithTypeParts, SyntaxLanguage.CSharp);
            if (result.NameWithType != nameWithTypeForCSharp)
            {
                result.NameWithTypeInDevLangs[Constants.DevLang.CSharp] = nameWithTypeForCSharp;
            }
            var nameWithTypeForVB = GetName(model.Value.NameWithTypeParts, SyntaxLanguage.VB);
            if (result.NameWithType != nameWithTypeForVB)
            {
                result.NameWithTypeInDevLangs[Constants.DevLang.VB] = nameWithTypeForVB;
            }

            result.FullName = GetName(model.Value.QualifiedNameParts, SyntaxLanguage.Default);
            var fullnameForCSharp = GetName(model.Value.QualifiedNameParts, SyntaxLanguage.CSharp);
            if (result.FullName != fullnameForCSharp)
            {
                result.FullNameInDevLangs[Constants.DevLang.CSharp] = fullnameForCSharp;
            }
            var fullnameForVB = GetName(model.Value.QualifiedNameParts, SyntaxLanguage.VB);
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

    public static ItemViewModel ToItemViewModel(this MetadataItem model, ExtractMetadataConfig config)
    {
        if (model == null)
        {
            return null;
        }

        var children = model.Type is MemberType.Enum && config.EnumSortOrder is EnumSortOrder.DeclaringOrder
            ? model.Items?.Select(x => x.Name).ToList()
            : model.Items?.Select(x => x.Name).OrderBy(s => s, StringComparer.Ordinal).ToList();

        var result = new ItemViewModel
        {
            Uid = model.Name,
            CommentId = model.CommentId,
            IsExplicitInterfaceImplementation = model.IsExplicitInterfaceImplementation,
            IsExtensionMethod = model.IsExtensionMethod,
            Parent = model.Parent?.Name,
            Children = children,
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
            SeeAlsos = model.SeeAlsos,
            DerivedClasses = model.DerivedClasses,
            Inheritance = model.Inheritance,
            Implements = model.Implements,
            InheritedMembers = model.InheritedMembers,
            ExtensionMethods = model.ExtensionMethods,
            Attributes = model.Attributes,
        };

        if (model.Parent is { Name: not null } && !model.Name.StartsWith(model.Parent.Name))
        {
            result.Id = model.Name.Substring(model.Name.LastIndexOf('.') + 1);
        }
        else
        {
            result.Id = model.Name.Substring((model.Parent?.Name?.Length ?? -1) + 1);
        }

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
        if (model.Content is { Count: > 0 })
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

    private static void AddChildren(MetadataItem model, PageViewModel result, ExtractMetadataConfig config)
    {
        if (model.Items is { Count: > 0 })
        {
            foreach (var item in model.Items)
            {
                result.Items.Add(item.ToItemViewModel(config));
                AddChildren(item, result, config);
            }
        }
    }

    private static string GetName(SortedList<SyntaxLanguage, List<LinkItem>> parts, SyntaxLanguage language)
    {
        var list = parts?.GetLanguageProperty(language);
        if (list == null)
        {
            return null;
        }
        if (list.Count == 0)
        {
            return null;
        }
        if (list.Count == 1)
        {
            return list[0].DisplayName;
        }
        return string.Concat(list.Select(p => p.DisplayName));
    }

    private static List<SpecViewModel> GetSpec(ReferenceItem reference, SyntaxLanguage language)
    {
        var list = reference.NameParts.GetLanguageProperty(language);
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
        foreach (var list in reference.NameParts.Values)
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
        foreach (var list in reference.NameParts.Values)
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
