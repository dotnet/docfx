using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Linq;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        public NamespaceSDPModel FormatNamespace(Namespace nsItem)
        {
            var sdpNS = InitWithBasicProperties<NamespaceSDPModel>(nsItem);

            if (nsItem.Types != null)
            {
                foreach (var tGroup in nsItem.Types?.GroupBy(t => t.ItemType))
                {
                    switch (tGroup.Key)
                    {
                        case ItemType.Class:
                            sdpNS.Classes = tGroup.Select(t => ConvertNamespaceTypeLink(nsItem, t)).ToList();
                            break;
                        case ItemType.Delegate:
                            sdpNS.Delegates = tGroup.Select(t => ConvertNamespaceTypeLink(nsItem, t)).ToList();
                            break;
                        case ItemType.Interface:
                            sdpNS.Interfaces = tGroup.Select(t => ConvertNamespaceTypeLink(nsItem, t)).ToList();
                            break;
                        case ItemType.Struct:
                            sdpNS.Structs = tGroup.Select(t => ConvertNamespaceTypeLink(nsItem, t)).ToList();
                            break;
                        case ItemType.Enum:
                            sdpNS.Enums = tGroup.Select(t => ConvertNamespaceTypeLink(nsItem, t)).ToList();
                            break;
                    }
                }
            }

            return sdpNS;
        }
    }
}
