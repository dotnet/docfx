using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;

namespace TripleCrownValidation.Models
{
    public class ValidatorHierarchyItem : HierarchyItem, IValidateModel
    {
        [JsonProperty("serviceData")]
        public string ServiceData { get; set; }
        [JsonProperty("source_relative_path")]
        public string SourceRelativePath { get; set; }
        public bool IsValid { get; set ; }
        public bool IsDeleted { get; set; }
        public string Uid => UId;
        public IValidateModel Parent { get; set; }
        [JsonProperty("ms.date")]
        public string MSDate { get; set; }
        [JsonProperty("updated_at")]
        public string PublishUpdatedAt { get; set; }
        [JsonProperty("page_kind")]
        public string PageKind { get; set; }

    }
}
