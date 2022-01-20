using System.Collections.Generic;
using System.Linq;

namespace ECMA2Yaml.Models
{
    public partial class ECMAStore
    {
        private void MonikerizeAssembly()
        {
            if (_frameworks.FrameworkAssemblies == null
                || _frameworks.FrameworkAssemblies.Count == 0)
            {
                return; //legacy xml, without any assemblies info in frameworkindex
            }
            foreach (var ns in _nsList)
            {
                foreach (var t in ns.Types)
                {
                    if (t.Monikers != null)
                    {
                        MonikerizeTypeAssembly(t);
                    }
                    if (t.Members != null)
                    {
                        foreach (var m in t.Members.Where(m => !string.IsNullOrEmpty(m.DocId)))
                        {
                            if (m.Monikers != null)
                            {
                                MonikerizeMemberAssembly(m, t.VersionedAssemblyInfo.ValuesPerMoniker);
                            }
                        }
                    }
                    //special handling for monikers metadata
                    if (t.Overloads != null)
                    {
                        foreach (var ol in t.Overloads)
                        {
                            if (ol.Monikers != null)
                            {
                                MonikerizeMemberAssembly(ol, t.VersionedAssemblyInfo.ValuesPerMoniker);
                            }
                        }
                    }
                }
            }
        }

        private void MonikerizeTypeAssembly(Type t)
        {
            if (_frameworks.FrameworkAssemblies?.Count > 0 && t.AssemblyInfo != null)
            {
                var valuesPerMoniker = new Dictionary<string, List<AssemblyInfo>>();
                foreach (var moniker in t.Monikers)
                {
                    if (_frameworks.FrameworkAssemblies.ContainsKey(moniker))
                    {
                        var frameworkAssemblies = _frameworks.FrameworkAssemblies[moniker];
                        var assemblies = t.AssemblyInfo.Where(
                            itemAsm => frameworkAssemblies.TryGetValue(itemAsm.Name, out var fxAsm) && fxAsm.Version == itemAsm.Version).ToList();
                        if (t.TypeForwardingChain?.TypeForwardingsPerMoniker != null
                            && t.TypeForwardingChain.TypeForwardingsPerMoniker.TryGetValue(moniker, out var fwdList) == true)
                        {
                            foreach (var fwd in fwdList)
                            {
                                if (assemblies.Contains(fwd.From) && assemblies.Contains(fwd.To))
                                {
                                    assemblies.Remove(fwd.From);
                                }
                            }
                        }
                        if (assemblies.Count == 0)
                        {
                            OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_UidAssembly_NotMatched, t.SourceFileLocalPath, t.Uid, moniker);
                        }
                        valuesPerMoniker[moniker] = assemblies;
                    }
                }
                t.VersionedAssemblyInfo = new VersionedProperty<AssemblyInfo>(valuesPerMoniker);
            }
        }

        private void MonikerizeMemberAssembly(Member m, Dictionary<string, List<AssemblyInfo>> typeAssemblies)
        {
            var mAssemblies = new Dictionary<string, List<AssemblyInfo>>();
            foreach (var moniker in m.Monikers)
            {
                if (typeAssemblies.TryGetValue(moniker, out var assemblies))
                {
                    mAssemblies[moniker] = assemblies;
                }
                else
                {
                    OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_ExtraMonikerFoundInMember, m.SourceFileLocalPath, moniker, m.Uid);
                }
            }
            m.VersionedAssemblyInfo = new VersionedProperty<AssemblyInfo>(mAssemblies);
        }


        private void BuildAssemblyMonikerMapping(ReflectionItem item)
        {
            if (item.VersionedAssemblyInfo != null)
            {
                var dict = item.VersionedAssemblyInfo.MonikersPerValue
                    .GroupBy(p => p.Key.Name)
                    .OrderBy(p => p.Key)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        {
                            var monikers = g.SelectMany(p => p.Value).ToList();
                            monikers.Sort();
                            return monikers;
                        });
                if (dict.Any())
                {
                    item.Metadata[OPSMetadata.AssemblyMonikerMapping] = dict;
                }
            }
        }

        private void FindMissingAssemblyNamesAndVersions()
        {
            foreach (var t in _tList)
            {
                //Sometimes type forwarding definitions do not specify the target version,
                //so we need to infer the actual version based on the version we have in frameworkindex file
                if (t.TypeForwardingChain?.TypeForwardingsPerMoniker?.Count > 0
                    && _frameworks.FrameworkAssemblies?.Count > 0)
                {
                    foreach (var fwdPerMoniker in t.TypeForwardingChain.TypeForwardingsPerMoniker)
                    {
                        if (_frameworks.FrameworkAssemblies.TryGetValue(fwdPerMoniker.Key, out var assemblyDict))
                        {
                            foreach (var fwd in fwdPerMoniker.Value)
                            {
                                if (fwd.To.Version == "0.0.0.0" && assemblyDict.TryGetValue(fwd.To.Name, out var asmInfo))
                                {
                                    fwd.To.Version = asmInfo.Version;
                                }
                            }
                        }
                    }
                }
                if (t.AssemblyInfo?.Count > 0 && t.Members?.Count > 0)
                {
                    foreach (var m in t.Members)
                    {
                        if (m.AssemblyInfo?.Count > 0)
                        {
                            foreach (var asm in m.AssemblyInfo)
                            {
                                if (string.IsNullOrEmpty(asm.Name) && asm.Version != null)
                                {
                                    var fallback = t.AssemblyInfo.FirstOrDefault(ta => ta.Version == asm.Version);
                                    asm.Name = fallback?.Name;
                                    OPSLogger.LogUserInfo($"AssemblyName fallback for {m.DocId} to {asm.Name}", m.SourceFileLocalPath);
                                }
                            }
                            // hack for https://github.com/mono/api-doc-tools/issues/399
                            var missingVersion = m.AssemblyInfo.Where(a => a.Version == null).ToList();
                            foreach (var asm in missingVersion)
                            {
                                var parentFallback = t.AssemblyInfo.Where(a => a.Name == asm.Name).ToList();
                                if (parentFallback.Count > 0)
                                {
                                    m.AssemblyInfo.Remove(asm);
                                    m.AssemblyInfo.AddRange(parentFallback);
                                    OPSLogger.LogUserInfo($"AssemblyVersion fallback for {m.DocId}, {asm.Name}", m.SourceFileLocalPath);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
