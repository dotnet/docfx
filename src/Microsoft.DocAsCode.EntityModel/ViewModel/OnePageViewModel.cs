namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Diagnostics;
    using YamlDotNet.Serialization;

    public class TocViewModel : List<ItemViewModel>
    {
        private static List<string> TocMetadataList = new List<string>
                                                           {
                                                               "uid",
                                                               "name",
                                                               "href",
                                                           };
        public TocViewModel() : base() { }
        public TocViewModel(IEnumerable<ItemViewModel> list)
            : base(list)
        {
        }

        public static TocViewModel Convert(MetadataItem item)
        {
            Debug.Assert(item.Type == MemberType.Toc);
            if (item.Type != MemberType.Toc) return null;
            var tocList = new TocViewModel();

            foreach (var namespaceItem in item.Items)
            {
                var namespaceTocViewModel = ItemViewModel.Convert(namespaceItem, TocMetadataList);
                if (namespaceItem.Items != null)
                {
                    var tocSubList = new TocViewModel();
                    foreach (var classItem in namespaceItem.Items)
                    {
                        var classTocViewModel = ItemViewModel.Convert(classItem, TocMetadataList);
                        tocSubList.Add(classTocViewModel);
                    }
                    namespaceTocViewModel["items"] = tocSubList;
                }

                tocList.Add(namespaceTocViewModel);
            }

            return tocList;
        }
    }

    public class OnePageViewModel
    {
        private static List<string> FullMetadataList = new List<string>
                                                           {
                                                               "uid",
                                                               "href",
                                                               "name",
                                                               "fullName",
                                                               "type",
                                                               "source",
                                                               "summary",
                                                               "remarks",
                                                               "exceptions",
                                                               "syntax",
                                                               "inheritance",
                                                               "implements",
                                                               "inheritedMembers",
                                                               "parent",
                                                               "id",
                                                               "children",
                                                               "assemblies",
                                                               "namespace",
                                                               "overridden",
                                                           };

        private static List<string> LiteMetadataList = new List<string>
                                                           {
                                                               "uid", "href", "name", "type", "summary"
                                                           };
        [YamlMember(Alias = "items")]
        public List<ItemViewModel> Items { get; set; }

        [YamlMember(Alias = "references")]
        public List<ItemViewModel> References { get; set; }

        public static OnePageViewModel Convert(MetadataItem item)
        {
            List<ItemViewModel> items = new List<ItemViewModel>();
            List<ItemViewModel> refs = new List<ItemViewModel>();
            var mainItem = ItemViewModel.Convert(item, FullMetadataList);
            items.Add(mainItem);
            if (item.Items != null)
            {
                if (item.Type.AllowMultipleItems())
                {
                    items.AddRange(item.Items.Select(s => ItemViewModel.Convert(s, FullMetadataList)));
                }
            }

            return new OnePageViewModel
            {
                Items = items,
                References = ConvertReferences(item.References).ToList(),
            };
        }

        private static IEnumerable<ItemViewModel> ConvertReferences(Dictionary<string, ReferenceItem> references)
        {
            if (references == null)
            {
                yield break;
            }
            if (references.Count == 0)
            {
                yield break;
            }
            foreach (var r in references)
            {
                var item = new ItemViewModel();
                item["uid"] = r.Key;
                if (r.Value.Type != null)
                {
                    item["type"] = r.Value.Type.ToString();
                }
                if (r.Value.Parts != null)
                {
                    foreach (var lang in r.Value.Parts)
                    {
                        string langStr = string.Empty;
                        if (lang.Key != SyntaxLanguage.Default)
                        {
                            langStr = "." + lang.Key.ToString().ToLower();
                        }
                        item["name" + langStr] = GetName(lang.Value, l => l.DisplayName);
                        item["fullName" + langStr] = GetName(lang.Value, l => l.DisplayQualifiedNames);
                        if (r.Value.IsDefinition == true)
                        {
                            if (lang.Value.Count == 1)
                            {
                                item["isExternal"] = lang.Value[0].IsExternalPath;
                                if (lang.Value[0].Href != null)
                                {
                                    item["href"] = lang.Value[0].Href;
                                }
                            }
                            else
                            {
                                item["isExternal"] = lang.Value.Any(x => x.IsExternalPath);
                                item["href"] = lang.Value.FirstOrDefault(x => x.Href != null)?.Href;
                            }
                        }
                        else if (lang.Value.Count > 1)
                        {
                            item["spec" + langStr] = GetSpecItem(lang.Value).ToList();
                        }
                    }
                }
                if (r.Value.Definition != null)
                {
                    item["definition"] = r.Value.Definition;
                }
                if (r.Value.Parent != null)
                {
                    item["parent"] = r.Value.Parent;
                }
                if (r.Value.Summary != null)
                {
                    item["summary"] = r.Value.Summary;
                }
                yield return item;
            }
        }

        private static string GetName(List<LinkItem> list, Func<LinkItem, string> getName)
        {
            if (list == null || list.Count == 0)
            {
                Debug.Fail("Unexpected reference.");
                return null;
            }
            if (list.Count == 1)
            {
                return getName(list[0]);
            }
            return string.Concat(from item in list select getName(item));
        }

        private static IEnumerable<ItemViewModel> GetSpecItem(List<LinkItem> list)
        {
            foreach (var link in list)
            {
                var item = new ItemViewModel();
                if (link.Name != null)
                {
                    item["uid"] = link.Name;
                }
                item["name"] = link.DisplayName;
                item["fullName"] = link.DisplayQualifiedNames;
                if (link.Name != null)
                {
                    item["isExternal"] = link.IsExternalPath;
                    if (link.Href != null)
                    {
                        item["href"] = link.Href;
                    }
                }
                yield return item;
            }
        }
    }
}
