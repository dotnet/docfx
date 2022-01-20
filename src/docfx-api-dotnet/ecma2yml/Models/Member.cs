using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ECMA2Yaml.Models
{
    public class Member : ReflectionItem
    {
        public string DisplayName { get; set; }
        public string FullDisplayName { get; set; }
        public string Overload { get; set; }
        public bool IsExtensionMethod { get; set; }
        public string TargetUid { get; set; }
        public bool IsIndexer
        {
            get
            {
                return ItemType == ItemType.Property
                    && Signatures.Dict.ContainsKey(ECMADevLangs.CSharp)
                    && Signatures.Dict[ECMADevLangs.CSharp].Any(s => s.Value.Contains("["));
            }
        }
        public List<VersionedString> Implements { get; set; }
        public bool IsEII
        {
            get
            {
                return ItemType != ItemType.Constructor && Name.Contains('.');
            }
        }

        public void BuildName(ECMAStore store)
        {
            DisplayName = ItemType == ItemType.Constructor ? Parent.Name : Name;
            if (DisplayName.StartsWith("op_"))
            {
                DisplayName = DisplayName.Substring("op_".Length);
            }
            var displayNameWithoutEII = DisplayName;

            if (IsEII)
            {
                var typeStr = DisplayName.Substring(0, DisplayName.LastIndexOf('.'));
                var memberStr = DisplayName.Substring(DisplayName.LastIndexOf('.') + 1);
                DisplayName = typeStr.ToDisplayName() + '.' + memberStr;
            }

            string paramPart = null;
            if (Parameters?.Count > 0)
            {
                if (Name == "op_Explicit" || Name == "op_Implicit")
                {
                    var rtype = ReturnValueType.VersionedTypes.First().Value.ToDisplayName();

                    paramPart = string.Format("({0} to {1})", Parameters.First().Type.ToDisplayName(), rtype);
                }
                else if (IsIndexer)
                {
                    paramPart = string.Format("[{0}]", string.Join(", ", Parameters.Select(p => p.Type.ToDisplayName())));
                }
                else
                {
                    paramPart = string.Format("({0})", string.Join(", ", Parameters.Select(p => p.Type.ToDisplayName())));
                }
            }
            else if (ItemType == ItemType.Method || ItemType == ItemType.Constructor)
            {
                paramPart = "()";
            }
            DisplayName += paramPart;
            FullDisplayName = ((Type)Parent).FullName + "." + displayNameWithoutEII + paramPart;
        }

        //The ID of a generic method uses postfix ``n, n is the count of in method parameters, for example, System.Tuple.Create``1(``0)
        public override void Build(ECMAStore store)
        {
            if (DocId != null && DocId.Contains('|'))
            {
                var parts = DocId.Split(':');
                if (parts?.Length == 2)
                {
                    Id = parts[1];
                    if (Id.StartsWith(Parent.Uid))
                    {
                        Id = Id.Substring(Parent.Uid.Length, Id.Length - Parent.Uid.Length).TrimStart('.');
                        return;
                    }
                }
            }
            Id = Name.Replace('.', '#');
            if (TypeParameters?.Count > 0)
            {
                Id = Id.Substring(0, Id.LastIndexOf('<')) + "``" + TypeParameters.Count;
            }
            //handle eii prefix
            Id = Id.Replace('<', '{').Replace('>', '}');
            Id = Id.Replace(',', '@');
            if (Parameters?.Count > 0)
            {
                //Type conversion operator can be considered a special operator whose name is the UID of the target type,
                //with one parameter of the source type.
                //For example, an operator that converts from string to int should be Explicit(System.String to System.Int32).
                if (Name == "op_Explicit" || Name == "op_Implicit")
                {
                    var typeParamsOnType = Parent.TypeParameters?.Select(tp => tp.Name).ToList();
                    var typeParamsOnMember = TypeParameters?.Select(tp => tp.Name).ToList();

                    var rtype = ReturnValueType.VersionedTypes.First().Value;

                    Id += string.Format("({0})~{1}",
                        Parameters.First().Type.ToSpecId(typeParamsOnType, typeParamsOnMember),
                        rtype.ToSpecId(typeParamsOnType, typeParamsOnMember));
                }
                //spec is wrong, no need to treat indexer specially, so comment this part out
                //else if (MemberType == MemberType.Property && Signatures.ContainsKey("C#") && Signatures["C#"].Contains("["))
                //{
                //    Id += string.Format("[{0}]", string.Join(",", GetParameterUids(store)));
                //}
                else
                {
                    Id += string.Format("({0})", string.Join(",", GetParameterUids(store)));
                }
            }

            //special handling for compatibility in UWP legacy MD content
            if (store.UWPMode && DocId != null)
            {
                var pos1 = Id.IndexOf('(');
                var pos2 = DocId.IndexOf('(');
                if (pos1 > 0 && pos2 > 0)
                {
                    Id = Id.Substring(0, pos1) + DocId.Substring(pos2);
                }
            }
        }

        public string GetOverloadId()
        {
            if (string.IsNullOrEmpty(Id))
            {
                return Id;
            }
            var overloadId = Id;
            if (overloadId.Contains("("))
            {
                overloadId = overloadId.Substring(0, overloadId.IndexOf("("));
            }
            if (TypeParameters?.Count > 0)
            {
                var suffix = "``" + TypeParameters.Count;
                if (overloadId.EndsWith(suffix))
                {
                    overloadId = overloadId.Remove(overloadId.Length - suffix.Length);
                }
            }

            return overloadId + "*";
        }

        private List<string> GetParameterUids(ECMAStore store)
        {
            List<string> ids = new List<string>();
            foreach (var p in Parameters)
            {
                var paraUid = p.Type.Replace('+', '.').Replace('<', '{').Replace('>', '}');
                if (p.RefType == "ref" || p.RefType == "out")
                {
                    paraUid += "@";
                }
                var parent = (Type)Parent;
                paraUid = ReplaceGenericInParameterUid(parent.TypeParameters, "`", paraUid);
                paraUid = ReplaceGenericInParameterUid(TypeParameters, "``", paraUid);
                ids.Add(paraUid);
            }

            return ids;
        }

        //Example:System.Collections.Generic.Dictionary`2.#ctor(System.Collections.Generic.IDictionary{`0,`1},System.Collections.Generic.IEqualityComparer{`0})
        private static Dictionary<string, Regex> TypeParameterRegexes = new Dictionary<string, Regex>();
        private string ReplaceGenericInParameterUid(List<TypeParameter> typeParameters, string prefix, string paraUid)
        {
            if (typeParameters?.Count > 0)
            {
                int genericCount = 0;
                foreach (var tp in typeParameters)
                {
                    string genericPara = prefix + genericCount;
                    if (tp.Name == paraUid)
                    {
                        return genericPara;
                    }

                    Regex regex = null;
                    if (TypeParameterRegexes.ContainsKey(tp.Name))
                    {
                        regex = TypeParameterRegexes[tp.Name];
                    }
                    else
                    {
                        regex = new Regex("[^\\w]?" + tp.Name + "[^\\w]", RegexOptions.Compiled);
                        TypeParameterRegexes[tp.Name] = regex;
                    }
                    paraUid = regex.Replace(paraUid, match => match.Value.Replace(tp.Name, genericPara));
                    genericCount++;
                }
            }
            return paraUid;
        }

        public Member ShallowCopy()
        {
            return (Member)this.MemberwiseClone();
        }
    }
}
