using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Linq;
using Type = ECMA2Yaml.Models.Type;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        public DelegateSDPModel FormatDelegate(Type t)
        {
            var sdpDelegate = InitWithBasicProperties<DelegateSDPModel>(t);

            sdpDelegate.TypeParameters = ConvertTypeParameters(t);
            sdpDelegate.Inheritances = t.InheritanceChains?.LastOrDefault().Values.Select(uid => UidToTypeMDString(uid, _store)).ToList();

            if (t.ReturnValueType != null)
            {
                sdpDelegate.ReturnsWithMoniker = ConvertReturnValue(t.ReturnValueType, t.TypeParameters, t.Signatures.DevLangs);
            }

            sdpDelegate.Parameters = t.Parameters?.Select(p => ConvertNamedParameter(p, t.TypeParameters, t.Signatures.DevLangs))
                .ToList().NullIfEmpty();

            if (t.Attributes != null
                && t.Attributes.Any(attr => attr.Declaration == "System.CLSCompliant(false)"))
            {
                sdpDelegate.IsNotClsCompliant = true;
            }
            sdpDelegate.AltCompliant = t.Docs.AltCompliant.ResolveCommentId(_store)?.Uid;

            if (t.ExtensionMethods?.Count > 0)
            {
                sdpDelegate.ExtensionMethods = t.ExtensionMethods.Select(im => ExtensionMethodToTypeMemberLink(t, im))
                    .Where(ext => ext != null).ToList();
            }

            return sdpDelegate;
        }
    }
}
