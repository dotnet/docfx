// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class ApplyApiScan : BaseDocumentBuildStep
    {
        // to-do: eii special logic and property getter/setter visibility
        private static readonly IDictionary<MemberType, Func<ItemViewModel, IEnumerable<string>>> _apiNameMapper = new Dictionary<MemberType, Func<ItemViewModel, IEnumerable<string>>>
        {
            { MemberType.Class, item => { return GetTypeApiNamesCore(item, string.Empty); } },
            { MemberType.Struct, item => { return GetTypeApiNamesCore(item, string.Empty); } },
            { MemberType.Interface, item => { return GetTypeApiNamesCore(item, string.Empty); } },
            { MemberType.Enum, item => { return GetTypeApiNamesCore(item, string.Empty); } },
            { MemberType.Delegate, item => { return GetTypeApiNamesCore(item, string.Empty, "..ctor", ".Invoke", ".BeginInvoke", ".EndInvoke"); } },
            { MemberType.Constructor, item => { return GetMemberApiNamesCore(item, "."); } },
            { MemberType.Method, item => { return GetMemberApiNamesCore(item, "."); } },
            { MemberType.Operator, item => { return GetMemberApiNamesCore(item, ".", ".op_"); } },
            { MemberType.Property, item => { return GetMemberApiNamesCore(item, ".", ".get_", ".set_"); } },
            { MemberType.Event, item => { return GetMemberApiNamesCore(item, ".", ".add_", ".remove_"); } },
        };

        public override int BuildOrder => 0x10;

        public override string Name => nameof(ApplyApiScan);

        public override void Build(FileModel model, IHostService host)
        {
            if (model.Type != DocumentType.Article)
            {
                return;
            }
            var page = model.Content as PageViewModel;
            if (page != null)
            {
                var apinames = GetApiNamesFromModel(page);
                if (apinames.Count > 0)
                {
                    page.Metadata["apiname"] = apinames;
                    page.Metadata["apilocation"] = GetApiLocationsFromModel(page);
                    page.Metadata["topictype"] = "apiref";
                    page.Metadata["apitype"] = "Assembly";
                }
            }
        }

        private static List<string> GetApiNamesFromModel(PageViewModel page)
        {
            return (from item in page.Items
                    from name in GetApiNamesCore(item)
                    select name).Distinct().ToList();
        }

        private static List<string> GetApiLocationsFromModel(PageViewModel page)
        {
            return (from item in page.Items
                    from assembly in item.AssemblyNameList ?? new List<string>()
                    select assembly + ".dll").Distinct().ToList();
        }

        private static IEnumerable<string> GetApiNamesCore(ItemViewModel item)
        {
            if (!item.Type.HasValue)
            {
                return new List<string>();
            }
            Func<ItemViewModel, IEnumerable<string>> func;
            _apiNameMapper.TryGetValue(item.Type.Value, out func);
            if (func == null)
            {
                return new List<string>();
            }
            return func(item);
        }

        private static void GetTypeAndMemberName(string name, out string type, out string member)
        {
            int index = name.IndexOf('(');
            if (index != -1)
            {
                name = name.Remove(index);
            }
            index = name.LastIndexOf(".");
            if (index == -1)
            {
                throw new InvalidOperationException(string.Format("api name {0} is illegal.", name));
            }
            type = name.Substring(0, index);
            member = name.Substring(index + 1);
        }

        private static IEnumerable<string> GetTypeApiNamesCore(ItemViewModel item, params string[] seperators)
        {
            foreach (var seperator in seperators)
            {
                yield return item.FullName + seperator;
            }
        }

        private static IEnumerable<string> GetMemberApiNamesCore(ItemViewModel item, params string[] seperators)
        {
            string type, member;
            GetTypeAndMemberName(item.FullName, out type, out member);
            if (item.Type == MemberType.Constructor)
            {
                member = ".ctor";
            }
            foreach (var seperator in seperators)
            {
                yield return type + seperator + member;
            }
        }
    }
}
