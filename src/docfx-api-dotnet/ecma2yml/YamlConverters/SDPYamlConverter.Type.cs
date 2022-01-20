using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Collections.Generic;
using System.Linq;
using Type = ECMA2Yaml.Models.Type;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        public TypeSDPModel FormatType(Type t)
        {
            var sdpType = InitWithBasicProperties<TypeSDPModel>(t);

            sdpType.Type = t.ItemType.ToString().ToLower();
            sdpType.TypeParameters = ConvertTypeParameters(t);
            sdpType.ThreadSafety = ConvertThreadSafety(t);

            Type child = t;
            sdpType.InheritancesWithMoniker = ConverterHelper.TrimMonikers(
                t.InheritanceChains?.Select(
                chain => GetInheritChainMDStringList(chain, t)).ToList(),
            t.Monikers);
            sdpType.DerivedClassesWithMoniker = MonikerizeDerivedClasses(t);
            sdpType.ImplementsWithMoniker = t.Interfaces?.Where(i => i != null && i.Value != null)
                 .Select(i => new VersionedString()
                  {
                     Monikers = i.Monikers,
                     Value = TypeStringToTypeMDString(i.Value, _store),
                     valuePerLanguage = TypeStringToMDWithTypeMapping(i.Value, t.Signatures?.DevLangs,nullIfTheSame: true)
                  })
                .ToList()
                .NullIfEmpty();
            sdpType.ImplementsMonikers = ConverterHelper.ConsolidateVersionedValues(sdpType.ImplementsWithMoniker, t.Monikers);

            sdpType.Permissions = t.Docs.Permissions?.Select(
                p => new TypeReference()
                {
                    Description = p.Description,
                    Type = DocIdToTypeMDString(p.CommentId, _store)
                })
                .ToList()
                .NullIfEmpty();

            if (t.Attributes != null
                && t.Attributes.Any(attr => attr.Declaration == "System.CLSCompliant(false)"))
            {
                sdpType.IsNotClsCompliant = true;
            }
            sdpType.AltCompliant = t.Docs.AltCompliant.ResolveCommentId(_store)?.Uid;

            PopulateTypeChildren(t, sdpType);

            return sdpType;
        }

        private VersionedCollection<string> GetInheritChainMDStringList(VersionedCollection<string> chain, Type current)
        {
            VersionedCollection<string> rval = new VersionedCollection<string>
            {
                Values = new List<string>(),
                ValuesPerLanguage = new List<PerLanguageString>(),
                Monikers = chain.Monikers
            };
            Type child = null;
            string parentUid = string.Empty;
            string childrenUid = string.Empty;
            string typeStr = string.Empty;
            string typeMDStr = string.Empty;
            List<PerLanguageString> typeMDStrPerLang;
            int i = 0;
            for (; i < chain.Values.Count - 1; i++)
            {
                parentUid = chain.Values[i];
                childrenUid = chain.Values[i + 1];
                child = _store.TypesByUid[childrenUid];

                typeMDStr = GetParentTypeStringFromChild(child, parentUid);
                rval.Values.Add(typeMDStr);
                typeMDStrPerLang = TypeStringToMDWithTypeMapping(GetParentTypeNameFromChild(child, parentUid), current.Signatures?.DevLangs);
                if (typeMDStrPerLang != null)
                {
                    rval.ValuesPerLanguage.AddRange(typeMDStrPerLang);
                }
            }

            parentUid = chain.Values[i];
            child = current;
            typeMDStr = GetParentTypeStringFromChild(child, parentUid);
            rval.Values.Add(typeMDStr);
            typeMDStrPerLang = TypeStringToMDWithTypeMapping(GetParentTypeNameFromChild(child, parentUid), current.Signatures?.DevLangs);
            if (typeMDStrPerLang != null)
            {
                rval.ValuesPerLanguage.AddRange(typeMDStrPerLang);
            }
            if (!rval.ValuesPerLanguage.Any() || rval.ValuesPerLanguage.Select(v => v.Value).SequenceEqual(rval.Values))
            {
                rval.ValuesPerLanguage = null;
            }
            return rval;
        }

        private string GetTypNameByUid(string uid)
        {
            if (_store.TypesByUid.TryGetValue(uid, out var t))
            {
                return t.Name;
            }
            if (_store.MembersByUid.TryGetValue(uid, out var m))
            {
                return m.Name;
            }
            return uid;
        }

        private string GetParentTypeNameFromChild(Type children, string parentUid)
        {
            var find = children.BaseTypes.Where(p => p.Uid == parentUid).FirstOrDefault();
            return find?.Name ?? GetTypNameByUid(parentUid);
        }

        private string GetParentTypeStringFromChild(Type children, string parentUid)
        {
            var find = children.BaseTypes.Where(p => p.Uid == parentUid).FirstOrDefault();
            if (find != null)
            {
                return TypeStringToTypeMDString(find.Name, _store);
            }
            else
            {
                return UidToTypeMDString(parentUid, _store);
            }
        }

        private void PopulateTypeChildren(Type t, TypeSDPModel sdpType)
        {
            var members = new List<Member>();
            if (t.Members != null)
            {
                members.AddRange(t.Members);
            }
            if (t.InheritedMembers != null)
            {
                members.AddRange(t.InheritedMembers.Keys.Select(im => _store.MembersByUid[im]));
            }
            members = members.OrderBy(m => m.DisplayName).ToList();
            if (members.Count > 0)
            {
                var eiis = members.Where(m => m.IsEII).ToList();
                if (eiis.Count > 0)
                {
                    sdpType.EIIs = eiis.Select(m => ConvertTypeMemberLink(t, m))
                        .Where(m => m != null).ToList().NullIfEmpty();
                }
                foreach (var mGroup in members
                    .Where(m => !m.IsEII)
                    .GroupBy(m => m.ItemType))
                {
                    var list = mGroup.Select(m => ConvertTypeMemberLink(t, m))
                        .Where(m => m != null).ToList().NullIfEmpty();
                    switch (mGroup.Key)
                    {
                        case ItemType.Property:
                            sdpType.Properties = list;
                            break;
                        case ItemType.Method:
                            sdpType.Methods = list;
                            break;
                        case ItemType.Event:
                            sdpType.Events = list;
                            break;
                        case ItemType.Field:
                            sdpType.Fields = list;
                            break;
                        case ItemType.AttachedEvent:
                            sdpType.AttachedEvents = list;
                            break;
                        case ItemType.AttachedProperty:
                            sdpType.AttachedProperties = list;
                            break;
                        case ItemType.Constructor:
                            sdpType.Constructors = list;
                            break;
                        case ItemType.Operator:
                            sdpType.Operators = list;
                            break;
                    }
                }
            }
            if (t.ExtensionMethods?.Count > 0)
            {
                sdpType.ExtensionMethods = t.ExtensionMethods.Select(im => ExtensionMethodToTypeMemberLink(t, im))
                    .Where(ext => ext != null).ToList().NullIfEmpty();
            }
        }
    }
}
