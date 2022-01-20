using Monodoc.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ECMA2Yaml.Models
{
    public partial class ECMAStore
    {
        public static EcmaUrlParser EcmaParser = new EcmaUrlParser();
        public Dictionary<string, Namespace> Namespaces { get; set; }
        public Dictionary<string, Type> TypesByFullName { get; set; }
        public Dictionary<string, Type> TypesByUid { get; set; }
        public Dictionary<string, Member> MembersByUid { get; set; }
        public Dictionary<string, ReflectionItem> ItemsByDocId { get; set; }
        public Dictionary<string, List<VersionedString>> InheritanceParentsByUid { get; set; }
        public Dictionary<string, List<VersionedString>> InheritanceChildrenByUid { get; set; }
        public Dictionary<string, List<VersionedString>> ImplementationParentsByUid { get; set; }
        public Dictionary<string, List<VersionedString>> ImplementationChildrenByUid { get; set; }
        public Dictionary<string, Member> ExtensionMethodsByMemberDocId { get; set; }
        public ILookup<string, Member> ExtensionMethodUidsByTargetUid { get; set; }
        public Dictionary<string, string> InheritDocItemsByUid { get; set; }    // Inheritdoc uid pair
        public Dictionary<string, List<string>> CrossRepoParentsByUid { get; set; }    // Parent in cross repo uid pair, <child type uid, parent type uids>

        public FilterStore FilterStore { get; set; }
        public TypeMappingStore TypeMappingStore { get; set; }
        public bool StrictMode { get; set; }
        public bool UWPMode { get; set; }
        public bool DemoMode { get; set; }
        public PackageInformationMapping PkgInfoMapping { get; set; }
        public HashSet<string> TotalDevLangs { get; set; }
        private static Dictionary<string, EcmaDesc> typeDescriptorCache;

        private IEnumerable<Namespace> _nsList;
        private IEnumerable<Type> _tList;
        private FrameworkIndex _frameworks;
        private List<Member> _extensionMethods;

        public ECMAStore(IEnumerable<Namespace> nsList, FrameworkIndex frameworks)
        {
            typeDescriptorCache = new Dictionary<string, EcmaDesc>();

            _nsList = nsList;
            _tList = nsList.SelectMany(ns => ns.Types).ToList();
            _frameworks = frameworks;

            InheritanceParentsByUid = new Dictionary<string, List<VersionedString>>();
            InheritanceChildrenByUid = new Dictionary<string, List<VersionedString>>();
            ImplementationParentsByUid = new Dictionary<string, List<VersionedString>>();
            ImplementationChildrenByUid = new Dictionary<string, List<VersionedString>>();
            InheritDocItemsByUid = new Dictionary<string, string>();
            CrossRepoParentsByUid = new Dictionary<string, List<string>>();
        }

        public void Build()
        {
            //The type changed from an enum to a regular object in the latest release of the library, 
            //so this would have to result in two separate yaml documents entirely for the same type.
            EnumConvertToClass();
            _tList = _nsList.SelectMany(ns => ns.Types).ToList();
            TotalDevLangs = _tList.SelectMany(t => t.Signatures.DevLangs).ToHashSet();
            Namespaces = _nsList.ToDictionary(ns => ns.Name);
            TypesByFullName = _tList.ToDictionary(t => t.FullName);

            BuildIds(_nsList, _tList);
            TypesByUid = _tList.ToDictionary(t => t.Uid);
            BuildUniqueMembers();
            BuildDocIdDictionary();

            foreach (var t in _tList)
            {
                BuildOverload(t);
            }

            PopulateMonikers();

            foreach (var t in _tList)
            {
                FillInheritanceImplementationGraph(t);
            }

            foreach (var t in _tList)
            {
                BuildInheritance(t);
                BuildDocs(t);
            }

            FindMissingAssemblyNamesAndVersions();

            BuildAttributes();

            MonikerizeAssembly();

            BuildExtensionMethods();

            BuildOtherMetadata();
        }

        private void EnumConvertToClass()
        {
            foreach (var ns in _nsList)
            {
                if (ns.Types == null || _frameworks?.DocIdToFrameworkDict == null)
                {
                    return;
                }

                var listType = new List<Type>();
                foreach (var type in ns.Types.Where(t => t.BaseTypes != null && t.DocId !=null && _frameworks.DocIdToFrameworkDict.ContainsKey(t.DocId)))
                {
                    if (!(type.BaseTypes.Any(bt => bt.Name == "System.Enum") && type.BaseTypes.Any(bt => bt.Name == "System.Object")))
                    {
                        continue;
                    }

                    //split monikers
                    var classMonikers = type.BaseTypes.Where(bt => bt.Name == "System.Object" && bt.Monikers != null)
                                            .SelectMany(o => o.Monikers).ToList();
                    var enumMonikers = type.BaseTypes.Where(bt => bt.Name == "System.Enum" && bt.Monikers != null)
                                            .SelectMany(o => o.Monikers).ToList();
                    var totalMonikers = _frameworks.DocIdToFrameworkDict[type.DocId].ToList();
                    if (!classMonikers.Any())
                    {
                        classMonikers = totalMonikers.Except(enumMonikers).ToList();
                    }
                    else if (!enumMonikers.Any())
                    {
                        enumMonikers = totalMonikers.Except(classMonikers).ToList();
                    }

                    //Generate Enum Type
                    var enumType = type.ShallowCopy();
                    enumType.Build(this); // build type id
                    enumType.ItemType = ItemType.Enum;
                    enumType.Id += "_e";
                    enumType.FullName += "_e";
                    enumType.DocId += "_e";
                    _frameworks.DocIdToFrameworkDict[enumType.DocId] = enumMonikers;
                    var docIdOldPart = type.DocId.Substring(2);
                    var docIdNewPart = docIdOldPart + "_e";
                    var members = new List<Member>();
                    if (enumType.Members != null)
                    {
                        foreach (var m in enumType.Members)
                        {
                            if (_frameworks.DocIdToFrameworkDict[m.DocId].Any(moniker => enumMonikers.Contains(moniker)))
                            {
                                var m1 = m.ShallowCopy();
                                m1.Parent = enumType;
                                m1.DocId = m1.DocId.Replace(docIdOldPart, docIdNewPart);
                                _frameworks.DocIdToFrameworkDict[m1.DocId] = enumMonikers;
                                members.Add(m1);
                            }
                        }

                        enumType.Members = members;
                    }

                    listType.Add(enumType);

                    //Generate Class Type
                    type.ItemType = ItemType.Class;
                    _frameworks.DocIdToFrameworkDict[type.DocId] = classMonikers;

                    if (type.Members != null)
                    {
                        members = new List<Member>();
                        foreach (var m in type.Members)
                        {
                            if (_frameworks.DocIdToFrameworkDict[m.DocId].Any(moniker => classMonikers.Contains(moniker)))
                            {
                                members.Add(m);
                            }
                        }

                        type.Members = members;
                    }
                }
                ns.Types.AddRange(listType);
            }
        }

        private void BuildDocIdDictionary()
        {
            ItemsByDocId = new Dictionary<string, ReflectionItem>();
            foreach (var item in TypesByUid.Values.Cast<ReflectionItem>()
                .Concat(MembersByUid.Values.Cast<ReflectionItem>()))
            {
                if (string.IsNullOrEmpty(item.DocId))
                {
                    OPSLogger.LogUserError(LogCode.ECMA2Yaml_DocId_IsNull, item.SourceFileLocalPath, item.Name);
                }
                else if (ItemsByDocId.ContainsKey(item.DocId))
                {
                    OPSLogger.LogUserError(LogCode.ECMA2Yaml_DocId_Duplicated, item.SourceFileLocalPath, item.DocId);
                    OPSLogger.LogUserError(LogCode.ECMA2Yaml_DocId_Duplicated, ItemsByDocId[item.DocId].SourceFileLocalPath, item.DocId);
                }
                else
                {
                    ItemsByDocId.Add(item.DocId, item);
                }
            }
        }

        private void BuildUniqueMembers()
        {
            var allMembers = _tList.Where(t => t.Members != null).SelectMany(t => t.Members).ToList();
            var groups = allMembers.GroupBy(m => m.Uid).Where(g => g.Count() > 1).ToList();
            if (groups.Count > 0)
            {
                foreach (var group in groups)
                {
                    var types = group.Select(m => m.ItemType).Distinct().ToList();
                    if (types.Count < group.Count())
                    {
                        foreach (var member in group)
                        {
                            OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_MemberNameAndSignature_NotUnique, member.SourceFileLocalPath, member.Name);
                        }
                    }
                }
            }

            MembersByUid = new Dictionary<string, Member>();
            var typesToLower = TypesByUid.ToDictionary(p => p.Key.ToLower(), p => p.Value);
            foreach (var member in allMembers)
            {
                if (typesToLower.ContainsKey(member.Uid.ToLower()) || MembersByUid.ContainsKey(member.Uid))
                {
                    member.Id = member.Id + "_" + member.ItemType.ToString().Substring(0, 1).ToLower();
                }
                MembersByUid[member.Uid] = member;
            }
        }

        public bool TryGetTypeByFullName(string fullName, out Type type)
        {
            if (fullName.IndexOf('+') > 0)
            {
                return TypesByFullName.TryGetValue(fullName.Replace('+', '.'), out type);
            }
            else
            {
                return TypesByFullName.TryGetValue(fullName, out type);
            }
        }
        public void TranlateContentSourceMeta(string publicGitRepoUrl,
           string publicGitBranch)
        {
            foreach (var ns in _nsList)
            {
                HandleContentSourceMeta(ns, publicGitBranch, publicGitRepoUrl);
                foreach (var t in ns.Types)
                {
                    HandleContentSourceMeta(t, publicGitBranch, publicGitRepoUrl);
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members)
                        {
                            HandleContentSourceMeta(m, publicGitBranch, publicGitRepoUrl);
                        }
                        if (t.Overloads != null)
                        {
                            foreach (var o in t.Overloads)
                            {
                                HandleContentSourceMeta(o, publicGitBranch, publicGitRepoUrl);
                            }
                        }
                    }
                }
            }
        }
        private void HandleContentSourceMeta(ReflectionItem item, string publicGitBranch, string publicGitRepoUrl)
        {
            if (item != null && item.Metadata.TryGetValue("contentSourcePath", out object val) && val != null)
            {
                var mdPath = val.ToString().Replace("\\", "/");
                mdPath = mdPath.StartsWith("/") ? mdPath : ("/" + mdPath);
                item.SourceDetail = new GitSourceDetail()
                {
                    Path = mdPath,
                    RepoBranch = publicGitBranch,
                    RepoUrl = publicGitRepoUrl
                };
                item.Metadata.Remove("contentSourcePath");
            }
        }
        public void TranslateSourceLocation(
            string sourcePathRoot,
            string gitRepoUrl,
            string gitRepoBranch,
            string publicGitRepoUrl,
            string publicGitBranch)
        {
            sourcePathRoot = System.IO.Path.GetFullPath(sourcePathRoot);
            if (!sourcePathRoot.EndsWith("\\"))
            {
                sourcePathRoot += "\\";
            }

            var gitUrlPattern = GetGitUrlGenerator(gitRepoUrl, gitRepoBranch);
            var publicGitUrlPattern = GetGitUrlGenerator(publicGitRepoUrl, publicGitBranch);

            foreach (var ns in _nsList)
            {
                TranslateSourceLocation(ns, sourcePathRoot, gitUrlPattern, publicGitUrlPattern);
                HandleContentSourceMeta(ns, publicGitBranch, publicGitRepoUrl);
                foreach (var t in ns.Types)
                {
                    TranslateSourceLocation(t, sourcePathRoot, gitUrlPattern, publicGitUrlPattern);
                    HandleContentSourceMeta(t, publicGitBranch, publicGitRepoUrl);
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members)
                        {
                            TranslateSourceLocation(m, sourcePathRoot, gitUrlPattern, publicGitUrlPattern);
                            HandleContentSourceMeta(m, publicGitBranch, publicGitRepoUrl);
                        }
                        if (t.Overloads != null)
                        {
                            foreach (var o in t.Overloads)
                            {
                                TranslateSourceLocation(o, sourcePathRoot, gitUrlPattern, publicGitUrlPattern);
                                HandleContentSourceMeta(o, publicGitBranch, publicGitRepoUrl);
                            }
                        }
                    }
                }
            }

            Func<string, string> GetGitUrlGenerator(string gitUrl, string gitBranch)
            {
                bool isVSTS = gitUrl.Contains("visualstudio.com");
                if (isVSTS)
                {
                    string pattern = gitUrl + "?path={0}&version=GB" + gitBranch;
                    return xmlPath => string.Format(pattern, WebUtility.UrlEncode(xmlPath));
                }
                else
                {
                    string pattern = gitUrl + "/blob/" + gitBranch + "{0}";
                    return xmlPath => string.Format(pattern, xmlPath);
                }
            }

        }

        /// <summary>
        /// reference doc: https://review.docs.microsoft.com/en-us/engineering/projects/ops/edit-button?branch=master
        /// content_git_url: the url that is used in live page edit button
        /// original_content_git_url: the url of the file that is used to really publish the page. Also used in review page edit button.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sourcePathRoot"></param>
        /// <param name="gitUrlPattern"></param>
        /// <param name="publicGitUrlPattern"></param>
        /// <param name="vstsRepo"></param>
        private void TranslateSourceLocation(
            ReflectionItem item,
            string sourcePathRoot,
            Func<string, string> gitUrlPattern,
            Func<string, string> publicGitUrlPattern)
        {
            if (!string.IsNullOrEmpty(item.SourceFileLocalPath)
                && item.SourceFileLocalPath.StartsWith(sourcePathRoot)
                && !item.Metadata.ContainsKey(OPSMetadata.ContentUrl))
            {
                string xmlPath = item.SourceFileLocalPath.Replace(sourcePathRoot, "/").Replace("\\", "/");

                string contentGitUrl = publicGitUrlPattern(xmlPath);
                item.Metadata[OPSMetadata.ContentUrl] = contentGitUrl;

                string originalContentGitUrl = gitUrlPattern(xmlPath);
                item.Metadata[OPSMetadata.OriginalContentUrl] = originalContentGitUrl;
                item.Metadata[OPSMetadata.RefSkeletionUrl] = originalContentGitUrl;
            }
        }

        private void BuildOtherMetadata()
        {
            foreach (var ns in _nsList)
            {
                bool nsInternalOnly = ns.Docs?.InternalOnly ?? false;
                AddAdditionalNotes(ns);
                if (!string.IsNullOrEmpty(ns.Docs?.AltCompliant))
                {
                    ns.Metadata[OPSMetadata.AltCompliant] = ns.Docs?.AltCompliant.ResolveCommentId(this)?.Uid;
                }
                if (nsInternalOnly)
                {
                    ns.Metadata[OPSMetadata.InternalOnly] = nsInternalOnly;
                }
                foreach (var t in ns.Types)
                {
                    BuildAssemblyMonikerMapping(t);
                    AddAdditionalNotes(t);
                    bool tInternalOnly = t.Docs?.InternalOnly ?? nsInternalOnly;
                    if (!string.IsNullOrEmpty(t.Docs?.AltCompliant))
                    {
                        t.Metadata[OPSMetadata.AltCompliant] = t.Docs?.AltCompliant.ResolveCommentId(this)?.Uid;
                    }
                    if (tInternalOnly)
                    {
                        t.Metadata[OPSMetadata.InternalOnly] = tInternalOnly;
                    }
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members)
                        {
                            bool mInternalOnly = m.Docs?.InternalOnly ?? tInternalOnly;
                            if (!string.IsNullOrEmpty(m.Docs?.AltCompliant))
                            {
                                m.Metadata[OPSMetadata.AltCompliant] = m.Docs?.AltCompliant.ResolveCommentId(this)?.Uid;
                            }
                            if (mInternalOnly)
                            {
                                m.Metadata[OPSMetadata.InternalOnly] = mInternalOnly;
                            }
                            if (m.ExtendedMetadata == null || m.ExtendedMetadata.Count == 0)
                            {
                                m.ExtendedMetadata = t.ExtendedMetadata;
                            }
                            AddAdditionalNotes(m);
                        }
                    }
                    if (t.Overloads != null)
                    {
                        foreach (var ol in t.Overloads)
                        {
                            if (ol.ExtendedMetadata == null || ol.ExtendedMetadata.Count == 0)
                            {
                                ol.ExtendedMetadata = t.ExtendedMetadata;
                            }
                        }
                    }
                }
            }
        }

        private void AddAdditionalNotes(ReflectionItem item)
        {
            if (item?.Docs.AdditionalNotes != null)
            {
                AdditionalNotes notes = new AdditionalNotes();
                foreach (var note in item.Docs.AdditionalNotes)
                {
                    var val = note.Value.TrimEnd();
                    switch (note.Key)
                    {
                        case "usage":
                            notes.Caller = val;
                            break;
                        case "overrides":
                            if (item.ItemType == ItemType.Interface
                                || item.Parent?.ItemType == ItemType.Interface
                                || item.Signatures.IsAbstract == true)
                            {
                                notes.Implementer = val;
                            }
                            else if (item.ItemType == ItemType.Class || item.Parent?.ItemType == ItemType.Class)
                            {
                                notes.Inheritor = val;
                            }
                            break;
                        default:
                            OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_NotesType_UnKnown, item.SourceFileLocalPath, note.Key);
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(notes.Caller)
                    || !string.IsNullOrEmpty(notes.Implementer)
                    || !string.IsNullOrEmpty(notes.Inheritor))
                {
                    item.Metadata[OPSMetadata.AdditionalNotes] = notes;
                }
            }
        }

        private void BuildExtensionMethods()
        {
            _extensionMethods = new List<Member>();
            foreach (var m in MembersByUid.Values)
            {
                if (!string.IsNullOrEmpty(m.DocId))
                {
                    var thisParam = m.Parameters?.FirstOrDefault(p => p.RefType == "this");
                    if (m.Parameters != null && thisParam != null)
                    {
                        if (m.Parent != null && (m.Parent.Signatures.IsStatic|| m.Parent.Signatures.IsPublicModule))
                        {
                            var targetType = thisParam.Type;

                            // Temp fix bug: 106581- C# extension methods not showing up in class reference docs
                            // Next step, transfer special char like "+" when loading xml data, need add more case to cover this change.
                            if (targetType.Contains("+"))
                            {
                                targetType = targetType.Replace("+", ".");
                            }

                            m.IsExtensionMethod = true;
                            if (IdExtensions.TryResolveSimpleTypeString(targetType, this, out string uid))
                            {
                                m.TargetUid = uid;
                                _extensionMethods.Add(m);
                            }
                        }
                    }
                }
            }

            if (_extensionMethods.Count == 0)
            {
                return;
            }

            ExtensionMethodsByMemberDocId = _extensionMethods.ToDictionary(ex => ex.DocId);
            ExtensionMethodUidsByTargetUid = _extensionMethods.ToLookup(ex => ex.TargetUid);
            foreach (var ex in _extensionMethods.Where(ex => ex.Uid == null))
            {
                OPSLogger.LogUserInfo(string.Format("ExtensionMethod {0} not found in its type {1}", ex.DocId, ex.Parent.Name), "index.xml");
            }

            foreach (var t in _tList)
            {
                var exMethodsFromBaseType = CheckAvailableExtensionMethods(t);
                if (exMethodsFromBaseType?.Count > 0)
                {
                    t.ExtensionMethods = exMethodsFromBaseType;
                }
            }
        }

        private List<VersionedString> CheckAvailableExtensionMethods(Type t)
        {
            var extensionMethods = GetExtensionMethodCandidatesForType(t.Uid);
            if (t.InheritanceChains != null)
            {
                foreach (var inheritanceChain in t.InheritanceChains)
                {
                    var extensionsPerChain = inheritanceChain.Values.Select(btUid => GetExtensionMethodCandidatesForType(btUid, inheritanceChain.Monikers))
                        .Where(exs => exs != null).SelectMany(exs => exs).DistinctBy(ext => ext.Value).ToList();
                    if (extensionMethods == null || extensionMethods.Count == 0)
                    {
                        extensionMethods = extensionsPerChain;
                    }
                    else
                    {
                        // merge extension methods found in different versions
                        // this is a O(n^2) operation, which is slow, however only a few classes have more than 1 inheritance chain, so we should be ok.
                        foreach (var extPerChain in extensionsPerChain)
                        {
                            var existingExt = extensionMethods.FirstOrDefault(ext => ext.Value == extPerChain.Value);
                            if (existingExt != null)
                            {
                                if (existingExt.Monikers != null)
                                {
                                    if (extPerChain.Monikers != null)
                                    {
                                        existingExt.Monikers = new HashSet<string>(existingExt.Monikers);
                                        existingExt.Monikers.UnionWith(extPerChain.Monikers);
                                    }
                                    else
                                    {
                                        existingExt.Monikers = null;
                                    }
                                }
                            }
                            else
                            {
                                extensionMethods.Add(extPerChain);
                            }
                        }
                    }
                }
            }

            if (extensionMethods != null)
            {
                foreach (var ext in extensionMethods)
                {
                    if (ext.Monikers != null)
                    {
                        ext.Monikers = ext.Monikers.Intersect(t.Monikers).ToHashSet();
                    }
                }
                extensionMethods = extensionMethods.DistinctBy(ext => ext.Value)
                    .OrderBy(ext => ext.Value).ToList();
            }
            return extensionMethods;

            List<VersionedString> GetExtensionMethodCandidatesForType(string uid, HashSet<string> monikers = null)
            {
                var a = GetExtensionMethodCandidatesForTypeCore(uid, monikers) ?? new List<VersionedString>();
                if (TypesByUid.TryGetValue(uid, out var type) && type.Interfaces?.Count > 0)
                {
                    foreach (var f in type.Interfaces)
                    {
                        var interfaceExts = GetExtensionMethodCandidatesForTypeCore(f.Value.ToOuterTypeUid(), f.Monikers ?? monikers);
                        if (interfaceExts != null)
                        {
                            a.AddRange(interfaceExts);
                        }
                    }
                }
                return a;
            }

            List<VersionedString> GetExtensionMethodCandidatesForTypeCore(string uid, HashSet<string> monikers = null)
            {
                if (ExtensionMethodUidsByTargetUid.Contains(uid))
                {
                    var exCandiates = ExtensionMethodUidsByTargetUid[uid].Where(ex =>
                    {
                        if (string.IsNullOrEmpty(ex.Uid))
                        {
                            return false;
                        }
                        HashSet<string> exMonikers = ex.Parent?.Monikers;
                        return (exMonikers == null && t.Monikers == null) ||
                               (exMonikers != null && t.Monikers != null && exMonikers.Overlaps(t.Monikers));
                    });

                    return exCandiates.Select(ex => new VersionedString(monikers, ex.Uid)).ToList();
                }
                return null;
            }
        }
        private void PopulateMonikers()
        {
            if (_frameworks == null || _frameworks.DocIdToFrameworkDict.Count == 0)
            {
                return;
            }

            var allMonikers = _frameworks.AllFrameworks;
            foreach (var ns in _nsList)
            {
                if (_frameworks.DocIdToFrameworkDict.ContainsKey(ns.CommentId))
                {
                    ns.Monikers = new HashSet<string>(_frameworks.DocIdToFrameworkDict[ns.CommentId]);
                }
                foreach (var t in ns.Types)
                {
                    if (!string.IsNullOrEmpty(t.DocId) && _frameworks.DocIdToFrameworkDict.ContainsKey(t.DocId))
                    {
                        t.Monikers = new HashSet<string>(_frameworks.DocIdToFrameworkDict[t.DocId]);
                        if (t.TypeForwardingChain != null)
                        {
                            t.TypeForwardingChain.Build(t.Monikers);
                        }
                        if (t.BaseTypes?.Count > 0)
                        {
                            //specify monikers for easier calculation
                            var remainingMonikers = new HashSet<string>(allMonikers);
                            foreach (var bt in t.BaseTypes.Where(b => b.Monikers != null))
                            {
                                remainingMonikers.ExceptWith(bt.Monikers);
                            }
                            foreach (var bt in t.BaseTypes.Where(b => b.Monikers == null))
                            {
                                bt.Monikers = remainingMonikers;
                            }
                        }
                    }
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members.Where(m => !string.IsNullOrEmpty(m.DocId)).ToList())
                        {
                            if (_frameworks.DocIdToFrameworkDict.ContainsKey(m.DocId))
                            {
                                m.Monikers = new HashSet<string>(_frameworks.DocIdToFrameworkDict[m.DocId]);
                                if (m.Signatures.IsProtected)
                                {
                                    //Filter out moniker of members that are in public sealed class;
                                    var publishSealedClasses = m.Parent.Signatures.GetPublishSealedClasses()?.SelectMany(s => s.Monikers).ToList();
                                    m.Monikers = m.Monikers.Except(publishSealedClasses).ToHashSet();
                                    if (m.Monikers.Count == 0)
                                    {
                                        t.Members.Remove(m);
                                    }
                                }
                            }
                            else
                            {
                                OPSLogger.LogUserError(LogCode.ECMA2Yaml_Framework_NotFound, m.SourceFileLocalPath, m.DocId);
                            }
                        }
                    }
                    //special handling for monikers metadata
                    if (t.Overloads != null)
                    {
                        foreach (var ol in t.Overloads)
                        {
                            var monikers = t.Members.Where(m => m.Overload == ol.Uid && !string.IsNullOrEmpty(m.DocId))
                                .SelectMany(m => _frameworks.DocIdToFrameworkDict.ContainsKey(m.DocId) ? _frameworks.DocIdToFrameworkDict[m.DocId] : Enumerable.Empty<string>()).Distinct().ToList();
                            if (monikers?.Count > 0)
                            {
                                ol.Monikers = new HashSet<string>(monikers);
                            }
                        }
                    }
                }
            }
        }

        private void BuildIds(IEnumerable<Namespace> nsList, IEnumerable<Type> tList)
        {
            foreach (var ns in nsList)
            {
                ns.Build(this);
            }
            foreach (var t in tList)
            {
                t.Build(this);
                if (t.BaseTypes != null)
                {
                    t.BaseTypes.ForEach(bt => bt.Build(this));
                }
            }
            foreach (var t in tList.Where(x => x.Members?.Count > 0))
            {
                t.Members.ForEach(m =>
                {
                    m.Build(this);
                    m.BuildName(this);
                });
            }

            foreach (var ns in nsList)
            {
                ns.Types = ns.Types.OrderBy(t => t.Uid, new TypeIdComparer()).ToList();
            }
        }

        private void BuildOverload(Type t)
        {
            var methods = t.Members?.Where(m =>
                m.ItemType == ItemType.Method
                || m.ItemType == ItemType.Constructor
                || m.ItemType == ItemType.Property
                || m.ItemType == ItemType.Operator
                || m.ItemType == ItemType.AttachedProperty)
                .ToList();
            if (methods?.Count() > 0)
            {
                Dictionary<string, Member> overloads = null;
                if (t.Overloads?.Count > 0)
                {
                    overloads = t.Overloads.Where(o => methods.Exists(m => m.Name == o.Name))
                        .ToDictionary(o => methods.First(m => m.Name == o.Name).GetOverloadId());
                }
                else
                {
                    overloads = new Dictionary<string, Member>();
                }
                foreach (var m in methods)
                {
                    string id = m.GetOverloadId();
                    if (!overloads.ContainsKey(id))
                    {
                        overloads.Add(id, new Member()
                        {
                            Name = m.Name,
                            Parent = t,
                            ItemType = m.ItemType
                        });
                    }
                    overloads[id].Id = id;
                    overloads[id].ItemType = m.ItemType;
                    overloads[id].DisplayName = m.ItemType == ItemType.Constructor ? t.Name : TrimDisplayName(m.DisplayName);
                    overloads[id].FullDisplayName = overloads[id].FullDisplayName ?? TrimDisplayName(m.FullDisplayName);
                    overloads[id].SourceFileLocalPath = m.SourceFileLocalPath;

                    if (overloads[id].ItemType == ItemType.Property)
                    {
                        overloads[id].DisplayName = RemoveindexerFromPropertyName(overloads[id].DisplayName);
                        overloads[id].FullDisplayName = RemoveindexerFromPropertyName(overloads[id].FullDisplayName);
                    }

                    if (overloads[id].Modifiers == null)
                    {
                        overloads[id].Modifiers = new SortedList<string, List<string>>();
                    }
                    foreach (var pair in m.Modifiers)
                    {
                        if (overloads[id].Modifiers.ContainsKey(pair.Key))
                        {
                            overloads[id].Modifiers[pair.Key].AddRange(pair.Value);
                        }
                        else
                        {
                            overloads[id].Modifiers[pair.Key] = new List<string>(pair.Value);
                        }
                    }

                    if (overloads[id].AssemblyInfo == null)
                    {
                        overloads[id].AssemblyInfo = new List<AssemblyInfo>();
                    }
                    overloads[id].AssemblyInfo.AddRange(m.AssemblyInfo);

                    m.Overload = overloads[id].Uid;
                }
                if (overloads.Count > 0)
                {
                    foreach (var overload in overloads.Values)
                    {
                        foreach (var lang in overload.Modifiers.Keys.ToList())
                        {
                            overload.Modifiers[lang] = overload.Modifiers[lang].Distinct().ToList();
                        }
                        overload.AssemblyInfo = overload.AssemblyInfo.Distinct().ToList();
                        ItemsByDocId[overload.CommentId] = overload;
                    }
                    t.Overloads = overloads.Values.ToList();
                }
            }

            string TrimDisplayName(string displayName)
            {
                if (displayName.Contains('('))
                {
                    displayName = displayName.Substring(0, displayName.LastIndexOf('('));
                }
                //if (displayName.Contains('['))
                //{
                //    displayName = displayName.Substring(0, displayName.LastIndexOf('['));
                //}
                if (displayName.Contains('<'))
                {
                    if (!displayName.Contains('.') || displayName.LastIndexOf('<') > displayName.LastIndexOf('.'))
                    {
                        displayName = displayName.Substring(0, displayName.LastIndexOf('<'));
                    }
                }
                return displayName;
            }

        }

        private void BuildAttributes()
        {
            foreach (var t in _tList)
            {
                if (t.Attributes?.Count > 0)
                {
                    t.Attributes.ForEach(attr => ResolveAttribute(attr));
                }
                if (t.Members?.Count > 0)
                {
                    foreach (var m in t.Members)
                    {
                        if (m.Attributes?.Count > 0)
                        {
                            m.Attributes.ForEach(attr => ResolveAttribute(attr));
                        }
                    }
                }
            }
        }

        readonly string[] attributePrefix = { "get: ", "set: ", "add: ", "remove: " };

        private void ResolveAttribute(ECMAAttribute attr)
        {
            var fqn = attr.Declaration;
            if (fqn.Contains("("))
            {
                fqn = fqn.Substring(0, fqn.IndexOf("("));
            }
            foreach (var prefix in attributePrefix)
            {
                if (fqn.StartsWith(prefix))
                {
                    fqn = fqn.Substring(prefix.Length);
                }
            }
            var nameWithSuffix = fqn + "Attribute";
            if (TypesByFullName.ContainsKey(nameWithSuffix) || !TypesByFullName.ContainsKey(fqn))
            {
                fqn = nameWithSuffix;
            }
            attr.TypeFullName = fqn;
            if (FilterStore?.AttributeFilters?.Count > 0)
            {
                foreach (var f in FilterStore.AttributeFilters)
                {
                    var result = TypesByFullName.ContainsKey(fqn) ? f.Filter(TypesByFullName[fqn]) : f.Filter(fqn);
                    if (result.HasValue)
                    {
                        attr.Visible = result.Value;
                    }
                }
            }
        }

        private void AddInheritanceMapping(string childUid, string parentUid, HashSet<string> monikers = null)
        {
            if (!InheritanceParentsByUid.ContainsKey(childUid))
            {
                InheritanceParentsByUid.Add(childUid, new List<VersionedString>());
            }
            InheritanceParentsByUid[childUid].Add(new VersionedString() { Value = parentUid, Monikers = monikers });

            if (!InheritanceChildrenByUid.ContainsKey(parentUid))
            {
                InheritanceChildrenByUid.Add(parentUid, new List<VersionedString>());
            }
            InheritanceChildrenByUid[parentUid].Add(new VersionedString() { Value = childUid, Monikers = monikers });
        }

        private void AddImplementMapping(string childUid, VersionedString parent)
        {
            var parentUid = parent.Value.ToOuterTypeUid();
            if (!ImplementationParentsByUid.ContainsKey(childUid))
            {
                ImplementationParentsByUid.Add(childUid, new List<VersionedString>());
            }
            ImplementationParentsByUid[childUid].Add(new VersionedString() { Value = parentUid, Monikers = parent.Monikers });

            if (!ImplementationChildrenByUid.ContainsKey(parentUid))
            {
                ImplementationChildrenByUid.Add(parentUid, new List<VersionedString>());
            }
            ImplementationChildrenByUid[parentUid].Add(new VersionedString() { Value = childUid, Monikers = parent.Monikers });
        }

        private void FillInheritanceImplementationGraph(Type t)
        {
            if (t.Interfaces?.Count > 0)
            {
                foreach (var f in t.Interfaces)
                {
                    AddImplementMapping(t.Uid, f);
                }
            }
            if (t.BaseTypes != null)
            {
                foreach (var bt in t.BaseTypes)
                {
                    if (bt.Uid != t.Uid)
                    {
                        AddInheritanceMapping(t.Uid, bt.Uid, bt.Monikers);
                    }
                }
            }
        }

        public List<VersionedCollection<string>> BuildInheritanceChain(string uid)
        {
            if (!TypesByUid.TryGetValue(uid, out Type t))
            {
                return null;
            }
            if (t.InheritanceChains != null)
            {
                return t.InheritanceChains; //already calculated, return directly
            }
            else if (InheritanceParentsByUid.TryGetValue(uid, out var parents))
            {
                var inheritanceChains = new List<VersionedCollection<string>>();
                foreach (var parent in parents)
                {
                    var grandParents = BuildInheritanceChain(parent.Value);
                    if (grandParents == null)
                    {
                        inheritanceChains.Add(new VersionedCollection<string>(parent.Monikers, new List<string>() { parent.Value }));
                    }
                    else
                    {
                        foreach (var grandParentChain in grandParents)
                        {
                            if (parent.Monikers.Overlaps(grandParentChain.Monikers))
                            {
                                var commonMonikers = new HashSet<string>(parent.Monikers.Intersect(grandParentChain.Monikers));
                                if (commonMonikers.Overlaps(t.Monikers))
                                {
                                    var chain = new List<string>(grandParentChain.Values);
                                    chain.Add(parent.Value);
                                    inheritanceChains.Add(new VersionedCollection<string>(commonMonikers, chain));
                                }
                            }
                        }
                    }
                }
                t.InheritanceChains = inheritanceChains;
                return inheritanceChains;
            }
            else
            {
                t.InheritanceChains = null;
                return null;
            }
        }

        private void BuildInheritance(Type t)
        {
            if (t.ItemType == ItemType.Interface)
            {
                BuildInheritanceForInterface(t);
            }
            else
            {
                BuildInheritanceDefault(t);
            }

            BuildInheritanceForDocs(t);
        }

        private void BuildInheritanceForInterface(Type t)
        {
            if (t.Interfaces?.Count > 0)
            {
                t.InheritedMembers = new Dictionary<string, VersionedString>();
                foreach (var f in t.Interfaces)
                {
                    var interfaceUid = f.Value.ToOuterTypeUid();

                    if (TypesByUid.TryGetValue(interfaceUid, out Type inter))
                    {
                        if (inter.Members != null)
                        {
                            foreach (var m in inter.Members)
                            {
                                if (m.Name != "Finalize" && m.ItemType != ItemType.Constructor && !m.Signatures.IsStatic)
                                {
                                    t.InheritedMembers[m.Id] = new VersionedString(f.Monikers, m.Uid);
                                }
                            }
                        }
                    }
                }
                if (t.Members != null)
                {
                    foreach (var m in t.Members)
                    {
                        if (t.InheritedMembers.ContainsKey(m.Id))
                        {
                            t.InheritedMembers.Remove(m.Id);
                        }
                    }
                }
                //transform to uid based dictionary too, similar to class inheritance
                t.InheritedMembers = t.InheritedMembers.ToDictionary(p => p.Value.Value, p => p.Value);
            }
        }

        private void BuildInheritanceDefault(Type t)
        {
            if (t.BaseTypes != null)
            {
                t.InheritanceChains = BuildInheritanceChain(t.Uid);

                if (t.ItemType == ItemType.Class && !t.Signatures.IsStatic)
                {
                    foreach (var inheritanceChain in t.InheritanceChains)
                    {
                        var inheritedMembersById = new Dictionary<string, VersionedString>();
                        var monikers = new HashSet<string>(inheritanceChain.Monikers);
                        monikers.IntersectWith(t.Monikers);
                        foreach (var btUid in inheritanceChain.Values)
                        {
                            if (TypesByUid.ContainsKey(btUid))
                            {
                                var bt = TypesByUid[btUid];
                                if (bt.Members != null)
                                {
                                    foreach (var m in bt.Members)
                                    {
                                        if (m.Name != "Finalize"
                                            && m.ItemType != ItemType.Constructor
                                            && m.ItemType != ItemType.AttachedProperty
                                            && m.ItemType != ItemType.AttachedEvent
                                            && !m.Signatures.IsStatic)
                                        {
                                            inheritedMembersById[m.Id] = new VersionedString(monikers, m.Uid);
                                        }
                                    }
                                }
                            }
                        }
                        if (t.Members != null)
                        {
                            foreach (var m in t.Members)
                            {
                                // could be defined in one moniker, but inherited in another moniker
                                // so we should check both the id and moniker
                                if (inheritedMembersById.TryGetValue(m.Id, out var inheritedMember))
                                {
                                    if (m.Monikers != null)
                                    {
                                        // Create another HashSet since all the inheritedMembersById share the same "Monikers" object
                                        inheritedMember.Monikers = inheritedMember.Monikers.Except(m.Monikers).ToHashSet();
                                        if (inheritedMember.Monikers.Count == 0)
                                        {
                                            inheritedMembersById.Remove(m.Id);
                                        }
                                    }
                                    else
                                    {
                                        OPSLogger.LogUserError(LogCode.ECMA2Yaml_Member_EmptyMoniker, m.SourceFileLocalPath, m.DocId);
                                    }
                                }
                            }
                        }

                        // merge with type level inherited members, which are tracked by uid instead of id.
                        if (t.InheritedMembers == null)
                        {
                            t.InheritedMembers = inheritedMembersById.ToDictionary(p => p.Value.Value, p => p.Value);
                        }
                        else
                        {
                            foreach (var inheritedMember in inheritedMembersById.Values)
                            {
                                if (t.InheritedMembers.TryGetValue(inheritedMember.Value, out var existingInheritedFrom))
                                {
                                    //inherited from the same type
                                    if (existingInheritedFrom.Monikers != null)
                                    {
                                        if (inheritedMember.Monikers != null)
                                        {
                                            existingInheritedFrom.Monikers = new HashSet<string>(existingInheritedFrom.Monikers);
                                            existingInheritedFrom.Monikers.UnionWith(inheritedMember.Monikers);
                                        }
                                        else
                                        {
                                            existingInheritedFrom.Monikers = null;
                                        }
                                    }
                                }
                                else
                                {
                                    t.InheritedMembers[inheritedMember.Value] = inheritedMember;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void BuildDocs(Type t)
        {
            if (t.Docs?.Inheritdoc != null)
            {
                SetInheritDocForType(t);
            }

            if (t.TypeParameters != null && t.Docs?.TypeParameters != null)
            {
                foreach (var ttp in t.TypeParameters)
                {
                    if (t.Docs.TypeParameters.TryGetValue(ttp.Name, out var ttpDesc))
                    {
                        ttp.Description = ttpDesc;
                    }
                }
            }
            if (t.Parameters != null && t.Docs?.Parameters != null)
            {
                foreach (var tp in t.Parameters)
                {
                    if (t.Docs.Parameters.TryGetValue(tp.Name, out var tpDesc))
                    {
                        tp.Description = tpDesc;
                    }
                }
            }
            if (t.Members != null)
            {
                foreach (var m in t.Members)
                {
                    // comment out this code so we don't remove duplicated notes, for https://ceapex.visualstudio.com/Engineering/_workitems/edit/41762
                    //if (m.Docs?.AdditionalNotes != null && t.Docs?.AdditionalNotes != null)
                    //{
                    //    m.Docs.AdditionalNotes = m.Docs.AdditionalNotes.Where(p => !(t.Docs.AdditionalNotes.ContainsKey(p.Key) && t.Docs.AdditionalNotes[p.Key] == p.Value))
                    //        .ToDictionary(p => p.Key, p => p.Value);
                    //}

                    if (m.Docs?.Inheritdoc != null)
                    {
                        SetInheritDocForMember(m, t);
                    }

                    if (m.TypeParameters != null && m.Docs?.TypeParameters != null)
                    {
                        foreach (var mtp in m.TypeParameters)
                        {
                            if (m.Docs.TypeParameters.TryGetValue(mtp.Name, out var mtpDesc))
                            {
                                mtp.Description = mtpDesc;
                            }
                        }
                    }
                    if (m.Parameters != null && m.Docs?.Parameters != null)
                    {
                        foreach (var mp in m.Parameters)
                        {
                            if (m.Docs.Parameters.TryGetValue(mp.Name, out var mpDesc))
                            {
                                mp.Description = mpDesc;
                            }
                        }
                    }
                    if (m.ReturnValueType != null && m.Docs?.Returns != null)
                    {
                        m.ReturnValueType.Description = m.Docs.Returns;
                    }
                    if (StrictMode && m.Docs?.Exceptions != null)
                    {
                        foreach (var ex in m.Docs?.Exceptions)
                        {
                            if (!TypesByUid.ContainsKey(ex.Uid) && !MembersByUid.ContainsKey(ex.Uid))
                            {
                                OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_ExceptionTypeNotFound, m.SourceFileLocalPath, ex.Uid);
                            }
                        }
                    }
                }
            }
            if (t.ReturnValueType != null && t.Docs?.Returns != null)
            {
                t.ReturnValueType.Description = t.Docs.Returns;
            }
        }

        public static EcmaDesc GetOrAddTypeDescriptor(string typeString)
        {
            EcmaDesc desc;
            if (typeDescriptorCache.ContainsKey(typeString))
            {
                desc = typeDescriptorCache[typeString];
            }
            else if (typeString != null && typeString.EndsWith("*"))
            {
                if (EcmaParser.TryParse("T:" + typeString.TrimEnd('*'), out desc))
                {
                    desc.DescModifier = EcmaDesc.Mod.Pointer;
                    typeDescriptorCache.Add(typeString, desc);
                }
            }
            else if (typeString != null && typeString.EndsWith("&"))
            {
                if (EcmaParser.TryParse("T:" + typeString.TrimEnd('&'), out desc))
                {
                    desc.DescModifier = EcmaDesc.Mod.Ref;
                    typeDescriptorCache.Add(typeString, desc);
                }
            }
            else if (EcmaParser.TryParse("T:" + typeString, out desc))
            {
                typeDescriptorCache.Add(typeString, desc);
            }
            return desc;
        }

        public FrameworkIndex GetFrameworkIndex()
        {
            return _frameworks;
        }

        private string RemoveindexerFromPropertyName(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            if (str.Contains("[") && str.Contains("]"))
            {
                str = str.Substring(0, str.IndexOf('['));
                str += "[]";
            }

            return str;
        }

        #region Inherit docs
        private void BuildInheritanceForDocs(Type t)
        {
            var allInheritedMembersById = new Dictionary<string, List<string>>();
            if (t.BaseTypes != null)
            {
                if (t.ItemType == ItemType.Class && !t.Signatures.IsStatic)
                {
                    foreach (var inheritanceChain in t.InheritanceChains)
                    {
                        foreach (var btUid in inheritanceChain.Values)
                        {
                            if (TypesByUid.ContainsKey(btUid))
                            {
                                var bt = TypesByUid[btUid];
                                if (bt.Members != null)
                                {
                                    foreach (var m in bt.Members)
                                    {
                                        if (m.Name != "Finalize"
                                            && m.ItemType != ItemType.AttachedProperty
                                            && m.ItemType != ItemType.AttachedEvent
                                            && !m.Signatures.IsStatic)
                                        {
                                            var inheritDocId = m.Id;
                                            if (allInheritedMembersById.ContainsKey(inheritDocId))
                                            {
                                                allInheritedMembersById[inheritDocId].Add(m.Uid);
                                            }
                                            else
                                            {
                                                allInheritedMembersById[inheritDocId] = new List<string>();
                                                allInheritedMembersById[inheritDocId].Add(m.Uid);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (CrossRepoParentsByUid.ContainsKey(t.Uid))
                                {
                                    CrossRepoParentsByUid[t.Uid].Add(btUid);
                                }
                                else
                                {
                                    CrossRepoParentsByUid[t.Uid] = new List<string>();
                                    CrossRepoParentsByUid[t.Uid].Add(btUid);
                                }
                            }
                        }
                    }
                }
            }


            if (t.Interfaces?.Count > 0)
            {
                foreach (var f in t.Interfaces)
                {
                    var interfaceUid = f.Value.ToOuterTypeUid();

                    if (TypesByUid.TryGetValue(interfaceUid, out Type inter))
                    {
                        if (inter.Members != null)
                        {
                            foreach (var m in inter.Members)
                            {
                                if (m.Name != "Finalize" && !m.Signatures.IsStatic)
                                {
                                    var inheritDocId = m.Id;
                                    if (allInheritedMembersById.ContainsKey(inheritDocId))
                                    {
                                        allInheritedMembersById[inheritDocId].Add(m.Uid);
                                    }
                                    else
                                    {
                                        allInheritedMembersById[inheritDocId] = new List<string>();
                                        allInheritedMembersById[inheritDocId].Add(m.Uid);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (CrossRepoParentsByUid.ContainsKey(t.Uid))
                        {
                            CrossRepoParentsByUid[t.Uid].Add(interfaceUid);
                        }
                        else
                        {
                            CrossRepoParentsByUid[t.Uid] = new List<string>();
                            CrossRepoParentsByUid[t.Uid].Add(interfaceUid);
                        }
                    }
                }

            }

            t.InheritedMembersById = allInheritedMembersById;
        }

        private void SetInheritDocForType(Type t)
        {
            if (!IsNeedInheritdoc(t))
            {
                return;
            }

            bool inheritedFlag = false;
            if (t.BaseTypes?.Count > 0)
            {
                foreach (var bt in t.BaseTypes)
                {
                    if (TypesByUid.ContainsKey(bt.Uid))
                    {
                        var baseType = TypesByUid[bt.Uid];
                        inheritedFlag = SetInheritDoc(baseType.Docs, t.Docs);
                        if (inheritedFlag)
                        {
                            InheritDocItemsByUid.Add(t.Uid, baseType.Uid);
                            break;
                        }
                    }
                }
            }

            if (!inheritedFlag && t.Interfaces?.Count > 0)
            {
                foreach (var intf in t.Interfaces)
                {
                    string uid = intf.Value.ToOuterTypeUid();
                    if (TypesByUid.ContainsKey(uid))
                    {
                        var parentInterface = TypesByUid[uid];
                        if (SetInheritDoc(parentInterface.Docs, t.Docs))
                        {
                            InheritDocItemsByUid.Add(t.Uid, parentInterface.Uid);
                            break;
                        }
                    }
                }
            }

            DoValidation(t, t, true);
        }

        private void SetInheritDocForMember(Member m, Type t)
        {
            if (m.Signatures.IsStatic)
            {
                OPSLogger.LogUserSuggestion(LogCode.ECMA2Yaml_Inheritdoc_InvalidTagsForStatic, t.SourceFileLocalPath, m.Uid);
                return;
            }

            if (!IsNeedInheritdoc(t))
            {
                return;
            }

            if (m.ItemType == ItemType.Method || m.ItemType == ItemType.Constructor || m.ItemType == ItemType.Property)
            {
                // 1. Get inheritdoc from cref object
                if (!string.IsNullOrEmpty((m.Docs?.Inheritdoc.Cref)))
                {
                    var crefUid = m.Docs?.Inheritdoc.Cref.Substring(2);
                    if (MembersByUid.ContainsKey(crefUid))
                    {
                        var inheritFrom = MembersByUid[crefUid];
                        if (SetInheritDoc(inheritFrom.Docs, m.Docs))
                        {
                            InheritDocItemsByUid.Add(m.Uid, inheritFrom.Uid);
                        }
                    }
                    else
                    {
                        m.CrossInheritdocUid = crefUid;
                    }
                }

                // 2. Get inheritdoc from implements
                if (!InheritDocItemsByUid.ContainsKey(m.Uid) && string.IsNullOrEmpty(m.CrossInheritdocUid) && m.Implements != null && m.Implements.Count() > 0)
                {
                    var implementUid = "";
                    var implementDocId = m.Implements.FirstOrDefault().Value;
                    if (!string.IsNullOrEmpty(implementDocId))
                    {
                        implementUid = implementDocId.Substring(2);
                    }

                    if (!string.IsNullOrEmpty(implementUid))
                    {
                        if (MembersByUid.ContainsKey(implementUid))
                        {
                            var inheritFrom = MembersByUid[implementUid];
                            if (SetInheritDoc(inheritFrom.Docs, m.Docs))
                            {
                                InheritDocItemsByUid.Add(m.Uid, inheritFrom.Uid);
                            }
                        }
                        else
                        {
                            m.CrossInheritdocUid = implementUid;
                        }
                    }

                }

                // 3. Get inheritdoc from inhertance chain list
                if (!InheritDocItemsByUid.ContainsKey(m.Uid) && string.IsNullOrEmpty(m.CrossInheritdocUid))
                {
                    var inheritDocId = m.Id;
                    if (t.InheritedMembersById?.Count > 0 && t.InheritedMembersById.ContainsKey(inheritDocId))
                    {
                        var uids = t.InheritedMembersById[inheritDocId];
                        if (uids.Count > 0)
                        {
                            foreach (var uid in uids)
                            {
                                if (MembersByUid.ContainsKey(uid))
                                {
                                    var inheritFrom = MembersByUid[uid];
                                    if (SetInheritDoc(inheritFrom.Docs, m.Docs))
                                    {
                                        InheritDocItemsByUid.Add(m.Uid, inheritFrom.Uid);
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            OPSLogger.LogUserSuggestion(LogCode.ECMA2Yaml_Inheritdoc_NoFoundParent, m.SourceFileLocalPath, inheritDocId, m.Uid);
                        }
                    }
                    else
                    {
                        OPSLogger.LogUserSuggestion(LogCode.ECMA2Yaml_Inheritdoc_NoFoundParent, m.SourceFileLocalPath, inheritDocId, m.Uid);
                    }
                }
            }
            else
            {
                OPSLogger.LogUserSuggestion(LogCode.ECMA2Yaml_Inheritdoc_NotSupportType, t.SourceFileLocalPath, m.ItemType, m.Uid);
                return;
            }

            DoValidation(m, t, false);
        }

        private bool SetInheritDoc(Docs inheritFrom, Docs inheritTo)
        {
            bool isInherit = false;

            if (!string.IsNullOrEmpty(inheritFrom?.Summary) && string.IsNullOrEmpty(inheritTo?.Summary))
            {
                inheritTo.Summary = inheritFrom.Summary;
                isInherit = true;
            }
            if (inheritFrom?.Parameters?.Count > 0 && inheritTo?.Parameters?.Count > 0)
            {
                inheritFrom.Parameters.ToList().ForEach(p => {
                    if (!string.IsNullOrEmpty(p.Value) && inheritTo.Parameters.ContainsKey(p.Key))
                    {
                        if (string.IsNullOrEmpty(inheritTo.Parameters[p.Key]))
                        {
                            inheritTo.Parameters[p.Key] = p.Value;
                            isInherit = true;
                        }
                    }
                });
            }
            if (!string.IsNullOrEmpty(inheritFrom?.Returns) && string.IsNullOrEmpty(inheritTo?.Returns))
            {
                inheritTo.Returns = inheritFrom.Returns;
                isInherit = true;
            }
            if (!string.IsNullOrEmpty(inheritFrom?.Value) && string.IsNullOrEmpty(inheritTo?.Value))
            {
                inheritTo.Value = inheritFrom.Value;
                isInherit = true;
            }

            return isInherit;
        }

        private bool DoValidation(ReflectionItem current, Type t, bool isType)
        {
            if (string.IsNullOrEmpty(current.Docs?.Summary) &&
                current.Docs?.Parameters?.Count == 0 &&
                string.IsNullOrEmpty(current.Docs?.Returns) &&
                string.IsNullOrEmpty(current.Docs?.Value))
            {
                // If we can't found inheritdoc from inner repo and can't found from implements
                // Get first parent(base class or interface) uid set to CrossInheritdocUid
                if (string.IsNullOrEmpty(current.CrossInheritdocUid))
                {
                    if (CrossRepoParentsByUid.ContainsKey(t.Uid))
                    {
                        if (isType)
                        {
                            current.CrossInheritdocUid = CrossRepoParentsByUid[t.Uid].FirstOrDefault();
                        }
                        else
                        {
                            current.CrossInheritdocUid = current.Uid.Replace(t.Uid, CrossRepoParentsByUid[t.Uid].FirstOrDefault());
                        }
                    }

                    if (string.IsNullOrEmpty(current.CrossInheritdocUid))
                    {
                        OPSLogger.LogUserSuggestion(LogCode.ECMA2Yaml_Inheritdoc_NoFoundDocs, current.SourceFileLocalPath, current.Uid);
                    }
                }
                return false;
            }

            return true;
        }

        private bool IsNeedInheritdoc(Type t)
        {
            if (t.Signatures.IsStatic)
            {
                OPSLogger.LogUserSuggestion(LogCode.ECMA2Yaml_Inheritdoc_InvalidTagsForStatic, t.SourceFileLocalPath, t.Uid);
                return false;
            }

            if (t.ItemType != ItemType.Class && t.ItemType != ItemType.Interface && t.ItemType != ItemType.Struct)
            {
                OPSLogger.LogUserSuggestion(LogCode.ECMA2Yaml_Inheritdoc_NotSupportType, t.SourceFileLocalPath, t.ItemType, t.Uid);
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion
    }
}
