using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Linq;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        public MemberSDPModel FormatSingleMember(Member m)
        {
            var sdpMember = InitWithBasicProperties<MemberSDPModel>(m);

            sdpMember.TypeParameters = ConvertTypeParameters(m);
            sdpMember.ThreadSafety = ConvertThreadSafety(m);
            sdpMember.ImplementsWithMoniker = m.Implements?.Select(impl => new VersionedString()
            {
                Monikers = impl.Monikers,
                Value = DocIdToTypeMDString(impl.Value, _store),
                valuePerLanguage = TypeStringToMDWithTypeMapping(impl.Value, m.Signatures?.DevLangs, nullIfTheSame: true)
            });

            sdpMember.ImplementsMonikers = ConverterHelper.ConsolidateVersionedValues(sdpMember.ImplementsWithMoniker, m.Monikers);

            var knowTypeParams = m.Parent.TypeParameters.ConcatList(m.TypeParameters);

            if (m.ReturnValueType != null)
            {
                sdpMember.ReturnsWithMoniker = ConvertReturnValue(m.ReturnValueType, knowTypeParams, m.Signatures.DevLangs);
            }

            sdpMember.Parameters = m.Parameters?.Select(p => ConvertNamedParameter(p, knowTypeParams, m.Signatures.DevLangs))
                .ToList().NullIfEmpty();

            sdpMember.Exceptions = m.Docs.Exceptions?.Select(
                p => new TypeReference()
                {
                    Description = p.Description,
                    Type = UidToTypeMDString(p.Uid, _store)
                }).ToList().NullIfEmpty();

            sdpMember.Permissions = m.Docs.Permissions?.Select(
                p => new TypeReference()
                {
                    Description = p.Description,
                    Type = DocIdToTypeMDString(p.CommentId, _store)
                }).ToList().NullIfEmpty();

            if (m.Attributes != null
                && m.Attributes.Any(attr => attr.Declaration == "System.CLSCompliant(false)"))
            {
                sdpMember.IsNotClsCompliant = true;
            }
            sdpMember.AltCompliant = m.Docs.AltCompliant.ResolveCommentId(_store)?.Uid;
            sdpMember.Type = m.ItemType.ToString().ToLower();

            return sdpMember;
        }
    }
}
