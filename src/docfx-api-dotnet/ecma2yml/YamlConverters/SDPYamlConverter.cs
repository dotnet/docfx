using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        private readonly ECMAStore _store;

        private static HashSet<string> defaultLangList = new HashSet<string> { "csharp" };

        public Dictionary<string, ItemSDPModelBase> NamespacePages { get; } = new Dictionary<string, ItemSDPModelBase>();
        public Dictionary<string, ItemSDPModelBase> TypePages { get; } = new Dictionary<string, ItemSDPModelBase>();
        public Dictionary<string, ItemSDPModelBase> MemberPages { get; } = new Dictionary<string, ItemSDPModelBase>();
        public Dictionary<string, ItemSDPModelBase> OverloadPages { get; } = new Dictionary<string, ItemSDPModelBase>();
        const string _guidXrefString = "<xref href=\"System.Guid?alt=System.Guid&text=Guid\" data-throw-if-not-resolved=\"True\"/>";
        const string _guidCppWinrt = "winrt::guid";
        const string _guidCppCX = "Platform::Guid";
        const string _cppCXLang = "cppcx";
        const string _cppWinrtLang = "cppwinrt";

        public SDPYamlConverter(ECMAStore store)
        {
            _store = store;
        }

        public void Convert()
        {
            HashSet<string> memberTouchCache = new HashSet<string>();

            foreach (var ns in _store.Namespaces)
            {
                NamespacePages.Add(ns.Key, FormatNamespace(ns.Value));
            }

            foreach (var type in _store.TypesByUid.Values)
            {
                switch (type.ItemType)
                {
                    case ItemType.Enum:
                        var enumPage = FormatEnum(type, memberTouchCache);
                        TypePages.Add(enumPage.Uid, enumPage);
                        break;
                    case ItemType.Class:
                    case ItemType.Interface:
                    case ItemType.Struct:
                        var tPage = FormatType(type);
                        TypePages.Add(tPage.Uid, tPage);
                        break;
                    case ItemType.Delegate:
                        var dPage = FormatDelegate(type);
                        TypePages.Add(dPage.Uid, dPage);
                        break;
                }

                var mGroups = type.Members
                    ?.Where(m => !memberTouchCache.Contains(m.Uid))
                    .GroupBy(m => m.Overload);
                if (mGroups != null)
                {
                    foreach (var mGroup in mGroups)
                    {
                        var parentType = (Models.Type)mGroup.FirstOrDefault()?.Parent;
                        var ol = parentType?.Overloads.FirstOrDefault(o => o.Uid == mGroup.Key);
                        if (mGroup.Key == null)
                        {
                            foreach (var m in mGroup)
                            {
                                OverloadPages.Add(m.Uid, FormatOverload(null, new List<Member> { m }));
                            }
                        }
                        else
                        {
                            OverloadPages.Add(mGroup.Key, FormatOverload(ol, mGroup.ToList()));
                        }
                    }
                }
            }
        }
        private T InitWithBasicProperties<T>(ReflectionItem item) where T : ItemSDPModelBase, new()
        {
            T rval = new T
            {
                Uid = item.Uid,
                CommentId = item.CommentId,
                Name = item.Name,
                DevLangs = item.Signatures?.DevLangs ?? defaultLangList,

                SeeAlso = BuildSeeAlsoList(item.Docs, _store),
                Summary = item.Docs.Summary,
                Remarks = item.Docs.Remarks,
                Examples = item.Docs.Examples,
                Monikers = item.Monikers,
                Source = (_store.UWPMode || _store.DemoMode) ? item.SourceDetail.ToSDPSourceDetail() : null
            };

            if (!string.IsNullOrEmpty(item.CrossInheritdocUid))
            {
                rval.CrossInheritdocUid = item.CrossInheritdocUid;
            }

            rval.AssembliesWithMoniker = _store.UWPMode ? null : MonikerizeAssemblyStrings(item);
            rval.PackagesWithMoniker = _store.UWPMode ? null : MonikerizePackageStrings(item, _store.PkgInfoMapping);
            rval.AttributesWithMoniker = item.Attributes?.Where(att => att.Visible)
                .Select(att => new VersionedString() 
                { 
                    Value = att.TypeFullName,
                    Monikers = att.Monikers?.ToHashSet(),
                    valuePerLanguage = TypeStringToMDWithTypeMapping(att.TypeFullName, item.Signatures?.DevLangs, nullIfTheSame: true, isToMDString: false) 
                })
                .DistinctVersionedString()
                .ToList().NullIfEmpty();
            rval.AttributeMonikers = ConverterHelper.ConsolidateVersionedValues(rval.AttributesWithMoniker, item.Monikers);
            rval.SyntaxWithMoniker = ConverterHelper.BuildVersionedSignatures(item, uwpMode: _store.UWPMode)?.NullIfEmpty();

            switch (item)
            {
                case Member m:
                    rval.Namespace = string.IsNullOrEmpty(m.Parent.Parent.Name) ? null : m.Parent.Parent.Name;
                    rval.FullName = m.FullDisplayName;
                    rval.Name = m.DisplayName;
                    rval.NameWithType = m.Parent.Name + '.' + m.DisplayName;
                    break;
                case ECMA2Yaml.Models.Type t:
                    rval.Namespace = string.IsNullOrEmpty(t.Parent.Name) ? null : t.Parent.Name;
                    rval.FullName = t.FullName;
                    rval.NameWithType = t.FullName;
                    var children = t.ItemType == ItemType.Enum
                        ? t.Members.Cast<ReflectionItem>().ToList()
                        : null;
                    GenerateRequiredMetadata(rval, item, children);
                    break;
                case Namespace n:
                    rval.Namespace = n.Name;
                    rval.FullName = n.Name;
                    GenerateRequiredMetadata(rval, item);
                    break;
            }

            if (item.Metadata.TryGetValue(OPSMetadata.InternalOnly, out object val))
            {
                rval.IsInternalOnly = (bool)val;
            }

            if (item.Metadata.TryGetValue(OPSMetadata.AdditionalNotes, out object notes))
            {
                rval.AdditionalNotes = (AdditionalNotes)notes;
            }

            if (item.Attributes != null)
            {
                rval.ObsoleteMessagesWithMoniker = item.Attributes
                    .Where(attr => attr.TypeFullName == "System.ObsoleteAttribute")
                    .Select(attr => new VersionedString()
                    {
                        Value = GenerateObsoleteNotification(attr.Declaration),
                        Monikers = attr.Monikers
                    })
                    .ToList().NullIfEmpty();
            }

            if (_store.UWPMode || _store.DemoMode)
            {
                GenerateUWPRequirements(rval, item);
            }

            return rval;
        }

        private string GenerateObsoleteNotification(string declaration)
        {
            var value = "";
            if (string.IsNullOrEmpty(declaration))
            {
                return value;
            }

            var startIndex = declaration.IndexOf('"');
            var endIndex = declaration.IndexOf('"', startIndex + 1);
            if (startIndex == -1 || endIndex == -1)
            {
                return value;
            }

            startIndex = startIndex + 1;
            value = declaration.Substring(startIndex, endIndex - startIndex);
            if (!string.IsNullOrEmpty(value))
            {
                value=value.Replace("<", "&lt;").Replace(">", "&gt;");
            }
            return value;
        }

        private void GenerateUWPRequirements(ItemSDPModelBase model, ReflectionItem item)
        {
            UWPRequirements uwpRequirements = new UWPRequirements();

            if (item.Metadata.TryGetValue(UWPMetadata.DeviceFamilyNames, out object deviceFamilies))
            {
                String[] familyNames = (String[])deviceFamilies;
                List<DeviceFamily> families = new List<DeviceFamily>();
                if (familyNames.Length > 0 && item.Metadata.TryGetValue(UWPMetadata.DeviceFamilyVersions, out object deviceFamilyVersions))
                {
                    String[] familyVersions = (String[])deviceFamilyVersions;

                    if (familyVersions.Length > 0)
                    {
                        int minNameVersionPairs = Math.Min(familyNames.Length, familyVersions.Length);

                        for (int i = 0; i < minNameVersionPairs; i++)
                        {
                            DeviceFamily df = new DeviceFamily { Name = familyNames[i], Version = familyVersions[i] };
                            families.Add(df);
                        }
                    }
                }

                if (families.Count > 0)
                    uwpRequirements.DeviceFamilies = families;
            }
            if (item.Metadata.TryGetValue(UWPMetadata.ApiContractNames, out object apiContracts))
            {
                String[] apicNames = (String[])apiContracts;
                List<APIContract> contracts = new List<APIContract>();
                if (apicNames.Length > 0 && item.Metadata.TryGetValue(UWPMetadata.ApiContractVersions, out object apicVersions))
                {
                    String[] contractVersions = (String[])apicVersions;

                    if (contractVersions.Length > 0)
                    {
                        int minNameVersionPairs = Math.Min(apicNames.Length, contractVersions.Length);

                        for (int i = 0; i < minNameVersionPairs; i++)
                        {
                            APIContract apic = new APIContract { Name = apicNames[i], Version = contractVersions[i] };
                            contracts.Add(apic);
                        }
                    }
                }

                if (contracts.Count > 0)
                    uwpRequirements.APIContracts = contracts;
            }
            if (item.Metadata.TryGetValue(UWPMetadata.SDKRequirementsName, out object sdkReqName))
            {
                SDKRequirements sdkRequirements = new SDKRequirements { Name = (string)sdkReqName };
                if (item.Metadata.TryGetValue(UWPMetadata.SDKRequirementsUrl, out object sdkReqUrl))
                {
                    sdkRequirements.Url = (string)sdkReqUrl;
                }
                model.SDKRequirements = sdkRequirements;
            }
            if (item.Metadata.TryGetValue(UWPMetadata.OSRequirementsName, out object osReqName))
            {
                OSRequirements osRequirements = new OSRequirements { Name = (string)osReqName };
                if (item.Metadata.TryGetValue(UWPMetadata.OSRequirementsMinVersion, out object osReqMinVer))
                {
                    osRequirements.MinVer = (string)osReqMinVer;
                }
                model.OSRequirements = osRequirements;
            }
            if (item.Metadata.TryGetValue(UWPMetadata.Capabilities, out object capabilities))
            {
                model.Capabilities = (IEnumerable<string>)capabilities;
            }
            if (item.Metadata.TryGetValue(UWPMetadata.XamlMemberSyntax, out object xamlMemberSyntax))
            {
                model.XamlMemberSyntax = (string)xamlMemberSyntax;
            }
            if (item.Metadata.TryGetValue(UWPMetadata.XamlSyntax, out object xamlSyntax))
            {
                model.XamlSyntax = (string)xamlSyntax;
            }

            if (uwpRequirements.DeviceFamilies != null
                || uwpRequirements.APIContracts != null)
                model.UWPRequirements = uwpRequirements;
        }

        private void GenerateRequiredMetadata(ItemSDPModelBase model, ReflectionItem item, List<ReflectionItem> childrenItems = null)
        {
            MergeAllowListedMetadata(model, item);
            if (item.ItemType != ItemType.Namespace)
            {
                ApiScanGenerator.Generate(model, item);
                if (_store.UWPMode)
                {
                    model.Metadata?.Remove(ApiScanGenerator.APISCAN_APILOCATION);
                }
            }
            F1KeywordsGenerator.Generate(model, item, childrenItems);
            HelpViewerKeywordsGenerator.Generate(model, item, childrenItems);

            // Per V3 requirement, we need to put page level monikers in metadata node.
            // To make it compatible with V2 and existing template code, we choose to duplicate this meta in both root level and metadata node
            if (model is OverloadSDPModel
                || model is TypeSDPModel
                || model is NamespaceSDPModel
                || model is EnumSDPModel
                || model is DelegateSDPModel)
            {
                if (!_store.NoMonikers)
                {
                    model.Metadata[OPSMetadata.Monikers] = model.Monikers;
                }
            }
        }

        private IEnumerable<TypeParameterSDPModel> ConvertTypeParameters(ReflectionItem item)
        {
            if (item.TypeParameters?.Count > 0)
            {
                return item.TypeParameters.Select(tp =>
                    new TypeParameterSDPModel()
                    {
                        Description = tp.Description,
                        Name = tp.Name,
                        IsContravariant = tp.IsContravariant,
                        IsCovariant = tp.IsCovariant
                    }).ToList();
            }
            return null;
        }

        private List<PerLanguageString> TypeStringToMDWithTypeMapping(
            string typeString,
            HashSet<string> totalLangs = null,
            bool nullIfTheSame = false, bool isToMDString = true)
        { 
            if (_store.TypeMappingStore != null)
            {
                var typePerLanguage = _store.TypeMappingStore.TranslateTypeString(typeString, totalLangs ?? _store.TotalDevLangs);
                if (typePerLanguage != null)
                {
                    if (typePerLanguage?.Count == 1 && typePerLanguage.First().Value == typeString && nullIfTheSame)
                    {
                        return null;
                    }

                    if (isToMDString)
                    {
                        typePerLanguage.ForEach(typePerLang => typePerLang.Value = TypeStringToTypeMDString(typePerLang.Value, _store));
                    }

                    if (typeString.Contains("System.Guid"))
                    {
                        if (totalLangs.Contains(_cppWinrtLang))
                        {
                            // Remove cppwinrt from the group, re-add it into new group.
                            for (int i = 0; i < typePerLanguage.Count(); i++)
                            {
                                if (typePerLanguage[i].Langs.Contains(_cppWinrtLang))
                                {
                                    if (typePerLanguage[i].Langs.Count() == 1)
                                    {
                                        typePerLanguage[i].Value = typePerLanguage[i].Value.Replace(_guidXrefString, _guidCppWinrt);
                                    }
                                    else
                                    {
                                        typePerLanguage[i].Langs.Remove(_cppWinrtLang);
                                        typePerLanguage.Add(new PerLanguageString() { Value = typePerLanguage[i].Value.Replace(_guidXrefString, _guidCppWinrt), Langs = new HashSet<string>() { _cppWinrtLang } });

                                        break;
                                    }
                                }
                            }
                        }

                        if (totalLangs.Contains(_cppCXLang))
                        {
                            // Remove cppcx from the group, re-add it into new group.
                            for (int i = 0; i < typePerLanguage.Count(); i++)
                            {
                                if (typePerLanguage[i].Langs.Contains(_cppCXLang))
                                {
                                    if (typePerLanguage[i].Langs.Count() == 1)
                                    {
                                        typePerLanguage[i].Value = typePerLanguage[i].Value.Replace(_guidXrefString, _guidCppCX);
                                    }
                                    else
                                    {
                                        typePerLanguage[i].Langs.Remove(_cppCXLang);
                                        typePerLanguage.Add(new PerLanguageString() { Value = typePerLanguage[i].Value.Replace(_guidXrefString, _guidCppCX), Langs = new HashSet<string>() { _cppCXLang } });

                                        break;
                                    }
                                }
                            }
                        }
                    }
                    return typePerLanguage;
                }
            }

            return null;
        }

        private ParameterReference ConvertNamedParameter(
            Parameter p,
            List<TypeParameter> knownTypeParams = null,
            HashSet<string> totalLangs = null)
        {
            var isGeneric = knownTypeParams?.Any(tp => tp.Name == p.Type) ?? false;
            var rval = new ParameterReference()
            {
                Description = p.Description,
                Type = isGeneric ? p.Type : TypeStringToTypeMDString(p.OriginalTypeString ?? p.Type, _store)
            };
            if (!isGeneric && _store.TypeMappingStore?.TypeMappingPerLanguage != null)
            {
                rval.TypePerLanguage = TypeStringToMDWithTypeMapping(
                    p.OriginalTypeString ?? p.Type,
                    totalLangs ?? _store.TotalDevLangs,
                    nullIfTheSame: true);
            }
            rval.NamesWithMoniker = p.VersionedNames;
            return rval;
        }

        private ReturnValue ConvertReturnValue(
            ReturnValue returns,
            List<TypeParameter> knownTypeParams = null,
            HashSet<string> totalLangs = null)
        {
            ReturnValue rval = null;

            var r = returns.VersionedTypes
                   .Where(v => !string.IsNullOrWhiteSpace(v.Value) && v.Value != "System.Void").ToArray();
            if (r.Any())
            {
                foreach (var t in returns.VersionedTypes)
                {
                    var isGeneric = knownTypeParams?.Any(tp => tp.Name == t.Value) ?? false;

                    if (!isGeneric && _store.TypeMappingStore?.TypeMappingPerLanguage != null)
                    {
                        t.ValuePerLanguage = TypeStringToMDWithTypeMapping(
                            t.Value,
                            totalLangs ?? _store.TotalDevLangs,
                            nullIfTheSame: true);
                    }
                    
                    t.Value = isGeneric ? t.Value : TypeStringToTypeMDString(t.Value, _store);
                }
                var returnType = new ReturnValue
                {
                    VersionedTypes = r,
                    Description = returns.Description
                };

                rval = returnType;
            }

            return rval;
        }

        private Models.SDP.ThreadSafety ConvertThreadSafety(ReflectionItem item)
        {
            if (item.Docs.ThreadSafetyInfo != null)
            {
                return new Models.SDP.ThreadSafety()
                {
                    CustomizedContent = item.Docs.ThreadSafetyInfo.CustomContent,
                    IsSupported = item.Docs.ThreadSafetyInfo.Supported,
                    MemberScope = item.Docs.ThreadSafetyInfo.MemberScope
                };
            }
            return null;
        }

        public static string BuildSeeAlsoList(Docs docs, ECMAStore store)
        {
            if ((docs.AltMemberCommentIds == null || docs.AltMemberCommentIds?.Count == 0) 
                && (docs.Related == null || docs.Related?.Count == 0))
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            if (docs.AltMemberCommentIds != null)
            {
                foreach (var altMemberId in docs.AltMemberCommentIds)
                {
                    var uid = altMemberId.ResolveCommentId(store)?.Uid ?? altMemberId.Substring(altMemberId.IndexOf(':') + 1);
                    uid = System.Web.HttpUtility.UrlEncode(uid);
                    sb.AppendLine($"- <xref:{uid}>");
                }
            }
            if (docs.Related != null)
            {
                foreach (var rTag in docs.Related)
                {
                    var uri = rTag.Uri.Contains(' ') ? rTag.Uri.Replace(" ", "%20") : rTag.Uri;
                    sb.AppendLine($"- [{rTag.OriginalText}]({uri})");
                }
            }

            return sb.ToString();
        }
    }
}
