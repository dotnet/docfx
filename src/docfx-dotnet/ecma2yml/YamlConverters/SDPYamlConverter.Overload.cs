using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Collections.Generic;
using System.Linq;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        public OverloadSDPModel FormatOverload(Member overload, List<Member> members)
        {
            var sdpOverload = new OverloadSDPModel()
            {
                Uid = overload?.Uid,
                CommentId = overload?.CommentId,
                Name = overload?.DisplayName,
                FullName = overload?.FullDisplayName,
                Summary = overload?.Docs?.Summary,
                Remarks = overload?.Docs?.Remarks,
                Examples = overload?.Docs?.Examples,
                Type = members.First().ItemType.ToString().ToLower(),
                Members = members.Select(m => FormatSingleMember(m)).ToList()
            };
            if (overload != null)
            {
                sdpOverload.NameWithType = members.First().Parent.Name + "." + sdpOverload.Name;
            }

            if (!_store.UWPMode)
            {
                sdpOverload.AssembliesWithMoniker = overload == null ? sdpOverload.Members.First().AssembliesWithMoniker
                    : MonikerizeAssemblyStrings(overload);
                sdpOverload.PackagesWithMoniker = overload == null ? sdpOverload.Members.First().PackagesWithMoniker
                    : MonikerizePackageStrings(overload, _store.PkgInfoMapping);
            }

            sdpOverload.Namespace = sdpOverload.Members.First().Namespace;
            sdpOverload.DevLangs = sdpOverload.Members.SelectMany(m => m.DevLangs).Distinct().ToList();
            sdpOverload.Monikers = sdpOverload.Members.Where(m => m.Monikers != null).SelectMany(m => m.Monikers).Distinct().ToList();

            bool resetMemberThreadSafety = false;
            // One group members keep only one thread safety info
            var withThreadSafetyMembers = sdpOverload.Members.Where(p => p.ThreadSafety != null).ToList();
            if (sdpOverload != null && overload?.Docs?.ThreadSafetyInfo != null)
            {
                sdpOverload.ThreadSafety = ConvertThreadSafety(overload);
                resetMemberThreadSafety = true;
            }

            foreach (var m in sdpOverload.Members)
            {
                m.Namespace = null;
                m.AssembliesWithMoniker = null;
                m.PackagesWithMoniker = null;
                m.DevLangs = null;

                if (resetMemberThreadSafety && m.ThreadSafety != null)
                {
                    m.ThreadSafety = null;
                }
            }

            GenerateRequiredMetadata(sdpOverload, overload ?? members.First(), members.Cast<ReflectionItem>().ToList());

            return sdpOverload;
        }
    }
}
