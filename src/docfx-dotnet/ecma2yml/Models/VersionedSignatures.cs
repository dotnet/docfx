using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ECMA2Yaml.Models
{
    public class VersionedSignatures
    {
        readonly string csharp = ECMADevLangs.OPSMapping[ECMADevLangs.CSharp];

        public Dictionary<string, List<VersionedString>> Dict { get; set; }
        public SortedList<string, List<string>> CombinedModifiers { get; private set; }
        public HashSet<string> DevLangs { get; private set; }
        public string DocId { get; private set; }

        public bool IsPublishSealedClass
        {
            get => Dict.ContainsKey(ECMADevLangs.CSharp)
                && Dict[ECMADevLangs.CSharp].All(s => s.Value.StartsWith("public sealed class"));
        }

        public bool IsProtected
        {
            get => CombinedModifiers.TryGetValue(csharp, out var list) && list.Contains("protected");
        }

        public bool IsAbstract
        {
            get => CombinedModifiers.TryGetValue(csharp, out var list) && list.Contains("abstract");
        }

        public bool IsStatic
        {
            get => CombinedModifiers.TryGetValue(csharp, out var list) && list.Contains("static");
        }

        public bool IsPublicModule
        {
            get => Dict.ContainsKey(ECMADevLangs.VB)
                && Dict[ECMADevLangs.VB].All(s => s.Value.StartsWith("Public Module"));
        }

        public List<VersionedString> GetPublishSealedClasses()
        {
            if (Dict.ContainsKey(ECMADevLangs.CSharp))
            {
                return Dict[ECMADevLangs.CSharp].Where(s => s.Value.StartsWith("public sealed class")).ToList();
            }

            return null;
        }

        public VersionedSignatures(IEnumerable<XElement> sigElements, ItemType? itemType = null)
        {
            Dict = sigElements.Select(sig =>
            {
                var val = (sig.Attribute("Value") ?? sig.Attribute("Usage"))?.Value;
                var lang = sig.Attribute("Language").Value;
                var monikers = ECMALoader.LoadFrameworkAlternate(sig);
                string monikersStr = null;
                // DocId should not have any monikers attached to it.
                // But we have to scan all versions to workaround https://ceapex.visualstudio.com/Engineering/_workitems/edit/148316
                if (lang == "DocId")
                {
                    DocId = val;
                }
                if (monikers != null)
                {
                    monikersStr = string.Join(";", monikers.OrderBy(m => m));
                }
                return (val, lang, monikersStr, monikers);
            })
            .GroupBy(t => t.lang)
            .ToDictionary(g => g.Key,
                g => g.Count() > 1
                ? g.Select(t => new VersionedString(t.monikers.ToHashSet(), t.val)).ToList()
                : g.Select(t => new VersionedString(null, t.val)).ToList() // remove monikers if there's only one version
                );

            DevLangs = Dict.Keys.Where(k => ECMADevLangs.OPSMapping.ContainsKey(k)).Select(k => ECMADevLangs.OPSMapping[k]).ToHashSet();

            foreach (var sig in Dict[ECMADevLangs.CSharp])
            {
                var modifierList = ParseModifiersFromSignatures(sig.Value, itemType);
                if (CombinedModifiers == null)
                {
                    CombinedModifiers = modifierList;
                }
                if (modifierList?.Count > 0
                    && modifierList.TryGetValue(csharp, out List<string> mods)
                    && CombinedModifiers.TryGetValue(csharp, out var existingMods))
                {
                    CombinedModifiers[csharp] = existingMods.ConcatList(mods).Distinct().ToList();
                }
            }
        }

        private SortedList<string, List<string>> ParseModifiersFromSignatures(string sig, ItemType? itemType = null)
        {
            if (sig == null)
            {
                return null;
            }

            var modifiers = new SortedList<string, List<string>>();
            var mods = new List<string>();

            if (itemType == ItemType.AttachedProperty)
            {
                if (sig.StartsWith("see Get"))
                {
                    mods.Add("get");
                }
                if (sig.Contains("and Set"))
                {
                    mods.Add("set");
                }
            }
            else
            {
                var startWithModifiers = new string[] { "public", "protected", "private" };
                mods.AddRange(startWithModifiers.Where(m => sig.StartsWith(m)));
                var containsModifiers = new string[] { "abstract", "static", "const", "readonly", "sealed", "get;", "set;" };
                mods.AddRange(containsModifiers.Where(m => sig.Contains(" " + m + " ")).Select(m => m.Trim(';')));
            }

            if (mods.Any())
            {
                modifiers.Add(csharp, mods);
            }
            return modifiers;
        }
    }
}
