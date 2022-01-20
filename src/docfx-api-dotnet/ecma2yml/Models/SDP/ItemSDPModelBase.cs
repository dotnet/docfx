using Newtonsoft.Json;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public abstract class ItemSDPModelBase
    {
        public ItemSDPModelBase()
        {
            Metadata = new Dictionary<string, object>();
        }

        [JsonIgnore]
        [YamlIgnore]
        abstract public string YamlMime { get; }

        [JsonProperty("uid")]
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }

        [JsonProperty("commentId")]
        [YamlMember(Alias = "commentId")]
        public string CommentId { get; set; }

        [JsonProperty("namespace")]
        [YamlMember(Alias = "namespace")]
        public string Namespace { get; set; }

        [JsonProperty("name")]
        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [JsonProperty("fullName")]
        [YamlMember(Alias = "fullName")]
        public string FullName { get; set; }

        [JsonProperty("nameWithType")]
        [YamlMember(Alias = "nameWithType")]
        public string NameWithType { get; set; }

        [JsonProperty("assembliesWithMoniker")]
        [YamlMember(Alias = "assembliesWithMoniker")]
        public IEnumerable<VersionedString> AssembliesWithMoniker { get; set; }

        [JsonProperty("packagesWithMoniker")]
        [YamlMember(Alias = "packagesWithMoniker")]
        public IEnumerable<VersionedString> PackagesWithMoniker { get; set; }

        [JsonProperty("attributesWithMoniker")]
        [YamlMember(Alias = "attributesWithMoniker")]
        public IEnumerable<VersionedString> AttributesWithMoniker { get; set; }

        [JsonProperty("attributeMonikers")]
        [YamlMember(Alias = "attributeMonikers")]
        public IEnumerable<string> AttributeMonikers { get; set; }

        [JsonProperty("syntaxWithMoniker")]
        [YamlMember(Alias = "syntaxWithMoniker")]
        public IEnumerable<VersionedSignatureModel> SyntaxWithMoniker { get; set; }

        [JsonProperty("devLangs")]
        [YamlMember(Alias = "devLangs")]
        public IEnumerable<string> DevLangs { get; set; }

        [JsonProperty("monikers")]
        [YamlMember(Alias = "monikers")]
        public IEnumerable<string> Monikers { get; set; }

        [JsonProperty("seeAlso")]
        [YamlMember(Alias = "seeAlso")]
        public string SeeAlso { get; set; }

        [JsonProperty("obsoleteMessagesWithMoniker")]
        [YamlMember(Alias = "obsoleteMessagesWithMoniker")]
        public IEnumerable<VersionedString> ObsoleteMessagesWithMoniker { get; set; }

        [JsonProperty("isInternalOnly")]
        [YamlMember(Alias = "isInternalOnly")]
        public bool IsInternalOnly { get; set; }

        [JsonProperty("additionalNotes")]
        [YamlMember(Alias = "additionalNotes")]
        public AdditionalNotes AdditionalNotes { get; set; }

        [JsonProperty("summary")]
        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [JsonProperty("crossInheritdocsUid")]
        [YamlMember(Alias = "crossInheritdocsUid")]
        public string CrossInheritdocUid { get; set; }

        [JsonProperty("remarks")]
        [YamlMember(Alias = "remarks")]
        public string Remarks { get; set; }

        [JsonProperty("examples")]
        [YamlMember(Alias = "examples")]
        public string Examples { get; set; }

        [JsonProperty("uwpRequirements")]
        [YamlMember(Alias = "uwpRequirements")]
        public UWPRequirements UWPRequirements { get; set; }

        [JsonProperty("sdkRequirements")]
        [YamlMember(Alias = "sdkRequirements")]
        public SDKRequirements SDKRequirements { get; set; }

        [JsonProperty("osRequirements")]
        [YamlMember(Alias = "osRequirements")]
        public OSRequirements OSRequirements { get; set; }

        [JsonProperty("capabilities")]
        [YamlMember(Alias = "capabilities")]
        public IEnumerable<string> Capabilities { get; set; }

        [JsonProperty("xamlSyntax")]
        [YamlMember(Alias = "xamlSyntax")]
        public string XamlSyntax { get; set; }

        [JsonProperty("xamlMemberSyntax")]
        [YamlMember(Alias = "xamlMemberSyntax")]
        public string XamlMemberSyntax { get; set; }

        [JsonProperty("source")]
        [YamlMember(Alias = "source")]
        public SourceDetail Source { get; set; }

        [JsonProperty("metadata")]
        [YamlMember(Alias = "metadata")]
        public Dictionary<string, object> Metadata { get; set; }
    }
}
