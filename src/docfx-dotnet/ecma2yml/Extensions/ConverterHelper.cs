using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ECMA2Yaml
{
    public static class ConverterHelper
    {
        public static readonly IReadOnlyDictionary<ItemType, string> ItemTypeNameMapping = new Dictionary<ItemType, string>()
        {
            {ItemType.Default, "default"},
            {ItemType.Toc, "toc"},
            {ItemType.Assembly, "assembly"},
            {ItemType.Namespace, "namespace"},
            {ItemType.Class, "class"},
            {ItemType.Interface, "interface"},
            {ItemType.Struct, "struct"},
            {ItemType.Delegate, "delegate"},
            {ItemType.Enum, "enum"},
            {ItemType.Field, "field"},
            {ItemType.Property, "property"},
            {ItemType.Event, "event"},
            {ItemType.Constructor, "constructor"},
            {ItemType.Method, "method"},
            {ItemType.Operator, "operator"},
            {ItemType.Container, "container"},
            {ItemType.AttachedEvent, "attachedevent"},
            {ItemType.AttachedProperty, "attachedproperty"}
        };

        public static SortedList<string, string> BuildSignatures(ReflectionItem item, bool uwpMode = false)
        {
            var contents = new SortedList<string, string>();
            if (item.Signatures?.Dict != null)
            {
                foreach (var sigPair in item.Signatures.Dict)
                {
                    if (Models.ECMADevLangs.OPSMapping.ContainsKey(sigPair.Key))
                    {
                        var lang = Models.ECMADevLangs.OPSMapping[sigPair.Key];
                        var val = sigPair.Value.LastOrDefault()?.Value;
                        switch (sigPair.Key)
                        {
                            case ECMADevLangs.CSharp:
                                var sig = uwpMode ? UWPCSharpSignatureTransform(val) : val;
                                var contentBuilder = new StringBuilder();
                                if (item.Attributes?.Count > 0)
                                {
                                    foreach (var att in item.Attributes.Where(attr => attr.Visible))
                                    {
                                        contentBuilder.AppendFormat("[{0}]\n", att.Declaration);
                                    }
                                }
                                contentBuilder.Append(sig);
                                contents[lang] = contentBuilder.ToString();
                                break;
                            case ECMADevLangs.CPP_CLI:
                            case ECMADevLangs.CPP_CX:
                            case ECMADevLangs.CPP_WINRT:
                                contents[lang] = uwpMode ? UWPCPPSignatureTransform(val) : val;
                                break;
                            default:
                                contents[lang] = val;
                                break;
                        }
                    }
                }
            }

            return contents;
        }

        public static List<VersionedSignatureModel> BuildVersionedSignatures(ReflectionItem item, bool uwpMode = false)
        {
            var contents = new SortedList<string, List<VersionedString>>();
            if (item.Signatures?.Dict != null)
            {
                var visibleAttrs = item.Attributes?.Where(attr => attr.Visible).ToList();
                foreach (var sigPair in item.Signatures.Dict)
                {
                    var attrsForThisLang = visibleAttrs?.Where(attr => attr.NamesPerLanguage?.ContainsKey(sigPair.Key) == true).ToList();
                    if (Models.ECMADevLangs.OPSMapping.ContainsKey(sigPair.Key))
                    {
                        var lang = Models.ECMADevLangs.OPSMapping[sigPair.Key];
                        var sigValues = sigPair.Value;

                        switch (sigPair.Key)
                        {
                            case ECMADevLangs.CSharp:
                                if (uwpMode)
                                {
                                    sigValues.ForEach(vs => vs.Value = UWPCSharpSignatureTransform(vs.Value));
                                }
                                break;
                            case ECMADevLangs.CPP_CLI:
                            case ECMADevLangs.CPP_CX:
                            case ECMADevLangs.CPP_WINRT:
                                if (uwpMode)
                                {
                                    sigValues.ForEach(vs => vs.Value = UWPCPPSignatureTransform(vs.Value));
                                }
                                break;
                        }
                        if (attrsForThisLang?.Count > 0)
                        {
                            bool versionedSig = sigValues.Count > 1 && sigValues.Any(v => v.Monikers?.Count > 0);
                            bool versionedAttr = attrsForThisLang.Any(attr => attr.Monikers?.Count > 0);
                            // devide into 2 cases for better perf, most of the time neither signature nor attributes are versioned
                            if (!versionedSig && !versionedAttr)
                            {
                                contents[lang] = new List<VersionedString>()
                                        {
                                            new VersionedString(null, AttachAttributesToSignature(attrsForThisLang, sigValues.First().Value, sigPair.Key))
                                        };
                            }
                            else
                            {
                                contents[lang] = item.Monikers.Select(moniker =>
                                {
                                    var sig = sigValues.FirstOrDefault(s => s.Monikers == null || s.Monikers.Contains(moniker));
                                    var attrs = attrsForThisLang.Where(a => a.Monikers == null || a.Monikers.Contains(moniker)).ToList();
                                    var combinedSig = AttachAttributesToSignature(attrs, sig?.Value, sigPair.Key);
                                    return (moniker, combinedSig);
                                })
                                .GroupBy(t => t.combinedSig)
                                .Where(g => !string.IsNullOrEmpty(g.Key))
                                .Select(g => new VersionedString(g.Select(t => t.moniker).ToHashSet(), g.Key))
                                .ToList();
                            }
                        }
                        else
                        {
                            contents[lang] = sigValues;
                        }
                    }
                }
            }

            return contents.Select(sig => new VersionedSignatureModel() { Lang = sig.Key, Values = sig.Value }).ToList();
        }

        static string[] attributePrefix = { "get: ", "set: " };
        private static string AttachAttributesToSignature(List<ECMAAttribute> attrs, string sig, string lang)
        {
            if (string.IsNullOrEmpty(sig)
                || attrs == null
                || attrs.Count == 0
                || attrs.Count(attr => attr.NamesPerLanguage?.ContainsKey(lang) == true) == 0)
            {
                return sig;
            }
            var contentBuilder = new StringBuilder();
            foreach (var att in attrs)
            {
                if (att.NamesPerLanguage.TryGetValue(lang, out string name))
                {
                    bool isGetterSetterAttribute = false;
                    if (!string.IsNullOrEmpty(name))
                    {
                        foreach (var prefix in attributePrefix)
                        {
                            if (name.Contains(prefix))
                            {
                                string newName = name.Replace(prefix, "");
                                string newSig = sig.Replace(prefix.Replace(": ", ";"), $"{newName} {prefix.Replace(": ", ";")}");
                                if (newSig != sig)
                                {
                                    isGetterSetterAttribute = true;
                                    sig = newSig;
                                    break;
                                }
                            }
                        }
                    }

                    if (!isGetterSetterAttribute)
                    {
                        contentBuilder.AppendFormat("{0}\n", name);
                    }
                }
            }

            contentBuilder.Append(sig);

            return contentBuilder.ToString();
        }

        public static HashSet<string> TrimMonikers(HashSet<string> propertyMonikers, HashSet<string> itemMonikers)
        {
            if (itemMonikers != null && propertyMonikers != null)
            {
                if (itemMonikers.IsSubsetOf(propertyMonikers))
                {
                    return null;
                }
                else if (propertyMonikers.Overlaps(itemMonikers))
                {
                    return new HashSet<string>(propertyMonikers.Intersect(itemMonikers));
                }
            }
            return propertyMonikers.NullIfEmpty();
        }

        public static List<VersionedValue<T>> TrimMonikers<T>(List<VersionedValue<T>> versionedValues, HashSet<string> itemMonikers)
        {
            if (versionedValues?.Count == 1) //for perf, 95% cases there's no versioning
            {
                versionedValues[0].Monikers = null;
            }
            else if (versionedValues?.Count > 1)
            {
                foreach (var v in versionedValues)
                {
                    v.Monikers = TrimMonikers(v.Monikers, itemMonikers);
                }
            }
            return versionedValues;
        }

        public static List<VersionedCollection<T>> TrimMonikers<T>(List<VersionedCollection<T>> versionedValues, HashSet<string> itemMonikers)
        {
            if (versionedValues?.Count == 1) //for perf, 95% cases there's no versioning
            {
                versionedValues[0].Monikers = null;
            }
            else if (versionedValues?.Count > 1)
            {
                foreach (var v in versionedValues)
                {
                    v.Monikers = TrimMonikers(v.Monikers, itemMonikers);
                }
            }
            return versionedValues;
        }

        public static IEnumerable<string> ConsolidateVersionedValues(IEnumerable<VersionedString> vals, HashSet<string> pageMonikers)
        {
            if (vals == null)
            {
                return null;
            }
            HashSet<string> allMonikers = new HashSet<string>();
            foreach (var val in vals.Where(v => v.Monikers != null))
            {
                allMonikers.UnionWith(val.Monikers);
                if (val.Monikers.SetEquals(pageMonikers))
                {
                    val.Monikers = null;
                }
            }
            if (vals.Any(v => v.Monikers == null))
            {
                return null;
            }
            return allMonikers.SetEquals(pageMonikers) ? null : allMonikers;
        }

        /// <summary>
        /// [AttributeA("value1")]
        /// [AttributeA("value2")]
        /// [AttributeA("value3")]
        /// A method can have same attribute assigned to it multiple times, we need to distinct the final attribute type list.
        /// </summary>
        /// <param name="versionedList"></param>
        /// <returns></returns>
        public static IEnumerable<VersionedString> DistinctVersionedString(this IEnumerable<VersionedString> versionedList)
        {
            if (versionedList == null)
            {
                return null;
            }
            return versionedList.GroupBy(vs => vs.Value).Select(g =>
            {
                if (g.Count() == 1)
                {
                    return g.First();
                }
                return new VersionedString()
                {
                    Value = g.Key,
                    Monikers = g.SelectMany(vs => vs.Monikers ?? Enumerable.Empty<string>()).ToHashSet().NullIfEmpty()
                };
            });
        }

        private static readonly Regex CSharpSignatureLongNameRegex = new Regex("\\w+(\\.\\w+){2,}", RegexOptions.Compiled);
        private static string UWPCSharpSignatureTransform(string sig)
        {
            var csharpSyntax = CSharpSignatureLongNameRegex.Replace(sig, match =>
            {
                var val = match.Value;
                return val.Substring(val.LastIndexOf('.') + 1);
            });
            return csharpSyntax.Replace(" (", "(");
        }

        private static readonly Regex CPPSignatureLongNameRegex = new Regex("\\w+(::\\w+){2,}", RegexOptions.Compiled);
        private static string UWPCPPSignatureTransform(string sig)
        {
            return CPPSignatureLongNameRegex.Replace(sig, match =>
            {
                var val = match.Value;
                return val.Substring(val.LastIndexOf(':') + 1);
            });
        }
    }
}
