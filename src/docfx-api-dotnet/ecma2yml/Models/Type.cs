using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ECMA2Yaml.Models
{
    public class Type : ReflectionItem
    {
        public string FullName { get; set; }
        public List<BaseType> BaseTypes { get; set; }
        public List<VersionedCollection<string>> InheritanceChains { get; set; }
        public TypeForwardingChain TypeForwardingChain { get; set; }
        public Dictionary<string, VersionedString> InheritedMembers { get; set; }
        public Dictionary<string, List<string>> InheritedMembersById { get; set; }  // Use to find inherit doc feature
        public List<string> IsA { get; set; }
        public List<VersionedString> Interfaces { get; set; }
        public List<Member> Members { get; set; }
        public List<Member> Overloads { get; set; }
        public List<VersionedString> ExtensionMethods { get; set; }
        private static Regex GenericRegex = new Regex("<[^<>]+>", RegexOptions.Compiled);

        public override void Build(ECMAStore store)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = Name.Replace('+', '.');
                if (Id.Contains('<'))
                {
                    Id = GenericRegex.Replace(Id, match => "`" + (match.Value.Count(c => c == ',') + 1));
                }
            }
        }

        public Type ShallowCopy()
        {
            Type rval = (Type)MemberwiseClone();
            rval.BaseTypes = BaseTypes == null ? null : new List<BaseType>(BaseTypes);
            rval.InheritanceChains = InheritanceChains == null ? null : new List<VersionedCollection<string>>(InheritanceChains);
            rval.InheritedMembers = InheritedMembers == null ? null : new Dictionary<string, VersionedString>(InheritedMembers);
            rval.IsA = IsA == null ? null : new List<string>(IsA);
            rval.Interfaces = Interfaces == null ? null : new List<VersionedString>(Interfaces);
            rval.Members = Members == null ? null : new List<Member>(Members);
            rval.Overloads = Overloads == null ? null : new List<Member>(Overloads);
            rval.ExtensionMethods = ExtensionMethods == null ? null : new List<VersionedString>(ExtensionMethods);
            rval.ExtensionMethods = ExtensionMethods == null ? null : new List<VersionedString>(ExtensionMethods);
            rval.TypeParameters = TypeParameters == null ? null : new List<TypeParameter>(TypeParameters);
            rval.Parameters = Parameters == null ? null : new List<Parameter>(Parameters);
            rval.Attributes = Attributes == null ? null : new List<ECMAAttribute>(Attributes);
            rval.Metadata = Metadata == null ? null : new Dictionary<string, object>(Metadata);
            rval.ExtendedMetadata = ExtendedMetadata == null ? null : new Dictionary<string, object>(ExtendedMetadata);
            rval.Modifiers = Modifiers == null ? null : new SortedList<string, List<string>>(Modifiers);
            rval.AssemblyInfo = AssemblyInfo == null ? null : new List<AssemblyInfo>(AssemblyInfo);
            rval.Monikers = Monikers == null ? null : new HashSet<string>(Monikers);
            return rval;
        }

    }
}
