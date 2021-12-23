using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Collections.Generic;
using System.Linq;

namespace ECMA2Yaml
{
    public static class F1KeywordsGenerator
    {
        public static void Generate(ItemSDPModelBase model, ReflectionItem item, List<ReflectionItem> childrenItems)
        {
            if (!model.Metadata.ContainsKey(OPSMetadata.F1Keywords))
            {
                var keywords = GetF1Keywords(item).ToList();
                if (childrenItems != null)
                {
                    foreach (var child in childrenItems)
                    {
                        keywords.AddRange(GetF1Keywords(child));
                    }
                }
                model.Metadata[OPSMetadata.F1Keywords] = keywords.Distinct().ToList();
            }
        }

        private static IEnumerable<string> GetF1Keywords(ReflectionItem item)
        {
            var uid = item.Uid;
            if (uid == null)
            {
                yield break;
            }
            uid = uid.TrimEnd('*');
            var index = uid.IndexOf('(');
            if (index != -1)
            {
                uid = uid.Remove(index);
            }
            yield return uid;
            if (uid.Contains("."))
            {
                yield return uid.Replace(".", "::");
            }

            /*for example
             * Current keywords: System.Collections.Generic.Dictionary`2.KeyCollection.Enumerator       
             *                   System::Collections::Generic::Dictionary`2::KeyCollection::Enumerator
             * Keywords to add: System.Collections.Generic.Dictionary.Keycollection.Enumerator
             */
            var specificIndex = uid.IndexOf('`');
            if (specificIndex != -1)
            {
                var nextIndex = uid.IndexOf(".", specificIndex);
                if (nextIndex != -1)
                {
                    yield return uid.Remove(specificIndex, nextIndex - specificIndex);
                }
                else
                {
                    yield return uid.Remove(specificIndex);
                }
            }

            /*for type page and member page
             * for example
             * Current keywords(type page and member page):  System.String
             * Keywords to add: String
             */
            var lastindex = uid.LastIndexOf(".");
            if (lastindex != -1)
                yield return uid.Substring(lastindex + 1);

            /*for member page
             *Current keywords(member page): System.Collections.Generic.Dictionary.Keycollection.Enumerator
             *Keywords to add: Keycollection.Enumerator
             *                 Keycollection::Enumerator
             */

            if (uid.Count(c => c == '.') < 2)
            {
                yield break;
            }

            switch (item.ItemType)
            {
                case ItemType.Enum:
                case ItemType.Class:
                case ItemType.Interface:
                case ItemType.Struct:
                case ItemType.Delegate:
                    break;
                default:
                    if (lastindex > 0)
                    {
                        var penultimateindex = uid.LastIndexOf(".", lastindex - 1);
                        var tempuid = uid;
                        if (penultimateindex != -1)
                            tempuid = tempuid.Substring(penultimateindex + 1);
                        yield return tempuid;
                        yield return tempuid.Replace(".", "::");
                    }

                    break;
            }
        }
    }
}
