using Newtonsoft.Json;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class TypeSDPModel : ItemSDPModelBase
    {
        [JsonIgnore]
        [YamlIgnore]
        public override string YamlMime { get; } = "YamlMime:NetType";

        [JsonProperty("typeParameters")]
        [YamlMember(Alias = "typeParameters")]
        public IEnumerable<TypeParameterSDPModel> TypeParameters { get; set; }

        [JsonProperty("type")]
        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [JsonProperty("threadSafety")]
        [YamlMember(Alias = "threadSafety")]
        public ThreadSafety ThreadSafety { get; set; }

        [JsonProperty("permissions")]
        [YamlMember(Alias = "permissions")]
        public IEnumerable<TypeReference> Permissions { get; set; }

        [JsonProperty("implementsWithMoniker")]
        [YamlMember(Alias = "implementsWithMoniker")]
        public IEnumerable<VersionedString> ImplementsWithMoniker { get; set; }

        /// <summary>
        /// A collection of all monikers that apply to this type's implements.
        /// </summary>
        [JsonProperty("implementsMonikers")]
        [YamlMember(Alias = "implementsMonikers")]
        public IEnumerable<string> ImplementsMonikers { get; set; }

        [JsonProperty("inheritancesWithMoniker")]
        [YamlMember(Alias = "inheritancesWithMoniker")]
        public IEnumerable<VersionedCollection<string>> InheritancesWithMoniker { get; set; }

        [JsonProperty("derivedClassesWithMoniker")]
        [YamlMember(Alias = "derivedClassesWithMoniker")]
        public IEnumerable<VersionedString> DerivedClassesWithMoniker { get; set; }

        [JsonProperty("isNotClsCompliant")]
        [YamlMember(Alias = "isNotClsCompliant")]
        public bool? IsNotClsCompliant { get; set; }

        [JsonProperty("altCompliant")]
        [YamlMember(Alias = "altCompliant")]
        public string AltCompliant { get; set; }

        #region Children

        [JsonProperty("extensionMethods")]
        [YamlMember(Alias = "extensionMethods")]
        public IEnumerable<TypeMemberLink> ExtensionMethods { get; set; }

        [JsonProperty("constructors")]
        [YamlMember(Alias = "constructors")]
        public IEnumerable<TypeMemberLink> Constructors { get; set; }

        [JsonProperty("operators")]
        [YamlMember(Alias = "operators")]
        public IEnumerable<TypeMemberLink> Operators { get; set; }

        [JsonProperty("methods")]
        [YamlMember(Alias = "methods")]
        public IEnumerable<TypeMemberLink> Methods { get; set; }

        [JsonProperty("eiis")]
        [YamlMember(Alias = "eiis")]
        public IEnumerable<TypeMemberLink> EIIs { get; set; }

        [JsonProperty("properties")]
        [YamlMember(Alias = "properties")]
        public IEnumerable<TypeMemberLink> Properties { get; set; }

        [JsonProperty("events")]
        [YamlMember(Alias = "events")]
        public IEnumerable<TypeMemberLink> Events { get; set; }

        [JsonProperty("fields")]
        [YamlMember(Alias = "fields")]
        public IEnumerable<TypeMemberLink> Fields { get; set; }

        [JsonProperty("attachedEvents")]
        [YamlMember(Alias = "attachedEvents")]
        public IEnumerable<TypeMemberLink> AttachedEvents { get; set; }

        [JsonProperty("attachedProperties")]
        [YamlMember(Alias = "attachedProperties")]
        public IEnumerable<TypeMemberLink> AttachedProperties { get; set; }

        #endregion
    }
}
