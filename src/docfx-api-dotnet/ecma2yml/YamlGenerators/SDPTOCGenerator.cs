using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Collections.Generic;
using System.Linq;

namespace ECMA2Yaml
{
    public static class SDPTOCGenerator
    {
        public static TOCRootYamlModel Generate(ECMAStore store)
        {
            TOCRootYamlModel toc = new TOCRootYamlModel()
            {
                Items = new List<TOCNodeYamlModel>()
            };

            foreach (var ns in store.Namespaces.Values)
            {
                if (ns.Types?.Count > 0)
                {
                    toc.Items.Add(GenerateTocItemForNamespace(ns));
                }
            }
            return toc;
        }

        private static TOCNodeYamlModel GenerateTocItemForNamespace(Namespace ns)
        {
            var nsToc = new TOCNodeYamlModel()
            {
                Uid = string.IsNullOrEmpty(ns.Uid) ? null : ns.Uid,
                Name = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name,
                Items = new List<TOCNodeYamlModel>(ns.Types.Select(t => GenerateTocItemForType(t)).ToList())
            };
            if (ns.Monikers?.Count > 0)
            {
                nsToc.Monikers = ns.Monikers.ToArray();
            }
            return nsToc;
        }

        private static TOCNodeYamlModel GenerateTocItemForType(Models.Type t)
        {
            var tToc = new TOCNodeYamlModel()
            {
                Uid = t.Uid,
                Name = t.Name,
                Items = GenerateTocItemsForMembers(t)
            };
            if (IsNeedAddMonikers(t.Parent.Monikers, t.Monikers))
            {
                tToc.Monikers = t.Monikers.ToArray();
            }
            return tToc;
        }

        private static List<TOCNodeYamlModel> GenerateTocItemsForMembers(Models.Type t)
        {
            if (t.Members == null
                || t.Members.Count == 0
                || t.ItemType == ItemType.Enum)
            {
                return null;
            }

            var items = new List<TOCNodeYamlModel>();
            foreach (var olGroup in t.Members.Where(m => m.Overload != null).GroupBy(m => m.Overload))
            {
                var ol = t.Overloads.FirstOrDefault(o => o.Uid == olGroup.Key);
                var tocEntry = new TOCNodeYamlModel()
                {
                    Uid = ol.Uid,
                    Name = ol.DisplayName
                };
                tocEntry.Type = ol.ItemType.ToString();
                if ((ol.ItemType == ItemType.Method || ol.ItemType == ItemType.Property) && olGroup.First().IsEII)
                {
                    tocEntry.IsEII = true;
                }
                if (IsNeedAddMonikers(t.Monikers, ol.Monikers))
                {
                    tocEntry.Monikers = ol.Monikers.ToArray();
                }
                items.Add(tocEntry);
            }
            foreach (var m in t.Members.Where(m => m.Overload == null))
            {
                var tocEntry = new TOCNodeYamlModel()
                {
                    Uid = m.Uid,
                    Name = m.DisplayName
                };
                tocEntry.Type = m.ItemType.ToString();
                if (IsNeedAddMonikers(t.Monikers, m.Monikers))
                {
                    tocEntry.Monikers = m.Monikers.ToArray();
                }
                items.Add(tocEntry);
            }
            return items;
        }

        private static bool IsNeedAddMonikers(HashSet<string> tMonikers, HashSet<string> mMonikers)
        {
            return mMonikers?.Count > 0 && tMonikers?.Count > 0 && !tMonikers.SetEquals(mMonikers);
        }
    }
}
