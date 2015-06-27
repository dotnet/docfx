namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Utility;

    public class ItemViewModel : Dictionary<string, object>
    {
        public ItemViewModel() : base()
        {
            
        }

        public ItemViewModel(Dictionary<string, object> dict)
            : base(dict)
        {
        }

        public void AddMetadataPair(string key, MetadataItem item)
        {
            var pair = ExtractPair(key, item);
            if (pair != null )
            {
                foreach (var tuple in pair.Where(tuple => tuple?.Item2 != null))
                {
                    this.Add(tuple.Item1, tuple.Item2);
                }
            }
        }

        public static ItemViewModel Convert(MetadataItem item, List<string> keys)
        {
            ItemViewModel viewModel = new ItemViewModel();
            keys.ForEach(s => viewModel.AddMetadataPair(s, item));
            return viewModel;
        }

        private static IEnumerable<Tuple<string, object>> ExtractPair(string key, MetadataItem item)
        {
            var uid = item.Name;
            var parentUid = item.Parent?.Name;
            var id = string.IsNullOrEmpty(parentUid) ? uid : uid.Replace(parentUid, string.Empty);
            id = id?.TrimStart('.');
            switch (key)
            {
                case "uid":
                    return GetPairList(key, uid);
                case "parent":
                    return GetPairList(key, parentUid);
                case "id":
                    return GetPairList(key, id);
                case "children":
                    if (item?.Items == null) return null;
                    var children = from i in item.Items select i.Name;
                    return GetPairList(key, children);
                case "summary":
                    return GetPairList(key, item.Summary);
                case "remarks":
                    return GetPairList(key, item.Remarks);
                case "exceptions":
                    return GetPairList(key, item.Exceptions);
                case "assemblies":
                    return GetPairList(key, item.AssemblyNameList);
                case "namespace":
                    return GetPairList(key, item.NamespaceName);
                case "href":
                    return GetPairList(key, item.Href);
                case "name":
                    return GetLanguageSpecificPairList(key, item.DisplayNames, id);
                case "fullName":
                    return GetLanguageSpecificPairList(key, item.DisplayQualifiedNames, uid);
                case "type":
                    return GetPairList(key, item.Type);
                case "source":
                    return GetPairList(key, item.Source);
                case "syntax":
                    return GetPairList(key, item.Syntax);
                case "inheritance":
                    return GetPairList(key, item.Inheritance);
                case "implements":
                    return GetPairList(key, item.Implements);
                case "inheritedMembers":
                    return GetPairList(key, item.InheritedMembers);
                default:
                    return null;
            }
        }

        private static IEnumerable<Tuple<string, object>> GetLanguageSpecificPairList(string baseKey, IDictionary<SyntaxLanguage, string> value, string fallbackValue)
        {
            if (value == null || value.Count == 0) return GetPairList(baseKey, fallbackValue);
            var pairList = new List<Tuple<string, object>>();
            string defaultValue;
            if (value.ContainsKey(SyntaxLanguage.Default))
            {
                defaultValue = value[SyntaxLanguage.Default];
            }
            else
            {
                defaultValue = value.FirstOrDefault().Value;
            }
            pairList.Add(GetPair(baseKey, defaultValue.CoalesceNullOrEmpty(fallbackValue)));
            pairList.AddRange(from pair in value
                              where pair.Value != defaultValue && !string.IsNullOrEmpty(pair.Value)
                              select GetPair(baseKey + "." + pair.Key.ToString().ToLower(), pair.Value));
            return pairList;
        }

        private static IEnumerable<Tuple<string, object>> GetPairList(string key, object value)
        {
            return new List<Tuple<string, object>> { Tuple.Create(key, value) };
        }

        private static Tuple<string, object> GetPair(string key, object value)
        {
            return Tuple.Create(key, value);
        }
    }
}
