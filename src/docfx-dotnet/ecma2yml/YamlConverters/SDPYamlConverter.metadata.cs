using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System.Collections.Generic;

namespace ECMA2Yaml
{
    public partial class SDPYamlConverter
    {
        private Dictionary<string, string> MetadataMapping = new Dictionary<string, string>() {
            { OPSMetadata.OriginalContentUrl, OPSMetadata.SDP_op_overwriteFileGitUrl },
            { OPSMetadata.RefSkeletionUrl, OPSMetadata.RefSkeletionUrl/*SDP_op_articleFileGitUrl*/ },
            { OPSMetadata.ContentUrl, OPSMetadata.ContentUrl }
        };

        private void MergeAllowListedMetadata(ItemSDPModelBase model, ReflectionItem item)
        {
            if (item?.Metadata != null)
            {
                foreach (var pair in item.Metadata)
                {
                    if (MetadataMapping.TryGetValue(pair.Key, out string newKey))
                    {
                        model.Metadata[newKey] = pair.Value;
                    }
                }
            }
            if (item?.ExtendedMetadata?.Count > 0)
            {
                foreach (var pair in item.ExtendedMetadata)
                {
                    model.Metadata[pair.Key] = pair.Value;
                }
            }
        }
    }
}
